// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "IMAPCommandStore.h"
#include "IMAPStore.h"
#include "IMAPConnection.h"

namespace HM
{
   IMAPCommandStore::IMAPCommandStore()
   {

   }

   IMAPCommandStore::~IMAPCommandStore()
   {

   }

   IMAPResult
   IMAPCommandStore::ExecuteCommand(shared_ptr<IMAPConnection> pConnection, shared_ptr<IMAPCommandArgument> pArgument)
   {


      String sTag = pArgument->Tag();
      String sCommand = pArgument->Command();

      if (!pConnection->IsAuthenticated())
         return IMAPResult(IMAPResult::ResultNo, "Authenticate first");

      if (!pConnection->GetCurrentFolder())
         return IMAPResult(IMAPResult::ResultNo, "No folder selected.");


      shared_ptr<IMAPStore> pStore = shared_ptr<IMAPStore>(new IMAPStore());
      pStore->SetIsUID(false);

      String sResponse; 
      long lMailNoStart = 6;
      long lMailNoEnd = sCommand.Find(_T(" "), lMailNoStart);
      long lMailNoLen = lMailNoEnd - lMailNoStart;
      String sMailNo = sCommand.Mid(lMailNoStart, lMailNoLen);
      String sShowPart = sCommand.Mid(lMailNoEnd);

      pArgument->Command(sShowPart);

      IMAPResult result = pStore->DoForMails(pConnection, sMailNo, pArgument);

      if (result.GetResult() == IMAPResult::ResultOK)
         pConnection->SendAsciiData(pArgument->Tag() + " OK STORE completed\r\n");

      return result;
   }
}
