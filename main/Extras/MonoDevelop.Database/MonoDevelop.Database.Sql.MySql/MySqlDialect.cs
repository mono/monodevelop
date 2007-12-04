//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Data;
using System.Collections.Generic;
namespace MonoDevelop.Database.Sql
{
	public class MySqlDialect : AbstractSqlDialect
	{
		public override string QuoteIdentifier (string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException ("identifier");
			
			if (IsReservedWord (identifier) || identifier.Contains (" "))
				return String.Concat ('`', identifier, '`');
			return identifier;
		}
		
		public override string MarkAsParameter (string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException ("identifier");
			
			return "?" + identifier;
		}
		
		//http://dev.mysql.com/doc/refman/5.0/en/reserved-words.html
		protected virtual bool IsReservedWord (string word)
		{
			word = word.ToUpper ();
			
			switch (word) {
				case "ADD": case "ALL": case "ALTER": case "ANALYZE":
				case "AND": case "AS": case "ASC":
				case "ASENSITIVE": case "BEFORE": case "BETWEEN":
				case "BIGINT": case "BINARY": case "BLOB":
				case "BOTH": case "BY": case "CALL":
				case "CASCADE": case "CASE": case "CHANGE":
				case "CHAR": case "CHARACTER": case "CHECK":
				case "COLLATE": case "COLUMN": case "CONDITION":
				case "CONSTRAINT": case "CONTINUE": case "CONVERT":
				case "CREATE": case "CROSS": case "CURRENT_DATE":
				case "CURRENT_TIME": case "CURRENT_TIMESTAMP": case "CURRENT_USER":
				case "CURSOR": case "DATABASE": case "DATABASES":
				case "DAY_HOUR": case "DAY_MICROSECOND": case "DAY_MINUTE":
				case "DAY_SECOND": case "DEC": case "DECIMAL":
				case "DECLARE": case "DEFAULT": case "DELAYED":
				case "DELETE": case "DESC": case "DESCRIBE":
				case "DETERMINISTIC": case "DISTINCT": case "DISTINCTROW":
				case "DIV": case "DOUBLE": case "DROP":
				case "DUAL": case "EACH": case "ELSE":
				case "ELSEIF": case "ENCLOSED": case "ESCAPED":
				case "EXISTS": case "EXIT": case "EXPLAIN":
				case "FALSE": case "FETCH": case "FLOAT":
				case "FLOAT4": case "FLOAT8": case "FOR":
				case "FORCE": case "FOREIGN": case "FROM":
				case "FULLTEXT": case "GRANT": case "GROUP":
				case "HAVING": case "HIGH_PRIORITY": case "HOUR_MICROSECOND":
				case "HOUR_MINUTE": case "HOUR_SECOND": case "IF":
				case "IGNORE": case "IN": case "INDEX":
				case "INFILE": case "INNER": case "INOUT":
				case "INSENSITIVE": case "INSERT": case "INT":
				case "INT1": case "INT2": case "INT3":
				case "INT4": case "INT8": case "INTEGER":
				case "INTERVAL": case "INTO": case "IS":
				case "ITERATE": case "JOIN": case "KEY":
				case "KEYS": case "KILL": case "LEADING":
				case "LEAVE": case "LEFT": case "LIKE":
				case "LIMIT": case "LINES": case "LOAD":
				case "LOCALTIME": case "LOCALTIMESTAMP": case "LOCK":
				case "LONG": case "LONGBLOB": case "LONGTEXT":
				case "LOOP": case "LOW_PRIORITY": case "MATCH":
				case "MEDIUMBLOB": case "MEDIUMINT": case "MEDIUMTEXT":
				case "MIDDLEINT": case "MINUTE_MICROSECOND": case "MINUTE_SECOND":
				case "MOD": case "MODIFIES": case "NATURAL":
				case "NOT": case "NO_WRITE_TO_BINLOG": case "NULL":
				case "NUMERIC": case "ON": case "OPTIMIZE":
				case "OPTION": case "OPTIONALLY": case "OR":
				case "ORDER": case "OUT": case "OUTER":
				case "OUTFILE": case "PRECISION": case "PRIMARY":
				case "PROCEDURE": case "PURGE": case "RAID0":
				case "READ": case "READS": case "REAL":
				case "REFERENCES": case "REGEXP": case "RELEASE":
				case "RENAME": case "REPEAT": case "REPLACE":
				case "REQUIRE": case "RESTRICT": case "RETURN":
				case "REVOKE": case "RIGHT": case "RLIKE":
				case "SCHEMA": case "SCHEMAS": case "SECOND_MICROSECOND":
				case "SELECT": case "SENSITIVE": case "SEPARATOR":
				case "SET": case "SHOW": case "SMALLINT":
				case "SONAME": case "SPATIAL": case "SPECIFIC":
				case "SQL": case "SQLEXCEPTION": case "SQLSTATE":
				case "SQLWARNING": case "SQL_BIG_RESULT": case "SQL_CALC_FOUND_ROWS":
				case "SQL_SMALL_RESULT": case "SSL": case "STARTING":
				case "STRAIGHT_JOIN": case "TABLE": case "TERMINATED":
				case "THEN": case "TINYBLOB": case "TINYINT":
				case "TINYTEXT": case "TO": case "TRAILING":
				case "TRIGGER": case "TRUE": case "UNDO":
				case "UNION": case "UNIQUE": case "UNLOCK":
				case "UNSIGNED": case "UPDATE": case "USAGE":
				case "USE": case "USING": case "UTC_DATE":
				case "UTC_TIME": case "UTC_TIMESTAMP": case "VALUES":
				case "VARBINARY": case "VARCHAR": case "VARCHARACTER":
				case "VARYING": case "WHEN": case "WHERE":
				case "WHILE": case "WITH": case "WRITE":
				case "X509": case "XOR": case "YEAR_MONTH":
				case "ZEROFILL":
					return true;
				default:
					return false;
			}
		}
	}
}