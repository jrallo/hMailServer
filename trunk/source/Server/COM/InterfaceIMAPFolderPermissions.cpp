// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "InterfaceIMAPFolderPermissions.h"
#include "InterfaceIMAPFolderPermission.h"

#include "../Common/BO/IMAPFolder.h"
#include "../Common/BO/ACLPermissions.h"
#include "../Common/BO/ACLPermission.h"
#include "../Common/Application/ACLManager.h"

void 
InterfaceIMAPFolderPermissions::AttachItem(shared_ptr<HM::IMAPFolder> pFolder)
{
   m_pFolder = pFolder;
   
   // Fetch the permissions in this ACL. It's really these we are
   // interested in in this interface.
   m_pACLPermissions = m_pFolder->GetPermissions();
}

STDMETHODIMP InterfaceIMAPFolderPermissions::get_Count(long *pVal)
{
   *pVal = m_pACLPermissions->GetCount();
   return S_OK;
}


STDMETHODIMP InterfaceIMAPFolderPermissions::Delete(long Index)
{
   m_pACLPermissions->DeleteItem(Index);
   return S_OK;
}

STDMETHODIMP InterfaceIMAPFolderPermissions::Refresh()
{
   m_pACLPermissions->Refresh();
   return S_OK;
}

STDMETHODIMP InterfaceIMAPFolderPermissions::get_Item(long Index, IInterfaceIMAPFolderPermission **pVal)
{
   CComObject<InterfaceIMAPFolderPermission>* pACLPermission = new CComObject<InterfaceIMAPFolderPermission>();
   pACLPermission->SetAuthentication(m_pAuthentication);

   shared_ptr<HM::ACLPermission> pPersACLPermission = m_pACLPermissions->GetItem(Index);

   if (!pPersACLPermission)
      return DISP_E_BADINDEX;  

   pACLPermission->AttachItem(pPersACLPermission);
   pACLPermission->AttachParent(m_pACLPermissions, true);
   pACLPermission->AddRef();
   *pVal = pACLPermission;
   return S_OK;
}

STDMETHODIMP InterfaceIMAPFolderPermissions::Add(IInterfaceIMAPFolderPermission **pVal)
{
   if (!m_pAuthentication->GetIsDomainAdmin())
      return m_pAuthentication->GetAccessDenied();

   if (!m_pACLPermissions)
      return m_pAuthentication->GetAccessDenied();

   CComObject<InterfaceIMAPFolderPermission>* pIntACLPermission = new CComObject<InterfaceIMAPFolderPermission>();
   pIntACLPermission->SetAuthentication(m_pAuthentication);

   shared_ptr<HM::ACLPermission> pACLPermission = shared_ptr<HM::ACLPermission>(new HM::ACLPermission);

   pACLPermission->SetShareFolderID(m_pFolder->GetID());

   pIntACLPermission->AttachItem(pACLPermission);
   pIntACLPermission->AttachParent(m_pACLPermissions, false);

   pIntACLPermission->AddRef();
   *pVal = pIntACLPermission;

   return S_OK;
}

STDMETHODIMP InterfaceIMAPFolderPermissions::get_ItemByDBID(long DBID, IInterfaceIMAPFolderPermission **pVal)
{
   CComObject<InterfaceIMAPFolderPermission>* pACLPermission = new CComObject<InterfaceIMAPFolderPermission>();
   pACLPermission->SetAuthentication(m_pAuthentication);

   shared_ptr<HM::ACLPermission> pPersACLPermission = m_pACLPermissions->GetItemByDBID(DBID);

   if (pPersACLPermission)
   {
      pACLPermission->AttachItem(pPersACLPermission);
      pACLPermission->AttachParent(m_pACLPermissions, true);
      pACLPermission->AddRef();
      *pVal = pACLPermission;
   }
   else
   {
      return DISP_E_BADINDEX;  
   }


   return S_OK;
}


STDMETHODIMP InterfaceIMAPFolderPermissions::DeleteByDBID(long DBID)
{

   if (m_pACLPermissions)
      m_pACLPermissions->DeleteItemByDBID(DBID);

   return S_OK;
}

STDMETHODIMP InterfaceIMAPFolderPermissions::get_ItemByName(BSTR Name, IInterfaceIMAPFolderPermission **pVal)
{
   CComObject<InterfaceIMAPFolderPermission>* pACLPermission = new CComObject<InterfaceIMAPFolderPermission>();
   pACLPermission->SetAuthentication(m_pAuthentication);

   shared_ptr<HM::ACLPermission> pPersACLPermission = m_pACLPermissions->GetItemByName(Name);

   if (pPersACLPermission)
   {
      pACLPermission->AttachItem(pPersACLPermission);
      pACLPermission->AttachParent(m_pACLPermissions, true);
      pACLPermission->AddRef();
      *pVal = pACLPermission;
   }
   else
   {
      return DISP_E_BADINDEX;  
   }


   return S_OK;
}

