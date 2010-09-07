// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "IMAPCommandSelect.h"
#include "IMAPConnection.h"
#include "IMAPSimpleCommandParser.h"
#include "IMAPConfiguration.h"

#include "../Common/BO/ACLPermission.h"
#include "../Common/BO/IMAPFolders.h"
#include "../Common/BO/IMAPFolder.h"
#include "../Common/BO/Message.h"

#ifdef _DEBUG
#define DEBUG_NEW new(_NORMAL_BLOCK, __FILE__, __LINE__)
#define new DEBUG_NEW
#endif

namespace HM
{
   IMAPResult
   IMAPCommandSELECT::ExecuteCommand(shared_ptr<HM::IMAPConnection> pConnection, shared_ptr<IMAPCommandArgument> pArgument)
   {

      if (!pConnection->IsAuthenticated())
         return IMAPResult(IMAPResult::ResultNo, "Authenticate first");

      shared_ptr<IMAPSimpleCommandParser> pParser = shared_ptr<IMAPSimpleCommandParser>(new IMAPSimpleCommandParser());

      pParser->Parse(pArgument);

      if (pParser->ParamCount() < 1)
         return IMAPResult(IMAPResult::ResultBad, "SELECT Command requires at least 1 parameter.");

      String sFolderName = pParser->GetParamValue(pArgument, 0);
      if (sFolderName == Configuration::Instance()->GetIMAPConfiguration()->GetIMAPPublicFolderName())
         return IMAPResult(IMAPResult::ResultBad, "SELECT Only sub folders of the root shared folder can be selected.");
         
      shared_ptr<IMAPFolder> pSelectedFolder = pConnection->GetFolderByFullPath(sFolderName);
      if (!pSelectedFolder)
         return IMAPResult(IMAPResult::ResultBad, "Folder could not be found.");

      bool readAccess = false;
      bool writeAccess = false;
      pConnection->CheckFolderPermissions(pSelectedFolder, readAccess, writeAccess);

      // Check if the user has access to read this folder.
      if (!readAccess)
         return IMAPResult(IMAPResult::ResultBad, "ACL: Read permission denied (Required for SELECT command).");

      pConnection->SetCurrentFolder(pSelectedFolder, false);
      shared_ptr<Messages> pMessages = pSelectedFolder->GetMessages();

      long lCount = pMessages->GetCount();
      __int64 lFirstUnseenID = pMessages->GetFirstUnseenUID();
      long lRecentCount = pMessages->GetNoOfRecent();

      String sRespTemp;

      sRespTemp.Format(_T("* %d EXISTS\r\n"), lCount);
      String sResponse = sRespTemp; // EXISTS

      sRespTemp.Format(_T("* %d RECENT\r\n"), lRecentCount);
      sResponse += sRespTemp;

	   sResponse += _T("* FLAGS (\\Deleted \\Seen \\Draft \\Answered \\Flagged)\r\n");

      sRespTemp.Format(_T("* OK [UIDVALIDITY %d]\r\n"), pSelectedFolder->GetCreationTime().ToInt());   
      sResponse += sRespTemp;
      
      if (lFirstUnseenID > 0)
      {
         sRespTemp.Format(_T("* OK [UNSEEN %d]\r\n"), lFirstUnseenID);
         sResponse += sRespTemp;
      }
      
      sRespTemp.Format(_T("* OK [UIDNEXT %d]\r\n"), pSelectedFolder->GetCurrentUID()+1);
      sResponse += sRespTemp;

      sResponse += _T("* OK [PERMANENTFLAGS (\\Deleted \\Seen \\Draft \\Answered \\Flagged)]\r\n");

      if (writeAccess)
         sResponse += pArgument->Tag() + _T(" OK [READ-WRITE] SELECT completed\r\n");
      else
         sResponse += pArgument->Tag() + _T(" OK [READ-ONLY] SELECT completed\r\n");


      pConnection->SendAsciiData(sResponse);   
 
      return IMAPResult();
   }
}