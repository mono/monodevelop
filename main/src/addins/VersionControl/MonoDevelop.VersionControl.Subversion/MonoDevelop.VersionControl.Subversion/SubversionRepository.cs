
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.VersionControl.Subversion
{
	class SubversionRepository: UrlBasedRepository
	{
		public SubversionRepository ()
		{
			Method = "svn";
		}
		
		public SubversionRepository (SubversionVersionControl vcs, string url): base (vcs)
		{
			Url = url;
		}
		
		public override bool HasChildRepositories {
			get { return true; }
		}
		
		public override IEnumerable<Repository> ChildRepositories {
			get {
				List<Repository> list = new List<Repository> ();
				
				foreach (LibSvnClient.DirEnt ent in Svn.List (Url, false)) {
					if (ent.IsDirectory) {
						SubversionRepository rep = new SubversionRepository (Svn, Url + "/" + ent.Name);
						rep.Name = ent.Name;
						list.Add (rep);
					}
				}
				return list;
			}
		}
		
		SubversionVersionControl Svn {
			get { return (SubversionVersionControl) VersionControlSystem; }
		}
		
		public override bool IsModified (string sourcefile)
		{
			return Svn.IsDiffAvailable (this, sourcefile);
		}
		
		public override bool IsVersioned (string sourcefile)
		{
			return Svn.IsVersioned (sourcefile);
		}
		
		public override bool CanAdd (string sourcepath)
		{
			if (IsVersioned (sourcepath) && File.Exists (sourcepath) && !Directory.Exists (sourcepath)) {
				VersionInfo srcInfo = GetVersionInfo (sourcepath, false);
				return srcInfo.HasLocalChange (VersionStatus.ScheduledDelete);
			}

			return Svn.CanAdd (this, sourcepath);
		}
		
		public override bool CanCommit (string localPath)
		{
			return Svn.CanCommit (this, localPath);
		}
		
		public override string GetPathToBaseText (string sourcefile)
		{
			return Svn.GetPathToBaseText (sourcefile);
		}
		
		public override string GetTextAtRevision (string repositoryPath, Revision revision)
		{
			return Svn.GetTextAtRevision (repositoryPath, revision);
		}
		
		public override Revision[] GetHistory (string sourcefile, Revision since)
		{
			return Svn.GetHistory (this, sourcefile, since);
		}
		
		public override VersionInfo GetVersionInfo (string localPath, bool getRemoteStatus)
		{
			return Svn.GetVersionInfo (this, localPath, getRemoteStatus);
		}
		
		public override VersionInfo[] GetDirectoryVersionInfo (string sourcepath, bool getRemoteStatus, bool recursive)
		{
			return Svn.GetDirectoryVersionInfo (this, sourcepath, getRemoteStatus, recursive);
		}
		
		public override bool RequestFileWritePermission (string path)
		{
			if (!File.Exists (path))
				return true;
			if ((File.GetAttributes (path) & FileAttributes.ReadOnly) == 0)
				return true;
			AlertButton but = new AlertButton ("Lock File");
			if (!MessageService.Confirm (GettextCatalog.GetString ("File locking required"), GettextCatalog.GetString ("The file '{0}' must be locked before editing.", path), but))
				return false;
			try {
				Svn.Lock (null, "", false, path);
			} catch (SubversionException ex) {
				MessageService.ShowError (GettextCatalog.GetString ("The file '{0}' could not be unlocked", path), ex.Message);
				return false;
			}
			VersionControlService.NotifyFileStatusChanged (this, path, false);
			return true;
		}
		
		public override void Lock (IProgressMonitor monitor, params string[] localPaths)
		{
			Svn.Lock (monitor, "", false, localPaths);
		}
		
		public override void Unlock (IProgressMonitor monitor, params string[] localPaths)
		{
			Svn.Unlock (monitor, false, localPaths);
		}
		
		public override bool CanLock (string localPath)
		{
			if (Directory.Exists (localPath))
				return false;
			VersionInfo ver = GetVersionInfo (localPath, false);
			if (ver == null || !ver.IsVersioned || ver.HasLocalChanges)
				return false;
			return (ver.Status & VersionStatus.LockOwned) == 0;
		}
		
		public override bool CanUnlock (string localPath)
		{
			if (Directory.Exists (localPath))
				return false;
			VersionInfo ver = GetVersionInfo (localPath, false);
			return ver != null && (ver.Status & VersionStatus.LockOwned) != 0;
		}

		public override Repository Publish (string serverPath, string localPath, string[] files, string message, IProgressMonitor monitor)
		{
			string url = Url;
			if (!serverPath.StartsWith ("/"))
				url += "/";
			url += serverPath;
			
			string[] paths = new string[] {url};
			
			CreateDirectory (paths, message, monitor);
			Svn.Checkout (this.Url + "/" + serverPath, localPath, null, true, monitor);

			Hashtable dirs = new Hashtable ();
			PublishDir (dirs, localPath, false, monitor);

			foreach (string file in files) {
				PublishDir (dirs, Path.GetDirectoryName (file), true, monitor);
				Add (file, false, monitor);
			}
			
			Svn.Commit (new string[] { localPath }, message, monitor);
			
			return new SubversionRepository (Svn, paths[0]);
		}

		void PublishDir (Hashtable dirs, string dir, bool rec, IProgressMonitor monitor)
		{
			while (dir [dir.Length - 1] == Path.DirectorySeparatorChar)
				dir = dir.Substring (0, dir.Length - 1);

			if (dirs.ContainsKey (dir))
				return;

			dirs [dir] = dir;
			if (rec) {
				PublishDir (dirs, Path.GetDirectoryName (dir), true, monitor);
				Add (dir, false, monitor);
			}
		}

		public override void Update (string[] paths, bool recurse, IProgressMonitor monitor)
		{
			foreach (string path in paths)
				Svn.Update (path, recurse, monitor);
		}
		
		public override void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
			ArrayList list = new ArrayList ();
			foreach (ChangeSetItem it in changeSet.Items)
				list.Add (it.LocalPath);
			Svn.Commit ((string[])list.ToArray (typeof(string)), changeSet.GlobalComment, monitor);
		}
		
		void CreateDirectory (string[] paths, string message, IProgressMonitor monitor)
		{
			Svn.Mkdir (paths, message, monitor);
		}
		
		public override void Checkout (string path, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			Svn.Checkout (this.Url, path, rev, recurse, monitor);
		}
		
		public void Resolve (string[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			Svn.Resolve (localPaths, recurse, monitor);
			foreach (string path in localPaths)
				VersionControlService.NotifyFileStatusChanged (this, path, Directory.Exists (path));
		}
		
		public override void Revert (string[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			Svn.Revert (localPaths, recurse, monitor);
		}

		public override void RevertRevision (string localPath, Revision revision, IProgressMonitor monitor)
		{
			Svn.RevertRevision (localPath, revision, monitor);
		}

		public override void RevertToRevision (string localPath, Revision revision, IProgressMonitor monitor)
		{
			Svn.RevertToRevision (localPath, revision, monitor);
		}
		
		public override void Add (string[] paths, bool recurse, IProgressMonitor monitor)
		{
			foreach (string path in paths) {
				if (IsVersioned (path) && File.Exists (path) && !Directory.Exists (path)) {
					VersionInfo srcInfo = GetVersionInfo (path, false);
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
					}
				}
				else
					Svn.Add (path, recurse, monitor);
			}
		}
		
		public override bool CanMoveFileFrom (Repository srcRepository, string localSrcPath, string localDestPath)
		{
			return (srcRepository is SubversionRepository) && ((SubversionRepository)srcRepository).Root == Root;
		}
		
		public override void MoveFile (string srcPath, string destPath, bool force, IProgressMonitor monitor)
		{
			bool destIsVersioned = false;
			
			if (File.Exists (destPath))
				throw new InvalidOperationException ("Cannot move file. Destination file already exist.");

			if (IsVersioned (destPath)) {
				// Revert to the original status
				Revert (destPath, false, monitor);
				if (File.Exists (destPath))
					File.Delete (destPath);
				destIsVersioned = true;
			}
			
			VersionInfo srcInfo = GetVersionInfo (srcPath, false);
			if (srcInfo != null && srcInfo.HasLocalChange (VersionStatus.ScheduledAdd)) {
				// If the file is scheduled to add, cancel it, move the file, and schedule to add again
				Revert (srcPath, false, monitor);
				if (!destIsVersioned)
					MakeDirVersioned (Path.GetDirectoryName (destPath), monitor);
				base.MoveFile (srcPath, destPath, force, monitor);
				if (!destIsVersioned)
					Add (destPath, false, monitor);
			} else {
				if (!destIsVersioned && IsVersioned (srcPath)) {
					MakeDirVersioned (Path.GetDirectoryName (destPath), monitor);
					Svn.Move (srcPath, destPath, force, monitor);
				} else
					base.MoveFile (srcPath, destPath, force, monitor);
			}
		}
		
		public override void MoveDirectory (string srcPath, string destPath, bool force, IProgressMonitor monitor)
		{
			if (IsVersioned (destPath))
			{
				VersionInfo vinfo = GetVersionInfo (destPath, false);
				if (!vinfo.HasLocalChange (VersionStatus.ScheduledDelete) && Directory.Exists (destPath))
					throw new InvalidOperationException ("Cannot move directory. Destination directory already exist.");
					
				srcPath = Path.GetFullPath (srcPath);
				
				// The target directory does not exist, but it is versioned. It may be because
				// it is scheduled to delete, or maybe it has been physicaly deleted. In any
				// case we are going to replace the old directory by the new directory.
				
				// Revert the old directory, so we can see which files were there so
				// we can delete or replace them
				Revert (destPath, true, monitor);
				
				// Get the list of files in the directory to be replaced
				ArrayList oldFiles = new ArrayList ();
				GetDirectoryFiles (destPath, oldFiles);
				
				// Get the list of files to move
				ArrayList newFiles = new ArrayList ();
				GetDirectoryFiles (srcPath, newFiles);
				
				// Move all new files to the new destination
				Hashtable copiedFiles = new Hashtable ();
				Hashtable copiedFolders = new Hashtable ();
				foreach (string file in newFiles) {
					string src = Path.GetFullPath (file);
					string dst = Path.Combine (destPath, src.Substring (srcPath.Length + 1));
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
						DeleteFile (oldFile, true, monitor);
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
				DeleteDirectory (srcPath, true, monitor);
			}
			else {
				if (Directory.Exists (destPath))
					throw new InvalidOperationException ("Cannot move directory. Destination directory already exist.");
				
				VersionInfo srcInfo = GetVersionInfo (srcPath, false);
				if (srcInfo != null && srcInfo.HasLocalChange (VersionStatus.ScheduledAdd)) {
					// If the directory is scheduled to add, cancel it, move the directory, and schedule to add it again
					MakeDirVersioned (Path.GetDirectoryName (destPath), monitor);
					Revert (srcPath, true, monitor);
					base.MoveDirectory (srcPath, destPath, force, monitor);
					Add (destPath, true, monitor);
				} else {
					if (IsVersioned (srcPath)) {
						MakeDirVersioned (Path.GetDirectoryName (destPath), monitor);
						Svn.Move (srcPath, destPath, force, monitor);
					} else
						base.MoveDirectory (srcPath, destPath, force, monitor);
				}
			}
		}
		
		void MakeDirVersioned (string dir, IProgressMonitor monitor)
		{
			if (Directory.Exists (Path.Combine (dir, ".svn")))
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

		
		public override void DeleteFiles (string[] paths, bool force, IProgressMonitor monitor)
		{
			foreach (string path in paths) {
				if (IsVersioned (path))
					Svn.Delete (path, force, monitor);
				else {
					VersionInfo srcInfo = GetVersionInfo (path, false);
					if (srcInfo != null && srcInfo.HasLocalChange (VersionStatus.ScheduledAdd)) {
						// Revert the add command
						Revert (path, false, monitor);
					}
					File.Delete (path);
				}
			}
		}
		
		public override void DeleteDirectories (string[] paths, bool force, IProgressMonitor monitor)
		{
			foreach (string path in paths) {
				if (IsVersioned (path))
					Svn.Delete (path, force, monitor);
				else
					Directory.Delete (path, true);
			}
		}
		
		public override DiffInfo[] PathDiff (string baseLocalPath, string[] localPaths, bool remoteDiff)
		{
			if (localPaths != null) {
				ArrayList list = new ArrayList ();
				foreach (string path in localPaths) {
					string diff = Svn.PathDiff (path, false, remoteDiff);
					if (diff == null)
						continue;
					try {
						list.AddRange (GenerateUnifiedDiffInfo (diff, path, new string [] { path }));
					} finally {
						FileService.DeleteFile (diff);
					}
				}
				return (DiffInfo[]) list.ToArray (typeof(DiffInfo));
			} else {
				string diff = Svn.PathDiff (baseLocalPath, true, remoteDiff);
				try {
					return GenerateUnifiedDiffInfo (diff, baseLocalPath, null);
				} finally {
					FileService.DeleteFile (diff);
				}
			}
		}
	}
}
