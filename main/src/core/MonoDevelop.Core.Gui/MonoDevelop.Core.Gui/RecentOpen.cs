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

namespace MonoDevelop.Core.Gui
{
	public class RecentOpen
	{
		static RecentFileStorage recentFiles = new RecentFileStorage ();
		
		const int ItemLimit = 10;
		
		#region Recent files
		const string fileGroup = "MonoDevelop Files";
		
		public IEnumerable<RecentItem> RecentFiles {
			get {
				return recentFiles.GetItemsInGroup (fileGroup);
			}
		}
		public int RecentFilesCount {
			get {
				return recentFiles.GetItemsInGroup (fileGroup).Length;
			}
		}
		public event EventHandler RecentFileChanged;
		protected virtual void OnRecentFileChange ()
		{
			if (RecentFileChanged != null) 
				RecentFileChanged (this, null);
		}
		
		public void ClearRecentFiles()
		{
			recentFiles.ClearGroup (fileGroup);
			OnRecentFileChange();
		}
		
		public void AddLastFile (string name, string project)
		{
			RecentItem recentItem = new RecentItem (RecentFileStorage.ToUri (name), DesktopService.GetMimeTypeForUri (name), fileGroup);
			recentItem.Private = project != null ? string.Format ("{0} [{1}]", Path.GetFileName (name), project) : Path.GetFileName (name);
			recentFiles.AddWithLimit (recentItem, fileGroup, ItemLimit);
			OnRecentFileChange();
		}
		#endregion
		
		#region Recent projects
		const string projectGroup = "MonoDevelop Projects";
		
		public IEnumerable<RecentItem> RecentProjects {
			get {
				return recentFiles.GetItemsInGroup (projectGroup);
			}
		}
		public int RecentProjectsCount {
			get {
				return recentFiles.GetItemsInGroup (projectGroup).Length;
			}
		}
		public event EventHandler RecentProjectChanged;
		protected virtual void OnRecentProjectChange ()
		{
			if (RecentProjectChanged != null) {
				RecentProjectChanged(this, null);
			}
		}
		public void ClearRecentProjects()
		{
			recentFiles.ClearGroup (projectGroup);
			OnRecentProjectChange();
		}
		public void AddLastProject (string name, string projectName)
		{
			RecentItem recentItem = new RecentItem (RecentFileStorage.ToUri (name), DesktopService.GetMimeTypeForUri (name), projectGroup);
			recentItem.Private = projectName;
			recentFiles.AddWithLimit (recentItem, projectGroup, ItemLimit);
			OnRecentProjectChange();
		}
		#endregion
		
		public RecentOpen ()
		{
			recentFiles.RemoveMissingFiles (projectGroup);
			OnRecentProjectChange ();
			
			recentFiles.RemoveMissingFiles (fileGroup);
			OnRecentFileChange ();
		}
		
		public void InformFileRemoved (object sender, FileEventArgs e)
		{
			if (!e.IsDirectory) {
				recentFiles.RemoveItem (RecentFileStorage.ToUri (e.FileName));
				OnRecentFileChange();
			}
		}
		
		public void InformFileRenamed (object sender, FileCopyEventArgs e)
		{
			if (!e.IsDirectory) {
				recentFiles.RenameItem (RecentFileStorage.ToUri (e.SourceFile), RecentFileStorage.ToUri (e.TargetFile));
				OnRecentFileChange();
			}
		}
	}
}

