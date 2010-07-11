// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "StdAfx.h"
#include ".\PersistentDomainAlias.h"
#include "..\BO\DomainAlias.h"
#include "..\Application\ObjectCache.h"

#include "PreSaveLimitationsCheck.h"

namespace HM
{
   PersistentDomainAlias::PersistentDomainAlias(void)
   {
   }

   PersistentDomainAlias::~PersistentDomainAlias(void)
   {
   }

   bool
   PersistentDomainAlias::ReadObject(shared_ptr<DomainAlias> oDA, const SQLCommand & command)
   {
      shared_ptr<DALRecordset> pRS = Application::Instance()->GetDBManager()->OpenRecordset(command);
      if (!pRS)
         return false;

      bool bRetVal = false;
      if (!pRS->IsEOF())
      {
         bRetVal = ReadObject(oDA, pRS);
      }

      return bRetVal;
   }

   bool
   PersistentDomainAlias::ReadObject(shared_ptr<DomainAlias> oDA, shared_ptr<DALRecordset> pRS)
   {

      if (pRS->IsEOF())
         return false;

      oDA->SetID(pRS->GetLongValue("daid"));
      oDA->SetDomainID(pRS->GetLongValue("dadomainid"));
      oDA->SetAlias(pRS->GetStringValue("daalias"));

      return true;
   }

   bool
   PersistentDomainAlias::DeleteObject(shared_ptr<DomainAlias> pDA)
   {
      SQLCommand command("delete from hm_domain_aliases where daid = @DAID");
      command.AddParameter("@DAID", pDA->GetID());

      bool bResult = Application::Instance()->GetDBManager()->Execute(command);

      if (!bResult)
         return false;

      ObjectCache::Instance()->SetDomainAliasesNeedsReload();

      return true;
   }


   bool 
   PersistentDomainAlias::SaveObject(shared_ptr<DomainAlias> oDA)
   {
      String sErrorMessage;
      return SaveObject(oDA, sErrorMessage);
   }


   bool 
   PersistentDomainAlias::SaveObject(shared_ptr<DomainAlias> oDA, String &sErrorMessage)
   {
      if (!PreSaveLimitationsCheck::CheckLimitations(oDA, sErrorMessage))
         return false;

      SQLStatement oStatement;
      oStatement.SetTable("hm_domain_aliases");

      bool bNewObject = oDA->GetID() == 0;

      oStatement.AddColumnInt64("dadomainid", oDA->GetDomainID());
      oStatement.AddColumn("daalias", oDA->GetAlias());

      if (bNewObject)
      {
         oStatement.SetStatementType(SQLStatement::STInsert);
         oStatement.SetIdentityColumn("daid");
      }
      else
      {
         String sWhere;
         sWhere.Format(_T("daid = %I64d"), oDA->GetID());

         oStatement.SetStatementType(SQLStatement::STUpdate);
         oStatement.SetWhereClause(sWhere);
      }

      // Save and fetch ID
      __int64 iDBID = 0;
      bool bRetVal = Application::Instance()->GetDBManager()->Execute(oStatement, bNewObject ? &iDBID : 0);
      if (bRetVal && bNewObject)
         oDA->SetID((int) iDBID);

      ObjectCache::Instance()->SetDomainAliasesNeedsReload();

      return true;
   }
   

}
