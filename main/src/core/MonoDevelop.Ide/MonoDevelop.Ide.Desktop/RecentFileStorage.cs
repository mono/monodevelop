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
using MonoDevelop.Core;

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
		List<RecentItem> cachedItemList;
		
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
			OnRecentFilesChanged (null);
		}
		
		void HandleWatcherRenamed (object sender, RenamedEventArgs e)
		{
			OnRecentFilesChanged (null);
		}
		
		public bool RemoveItem (string uri)
		{
			return ModifyStore (list => RemoveMatches (list, item => item.Uri != null && item.Uri.Equals (uri)));
		}
		
		public bool RemoveItem (RecentItem item)
		{
			return item != null && RemoveItem (item.Uri);
		}
		
		public bool RenameItem (string oldUri, string newUri)
		{
			if (oldUri == null || newUri == null)
				return false;
			
			return ModifyStore (list => {
				bool modified = false;
				foreach (var item in list) {
					if (item.Uri == oldUri) {
						string oldName = Path.GetFileName (item.LocalPath);
						item.Uri = newUri;
						if (item.Private.Contains (oldName)) {
							item.Private = item.Private.Replace (oldName, Path.GetFileName (item.LocalPath));
						}
						item.NewTimeStamp ();
						modified = true;
					}
				}
				return modified;
			});
		}
		
		public RecentItem[] GetItemsInGroup (string group)
		{
			//don't create the file since we're just reading
			if (!File.Exists (filePath)) {
				 return new RecentItem[0];
			}
			
			var c = cachedItemList;
			if (c == null) {
				using (var fs = AcquireFileExclusive (filePath)) {
					c = cachedItemList = ReadStore (fs);
				}
			}
			c.Sort ();
			return c.Where (item => item.IsInGroup (group)).ToArray ();
		}
		
		public void RemoveMissingFiles (params string[] groups)
		{
			//don't create the file since we're just reading
			if (!File.Exists (filePath)) {
				return;
			}
			
			ModifyStore (list => RemoveMatches (list, item =>
				item.IsFile && groups.Any (g => item.IsInGroup (g)) && !File.Exists (item.LocalPath)
			));
		}
		
		public void ClearGroup (params string[] groups)
		{
			ModifyStore (list => RemoveMatches (list, item => groups.Any (group => item.IsInGroup (group))));
		}
		
		public void AddWithLimit (RecentItem item, string group, int limit)
		{
			ModifyStore (list => {
				RemoveMatches (list, i => i.Uri == item.Uri);
				list.Add (item);
				CheckLimit (list, group, limit);
				return true;
			});
		}
		
		static bool CheckLimit (List<RecentItem> list, string group, int limit)
		{
			list.Sort ();
			bool modified = false;
			int count = 0;
			for (int i = 0; i < list.Count; i++) {
				if (list[i].IsInGroup (group) && (++count > limit)) {
					list.RemoveAt (i);
					i--;
				}
			}
			return modified;
		}
		
		static bool RemoveMatches<T> (List<T> list, Func<T,bool> predicate)
		{
			bool modified = false;
			for (int i = list.Count - 1; i >= 0; i--) {
				if (predicate (list[i])) {
					list.RemoveAt (i);
					modified = true;
				}
			}
			return modified;
		}
		
		bool ModifyStore (Func<List<RecentItem>,bool> modify)
		{
			List<RecentItem> list;
			using (var fs = AcquireFileExclusive (filePath)) {
				list = ReadStore (fs);
				if (!modify (list)) {
					return false;
				}
				fs.Position = 0;
				fs.SetLength (0);
				WriteStore (fs, list);
			}
			//TODO: can we suppress duplicate event from the FSW?
			OnRecentFilesChanged (list);
			return true;
		}
		
		static List<RecentItem> ReadStore (FileStream file)
		{
			var result = new List<RecentItem> ();
			if (file.Length == 0) {
				return result;
			}

			try {
				using (var reader = XmlReader.Create (file, new XmlReaderSettings { CloseInput = false })) {
					while (reader.Read ()) {
						if (reader.IsStartElement () && reader.LocalName == RecentItem.Node) {
							result.Add (RecentItem.Read (reader));
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading recent file store.", e);
			}
			
			return result;
		}
		
		static Encoding utf8WithoutByteOrderMark = new UTF8Encoding (false);
		
		static void WriteStore (FileStream stream, List<RecentItem> items)
		{
			items.Sort ();
			if (items.Count > MaxRecentItemsCount)
				items.RemoveRange (MaxRecentItemsCount, items.Count - MaxRecentItemsCount);
			using (var writer = new XmlTextWriter (stream, utf8WithoutByteOrderMark)) {
				writer.Formatting = Formatting.Indented;
				writer.WriteStartDocument ();
				writer.WriteStartElement ("RecentFiles");
				if (items != null) 
					foreach (RecentItem item in items)
						item.Write (writer);
				writer.WriteEndElement (); // RecentFiles
			}
		}
		
		//FIXME: should we P/Invoke lockf on POSIX or is Mono's FileShare.None sufficient?
		static FileStream AcquireFileExclusive (string filePath)
		{
			const int MAX_WAIT_TIME = 1000;
			const int RETRY_WAIT = 50;
			
			int remainingTries = MAX_WAIT_TIME / RETRY_WAIT;
			while (true) {
				try {
					Directory.CreateDirectory (Path.GetDirectoryName (filePath));
					return File.Open (filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				} catch (Exception ex) {
					//FIXME: will it work on Mono if we check that it's an access conflict, i.e. HResult is 0x80070020?
					if (ex is IOException && remainingTries > 0) {
						Thread.Sleep (RETRY_WAIT);
						remainingTries--;
						continue;
					}
					throw;
				}
			}
		}
		
		public static string ToUri (string fileName)
		{
			return fileName.StartsWith ("file://") ? fileName : "file://" + fileName;
		}
		
		void OnRecentFilesChanged (List<RecentItem> cachedItemList)
		{
			this.cachedItemList = cachedItemList;
			if (changed != null) {
				changed (this, EventArgs.Empty);
			}
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