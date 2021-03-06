// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "StdAfx.h"
#include "SpamTestSPF.h"

#include "SpamTestData.h"
#include "SpamTestResult.h"

#include "AntiSpamConfiguration.h"

#include "../../SMTP/SPF/SPF.h"

#ifdef _DEBUG
#define DEBUG_NEW new(_NORMAL_BLOCK, __FILE__, __LINE__)
#define new DEBUG_NEW
#endif

namespace HM
{
   String 
   SpamTestSPF::GetName() const
   {
      return GetTestName();
   }

   String 
   SpamTestSPF::GetTestName() 
   {
      return "SpamTestSPF";
   }


   bool 
   SpamTestSPF::GetIsEnabled()
   {
      if (Configuration::Instance()->GetAntiSpamConfiguration().GetUseSPF())
         return true;
      else
         return false;
   }

   set<shared_ptr<SpamTestResult> > 
   SpamTestSPF::RunTest(shared_ptr<SpamTestData> pTestData)
   {
      set<shared_ptr<SpamTestResult> > setSpamTestResults;

      String sMessage = "";
      int iScore = 0;

      const IPAddress &originatingAddress = pTestData->GetOriginatingIP();

      if (originatingAddress.IsAny())
         return setSpamTestResults;

      String sExplanation;
      SPF::Result result = SPF::Instance()->Test(originatingAddress.ToString(), pTestData->GetEnvelopeFrom(), sExplanation);
      
      if (result == SPF::Fail)
      {
         // Blocked by SPF.s
         sMessage.Format(_T("Blocked by SPF (%s)"), sExplanation);
         iScore = Configuration::Instance()->GetAntiSpamConfiguration().GetUseSPFScore();

         shared_ptr<SpamTestResult> pResult = shared_ptr<SpamTestResult>(new SpamTestResult(GetName(), SpamTestResult::Fail, iScore, sMessage));
         setSpamTestResults.insert(pResult);
      }      
      else if (result == SPF::Pass)
      {
         shared_ptr<SpamTestResult> pResult = shared_ptr<SpamTestResult>(new SpamTestResult(GetName(), SpamTestResult::Pass, 0, ""));
         setSpamTestResults.insert(pResult);
      }


      return setSpamTestResults;
   }

}