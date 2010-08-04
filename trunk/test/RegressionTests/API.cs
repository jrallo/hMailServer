// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using NUnit.Framework;

namespace UnitTest.API
{
    [TestFixture]
    public class Basics : TestFixtureBase
    {
        [Test]
        public void TestSendMessage()
        {
            hMailServer.Account oAccount1 = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

            // Send a message to the account.
            hMailServer.Message oMessage = new hMailServer.Message();

            Assert.AreEqual(0, oMessage.State);

            oMessage.AddRecipient("Martin", oAccount1.Address);
            oMessage.Body = "Všlkommen till verkligheten";
            oMessage.Subject = "Hej";
            oMessage.Save();

            Assert.AreEqual(1, oMessage.State);

            // Check that the message exists
            string message = POP3Simulator.AssertGetFirstMessageText(oAccount1.Address, "test");

            Assert.IsNotEmpty(message);
            Assert.Less(0, message.IndexOf("Hej"));
        }

        [Test]
        public void TestSaveMessageInExistingIMAPFolder()
        {
            hMailServer.Settings settings = SingletonProvider<Utilities>.Instance.GetApp().Settings;

            hMailServer.Account oAccount1 = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

            // Check that the message does not exist
            POP3Simulator.AssertMessageCount(oAccount1.Address, "test", 0);

            // Send a message to the account.
            hMailServer.IMAPFolder folder = oAccount1.IMAPFolders.get_ItemByName("INBOX");

            hMailServer.Message oMessage = folder.Messages.Add();

            Assert.AreEqual(0, oMessage.State);

            oMessage.Body = "Všlkommen till verkligheten";
            oMessage.Subject = "Hej";
            oMessage.Save();

            Assert.AreEqual(2, oMessage.State);
            Assert.IsFalse(oMessage.Filename.Contains(settings.PublicFolderDiskName));
            Assert.IsTrue(oMessage.Filename.Contains(_domain.Name));

            // Check that the message exists
            string message = POP3Simulator.AssertGetFirstMessageText(oAccount1.Address, "test");

            Assert.IsNotEmpty(message);
            Assert.Less(0, message.IndexOf("Hej"));
        }

        [Test]
        public void TestSaveMessageInPublicIMAPFolder()
        {
            hMailServer.Settings settings = SingletonProvider<Utilities>.Instance.GetApp().Settings;
            hMailServer.IMAPFolders publicFolders = settings.PublicFolders;

            hMailServer.IMAPFolder testFolder = publicFolders.Add("TestFolder");
            testFolder.Save();

            // Send a message to the account.
            hMailServer.Message oMessage = testFolder.Messages.Add();

            Assert.AreEqual(0, oMessage.State);

            oMessage.Body = "Všlkommen till verkligheten";
            oMessage.Subject = "Hej";
            oMessage.Save();

            Assert.AreEqual(2, oMessage.State);
            Assert.IsTrue(oMessage.Filename.Contains(settings.PublicFolderDiskName));
            Assert.IsFalse(oMessage.Filename.Contains(_domain.Name));
        }

        [Test]
        public void TestReinitialize()
        {
           string @messageText =
               "From: test@test.com\r\n" +
               "\r\n" +
               "WhatTest\r\n";

           hMailServer.Account account = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");
           Assert.IsTrue(SMTPSimulator.StaticSend(account.Address, account.Address, "First message", "Test message"));
           POP3Simulator.AssertMessageCount(account.Address, "test", 1);

           // Create another message on disk and import it.
           string domainPath = Path.Combine(_application.Settings.Directories.DataDirectory, "test.com");
           string accountPath = Path.Combine(domainPath, "test");
           Directory.CreateDirectory(accountPath);
           string fileName = Path.Combine(accountPath, "something.eml");
           File.WriteAllText(fileName, messageText);
           Assert.IsTrue(_application.Utilities.ImportMessageFromFile(fileName, account.ID));

           // Since the cache isn't refreshed, the message has not yet appeared.
           POP3Simulator.AssertMessageCount(account.Address, "test", 1);
           
           // Reinitialize the server. Should, among other things, clear the cache.
           _application.Reinitialize();

           // Now the message should have appeared.
           POP3Simulator.AssertMessageCount(account.Address, "test", 2);

           POP3Simulator sim = new POP3Simulator();
           sim.ConnectAndLogon(account.Address, "test");
           messageText = sim.RETR(2);
           sim.QUIT();

           Assert.IsTrue(messageText.Contains("WhatTest"), messageText);
           
        }

