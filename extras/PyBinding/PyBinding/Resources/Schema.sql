CREATE TABLE IF NOT EXISTS Items (
	FullName varchar PRIMARY KEY,
	FileName varchar,
	LineNumber integer,
	ItemType integer,
	Pydoc varchar,
	Extra varchar
);