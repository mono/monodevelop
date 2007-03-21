using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.VersionControl
{
	public class VersionControlProjectService
	{
		static Gdk.Pixbuf overlay_modified;
		static Gdk.Pixbuf overlay_removed;
		static Gdk.Pixbuf overlay_conflicted;
		static Gdk.Pixbuf overlay_added;
		internal static Gdk.Pixbuf overlay_controled;
		static Gdk.Pixbuf overlay_unversioned;
		static Gdk.Pixbuf overlay_protected;
//		static Gdk.Pixbuf overlay_normal;

		static Gdk.Pixbuf icon_modified;
		static Gdk.Pixbuf icon_removed;
		static Gdk.Pixbuf icon_conflicted;
		static Gdk.Pixbuf icon_added;
		internal static Gdk.Pixbuf icon_controled;
		
		static Hashtable comments;
		
		static VersionControlProjectService ()
		{
			overlay_modified = Gdk.Pixbuf.LoadFromResource("overlay_modified.png");
			overlay_removed = Gdk.Pixbuf.LoadFromResource("overlay_removed.png");
			overlay_conflicted = Gdk.Pixbuf.LoadFromResource("overlay_conflicted.png");
			overlay_added = Gdk.Pixbuf.LoadFromResource("overlay_added.png");
			overlay_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
			overlay_unversioned = Gdk.Pixbuf.LoadFromResource("overlay_unversioned.png");
			overlay_protected = Gdk.Pixbuf.LoadFromResource("overlay_locked.png");
//			overlay_normal = Gdk.Pixbuf.LoadFromResource("overlay_normal.png");
			
			icon_modified = MonoDevelop.Core.Gui.Services.Resources.GetIcon ("gtk-edit", Gtk.IconSize.Menu);
			icon_removed = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.Remove, Gtk.IconSize.Menu);
			icon_conflicted = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu);
			icon_added = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.Add, Gtk.IconSize.Menu);
			icon_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
			
			IdeApp.ProjectOperations.FileAddedToProject += OnFileAdded;
			//IdeApp.ProjectOperations.FileChangedInProject += OnFileChanged;
			//IdeApp.ProjectOperations.FileRemovedFromProject += OnFileRemoved;
			//IdeApp.ProjectOperations.FileRenamedInProject += OnFileRenamed;
			
			IdeApp.ProjectOperations.EntryAddedToCombine += OnEntryAdded;
		}
		
		public static Gdk.Pixbuf LoadOverlayIconForStatus(VersionStatus status)
		{
			switch (status) {
				case VersionStatus.Unchanged:
					return null;
				case VersionStatus.Protected:
					return overlay_protected;
				case VersionStatus.Modified:
					return overlay_modified;
				case VersionStatus.Conflicted:
					return overlay_conflicted;
				case VersionStatus.ScheduledAdd:
					return overlay_added;
				case VersionStatus.ScheduledDelete:
					return overlay_removed;
				case VersionStatus.UnversionedIgnored:
				case VersionStatus.Unversioned:
					return overlay_unversioned;
			}
			return null;
		}
		
		public static Gdk.Pixbuf LoadIconForStatus(VersionStatus status)
		{
			switch (status) {
				case VersionStatus.Unchanged:
					return null;
				case VersionStatus.Modified:
					return icon_modified;
				case VersionStatus.Conflicted:
					return icon_conflicted;
				case VersionStatus.ScheduledAdd:
					return icon_added;
				case VersionStatus.ScheduledDelete:
					return icon_removed;
				default:
					if (status != VersionStatus.Unversioned && status != VersionStatus.UnversionedIgnored)
						return icon_controled;
					break;
			}
			return null;
		}

		public static string GetStatusLabel (VersionStatus status)
		{
			switch (status) {
				case VersionStatus.Unchanged:
					return "";
				case VersionStatus.Modified:
					return GettextCatalog.GetString ("Modified");
				case VersionStatus.Conflicted:
					return GettextCatalog.GetString ("Conflict");
				case VersionStatus.ScheduledAdd:
					return GettextCatalog.GetString ("Add");
				case VersionStatus.ScheduledDelete:
					return GettextCatalog.GetString ("Delete");
				default:
					if (status != VersionStatus.Unversioned && status != VersionStatus.UnversionedIgnored)
						return "";
					break;
			}
			return GettextCatalog.GetString ("Unversioned");
		}
		
		public static Repository GetRepository (CombineEntry entry)
		{
			Repository repo = (Repository) entry.ExtendedProperties [typeof(Repository)];
			if (repo != null)
				return repo;
			
			repo = VersionControlService.GetRepositoryReference (entry.BaseDirectory, entry.Name);
			entry.ExtendedProperties [typeof(Repository)] = repo;
			
			return repo;
		}
		
		internal static void SetCommitComment (string file, string comment, bool save)
		{
			Hashtable doc = GetCommitComments ();
			if (comment == null || comment.Length == 0) {
				if (doc.ContainsKey (file)) {
					doc.Remove (file);
					if (save) SaveComments ();
				}
			} else {
				CommitComment cm = new CommitComment ();
				cm.Comment = comment;
				cm.Date = DateTime.Now;
				doc [file] = cm;
				if (save) SaveComments ();
			}
		}
		
		internal static string GetCommitComment (string file)
		{
			Hashtable doc = GetCommitComments ();
			CommitComment cm = doc [file] as CommitComment;
			if (cm != null) {
				cm.Date = DateTime.Now;
				return cm.Comment;
			}
			else
				return null;
		}
		
		static Hashtable GetCommitComments ()
		{
			if (comments != null)
				return comments;
			
			string file = Path.Combine (Runtime.Properties.ConfigDirectory, "version-control-commit-msg");
			if (File.Exists (file)) {
				FileStream stream = null;
				try {
					stream = File.OpenRead (file);
					BinaryFormatter formatter = new BinaryFormatter ();
					comments = (Hashtable) formatter.Deserialize (stream);
				
					// Remove comments for files that don't exists
					// Remove comments more than 60 days old
					
					ArrayList toDelete = new ArrayList ();
					foreach (DictionaryEntry e in comments) {
						if (!File.Exists ((string)e.Key))
							toDelete.Add (e.Key);
						if ((DateTime.Now - ((CommitComment)e.Value).Date).TotalDays > 60)
							toDelete.Add (e.Key);
					}
					foreach (string f in toDelete)
						comments.Remove (f);
						
				} catch (Exception ex) {
					// If there is an error, just discard the file
					Runtime.LoggingService.Error (ex);
					comments = new Hashtable ();
				} finally {
					if (stream != null)
						stream.Close ();
				}
			} else {
				comments = new Hashtable ();
			}
			return comments;
		}
		
		static void SaveComments ()
		{
			if (comments == null)
				return;
				
			FileStream stream = null;
			try {
				string file = Path.Combine (Runtime.Properties.ConfigDirectory, "version-control-commit-msg");
				if (comments.Count == 0) {
					if (File.Exists (file))
						Runtime.FileService.DeleteFile (file);
					return;
				}
			
				if (!Directory.Exists (Runtime.Properties.ConfigDirectory))
					Directory.CreateDirectory (Runtime.Properties.ConfigDirectory);
				stream = new FileStream (file, FileMode.Create, FileAccess.Write);
				BinaryFormatter formatter = new BinaryFormatter ();
				formatter.Serialize (stream, comments);
			} catch (Exception ex) {
				// If there is an error, just discard the file
				Runtime.LoggingService.Error (ex);
			} finally {
				if (stream != null)
					stream.Close ();
			}
		}
		
		internal static bool NotifyPrepareCommit (Repository repo, ChangeSet changeSet)
		{
			if (PrepareCommit != null) {
				try {
					PrepareCommit (null, new CommitEventArgs (repo, changeSet, false));
				} catch (Exception ex) {
					IdeApp.Services.MessageService.ShowError (ex);
					return false;
				}
			}
			return true;
		}
		
		internal static bool NotifyBeforeCommit (Repository repo, ChangeSet changeSet)
		{
			if (BeginCommit != null) {
				try {
					BeginCommit (null, new CommitEventArgs (repo, changeSet, false));
				} catch (Exception ex) {
					IdeApp.Services.MessageService.ShowError (ex);
					return false;
				}
			}
			return true;
		}
		
		internal static bool NotifyAfterCommit (Repository repo, ChangeSet changeSet, bool success)
		{
			if (EndCommit != null) {
				try {
					EndCommit (null, new CommitEventArgs (repo, changeSet, success));
				} catch (Exception ex) {
					IdeApp.Services.MessageService.ShowError (ex);
					return false;
				}
			}
			if (success) {
				foreach (ChangeSetItem it in changeSet.Items)
					SetCommitComment (it.LocalPath, null, false);
				SaveComments ();
			}
			return true;
		}
		
		internal static void NotifyFileStatusChanged (Repository repo, string localPath, bool isDirectory) 
		{
			if (FileStatusChanged != null)
				FileStatusChanged (null, new FileUpdateEventArgs (repo, localPath, isDirectory));
		}
		
