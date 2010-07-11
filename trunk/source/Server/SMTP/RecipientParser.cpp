// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"

#include "RecipientParser.h"

#include "SMTPConfiguration.h"

#include "PlusAddressing.h"

#include "../Common/Application/ObjectCache.h"
#include "../common/Cache/CacheContainer.h"

#include "../Common/BO/Domain.h"
#include "../Common/BO/Alias.h"
#include "../Common/BO/Routes.h"
#include "../Common/BO/DistributionList.h"

#include "../Common/BO/DistributionListRecipients.h"
#include "../Common/BO/DistributionListRecipient.h"
#include "../Common/BO/MessageRecipient.h"
#include "../Common/BO/MessageRecipients.h"
#include "../common/BO/RouteAddresses.h"
#include "../common/BO/Account.h"
#include "../Common/BO/DomainAliases.h"

#include "../Common/Persistence/PersistentDistributionListRecipient.h"


namespace HM
{

   const String CONST_UNKNOWN_USER = "550 Unknown user";

   RecipientParser::RecipientParser()
   {

   }

   RecipientParser::~RecipientParser()
   {

   }

   RecipientParser::DeliveryPossibility
   RecipientParser::CheckDeliveryPossibility(bool bSenderIsAuthed, String sSender, const String &sOriginalRecipient, String &sErrMsg, bool &bTreatSecurityAsLocal, int iRecursionLevel)
   {
      bool bDomainIsLocal = false;
      bTreatSecurityAsLocal = false;

      // Apply domain name aliases to the sender address. Can be done
      // outside the loop since this won't be recursed.
      shared_ptr<DomainAliases> pDA = ObjectCache::Instance()->GetDomainAliases();
      sSender = pDA->ApplyAliasesOnAddress(sSender);

      String recipientAddress = sOriginalRecipient;

      while (true)
      {
         iRecursionLevel ++;

         if (iRecursionLevel > 25)
         {
            // Extreme aliasing. disallow
            sErrMsg = "550 Mail server configuration error. Too many recursive forwards.";
            return DP_RecipientUnknown;
         }

         // Apply domain name aliases on the recipient address.
         const String primaryAddressWithoutPlusaddressing = pDA->ApplyAliasesOnAddress(recipientAddress);
         const String primaryDomain = StringParser::ExtractDomain(primaryAddressWithoutPlusaddressing);
         
         shared_ptr<const Domain> pDomain = CacheContainer::Instance()->GetDomain(primaryDomain);
         bDomainIsLocal = pDomain;

         // Apply plus addressing on the recipient address
         const String primaryAddress = PlusAddressing::ExtractAccountAddress(primaryAddressWithoutPlusaddressing, pDomain); 

         // If this is a local domain, we should check for accounts, aliases and distribution lists.
         if (bDomainIsLocal)
         {
            if (iRecursionLevel == 1)
            {
               // Why only if iRecurse == 1?
               // Because:
               // If you have set up an alias pointing from user@local.com, user@external.com
               // you want to treat is as local even if the end account is remote.
               bTreatSecurityAsLocal = true;
            }

            if (!pDomain->GetIsActive())
            {
               sErrMsg = "550 Domain has been disabled.";
               return DP_RecipientUnknown;
            }

            // Check for an account with this name.
            shared_ptr<const Account> pAccount = CacheContainer::Instance()->GetAccount(primaryAddress);
            if (pAccount)
            {
               if (pAccount->GetActive())
               {
                  bDomainIsLocal = true;
                  return DP_Possible;
               }
               else
               {
                  sErrMsg = "550 Account is not active.";
                  return DP_RecipientUnknown;
               }
            }

            // If no account found, check if an alias with this name exists.
            shared_ptr<const Alias> pAlias = CacheContainer::Instance()->GetAlias(primaryAddress);
            if (pAlias)
            {
               if (pAlias->GetIsActive())
               {
                  recipientAddress = pAlias->GetValue();
                  continue;
               }
               else
               {
                  sErrMsg = "550 Alias is not active.";
                  return DP_RecipientUnknown;
               }
            }

            // Check if distributionlist with this address exists.
            shared_ptr<const DistributionList> pList = CacheContainer::Instance()->GetDistributionList(primaryAddress);
            if (pList)
            {
               if (!pList->GetActive())
               {
                  sErrMsg = "550 Distribution list is not active.";
                  return DP_RecipientUnknown;
               }

               // Need to check if this sender is authorized to send
               // to this distribution list.
               if (_UserCanSendToList(sSender, bSenderIsAuthed, pList, sErrMsg, iRecursionLevel) == DP_PermissionDenied)
                  return DP_PermissionDenied;


               bDomainIsLocal = true;
               return DP_Possible;
            }

            // OK, we are now finished looking through the domain.
         }

         // We have not found the recipient yet. Check if the original address matches a route.
         String recipientDomain = StringParser::ExtractDomain(recipientAddress);
         vector<shared_ptr<Route> > vecRoutes = Configuration::Instance()->GetSMTPConfiguration()->GetRoutes()->GetItemsByName(recipientDomain);
         
         if (vecRoutes.size() > 0)
         {
            vector<shared_ptr<Route> >::iterator iter = vecRoutes.begin();
            vector<shared_ptr<Route> >::iterator iterEnd = vecRoutes.end();

            for (; iter != iterEnd; iter++)
            {
               shared_ptr<Route> pRoute = (*iter);

               if (pRoute->ToAllAddresses() || pRoute->GetAddresses()->GetItemByName(recipientAddress))
               {
                  if (iRecursionLevel == 1)
                     bTreatSecurityAsLocal = pRoute->GetTreatRecipientAsLocalDomain();

                  return DP_Possible;
               }
            }

            // We found routes matching the recipients domain, but the recipient 
            // doesn't exist in any of them.
            sErrMsg = "550 Recipient not in route list.";
            return DP_RecipientUnknown;
         }

         // If this is a local domain, try to find a catch-all 
         // account for this domain.

         if (pDomain)
         {
            String sPostMaster = pDomain->GetPostmaster();

            if (!sPostMaster.IsEmpty())
            {
               // Could not find the address, but a post master was specified,
               // so we'll send to him instead.
               // Found an alias.
               recipientAddress = sPostMaster;
               continue;
            }
         }
         else
         {
            // Domain is not local. SMTPConnection should determine
            // whether the sender is allowed to send.
            return DP_Possible;
         }

         sErrMsg = CONST_UNKNOWN_USER;
         return DP_RecipientUnknown;
      }
   }

