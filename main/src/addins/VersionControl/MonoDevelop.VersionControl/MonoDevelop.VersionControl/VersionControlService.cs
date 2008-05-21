
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using Mono.Addins;

namespace MonoDevelop.VersionControl
{
	public class VersionControlService
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
		static object commentsLock = new object ();
		static DateTime nextSave = DateTime.MinValue;
		
		static List<VersionControlSystem> handlers = new List<VersionControlSystem> ();
		static VersionControlConfiguration configuration;
		static DataContext dataContext = new DataContext ();
		
		public static event FileUpdateEventHandler FileStatusChanged;
		public static event CommitEventHandler PrepareCommit;
		public static event CommitEventHandler BeginCommit;
		public static event CommitEventHandler EndCommit;
		
		static VersionControlService ()
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
			
			IdeApp.Workspace.FileAddedToProject += OnFileAdded;
			//IdeApp.Workspace.FileChangedInProject += OnFileChanged;
			//IdeApp.Workspace.FileRemovedFromProject += OnFileRemoved;
			//IdeApp.Workspace.FileRenamedInProject += OnFileRenamed;
			
			IdeApp.Workspace.ItemAddedToSolution += OnEntryAdded;
			IdeApp.Exiting += delegate {
				DelayedSaveComments (null);
			};
			
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/VersionControl/VersionControlSystems", OnExtensionChanged);
		}
		
		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			VersionControlSystem vcs = (VersionControlSystem) args.ExtensionObject;
			if (args.Change == ExtensionChange.Add) {
				handlers.Add (vcs);
				// Include the repository type in the serialization context, so repositories
				// of this type can be deserialized from the configuration file.
				dataContext.IncludeType (vcs.CreateRepositoryInstance ().GetType ());
			}
			else {
				handlers.Remove (vcs);
			}
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
		
		public static Repository GetRepository (IWorkspaceObject entry)
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
			lock (commentsLock) {
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
		}
		
		internal static string GetCommitComment (string file)
		{
			lock (commentsLock) {
				Hashtable doc = GetCommitComments ();
				CommitComment cm = doc [file] as CommitComment;
				if (cm != null) {
					cm.Date = DateTime.Now;
					return cm.Comment;
				}
				else
					return null;
			}
		}
		
		static Hashtable GetCommitComments ()
		{
			if (comments != null)
				return comments;
			
			string file = Path.Combine (PropertyService.ConfigPath, "version-control-commit-msg");
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
					LoggingService.LogError (ex.ToString ());
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
			lock (commentsLock) {
				if (comments == null)
					return;
				if (nextSave == DateTime.MinValue)
					ThreadPool.QueueUserWorkItem (DelayedSaveComments);
				nextSave = DateTime.Now.AddSeconds (3);
			}
		}
		
		static void DelayedSaveComments (object ob)
		{
			lock (commentsLock)
			{
				if (comments == null || nextSave == DateTime.MinValue)
					return;
				
				DateTime tim = DateTime.Now;
				while (tim < nextSave) {
					Monitor.Wait (commentsLock, nextSave - tim);
					tim = DateTime.Now;
				}
				nextSave = DateTime.MinValue;
				
				FileStream stream = null;
				try {
					string file = Path.Combine (PropertyService.ConfigPath, "version-control-commit-msg");
					if (comments.Count == 0) {
						if (File.Exists (file))
							FileService.DeleteFile (file);
						return;
					}
				
					if (!Directory.Exists (PropertyService.ConfigPath))
						Directory.CreateDirectory (PropertyService.ConfigPath);
					stream = new FileStream (file, FileMode.Create, FileAccess.Write);
					BinaryFormatter formatter = new BinaryFormatter ();
					formatter.Serialize (stream, comments);
				} catch (Exception ex) {
					// If there is an error, just discard the file
					LoggingService.LogError (ex.ToString ());
				} finally {
					if (stream != null)
						stream.Close ();
				}
			}
		}
		
		internal static bool NotifyPrepareCommit (Repository repo, ChangeSet changeSet)
		{
			if (PrepareCommit != null) {
				try {
					PrepareCommit (null, new CommitEventArgs (repo, changeSet, false));
				} catch (Exception ex) {
					MessageService.ShowException (ex);
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
					MessageService.ShowException (ex);
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
					MessageService.ShowException (ex);
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
			if (!DispatchService.IsGuiThread)
				Gtk.Application.Invoke (delegate {
					NotifyFileStatusChanged (repo, localPath, isDirectory);
				});
			else {
				if (FileStatusChanged != null)
					FileStatusChanged (null, new FileUpdateEventArgs (repo, localPath, isDirectory));
			}
		}
		
		//static void OnFileChanged (object s, ProjectFileEventArgs args)
		//{
		//	Repository repo = GetRepository (args.Project);
		//	if (repo != null)
		//		NotifyFileStatusChanged (repo, args.ProjectFile.FilePath, false);
		//}

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
		static void SolutionItemAddFiles (SolutionItem entry, ArrayList files)
		{
			if (entry is SolutionEntityItem) {
				string file = ((SolutionEntityItem)entry).FileName;
				if (!File.Exists (file))
					return;
				files.Add (file);
			}
			
			if (entry is Project) {
				foreach (ProjectFile file in ((Project) entry).Files) {
					if (file.Subtype != Subtype.Directory)
						files.Add (file.FilePath);
				}
			} else if (entry is SolutionFolder) {
				foreach (SolutionItem ent in ((SolutionFolder) entry).Items)
					SolutionItemAddFiles (ent, files);
			}
		}
		
		static void OnEntryAdded (object o, SolutionItemEventArgs args)
		{
			// handles addition of solutions and projects
			SolutionItem parent = (SolutionItem) args.SolutionItem.ParentFolder;
			
			if (parent == null)
				return;
			
			Repository repo = GetRepository (parent);
			
			if (repo == null)
				return;
			
			SolutionItem entry = args.SolutionItem;
			string path = entry.BaseDirectory;
			
			if (!repo.CanAdd (path))
				return;
			
			// While we /could/ call repo.Add with `recursive = true', we don't
			// necessarily want to add files under the project/solution directory
			// that may not be a part of this project/solution.
			
			ArrayList files = new ArrayList ();
			
			files.Add (path);
			SolutionItemAddFiles (entry, files);
			
			using (IProgressMonitor monitor = GetStatusMonitor ()) {
				string[] paths = (string[]) files.ToArray (typeof (string));
				
				for (int i = 0; i < paths.Length; i++)
					repo.Add (paths[i], false, monitor);
			}
			
			NotifyFileStatusChanged (repo, parent.BaseDirectory, true);
		}
		
		static IProgressMonitor GetStatusMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Updating version control repository"), "vc-remote-status", true);
		}
		
		static string ConfigFile {
			get {
				return Path.Combine (PropertyService.ConfigPath, "VersionControl.config");
			}
		}
		
		static public IEnumerable<VersionControlSystem> GetVersionControlSystems ()
		{
			foreach (VersionControlSystem vs in handlers)
				if (vs.IsInstalled)
					yield return vs;
		}
		
		public static void AddRepository (Repository repo)
		{
			GetConfiguration ().Repositories.Add (repo);
		}
		
		public static void RemoveRepository (Repository repo)
		{
			GetConfiguration ().Repositories.Remove (repo);
		}
		
		static public IEnumerable<Repository> GetRepositories ()
		{
			return GetConfiguration ().Repositories;
		}
		
		static VersionControlConfiguration GetConfiguration ()
		{
			if (configuration == null) {
				if (File.Exists (ConfigFile)) {
					XmlDataSerializer ser = new XmlDataSerializer (dataContext);
					XmlTextReader reader = new XmlTextReader (new StreamReader (ConfigFile));
					try {
						configuration = (VersionControlConfiguration) ser.Deserialize (reader, typeof(VersionControlConfiguration));
					} finally {
						reader.Close ();
					}
				}
				if (configuration == null)
					configuration = new VersionControlConfiguration ();
			}
			return configuration;
		}
		
		public static void SaveConfiguration ()
		{
			if (configuration != null) {
				XmlDataSerializer ser = new XmlDataSerializer (dataContext);
				XmlTextWriter tw = new XmlTextWriter (new StreamWriter (ConfigFile));
				try {
					ser.Serialize (tw, configuration, typeof(VersionControlConfiguration));
				} finally {
					tw.Close ();
				}
			}
		}
		
		public static void ResetConfiguration ()
		{
			configuration = null;
		}
		
		public static Repository GetRepositoryReference (string path, string id)
		{
			foreach (VersionControlSystem vcs in GetVersionControlSystems ()) {
				Repository repo = vcs.GetRepositoryReference (path, id);
				if (repo != null)
					return repo;
			}
			return null;
		}
		
		public static void StoreRepositoryReference (Repository repo, string path, string id)
		{
			repo.VersionControlSystem.StoreRepositoryReference (repo, path, id);
		}
		
		public static bool CheckVersionControlInstalled ()
		{
			foreach (VersionControlSystem vcs in GetVersionControlSystems ()) {
				if (vcs.IsInstalled)
					return true;
			}
			
			MessageService.ShowError (GettextCatalog.GetString ("There isn't any supported version control system installed. You may need to install additional add-ins or packages."));
			return false;
		}
		
		internal static Repository InternalGetRepositoryReference (string path, string id)
		{
			string file = Path.Combine (path, id) + ".mdvcs";
			if (!File.Exists (file))
				return null;
			
			XmlDataSerializer ser = new XmlDataSerializer (dataContext);
			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
			try {
				return (Repository) ser.Deserialize (reader, typeof(Repository));
			} finally {
				reader.Close ();
			}
		}
		
		internal static void InternalStoreRepositoryReference (Repository repo, string path, string id)
		{
			string file = Path.Combine (path, id) + ".mdvcs";
			
			XmlDataSerializer ser = new XmlDataSerializer (dataContext);
			XmlTextWriter tw = new XmlTextWriter (new StreamWriter (file));
			try {
				ser.Serialize (tw, repo, typeof(Repository));
			} finally {
				tw.Close ();
			}
		}
	}
	
	[Serializable]
	class CommitComment
	{
		public string Comment;
		public DateTime Date;
	}
}
