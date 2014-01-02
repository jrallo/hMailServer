﻿using NUnit.Framework;
using RegressionTests.Shared;
using hMailServer;

namespace RegressionTests.MIME
{
   [TestFixture]
   public class MessageParsing : TestFixtureBase
   {
      [Test]
      [Description("Test to parse a multi-part message with no text in the main body directly after the header.")]
      public void TestParseMultipartNoBody()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "search@test.com", "test");
         string body = TestSetup.GetResource("Messages.MultipartMessageWithNoMainBodyText.txt");
         SMTPClientSimulator.StaticSendRaw(account.Address, account.Address, body);

         POP3Simulator.AssertMessageCount(account.Address, "test", 1);

         var imapSim = new IMAPSimulator("search@test.com", "test", "INBOX");
         string result = imapSim.Fetch("1 (BODY.PEEK[HEADER] BODY.PEEK[TEXT])");

         imapSim.Logout();
      }
   }
}