   void 
   RecipientParser::CreateMessageRecipientList(const String &sRecipientAddress, shared_ptr<MessageRecipients> pRecipients, bool &recipientOK)
   {
      recipientOK = false;

      try
      {
         long lRecurse = 0;
         _CreateMessageRecipientList(sRecipientAddress, sRecipientAddress, 0, pRecipients, recipientOK);
      }
      catch (...)
      {
         ErrorManager::Instance()->ReportError(ErrorManager::Medium, 4381, "RecipientParser::CreateMessageRecipientList", "An error occurred while creating message recipient list.");
         throw;
      }
   }

   void
   RecipientParser::_CreateMessageRecipientList(const String &recipientAddress, const String &sOriginalAddress, long lRecurse, shared_ptr<MessageRecipients> pRecipients, bool &recipientOK)
   {
      lRecurse++;

      if (lRecurse>25)
      {
         // To deep recursion! 
         return;
      }

      shared_ptr<DomainAliases> pDA = ObjectCache::Instance()->GetDomainAliases();
      String primaryAddress = pDA->ApplyAliasesOnAddress(recipientAddress);
      String primaryDomain = StringParser::ExtractDomain(primaryAddress);

      // First check if the domain is remote. If it is, we don't really
      // have to care what type of email this is.

      
      shared_ptr<const Domain> pDomain = CacheContainer::Instance()->GetDomain(primaryDomain);
      
      // Apply plus addressing on the recipient address
      primaryAddress = PlusAddressing::ExtractAccountAddress(primaryAddress, pDomain); 

      bool bIsLocalDomain = pDomain ? true : false;
      if (bIsLocalDomain)
      {
         // First check if this domain is really active.
         if (!pDomain->GetIsActive())
            return;

         // Check if there exists a account with this address.
         shared_ptr<const Account> pAccount = CacheContainer::Instance()->GetAccount(primaryAddress);

         if (pAccount)
         {
            if (!pAccount->GetActive())
               return;

            shared_ptr<MessageRecipient> NewRecipient = shared_ptr<MessageRecipient>(new MessageRecipient);
         
            NewRecipient->SetLocalAccountID(pAccount->GetID());
            NewRecipient->SetAddress(primaryAddress);
            NewRecipient->SetOriginalAddress(sOriginalAddress);
            NewRecipient->SetIsLocalName(true);

            recipientOK = true;

            _AddRecipient(pRecipients, NewRecipient);

            return;
         }
   
         // Check if there is a alias with this address.
         shared_ptr<const Alias> pAlias = CacheContainer::Instance()->GetAlias(primaryAddress);

         if (pAlias)
         {
            if (!pAlias->GetIsActive())
               return;

            _CreateMessageRecipientList(pAlias->GetValue(), sOriginalAddress, lRecurse, pRecipients, recipientOK);

            return;
         }  

         // Check if there is a distribution list with this address.
         shared_ptr<const DistributionList> pListADO = CacheContainer::Instance()->GetDistributionList(primaryAddress);

         if (pListADO)
         {
            if (!pListADO->GetActive())
               return;

            shared_ptr<const DistributionListRecipients> listRecipients = pListADO->GetMembers();
            const vector<shared_ptr<DistributionListRecipient> > vecRecipients = listRecipients->GetConstVector();
            vector<shared_ptr<DistributionListRecipient> >::const_iterator iterRecipient = vecRecipients.begin();

            while (iterRecipient != vecRecipients.end())
            {
               _CreateMessageRecipientList((*iterRecipient)->GetAddress(), sOriginalAddress,lRecurse, pRecipients, recipientOK);

               iterRecipient++;
            }

            return;
         }

      }
      
      // Check for routes. This happens under two circumstances:
      // 1) The domain is local but the recipient user was not found in the domain.
      // 2) The domain is not local. 
      // When we get here, one of these are true.
      String recipientDomain = StringParser::ExtractDomain(recipientAddress);
      vector<shared_ptr<Route> > vecRoutes = HM::Configuration::Instance()->GetSMTPConfiguration()->GetRoutes()->GetItemsByName(recipientDomain);
      if (vecRoutes.size() > 0)
      {
         vector<shared_ptr<Route> >::iterator iter = vecRoutes.begin();
         vector<shared_ptr<Route> >::iterator iterEnd = vecRoutes.end();

         bool recipientExists = false;
         bool isLocalName = false;
         for (; iter != iterEnd; iter++)
         {
            // Check whether we should route to all, or if we should allow route to this specific address
            shared_ptr<Route> pRoute = (*iter);
            if (pRoute->ToAllAddresses() || pRoute->GetAddresses()->GetItemByName(recipientAddress))
            {
               recipientExists = true;
               isLocalName = pRoute->GetTreatRecipientAsLocalDomain();
               break;
            }
         }

         if (recipientExists)
         {
            shared_ptr<MessageRecipient> NewRecipient = shared_ptr<MessageRecipient>(new MessageRecipient);
            NewRecipient->SetAddress(recipientAddress);
            NewRecipient->SetOriginalAddress(sOriginalAddress);
            NewRecipient->SetIsLocalName(isLocalName);

            recipientOK = true;

            _AddRecipient(pRecipients, NewRecipient);
         }

         return;

      }

      if (bIsLocalDomain)
      {
         String sPostMaster = pDomain->GetPostmaster();
         if (!sPostMaster.IsEmpty())
         {
            // The domain is local but we could not find the recipient address
            // in our domain. We've looked in routes as well but not found a
            // match there either.
            _CreateMessageRecipientList(sPostMaster, sOriginalAddress, lRecurse, pRecipients, recipientOK);

            return;
         }
      }
      else
      {
         // The recipient is external. We have already checked if it's OK that the user
         // delivers to this, so go ahead.

         shared_ptr<MessageRecipient> NewRecipient = shared_ptr<MessageRecipient>(new MessageRecipient);

         NewRecipient->SetAddress(recipientAddress);
         NewRecipient->SetOriginalAddress(sOriginalAddress);
         NewRecipient->SetLocalAccountID(0);
         NewRecipient->SetIsLocalName(false);

         recipientOK = true;

         _AddRecipient(pRecipients, NewRecipient);
      }
   }

