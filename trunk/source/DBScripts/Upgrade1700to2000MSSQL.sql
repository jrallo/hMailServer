alter table hm_domains add domainpostmaster varchar (80) not null default ('')

update hm_dbversion set value = 2000

