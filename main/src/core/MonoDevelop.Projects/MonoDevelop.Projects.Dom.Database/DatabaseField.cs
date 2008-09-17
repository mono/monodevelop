// DatabaseField.cs
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
//
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Text;
//
//using Mono.Data.SqliteClient;
//using Hyena.Data.Sqlite;
//
//namespace MonoDevelop.Projects.Dom.Database
//{
//	public class DatabaseField : DomField
//	{
//		internal const string Table = "Fields";
//		DatabaseType declaringType;
//		long memberId;
//		
//		public DatabaseField (DatabaseType declaringType, long memberId)
//		{
//			this.DeclaringType = this.declaringType = declaringType;
//			this.memberId      = memberId;
//			FillMembers (this, declaringType.Database, memberId);
//		}
//		
//		public void Delete ()
//		{
//			declaringType.Database.DeleteMember (memberId);
//			declaringType.Database.Connection.Execute (String.Format (@"DELETE FROM {0} WHERE TypeID={1} AND MemberID={2}", Table, declaringType.TypeId, memberId));
//		}
//		
//		internal static void FillMembers (IMember member, CodeCompletionDatabase db, long memberId)
//		{
//			string query = String.Format (@"SELECT ReturnTypeID, Name, Documentation, Location, BodyRegion, Modifiers FROM {0} WHERE MemberId = {1}", 
//			                              CodeCompletionDatabase.MemberTable,
//			                              memberId);
//			IDataReader reader = db.Connection.Query (query);
//			if (reader != null) {
//				if (reader.Read ()) {
//					long returnTypeID    = SqliteUtils.FromDbFormat<long> (reader[0]);
//					member.ReturnType    = db.ReadReturnType (returnTypeID);
//					member.Name          = SqliteUtils.FromDbFormat<string> (reader[1]);
//					member.Documentation = SqliteUtils.FromDbFormat<string> (reader[2]);
//					member.Location      = DomLocation.FromInvariantString (SqliteUtils.FromDbFormat<string> (reader[3]));
//					member.BodyRegion    = DomRegion.FromInvariantString (SqliteUtils.FromDbFormat<string> (reader[4]));
//					member.Modifiers     = (Modifiers)SqliteUtils.FromDbFormat<long> (reader[5]);
//					
//				}
//			}
//		}
//		
//		public static void CheckTables (CodeCompletionDatabase db)
//		{
//			if (!db.Connection.TableExists (Table)) {
//				db.Connection.Execute (String.Format (@"
//					CREATE TABLE {0} (
//						TypeID INTEGER,
//						MemberID INTEGER
//					)", Table
//				));
//			}
//		}
//		
//		public static IEnumerable<IField> ReadList (DatabaseType declaringType)
//		{
//			IDataReader reader = declaringType.Database.Connection.Query (String.Format (@"SELECT MemberID FROM {0} WHERE TypeId={1}", Table, declaringType.TypeId));
//			if (reader != null) {
//				try {
//					while (reader.Read ()) {
//						long memberId = SqliteUtils.FromDbFormat<long> (reader[0]);
//						yield return new DatabaseField (declaringType, memberId);
//					}
//				} finally {
//					reader.Dispose ();
//				}
//			}
//		}
//		
//		public static void Insert (CodeCompletionDatabase db, long typeId, IField field)
//		{
//			long memberId = db.InsertMember (field);
//			db.Connection.Execute (String.Format (@"INSERT INTO {0} (TypeID, MemberID) VALUES ({1}, {2})",
//				Table,
//				typeId,
//				memberId
//			));
//		}
//	}
//}
