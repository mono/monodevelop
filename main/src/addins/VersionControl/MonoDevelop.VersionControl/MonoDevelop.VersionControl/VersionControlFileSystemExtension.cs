
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.FileSystem;
using MonoDevelop.Ide;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	internal class VersionControlFileSystemExtension: FileSystemExtension
	{
		public override bool CanHandlePath (FilePath path, bool isDirectory)
		{
			// FIXME: don't load this extension if the ide is not loaded.
			if (!IdeApp.IsInitialized || !IdeApp.Workspace.IsOpen)
				return false;

			return GetRepository (path) != null;
		}
		
		static Repository GetRepository (FilePath path)
		{
			path = path.FullPath;
			
			Project p = IdeApp.Workspace.GetProjectsContainingFile (path).FirstOrDefault ();
			if (p != null)
				return VersionControlService.GetRepository (p);
			
			foreach (Project prj in IdeApp.Workspace.GetAllProjects ()) {
				if (path == prj.BaseDirectory || path.IsChildPathOf (prj.BaseDirectory))
					return VersionControlService.GetRepository (prj);
			}
			foreach (Solution sol in IdeApp.Workspace.GetAllSolutions ()) {
				if (path == sol.BaseDirectory || path.IsChildPathOf (sol.BaseDirectory))
					return VersionControlService.GetRepository (sol);
			}
			return null;
		}
		
		public override void CopyFile (FilePath source, FilePath dest, bool overwrite)
		{
			base.CopyFile (source, dest, overwrite);
			Task.Run (async () => {
				Repository repo = GetRepository (dest);
				if (await repo.RequestFileWritePermissionAsync (dest)) {
					LoggingService.LogError ("Write permission denied.");
					return;
				}
				repo.NotifyFileChanged (dest);
			});
		}
		
		public override void MoveFile (FilePath source, FilePath dest)
		{
			ProgressMonitor monitor = new ProgressMonitor ();

			Repository srcRepo = GetRepository (source);
			Repository dstRepo = GetRepository (dest);
			
			if (dstRepo != null && dstRepo.CanMoveFilesFrom (srcRepo, source, dest))
				srcRepo.MoveFileAsync (source, dest, true, monitor);
			else {
				CopyFile (source, dest, true);
				srcRepo.DeleteFileAsync (source, true, monitor, false);
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
			repo.DeleteFileAsync (file, true, new ProgressMonitor (), false);
		}
		
		public override void CreateDirectory (FilePath path)
		{
			Repository repo = GetRepository (path);
			repo.ClearCachedVersionInfo (path);
			System.IO.Directory.CreateDirectory (path);
			repo.Add (path, false, new ProgressMonitor ());
		}
		
		public override void MoveDirectory (FilePath sourcePath, FilePath destPath)
		{
			ProgressMonitor monitor = new ProgressMonitor ();
			
			Repository srcRepo = GetRepository (sourcePath);
			Repository dstRepo = GetRepository (destPath);
			
			if (dstRepo.CanMoveFilesFrom (srcRepo, sourcePath, destPath))
				srcRepo.MoveDirectoryAsync (sourcePath, destPath, true, monitor);
			else {
				CopyDirectory (sourcePath, destPath);
				srcRepo.DeleteDirectory (sourcePath, true, monitor, false);
			}
		}

		public override void DeleteDirectory (FilePath path)
		{
			Repository repo = GetRepository (path);
			repo.DeleteDirectory (path, true, new ProgressMonitor (), false);
		}

		public override void RequestFileEdit (FilePath file)
		{
			Repository repo = GetRepository (file.FullPath);
			Task.Run (async () => await repo.RequestFileWritePermissionAsync (file));
		}
		
		public override void NotifyFilesChanged (IEnumerable<FilePath> files)
		{
			FileUpdateEventArgs args = new FileUpdateEventArgs ();
			foreach (var file in files) {
				var rep = GetRepository (file);
				if (rep != null && !rep.IsDisposed) {
					rep.ClearCachedVersionInfo (file);
					if (rep.TryGetFileUpdateEventInfo (rep, file, out var eventInfo))
						args.Add (eventInfo);
				}
			}
			VersionControlService.NotifyFileStatusChanged (args);
		}
	}
}
