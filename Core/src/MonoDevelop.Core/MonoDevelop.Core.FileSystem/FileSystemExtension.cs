
using System;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Core.FileSystem
{
	public abstract class FileSystemExtension
	{
		FileSystemExtension next;
		
		internal FileSystemExtension Next {
			get { return next; }
			set { next = value; }
		}
		
		public abstract bool CanHandlePath (string path, bool isDirectory);
		
		FileSystemExtension GetNextForPath (string file, bool isDirectory)
		{
			FileSystemExtension nx = next;
			while (nx != null && !nx.CanHandlePath (file, isDirectory))
				nx = nx.next;
			return nx;
		}
		
		public virtual void CopyFile (string source, string dest, bool overwrite)
		{
			GetNextForPath (dest, false).CopyFile (source, dest, overwrite);
		}
		
		public virtual void RenameFile (string file, string newName)
		{
			GetNextForPath (file, false).RenameFile (file, newName);
		}
		
		public virtual void MoveFile (string source, string dest)
		{
			GetNextForPath (source, false).MoveFile (source, dest);
			
			FileSystemExtension srcExt = GetNextForPath (source, false);
			FileSystemExtension dstExt = GetNextForPath (dest, false);
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveFile (source, dest);
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyFile (source, dest, true);
				srcExt.DeleteFile (source);
			}
		}
		
		public virtual void DeleteFile (string file)
		{
			GetNextForPath (file, false).DeleteFile (file);
		}
		
		public virtual void CreateDirectory (string path)
		{
			GetNextForPath (path, true).CreateDirectory (path);
		}
		
		public virtual void CopyDirectory (string sourcePath, string destPath)
		{
			GetNextForPath (destPath, true).CopyDirectory (sourcePath, destPath);
		}
		
		public virtual void RenameDirectory (string path, string newName)
		{
			GetNextForPath (path, true).RenameDirectory (path, newName);
		}
		
		public virtual void MoveDirectory (string sourcePath, string destPath)
		{
			FileSystemExtension srcExt = GetNextForPath (sourcePath, true);
			FileSystemExtension dstExt = GetNextForPath (destPath, true);
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveDirectory (sourcePath, destPath);
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyDirectory (sourcePath, destPath);
				srcExt.DeleteDirectory (sourcePath);
			}
		}
		
		public virtual void DeleteDirectory (string path)
		{
			GetNextForPath (path, true).DeleteDirectory (path);
		}
		
		public virtual bool RequestFileEdit (string file)
		{
			return GetNextForPath (file, false).RequestFileEdit (file);
		}
		
		public virtual void NotifyFileChanged (string file)
		{
			GetNextForPath (file, false).NotifyFileChanged (file);
		}
	}
}
