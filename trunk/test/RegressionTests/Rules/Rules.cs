// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using RegressionTests.SMTP;
using RegressionTests.Shared;
using hMailServer;

namespace RegressionTests.Rules
{
   [TestFixture]
   public class Rules : TestFixtureBase
   {
      private void CreatePrintRecipientCountRule(hMailServer.Rules rules)
      {
         Rule oRule = rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRARunScriptFunction;
         oRuleAction.ScriptFunction = "PrintRecipientCount";
         oRuleAction.Save();

         oRule.Save();

         File.WriteAllText(_settings.Scripting.CurrentScriptFile,
                           "Sub PrintRecipientCount(oMessage)" + Environment.NewLine +
                           " Call EventLog.Write(oMessage.Recipients.Count)" + Environment.NewLine +
                           "End Sub");

         _settings.Scripting.Reload();
      }

      [Test]
      public void ActionAccountRuleMoveToExistingPublicFolder()
      {
         Application application = SingletonProvider<TestSetup>.Instance.GetApp();

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");
         IMAPFolders publicFolders = _settings.PublicFolders;
         IMAPFolder folder = publicFolders.Add("Share1");
         folder.Save();

         IMAPFolderPermission permission = folder.Permissions.Add();
         permission.PermissionAccountID = account1.ID;
         permission.PermissionType = eACLPermissionType.ePermissionTypeUser;
         permission.set_Permission(eACLPermission.ePermissionInsert, true);
         permission.set_Permission(eACLPermission.ePermissionRead, true);
         permission.set_Permission(eACLPermission.ePermissionExpunge, true);
         permission.Save();

         Rule oRule = account1.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "#public.Share1";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         Assert.IsTrue(oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString",
                                  "Detta ska hamna i public folder."));

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "#public.Share1", 1);
      }

      [Test]
      public void ActionAccountRuleMoveToExistingPublicFolderSubStructureWithCreatePermission()
      {
         Application application = SingletonProvider<TestSetup>.Instance.GetApp();

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "account1@test.com", "test");
         IMAPFolders publicFolders = _settings.PublicFolders;
         IMAPFolder folder = publicFolders.Add("Share1");
         folder.Save();

         IMAPFolderPermission permission = folder.Permissions.Add();
         permission.PermissionAccountID = account1.ID;
         permission.PermissionType = eACLPermissionType.ePermissionTypeUser;
         permission.set_Permission(eACLPermission.ePermissionInsert, true);
         permission.set_Permission(eACLPermission.ePermissionRead, true);
         permission.set_Permission(eACLPermission.ePermissionCreate, true);
         permission.set_Permission(eACLPermission.ePermissionLookup, true);
         permission.Save();

         Rule oRule = account1.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "#public.Share1.Sub";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "account1@test.com", "SomeString",
                    "This should end up in the #public.share1.sub since user lacks right.");

         IMAPSimulator.AssertMessageCount("account1@test.com", "test", "#public.Share1.Sub", 1);
      }

      [Test]
      public void ActionAccountRuleMoveToNonExistingPublicFolder()
      {
         Application application = SingletonProvider<TestSetup>.Instance.GetApp();

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");
         IMAPFolders publicFolders = _settings.PublicFolders;

         Rule oRule = account1.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "#public.Share1";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString",
                    "This should end up in the inbox since user lacks right.");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "INBOX", 1);
      }

      [Test]
      [Description("Tessts the bind to address rule action.")]
      public void ActionBindToAddress()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = SingletonProvider<TestSetup>.Instance.GetApp().Rules.Add();
         oRule.Name = "Global rule test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTEquals;
         oRuleCriteria.MatchValue = "SomeString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRABindToAddress;
         oRuleAction.Value = "255.254.253.252";
         oRuleAction.Save();
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "bindtoaddress@example.com", "SomeString",
                    "This mail should not be delivered.");

         TestSetup.AssertRecipientsInDeliveryQueue(0);

         string errorLog = TestSetup.ReadAndDeleteErrorLog();

         Assert.IsTrue(errorLog.Contains("Failed to bind to IP address 255.254.253.252."));
      }

      [Test]
      [Description("Test to delete using a rule. Confirm that the account folder does not contain the files")]
      public void ActionDelete()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTContains;
         oRuleCriteria.MatchValue = "TestString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRADeleteEmail;
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "TestString", "Test 1");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "a", "Test 2");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "TestString", "Test 3");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "b", "Test 2");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 2);

         int fileCount = 0;
         string domainDir = Path.Combine(_application.Settings.Directories.DataDirectory, "test.com");
         string userDir = Path.Combine(domainDir, "ruletest");

         string[] dirs = Directory.GetDirectories(userDir);
         foreach (string dir in dirs)
         {
            string[] files = Directory.GetFiles(dir);
            fileCount += files.Length;
         }

         Assert.AreEqual(2, fileCount);
      }

      [Test]
      public void ActionGlobalMoveToIMAPFolder()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain,
                                                                             "ActionGlobalMoveToIMAPFolder@test.com",
                                                                             "test");

         Rule oRule = SingletonProvider<TestSetup>.Instance.GetApp().Rules.Add();
         oRule.Name = "Global rule test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTNotEquals;
         oRuleCriteria.MatchValue = "SomeString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.GlobalBox";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ActionGlobalMoveToIMAPFolder@test.com", "ActionGlobalMoveToIMAPFolder@test.com", "SomeString",
                    "Detta ska inte hamna i mappen Inbox\\NotEquals");
         oSMTP.Send("ActionGlobalMoveToIMAPFolder@test.com", "ActionGlobalMoveToIMAPFolder@test.com", "SomeStringA",
                    "Detta ska hamna i mappen Inbox\\NotEquals");
         oSMTP.Send("ActionGlobalMoveToIMAPFolder@test.com", "ActionGlobalMoveToIMAPFolder@test.com", "somestring",
                    "Detta ska inte hamna i mappen Inbox\\NotEquals");

         IMAPSimulator.AssertMessageCount("ActionGlobalMoveToIMAPFolder@test.com", "test", "Inbox.GlobalBox", 1);
         IMAPSimulator.AssertMessageCount("ActionGlobalMoveToIMAPFolder@test.com", "test", "Inbox", 2);
      }

      [Test]
      public void ActionGlobalRuleMoveToIMAPFolderPublicFolder()
      {
         Application application = SingletonProvider<TestSetup>.Instance.GetApp();

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");
         IMAPFolders publicFolders = _settings.PublicFolders;
         IMAPFolder folder = publicFolders.Add("Share1");
         folder.Save();

         IMAPFolderPermission permission = folder.Permissions.Add();
         permission.PermissionAccountID = account1.ID;
         permission.PermissionType = eACLPermissionType.ePermissionTypeUser;
         permission.set_Permission(eACLPermission.ePermissionInsert, true);
         permission.set_Permission(eACLPermission.ePermissionRead, true);
         permission.set_Permission(eACLPermission.ePermissionExpunge, true);
         permission.Save();

         Rule oRule = application.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "#public.Share1";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString", "Detta ska hamna i public folder.");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "#public.Share1", 1);
      }

      [Test]
      public void ActionGlobalRuleMoveToIMAPFolderPublicFolderNonExistant()
      {
         Application application = SingletonProvider<TestSetup>.Instance.GetApp();
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = application.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "#public.MyFolder";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         Assert.IsTrue(oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString",
                                  "Detta ska hamna i public folder."));

         // Wait for the folder to be created.
         IMAPFolder folder = TestSetup.AssertFolderExists(_settings.PublicFolders, "MyFolder");

         // Wait for the message to appear.
         TestSetup.AssertFolderMessageCount(folder, 1);

         // Make sure we can't access it.
         bool ok = false;
         try
         {
            IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "#public.MyFolder", 0);
         }
         catch (Exception)
         {
            ok = true;
         }
         Assert.IsTrue(ok);


         // Set permissions on this folder.
         IMAPFolderPermission permission = folder.Permissions.Add();
         permission.PermissionAccountID = account1.ID;
         permission.PermissionType = eACLPermissionType.ePermissionTypeUser;
         permission.set_Permission(eACLPermission.ePermissionInsert, true);
         permission.set_Permission(eACLPermission.ePermissionRead, true);
         permission.set_Permission(eACLPermission.ePermissionExpunge, true);
         permission.Save();


         // Make sure we can access it now.
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "#public.MyFolder", 1);
      }

      [Test]
      public void ActionMoveToIMAPFolder()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTNotEquals;
         oRuleCriteria.MatchValue = "SomeString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.NotEquals";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString",
                    "Detta ska inte hamna i mappen Inbox\\NotEquals");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeStringA",
                    "Detta ska hamna i mappen Inbox\\NotEquals");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "somestring",
                    "Detta ska inte hamna i mappen Inbox\\NotEquals");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.NotEquals", 1);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 2);
      }

      [Test]
      public void ActionOverrideMoveToIMAPFolder()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = SingletonProvider<TestSetup>.Instance.GetApp().Rules.Add();
         oRule.Name = "Global rule test";
         oRule.Active = true;
         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTNotEquals;
         oRuleCriteria.MatchValue = "SomeString";
         oRuleCriteria.Save();
         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.GlobalBox";
         oRuleAction.Save();
         oRule.Save();

         // Account-level rule
         Rule oAccountRule = oAccount.Rules.Add();
         oAccountRule.Name = "Criteria test";
         oAccountRule.Active = true;

         oRuleCriteria = oAccountRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTNotEquals;
         oRuleCriteria.MatchValue = "SomeString";
         oRuleCriteria.Save();

         // Add action
         oRuleAction = oAccountRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.Overriden.Test";
         oRuleAction.Save();

         // Save the rule in the database
         oAccountRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString",
                    "Detta ska inte hamna i mappen Inbox.Overriden.Test");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeStringA",
                    "Detta ska hamna i mappen Inbox.Overriden.Test");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "somestring",
                    "Detta ska inte hamna i mappen Inbox.Overriden.Test");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.Overriden.Test", 1);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 2);
      }

      [Test]
      public void ActionSendUsingRoute()
      {
         int smtpServerPort = TestSetup.GetNextFreePort();

         // add an account to send from
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         // Add a route so we can conenct to localhost.
         Route route = SMTPClientTests.AddRoutePointingAtLocalhost(5, smtpServerPort, false);

         // Add a global send-using-route rule
         Rule oRule = _application.Rules.Add();
         oRule.Name = "Send using route";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTContains;
         oRuleCriteria.MatchValue = "TestString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRASendUsingRoute;
         oRuleAction.RouteID = route.ID;
         oRuleAction.Save();

         oRule.Save();

         // Send message and confirm that the route does not affect it.
         var smtp = new SMTPClientSimulator();
         var recipients = new List<string>();
         recipients.Add("test@test.com");
         if (!smtp.Send("test@test.com", recipients, "Test", "Test message"))
            Assert.Fail("Delivery failed");
         string message = POP3Simulator.AssertGetFirstMessageText(account.Address, "test");
         Assert.IsTrue(message.Contains("Test message"));

         // Send a message and confirm that the rule affects it.
         var deliveryResults = new Dictionary<string, int>();
         deliveryResults["test@nonexistantdomain.com"] = 550;

         
         using (var server = new SMTPServerSimulator(1, smtpServerPort))
         {
            server.AddRecipientResult(deliveryResults);
            server.StartListen();

            // Send the actual message
            recipients = new List<string>();
            recipients.Add("test@nonexistantdomain.com");
            if (!smtp.Send("test@test.com", recipients, "TestString", "Test message"))
               Assert.Fail("Delivery failed");

            server.WaitForCompletion();
         }

         // Submit the bounce message...
         TestSetup.AssertRecipientsInDeliveryQueue(0);

         // Download it.
         message = POP3Simulator.AssertGetFirstMessageText(account.Address, "test");

         Assert.IsTrue(message.Contains("550"));
         Assert.IsTrue(message.Contains("test@nonexistantdomain.com"));
      }

      [Test]
      public void ActionSetHeaderContents()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTContains;
         oRuleCriteria.MatchValue = "TestString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRASetHeaderValue;
         oRuleAction.HeaderName = "SomeHeader";
         oRuleAction.Value = "SomeValue";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "TestString", "Test 1");

         string sContents = POP3Simulator.AssertGetFirstMessageText("ruletest@test.com", "test");

         if (sContents.IndexOf("SomeHeader: SomeValue") <= 0)
            throw new Exception("Message header not set");
      }

      [Test]
      public void CriteriaContains()
      {
         var watch = new Stopwatch();
         watch.Start();

         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTContains;
         oRuleCriteria.MatchValue = "TestString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.Wildcard";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "TestString",
                    "Detta ska hamna i mappen Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "TestStri", "Detta ska inte hamna Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "VaffeTestStringBaffe",
                    "Detta ska hamna i mappen Inbox\\Wildcard");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.Wildcard", 2);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 1);


         Trace.WriteLine(watch.ElapsedMilliseconds);
      }

      [Test]
      [Description("2009-12-08: The Body criteria didn't include the HTML body. It should.")]
      public void CriteriaContainsHTMLBody()
      {
         // Add an account
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = account.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTBody;
         oRuleCriteria.MatchType = eRuleMatchType.eMTContains;
         oRuleCriteria.MatchValue = "MyHTMLBody";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.Wildcard";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         string message = "From: Someone <Someone@example.org>" + Environment.NewLine +
                          "Content-Type: text/html; charset=\"Windows-1251\"" + Environment.NewLine +
                          "Content-Transfer-Encoding: quoted-printable" + Environment.NewLine +
                          Environment.NewLine +
                          "<HTML><Center>MyHTMLBody</Center></HTML>" + Environment.NewLine;

         oSMTP.SendRaw("someone@example.org", account.Address, message);

         // The message should be placed in the Wildcard folder, since the HTML body of the message contains MyHTMLBody.
         IMAPSimulator.AssertMessageCount(account.Address, "test", "Inbox.Wildcard", 1);
         IMAPSimulator.AssertMessageCount(account.Address, "test", "Inbox", 0);
      }

      [Test]
      public void CriteriaEquals()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTEquals;
         oRuleCriteria.MatchValue = "TestString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.Wildcard";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "TestString",
                    "Detta ska hamna i mappen Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "teststring", "Detta ska hamna Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "Testar",
                    "Detta ska inte hamna i mappen Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "teststring vaffe",
                    "Detta ska inte hamna i mappen Inbox\\Wildcard");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.Wildcard", 2);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 2);
      }


      [Test]
      public void CriteriaGreaterThan()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "2";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.GreaterThan";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "0", "Detta ska inte hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "1", "Detta ska inte hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "2", "Detta ska inte hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "3", "Detta ska hamna i mappen Inbox\\GreaterThan");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "4", "Detta ska hamna i mappen Inbox\\GreaterThan");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 3);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.GreaterThan", 2);
      }

      [Test]
      public void CriteriaLessThan()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTLessThan;
         oRuleCriteria.MatchValue = "2";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.LessThan";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "0", "Detta ska hamna i mappen Inbox\\LessThan");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "2", "Detta ska hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "3", "Detta ska hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "4", "Detta ska hamna i mappen Inbox");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.LessThan", 1);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 3);
      }

      [Test]
      public void CriteriaNotEquals()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTNotEquals;
         oRuleCriteria.MatchValue = "SomeString";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.NotEquals";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString",
                    "Detta ska inte hamna i mappen Inbox\\NotEquals");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeStringA",
                    "Detta ska hamna i mappen Inbox\\NotEquals");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "somestring",
                    "Detta ska inte hamna i mappen Inbox\\NotEquals");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.NotEquals", 1);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 2);
      }

      [Test]
      public void CriteriaRegEx()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTRegExMatch;
         oRuleCriteria.MatchValue = "[a-f]*";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.RegEx";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "abc", "Detta ska hamna i mappen Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "abcdef", "Detta ska hamna i mappen Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "abcdefghi",
                    "Detta ska inte hamna i mappen Inbox\\Wildcard");

         TestSetup.AssertRecipientsInDeliveryQueue(0);

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.RegEx", 2);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 1);
      }

      [Test]
      public void CriteriaWildcardExactMatch()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTWildcard;
         oRuleCriteria.MatchValue = "Exact wildcard";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.Wildcard";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "Exact wildcard",
                    "Detta ska hamna i mappen Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "Exact wildcard",
                    "Detta ska hamna i mappen Inbox\\Wildcard");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.Wildcard", 2);
      }

      [Test]
      public void CriteriaWildcardNoCase()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain,
                                                                             "CriteriaWildcardNoCase@test.com",
                                                                             "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTWildcard;
         oRuleCriteria.MatchValue = "Exact*Match";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.Wildcard";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("CriteriaWildcardNoCase@test.com", "CriteriaWildcardNoCase@test.com", "exact Test match",
                    "Detta ska hamna i mappen Inbox\\Wildcard");

         IMAPSimulator.AssertMessageCount("CriteriaWildcardNoCase@test.com", "test", "Inbox.Wildcard", 1);
      }

      [Test]
      public void CriteriaWildcardPartialMatch()
      {
         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         Rule oRule = oAccount.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "Subject";
         oRuleCriteria.MatchType = eRuleMatchType.eMTWildcard;
         oRuleCriteria.MatchValue = "Exact*Match";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "INBOX.Wildcard";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "Exact Test Match",
                    "Detta ska hamna i mappen Inbox\\Wildcard");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "ExactMatchArInte",
                    "Detta ska inte hamna Inbox\\Wildcard");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.Wildcard", 1);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 1);
      }

      [Test]
      public void MovedToRenamedPublicFolder()
      {
         Application application = SingletonProvider<TestSetup>.Instance.GetApp();

         _settings.IMAPPublicFolderName = "Public";

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");
         IMAPFolders publicFolders = _settings.PublicFolders;
         IMAPFolder folder = publicFolders.Add("Share1");
         folder.Save();

         IMAPFolderPermission permission = folder.Permissions.Add();
         permission.PermissionAccountID = account1.ID;
         permission.PermissionType = eACLPermissionType.ePermissionTypeUser;
         permission.set_Permission(eACLPermission.ePermissionInsert, true);
         permission.set_Permission(eACLPermission.ePermissionRead, true);
         permission.set_Permission(eACLPermission.ePermissionExpunge, true);
         permission.Save();

         Rule oRule = application.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "Public.Share1";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString", "Detta ska hamna i public folder.");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Public.Share1", 1);
      }

      [Test]
      public void TestBasics()
      {
         // Fetch the default domain

         // Add an account
         Account oAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");

         // Add a rule to this account.
         SingletonProvider<TestSetup>.Instance.AddSpamRule(oAccount);
         SingletonProvider<TestSetup>.Instance.AddCorporateRule(oAccount);
         SingletonProvider<TestSetup>.Instance.AddExactMatchRule(oAccount);

         var oSMTP = new SMTPClientSimulator();


         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "**SPAM** INBOX->SPAM",
                    "Detta ska hamna i mappen Inbox\\Spam");

         // Corporate folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "**CORPORATE** INBOX->CORPORATE",
                    "Detta ska hamna i mappen Inbox\\Corporate");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "CORPORATE EXACT MATCH",
                    "Detta ska hamna i mappen Inbox\\Corporate");

         // Inbox folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "**CORPORATE EXACT MATCH**",
                    "Detta ska hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "INBOX", "Detta ska hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "INBOX", "Detta ska hamna i mappen Inbox");
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "INBOX", "Detta ska hamna i mappen Inbox");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.Spam", 1);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox.Corporate", 2);
         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "Inbox", 4);

         // Test move to imap with mail with multiple recipients.

         Account oAccount1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest-m1@test.com", "test");
         Account oAccount2 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest-m2@test.com", "test");
         SingletonProvider<TestSetup>.Instance.AddSpamRule(oAccount1);

         // Send email to both recipients
         var lstRecipients = new List<string> {"ruletest-m1@test.com", "ruletest-m2@test.com"};

         const string sBody = "Test of sending same email to multiple accounts.";

         oSMTP.Send(oAccount1.Address, lstRecipients, "**SPAM** INBOX->SPAM", sBody);

         IMAPSimulator.AssertMessageCount(oAccount1.Address, "test", "Inbox.Spam", 1);
         IMAPSimulator.AssertMessageCount(oAccount2.Address, "test", "Inbox", 1);

         var sim = new IMAPSimulator();
         sim.ConnectAndLogon(oAccount2.Address, "test");
         Assert.IsFalse(sim.SelectFolder("Inbox.Spam"));
      }

      [Test]
      [Description("Test that creating a message copy in a rule works using a account rule.")]
      public void TestCreateCopyAccountRule()
      {
         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest1@test.com", "test");
         Account account2 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest2@test.com", "test");
         Account account3 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest3@test.com", "test");

         // Set up a rule to forward from account1 to 2 and 3.
         Rule oRule = account1.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;
         oRule.UseAND = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Only send one copy.
         oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "X-hMailServer-LoopCount";
         oRuleCriteria.MatchType = eRuleMatchType.eMTLessThan;
         oRuleCriteria.MatchValue = "1";
         oRuleCriteria.Save();

         // Set up the actions to forward.
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRACreateCopy;
         oRuleAction.To = "ruletest2@test.com";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Test to send the message to account 1. Make sure a copy is created by this rule.
         oSMTP.Send(account1.Address, account1.Address, "Test", "Test message.");
         TestSetup.AssertRecipientsInDeliveryQueue(0, true);
         IMAPSimulator.AssertMessageCount(account1.Address, "test", "Inbox", 2);

         string firstTemp = POP3Simulator.AssertGetFirstMessageText(account1.Address, "test");
         string secondTemp = POP3Simulator.AssertGetFirstMessageText(account1.Address, "test");

         // This is where it gets really ugly. The order of the two deliveries
         // are not defined. The message created by the rule could be delivered
         // before the first message.
         string first = "";
         string second = "";

         if (secondTemp.Contains("X-hMailServer-LoopCount"))
         {
            first = firstTemp;
            second = secondTemp;
         }
         else
         {
            first = secondTemp;
            second = firstTemp;
         }

         Assert.IsFalse(first.Contains("X-hMailServer-LoopCount: 1"), first);
         Assert.IsFalse(first.Contains("X-CopyRule: Criteria test"), first);
         Assert.IsTrue(second.Contains("X-hMailServer-LoopCount"), second);
         Assert.IsTrue(second.Contains("X-CopyRule: Criteria test"), second);
      }

      [Test]
      [Description("Test that creating a message copy in a rule works using a global rule.")]
      public void TestCreateCopyGlobalRule()
      {
         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest1@test.com", "test");
         Account account2 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest2@test.com", "test");

         // Set up a rule to forward from account1 to 2 and 3.
         Rule oRule = _application.Rules.Add();
         oRule.Name = "CriteriaTest";
         oRule.Active = true;
         oRule.UseAND = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Only send one copy.
         oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = false;
         oRuleCriteria.HeaderField = "X-hMailServer-LoopCount";
         oRuleCriteria.MatchType = eRuleMatchType.eMTLessThan;
         oRuleCriteria.MatchValue = "1";
         oRuleCriteria.Save();

         // Set up the actions to create a copy
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRACreateCopy;
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Test to send the message to account 1. Make sure a copy is created by this rule.
         oSMTP.Send(account1.Address, new List<string> {account1.Address, account2.Address}, "Test", "Test message.");
         TestSetup.AssertRecipientsInDeliveryQueue(0, true);
         IMAPSimulator.AssertMessageCount(account1.Address, "test", "Inbox", 2);
         IMAPSimulator.AssertMessageCount(account2.Address, "test", "Inbox", 2);

         /*
          * The delivery order is not guaranteed. The copied message may be delivered
          * before the original message. Check both situations.
          * 
          */
         string first = POP3Simulator.AssertGetFirstMessageText(account1.Address, "test");
         string second = POP3Simulator.AssertGetFirstMessageText(account1.Address, "test");

         if (first.Contains("X-hMailServer-LoopCount: 1"))
         {
            Assert.IsFalse(second.Contains("X-CopyRule: CriteriaTest"), first);
            Assert.IsTrue(first.Contains("X-hMailServer-LoopCount"), first);
         }
         else
         {
            Assert.IsTrue(second.Contains("X-hMailServer-LoopCount: 1"), second);
            Assert.IsTrue(second.Contains("X-CopyRule: CriteriaTest"), first);
         }

         first = POP3Simulator.AssertGetFirstMessageText(account2.Address, "test");
         second = POP3Simulator.AssertGetFirstMessageText(account2.Address, "test");

         if (first.Contains("X-hMailServer-LoopCount: 1"))
         {
            Assert.IsFalse(second.Contains("X-hMailServer-LoopCount"), first);
            Assert.IsTrue(first.Contains("X-CopyRule: CriteriaTest"), first);
         }
         else
         {
            Assert.IsTrue(second.Contains("X-hMailServer-LoopCount: 1"), second);
            Assert.IsTrue(second.Contains("X-CopyRule: CriteriaTest"), first);
         }
      }

      [Test]
      [Description("Test the delivery attempts criteria.")]
      public void TestDeliveryAttempts()
      {
         // Add an account
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");
         Account adminAccount = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "admin@test.com", "test");

         // Set up a rule to forward from account1 to 2 and 3.
         Rule oRule = _application.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;
         oRule.UseAND = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTDeliveryAttempts;
         oRuleCriteria.MatchType = eRuleMatchType.eMTEquals;
         oRuleCriteria.MatchValue = "2";
         oRuleCriteria.Save();

         // The second time we try to deliver an email, forward a copy to the admin!
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAForwardEmail;
         oRuleAction.To = adminAccount.Address;
         oRuleAction.Save();

         // And then delete it.
         oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRADeleteEmail;
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();


         var deliveryResults = new Dictionary<string, int>();
         deliveryResults["ahem@dummy-example.com"] = 452;

         int smtpServerPort = TestSetup.GetNextFreePort();
         using (var smtpServer = new SMTPServerSimulator(1, smtpServerPort))
         {
            smtpServer.AddRecipientResult(deliveryResults);
            smtpServer.StartListen();

            // Add a route so we can connect to localhost.
            SMTPClientTests.AddRoutePointingAtLocalhost(2, smtpServerPort, false);

            var oSMTP = new SMTPClientSimulator();

            // Test to send the message to account 1. Make sure a copy is created by this rule.
            oSMTP.Send(account.Address, new List<string> {"ahem@dummy-example.com"}, "Test", "Test message.");

            smtpServer.WaitForCompletion();
         }

         TestSetup.AssertRecipientsInDeliveryQueue(0, true);

         string first = POP3Simulator.AssertGetFirstMessageText(adminAccount.Address, "test");

         Assert.IsTrue(first.Contains("X-hMailServer-LoopCount: 1"), first);

         POP3Simulator.AssertMessageCount(account.Address, "test", 0);
      }

      [Test]
      [Description("Test that forwarding set up in a rule works.")]
      public void TestForward()
      {
         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest1@test.com", "test");
         Account account2 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest2@test.com", "test");
         Account account3 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest3@test.com", "test");

         // Set up a rule to forward from account1 to 2 and 3.
         Rule oRule = account1.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Set up the actions to forward.
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAForwardEmail;
         oRuleAction.To = "ruletest2@test.com";
         oRuleAction.Save();

         RuleAction oRuleAction2 = oRule.Actions.Add();
         oRuleAction2.Type = eRuleActionType.eRAForwardEmail;
         oRuleAction2.To = "ruletest3@test.com";
         oRuleAction2.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Test to send the messge to account 1.
         oSMTP.Send(account1.Address, account1.Address, "Test", "Test message.");

         IMAPSimulator.AssertMessageCount(account1.Address, "test", "Inbox", 1);
         TestSetup.AssertRecipientsInDeliveryQueue(0);
         IMAPSimulator.AssertMessageCount(account3.Address, "test", "Inbox", 1);
         IMAPSimulator.AssertMessageCount(account2.Address, "test", "Inbox", 1);
      }

      [Test]
      [Description("Test to move to a public folder without permission.")]
      public void TestMoveToPublicFolderWithoutPermission()
      {
         Application application = SingletonProvider<TestSetup>.Instance.GetApp();

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest@test.com", "test");
         IMAPFolders publicFolders = _settings.PublicFolders;
         IMAPFolder folder = publicFolders.Add("Share1");
         folder.Save();

         IMAPFolderPermission permission = folder.Permissions.Add();
         permission.PermissionAccountID = account1.ID;
         permission.PermissionType = eACLPermissionType.ePermissionTypeUser;
         permission.set_Permission(eACLPermission.ePermissionInsert, true);
         permission.set_Permission(eACLPermission.ePermissionRead, true);
         permission.set_Permission(eACLPermission.ePermissionExpunge, true);
         permission.Save();

         Rule oRule = account1.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Add action
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAMoveToImapFolder;
         oRuleAction.IMAPFolder = "#public.Share1.Sub";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Spam folder
         oSMTP.Send("ruletest@test.com", "ruletest@test.com", "SomeString",
                    "This should end up in the inbox since user lacks right.");

         IMAPSimulator.AssertMessageCount("ruletest@test.com", "test", "INBOX", 1);
      }

      [Test]
      [Description("Issue 338, Recipient Count is wrong under certain circumstances")]
      public void TestRecipientCountInAccountLevelRule()
      {
         _settings.Scripting.Enabled = true;

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest1@test.com", "test");
         Account account2 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest2@test.com", "test");

         CreatePrintRecipientCountRule(account1.Rules);

         SMTPClientSimulator.StaticSend(account1.Address, account1.Address, "SomeString",
                                        "Detta ska inte hamna i mappen Inbox.Overriden.Test");
         TestSetup.AssertRecipientsInDeliveryQueue(0);
         // This should print a single recipient.
         string eventLogText = TestSetup.ReadExistingTextFile(TestSetup.GetEventLogFileName());
         TestSetup.AssertDeleteFile(TestSetup.GetEventLogFileName());
         Assert.IsTrue(eventLogText.Contains("\"1\""), eventLogText);

         // Send message to two recipients. Recipient should still be one, since it's an account-level rule.
         SMTPClientSimulator.StaticSend(account1.Address, new List<string> {account1.Address, account2.Address},
                                        "SomeString", "Detta ska inte hamna i mappen Inbox.Overriden.Test");

         TestSetup.AssertRecipientsInDeliveryQueue(0);
         // This should print a single recipient.
         eventLogText = TestSetup.ReadExistingTextFile(TestSetup.GetEventLogFileName());
         TestSetup.AssertDeleteFile(TestSetup.GetEventLogFileName());
         Assert.IsTrue(eventLogText.Contains("\"1\""), eventLogText);
      }

      [Test]
      [Description("Issue 338, Recipient Count is wrong under certain circumstances")]
      public void TestRecipientCountInGlobalRule()
      {
         _settings.Scripting.Enabled = true;

         // Add an account
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest1@test.com", "test");
         Account account2 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest2@test.com", "test");

         CreatePrintRecipientCountRule(_application.Rules);

         SMTPClientSimulator.StaticSend(account1.Address, account1.Address, "SomeString",
                                        "Detta ska inte hamna i mappen Inbox.Overriden.Test");
         TestSetup.AssertRecipientsInDeliveryQueue(0);
         // This should print a single recipient.
         string eventLogText = TestSetup.ReadExistingTextFile(TestSetup.GetEventLogFileName());
         TestSetup.AssertDeleteFile(TestSetup.GetEventLogFileName());
         Assert.IsTrue(eventLogText.Contains("\"1\""), eventLogText);

         // Send message to two recipients. 
         SMTPClientSimulator.StaticSend(account1.Address, new List<string> {account1.Address, account2.Address},
                                        "SomeString", "Detta ska inte hamna i mappen Inbox.Overriden.Test");

         TestSetup.AssertRecipientsInDeliveryQueue(0);
         // This should print a two recipients. Global rule is affected before message reaches recipients.
         eventLogText = TestSetup.ReadExistingTextFile(TestSetup.GetEventLogFileName());
         TestSetup.AssertDeleteFile(TestSetup.GetEventLogFileName());
         Assert.IsTrue(eventLogText.Contains("\"2\""), eventLogText);
      }

      [Test]
      [Description("Test that replying using rule works.")]
      public void TestReply()
      {
         // Add accounts
         Account account1 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest1@test.com", "test");
         Account account2 = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "ruletest2@test.com", "test");

         // Set up a rule to reply to any message sent to account2.
         Rule oRule = account2.Rules.Add();
         oRule.Name = "Criteria test";
         oRule.Active = true;

         RuleCriteria oRuleCriteria = oRule.Criterias.Add();
         oRuleCriteria.UsePredefined = true;
         oRuleCriteria.PredefinedField = eRulePredefinedField.eFTMessageSize;
         oRuleCriteria.MatchType = eRuleMatchType.eMTGreaterThan;
         oRuleCriteria.MatchValue = "0";
         oRuleCriteria.Save();

         // Set up the actions to forward.
         RuleAction oRuleAction = oRule.Actions.Add();
         oRuleAction.Type = eRuleActionType.eRAReply;
         oRuleAction.FromAddress = account2.Address;
         oRuleAction.FromName = "Rule Test 2";
         oRuleAction.Subject = "Autoreply";
         oRuleAction.Save();

         // Save the rule in the database
         oRule.Save();

         var oSMTP = new SMTPClientSimulator();

         // Test to send the message to account 2.
         oSMTP.Send(account1.Address, account2.Address, "Test", "Test message.");
         IMAPSimulator.AssertMessageCount(account2.Address, "test", "Inbox", 1);

         // Make sure a reply is sent back to account 1.
         TestSetup.AssertRecipientsInDeliveryQueue(0);
         Message message = TestSetup.AssertGetFirstMessage(account1, "Inbox");

         Assert.IsNullOrEmpty(message.FromAddress);
         Assert.AreEqual("auto-replied", message.get_HeaderValue("Auto-Submitted"));
      }
   }
}