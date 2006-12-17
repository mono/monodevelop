
using System;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Core.FileSystem
{
	public class DefaultFileSystemExtension: FileSystemExtension
	{
		public override bool CanHandlePath (string path, bool isDirectory)
		{
			return true;
		}
		
		public override void CopyFile (string source, string dest, bool overwrite)
		{
			File.Copy (source, dest, overwrite);
		}
		
		public override void RenameFile (string file, string newName)
		{
			File.Move (file, newName);
		}
		
		public override void MoveFile (string source, string dest)
		{
			File.Move (source, dest);
		}
		
		public override void DeleteFile (string file)
		{
			File.Delete (file);
		}
		
		public override void CreateDirectory (string path)
		{
			Directory.CreateDirectory (path);
		}
		
		public override void CopyDirectory (string sourcePath, string destPath)
		{
			CopyDirectory (sourcePath, destPath, "");
		}
		
		void CopyDirectory (string src, string dest, string subdir)
		{
			string destDir = Path.Combine (dest, subdir);
	
			if (!Directory.Exists (destDir))
				Runtime.FileService.CreateDirectory (destDir);
	
			foreach (string file in Directory.GetFiles (src))
				Runtime.FileService.CopyFile (file, Path.Combine (destDir, Path.GetFileName (file)));
	
			foreach (string dir in Directory.GetDirectories (src))
				CopyDirectory (dir, dest, Path.Combine (subdir, Path.GetFileName (dir)));
		}
		
		public override void RenameDirectory (string path, string newName)
		{
			Directory.Move (path, newName);
		}
		
		public override void MoveDirectory (string source, string dest)
		{
			Directory.Move (source, dest);
		}
		
		public override void DeleteDirectory (string path)
		{
			Directory.Delete (path, true);
		}
		
		public override bool RequestFileEdit (string file)
		{
			return true;
		}
		
		public override void NotifyFileChanged (string file)
		{
		}
	}
}
