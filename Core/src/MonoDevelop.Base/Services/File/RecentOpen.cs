// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.IO;
using Vfs = Gnome.Vfs.Vfs;
using MimeType = Gnome.Vfs.MimeType;

using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Utils;
using MonoDevelop.Services;
using Freedesktop.RecentFiles;

namespace MonoDevelop.Services
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
		
		RecentItem[] lastfile;
		RecentItem[] lastproject;
		RecentFiles recentFiles;
		
		public event EventHandler RecentFileChanged;
		public event EventHandler RecentProjectChanged;
		
		public RecentItem[] RecentFile {
			get {
				Debug.Assert(lastfile != null, "RecentOpen : set string[] LastFile (value == null)");
				return lastfile;
			}
		}

		public RecentItem[] RecentProject {
			get {
				Debug.Assert(lastproject != null, "RecentOpen : set string[] LastProject (value == null)");
				return lastproject;
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
			recentFiles = RecentFiles.GetInstance ();
			UpdateLastFile ();
			UpdateLastProject ();
		}
		
		public void AddLastFile (string name, string project)
		{
			RecentItem ri = new RecentItem (new Uri (name), MimeType.GetMimeTypeForUri (name), "MonoDevelop Files");
			if (project == null)
				ri.Private = Path.GetFileName (name);
			else
				ri.Private = String.Format ("{0} [{1}]", Path.GetFileName (name), project);

			recentFiles.AddWithLimit (ri, "MonoDevelop Files", MAX_LENGTH);
			UpdateLastFile ();
		}
		
		public void ClearRecentFiles()
		{
			lastfile = null;
			recentFiles.ClearGroup ("MonoDevelop Files");
			OnRecentFileChange();
		}
		
		public void ClearRecentProjects()
		{
			lastproject = null;
			recentFiles.ClearGroup ("MonoDevelop Projects");
			OnRecentProjectChange();
		}
		
		public void AddLastProject (string name, string projectName)
		{
			RecentItem ri = new RecentItem (new Uri (name), MimeType.GetMimeTypeForUri (name), "MonoDevelop Projects");
			ri.Private = projectName;
			recentFiles.AddWithLimit (ri, "MonoDevelop Projects", MAX_LENGTH);
			UpdateLastProject ();
		}
		
		public void FileRemoved(object sender, FileEventArgs e)
		{
			if (e.IsDirectory)
				return;
			
			recentFiles.RemoveItem (new Uri (e.FileName));
			UpdateLastFile ();
		}
		
		public void FileRenamed(object sender, FileEventArgs e)
		{
			if (e.IsDirectory)
				return;
			
			if (e.FileName == null)
				recentFiles.RenameItem (new Uri (e.SourceFile), new Uri (e.TargetFile));
			else
				recentFiles.RenameItem (new Uri (e.FileName), new Uri (e.TargetFile));
			UpdateLastFile ();
		}

		void UpdateLastFile ()
		{
			lastfile = recentFiles.GetItemsInGroup ("MonoDevelop Files");
			OnRecentFileChange();
		}

		void UpdateLastProject ()
		{
			lastproject = recentFiles.GetItemsInGroup ("MonoDevelop Projects");
			OnRecentProjectChange();
		}
	}
}

