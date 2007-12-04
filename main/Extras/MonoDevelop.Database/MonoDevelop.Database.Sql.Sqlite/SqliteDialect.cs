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
	public class SqliteDialect : AbstractSqlDialect
	{
		public override string QuoteIdentifier (string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException ("identifier");
			
			if (IsReservedWord (identifier) || identifier.Contains (" "))
				return String.Concat ('"', identifier, '"');
			
			return identifier;
		}
		
		public override string MarkAsParameter (string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException ("identifier");
			
			return ":" + identifier;
		}

		//http://www.sqlite.org/lang_keywords.html
		protected virtual bool IsReservedWord (string word)
		{
			word = word.ToUpper ();
			
			switch (word) {
				case "CREATE": case "FOREIGN": case "LIMIT":
				case "ROLLBACK": case "ADD": case "CROSS":
				case "FROM": case "FULL": case "NATURAL":
				case "SELECT": case "ALL": case "NOT":
				case "SET": case "ALTER": case "GROUP":
				case "NOTNULL": case "TABLE": case "HAVING":
				case "NULL": case "AND": case "DEFAULT":
				case "AS": case "DEFERRABLE": case "THEN":
				case "ON": case "TO": case "DELETE":
				case "IN": case "OR": case "TRANSACTION":
				case "INDEX": case "ORDER": case "AUTOINCREMENT":
				case "OUTER": case "UNION": case "DISTINCT":
				case "INNER": case "UNIQUE": case "BETWEEN":
				case "DROP": case "INSERT": case "UPDATE":
				case "BY": case "PRIMARY": case "USING":
				case "ELSE": case "INTERSECT": case "CASE":
				case "INTO": case "ESCAPE": case "IS":
				case "REFERENCES": case "CHECK": case "EXCEPT":
				case "ISNULL": case "COLLATE": case "JOIN":
				case "WHEN": case "WHERE": case "COMMIT":
				case "LEFT": case "RIGHT": case "CONSTRAINT":
					return true;
				default:
					return false;
			}
		}
	}
}