
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.FileSystem;
using MonoDevelop.Ide;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.VersionControl
{
	internal class VersionControlFileSystemExtension: FileSystemExtension
	{
		public override bool CanHandlePath (FilePath path, bool isDirectory)
		{
			// FIXME: don't load this extension if the ide is not loaded.
			if (IdeApp.ProjectOperations == null || !IdeApp.Workspace.IsOpen)
				return false;
			else
				return GetRepository (path) != null;
		}
		
		Repository GetRepository (FilePath path)
		{
			path = path.FullPath;
			
			Project p = IdeApp.Workspace.GetProjectContainingFile (path);
			if (p != null)
				return VersionControlService.GetRepository (p);
			
			foreach (Project prj in IdeApp.Workspace.GetAllProjects ()) {
				if (path == prj.BaseDirectory || path.IsChildPathOf (prj.BaseDirectory))
					return VersionControlService.GetRepository (prj);
			}
			return null;
		}
		
		public override void CopyFile (FilePath source, FilePath dest, bool overwrite)
		{
			Repository repo = GetRepository (dest);
			if (repo.RequestFileWritePermission (dest)) {
				base.CopyFile (source, dest, overwrite);
				repo.NotifyFileChanged (dest);
			}
			else
				throw new System.IO.IOException ("Write permission denied");
		}
		
		public override void MoveFile (FilePath source, FilePath dest)
		{
			IProgressMonitor monitor = new NullProgressMonitor ();
			
			Repository srcRepo = GetRepository (source);
			Repository dstRepo = GetRepository (dest);
			
			if (dstRepo.CanMoveFilesFrom (srcRepo, source, dest))
				srcRepo.MoveFile (source, dest, true, monitor);
			else {
				CopyFile (source, dest, true);
				srcRepo.DeleteFile (source, true, monitor);
			}
		}
		
		public override void RenameDirectory (FilePath path, string newName)
		{
			MoveDirectory (path, path.ParentDirectory.Combine (newName));
		}

		public override void RenameFile (FilePath file, string newName)
		{
			MoveFile (file, file.ParentDirectory.Combine (newName));
		}

		public override void DeleteFile (FilePath file)
		{
			Repository repo = GetRepository (file);
			repo.DeleteFile (file, true, new NullProgressMonitor ());
		}
		
		public override void CreateDirectory (FilePath path)
		{
			Repository repo = GetRepository (path);
			repo.CreateLocalDirectory (path);
			repo.Add (path, false, new NullProgressMonitor ());
		}
		
		public override void MoveDirectory (FilePath sourcePath, FilePath destPath)
		{
			IProgressMonitor monitor = new NullProgressMonitor ();
			
			Repository srcRepo = GetRepository (sourcePath);
			Repository dstRepo = GetRepository (destPath);
			
			if (dstRepo.CanMoveFilesFrom (srcRepo, sourcePath, destPath))
				srcRepo.MoveDirectory (sourcePath, destPath, true, monitor);
			else {
				CopyDirectory (sourcePath, destPath);
				srcRepo.DeleteDirectory (sourcePath, true, monitor);
			}
		}
		
		public override void DeleteDirectory (FilePath path)
		{
			Repository repo = GetRepository (path);
			repo.DeleteDirectory (path, true, new NullProgressMonitor ());
		}
		
		public override bool RequestFileEdit (FilePath file)
		{
			Repository repo = GetRepository (file);
			return repo.RequestFileWritePermission (file);
		}
		
		public override void NotifyFilesChanged (IEnumerable<FilePath> files)
		{
			FileUpdateEventArgs args = new FileUpdateEventArgs ();
			args.AddRange (files.Select (f => {
				var rep = GetRepository (f);
				rep.ClearCachedVersionInfo (f);
				return new FileUpdateEventInfo (rep, f, false);
			}));
			VersionControlService.NotifyFileStatusChanged (args);
		}
	}
}
