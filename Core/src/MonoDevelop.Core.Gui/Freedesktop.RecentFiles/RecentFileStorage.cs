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
using System.IO;
using System.Xml;

namespace Freedesktop.RecentFiles
{
	/// <summary>
	/// Implementation of Recent File Storage according to 
	/// "Recent File Storage Specification v0.2" from freedesktop.org.
	///
	/// http://standards.freedesktop.org/recent-file-spec/recent-file-spec-0.2.html
	/// </summary>
	public sealed class RecentFileStorage
	{
		const int MaxRecentItemsCount = 500; // max. items according to the spec.
		static readonly string FileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".recently-used");
		
//		FileSystemWatcher watcher;
        
		public RecentFileStorage()
		{
/* TODO: watcher
			watcher = new FileSystemWatcher (FileName);
			watcher.Changed += delegate {
				OnRecentFilesChanged (EventArgs.Empty);
			};
			watcher.EnableRaisingEvents = true;*/
		}
		int lockLevel = 0;
		void ObtainLock ()
		{
			lockLevel++;
			// todo
		}
		
		void ReleaseLock ()
		{
			if (lockLevel <= 0)
				throw new InvalidOperationException ("not locked.");
			lockLevel--;
			// todo
		}
		
		delegate bool RecentItemPredicate (RecentItem item);
		delegate void RecentItemOperation (RecentItem item);
		void FilterOut (RecentItemPredicate pred)
		{
			ObtainLock ();
			try {
				bool filteredSomething = false;
				List<RecentItem> store = ReadStore ();
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
			} finally {
				ReleaseLock ();
			}
		}
		void RunOperation (bool writeBack, RecentItemOperation operation)
		{
			ObtainLock ();
			try {
				List<RecentItem> store = ReadStore ();
				for (int i = 0; i < store.Count; ++i) 
					operation (store[i]);
				if (writeBack) 
					WriteStore (store);
			} finally {
				ReleaseLock ();
			}
		}
		
		public void ClearGroup (string group)
		{
			FilterOut (delegate(RecentItem item) {
				return item.IsInGroup (group);
			});
		}
		
		public void RemoveMissingFiles (string group)
		{
			FilterOut (delegate(RecentItem item) {
				return item.IsInGroup (group) &&
					   item.Uri.StartsWith ("file:") &&
					   !File.Exists (new Uri (item.Uri).LocalPath);
			});
		}
		

		public void RemoveItem (Uri uri)
		{
			FilterOut (delegate(RecentItem item) {
				return item.Uri == uri.ToString ();
			});
		}
		
		public void RemoveItem (RecentItem itemToRemove)
		{
			FilterOut (delegate(RecentItem item) {
				return item == itemToRemove;
			});
		}
		
		public void RenameItem (Uri oldUri, Uri newUri)
		{
			RunOperation (true, delegate(RecentItem item) {
				if (item.Uri == oldUri.ToString ()) {
					item.Uri = newUri.ToString ();
					item.NewTimeStamp ();
				}
			});
		}
		
		public RecentItem[] GetItemsInGroup (string group)
		{
			List<RecentItem> result = new List<RecentItem> ();
			RunOperation (false, delegate(RecentItem item) {
				if (item.IsInGroup (group)) 
					result.Add (item);
			});
			result.Sort ();
			return result.ToArray ();
		}
		
		public void AddWithLimit (RecentItem item, string group, int max)
		{
			ObtainLock ();
			try {
				RecentItem[] items = GetItemsInGroup (group);
				if (items.Length == max && items.Length > 0)
					this.RemoveItem (items[items.Length - 1]);
				
				List<RecentItem> store = ReadStore ();
				store.Add (item);
				WriteStore (store);
			} finally {
				ReleaseLock ();
			}
		}
		
		static List<RecentItem> ReadStore ()
		{
			List<RecentItem> result = new List<RecentItem> ();
			if (!File.Exists (RecentFileStorage.FileName))
				return result;
			XmlTextReader reader = new XmlTextReader (RecentFileStorage.FileName);
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
			} finally {
				if (reader != null)
					reader.Close ();
			}
			return result;
		}
		
		static void WriteStore (List<RecentItem> items)
		{
			items.Sort ();
			if (items.Count > MaxRecentItemsCount)
				items.RemoveRange (MaxRecentItemsCount, items.Count - MaxRecentItemsCount);
			XmlTextWriter writer = new XmlTextWriter (RecentFileStorage.FileName, System.Text.Encoding.UTF8);
			try {
				writer.Formatting = Formatting.Indented;
				writer.WriteStartDocument();
				writer.WriteStartElement ("RecentFiles");
				if (items != null) 
					foreach (RecentItem item in items)
						item.Write (writer);
				writer.WriteEndElement (); // RecentFiles
			} finally {
				writer.Close ();
			}
		}
		
		void OnRecentFilesChanged (EventArgs e)
		{
			if (this.RecentFilesChanged != null)
				this.RecentFilesChanged (this, e);
		}
		
		public event EventHandler RecentFilesChanged;
	}
}
