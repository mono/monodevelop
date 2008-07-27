// DatabaseProperty.cs
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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data;
using System.Text;

using Mono.Data.SqliteClient;
using Hyena.Data.Sqlite;

namespace MonoDevelop.Projects.Dom.Database
{
	[Flags]
	public enum PropertyModifier {	
		None      = 0,
		IsIndexer = 1,
		HasGet    = 2,
		HasSet    = 4
	}
	
	public class DatabaseProperty : DomProperty
	{
		internal const string Table = "Properties";
		
		DatabaseType declaringType;
		long memberId;
		
		bool hasSet;
		bool hasGet;
		
		public override bool HasSet {
			get {
				return hasSet;
			}
		}
		
		public override bool HasGet {
			get {
				return hasGet;
			}
		}
		
		bool readParameterList = false;
		public override ReadOnlyCollection<IParameter> Parameters {
			get {
				if (!readParameterList) {
					foreach (IParameter parameter in DatabaseParameter.ReadList (declaringType.Database, declaringType.TypeId, memberId)) {
						Add (parameter);
					}
					readParameterList = true;
				}
				return base.Parameters;
			}
		}
		
		public DatabaseProperty (DatabaseType declaringType, long memberId, PropertyModifier modifier)
		{
			this.memberId = memberId;
			this.DeclaringType = this.declaringType = declaringType;
			
			this.hasGet        = (modifier & PropertyModifier.HasGet) == PropertyModifier.HasGet;
			this.hasSet        = (modifier & PropertyModifier.HasSet) == PropertyModifier.HasSet;
			this.IsIndexer     = (modifier & PropertyModifier.IsIndexer) == PropertyModifier.IsIndexer;
			
			DatabaseField.FillMembers (this, declaringType.Database, memberId);
		}
		
		public void Delete ()
		{
			if (Parameters != null) {
				foreach (DatabaseParameter para in Parameters) {
					para.Delete ();
				}
			}
			declaringType.Database.DeleteMember (memberId);
			declaringType.Database.Connection.Execute (String.Format (@"DELETE FROM {0} WHERE TypeID={1} AND MemberID={2}", Table, declaringType.TypeId, memberId));
		}
	
		public static void CheckTables (CodeCompletionDatabase db)
		{
			if (!db.Connection.TableExists (Table)) {
				db.Connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						TypeID INTEGER,
						MemberID INTEGER,
						PropertyModifiers INTEGER
					)", Table
				));
			}
		}
		
		public static IEnumerable<IProperty> ReadList (DatabaseType declaringType)
		{
			IDataReader reader = declaringType.Database.Connection.Query (String.Format (@"SELECT MemberID, PropertyModifiers FROM {0} WHERE TypeId={1}", Table, declaringType.TypeId));
			if (reader != null) {
				try {
					while (reader.Read ()) {
						long             memberId = SqliteUtils.FromDbFormat<long> (reader[0]);
						PropertyModifier modifier = (PropertyModifier)SqliteUtils.FromDbFormat<long> (reader[1]);
						yield return new DatabaseProperty (declaringType, memberId, modifier);
					}
				} finally {
					reader.Dispose ();
				}
			}
		}
		
		public static void Insert (CodeCompletionDatabase db, long typeId, IProperty property)
		{
			long memberId = db.InsertMember (property);
			DatabaseParameter.InsertParameterList (db, typeId, memberId, property.Parameters);
			PropertyModifier modifier = PropertyModifier.None;
			if (property.IsIndexer)
				modifier |= PropertyModifier.IsIndexer;
			if (property.HasGet)
				modifier |= PropertyModifier.HasGet;
			if (property.HasSet)
				modifier |= PropertyModifier.HasSet;
				
			db.Connection.Execute (String.Format (@"INSERT INTO {0} (TypeID, MemberID, PropertyModifiers) VALUES ({1}, {2}, {3})", 
				Table,
				typeId,
				memberId,
				(long)modifier
			));
		}
		
	}
}
