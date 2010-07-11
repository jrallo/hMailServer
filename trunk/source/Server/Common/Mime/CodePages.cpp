// Copyright (c) 2007 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com
//
// Contains mapping between character sets and code pages
// http://www.iana.org/assignments/character-sets
// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/intl/unicode_81rn.asp

#include "stdafx.h"
#include "CodePages.h"


namespace HM
{
   CodePages::CodePages()
   {
      Initialize();
   }

   CodePages::~CodePages()
   {

   }

   void
   CodePages::Initialize()
   {
      // Complete this list.
      _AddCodePage("US-ASCII", 20127);
      
      _AddCodePage("BIG5", 950);
      _AddCodePage("csBig5", 950);

      _AddCodePage("iso-2022-jp", 50221);
      _AddCodePage("csISO2022JP", 50221);

      _AddCodePage("windows-1250", 1250);
      _AddCodePage("windows-1251", 1251);
      _AddCodePage("windows-1252", 1252);
      _AddCodePage("windows-1253", 1253);
      _AddCodePage("windows-1254", 1254);
      _AddCodePage("windows-1255", 1255);
      _AddCodePage("windows-1256", 1256);
      _AddCodePage("windows-1257", 1257);
      _AddCodePage("windows-1258", 1258);

      _AddCodePage("utf-8", CP_UTF8);
      _AddCodePage("utf-7", CP_UTF7);
   }

   void 
   CodePages::_AddCodePage(const AnsiString &sName, int iCodePage)
   {
      AnsiString sTmp = sName;
      sTmp.ToLower();

      m_mapCodePages[sTmp] = iCodePage;
   }

   int 
   CodePages::GetCodePage(const AnsiString &sName) const
   {
      AnsiString lowerCaseCharSet = sName;
      lowerCaseCharSet.ToLower();

      std::map<AnsiString, int>::const_iterator iter = m_mapCodePages.find(lowerCaseCharSet);

      if (iter == m_mapCodePages.end())
         return 0;

      return (*iter).second;
   }
      
}
