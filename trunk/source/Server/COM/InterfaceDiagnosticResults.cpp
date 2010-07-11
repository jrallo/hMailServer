// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "InterfaceDiagnosticResults.h"
#include "InterfaceDiagnosticResult.h"



STDMETHODIMP InterfaceDiagnosticResults::get_Count(long* count)
{
   if (!m_pAuthentication->GetIsServerAdmin())
      return m_pAuthentication->GetAccessDenied();

   *count =  (int) _results.size();

   return S_OK;
}

STDMETHODIMP InterfaceDiagnosticResults::get_Item(long Index, IInterfaceDiagnosticResult* *pVal)
{
   if (!m_pAuthentication->GetIsServerAdmin())
      return m_pAuthentication->GetAccessDenied();

   if (Index >= (long) _results.size())
      return DISP_E_BADINDEX;

   CComObject<InterfaceDiagnosticResult>* pResult = new CComObject<InterfaceDiagnosticResult>();
   pResult->SetAuthentication(m_pAuthentication);
   pResult->AttachResult(_results[Index]);
   pResult->AddRef();
   *pVal = pResult;

   return S_OK;
}

