// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "NotificationClientSubscription.h"

namespace HM
{
   NotificationClientSubscription::NotificationClientSubscription(__int64 subscriptionKey, weak_ptr<NotificationClient> client) :
      _subscriptionKey(subscriptionKey),
      _client(client)
   {

   }

   NotificationClientSubscription::~NotificationClientSubscription()
   {

   }

   __int64 
   NotificationClientSubscription::GetSubscriptionKey()
   {
      return _subscriptionKey;
   }

   weak_ptr<NotificationClient> 
   NotificationClientSubscription::GetSubscribedClient()
   {
      return _client;
   }

}