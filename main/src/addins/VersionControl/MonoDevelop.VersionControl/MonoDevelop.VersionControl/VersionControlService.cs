
using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using MonoDevelop.Ide;
using MonoDevelop.Core.ProgressMonitoring;

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
		static Gdk.Pixbuf overlay_locked;
		static Gdk.Pixbuf overlay_unlocked;
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
			try {
				overlay_modified = Gdk.Pixbuf.LoadFromResource("overlay_modified.png");
				overlay_removed = Gdk.Pixbuf.LoadFromResource("overlay_removed.png");
				overlay_conflicted = Gdk.Pixbuf.LoadFromResource("overlay_conflicted.png");
				overlay_added = Gdk.Pixbuf.LoadFromResource("overlay_added.png");
				overlay_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
				overlay_unversioned = Gdk.Pixbuf.LoadFromResource("overlay_unversioned.png");
				overlay_protected = Gdk.Pixbuf.LoadFromResource("overlay_lock_required.png");
				overlay_unlocked = Gdk.Pixbuf.LoadFromResource("overlay_unlocked.png");
				overlay_locked = Gdk.Pixbuf.LoadFromResource("overlay_locked.png");
	//			overlay_normal = Gdk.Pixbuf.LoadFromResource("overlay_normal.png");
			
				icon_modified = ImageService.GetPixbuf ("gtk-edit", Gtk.IconSize.Menu);
				icon_removed = ImageService.GetPixbuf (Gtk.Stock.Remove, Gtk.IconSize.Menu);
				icon_conflicted = ImageService.GetPixbuf (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu);
				icon_added = ImageService.GetPixbuf (Gtk.Stock.Add, Gtk.IconSize.Menu);
				icon_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
			} catch (Exception e) {
				LoggingService.LogError ("Error while loading icons.", e);
			}
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
				try {
					// Include the repository type in the serialization context, so repositories
					// of this type can be deserialized from the configuration file.
					Repository r = vcs.CreateRepositoryInstance ();
					r.AddRef ();
					dataContext.IncludeType (r.GetType ());
					r.Unref ();
				} catch (Exception e) {
					LoggingService.LogError ("Error while adding version control system.", e);
				}
			}
			else {
				handlers.Remove (vcs);
			}
		}
		
		public static Gdk.Pixbuf LoadOverlayIconForStatus(VersionStatus status)
		{
			if ((status & VersionStatus.Versioned) == 0)
				return overlay_unversioned;
			
			switch (status & VersionStatus.LocalChangesMask) {
				case VersionStatus.Modified:
				case VersionStatus.ScheduledReplace:
				case VersionStatus.ScheduledIgnore:
					return overlay_modified;
				case VersionStatus.Conflicted:
					return overlay_conflicted;
				case VersionStatus.ScheduledAdd:
					return overlay_added;
				case VersionStatus.Missing:
				case VersionStatus.ScheduledDelete:
					return overlay_removed;
			}
			
			if ((status & VersionStatus.LockOwned) != 0)
				return overlay_unlocked;
			
			if ((status & VersionStatus.Locked) != 0)
				return overlay_locked;
			
			if ((status & VersionStatus.LockRequired) != 0)
				return overlay_protected;
				
			return null;
		}
		
		public static Gdk.Pixbuf LoadIconForStatus (VersionStatus status)
		{
			switch (status & VersionStatus.LocalChangesMask) {
				case VersionStatus.Modified:
				case VersionStatus.ScheduledReplace:
					return icon_modified;
				case VersionStatus.Conflicted:
					return icon_conflicted;
				case VersionStatus.ScheduledAdd:
					return icon_added;
				case VersionStatus.Missing:
				case VersionStatus.ScheduledDelete:
					return icon_removed;
			}
			return null;
		}

		public static string GetStatusLabel (VersionStatus status)
		{
			if ((status & VersionStatus.Versioned) == 0)
				return GettextCatalog.GetString ("Unversioned");
			
			switch (status & VersionStatus.LocalChangesMask) {
				case VersionStatus.Modified:
				case VersionStatus.ScheduledReplace:
					return GettextCatalog.GetString ("Modified");
				case VersionStatus.Conflicted:
					return GettextCatalog.GetString ("Conflict");
				case VersionStatus.ScheduledAdd:
					return GettextCatalog.GetString ("Add");
				case VersionStatus.ScheduledDelete:
					return GettextCatalog.GetString ("Delete");
				case VersionStatus.Missing:
					return GettextCatalog.GetString ("Missing");
			}
			return String.Empty;
		}
		
		public static Repository GetRepository (IWorkspaceObject entry)
		{
			InternalRepositoryReference repoRef = (InternalRepositoryReference) entry.ExtendedProperties [typeof(InternalRepositoryReference)];
			if (repoRef != null)
				return repoRef.Repo;
			
			Repository repo = VersionControlService.GetRepositoryReference (entry.BaseDirectory, entry.Name);
			InternalRepositoryReference rref = null;
			if (repo != null) {
				repo.AddRef ();
				rref = new InternalRepositoryReference (repo);
			}
			entry.ExtendedProperties [typeof(InternalRepositoryReference)] = rref;
			
			return repo;
		}
		
		static Repository GetRepositoryReference (string path, string id)
		{
			foreach (VersionControlSystem vcs in GetVersionControlSystems ()) {
				Repository repo = vcs.GetRepositoryReference (path, id);
				if (repo != null) {
					repo.VersionControlSystem = vcs;
					return repo;
				}
			}
			return null;
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
		
		static string CommitMessagesFile {
			get {
				return UserProfile.Current.CacheDir.Combine ("version-control-commit-msg");
				
			}
		}
		
		static Hashtable GetCommitComments ()
		{
			if (comments != null)
				return comments;

			ResolveEventHandler localResolve = delegate (object s, ResolveEventArgs args) {
				foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (asm.GetName ().FullName == args.Name)
						return asm;
				}
				return null;
			};

			string file = CommitMessagesFile;
			if (File.Exists (file)) {
				FileStream stream = null;
				try {
					AppDomain.CurrentDomain.AssemblyResolve += localResolve;

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
					AppDomain.CurrentDomain.AssemblyResolve -= localResolve;
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
					FilePath file = CommitMessagesFile;
					if (comments.Count == 0) {
						if (File.Exists (file))
							FileService.DeleteFile (file);
						return;
					}
				
					FileService.EnsureDirectoryExists (file.ParentDirectory);
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

		public static void NotifyFileStatusChanged (IEnumerable<VersionControlItem> items) 
		{
			FileUpdateEventArgs vargs = new FileUpdateEventArgs ();
			vargs.AddRange (items.Select (i => new FileUpdateEventInfo (i.Repository, i.Path, i.IsDirectory)));
			NotifyFileStatusChanged (vargs);
		}
		
		public static void NotifyFileStatusChanged (FileUpdateEventArgs args) 
		{
			if (!DispatchService.IsGuiThread)
				Gtk.Application.Invoke (delegate {
					NotifyFileStatusChanged (args);
				});
			else {
				if (FileStatusChanged != null)
					FileStatusChanged (null, args);
			}
		}
		
		//static void OnFileChanged (object s, ProjectFileEventArgs args)
		//{
		//	Repository repo = GetRepository (args.Project);
		//	if (repo != null)
		//		NotifyFileStatusChanged (repo, args.ProjectFile.FilePath, false);
		//}

		static void OnFileAdded (object s, ProjectFileEventArgs e)
		{
			FileUpdateEventArgs vargs = new FileUpdateEventArgs ();
			IProgressMonitor monitor = null;
			try {
				foreach (var repoFiles in e.GroupBy (i => i.Project)) {
					Repository repo = GetRepository (repoFiles.Key);
					if (repo == null)
						continue;
					var versionInfos = repo.GetVersionInfo (repoFiles.Select (f => f.ProjectFile.FilePath), VersionInfoQueryFlags.IgnoreCache);
					FilePath[] paths = versionInfos.Where (i => i.CanAdd).Select (i => i.LocalPath).ToArray ();
					if (paths.Length > 0) {
						if (monitor == null)
							monitor = GetStatusMonitor ();
						repo.Add (paths, false, monitor);
					}
					vargs.AddRange (repoFiles.Select (i => new FileUpdateEventInfo (repo, i.ProjectFile.FilePath, i.ProjectFile.Subtype == Subtype.Directory)));
				}
			}
			finally {
				if (monitor != null)
					monitor.Dispose ();
			}
			if (vargs.Count > 0)
				NotifyFileStatusChanged (vargs);
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
		static void SolutionItemAddFiles (string rootPath, SolutionItem entry, HashSet<string> files)
		{
			if (entry is SolutionEntityItem) {
				string file = ((SolutionEntityItem)entry).FileName;
				SolutionItemAddFile (rootPath, files, file);
			}
			
			if (entry is Project) {
				foreach (ProjectFile file in ((Project) entry).Files) {
					if (file.Subtype != Subtype.Directory)
						SolutionItemAddFile (rootPath, files, file.FilePath);
				}
			} else if (entry is SolutionFolder) {
				foreach (SolutionItem ent in ((SolutionFolder) entry).Items)
					SolutionItemAddFiles (rootPath, ent, files);
			}
		}
		
		static void SolutionItemAddFile (string rootPath, HashSet<string> files, string file)
		{
			if (!file.StartsWith (rootPath + Path.DirectorySeparatorChar))
			    return;
			if (!File.Exists (file))
				return;
			if (files.Add (file)) {
				string dir = Path.GetDirectoryName (file);
				while (dir != rootPath && files.Add (dir))
					dir = Path.GetDirectoryName (dir);
			}
		}
		
		static void OnEntryAdded (object o, SolutionItemEventArgs args)
		{
			if (args is SolutionItemChangeEventArgs && ((SolutionItemChangeEventArgs) args).Reloading)
				return;

			// handles addition of solutions and projects
			SolutionItem parent = (SolutionItem) args.SolutionItem.ParentFolder;
			
			if (parent == null)
				return;
			
			Repository repo = GetRepository (parent);
			
			if (repo == null)
				return;
			
			SolutionItem entry = args.SolutionItem;
			Repository currentRepo = GetRepository (entry);
			if (currentRepo != null && currentRepo.VersionControlSystem != repo.VersionControlSystem) {
				// If the item is already under version control using a different version control system
				// don't add it to the parent repo.
				return;
			}
			
			string path = entry.BaseDirectory;
			
			// While we /could/ call repo.Add with `recursive = true', we don't
			// necessarily want to add files under the project/solution directory
			// that may not be a part of this project/solution.

			var files = new HashSet<string> { path };
			SolutionItemAddFiles (path, entry, files);
			
			using (IProgressMonitor monitor = GetStatusMonitor ()) {
				var status = repo.GetDirectoryVersionInfo (path, false, true);
				foreach (var v in status) {
					if (!v.IsVersioned && files.Contains (v.LocalPath))
						repo.Add (v.LocalPath, false, monitor);
				}
			}
			
			NotifyFileStatusChanged (new FileUpdateEventArgs (repo, parent.BaseDirectory, true));
		}
		
		public static IProgressMonitor GetProgressMonitor (string operation)
		{
			return GetProgressMonitor (operation, VersionControlOperationType.Other);
		}
		
		public static IProgressMonitor GetProgressMonitor (string operation, VersionControlOperationType op)
		{
			IconId icon;
			switch (op) {
			case VersionControlOperationType.Pull: icon = Stock.StatusDownload; break;
			case VersionControlOperationType.Push: icon = Stock.StatusUpload; break;
			default: icon = "md-version-control"; break;
			}

			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ("MonoDevelop.VersionControlOutput", "Version Control", "md-version-control", false, true);
			Pad outPad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor (monitor);
			
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor (monitor);
			mon.AddSlaveMonitor (IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (operation, icon, true, true, false, outPad));
			return mon;
		}
		
		static IProgressMonitor GetStatusMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Updating version control repository"), "vc-remote-status", true);
		}
		
		static string ConfigFile {
			get {
				return UserProfile.Current.ConfigDir.Combine ("VersionControl.config");
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
					try {
						XmlDataSerializer ser = new XmlDataSerializer (dataContext);
						using (var reader = File.OpenText (ConfigFile))
							configuration = (VersionControlConfiguration) ser.Deserialize (reader, typeof (VersionControlConfiguration));
					} catch {
						((FilePath) ConfigFile).Delete ();
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
				using (var tw = new XmlTextWriter (File.CreateText (ConfigFile))) {
					tw.Formatting = Formatting.Indented;
					ser.Serialize (tw, configuration, typeof (VersionControlConfiguration));
				}
			}
		}
		
		public static void ResetConfiguration ()
		{
			configuration = null;
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
		
		public static CommitMessageFormat GetCommitMessageFormat (SolutionItem item)
		{
			CommitMessageFormat format = new CommitMessageFormat ();
			format.Style = item.Policies.Get<VersionControlPolicy> ().CommitMessageStyle;
			format.ShowFilesForSingleComment = false;
			return format;
		}
		
		public static CommitMessageFormat GetCommitMessageFormat (ChangeSet cset, out AuthorInformation authorInfo)
		{
			// If all files belong to a project, use that project's policy. If not, use the solution policy
			Project project = null;
			bool sameProject = true;
			foreach (ChangeSetItem item in cset.Items) {
				if (project != null) {
					if (project.Files.GetFile (item.LocalPath) == null) {
						// Not all files belong to the same project
						sameProject = false;
						break;
					}
				} else {
					project = IdeApp.Workspace.GetProjectContainingFile (item.LocalPath);
				}
			}
			CommitMessageStyle style;
			
			if (project != null) {
				VersionControlPolicy policy;
				if (sameProject)
					policy = project.Policies.Get<VersionControlPolicy> ();
				else
					policy = project.ParentSolution.Policies.Get<VersionControlPolicy> ();
				style = policy.CommitMessageStyle;
			}
			else {
				style = PolicyService.GetDefaultPolicy<CommitMessageStyle> ();
			}
			
			authorInfo = project != null ? project.AuthorInformation : AuthorInformation.Default;
			
			CommitMessageFormat format = new CommitMessageFormat ();
			format.Style = style;
			format.ShowFilesForSingleComment = false;
			
			return format;
		}
	}
	
	[Serializable]
	class CommitComment
	{
		public string Comment;
		public DateTime Date;
	}
	
	class InternalRepositoryReference: IDisposable
	{
		Repository repo;
		
		public InternalRepositoryReference (Repository repo)
		{
			this.repo = repo;
		}

		public Repository Repo {
			get {
				return this.repo;
			}
		}
		
		public void Dispose ()
		{
			repo.Unref ();
		}
	}

	public enum VersionControlOperationType
	{
		Pull,
		Push,
		Other
	}
}
