
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
		static Xwt.Drawing.Image overlay_modified;
		static Xwt.Drawing.Image overlay_removed;
		static Xwt.Drawing.Image overlay_renamed;
		static Xwt.Drawing.Image overlay_conflicted;
		static Xwt.Drawing.Image overlay_added;
		internal static Xwt.Drawing.Image overlay_controled;
		static Xwt.Drawing.Image overlay_unversioned;
		static Xwt.Drawing.Image overlay_protected;
		static Xwt.Drawing.Image overlay_locked;
		static Xwt.Drawing.Image overlay_unlocked;
        static Xwt.Drawing.Image overlay_ignored;

		static Xwt.Drawing.Image icon_modified;
		static Xwt.Drawing.Image icon_removed;
		static Xwt.Drawing.Image icon_conflicted;
		static Xwt.Drawing.Image icon_added;
		internal static Xwt.Drawing.Image icon_controled;
		
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
			IdeApp.Initialized += delegate {
				try {
					overlay_modified = Xwt.Drawing.Image.FromResource("modified-overlay-16.png");
					overlay_removed = Xwt.Drawing.Image.FromResource("removed-overlay-16.png");
					overlay_renamed = Xwt.Drawing.Image.FromResource("renamed-overlay-16.png");
					overlay_conflicted = Xwt.Drawing.Image.FromResource("conflict-overlay-16.png");
					overlay_added = Xwt.Drawing.Image.FromResource("added-overlay-16.png");
					overlay_controled = Xwt.Drawing.Image.FromResource("versioned-overlay-16.png");
					overlay_unversioned = Xwt.Drawing.Image.FromResource("unversioned-overlay-16.png");
					overlay_protected = Xwt.Drawing.Image.FromResource("lock-required-overlay-16.png");
					overlay_unlocked = Xwt.Drawing.Image.FromResource("unlocked-overlay-16.png");
					overlay_locked = Xwt.Drawing.Image.FromResource("locked-overlay-16.png");
					overlay_ignored = Xwt.Drawing.Image.FromResource("ignored-overlay-16.png");

					icon_modified = ImageService.GetIcon ("vc-file-modified", Gtk.IconSize.Menu);
					icon_removed = ImageService.GetIcon ("vc-file-removed", Gtk.IconSize.Menu);
					icon_conflicted = ImageService.GetIcon ("vc-file-conflicted", Gtk.IconSize.Menu);
					icon_added = ImageService.GetIcon ("vc-file-added", Gtk.IconSize.Menu);
					icon_controled = Xwt.Drawing.Image.FromResource("versioned-overlay-16.png");
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading icons.", e);
				}

				IdeApp.Workspace.SolutionLoaded += (sender, e) => SessionSolutionDisabled |= IsSolutionDisabled (e.Solution);

				IdeApp.Workspace.FileAddedToProject += OnFileAdded;
				//IdeApp.Workspace.FileChangedInProject += OnFileChanged;
				//IdeApp.Workspace.FileRemovedFromProject += OnFileRemoved;
				//IdeApp.Workspace.FileRenamedInProject += OnFileRenamed;

				IdeApp.Workspace.ItemAddedToSolution += OnEntryAdded;
				IdeApp.Exiting += delegate {
					DelayedSaveComments (null);
				};
			};

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/VersionControl/VersionControlSystems", OnExtensionChanged);
		}

		// This exists for the sole purpose of calling the static constructor.
		public static void Initialize ()
		{
		}

		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			VersionControlSystem vcs;

			try {
				vcs = (VersionControlSystem) args.ExtensionObject;
			} catch (Exception e) {
				LoggingService.LogError ("Failed to initialize VersionControlSystem type.", e);
				return;
			}

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
		
		public static Xwt.Drawing.Image LoadOverlayIconForStatus(VersionStatus status)
		{
			if ((status & VersionStatus.Ignored) != 0)
				return overlay_ignored;

			if ((status & VersionStatus.Versioned) == 0)
				return overlay_unversioned;
			
			switch (status & VersionStatus.LocalChangesMask) {
				case VersionStatus.Modified:
				case VersionStatus.ScheduledIgnore:
					return overlay_modified;
				case VersionStatus.ScheduledReplace:
					return overlay_renamed;
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
		
		public static Xwt.Drawing.Image LoadIconForStatus (VersionStatus status)
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
					return GettextCatalog.GetString ("Modified");
				case VersionStatus.ScheduledReplace:
					return GettextCatalog.GetString ("Renamed");
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

		internal static Dictionary<Repository, InternalRepositoryReference> referenceCache = new Dictionary<Repository, InternalRepositoryReference> ();
		public static Repository GetRepository (WorkspaceObject entry)
		{
			if (IsGloballyDisabled)
				return null;

			InternalRepositoryReference repoRef = (InternalRepositoryReference) entry.ExtendedProperties [typeof(InternalRepositoryReference)];
			if (repoRef != null && !repoRef.Repo.Disposed)
				return repoRef.Repo;
			
			Repository repo = GetRepositoryReference (entry.BaseDirectory, entry.Name);
			InternalRepositoryReference rref = null;
			if (repo != null) {
				repo.AddRef ();
				if (!referenceCache.TryGetValue (repo, out rref)) {
					rref = new InternalRepositoryReference (repo);
					referenceCache [repo] = rref;
				}
			}
			entry.ExtendedProperties [typeof(InternalRepositoryReference)] = rref;
			
			return repo;
		}

		public static Repository GetRepositoryReference (string path, string id)
		{
			VersionControlSystem detectedVCS = null;
			FilePath bestMatch = FilePath.Null;

			foreach (VersionControlSystem vcs in GetVersionControlSystems ()) {
				var newPath = vcs.GetRepositoryPath (path, id);
				if (!newPath.IsNullOrEmpty) {
					// Check whether we have no match or if a new match is found with a longer path.
					// TODO: If the repo root is not the same as the repo reference, ask user for input.
					// TODO: If we have two version control directories in the same place, ask user for input.
					if (bestMatch.IsNullOrEmpty) {
						bestMatch = newPath;
						detectedVCS = vcs;
					} else if (bestMatch.CompareTo (newPath) <= 0) {
						bestMatch = newPath;
						detectedVCS = vcs;
					}
				}
			}
			return detectedVCS == null ? null : detectedVCS.GetRepositoryReference (bestMatch, id);

		}
		
		internal static void SetCommitComment (string file, string comment, bool save)
		{
			lock (commentsLock) {
				Hashtable doc = GetCommitComments ();
				if (String.IsNullOrEmpty (comment)) {
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

			ResolveEventHandler localResolve = (s, args) =>
				AppDomain.CurrentDomain.GetAssemblies ()
					.FirstOrDefault (asm => asm.GetName ().FullName == args.Name);

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
				
					Directory.CreateDirectory (file.ParentDirectory);
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
		
		internal static void NotifyPrepareCommit (Repository repo, ChangeSet changeSet)
		{
			if (!Runtime.IsMainThread) {
				Gtk.Application.Invoke (delegate {
					NotifyPrepareCommit (repo, changeSet);
				});
				return;
			}

			try {
				PrepareCommit?.Invoke (null, new CommitEventArgs (repo, changeSet, false));
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
		}
		
		internal static void NotifyBeforeCommit (Repository repo, ChangeSet changeSet)
		{
			if (!Runtime.IsMainThread) {
				Gtk.Application.Invoke (delegate {
					NotifyBeforeCommit (repo, changeSet);
				});
				return;
			}

			try {
				BeginCommit?.Invoke (null, new CommitEventArgs (repo, changeSet, false));
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
		}
		
		internal static void NotifyAfterCommit (Repository repo, ChangeSet changeSet, bool success)
		{
			if (!Runtime.IsMainThread) {
				Gtk.Application.Invoke (delegate {
					NotifyAfterCommit (repo, changeSet, success);
				});
				return;
			}

			try {
				EndCommit?.Invoke (null, new CommitEventArgs (repo, changeSet, success));
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
				return;
			}

			if (success) {
				foreach (ChangeSetItem it in changeSet.Items)
					SetCommitComment (it.LocalPath, null, false);
				SaveComments ();
			}
		}

		public static void NotifyFileStatusChanged (IEnumerable<VersionControlItem> items) 
		{
			FileUpdateEventArgs vargs = new FileUpdateEventArgs ();
			vargs.AddRange (items.Select (i => new FileUpdateEventInfo (i.Repository, i.Path, i.IsDirectory)));
			NotifyFileStatusChanged (vargs);
		}
		
		public static void NotifyFileStatusChanged (FileUpdateEventArgs args) 
		{
			if (!Runtime.IsMainThread)
				Gtk.Application.Invoke (delegate {
					NotifyFileStatusChanged (args);
				});
			else {
				FileStatusChanged?.Invoke (null, args);
			}
		}

		static bool ShouldAddFile (ProjectFileEventInfo info)
		{
			const ProjectItemFlags ignoreFlags = ProjectItemFlags.DontPersist | ProjectItemFlags.Hidden;
			return (info.ProjectFile.Flags & ignoreFlags) != ignoreFlags;
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
			ProgressMonitor monitor = null;
			try {
				foreach (var repoFiles in e.GroupBy (i => i.Project)) {
					Repository repo = GetRepository (repoFiles.Key);
					if (repo == null)
						continue;
					var filePaths = repoFiles.Where (ShouldAddFile).Select (f => f.ProjectFile.FilePath);
					var versionInfos = repo.GetVersionInfo (filePaths, VersionInfoQueryFlags.IgnoreCache);
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
		static void SolutionItemAddFiles (string rootPath, SolutionFolderItem entry, HashSet<string> files)
		{
			if (entry is SolutionItem) {
				foreach (var file in ((SolutionItem)entry).GetItemFiles (false))
					SolutionItemAddFile (rootPath, files, file);
			}
			
			if (entry is Project) {
				foreach (ProjectFile file in ((Project) entry).Files) {
					if (file.Subtype != Subtype.Directory)
						SolutionItemAddFile (rootPath, files, file.FilePath);
				}
			} else if (entry is SolutionFolder) {
				foreach (SolutionFolderItem ent in ((SolutionFolder) entry).Items)
					SolutionItemAddFiles (rootPath, ent, files);
			}
		}
		
		static void SolutionItemAddFile (string rootPath, HashSet<string> files, string file)
		{
			if (!file.StartsWith (rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
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
			SolutionFolderItem parent = (SolutionFolderItem) args.SolutionItem.ParentFolder;
			
			if (parent == null)
				return;
			
			Repository repo = GetRepository (parent);
			
			if (repo == null)
				return;
			
			SolutionFolderItem entry = args.SolutionItem;
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
			
			using (ProgressMonitor monitor = GetStatusMonitor ()) {
				var status = repo.GetDirectoryVersionInfo (path, false, true);
				foreach (var v in status) {
					if (!v.IsVersioned && files.Contains (v.LocalPath))
						repo.Add (v.LocalPath, false, monitor);
				}
			}

			if (entry is SolutionFolder && files.Count == 1)
				return;

			NotifyFileStatusChanged (new FileUpdateEventArgs (repo, parent.BaseDirectory, true));
		}
		
		public static ProgressMonitor GetProgressMonitor (string operation)
		{
			return GetProgressMonitor (operation, VersionControlOperationType.Other);
		}
		
		public static ProgressMonitor GetProgressMonitor (string operation, VersionControlOperationType op)
		{
			IconId icon;
			switch (op) {
			case VersionControlOperationType.Pull: icon = Stock.PadDownload; break;
			case VersionControlOperationType.Push: icon = Stock.PadUpload; break;
			default: icon = "md-version-control"; break;
			}

			ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ("MonoDevelop.VersionControlOutput", GettextCatalog.GetString ("Version Control"), "md-version-control", false, true);
			Pad outPad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor (monitor);
			
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor (monitor);
			mon.AddFollowerMonitor (IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (operation, icon, true, true, false, outPad));
			return mon;
		}
		
		static ProgressMonitor GetStatusMonitor ()
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

		public static bool IsGloballyDisabled {
			get { return ConfigurationGlobalDisabled || SessionSolutionDisabled; }
			set { ConfigurationGlobalDisabled = value; }
		}

		public static bool IsSolutionDisabled (Solution it)
		{
			return it.UserProperties.HasValue ("VersionControlDisabled");
		}

		public static void SetSolutionDisabled (Solution it, bool value)
		{
			if (value)
				it.UserProperties.SetValue ("VersionControlDisabled", true);
			else
				it.UserProperties.RemoveValue ("VersionControlDisabled");
			SessionSolutionDisabled = value;
		}

		internal static bool ConfigurationGlobalDisabled {
			get { return GetConfiguration ().Disabled; }
			set { GetConfiguration ().Disabled = value; }
		}

		internal static bool SessionSolutionDisabled {
			get;
			private set;
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

		public static bool CheckVersionControlInstalled ()
		{
			if (IsGloballyDisabled)
				return false;

			return GetVersionControlSystems ().Any (vcs => vcs.IsInstalled);
		}
		
		public static CommitMessageFormat GetCommitMessageFormat (SolutionFolderItem item)
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
					project = IdeApp.Workspace.GetProjectsContainingFile (item.LocalPath).FirstOrDefault ();
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
		readonly Repository repo;
		public InternalRepositoryReference (Repository repo)
		{
			this.repo = repo;
		}

		public Repository Repo {
			get {
				return repo;
			}
		}
		
		public void Dispose ()
		{
			VersionControlService.referenceCache.Remove (repo);
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
