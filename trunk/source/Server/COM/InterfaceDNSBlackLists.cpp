// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "InterfaceDNSBlackLists.h"
#include "InterfaceDNSBlackList.h"

#include "../SMTP/SMTPConfiguration.h"


bool 
InterfaceDNSBlackLists::LoadSettings()
{
   if (!GetIsServerAdmin())
      return false;

   m_pBlackLists = HM::Configuration::Instance()->GetAntiSpamConfiguration().GetDNSBlackLists();

   return true;
}


STDMETHODIMP 
InterfaceDNSBlackLists::Refresh()
{
   if (!m_pBlackLists)
      return S_FALSE;

   m_pBlackLists->Refresh();
   
   return S_OK;
}


STDMETHODIMP InterfaceDNSBlackLists::get_Count(long *pVal)
{
   *pVal = m_pBlackLists->GetCount();

   return S_OK;
}

STDMETHODIMP 
InterfaceDNSBlackLists::get_Item(long Index, IInterfaceDNSBlackList **pVal)
{
   CComObject<InterfaceDNSBlackList>* pInterfaceDNSBlackList = new CComObject<InterfaceDNSBlackList>();
   pInterfaceDNSBlackList->SetAuthentication(m_pAuthentication);

   shared_ptr<HM::DNSBlackList> pDNSBlackList = m_pBlackLists->GetItem(Index);

   if (!pDNSBlackList)
      return DISP_E_BADINDEX;

   pInterfaceDNSBlackList->AttachItem(pDNSBlackList);
   pInterfaceDNSBlackList->AttachParent(m_pBlackLists, true);
   pInterfaceDNSBlackList->AddRef();
   *pVal = pInterfaceDNSBlackList;

   return S_OK;
}

STDMETHODIMP 
InterfaceDNSBlackLists::DeleteByDBID(long DBID)
{
   m_pBlackLists->DeleteItemByDBID(DBID);
   return S_OK;
}

STDMETHODIMP 
InterfaceDNSBlackLists::get_ItemByDBID(long lDBID, IInterfaceDNSBlackList **pVal)
{
   CComObject<InterfaceDNSBlackList>* pInterfaceDNSBlackList = new CComObject<InterfaceDNSBlackList>();
   pInterfaceDNSBlackList->SetAuthentication(m_pAuthentication);
   shared_ptr<HM::DNSBlackList> pDNSBlackList = m_pBlackLists->GetItemByDBID(lDBID);

   if (!pDNSBlackList)
      return DISP_E_BADINDEX;

   pInterfaceDNSBlackList->AttachItem(pDNSBlackList);
   pInterfaceDNSBlackList->AttachParent(m_pBlackLists, true);
   pInterfaceDNSBlackList->AddRef();

   *pVal = pInterfaceDNSBlackList;

   return S_OK;
}

STDMETHODIMP 
InterfaceDNSBlackLists::Add(IInterfaceDNSBlackList **pVal)
{
   if (!m_pBlackLists)
      return m_pAuthentication->GetAccessDenied();

   CComObject<InterfaceDNSBlackList>* pInterfaceDNSBlackList = new CComObject<InterfaceDNSBlackList>();
   pInterfaceDNSBlackList->SetAuthentication(m_pAuthentication);
   shared_ptr<HM::DNSBlackList> pDNSBL = shared_ptr<HM::DNSBlackList>(new HM::DNSBlackList);

   pInterfaceDNSBlackList->AttachItem(pDNSBL);
   pInterfaceDNSBlackList->AttachParent(m_pBlackLists, false);

   pInterfaceDNSBlackList->AddRef();

   *pVal = pInterfaceDNSBlackList;

   return S_OK;
}

STDMETHODIMP 
InterfaceDNSBlackLists::get_ItemByDNSHost(BSTR ItemName, IInterfaceDNSBlackList **pVal)
{
   CComObject<InterfaceDNSBlackList>* pInterfaceDNSBlackList = new CComObject<InterfaceDNSBlackList>();
   pInterfaceDNSBlackList->SetAuthentication(m_pAuthentication);

   shared_ptr<HM::DNSBlackList> pDNSBL = m_pBlackLists->GetItemByName(ItemName);
   if (!pDNSBL)
      return S_FALSE;

   pInterfaceDNSBlackList->AttachItem(pDNSBL);
   pInterfaceDNSBlackList->AttachParent(m_pBlackLists, true);
   pInterfaceDNSBlackList->AddRef();

   *pVal = pInterfaceDNSBlackList;

   return S_OK;
}



