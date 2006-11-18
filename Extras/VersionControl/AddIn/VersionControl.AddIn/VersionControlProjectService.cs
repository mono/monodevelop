using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;

using VersionControl.Service;

namespace VersionControl.AddIn
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
		static Gdk.Pixbuf overlay_normal;

		static Gdk.Pixbuf icon_modified;
		static Gdk.Pixbuf icon_removed;
		static Gdk.Pixbuf icon_conflicted;
		static Gdk.Pixbuf icon_added;
		internal static Gdk.Pixbuf icon_controled;
		
		static VersionControlProjectService ()
		{
			overlay_modified = Gdk.Pixbuf.LoadFromResource("overlay_modified.png");
			overlay_removed = Gdk.Pixbuf.LoadFromResource("overlay_removed.png");
			overlay_conflicted = Gdk.Pixbuf.LoadFromResource("overlay_conflicted.png");
			overlay_added = Gdk.Pixbuf.LoadFromResource("overlay_added.png");
			overlay_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
			overlay_unversioned = Gdk.Pixbuf.LoadFromResource("overlay_unversioned.png");
			overlay_protected = Gdk.Pixbuf.LoadFromResource("overlay_locked.png");
			overlay_normal = Gdk.Pixbuf.LoadFromResource("overlay_normal.png");
			
			icon_modified = MonoDevelop.Core.Gui.Services.Resources.GetIcon ("gtk-edit", Gtk.IconSize.Menu);
			icon_removed = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.Remove, Gtk.IconSize.Menu);
			icon_conflicted = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu);
			icon_added = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.Add, Gtk.IconSize.Menu);
			icon_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
			
			IdeApp.ProjectOperations.FileChangedInProject += OnFileChanged;
			IdeApp.ProjectOperations.FileAddedToProject += OnFileAdded;
			IdeApp.ProjectOperations.FileRemovedFromProject += OnFileRemoved;
			IdeApp.ProjectOperations.FileRenamedInProject += OnFileRenamed;
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
		
		public static Repository GetRepository (CombineEntry entry, string path)
		{
			Repository repo = (Repository) entry.ExtendedProperties [typeof(Repository)];
			if (repo != null)
				return repo;
			
			repo = VersionControlService.GetRepositoryReference (entry.BaseDirectory, entry.Name);
			entry.ExtendedProperties [typeof(Repository)] = repo;
			
			return repo;
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
			return true;
		}
		
		internal static void NotifyFileStatusChanged (Repository repo, string localPath, bool isDirectory) 
		{
			if (FileStatusChanged != null)
				FileStatusChanged (null, new FileUpdateEventArgs (repo, localPath, isDirectory));
		}
		
		static void OnFileChanged (object s, ProjectFileEventArgs args)
		{
			Repository repo = GetRepository (args.Project, args.ProjectFile.FilePath);
			if (repo != null)
				NotifyFileStatusChanged (repo, args.ProjectFile.FilePath, false);
		}
		
		static void OnFileAdded (object s, ProjectFileEventArgs args)
		{
			string path = args.ProjectFile.FilePath;
			Repository repo = GetRepository (args.Project, path);
			if (repo != null && !repo.IsVersioned (path) && repo.CanAdd (path)) {
				using (IProgressMonitor monitor = GetStatusMonitor ()) {
					repo.Add (path, false, monitor);
				}
				NotifyFileStatusChanged (repo, path, args.ProjectFile.Subtype == Subtype.Directory);
			}
		}
		
		static void OnFileRemoved (object s, ProjectFileEventArgs args)
		{
			string path = args.ProjectFile.FilePath;
			Repository repo = GetRepository (args.Project, path);
			if (repo != null && repo.IsVersioned (path) && repo.CanRemove (path)) {
				using (IProgressMonitor monitor = GetStatusMonitor ()) {
					repo.Delete (path, true, monitor);
				}
				NotifyFileStatusChanged (repo, path, args.ProjectFile.Subtype == Subtype.Directory);
			}
		}
		
		static void OnFileRenamed (object s, ProjectFileRenamedEventArgs args)
		{
/*			string path = args.ProjectFile.FilePath;
			Repository repo = GetRepository (args.Project, path);
			if (repo.IsVersioned (path) && repo.CanRemove (path)) {
				repo.Remove (path);
				NotifyFileStatusChanged (repo, path, args.ProjectFile.Subtype == Subtype.Directory);
			}
*/		}

		static IProgressMonitor GetStatusMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Updating version control repository"), "vc-remote-status", true);
		}
		
		public static event FileUpdateEventHandler FileStatusChanged;
		public static event CommitEventHandler PrepareCommit;
		public static event CommitEventHandler BeginCommit;
		public static event CommitEventHandler EndCommit;
	}
}
