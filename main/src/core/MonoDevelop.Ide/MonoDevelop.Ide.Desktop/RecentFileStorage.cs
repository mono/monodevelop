//
// RecentFileStorage.cs
//
// Implementation of Recent File Storage according to 
// "Recent File Storage Specification v0.2" from freedesktop.org.
//
// http://standards.freedesktop.org/recent-file-spec/recent-file-spec-0.2.html
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Linq;

namespace MonoDevelop.Ide.Desktop
{
	/// <summary>
	/// Implementation of Recent File Storage according to 
	/// "Recent File Storage Specification v0.2" from freedesktop.org.
	///
	/// http://standards.freedesktop.org/recent-file-spec/recent-file-spec-0.2.html
	/// </summary>
	internal sealed class RecentFileStorage : IDisposable
	{
		const int MaxRecentItemsCount = 500; // max. items according to the spec.
		
		string filePath;
		FileSystemWatcher watcher;
		object writerLock = new object ();
		
		public static string DefaultPath {
			get {
				return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".recently-used");
			}
		}
		
		public RecentFileStorage (string filePath)
		{
			this.filePath = filePath;
		}
		
		void EnableWatching ()
		{
			if (watcher != null)
				return;
			var dirName = Path.GetDirectoryName (filePath);
			if (!Directory.Exists (dirName))
				Directory.CreateDirectory (dirName);
			watcher = new FileSystemWatcher (dirName, Path.GetFileName (filePath));
			watcher.Created += FileChanged;
			watcher.Changed += FileChanged;
			watcher.Deleted += FileChanged;
			watcher.Renamed += HandleWatcherRenamed;
			watcher.EnableRaisingEvents = true;
		}

		void DisableWatching ()
		{
			if (watcher == null)
				return;
			watcher.EnableRaisingEvents = false;
			watcher.Created -= FileChanged;
			watcher.Changed -= FileChanged;
			watcher.Deleted -= FileChanged;
			watcher.Renamed -= HandleWatcherRenamed;
			watcher.Dispose ();
			watcher = null;
		}
		
		void FileChanged (object sender, FileSystemEventArgs e)
		{
			OnRecentFilesChanged (EventArgs.Empty);
		}
		
		void HandleWatcherRenamed (object sender, RenamedEventArgs e)
		{
			OnRecentFilesChanged (EventArgs.Empty);
		}	
		
		bool FilterOut (Func<RecentItem,bool> pred)
		{
			lock (writerLock) {
				bool filteredSomething = false;
				List<RecentItem> store = ReadStore (0);
				if (store != null) {
					for (int i = 0; i < store.Count; ++i) {
						if (pred (store[i])) {
							store.RemoveAt (i);
							filteredSomething = true;
							--i;
							continue;
						}
					}
					if (filteredSomething) 
						WriteStore (store);
				}
				return filteredSomething;
			}
		}
		
		///operation should return true if it modified an item
		bool RunOperation (Func<RecentItem,bool> operation)
		{
			lock (writerLock) {
				bool changedSomething = false;
				List<RecentItem> store = ReadStore (0);
				if (store != null) {
					for (int i = 0; i < store.Count; ++i) 
						changedSomething |= operation (store[i]);
					if (changedSomething)
						WriteStore (store);
				}
				return changedSomething;
			}
		}
		
		public void ClearGroup (params string[] groups)
		{
			FilterOut (item => groups.Any (g => item.IsInGroup (g)));
		}
		
		public void RemoveMissingFiles (params string[] groups)
		{
			FilterOut (item => item.IsFile && groups.Any (g => item.IsInGroup (g)) && !File.Exists (item.LocalPath));
		}
		
		public bool RemoveItem (string uri)
		{
			return uri != null && FilterOut (item => item.Uri != null && item.Uri.Equals (uri));
		}
		
		public bool RemoveItem (RecentItem item)
		{
			return item != null && RemoveItem (item.Uri);
		}
		
		public bool RenameItem (string oldUri, string newUri)
		{
			if (oldUri == null || newUri == null)
				return false;
			return RunOperation (delegate(RecentItem item) {
				if (item.Uri == null)
					return false;
				if (item.Uri == oldUri) {
					string oldName = Path.GetFileName (item.LocalPath);
					item.Uri = newUri;
					if (item.Private.Contains (oldName)) {
						item.Private = item.Private.Replace (oldName, Path.GetFileName (item.LocalPath));
					}
					item.NewTimeStamp ();
					return true;
				}
				return false;
			});
		}
		
		public RecentItem[] GetItemsInGroup (string group)
		{
			List<RecentItem> result = new List<RecentItem> ();
			RunOperation (delegate(RecentItem item) {
				if (item.IsInGroup (group)) 
					result.Add (item);
				return false;
			});
			result.Sort ();
			return result.ToArray ();
		}
		
		void CheckLimit (string group, int limit)
		{
			RecentItem[] items = GetItemsInGroup (group);
			for (int i = limit; i < items.Length; i++)
				this.RemoveItem (items[i]);
		}
		
		public void AddWithLimit (RecentItem item, string group, int limit)
		{
			lock (writerLock) {
				RemoveItem (item.Uri);
				List<RecentItem> store = ReadStore (0);
				if (store != null) {
					store.Add (item);
					WriteStore (store);
					CheckLimit (group, limit);
				}
			}
		}
		const int MAX_TRIES = 5;
		List<RecentItem> ReadStore (int numberOfTry)
		{
			List<RecentItem> result = new List<RecentItem> ();
			if (!File.Exists (filePath))
				return result;
			var reader = new XmlTextReader (filePath);
			try {
				while (true) {
					bool read = false;
					try {
						// seems to crash on empty files ?
						read = reader.Read ();
					} catch (Exception) { 
						read = false; 
					}
					if (!read)
						break;
					if (reader.IsStartElement () && reader.LocalName == RecentItem.Node)
						result.Add (RecentItem.Read (reader));
				}
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Exception while reading the store", e);
				if (numberOfTry < MAX_TRIES) {
					Thread.Sleep (200);
					return ReadStore (numberOfTry + 1);
				}
				return null;
			} finally {
				if (reader != null)
					reader.Close ();
			}
			return result;
		}
		
		static Encoding utf8WithoutByteOrderMark = new UTF8Encoding (false);
		void WriteStore (List<RecentItem> items)
		{
			items.Sort ();
			if (items.Count > MaxRecentItemsCount)
				items.RemoveRange (MaxRecentItemsCount, items.Count - MaxRecentItemsCount);
			var writer = new XmlTextWriter (filePath, utf8WithoutByteOrderMark);
			try {
				writer.Formatting = Formatting.Indented;
				writer.WriteStartDocument ();
				writer.WriteStartElement ("RecentFiles");
				if (items != null) 
					foreach (RecentItem item in items)
						item.Write (writer);
				writer.WriteEndElement (); // RecentFiles
			} finally {
				writer.Close ();
				OnRecentFilesChanged (EventArgs.Empty);
			}
		}
		
		public static string ToUri (string fileName)
		{
			return fileName.StartsWith ("file://") ? fileName : "file://" + fileName;
		}
		
		void OnRecentFilesChanged (EventArgs e)
		{
			if (changed != null)
				changed (this, e);
		}
		
		EventHandler changed;
		public event EventHandler RecentFilesChanged {
			add {
				lock (this) {
					if (changed == null)
						EnableWatching ();
					changed += value;
				}
			}
			remove {
				lock (this) {
					changed -= value;
					if (changed == null)
						DisableWatching ();
				}
			}
		}
		
		public void Dispose ()
		{
			changed = null;
			DisableWatching ();
		}
	}
}
