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
	public class NpgsqlDialect : AbstractSqlDialect
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
		
		//http://www.postgresql.org/docs/8.1/interactive/sql-keywords-appendix.html
		protected virtual bool IsReservedWord (string word)
		{
			word = word.ToUpper ();
			
			switch (word) {
				case "A":
				case "ABS":
				case "ADA":
				case "ALIAS":
				case "ALL":
				case "ALLOCATE":
				case "ALWAYS":
				case "ANALYSE":
				case "ANALYZE":
				case "AND":
				case "ANY":
				case "ARE":
				case "ARRAY":
				case "AS":
				case "ASC":
				case "ASENSITIVE":
				case "ASYMMETRIC":
				case "ATOMIC":
				case "ATTRIBUTE":
				case "ATTRIBUTES":
				case "AUTHORIZATION":
				case "AVG":
				case "BERNOULLI":
				case "BETWEEN":
				case "BINARY":
				case "BITVAR":
				case "BIT_LENGTH":
				case "BLOB":
				case "BOTH":
				case "BREADTH":
				case "C":
				case "CALL":
				case "CARDINALITY":
				case "CASCADED":
				case "CASE":
				case "CAST":
				case "CATALOG":
				case "CATALOG_NAME":
				case "CEIL":
				case "CEILING":
				case "CHARACTERS":
				case "CHARACTER_LENGTH":
				case "CHARACTER_SET_CATALOG":
				case "CHARACTER_SET_NAME":
				case "CHARACTER_SET_SCHEMA":
				case "CHAR_LENGTH":
				case "CHECK":
				case "CHECKED":
				case "CLASS_ORIGIN":
				case "CLOB":
				case "COBOL":
				case "COLLATE":
				case "COLLATION":
				case "COLLATION_CATALOG":
				case "COLLATION_NAME":
				case "COLLATION_SCHEMA":
				case "COLLECT":
				case "COLUMN":
				case "COLUMN_NAME":
				case "COMMAND_FUNCTION":
				case "COMMAND_FUNCTION_CODE":
				case "COMPLETION":
				case "CONDITION":
				case "CONDITION_NUMBER":
				case "CONNECT":
				case "CONNECTION_NAME":
				case "CONSTRAINT":
				case "CONSTRAINT_CATALOG":
				case "CONSTRAINT_NAME":
				case "CONSTRAINT_SCHEMA":
				case "CONSTRUCTOR":
				case "CONTAINS":
				case "CONTINUE":
				case "CORR":
				case "CORRESPONDING":
				case "COUNT":
				case "COVAR_POP":
				case "COVAR_SAMP":
				case "CREATE":
				case "CROSS":
				case "CUBE":
				case "CUME_DIST":
				case "CURRENT":
				case "CURRENT_DATE":
				case "CURRENT_DEFAULT_TRANSFORM_GROUP":
				case "CURRENT_PATH":
				case "CURRENT_ROLE":
				case "CURRENT_TIME":
				case "CURRENT_TIMESTAMP":
				case "CURRENT_TRANSFORM_GROUP_FOR_TYPE":
				case "CURRENT_USER":
				case "CURSOR_NAME":
				case "DATA":
				case "DATE":
				case "DATETIME_INTERVAL_CODE":
				case "DATETIME_INTERVAL_PRECISION":
				case "DEFAULT":
				case "DEFERRABLE":
				case "DEFINED":
				case "DEGREE":
				case "DENSE_RANK":
				case "DEPTH":
				case "DEREF":
				case "DERIVED":
				case "DESC":
				case "DESCRIBE":
				case "DESCRIPTOR":
				case "DESTROY":
				case "DESTRUCTOR":
				case "DETERMINISTIC":
				case "DIAGNOSTICS":
				case "DICTIONARY":
				case "DISCONNECT":
				case "DISPATCH":
				case "DISTINCT":
				case "DO":
				case "DYNAMIC":
				case "DYNAMIC_FUNCTION":
				case "DYNAMIC_FUNCTION_CODE":
				case "ELEMENT":
				case "ELSE":
				case "END":
				case "END-EXEC":
				case "EQUALS":
				case "EVERY":
				case "EXCEPT":
				case "EXCEPTION":
				case "EXCLUDE":
				case "EXEC":
				case "EXISTING":
				case "EXP":
				case "0":
				case "FILTER":
				case "FINAL":
				case "FLOOR":
				case "FOLLOWING":
				case "FOR":
				case "FOREIGN":
				case "FORTRAN":
				case "FOUND":
				case "FREE":
				case "FREEZE":
				case "FROM":
				case "FULL":
				case "FUSION":
				case "G":
				case "GENERAL":
				case "GENERATED":
				case "GET":
				case "GO":
				case "GOTO":
				case "GRANT":
				case "GROUP":
				case "GROUPING":
				case "HAVING":
				case "HIERARCHY":
				case "HOST":
				case "IDENTITY":
				case "IGNORE":
				case "ILIKE":
				case "IMPLEMENTATION":
				case "IN":
				case "INDICATOR":
				case "INFIX":
				case "INITIALIZE":
				case "INITIALLY":
				case "INNER":
				case "INSTANCE":
				case "INSTANTIABLE":
				case "INTERSECT":
				case "INTERSECTION":
				case "INTO":
				case "IS":
				case "ISNULL":
				case "ITERATE":
				case "JOIN":
				case "K":
				case "KEY_MEMBER":
				case "KEY_TYPE":
				case "LATERAL":
				case "LEADING":
				case "LEFT":
				case "LENGTH":
				case "LESS":
				case "LIKE":
				case "LIMIT":
				case "LN":
				case "LOCALTIME":
				case "LOCALTIMESTAMP":
				case "LOCATOR":
				case "LOWER":
				case "M":
				case "MAP":
				case "MATCHED":
				case "MAX":
				case "MEMBER":
				case "MERGE":
				case "MESSAGE_LENGTH":
				case "MESSAGE_OCTET_LENGTH":
				case "MESSAGE_TEXT":
				case "METHOD":
				case "MIN":
				case "MOD":
				case "MODIFIES":
				case "MODIFY":
				case "MODULE":
				case "MORE":
				case "MULTISET":
				case "MUMPS":
				case "NAME":
				case "NATURAL":
				case "NCLOB":
				case "NESTING":
				case "NEW":
				case "NOLOGIN":
				case "NORMALIZE":
				case "NORMALIZED":
				case "NOT":
				case "NOTNULL":
				case "NULL":
				case "NULLABLE":
				case "NULLS":
				case "NUMBER":
				case "OCTETS":
				case "OCTET_LENGTH":
				case "OFF":
				case "OFFSET":
				case "OLD":
				case "ON":
				case "ONLY":
				case "OPEN":
				case "OPERATION":
				case "OPTIONS":
				case "OR":
				case "ORDER":
				case "ORDERING":
				case "ORDINALITY":
				case "OTHERS":
				case "OUTER":
				case "OUTPUT":
				case "OVER":
				case "OVERLAPS":
				case "OVERRIDING":
				case "PAD":
				case "PARAMETER":
				case "PARAMETERS":
				case "PARAMETER_MODE":
				case "PARAMETER_NAME":
				case "PARAMETER_ORDINAL_POSITION":
				case "PARAMETER_SPECIFIC_CATALOG":
				case "PARAMETER_SPECIFIC_NAME":
				case "PARAMETER_SPECIFIC_SCHEMA":
				case "PARTITION":
				case "PASCAL":
				case "PATH":
				case "PERCENTILE_CONT":
				case "PERCENTILE_DISC":
				case "PERCENT_RANK":
				case "PLACING":
				case "PLI":
				case "POSTFIX":
				case "POWER":
				case "PRECEDING":
				case "PREFIX":
				case "PREORDER":
				case "PRIMARY":
				case "PUBLIC":
				case "RANGE":
				case "RANK":
				case "READS":
				case "RECURSIVE":
				case "REF":
				case "REFERENCES":
				case "REFERENCING":
				case "REGR_AVGX":
				case "REGR_AVGY":
				case "REGR_COUNT":
				case "REGR_INTERCEPT":
				case "REGR_R2":
				case "REGR_SLOPE":
				case "REGR_SXX":
				case "REGR_SXY":
				case "REGR_SYY":
				case "RESULT":
				case "RETURN":
				case "RETURNED_CARDINALITY":
				case "RETURNED_LENGTH":
				case "RETURNED_OCTET_LENGTH":
				case "RETURNED_SQLSTATE":
				case "RIGHT":
				case "ROLLUP":
				case "ROUTINE":
				case "ROUTINE_CATALOG":
				case "ROUTINE_NAME":
				case "ROUTINE_SCHEMA":
				case "ROW_COUNT":
				case "ROW_NUMBER":
				case "SCALE":
				case "SCHEMA_NAME":
				case "SCOPE":
				case "SCOPE_CATALOG":
				case "SCOPE_NAME":
				case "SCOPE_SCHEMA":
				case "SEARCH":
				case "SECTION":
				case "SELECT":
				case "SELF":
				case "SENSITIVE":
				case "SERVER_NAME":
				case "SESSION_USER":
				case "SETS":
				case "SIMILAR":
				case "SIZE":
				case "SOME":
				case "SOURCE":
				case "SPACE":
				case "SPECIFIC":
				case "SPECIFICTYPE":
				case "SPECIFIC_NAME":
				case "SQL":
				case "SQLCODE":
				case "SQLERROR":
				case "SQLEXCEPTION":
				case "SQLSTATE":
				case "SQLWARNING":
				case "SQRT":
				case "STATE":
				case "STATIC":
				case "STDDEV_POP":
				case "STDDEV_SAMP":
				case "STRUCTURE":
				case "STYLE":
				case "SUBCLASS_ORIGIN":
				case "SUBLIST":
				case "SUBMULTISET":
				case "SUM":
				case "SYMMETRIC":
				case "SYSTEM_USER":
				case "TABLE":
				case "TABLESAMPLE":
				case "TABLE_NAME":
				case "TERMINATE":
				case "THAN":
				case "THEN":
				case "TIES":
				case "TIMEZONE_HOUR":
				case "TIMEZONE_MINUTE":
				case "TO":
				case "TOP_LEVEL_COUNT":
				case "TRAILING":
				case "TRANSACTIONS_COMMITTED":
				case "TRANSACTIONS_ROLLED_BACK":
				case "TRANSACTION_ACTIVE":
				case "TRANSFORM":
				case "TRANSFORMS":
				case "TRANSLATE":
				case "TRANSLATION":
				case "TRIGGER_CATALOG":
				case "TRIGGER_NAME":
				case "TRIGGER_SCHEMA":
				case "1":
				case "UESCAPE":
				case "UNBOUNDED":
				case "UNDER":
				case "UNION":
				case "UNIQUE":
				case "UNNAMED":
				case "UNNEST":
				case "UPPER":
				case "USAGE":
				case "USER":
				case "USER_DEFINED_TYPE_CATALOG":
				case "USER_DEFINED_TYPE_CODE":
				case "USER_DEFINED_TYPE_NAME":
				case "USER_DEFINED_TYPE_SCHEMA":
				case "USING":
				case "VALUE":
				case "VARIABLE":
				case "VAR_POP":
				case "VAR_SAMP":
				case "VERBOSE":
				case "WHEN":
				case "WHENEVER":
				case "WHERE":
				case "WIDTH_BUCKET":
				case "WINDOW":
				case "WITHIN":
					return true;
				default:
					return false;
			}
		}
	}
}