--
-- pgsql.sql: Database for Npgsql Tests on PostgreSQL.
--
-- Authors:
--   Christian Hergert (christian.hergert@gmail.com)
--

CREATE TABLE users (
	id serial NOT NULL PRIMARY KEY,
	email varchar(100) NOT NULL UNIQUE,
	password varchar(32) NOT NULL DEFAULT md5(''),
	firstname varchar(30) NOT NULL,
	lastname varchar(30),
	created timestamp DEFAULT CURRENT_TIMESTAMP
);