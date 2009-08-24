// 
// ParserItem.cs
//  
// Author:
//       Christian Hergert <chris@dronelabs.com>
// 
// Copyright (c) 2009 Christian Hergert
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

using System;
using System.Data;

using Mono.Data.Sqlite;

namespace PyBinding.Parser
{
	public enum ParserItemType
	{
		Module,
		Class,
		Function,
		Attribute,
		Local,
	}
	
	public class ParserItem
	{
		public string FileName {
			get;
			set;
		}
		
		public int LineNumber {
			get;
			set;
		}
		
		public ParserItemType ItemType {
			get;
			set;
		}
		
		public string FullName {
			get;
			set;
		}
		
		public string Documentation {
			get;
			set;
		}
		
		public string Extra {
			get;
			set;
		}
		
		static SqliteCommand s_InsertCommand;
		
		SqliteCommand GetInsertCommand ()
		{
			if (s_InsertCommand == null) {
				var command = new SqliteCommand ();
				command.CommandText =
					"INSERT OR REPLACE into Items (" +
					"FullName, FileName, LineNumber, ItemType, Pydoc, Extra) " +
					"VALUES (@FullName, @FileName, @LineNumber, @ItemType, @Pydoc, @Extra)";
				command.Parameters.Add ("FullName", DbType.String);
				command.Parameters.Add ("FileName", DbType.String);
				command.Parameters.Add ("LineNumber", DbType.Int32);
				command.Parameters.Add ("ItemType", DbType.Int32);
				command.Parameters.Add ("Pydoc", DbType.String);
				command.Parameters.Add ("Extra", DbType.String);
				s_InsertCommand = command;
			}
			
			var copyCommand = s_InsertCommand.Clone () as SqliteCommand;
			return copyCommand;
		}
		
		public void Serialize (SqliteConnection conn)
		{
			using (var command = GetInsertCommand ())
			{
				command.Connection = conn;
				command.Parameters["FullName"].Value = FullName;
				command.Parameters["FileName"].Value = FileName;
				command.Parameters["LineNumber"].Value = LineNumber;
				command.Parameters["ItemType"].Value = (int)ItemType;
				command.Parameters["Pydoc"].Value = Documentation;
				command.Parameters["Extra"].Value = Extra;
				command.ExecuteNonQuery ();
			}
		}
		
		public void Deserialize (SqliteDataReader reader)
		{
			this.FullName = reader.GetString (0) as String;
			this.FileName = reader.GetString (1) as String;
			this.LineNumber = reader.GetInt32 (2);
			this.ItemType = (ParserItemType)reader.GetInt32 (3);
			var docValue = reader.GetValue (4);
			if (docValue.GetType () == typeof (String))
				this.Documentation = (string)docValue;
			var extraValue = reader.GetValue (5);
			if (extraValue.GetType () == typeof (string))
				this.Extra = (string)extraValue;
		}
	}
}