        private static void SendMessageToTest()
        {
            SMTPSimulator smtp = new SMTPSimulator();
            List<string> recipients = new List<string>();
            recipients.Add("test@test.com");
            smtp.Send("test@test.com", recipients, "Test", "Test message");
        }



        [Test]
        public void TestEventLog()
        {
            hMailServer.Application application = SingletonProvider<Utilities>.Instance.GetApp();

            // First set up a script
            string script = @"Sub OnAcceptMessage(oClient, oMessage)
                               EventLog.Write(""HOWDY"")
                              End Sub";

            hMailServer.Scripting scripting = _settings.Scripting;
            string file = scripting.CurrentScriptFile;
            Utilities.WriteFile(file, script);
            scripting.Enabled = true;
            scripting.Reload();

            // Drop the current event log
            string eventLogFile = _settings.Logging.CurrentEventLog;

            SingletonProvider<Utilities>.Instance.DeleteEventLog();

            // Add an account and send a message to it.
            hMailServer.Account oAccount1 = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

            SendMessageToTest();

            POP3Simulator.AssertGetFirstMessageText(oAccount1.Address, "test");

            Utilities.AssertFileExists(eventLogFile, false);

            // Check that it starts with Unicode markers.
            for (int i = 0; i <= 400; i++)
            {
                try
                {

                    FileStream fs = new FileStream(eventLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    BinaryReader br = new BinaryReader(fs);
                    int i1 = br.ReadByte();
                    int i2 = br.ReadByte();
                    br.Close();
                    fs.Close();

                    Assert.AreEqual(255, i1);
                    Assert.AreEqual(254, i2);

                    break;
                }
                catch (Exception e)
                {
                    if (i == 40)
                        throw e;
                }

                Thread.Sleep(25);
            }
        }

        [Test]
        [Description("Add text to an empty body during sending of attachments")]
        public void TestAddTextToEmptyBody()
        {
            hMailServer.Account account1 = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

            // Send a message to the account.
            string messageText = @"Date: Thu, 03 Jul 2008 22:01:53 +0200\r\n" +
                                   "From: Test <test@test.com>\r\n" +
                                   "MIME-Version: 1.0\r\n" +
                                   "To: test@test.com\r\n" +
                                   "Subject: test\r\n" +
                                   "Content-Type: multipart/mixed;\r\n" +
                                   "  boundary=\"------------050908050500020808050006\"\r\n" +
                                   "\r\n" +
                                   "This is a multi-part message in MIME format.\r\n" +
                                   "--------------050908050500020808050006\r\n" +
                                   "Content-Type: text/plain; charset=ISO-8859-1; format=flowed\r\n" +
                                   "Content-Transfer-Encoding: 7bit\r\n" +
                                   "\r\n" +
                                   "Test\r\n" +
                                   "\r\n" +
                                   "--------------050908050500020808050006\r\n" +
                                   "Content-Type: text/plain;\r\n" +
                                   " name=\"AUTOEXEC.BAT\"\r\n" +
                                   "Content-Transfer-Encoding: base64\r\n" +
                                   "Content-Disposition: inline;\r\n" +
                                   " filename=\"AUTOEXEC.BAT\"\r\n" +
                                   "\r\n" +
                                   "\r\n" +
                                   "--------------050908050500020808050006--\r\n";

            SMTPSimulator.StaticSendRaw("test@test.com", "test@test.com", messageText);

            hMailServer.Message message = Utilities.AssertRetrieveFirstMessage(account1.IMAPFolders.get_ItemByName("INBOX"));
            Assert.AreEqual(1, message.Attachments.Count);
            Assert.AreEqual("AUTOEXEC.BAT", message.Attachments[0].Filename);
        }

        [Test]
        [Category("COM API")]
        [Description("Test deletion of IMAP folders using COM API.")]
        public void TestFolderDeletion()
        {

            // Create a test account
            // Fetch the default domain
            hMailServer.Account account1 = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "folder@test.com", "test");
            hMailServer.IMAPFolder folder = account1.IMAPFolders.Add("TestFolder1");
            folder.Save();

            IMAPSimulator simulator1 = new IMAPSimulator();
            simulator1.ConnectAndLogon(account1.Address, "test");
            string result = simulator1.List();
            Assert.IsTrue(result.Contains(folder.Name));
            simulator1.Disconnect();

            // Delete the folder and confirm it's no longer listed.
            folder.Delete();

            simulator1.ConnectAndLogon(account1.Address, "test");
            result = simulator1.List();
            Assert.IsFalse(result.Contains(folder.Name));
            simulator1.Disconnect();
        }

