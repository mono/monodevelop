CREATE TABLE IF NOT EXISTS Items (
	FullName varchar PRIMARY KEY,
	Depth integer,
	FileName varchar,
	LineNumber integer,
	ItemType integer,
	Pydoc varchar,
	Extra varchar
);

CREATE INDEX IF NOT EXISTS Items_Depth_Index ON Items (Depth);
