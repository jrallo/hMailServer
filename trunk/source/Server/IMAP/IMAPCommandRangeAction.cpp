// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "IMAPCommandRangeAction.h"
#include "IMAPConnection.h"
#include "../Common/BO/Messages.h"
#include "../Common/BO/Message.h"
#include "../Common/BO/IMAPFolder.h"
#include "../Common/Persistence/PersistentMessage.h"


namespace HM
{
   IMAPCommandRangeAction::IMAPCommandRangeAction() :
      _isUID(false)
   {
    
   }

   IMAPCommandRangeAction::~IMAPCommandRangeAction()
   {

   }

   void
   IMAPCommandRangeAction::SetIsUID(bool bIsUID)
   {
      _isUID = bIsUID;
   }

   bool 
   IMAPCommandRangeAction::GetIsUID()
   {
      return _isUID;
   }

   IMAPResult
   IMAPCommandRangeAction::DoForMails(shared_ptr<IMAPConnection> pConnection, const String &sMailNos, shared_ptr<IMAPCommandArgument> pArgument)
   {
      long lColonPos = -1;

      std::vector<String> sSplitted = StringParser::SplitString(sMailNos, ",");

      if (_isUID)
      {
         boost_foreach(String sCur, sSplitted)
         {
            lColonPos = sCur.Find(_T(":"));

            if (lColonPos >= 0)
            {
               String sFirstPart = sCur.Mid(0, lColonPos);
               String sSecondPart = sCur.Mid(lColonPos + 1);

               unsigned int lStartDBID = _ttoi(sFirstPart);
               unsigned int lEndDBID = -1;
               if (sSecondPart != _T("*"))
                  lEndDBID = _ttoi(sSecondPart);

               std::vector<shared_ptr<Message>> messages = pConnection->GetCurrentFolder()->GetMessages()->GetCopy();

               int index = 0;
               boost_foreach(shared_ptr<Message> pMessage, messages)
               {
                  index++;
                  unsigned int uid = pMessage->GetUID();

                  if (uid >= lStartDBID)
                  {
                     if (lEndDBID == -1 || uid <= lEndDBID)
                     {
                        // UID doesn't fail just because the message is missing.
                        // This is why we don't check the return value.
                        IMAPResult result = DoAction(pConnection, index, pMessage, pArgument);
                        if (result.GetResult() != IMAPResult::ResultOK)
                        {
                           return result;
                        }
                     }
                  }
               }

            }
            else 
            {
               unsigned int uid = _ttoi(sCur);

               unsigned int foundIndex = 0;
               shared_ptr<Messages> messages = pConnection->GetCurrentFolder()->GetMessages();
               shared_ptr<Message> message = messages->GetItemByUID(uid, foundIndex);
               if (!message)
                  continue;
               
               IMAPResult result = DoAction(pConnection, foundIndex, message, pArgument);
               if (result.GetResult() != IMAPResult::ResultOK)
               {
                  return result;
               }
            }
         }            

      }
      else
      {
         boost_foreach(String sCur, sSplitted)
         {
            lColonPos = sCur.Find(_T(":"));

            if (lColonPos >= 0)
            {
               String sFirstPart = sCur.Mid(0, lColonPos);
               String sSecondPart = sCur.Mid(lColonPos + 1);

               int lStartIndex = _ttoi(sFirstPart);
               int lEndIndex = -1;
               if (sSecondPart != _T("*"))
                  lEndIndex = _ttoi(sSecondPart);

               MessagesVector vecMessages = pConnection->GetCurrentFolder()->GetMessages()->GetCopy();
               
               int index = 0;
               boost_foreach(shared_ptr<Message> message, vecMessages)
               {
                  index++;

                  if (index >= lStartIndex)
                  {
                     if (lEndIndex == -1 || index <= lEndIndex)
                     {
                        IMAPResult result = DoAction(pConnection, index, message, pArgument);
                        if (result.GetResult() != IMAPResult::ResultOK)
                        {
                           return result;
                        }
                     }
                  }
               }

            }
            else 
            {
               int messageIndex = _ttoi(sCur);
               shared_ptr<Message> pMessage = pConnection->GetCurrentFolder()->GetMessages()->GetItem(messageIndex-1);

               if (!pMessage)
                  continue;

               IMAPResult result = DoAction(pConnection, messageIndex, pMessage, pArgument);
               if (result.GetResult() != IMAPResult::ResultOK)
               {
                  return result;
               }
            }
         }   

      }

      return IMAPResult();

   }

}
