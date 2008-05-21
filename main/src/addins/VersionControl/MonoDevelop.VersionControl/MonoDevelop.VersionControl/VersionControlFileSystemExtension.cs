
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.FileSystem;

namespace MonoDevelop.VersionControl
{
	internal class VersionControlFileSystemExtension: FileSystemExtension
	{
		public override bool CanHandlePath (string path, bool isDirectory)
		{
			// FIXME: don't load this extension if the ide is not loaded.
			if (IdeApp.ProjectOperations == null || !IdeApp.Workspace.IsOpen)
				return false;
			else
				return GetRepository (path) != null;
		}
		
		Repository GetRepository (string path)
		{
			// FIXME: Optimize
			foreach (Project prj in IdeApp.Workspace.GetAllProjects ()) {
				if (path.StartsWith (prj.BaseDirectory)) {
					return VersionControlService.GetRepository (prj);
				}
			}
			return null;
		}
		
		public override void CopyFile (string source, string dest, bool overwrite)
		{
			Repository repo = GetRepository (dest);
			if (repo.RequestFileWritePermission (dest)) {
				base.CopyFile (source, dest, overwrite);
				repo.NotifyFileChanged (dest);
			}
			else
				throw new System.IO.IOException ("Write permission denied");
		}
		
		public override void MoveFile (string source, string dest)
		{
			IProgressMonitor monitor = new NullProgressMonitor ();
			
			Repository srcRepo = GetRepository (source);
			Repository dstRepo = GetRepository (dest);
			
			if (dstRepo.CanMoveFileFrom (srcRepo, source, dest))
				srcRepo.MoveFile (source, dest, true, monitor);
			else {
				CopyFile (source, dest, true);
				srcRepo.DeleteFile (source, true, monitor);
			}
		}
		
		public override void DeleteFile (string file)
		{
			Repository repo = GetRepository (file);
			repo.DeleteFile (file, true, new NullProgressMonitor ());
		}
		
		public override void CreateDirectory (string path)
		{
			Repository repo = GetRepository (path);
			repo.CreateLocalDirectory (path);
		}
		
		public override void MoveDirectory (string sourcePath, string destPath)
		{
			IProgressMonitor monitor = new NullProgressMonitor ();
			
			Repository srcRepo = GetRepository (sourcePath);
			Repository dstRepo = GetRepository (destPath);
			
			if (srcRepo == dstRepo)
				srcRepo.MoveDirectory (sourcePath, destPath, true, monitor);
			else {
				CopyDirectory (sourcePath, destPath);
				srcRepo.DeleteDirectory (sourcePath, true, monitor);
			}
		}
		
		public override void DeleteDirectory (string path)
		{
			Repository repo = GetRepository (path);
			repo.DeleteDirectory (path, true, new NullProgressMonitor ());
		}
		
		public override bool RequestFileEdit (string file)
		{
			Repository repo = GetRepository (file);
			return repo.RequestFileWritePermission (file);
		}
		
		public override void NotifyFileChanged (string file)
		{
			Repository repo = GetRepository (file);
			VersionControlService.NotifyFileStatusChanged (repo, file, false);
		}
	}
}
