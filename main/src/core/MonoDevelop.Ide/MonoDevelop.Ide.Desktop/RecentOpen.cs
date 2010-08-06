//
// RecentOpen.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.Ide.Desktop
{
	public class FdoRecentFiles : RecentFiles, IDisposable
	{
		RecentFileStorage recentFiles;
		
		const string projGroup = "MonoDevelop Projects";
		const string fileGroup = "MonoDevelop Files";
		
		const int ItemLimit = 10;
		
		public FdoRecentFiles () : this (RecentFileStorage.DefaultPath)
		{
		}
		
		public FdoRecentFiles (string storageFile)
		{
			recentFiles = new RecentFileStorage (storageFile);
			recentFiles.RemoveMissingFiles (projGroup, fileGroup);
		}
		
		public override event EventHandler Changed {
			add { recentFiles.RecentFilesChanged += value; }
			remove { recentFiles.RecentFilesChanged -= value; }
		}
		
		public override IList<RecentFile> GetProjects ()
		{
			return Get (projGroup);
		}
		
		public override IList<RecentFile> GetFiles ()
		{
			return Get (fileGroup);
		}
		
		IList<RecentFile> Get (string grp)
		{
			var gp = recentFiles.GetItemsInGroup (grp);
			return gp.Select (i => new RecentFile (i.LocalPath, i.Private, i.Timestamp)).ToList ();
		}
		
		public override void ClearProjects ()
		{
			recentFiles.ClearGroup (projGroup);
		}
		
		public override void ClearFiles ()
		{
			recentFiles.ClearGroup (fileGroup);
		}
		
		public override void AddFile (string fileName, string displayName)
		{
			Add (fileGroup, fileName, displayName);
		}
		
		public override void AddProject (string fileName, string displayName)
		{
			Add (projGroup, fileName, displayName);
		}
		
		void Add (string grp, string fileName, string displayName)
		{
			var mime = DesktopService.GetMimeTypeForUri (fileName);
			var uri = RecentFileStorage.ToUri (fileName);
			var recentItem = new RecentItem (uri, mime, grp) { Private = displayName };
			recentFiles.AddWithLimit (recentItem, grp, ItemLimit);
		}
		
		public override void NotifyFileRemoved (string fileName)
		{
			recentFiles.RemoveItem (RecentFileStorage.ToUri (fileName));
		}
		
		public override void NotifyFileRenamed (string oldName, string newName)
		{
			recentFiles.RenameItem (RecentFileStorage.ToUri (oldName), RecentFileStorage.ToUri (newName));
		}
		
		public void Dispose ()
		{
			recentFiles.Dispose ();
			recentFiles = null;
		}
	}
		
	public abstract class RecentFiles
	{
		public abstract IList<RecentFile> GetFiles ();
		public abstract IList<RecentFile> GetProjects ();
		public abstract event EventHandler Changed;
		public abstract void ClearProjects ();
		public abstract void ClearFiles ();
		public abstract void AddFile (string fileName, string displayName);
		public abstract void AddProject (string fileName, string displayName);
		public abstract void NotifyFileRemoved (string filename);
		public abstract void NotifyFileRenamed (string oldName, string newName);
		
		public void AddFile (string fileName, MonoDevelop.Projects.Project project)
		{
			var projectName = project != null? project.Name : null;
			var displayName = projectName != null?
				string.Format ("{0} [{1}]", Path.GetFileName (fileName), projectName) 
				: Path.GetFileName (fileName);
			AddFile (fileName, displayName);
		}
	}
	
	public class RecentFile
	{
		string displayName, fileName;
		DateTime timestamp;
		
		public RecentFile (string fileName, string displayName, DateTime timestamp)
		{
			this.fileName = fileName;
			this.displayName = displayName;
			this.timestamp = timestamp;
		}

		public string FileName { get { return fileName; } }
		public string DisplayName {
			get {
				return string.IsNullOrEmpty (displayName)? Path.GetFileName (fileName) : displayName;
			}
		}
		
		public DateTime TimeStamp { get { return timestamp; } }
		
		public override string ToString ()
		{
			return FileName;
		}
	}
}

