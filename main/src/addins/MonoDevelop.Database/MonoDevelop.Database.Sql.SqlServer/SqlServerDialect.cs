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
	public class SqlServerDialect : AbstractSqlDialect
	{
		//http://msdn2.microsoft.com/En-US/library/aa224033(SQL.80).aspx
		public override string QuoteIdentifier (string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException ("identifier");
			
			if (IsReservedWord (identifier))
				return String.Concat ('[', identifier, ']');

			foreach (char c in identifier.ToCharArray ()) {
				switch (c) {
				case ' ': case '~': case '-':
				case '!': case '{': case '%':
				case '}': case '^': case '\'':
				case '&': case '.': case '(':
				case '\\': case ')': case '`':
					return String.Concat ('[', identifier, ']');
				default:
					break;
				}
			}
			return identifier;
		}
		
		public override string MarkAsParameter (string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException ("identifier");
			
			return "@" + identifier;
		}

		//http://msdn2.microsoft.com/en-US/library/aa238507(SQL.80).aspx
		protected virtual bool IsReservedWord (string word)
		{
			word = word.ToUpper ();
			
			switch (word) {
				case "EXCEPT":	case "PERCENT":
				case "ALL":	case "EXEC":	case "PLAN":
				case "ALTER":	case "EXECUTE":	case "PRECISION":
				case "AND":	case "EXISTS":	case "PRIMARY":
				case "ANY":	case "EXIT":	case "PRINT":
				case "AS":	case "FETCH":	case "PROC":
				case "ASC":	case "FILE":	case "PROCEDURE":
				case "AUTHORIZATION":	case "FILLFACTOR":	case "PUBLIC":
				case "BACKUP":	case "FOR":	case "RAISERROR":
				case "BEGIN":	case "FOREIGN":	case "READ":
				case "BETWEEN":	case "FREETEXT":	case "READTEXT":
				case "BREAK":	case "FREETEXTTABLE":	case "RECONFIGURE":
				case "BROWSE":	case "FROM":	case "REFERENCES":
				case "BULK":	case "FULL":	case "REPLICATION":
				case "BY":	case "FUNCTION":	case "RESTORE":
				case "CASCADE":	case "GOTO":	case "RESTRICT":
				case "CASE":	case "GRANT":	case "RETURN":
				case "CHECK":	case "GROUP":	case "REVOKE":
				case "CHECKPOINT":	case "HAVING":	case "RIGHT":
				case "CLOSE":	case "HOLDLOCK":	case "ROLLBACK":
				case "CLUSTERED":	case "IDENTITY":	case "ROWCOUNT":
				case "COALESCE":	case "IDENTITY_INSERT":	case "ROWGUIDCOL":
				case "COLLATE":	case "IDENTITYCOL":	case "RULE":
				case "COLUMN":	case "IF":	case "SAVE":
				case "COMMIT":	case "IN":	case "SCHEMA":
				case "COMPUTE":	case "INDEX":	case "SELECT":
				case "CONSTRAINT":	case "INNER":	case "SESSION_USER":
				case "CONTAINS":	case "INSERT":	case "SET":
				case "CONTAINSTABLE":	case "INTERSECT":	case "SETUSER":
				case "CONTINUE":	case "INTO":	case "SHUTDOWN":
				case "CONVERT":	case "IS":	case "SOME":
				case "CREATE":	case "JOIN":	case "STATISTICS":
				case "CROSS":	case "KEY":	case "SYSTEM_USER":
				case "CURRENT":	case "KILL":	case "TABLE":
				case "CURRENT_DATE":	case "LEFT":	case "TEXTSIZE":
				case "CURRENT_TIME":	case "LIKE":	case "THEN":
				case "CURRENT_TIMESTAMP":	case "LINENO":	case "TO":
				case "CURRENT_USER":	case "LOAD":	case "TOP":
				case "CURSOR":	case "NATIONAL ":	case "TRAN":
				case "DATABASE":	case "NOCHECK":	case "TRANSACTION":
				case "DBCC":	case "NONCLUSTERED":	case "TRIGGER":
				case "DEALLOCATE":	case "NOT":	case "TRUNCATE":
				case "DECLARE":	case "NULL":	case "TSEQUAL":
				case "DEFAULT":	case "NULLIF":	case "UNION":
				case "DELETE":	case "OF":	case "UNIQUE":
				case "DENY":	case "OFF":	case "UPDATE":
				case "DESC":	case "OFFSETS":	case "UPDATETEXT":
				case "DISK":	case "ON":	case "USE":
				case "DISTINCT":	case "OPEN":	case "USER":
				case "DISTRIBUTED":	case "OPENDATASOURCE":	case "VALUES":
				case "DOUBLE":	case "OPENQUERY":	case "VARYING":
				case "DROP":	case "OPENROWSET":	case "VIEW":
				case "DUMMY":	case "OPENXML":	case "WAITFOR":
				case "DUMP":	case "OPTION":	case "WHEN":
				case "ELSE":	case "OR":	case "WHERE":
				case "END":	case "ORDER":	case "WHILE":
				case "ERRLVL":	case "OUTER":	case "WITH":
				case "ESCAPE":	case "OVER":	case "WRITETEXT":
					return true;
				default:
					return false;
			}
		}
	}
}