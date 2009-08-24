// 
// ParserDatabase.cs
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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading;

using Mono.Data.Sqlite;

namespace PyBinding.Parser
{
	public delegate void ParserItemCallback (ParserItem item);
	
	/// <summary>
	/// This is a crappy little storage system for the parsed data from python
	/// source files.
	/// </summary>
	public class ParserDatabase
	{
		ReaderWriterLock m_rwLock;
		string m_FileName;
		SqliteConnection m_conn;
		
		public ParserDatabase (string fileName)
		{
			m_FileName = fileName;
			m_rwLock = new ReaderWriterLock ();
		}
		
		public string FileName {
			get { return m_FileName; }
		}
		
		public void Open ()
		{
			string connString = String.Format ("Data Source={0}", m_FileName);
			
			var dirInfo = new FileInfo (m_FileName).Directory;
			if (!dirInfo.Exists)
				dirInfo.Create ();
			
			m_rwLock.AcquireWriterLock (TimeSpan.FromSeconds (60));
			
			if (m_conn == null)
				m_conn = new SqliteConnection (connString);
			m_conn.Open ();
			EnsureTables ();
			
			m_rwLock.ReleaseWriterLock ();
		}
		
		public void Close ()
		{
			m_rwLock.AcquireWriterLock (TimeSpan.FromSeconds (60));
			m_conn.Close ();
			m_rwLock.ReleaseWriterLock ();
		}
		
		public void Add (ParserItem item)
		{
			m_rwLock.AcquireWriterLock (TimeSpan.FromSeconds (60));
			item.Serialize (m_conn);
			m_rwLock.ReleaseWriterLock ();
		}
		
		public void AddRange (IEnumerable<ParserItem> items)
		{
			m_rwLock.AcquireWriterLock (TimeSpan.FromSeconds (60));
			foreach (var item in items)
				item.Serialize (m_conn);
			m_rwLock.ReleaseWriterLock ();
		}
		
		static SqliteCommand s_Find;
		static SqliteCommand s_FindWithType;
		
		public IEnumerable<ParserItem> Find (string prefix)
		{
			m_rwLock.AcquireReaderLock (TimeSpan.FromSeconds (60));
			
			if (s_Find == null) {
				var command = new SqliteCommand ();
				command.CommandText = "SELECT * FROM Items WHERE FullName LIKE @FullName;";
				command.CommandType = CommandType.Text;
				command.Parameters.Add ("FullName", DbType.String);
				s_Find = command;
			}
			
			var find = s_Find.Clone () as SqliteCommand;
			find.Connection = m_conn;
			find.Parameters ["FullName"].Value = prefix.Replace ("%", "\\%") + "%";
			
			m_rwLock.ReleaseReaderLock ();
			
			using (var reader = find.ExecuteReader ())
			{
				while (reader.Read ())
				{
					ParserItem item = new ParserItem ();
					item.Deserialize (reader);
					yield return item;
				}
			}
		}
		
		public IEnumerable<ParserItem> Find (string prefix, ParserItemType itemType)
		{
			m_rwLock.AcquireReaderLock (TimeSpan.FromSeconds (60));
			
			if (s_FindWithType == null) {
				var command = new SqliteCommand ();
				command.CommandText = "SELECT * FROM Items WHERE ItemType = @ItemType AND FullName LIKE @FullName;";
				command.CommandType = CommandType.Text;
				command.Parameters.Add ("FullName", DbType.String);
				command.Parameters.Add ("ItemType", DbType.Int32);
				s_FindWithType = command;
			}
			
			var findWithType = s_FindWithType.Clone () as SqliteCommand;
			findWithType.Connection = m_conn;
			findWithType.Parameters ["FullName"].Value = prefix.Replace ("%", "\\%") + "%";
			findWithType.Parameters ["ItemType"].Value = (int)itemType;
			
			m_rwLock.ReleaseReaderLock ();
			
			using (var reader = findWithType.ExecuteReader ())
			{
				while (reader.Read ())
				{
					ParserItem item = new ParserItem ();
					item.Deserialize (reader);
					yield return item;
				}
			}
		}
		
		static SqliteCommand s_RemoveByFilePrefix;
		
		public void RemoveByFilePrefix (string prefix)
		{
			if (s_RemoveByFilePrefix == null) {
				var command = new SqliteCommand ();
				command.CommandText = "DELETE FROM Items WHERE FileName LIKE @FileName;";
				command.CommandType = CommandType.Text;
				command.Parameters.Add ("FileName", DbType.String);
				s_RemoveByFilePrefix = command;
			}
			
			var copyCommand = s_RemoveByFilePrefix.Clone () as SqliteCommand;
			copyCommand.Connection = m_conn;
			copyCommand.Parameters ["FileName"].Value = prefix + "%";
			copyCommand.ExecuteNonQuery ();
		}
		
		void EnsureTables ()
		{
			string schemaSql = String.Empty;
			
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream ("Schema.sql"))
				using (var reader = new StreamReader (stream))
					schemaSql = reader.ReadToEnd ();
			
			foreach (string commandText in schemaSql.Split (';'))
				new SqliteCommand (commandText, m_conn).ExecuteNonQuery ();
		}
	}
}
