#pragma once

#include "../Common/BO/IncomingRelays.h"

namespace HM
{
   class PropertySet;
   class Routes;
   class DNSBlackLists;
   class BlockedAttachments;
   
   class SMTPConfiguration
   {
   public:
	   SMTPConfiguration();
	   virtual ~SMTPConfiguration();

      enum eAVAction
      {
         ActionDelete = 0,
         ActionStripAttachments = 1
      };

      bool Load();

      void SetMaxSMTPConnections(int newVal);
      void SetAuthAllowPlainText(bool newVal);
      void SetAllowMailFromNull(bool newVal);
      void SetLogSMTPConversations(bool bNewVal);
      void SetUseORDB(bool NewVal);
      void SetUseSpamhaus(bool NewVal);
      void SetNoOfRetries(long lNewVal);
      void SetMinutesBetweenTry(long lHoursBetween);
      void SetSMTPRelayer(const String &sRelayer);
      
      void SetSMTPRelayerPort(long lPort);
      long GetSMTPRelayerPort();

      void SetSMTPRelayerUseSSL(bool bNewValue);
      bool GetSMTPRelayerUseSSL();

      void SetMaxNoOfDeliveryThreads(int lNewValue);

      String GetWelcomeMessage() const;
      void SetWelcomeMessage(const String &sMessage);

      String GetSMTPDeliveryBindToIP() const;
      void SetSMTPDeliveryBindToIP(const String &sIP);

      bool GetBlockBareLFs() const;

      int GetMaxSMTPConnections();
      bool GetAuthAllowPlainText();
      bool GetAllowMailFromNull();

      long GetMinutesBetweenTry();
      long GetNoOfRetries();
      String GetSMTPRelayer() const;

      // ANTIVIRUS SETTINGS
      bool ClamWinEnabled() const;
      void ClamWinEnabled(bool Value);
      String ClamWinExecutable() const;
      void ClamWinExecutable(const String & Value);
      String ClamWinDatabase() const;
      void ClamWinDatabase(const String & Value);
      
      bool GetCustomScannerEnabled() const;
      void SetCustomScannerEnabled(bool Value);
      String GetCustomScannerExecutable() const;
      void SetCustomScannerExecutable(const String & Value);
      long GetCustomScannerReturnValue() const;
      void SetCustomScannerReturnValue(long Value);

      int GetVirusScanMaxSize() const;
      void SetVirusScanMaxSize(int iNewVal);

      bool GetEnableAttachmentBlocking() const;
      void SetEnableAttachmentBlocking(bool iNewVal);

      eAVAction AVAction() const;
      void AVAction(eAVAction Value);
      bool AVNotifySender();
      void AVNotifySender(bool Value);
      bool AVNotifyReceiver();
      void AVNotifyReceiver(bool Value);

      int GetMaxNoOfDeliveryThreads();
      

      bool GetSMTPRelayerRequiresAuthentication();
      void SetSMTPRelayerRequiresAuthentication(bool bNewVal);
      String GetSMTPRelayerUsername() const;
      void SetSMTPRelayerUsername(const String & Value);
      
      String GetSMTPRelayerPassword() const;
      void SetSMTPRelayerPassword(const String & Value);

      bool GetAllowIncorrectLineEndings();
      void SetAllowIncorrectLineEndings(bool bNewVal);

      int GetMaxMessageSize();
      void SetMaxMessageSize(int iNewVal);

      int GetMaxSMTPRecipientsInBatch();
      void SetMaxSMTPRecipientsInBatch(int iNewVal);

      int GetRuleLoopLimit();
      void SetRuleLoopLimit(int iNewVal);

      int GetMaxNumberOfMXHosts();
      void SetMaxNumberOfMXHosts(int iNewVal);


      bool XMLStore(XNode *pBackupNode, int Options);
      bool XMLLoad(XNode *pBackupNode, int iRestoreOptions);

      bool GetAddDeliveredToHeader();
      void SetAddDeliveredToHeader(bool bNewVal);

      void OnPropertyChanged(shared_ptr<Property> pProperty);

      shared_ptr<IncomingRelays> GetIncomingRelays() {return _incomingRelays;}
      shared_ptr<Routes> GetRoutes() {return _routes;}

   private:

      shared_ptr<PropertySet> _GetSettings() const;
      shared_ptr<IncomingRelays> _incomingRelays;
      shared_ptr<Routes> _routes;
   };
}