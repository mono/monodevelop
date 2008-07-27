// DatabaseParameter.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Data;
using System.Text;

using Mono.Data.SqliteClient;
using Hyena.Data.Sqlite;

namespace MonoDevelop.Projects.Dom.Database
{
	public class DatabaseParameter : DomParameter
	{
		internal const string Table = "Parameters";
		CodeCompletionDatabase db;
		long typeId;
		long memberId;
		
		public DatabaseParameter (CodeCompletionDatabase db, long typeId, long memberId)
		{
			this.db = db;
			this.typeId = typeId;
			this.memberId = memberId;
		}
		
		public void Delete ()
		{
			db.Connection.Execute (String.Format (@"DELETE FROM {0} WHERE TypeID={1} AND MemberID={2}", Table, typeId, memberId));
		}
	
		public static IEnumerable<IParameter> ReadList (CodeCompletionDatabase db, long typeId, long memberId)
		{
			IDataReader reader = db.Connection.Query (String.Format (@"SELECT ParameterModifiers, ReturnTypeID, Name FROM {0} WHERE TypeId={1} AND MemberId={2}", Table, typeId, memberId));
			if (reader != null) {
				try {
					while (reader.Read ()) {
						DatabaseParameter result = new DatabaseParameter (db, typeId, memberId);
						result.ParameterModifiers = (ParameterModifiers)SqliteUtils.FromDbFormat<long> (reader[0]);
						result.ReturnType         = db.ReadReturnType (SqliteUtils.FromDbFormat<long> (reader[1]));
						result.Name               = SqliteUtils.FromDbFormat<string> (reader[2]);
						yield return result;
					}
				} finally {
					reader.Dispose ();
				}
			}
		}

		
		public static void CheckTables (CodeCompletionDatabase db)
		{
			if (!db.Connection.TableExists (Table)) {
				db.Connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						TypeID INTEGER,
						MemberID INTEGER,
						ParameterModifiers INTEGER,
						ReturnTypeID INTEGER,
						Name TEXT
						)", Table
				));
			}
		}
		
		public static void InsertParameterList (CodeCompletionDatabase db, long typeId, long memberId, IEnumerable<IParameter> parameters)
		{
			if (parameters == null)
				return;
			foreach (IParameter param in parameters) {
				db.Connection.Execute (String.Format (@"INSERT INTO {0} (TypeID, MemberID, ParameterModifiers, ReturnTypeID, Name) VALUES ({1}, {2}, {3}, {4}, '{5}')", 
					Table,
					typeId,
					memberId,
					(long)param.ParameterModifiers,
					db.GetReturnTypeID (param.ReturnType),
					param.Name
				));
			}
		}
	}
}
