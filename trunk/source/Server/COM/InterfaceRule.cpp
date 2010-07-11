// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "InterfaceRule.h"

#include "..\Common\Bo\Rule.h"
#include "..\Common\Bo\Rules.h"
#include "..\Common\Persistence\PersistentRule.h"

#include "InterfaceRuleCriterias.h"
#include "InterfaceRuleActions.h"

#include "COMError.h"

STDMETHODIMP InterfaceRule::get_ID(long *pVal)
{
   *pVal = (long) m_pObject->GetID();
   return S_OK;
}

STDMETHODIMP InterfaceRule::get_AccountID(LONG* pVal)
{
   *pVal = (long) m_pObject->GetAccountID();

   return S_OK;
}

STDMETHODIMP InterfaceRule::put_AccountID(LONG newVal)
{
   m_pObject->SetAccountID(newVal);

   return S_OK;
}

STDMETHODIMP InterfaceRule::get_Name(BSTR* pVal)
{
   *pVal = m_pObject->GetName().AllocSysString();

   return S_OK;
}

STDMETHODIMP InterfaceRule::put_Name(BSTR newVal)
{
   m_pObject->SetName(newVal);

   return S_OK;
}

STDMETHODIMP InterfaceRule::get_Active(VARIANT_BOOL* pVal)
{
   *pVal = m_pObject->GetActive() ? VARIANT_TRUE : VARIANT_FALSE;

   return S_OK;
}

STDMETHODIMP InterfaceRule::put_Active(VARIANT_BOOL newVal)
{
   m_pObject->SetActive(newVal == VARIANT_TRUE);

   return S_OK;
}

STDMETHODIMP InterfaceRule::get_UseAND(VARIANT_BOOL* pVal)
{
   *pVal = m_pObject->GetUseAND() ? VARIANT_TRUE : VARIANT_FALSE;

   return S_OK;
}

STDMETHODIMP InterfaceRule::put_UseAND(VARIANT_BOOL newVal)
{
   m_pObject->SetUseAND(newVal == VARIANT_TRUE);

   return S_OK;
}

STDMETHODIMP InterfaceRule::get_Criterias(IInterfaceRuleCriterias **pVal)
{
   CComObject<InterfaceRuleCriterias>* pItem = new CComObject<InterfaceRuleCriterias >();
   pItem->SetAuthentication(m_pAuthentication);

   shared_ptr<HM::RuleCriterias> pRuleCriterias = m_pObject->GetCriterias();

   if (pRuleCriterias)
   {
      pItem->Attach(pRuleCriterias);
      pItem->AddRef();
      *pVal = pItem;
   }

   return S_OK;
}

STDMETHODIMP InterfaceRule::get_Actions(IInterfaceRuleActions **pVal)
{
   CComObject<InterfaceRuleActions>* pItem = new CComObject<InterfaceRuleActions >();

   shared_ptr<HM::RuleActions> pRuleActions = m_pObject->GetActions();

   if (pRuleActions)
   {
      pItem->Attach(pRuleActions);
      pItem->AddRef();
      *pVal = pItem;
   }

   return S_OK;
}

STDMETHODIMP InterfaceRule::MoveUp()
{
   if (m_pObject->GetID() <= 0)
      return COMError::GenerateError("Object not yet saved.");

   m_pParentCollection->MoveUp(m_pObject->GetID());
   return S_OK;

}

STDMETHODIMP InterfaceRule::MoveDown()
{
   if (m_pObject->GetID() <= 0)
      return COMError::GenerateError("Object not yet saved.");
      
   m_pParentCollection->MoveDown(m_pObject->GetID());
   return S_OK;
}


STDMETHODIMP InterfaceRule::Save()
{
   // Set the sort order of the rule.
   if (m_pObject->GetID() == 0 && m_pObject->GetSortOrder() == 0)
   {
      std::vector<shared_ptr<HM::Rule> > vecExistingRules = m_pParentCollection->GetVector();

      // Determine the highest SortOrder.
      if (vecExistingRules.size() == 0)
         m_pObject->SetSortOrder(1);
      else
      {
         shared_ptr<HM::Rule> pLastRule = vecExistingRules[vecExistingRules.size() -1];
         m_pObject->SetSortOrder(pLastRule->GetSortOrder() +1);
      }
   }

   if (HM::PersistentRule::SaveObject(m_pObject))
   {
      // Add to parent collection
      AddToParentCollection();

      return S_OK;
   }

   return COMError::GenerateError("Failed to save object. See hMailServer error log.");
}

STDMETHODIMP InterfaceRule::Delete()
{
   if (!m_pParentCollection)
      return HM::PersistentRule::DeleteObject(m_pObject) ? S_OK : S_FALSE;

   m_pParentCollection->DeleteItemByDBID(m_pObject->GetID());

   return S_OK;
}