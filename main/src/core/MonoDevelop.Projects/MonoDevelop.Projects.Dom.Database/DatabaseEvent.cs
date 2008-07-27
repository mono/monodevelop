// DatabaseEvent.cs
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
	public class DatabaseEvent : DomEvent
	{
		internal const string Table = "Events";
		DatabaseType declaringType;
		long memberId;
		
		public DatabaseEvent (DatabaseType declaringType, long memberId)
		{
			this.DeclaringType = this.declaringType = declaringType;
			this.memberId      = memberId;
			DatabaseField.FillMembers (this, declaringType.Database, memberId);
		}
		
		public void Delete ()
		{
			declaringType.Database.DeleteMember (memberId);
			declaringType.Database.Connection.Execute (String.Format (@"DELETE FROM {0} WHERE TypeID={1} AND MemberID={2}", Table, declaringType.TypeId, memberId));
		}
		
		
		
		public static void CheckTables (CodeCompletionDatabase db)
		{
			if (!db.Connection.TableExists (Table)) {
				db.Connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						TypeID INTEGER,
						MemberID INTEGER
					)", Table
				));
			}
		}
		
		public static IEnumerable<IEvent> ReadList (DatabaseType declaringType)
		{
			IDataReader reader = declaringType.Database.Connection.Query (String.Format (@"SELECT MemberID FROM {0} WHERE TypeId={1}", Table, declaringType.TypeId));
			if (reader != null) {
				try {
					while (reader.Read ()) {
						long memberId = SqliteUtils.FromDbFormat<long> (reader[0]);
						yield return new DatabaseEvent (declaringType, memberId);
					}
				} finally {
					reader.Dispose ();
				}
			}
		}
		
		public static void Insert (CodeCompletionDatabase db, long typeId, IEvent evt)
		{
			long memberId = db.InsertMember (evt);
			db.Connection.Execute (String.Format (@"INSERT INTO {0} (TypeID, MemberID) VALUES ({1}, {2})", 
				Table,
				typeId,
				memberId
			));
		}
	}
}
