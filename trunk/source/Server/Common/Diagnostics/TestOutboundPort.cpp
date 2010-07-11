// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "TestOutboundPort.h"
#include "TestConnect.h"

#include "../../SMTP/SMTPConfiguration.h"


namespace HM
{
   TestOutboundPort::TestOutboundPort()
   {

   }

   TestOutboundPort::~TestOutboundPort()
   {

   }

   DiagnosticResult
   TestOutboundPort::PerformTest()
   {
      DiagnosticResult diagResult;
      diagResult.SetName("Test outbound port");
      diagResult.SetDescription("Tests to connect to a remote SMTP server.");

      String runDetails;


      String localAddressStr = Configuration::Instance()->GetSMTPConfiguration()->GetSMTPDeliveryBindToIP();
      String smtpHost = Configuration::Instance()->GetSMTPConfiguration()->GetSMTPRelayer();
      int smtpPort = Configuration::Instance()->GetSMTPConfiguration()->GetSMTPRelayerPort();

      if (smtpHost.GetLength() == 0)
      {
         // Test to connect to mail.hmailserver.com
         runDetails.AppendFormat(_T("SMTP relayer not in use. Attempting mail.hmailserver.com:25\r\n"));

         smtpHost = "mail.hmailserver.com";
         smtpPort = 25;
      }
      else
      {
         runDetails.AppendFormat(_T("SMTP relayer is in use.\r\n"));
      }
      
      TestConnect connTest;
      
      if (connTest.PerformTest(localAddressStr, smtpHost, smtpPort, runDetails))
      {
         diagResult.SetDetails((runDetails));
         diagResult.SetSuccess(true);
      }
      else
      {
         diagResult.SetDetails((runDetails));
         diagResult.SetSuccess(false);
      }

      return diagResult;
   }


   
      
}
