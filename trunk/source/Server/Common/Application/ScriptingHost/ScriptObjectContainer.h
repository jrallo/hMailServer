// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#pragma once

namespace HM
{

   // class IScriptObject;

   class ScriptObject
   {  
   public:   
      enum ObjectType
      {
         OTResult = 1000,
         OTMessage = 1001,
         OTClient = 1002,
         OTEventLog = 1003,
         OTFetchAccount = 1004
      };

      String sName;
      shared_ptr<void> pObject;
      ObjectType eType;      
      
   };

   class ScriptObjectContainer
   {
   public:
      ScriptObjectContainer(void);
      ~ScriptObjectContainer(void);

      void AddObject(const String &sName, ScriptObject::ObjectType type);
      void AddObject(const String &sName, shared_ptr<void> pObj, ScriptObject::ObjectType type);

      vector<String> GetObjectNames();
      bool GetObjectByName(const String &sName, LPUNKNOWN* ppunkItem) const;
   
   private:

      map<String, shared_ptr<ScriptObject> > m_mapObjects;
   };
}