   RecipientParser::DeliveryPossibility 
   RecipientParser::_UserCanSendToList(const String &sSender, bool bSenderIsAuthenticated, shared_ptr<const DistributionList> pList, String &sErrMsg, int iRecursionLevel)
   {
      shared_ptr<DomainAliases> pDA = ObjectCache::Instance()->GetDomainAliases();

      if (pList->GetRequireAuth() && !bSenderIsAuthenticated)
      {
         sErrMsg = "550 SMTP authentication required.";
         return DP_PermissionDenied;
      }

      DistributionList::ListMode lm = pList->GetListMode();

      if (lm == DistributionList::LMAnnouncement)
      {
         // Only one person can send to list. Check if it's the correct. Before we do the
         // comparision, we resolve any domain name aliases.

         String sFormattedSender = pDA->ApplyAliasesOnAddress(sSender);
         String sFormattedRequiredSender = pDA->ApplyAliasesOnAddress(pList->GetRequireAddress());
         if (sFormattedSender.CompareNoCase(sFormattedRequiredSender) != 0)
         {
            sErrMsg = "550 Not authorized.";
            return DP_PermissionDenied;
         }

         // OK. The correct user is sending
      }
      else if (lm == DistributionList::LMPublic)
      {
         // Anyone can send. OK
      }
      else if (lm == DistributionList::LMMembership)
      {
         // Only members of the list can send messages. 
         // Check if the sender is a member of the list.
         std::vector<shared_ptr<DistributionListRecipient> > vecRecipients = pList->GetMembers()->GetVector();
         std::vector<shared_ptr<DistributionListRecipient> >::iterator iterRecipient = vecRecipients.begin();

         for (; iterRecipient != vecRecipients.end(); iterRecipient++)
         {
            String sRecipient = (*iterRecipient)->GetAddress();
            sRecipient = pDA->ApplyAliasesOnAddress(sRecipient);

            if (sRecipient.CompareNoCase(sSender) == 0)
            {
               break;
            }

         }

         // If we reached the end of the list, it means that we
         // didn't find the recipient.
         if (iterRecipient == vecRecipients.end())
         {
            sErrMsg = "550 Not authorized.";
            return DP_PermissionDenied;
         }
      }


      // Check that the user is allowed to send to all recipient
      // of the list. This is a bit CPU intensive, but we need
      // to recursively look up all the recipients.
      shared_ptr<DistributionListRecipients> pListMembers = pList->GetMembers();

      vector<shared_ptr<DistributionListRecipient> > vecRecipients = pListMembers->GetVector();
      vector<shared_ptr<DistributionListRecipient> >::const_iterator iterRecipient = vecRecipients.begin();

      while (iterRecipient != vecRecipients.end())
      {
         bool bTreatSecurityAsLocal = true;

         DeliveryPossibility dp = CheckDeliveryPossibility(bSenderIsAuthenticated, sSender, (*iterRecipient)->GetAddress(), sErrMsg, bTreatSecurityAsLocal, iRecursionLevel);
         if (dp == DP_PermissionDenied)
            return DP_PermissionDenied;

         iterRecipient++;
      }

      return DP_Possible;
   }

   void
   RecipientParser::_AddRecipient(shared_ptr<MessageRecipients> pRecipients, shared_ptr<MessageRecipient> pRecipient)
   {
      String address = pRecipient->GetAddress().ToLower();

      if (address.IsEmpty())
         return;
      
      vector<shared_ptr<MessageRecipient> > vecResult = pRecipients->GetVector();
      vector<shared_ptr<MessageRecipient> >::iterator iterRecip = vecResult.begin();

      while (iterRecip != vecResult.end())
      {
         if ((*iterRecip)->GetAddress().ToLower() == address)
            return;

         iterRecip++;
      }

      pRecipients->Add(pRecipient);
   }
}