        [Test]
        public void TestIMAPFolderPermissionAccessGroup()
        {
            hMailServer.Application application = SingletonProvider<Utilities>.Instance.GetApp();

            ;
            hMailServer.Account account1 = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "account1@test.com", "test");
            hMailServer.Group group = SingletonProvider<Utilities>.Instance.AddGroup("TestGroup");

            hMailServer.IMAPFolders publicFolders = _settings.PublicFolders;
            hMailServer.IMAPFolder folder = publicFolders.Add("Share1");
            folder.Save();

            hMailServer.IMAPFolderPermission permission = folder.Permissions.Add();
            permission.PermissionGroupID = group.ID;
            permission.PermissionType = hMailServer.eACLPermissionType.ePermissionTypeGroup;
            permission.Save();

            Assert.AreEqual(permission.Group.Name, group.Name);

            permission = folder.Permissions.Add();
            permission.PermissionAccountID = account1.ID;
            permission.PermissionType = hMailServer.eACLPermissionType.ePermissionTypeUser;
            permission.Save();

            Assert.AreEqual(permission.Account.Address, account1.Address);

        }

        [Test]
        public void TestInternalDateCombinedWithOnDeliverMessage()
        {
            hMailServer.Application application = SingletonProvider<Utilities>.Instance.GetApp();
            hMailServer.Scripting scripting = _settings.Scripting;
            scripting.Language = "JScript";
            // First set up a script
            string script = @"function OnDeliverMessage(oMessage)
                           {
                               EventLog.Write(oMessage.InternalDate);
                           }";


            string file = scripting.CurrentScriptFile;
            Utilities.WriteFile(file, script);
            scripting.Enabled = true;
            scripting.Reload();

            // Add an account and send a message to it.
            hMailServer.Account oAccount1 = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

            SMTPSimulator.StaticSend(oAccount1.Address, oAccount1.Address, "Test", "SampleBody");

            POP3Simulator.AssertMessageCount(oAccount1.Address, "test",1);
            string text = Utilities.ReadExistingTextFile(_settings.Logging.CurrentEventLog);

            string[] columns = text.Split('\t');

            if (columns.Length != 3)
               Assert.Fail("Wrong number of cols: " + text);

            string lastColumn = columns[columns.Length - 1];

            Assert.IsFalse(lastColumn.Contains("00:00:00"), lastColumn);
            Assert.IsTrue(lastColumn.Contains(DateTime.Now.Year.ToString()), lastColumn);
            Assert.IsTrue(lastColumn.Contains(DateTime.Now.Month.ToString()), lastColumn);
            Assert.IsTrue(lastColumn.Contains(DateTime.Now.Day.ToString()), lastColumn);

            
        }


        [Test]
        public void TestDomainDeletion()
        {
            hMailServer.Application app = SingletonProvider<Utilities>.Instance.GetApp();

            Assert.IsNotNull(app.Links.get_Domain(_domain.ID));

            app.Domains.DeleteByDBID(_domain.ID);

            try
            {
                app.Links.get_Domain(_domain.ID);

                Assert.Fail("Didn't throw");
            }
            catch (Exception)
            {
                // should throw => ok
            }
        }