/*		static void OnFileChanged (object s, ProjectFileEventArgs args)
		{
			Repository repo = GetRepository (args.Project);
			if (repo != null)
				NotifyFileStatusChanged (repo, args.ProjectFile.FilePath, false);
		}
*/
		static void OnFileAdded (object s, ProjectFileEventArgs args)
		{
			string path = args.ProjectFile.FilePath;
			Repository repo = GetRepository (args.Project);
			if (repo != null && repo.CanAdd (path)) {
				using (IProgressMonitor monitor = GetStatusMonitor ()) {
					repo.Add (path, false, monitor);
				}
				NotifyFileStatusChanged (repo, path, args.ProjectFile.Subtype == Subtype.Directory);
			}
		}
		
/*		static void OnFileRemoved (object s, ProjectFileEventArgs args)
		{
			string path = args.ProjectFile.FilePath;
			Repository repo = GetRepository (args.Project);
			if (repo != null && repo.IsVersioned (path) && repo.CanRemove (path)) {
				using (IProgressMonitor monitor = GetStatusMonitor ()) {
					repo.DeleteFile (path, true, monitor);
				}
				NotifyFileStatusChanged (repo, path, args.ProjectFile.Subtype == Subtype.Directory);
			}
		}

		static void OnFileRenamed (object s, ProjectFileRenamedEventArgs args)
		{
			string path = args.ProjectFile.FilePath;
			Repository repo = GetRepository (args.Project);
			if (repo.IsVersioned (path) && repo.CanRemove (path)) {
				repo.Remove (path);
				NotifyFileStatusChanged (repo, path, args.ProjectFile.Subtype == Subtype.Directory);
			}
		}
*/
		static void OnEntryAdded (object o, CombineEntryEventArgs args)
		{
			// handles addition of solutions and projects
			CombineEntry parent = (CombineEntry) args.CombineEntry.ParentCombine;
			
			if (parent == null)
				return;
			
			Repository repo = GetRepository (parent);
			
			if (repo == null)
				return;
			
			string path = args.CombineEntry.BaseDirectory;
			
			if (!repo.CanAdd (path))
				return;
			
			using (IProgressMonitor monitor = GetStatusMonitor ()) {
				repo.Add (path, true, monitor);
			}
			
			NotifyFileStatusChanged (repo, parent.BaseDirectory, true);
		}
		
		static IProgressMonitor GetStatusMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Updating version control repository"), "vc-remote-status", true);
		}
		
		public static event FileUpdateEventHandler FileStatusChanged;
		public static event CommitEventHandler PrepareCommit;
		public static event CommitEventHandler BeginCommit;
		public static event CommitEventHandler EndCommit;
	}
	
	[Serializable]
	class CommitComment
	{
		public string Comment;
		public DateTime Date;
	}
}
