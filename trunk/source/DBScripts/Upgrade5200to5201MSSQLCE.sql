ALTER TABLE  hm_routes ADD routetreatsenderaslocaldomain tinyint NOT NULL DEFAULT 0

update hm_dbversion set value = 5201
