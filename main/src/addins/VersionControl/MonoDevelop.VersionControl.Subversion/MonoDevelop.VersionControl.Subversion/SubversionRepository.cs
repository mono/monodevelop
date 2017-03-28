
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Subversion
{
	public sealed class SubversionRepository: UrlBasedRepository
	{
		public SubversionRepository ()
		{
			Url = "svn://";
		}
		
		public SubversionRepository (SubversionVersionControl vcs, string url, FilePath rootPath): base (vcs)
		{
			Url = url;
			RootPath = !rootPath.IsNullOrEmpty ? rootPath.CanonicalPath : null;
		}

		public override string[] SupportedProtocols {
			get {
				return new string[] {"svn", "svn+ssh", "http", "https", "file"};

			}
		}

		// TODO: Handle externals.
		public override bool HasChildRepositories {
			get { return false; }
		}
		
		/*public override IEnumerable<Repository> ChildRepositories {
			get {
				List<Repository> list = new List<Repository> ();
				
				foreach (DirectoryEntry ent in Svn.ListUrl (Url, false)) {
					if (ent.IsDirectory) {
						SubversionRepository rep = new SubversionRepository (VersionControlSystem, Url + "/" + ent.Name, null);
						rep.Name = ent.Name;
						list.Add (rep);
					}
				}
				return list;
			}
		}*/

		public override bool SupportsRemoteStatus {
			get { return true; }
		}

		public override bool SupportsRevertRevision {
			get { return true; }
		}

		public override bool SupportsRevertToRevision {
			get { return true; }
		}

		new SubversionVersionControl VersionControlSystem {
			get { return (SubversionVersionControl)base.VersionControlSystem; }
		}

		SubversionBackend backend;
		internal SubversionBackend Svn {
			get {
				if (backend == null)
					backend = VersionControlSystem.CreateBackend ();
				return backend;
			}
		}

		bool IsVersioned (FilePath sourcefile)
		{
			return GetVersionInfo (sourcefile, VersionInfoQueryFlags.IgnoreCache).IsVersioned;
		}
		
		public override string GetBaseText (FilePath localFile)
		{
			return Svn.GetTextBase (localFile);
		}

		protected override string OnGetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			return Svn.GetTextAtRevision (repositoryPath, revision, RootPath);
		}

		protected override Revision[] OnGetHistory (FilePath localFile, Revision since)
		{
			return Svn.GetHistory (this, localFile, since);
		}
		
		protected override RevisionPath[] OnGetRevisionChanges (Revision revision)
		{
			SvnRevision rev = (SvnRevision) revision;
			return rev.ChangedFiles ?? new RevisionPath [0];
		}

		protected override IEnumerable<VersionInfo> OnGetVersionInfo (IEnumerable<FilePath> paths, bool getRemoteStatus)
		{
			// TODO: optimize by adding a method for handling collections of files
			foreach (var p in paths)
				yield return Svn.GetVersionInfo (this, p, getRemoteStatus);
		}

		protected override VersionInfo[] OnGetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			return Svn.GetDirectoryVersionInfo (this, localDirectory, getRemoteStatus, recursive);
		}
		
		protected override VersionControlOperation GetSupportedOperations (VersionInfo vinfo)
		{
			return Svn.GetSupportedOperations (this, vinfo, base.GetSupportedOperations (vinfo));
		}

		public override bool RequestFileWritePermission (params FilePath[] paths)
		{
			var toLock = new List<FilePath>();

			foreach (var path in paths) {
				if (!File.Exists (path) || !GetVersionInfo (path).IsVersioned || !Svn.HasNeedLock (path) || (File.GetAttributes (path) & FileAttributes.ReadOnly) == 0)
					continue;
				toLock.Add (path);
			}

			if (toLock.Count == 0)
				return true;

			AlertButton but = new AlertButton (GettextCatalog.GetString ("Lock File"));
			if (!MessageService.Confirm (GettextCatalog.GetString ("The following files must be locked before editing."),
				String.Join ("\n", toLock.Select (u => u.ToString ())), but))
				return false;

			try {
				Svn.Lock (null, "", false, toLock.ToArray ());
			} catch (SubversionException ex) {
				MessageService.ShowError (GettextCatalog.GetString ("File could not be unlocked."), ex.Message);
				return false;
			}

			VersionControlService.NotifyFileStatusChanged (new FileUpdateEventArgs (this, toLock.ToArray ()));
			return true;
		}

		protected override void OnLock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			Svn.Lock (monitor, "", false, localPaths);
		}

		protected override void OnUnlock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			Svn.Unlock (monitor, false, localPaths);
		}

		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor)
		{
			string url = Url;
			if (!serverPath.StartsWith ("/", StringComparison.Ordinal) && !url.EndsWith ("/", StringComparison.Ordinal))
				url += "/";
			url += serverPath;
			
			string[] paths = new string[] {url};

			CreateDirectory (paths, message, monitor);
			Svn.Checkout (this.Url + "/" + serverPath, localPath, null, true, monitor);

			RootPath = localPath;
			Set<FilePath> dirs = new Set<FilePath> ();
			PublishDir (dirs, localPath, false, monitor);

			foreach (FilePath file in files) {
				PublishDir (dirs, file.ParentDirectory, true, monitor);
				Add (file, false, monitor);
			}

			Svn.Commit (new FilePath[] { localPath }, message, monitor);
			
			return new SubversionRepository (VersionControlSystem, paths[0], localPath);
		}

		void PublishDir (Set<FilePath> dirs, FilePath dir, bool rec, ProgressMonitor monitor)
		{
			if (dirs.Add (dir.CanonicalPath)) {
				if (rec) {
					PublishDir (dirs, dir.ParentDirectory, true, monitor);
					Add (dir, false, monitor);
				}
			}
		}

		protected override void OnUpdate (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			foreach (string path in localPaths)
				Svn.Update (path, recurse, monitor);
		}
		
		protected override void OnCommit (ChangeSet changeSet, ProgressMonitor monitor)
		{
			Svn.Commit (changeSet.Items.Select (it => it.LocalPath).ToArray (), changeSet.GlobalComment, monitor);
		}

		void CreateDirectory (string[] paths, string message, ProgressMonitor monitor)
		{
			Svn.Mkdir (paths, message, monitor);
		}

		protected override void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
		{
			if (!VersionControlSystem.InstallDependencies ()) {
				throw new SubversionException (GettextCatalog.GetString ("Restart {0} after the installation process has completed", BrandingService.ApplicationName));
			};
			Svn.Checkout (this.Url, targetLocalPath, rev, recurse, monitor);
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			// If we have an array of paths such as: new [] { "/Foo/Directory", "/Foo/Directory/File1", "/Foo/Directory/File2" }
			// svn will successfully revert the first entry (the directory) and then throw an error when trying to revert the
			// second and third entries because by reverting the directory the files are implicitly reverted. Try to work around
			// this issue.
			Array.Sort<FilePath> (localPaths, (a, b) => b.CompareTo (a));
			Svn.Revert (localPaths, recurse, monitor);
		}

		protected override void OnRevertRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			Svn.RevertRevision (localPath, revision, monitor);
		}

		protected override void OnRevertToRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			Svn.RevertToRevision (localPath, revision, monitor);
		}

		protected override void OnAdd (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			foreach (FilePath path in localPaths) {
				if (IsVersioned (path) && File.Exists (path) && !Directory.Exists (path)) {
					if (RootPath.IsNull)
						throw new UserException (GettextCatalog.GetString ("Project publishing failed. There is a stale .svn folder in the path '{0}'", path.ParentDirectory));
					VersionInfo srcInfo = GetVersionInfo (path, VersionInfoQueryFlags.IgnoreCache);
					if (srcInfo.HasLocalChange (VersionStatus.ScheduledDelete)) {
						// It is a file that was deleted. It can be restored now since it's going
						// to be added again.
						// First of all, make a copy of the file
						string tmp = Path.GetTempFileName ();
						File.Copy (path, tmp, true);
						
						// Now revert the status of the file
						Revert (path, false, monitor);
						
						// Copy the file over the old one and clean up
						File.Copy (tmp, path, true);
						File.Delete (tmp);
						continue;
					}
				}
				else {
					if (!IsVersioned (path.ParentDirectory)) {
						// The file/folder belongs to an unversioned folder. We can add it by versioning the parent
						// folders up to the root of the repository
						
						if (!path.IsChildPathOf (RootPath))
							throw new InvalidOperationException ("File outside the repository directory");

						List<FilePath> dirChain = new List<FilePath> ();
						FilePath parentDir = path.CanonicalPath;
						do {
							parentDir = parentDir.ParentDirectory;
							if (IsVersioned (parentDir))
								break;
							dirChain.Add (parentDir);
						}
						while (parentDir != RootPath);

						// Found all parent unversioned dirs. Versin them now.
						dirChain.Reverse ();
						FileUpdateEventArgs args = new FileUpdateEventArgs ();
						foreach (var d in dirChain) {
							Svn.Add (d, false, monitor);
							args.Add (new FileUpdateEventInfo (this, d, true));
						}
						VersionControlService.NotifyFileStatusChanged (args);
					}
				}
				Svn.Add (path, recurse, monitor);
			}
		}
		
		public string Root {
			get {
				try {
					return new UriBuilder (Url) {
						Path = string.Empty,
						Query = string.Empty
					}.ToString ();
				} catch {
					return string.Empty;
				}
			}
		}		

		public override bool CanMoveFilesFrom (Repository srcRepository, FilePath localSrcPath, FilePath localDestPath)
		{
			return (srcRepository is SubversionRepository) && ((SubversionRepository)srcRepository).Root == Root;
		}

		protected override void OnMoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			bool destIsVersioned = false;

			if (File.Exists (localDestPath)) {
				if (string.Equals (localSrcPath, localDestPath, StringComparison.OrdinalIgnoreCase)) {
					Svn.Move (localSrcPath, localDestPath, true, monitor);
					return;
				}
				throw new InvalidOperationException ("Cannot move file. Destination file already exist.");
			}

			if (IsVersioned (localDestPath)) {
				// Revert to the original status
				Revert (localDestPath, false, monitor);
				if (File.Exists (localDestPath))
					File.Delete (localDestPath);
				destIsVersioned = true;
			}
			
			VersionInfo srcInfo = GetVersionInfo (localSrcPath, VersionInfoQueryFlags.IgnoreCache);
			if (srcInfo != null && srcInfo.HasLocalChange (VersionStatus.ScheduledAdd)) {
				// Subversion automatically detects the rename and moves the new file accordingly.
				if (!destIsVersioned) {
					MakeDirVersioned (Path.GetDirectoryName (localDestPath), monitor);
					Svn.Move (localSrcPath, localDestPath, force, monitor);
				} else {
					base.OnMoveFile (localSrcPath, localDestPath, force, monitor);
					Add (localDestPath, false, monitor);
					DeleteFile (localSrcPath, force, monitor, keepLocal: false);
				}
			} else {
				if (!destIsVersioned && IsVersioned (localSrcPath)) {
					MakeDirVersioned (Path.GetDirectoryName (localDestPath), monitor);
					Svn.Move (localSrcPath, localDestPath, force, monitor);
				} else
					base.OnMoveFile (localSrcPath, localDestPath, force, monitor);
			}
			ClearCachedVersionInfo (localSrcPath, localDestPath);
		}

		protected override void OnMoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			if (IsVersioned (localDestPath))
			{
				VersionInfo vinfo = GetVersionInfo (localDestPath, VersionInfoQueryFlags.IgnoreCache);
				if (!vinfo.HasLocalChange (VersionStatus.ScheduledDelete) && Directory.Exists (localDestPath))
					throw new InvalidOperationException ("Cannot move directory. Destination directory already exist.");
					
				localSrcPath = localSrcPath.FullPath;
				
				// The target directory does not exist, but it is versioned. It may be because
				// it is scheduled to delete, or maybe it has been physicaly deleted. In any
				// case we are going to replace the old directory by the new directory.
				
				// Revert the old directory, so we can see which files were there so
				// we can delete or replace them
				Revert (localDestPath, true, monitor);
				
				// Get the list of files in the directory to be replaced
				ArrayList oldFiles = new ArrayList ();
				GetDirectoryFiles (localDestPath, oldFiles);
				
				// Get the list of files to move
				ArrayList newFiles = new ArrayList ();
				GetDirectoryFiles (localSrcPath, newFiles);
				
				// Move all new files to the new destination
				Hashtable copiedFiles = new Hashtable ();
				Hashtable copiedFolders = new Hashtable ();
				foreach (string file in newFiles) {
					string src = Path.GetFullPath (file);
					string dst = Path.Combine (localDestPath, src.Substring (((string)localSrcPath).Length + 1));
					if (File.Exists (dst))
						File.Delete (dst);
					
					// Make sure the target directory exists
					string destDir = Path.GetDirectoryName (dst);
					if (!Directory.Exists (destDir))
						Directory.CreateDirectory (destDir);
						
					// If the source file is versioned, make sure the target directory
					// is also versioned.
					if (IsVersioned (src))
						MakeDirVersioned (destDir, monitor);

					MoveFile (src, dst, true, monitor);
					copiedFiles [dst] = dst;
					string df = Path.GetDirectoryName (dst);
					copiedFolders [df] = df;
				}

				// Delete all old files which have not been replaced
				ArrayList foldersToDelete = new ArrayList ();
				foreach (string oldFile in oldFiles) {
					if (!copiedFiles.Contains (oldFile)) {
						DeleteFile (oldFile, true, monitor, false);
						string fd = Path.GetDirectoryName (oldFile);
						if (!copiedFolders.Contains (fd) && !foldersToDelete.Contains (fd))
							foldersToDelete.Add (fd);
					}
				}
				
				// Delete old folders
				foreach (string folder in foldersToDelete) {
					Svn.Delete (folder, true, monitor);
				}
				
				// Delete the source directory
				DeleteDirectory (localSrcPath, true, monitor, false);
			}
			else {
				if (Directory.Exists (localDestPath))
					throw new InvalidOperationException ("Cannot move directory. Destination directory already exist.");
				
				VersionInfo srcInfo = GetVersionInfo (localSrcPath, VersionInfoQueryFlags.IgnoreCache);
				if (srcInfo != null && srcInfo.HasLocalChange (VersionStatus.ScheduledAdd)) {
					// If the directory is scheduled to add, cancel it, move the directory, and schedule to add it again
					MakeDirVersioned (Path.GetDirectoryName (localDestPath), monitor);
					Revert (localSrcPath, true, monitor);
					base.OnMoveDirectory (localSrcPath, localDestPath, force, monitor);
					Add (localDestPath, true, monitor);
				} else {
					if (IsVersioned (localSrcPath)) {
						MakeDirVersioned (Path.GetDirectoryName (localDestPath), monitor);
						Svn.Move (localSrcPath, localDestPath, force, monitor);
					} else
						base.OnMoveDirectory (localSrcPath, localDestPath, force, monitor);
				}
			}
		}
		
		void MakeDirVersioned (string dir, ProgressMonitor monitor)
		{
			if (Directory.Exists (SubversionBackend.GetDirectoryDotSvn (VersionControlSystem, dir)))
				return;
			
			// Make the parent versioned
			string parentDir = Path.GetDirectoryName (dir);
			if (parentDir == dir || parentDir == "")
				throw new InvalidOperationException ("Could not create versioned directory.");
			MakeDirVersioned (parentDir, monitor);
			
			Add (dir, false, monitor);
		}

		void GetDirectoryFiles (string directory, ArrayList collection)
		{
			string[] dir = Directory.GetDirectories(directory);
			foreach (string d in dir) {
				// Get all files ignoring the .svn directory
				if (Path.GetFileName (d) != ".svn")
					GetDirectoryFiles (d, collection);
			}
			string[] file = Directory.GetFiles (directory);
			foreach (string f in file)
				collection.Add(f);
		}

		protected override void OnDeleteFiles (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
		{
			foreach (string path in localPaths) {
				if (IsVersioned (path)) {
					string newPath = String.Empty;
					if (keepLocal) {
						newPath = Path.GetTempFileName ();
						File.Copy (path, newPath, true);
					}
					try {
						Svn.Delete (path, force, monitor);
					} finally {
						if (keepLocal)
							File.Move (newPath, path);
					}
				} else {
					if (keepLocal) {
						VersionInfo srcInfo = GetVersionInfo (path, VersionInfoQueryFlags.IgnoreCache);
						if (srcInfo != null && srcInfo.HasLocalChange (VersionStatus.ScheduledAdd)) {
							// Revert the add command
							Revert (path, false, monitor);
						}
					} else
						File.Delete (path);
				}
			}
		}

		protected override void OnDeleteDirectories (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
		{
			foreach (string path in localPaths) {
				if (IsVersioned (path)) {
					string newPath = String.Empty;
					if (keepLocal) {
						newPath = FileService.CreateTempDirectory ();
						FileService.CopyDirectory (path, newPath);
					}
					try {
						Svn.Delete (path, force, monitor);
					} finally {
						if (keepLocal)
							FileService.MoveDirectory (newPath, path);
					}
				} else {
					if (keepLocal) {
						foreach (var info in GetDirectoryVersionInfo (path, false, true)) {
							if (info != null && info.HasLocalChange (VersionStatus.ScheduledAdd)) {
								// Revert the add command
								Revert (path, false, monitor);
							}
						}
					} else
						Directory.Delete (path, true);
				}
			}
		}
		
		public override DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			string diff = Svn.GetUnifiedDiff (versionInfo.LocalPath, false, false);
			if (!string.IsNullOrEmpty (diff))
				return GenerateUnifiedDiffInfo (diff, baseLocalPath, new FilePath[] { versionInfo.LocalPath }).FirstOrDefault ();
			return null;
		}
		
		public override DiffInfo[] PathDiff (FilePath localPath, Revision fromRevision, Revision toRevision)
		{
			string diff = Svn.GetUnifiedDiff (localPath, (SvnRevision)fromRevision, localPath, (SvnRevision)toRevision, true);
			return GenerateUnifiedDiffInfo (diff, localPath, null);
		}

		public override DiffInfo[] PathDiff (FilePath baseLocalPath, FilePath[] localPaths, bool remoteDiff)
		{
			if (localPaths != null) {
				var list = new List<DiffInfo> ();
				foreach (string path in localPaths) {
					string diff = Svn.GetUnifiedDiff (path, false, remoteDiff);
					if (string.IsNullOrEmpty (diff))
						continue;
					list.AddRange (GenerateUnifiedDiffInfo (diff, baseLocalPath, new FilePath[] { path }));
				}
				return list.ToArray ();
			} else {
				string diff = Svn.GetUnifiedDiff (baseLocalPath, true, remoteDiff);
				return GenerateUnifiedDiffInfo (diff, baseLocalPath, null);
			}
		}
		
		public override Annotation[] GetAnnotations (FilePath repositoryPath, Revision since)
		{
			SvnRevision sinceRev = since != null ? (SvnRevision)since : null;
			List<Annotation> annotations = new List<Annotation> (Svn.GetAnnotations (this, repositoryPath, SvnRevision.First, sinceRev ?? SvnRevision.Base));
			Annotation nextRev = new Annotation (null, GettextCatalog.GetString ("<uncommitted>"), DateTime.MinValue, null, GettextCatalog.GetString ("working copy"));
			var baseDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (GetBaseText (repositoryPath));
			var workingDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (File.ReadAllText (repositoryPath));

			// "SubversionException: blame of the WORKING revision is not supported"
			if (sinceRev == null) {
				foreach (var hunk in baseDocument.Diff (workingDocument, includeEol: false)) {
					annotations.RemoveRange (hunk.RemoveStart - 1, hunk.Removed);
					for (int i = 0; i < hunk.Inserted; ++i) {
						if (hunk.InsertStart + i >= annotations.Count)
							annotations.Add (nextRev);
						else
							annotations.Insert (hunk.InsertStart - 1, nextRev);
					}
				}
			}
			
			return annotations.ToArray ();
		}
		
		public override string CreatePatch (IEnumerable<DiffInfo> diffs)
		{
			StringBuilder patch = new StringBuilder ();
			
			if (null != diffs) {
				foreach (DiffInfo diff in diffs) {
					string relpath;
					if (diff.FileName.IsChildPathOf (diff.BasePath))
						relpath = diff.FileName.ToRelative (diff.BasePath);
					else if (diff.FileName == diff.BasePath)
						relpath = diff.FileName.FileName;
					else
						relpath = diff.FileName;
					relpath = relpath.Replace (Path.DirectorySeparatorChar, '/');
					patch.Append ("Index: ").AppendLine (relpath);
					patch.AppendLine (new string ('=', 67));
					patch.AppendLine (diff.Content);
				}
			}
			
			return patch.ToString ();
		}

		protected override void OnIgnore (FilePath[] localPath)
		{
			Svn.Ignore (localPath);
		}

		protected override void OnUnignore (FilePath[] localPath)
		{
			Svn.Unignore (localPath);
		}
	}
}