        [Test]
        public void TestCriteriaMatching()
        {
            hMailServer.Application app = SingletonProvider<Utilities>.Instance.GetApp();
            hMailServer.Utilities utilities = app.Utilities;

            Assert.IsTrue(utilities.CriteriaMatch("Test", hMailServer.eRuleMatchType.eMTEquals, "Test"));
            Assert.IsFalse(utilities.CriteriaMatch("Testa", hMailServer.eRuleMatchType.eMTEquals, "Test"));

            Assert.IsTrue(utilities.CriteriaMatch("Test*", hMailServer.eRuleMatchType.eMTWildcard, "Testar!"));
            Assert.IsFalse(utilities.CriteriaMatch("Test*", hMailServer.eRuleMatchType.eMTWildcard, "Tesb"));
        }


        [Test]
        public void TestRetrieveMessageID()
        {
           hMailServer.Application app = SingletonProvider<Utilities>.Instance.GetApp();
           hMailServer.Utilities utilities = app.Utilities;

           // Add an account and send a message to it.
           hMailServer.Account account = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

           SMTPSimulator.StaticSend(account.Address, account.Address, "Test", "SampleBody");
           POP3Simulator.AssertMessageCount(account.Address, "test", 1);

           hMailServer.Message message = account.IMAPFolders.get_ItemByName("INBOX").Messages[0];

           Assert.AreEqual(message.ID, utilities.RetrieveMessageID(message.Filename));
           Assert.AreEqual(0, utilities.RetrieveMessageID(@"C:\some\nonexistant\file"));
         }


        [Test]
        [Description("Test that live log works and that it's reset when enabled.")]
        public void TestLiveLog()
        {
           hMailServer.Application app = SingletonProvider<Utilities>.Instance.GetApp();
           hMailServer.Utilities utilities = app.Utilities;

           hMailServer.Logging logging = app.Settings.Logging;

           logging.EnableLiveLogging(true);

           // Add an account and send a message to it.
           hMailServer.Account account = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

           SMTPSimulator.StaticSend(account.Address, account.Address, "Test", "SampleBody");
           POP3Simulator.AssertMessageCount(account.Address, "test", 1);

           string liveLog = logging.LiveLog;
           Assert.IsTrue(liveLog.Length > 0, liveLog);

           SMTPSimulator.StaticSend(account.Address, account.Address, "Test", "SampleBody");
           POP3Simulator.AssertMessageCount(account.Address, "test", 2);

           logging.EnableLiveLogging(true);

           liveLog = logging.LiveLog;
           Assert.IsFalse(liveLog.Contains("SMTPDeliverer - Message"));
        }

        [Test]
        [Description("Issue 210, Duplicate UIDS when COM Messages.Add is used")]
        public void TestAddMessage()
        {
           hMailServer.Application app = SingletonProvider<Utilities>.Instance.GetApp();
           hMailServer.Utilities utilities = app.Utilities;

           hMailServer.Account account = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

           // Create a new folder.
           hMailServer.IMAPFolder folder = account.IMAPFolders.get_ItemByName("INBOX");
           folder.Save();

           for (int i = 0; i < 3; i++)
           {
              hMailServer.Message message = folder.Messages.Add();
              message.set_Flag(hMailServer.eMessageFlag.eMFSeen, true);
              message.Save();

              POP3Simulator.AssertMessageCount(account.Address, "test", ((i + 1) * 2)-1);

              SMTPSimulator.StaticSend("test@example.com", account.Address, "Test", "Test");
              POP3Simulator.AssertMessageCount(account.Address, "test", (i+1) * 2);
           }

           POP3Simulator.AssertMessageCount(account.Address, "test", 6);

           IMAPSimulator sim = new IMAPSimulator();
           sim.ConnectAndLogon(account.Address, "test");
           sim.SelectFolder("Inbox");

           string response = sim.Fetch("1:6 UID");

           string [] lines = Microsoft.VisualBasic.Strings.Split(response, Environment.NewLine, -1, Microsoft.VisualBasic.CompareMethod.Text);

           List<string> uids =new List<string>();

           foreach (string line in lines)
           {
              int paraPos = line.IndexOf("(");
              int paraEndPos = line.IndexOf(")");

              if (paraPos < 0 || paraEndPos < 0)
                 continue;

              string paraContent = line.Substring(paraPos+1, paraEndPos-paraPos-1);

              if (!uids.Contains(paraContent))
                 uids.Add(paraContent);
          }

           Assert.AreEqual(6, uids.Count);

           // Make sure the UIDS are sorted properly by creating a copy, sort the copy
           // and then compare to original.
           List<string> copy = new List<string>();
           copy.InsertRange(0, uids);
           copy.Sort();

           Assert.AreEqual(copy, uids);
        }

