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
using MonoDevelop.Ide;

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
		static readonly TimeSpan s_lockTimeout = TimeSpan.FromSeconds (60);
		static readonly int s_version = 1;
		static SqliteCommand s_RemoveByFilePrefix;
		static SqliteCommand s_Find;
		static SqliteCommand s_FindWithType;
		
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
		
		string VersionFile {
			get {
				return m_FileName + ".version";
			}
		}
		
		bool NeedsUpgrade {
			get {
				if (!File.Exists (m_FileName))
					return false;
				else if (!File.Exists (VersionFile))
					return true;
				int version;
				string content = File.ReadAllText (VersionFile).Trim ();
				if (!Int32.TryParse (content, out version))
					return true;
				return version != s_version;
			}
		}
		
		string ConnStrForFile (string filename)
		{
			return String.Format ("Data Source={0}", filename);
		}
		
		public void Open ()
		{
			string connString = ConnStrForFile (m_FileName);
			
			var dirInfo = new FileInfo (m_FileName).Directory;
			if (!dirInfo.Exists)
				dirInfo.Create ();
			
			m_rwLock.AcquireWriterLock (s_lockTimeout);
			
			try {
				// Backup if needed
				var backup = m_FileName + ".bak";
				var needsUpgrade = NeedsUpgrade;
				if (needsUpgrade && File.Exists (m_FileName))
					File.Move (m_FileName, backup);
				else if (!File.Exists (m_FileName))
					File.WriteAllText (VersionFile, String.Format ("{0}", s_version));
				
				// Build
				m_conn = new SqliteConnection (connString);
				m_conn.Open ();
				EnsureTables ();
				
				if (needsUpgrade) {
					File.WriteAllText (VersionFile, String.Format ("{0}", s_version));
					
					// Open backup database
					var conn = new SqliteConnection (ConnStrForFile (backup));
					conn.Open ();
					
					// Copy the database in a thread
					ThreadPool.QueueUserWorkItem (delegate {
						try {
							this.CopyDatabase (conn);
						} catch (Exception ex) {
							Console.WriteLine (ex.ToString ());
						}
						
						conn.Close ();
						conn.Dispose ();
						
						if (File.Exists (backup))
							File.Delete (backup);
					});
				}
			}
			finally {
				m_rwLock.ReleaseWriterLock ();
			}
		}
		
		public void Close ()
		{
			m_rwLock.AcquireWriterLock (s_lockTimeout);
			try {
				m_conn.Close ();
			}
			finally {
				m_rwLock.ReleaseWriterLock ();
			}
		}
		
		public void Add (ParserItem item)
		{
			m_rwLock.AcquireWriterLock (s_lockTimeout);
			try {
				item.Serialize (m_conn);
			}
			finally {
				m_rwLock.ReleaseWriterLock ();
			}
		}
		
		public void AddRange (IEnumerable<ParserItem> items)
		{
			m_rwLock.AcquireWriterLock (s_lockTimeout);
			try {
				foreach (var item in items)
					item.Serialize (m_conn);
			} finally {
				m_rwLock.ReleaseWriterLock ();
			}
		}
		
		public IEnumerable<ParserItem> Find (string prefix)
		{
			if (s_Find == null) {
				var command = new SqliteCommand ();
				command.CommandText = "SELECT " + s_ItemColumns + " FROM Items WHERE FullName LIKE @FullName;";
				command.CommandType = CommandType.Text;
				command.Parameters.Add ("FullName", DbType.String);
				s_Find = command;
			}
			
			var find = s_Find.Clone () as SqliteCommand;
			find.Connection = m_conn;
			find.Parameters ["FullName"].Value = prefix.Replace ("%", "\\%") + "%";
			
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
		
		const string s_ItemColumns = "FullName, FileName, LineNumber, ItemType, PyDoc, Extra";
		
		public IEnumerable<ParserItem> Find (string prefix, ParserItemType itemType)
		{
			foreach (var item in Find (prefix, itemType, -1))
				yield return item;
		}
		
		public IEnumerable<ParserItem> Find (string prefix, ParserItemType itemType, int depth)
		{
			if (s_FindWithType == null) {
				var command = new SqliteCommand ();
				command.CommandText = "SELECT " + s_ItemColumns + " FROM Items WHERE FullName LIKE @FullName";
				command.CommandType = CommandType.Text;
				command.Parameters.Add ("FullName", DbType.String);
				command.Parameters.Add ("ItemType", DbType.Int32);
				s_FindWithType = command; // Race condition shouldn't matter
			}
			
			var findWithType = s_FindWithType.Clone () as SqliteCommand;
			
			if (itemType != ParserItemType.Any) {
				findWithType.CommandText += " AND ItemType == @ItemType";
				findWithType.Parameters.Add ("ItemType", DbType.Int32);
				findWithType.Parameters ["ItemType"].Value = (int)itemType;
			}
			
			if (depth >= 0) {
				findWithType.CommandText += " AND Depth == @Depth";
				findWithType.Parameters.Add ("Depth", DbType.Int32);
				findWithType.Parameters ["Depth"].Value = depth;
			}
			
			findWithType.Connection = m_conn;
			findWithType.Parameters ["FullName"].Value = prefix.Replace ("%", "\\%") + "%";
			
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
			string schemaSql;
			
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream ("Schema.sql"))
				using (var reader = new StreamReader (stream))
					schemaSql = reader.ReadToEnd ();
			
			foreach (string commandText in schemaSql.Split (';'))
				new SqliteCommand (commandText, m_conn).ExecuteNonQuery ();
		}
		
		void CopyDatabase (SqliteConnection src)
		{
			Console.WriteLine ("Migrating python completion database to version {0}", s_version);
			
			int batchSize = 100;
			var cmd = new SqliteCommand ("SELECT " + s_ItemColumns + " FROM Items;", src);
			var reader = cmd.ExecuteReader ();
			int i = 0;
			var items = new List<ParserItem> ();
			
			var rowsCommand = new SqliteCommand ("SELECT count(*) FROM Items;", src);
			var count = (long)rowsCommand.ExecuteScalar ();
			
			var progress = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Python Completion Database", Gtk.Stock.Execute);
			progress.BeginTask ("Migrating completion database", (int)count);
			
			while (reader.Read ())
			{
				var item = new ParserItem ();
				item.Deserialize (reader);
				items.Add (item);
				i++;
				if (i % batchSize == 0) {
					AddRange (items);
					progress.Step (items.Count);
					items.Clear ();
				}
			}
			
			if (items.Count > 0)
				AddRange (items);
			progress.Step (items.Count);
			items.Clear ();
			
			progress.Dispose ();
		}
	}
}
