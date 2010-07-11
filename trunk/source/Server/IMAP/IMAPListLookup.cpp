// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"

#include "IMAPListLookup.h"

namespace HM
{
   IMAPListLookup::IMAPListLookup()
   {

   }

   IMAPListLookup::~IMAPListLookup()
   {

   }
   bool 
   IMAPListLookup::IsItemInList(vector<String> vecItems, int item)
   {
      boost_foreach(String sCur, vecItems)
      {
         int lColonPos = sCur.Find(_T(":"));

         if (lColonPos >= 0)
         {
            String sFirstPart = sCur.Mid(0, lColonPos);
            String sSecondPart = sCur.Mid(lColonPos + 1);

            int lower = _ttoi(sFirstPart);
            int upper = -1;
            if (sSecondPart != _T("*"))
               upper = _ttoi(sSecondPart);

            bool match = true;

            if (lower >= 0)
            {
               if (item < lower)
                  match = false;
            }

            if (upper >= 0)
            {
               if (item > upper)
                  match = false;
            }

            if (match)
               return true;
         }
         else
         {
            int foundItem = _ttoi(sCur);
            if (foundItem == item)
               return true;
         }
      }

      return false;

   }


}
