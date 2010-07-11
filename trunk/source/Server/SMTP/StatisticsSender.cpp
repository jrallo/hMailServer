// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "StatisticsSender.h"
#include "../Common/Util/HTTPClient.h"

namespace HM
{
   StatisticsSender::StatisticsSender()
   {

   }

   StatisticsSender::~StatisticsSender()
   {

   }

   bool
   StatisticsSender::SendStatistics(int iNoOfMessages) const
   {
      String sPage; 
      sPage.Format(_T("/statistics/update.php?hm_version=%s&hm_messages=%d"), Application::Instance()->GetVersionNumber(), iNoOfMessages);

      AnsiString output;
      HTTPClient Client;
      return Client.ExecuteScript("www.hmailserver.com", sPage, output);

   }


   

}