        [Test]
        [Description("Issue 210, Duplicate UIDS when COM Messages.Add is used")]
        public void TestCopyMessage()
        {
           hMailServer.Application app = SingletonProvider<Utilities>.Instance.GetApp();
           hMailServer.Utilities utilities = app.Utilities;

           hMailServer.Account account = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

           // Create a new folder.
           hMailServer.IMAPFolder folder = account.IMAPFolders.get_ItemByName("INBOX");
           folder.Save();

           hMailServer.IMAPFolder someOtherFolder = account.IMAPFolders.Add("SomeOtherFolder");

           for (int i = 0; i < 3; i++)
           {
              hMailServer.Message message = folder.Messages.Add();
              message.set_Flag(hMailServer.eMessageFlag.eMFSeen, true);
              message.Save();

              message.Copy(someOtherFolder.ID);
           }

           SMTPSimulator.StaticSend("test@example.com", account.Address, "Test", "Test");
           
           // Copy back to inbox.
           for (int i = 0; i < 3; i ++ )
           {
              hMailServer.Message message = someOtherFolder.Messages[i];
              message.Copy(folder.ID);
           }

           POP3Simulator.AssertMessageCount(account.Address, "test", 7);

           IMAPSimulator sim = new IMAPSimulator();
           sim.ConnectAndLogon(account.Address, "test");
           sim.SelectFolder("Inbox");
           string response = sim.Fetch("1:7 UID");

           string[] lines = Microsoft.VisualBasic.Strings.Split(response, Environment.NewLine, -1, Microsoft.VisualBasic.CompareMethod.Text);

           List<string> uids = new List<string>();

           foreach (string line in lines)
           {
              int paraPos = line.IndexOf("(");
              int paraEndPos = line.IndexOf(")");

              if (paraPos < 0 || paraEndPos < 0)
                 continue;

              string paraContent = line.Substring(paraPos + 1, paraEndPos - paraPos - 1);

              if (!uids.Contains(paraContent))
                 uids.Add(paraContent);
           }

           Assert.AreEqual(7, uids.Count);

           // Make sure the UIDS are sorted properly by creating a copy, sort the copy
           // and then compare to original.
           List<string> copy = new List<string>();
           copy.InsertRange(0, uids);
           copy.Sort();

           Assert.AreEqual(copy, uids);
        }

        [Test]
        public void BlowfishEncryptShouldNotRequireAdminPrivileges()
        {
           hMailServer.Application app = SingletonProvider<Utilities>.Instance.GetApp();

           hMailServer.Account account = SingletonProvider<Utilities>.Instance.AddAccount(_domain, "test@test.com", "test");

           hMailServer.Application newApp = new hMailServer.Application();
           Assert.IsNotNull(newApp.Authenticate(account.Address, "test"));

           hMailServer.Utilities utilities = newApp.Utilities;

           string encryptedResult = utilities.BlowfishEncrypt("Test");
           Assert.AreNotEqual("Test", encryptedResult, encryptedResult);

           string decrypted = utilities.BlowfishDecrypt(encryptedResult);
           Assert.AreEqual("Test", decrypted, decrypted);
        }
    }
}
