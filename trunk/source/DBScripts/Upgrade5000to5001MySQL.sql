ALTER TABLE hm_accounts CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_aliases CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_domains CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_domain_aliases CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_messages CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_settings CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_dbversion CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_distributionlists CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_distributionlistsrecipients CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_messagerecipients CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_imapfolders CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_securityranges CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_routes CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_routeaddresses CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_dnsbl CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_fetchaccounts CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_fetchaccounts_uids CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_rules CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_rule_criterias CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_rule_actions CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_adsynchronization CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_surblservers CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_greylisting_triplets CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_greylisting_whiteaddresses CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_blocked_attachments CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_servermessages CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_tcpipports CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_whitelist CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_sslcertificates CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_groups CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_group_members CONVERT TO CHARACTER SET utf8

ALTER TABLE hm_acl CONVERT TO CHARACTER SET utf8

update hm_dbversion set value = 5001

