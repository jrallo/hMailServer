// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "POP3Configuration.h"

namespace HM
{
   POP3Configuration::POP3Configuration()
   {

   }

   POP3Configuration::~POP3Configuration()
   {

   }

   shared_ptr<PropertySet> 
   POP3Configuration::_GetSettings() const
   {
      return Configuration::Instance()->GetSettings();
   }

   void
   POP3Configuration::SetMaxPOP3Connections(int newVal)
   {
      _GetSettings()->SetLong(PROPERTY_MAXPOP3CONNECTIONS, newVal);
   }

   long
   POP3Configuration::GetMaxPOP3Connections() const
   {
      return _GetSettings()->GetLong(PROPERTY_MAXPOP3CONNECTIONS);
   }


   String 
   POP3Configuration::GetWelcomeMessage() const
   {
      return _GetSettings()->GetString(PROPERTY_WELCOMEPOP3);
   }

   void 
   POP3Configuration::SetWelcomeMessage(const String &sMessage)
   {
      _GetSettings()->SetString(PROPERTY_WELCOMEPOP3, sMessage);
   }
}