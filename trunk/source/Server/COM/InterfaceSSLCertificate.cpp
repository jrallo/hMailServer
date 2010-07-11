// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "InterfaceSSLCertificate.h"

#include "..\Common\Persistence\PersistentSSLCertificate.h"
#include "..\Common\BO\SSLCertificate.h"

// InterfaceSSLCertificate

STDMETHODIMP 
InterfaceSSLCertificate::Save()
{
   if (!m_pAuthentication->GetIsServerAdmin())
      return m_pAuthentication->GetAccessDenied();

   if (HM::PersistentSSLCertificate::SaveObject(m_pObject))
   {
      // Add to parent collection
      AddToParentCollection();
   }

   return S_OK;
}


STDMETHODIMP InterfaceSSLCertificate::Delete()
{
   if (!m_pAuthentication->GetIsServerAdmin())
      return m_pAuthentication->GetAccessDenied();

   if (!m_pParentCollection)
      return HM::PersistentSSLCertificate::DeleteObject(m_pObject) ? S_OK : S_FALSE;

   m_pParentCollection->DeleteItemByDBID(m_pObject->GetID());

   return S_OK;
}

STDMETHODIMP InterfaceSSLCertificate::get_ID(long *pVal)
{
   *pVal = (long) m_pObject->GetID();
   return S_OK;
}

STDMETHODIMP InterfaceSSLCertificate::put_Name(BSTR newVal)
{
   m_pObject->SetName(newVal);
   return S_OK;
}

STDMETHODIMP InterfaceSSLCertificate::get_Name(BSTR *pVal)
{
   *pVal = m_pObject->GetName().AllocSysString();
   return S_OK;
}

STDMETHODIMP InterfaceSSLCertificate::put_CertificateFile(BSTR newVal)
{
   m_pObject->SetCertificateFile(newVal);
   return S_OK;
}

STDMETHODIMP InterfaceSSLCertificate::get_CertificateFile(BSTR *pVal)
{
   *pVal = m_pObject->GetCertificateFile().AllocSysString();
   return S_OK;
}

STDMETHODIMP InterfaceSSLCertificate::put_PrivateKeyFile(BSTR newVal)
{
   m_pObject->SetPrivateKeyFile(newVal);
   return S_OK;
}

STDMETHODIMP InterfaceSSLCertificate::get_PrivateKeyFile(BSTR *pVal)
{
   *pVal = m_pObject->GetPrivateKeyFile().AllocSysString();
   return S_OK;
}