// Copyright (c) 2009 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"

#include "PersistentLogonFailure.h"
#include "..\SQL\IPAddressSQLHelper.h"

namespace HM   
{
   PersistentLogonFailure::PersistentLogonFailure()
   {

   }

   PersistentLogonFailure::~PersistentLogonFailure()
   {

   }

   int 
   PersistentLogonFailure::GetCurrrentFailureCount(const IPAddress &ipaddress)
   {
      IPAddressSQLHelper helper;

      String sql;
      sql.Format(_T("select count(*) as c from hm_logon_failures where ipaddress1 %s and ipaddress2 %s"), 
         String(helper.GetAddress1Equals(ipaddress)),
         String(helper.GetAddress2Equals(ipaddress)));

      SQLCommand command(sql);

      shared_ptr<DALRecordset> pRS = Application::Instance()->GetDBManager()->OpenRecordset(command);
      if (!pRS)
         return 0;

      long count = pRS->GetLongValue("c");

      return count;
   }
   
   bool 
   PersistentLogonFailure::AddFailure(const IPAddress &ipaddress)
   {
      SQLStatement statement;

      IPAddressSQLHelper helper;
      helper.AppendStatement(statement, ipaddress, "ipaddress1", "ipaddress2");

      statement.AddColumnCommand("failuretime", SQLStatement::GetCurrentTimestamp());
      statement.SetStatementType(SQLStatement::STInsert);
      statement.SetTable("hm_logon_failures");

      return Application::Instance()->GetDBManager()->Execute(statement);
   }

   bool 
   PersistentLogonFailure::ClearFailuresByIP(const IPAddress &ipaddress)
   {
      IPAddressSQLHelper helper;

      String whereClause;
      whereClause.Format(_T("ipaddress1 %s and ipaddress2 %s"), 
         String(helper.GetAddress1Equals(ipaddress)),
         String(helper.GetAddress2Equals(ipaddress)));

      SQLStatement statement;
      statement.SetStatementType(SQLStatement::STDelete);
      statement.SetWhereClause(whereClause);
      statement.SetTable("hm_logon_failures");

      return Application::Instance()->GetDBManager()->Execute(statement);
   }

   bool 
   PersistentLogonFailure::ClearOldFailures(int olderThanMinutes)
   {
      String whereClause;
      whereClause.Format(_T("failuretime < %s"), SQLStatement::GetCurrentTimestampPlusMinutes(-olderThanMinutes));

      SQLStatement statement;
      statement.SetStatementType(SQLStatement::STDelete);
      statement.SetWhereClause(whereClause);
      statement.SetTable("hm_logon_failures");

      return Application::Instance()->GetDBManager()->Execute(statement);

   }
}