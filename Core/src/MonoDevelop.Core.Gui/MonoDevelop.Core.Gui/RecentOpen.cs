//  RecentOpen.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.IO;
using Vfs = Gnome.Vfs.Vfs;
using MimeType = Gnome.Vfs.MimeType;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Utils;
using Freedesktop.RecentFiles;

namespace MonoDevelop.Core.Gui
{
	/// <summary>
	/// This class handles the recent open files and the recent open project files of MonoDevelop
	/// </summary>
	public class RecentOpen
	{
		/// <summary>
		/// This variable is the maximal length of lastfile/lastopen entries
		/// must be > 0
		/// </summary>
		const int MAX_LENGTH = 10;
		
		static RecentFileStorage recentFiles = new RecentFileStorage ();
		
		public event EventHandler RecentFileChanged;
		public event EventHandler RecentProjectChanged;
		
		public RecentItem[] RecentFile {
			get {
				return recentFiles.GetItemsInGroup ("MonoDevelop Files");
			}
		}

		public RecentItem[] RecentProject {
			get {
				return recentFiles.GetItemsInGroup ("MonoDevelop Projects");
			}
		}
		
		void OnRecentFileChange()
		{
			if (RecentFileChanged != null) {
				RecentFileChanged(this, null);
			}
		}
		
		void OnRecentProjectChange()
		{
			if (RecentProjectChanged != null) {
				RecentProjectChanged(this, null);
			}
		}

		public RecentOpen()
		{
			recentFiles.RemoveMissingFiles ("MonoDevelop Files");
			recentFiles.RemoveMissingFiles ("MonoDevelop Projects");
			OnRecentFileChange();
			OnRecentProjectChange();
		}

		string ToUri (string fileName)
		{
			return "file://" + fileName;
		}
		
		public void AddLastFile (string name, string project)
		{
			RecentItem ri = new RecentItem (ToUri (name), MimeType.GetMimeTypeForUri (name), "MonoDevelop Files");
			if (project == null)
				ri.Private = Path.GetFileName (name);
			else
				ri.Private = String.Format ("{0} [{1}]", Path.GetFileName (name), project);

			recentFiles.AddWithLimit (ri, "MonoDevelop Files", MAX_LENGTH);
			OnRecentFileChange();
		}
		
		public void ClearRecentFiles()
		{
			recentFiles.ClearGroup ("MonoDevelop Files");
			OnRecentFileChange();
		}
		
		public void ClearRecentProjects()
		{
			recentFiles.ClearGroup ("MonoDevelop Projects");
			OnRecentProjectChange();
		}
		
		public void AddLastProject (string name, string projectName)
		{
			RecentItem ri = new RecentItem (ToUri (name), MimeType.GetMimeTypeForUri (name), "MonoDevelop Projects");
			ri.Private = projectName;
			recentFiles.AddWithLimit (ri, "MonoDevelop Projects", MAX_LENGTH);
			OnRecentProjectChange();
		}
		
		public void FileRemoved(object sender, FileEventArgs e)
		{
			if (e.IsDirectory)
				return;
			
			recentFiles.RemoveItem (ToUri (e.FileName));
			OnRecentFileChange();
		}
		
		public void FileRenamed (object sender, FileCopyEventArgs e)
		{
			if (e.IsDirectory)
				return;
			recentFiles.RenameItem (ToUri (e.SourceFile), ToUri (e.TargetFile));
			OnRecentFileChange();
		}


	}
}

