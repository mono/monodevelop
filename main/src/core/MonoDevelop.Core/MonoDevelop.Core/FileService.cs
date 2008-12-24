//
// FileService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core.FileSystem;

namespace MonoDevelop.Core
{
	public static class FileService
	{
		const string addinFileSystemExtensionPath = "/MonoDevelop/Core/FileSystemExtensions";
		readonly static char[] separators = { Path.DirectorySeparatorChar, Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar };
		
		static FileServiceErrorHandler errorHandler;
		
		static FileSystemExtension fileSystemChain;
		static FileSystemExtension defaultExtension = new DefaultFileSystemExtension ();
		
		static string applicationRootPath = Path.Combine (PropertyService.EntryAssemblyPath, "..");
		public static string ApplicationRootPath {
			get {
				return applicationRootPath;
			}
		}
		
		static FileService()
		{
			AddinManager.ExtensionChanged += delegate (object sender, ExtensionEventArgs args) {
				if (args.PathChanged (addinFileSystemExtensionPath))
					UpdateExtensions ();
			};
		}
		
		static void UpdateExtensions ()
		{
			if (!Runtime.Initialized) {
				fileSystemChain = defaultExtension;
				return;
			}
			
			FileSystemExtension[] extensions = (FileSystemExtension[]) AddinManager.GetExtensionObjects (addinFileSystemExtensionPath, typeof(FileSystemExtension));
			for (int n=0; n<extensions.Length - 1; n++) {
				extensions [n].Next = extensions [n + 1];
			}

			if (extensions.Length > 0) {
				extensions [extensions.Length - 1].Next = defaultExtension;
				fileSystemChain = extensions [0];
			} else {
				fileSystemChain = defaultExtension;
			}
		}
		
		public static void DeleteFile (string fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			try {
				GetFileSystemForPath (fileName, false).DeleteFile (fileName);
			} catch (Exception e) {
				if (!HandleError (GettextCatalog.GetString ("Can't remove file {0}", fileName), e))
					throw;
				return;
			}
			OnFileRemoved (new FileEventArgs (fileName, false));
		}
		
		public static void DeleteDirectory (string path)
		{
			Debug.Assert (!String.IsNullOrEmpty (path));
			try {
				GetFileSystemForPath (path, true).DeleteDirectory (path);
			} catch (Exception e) {
				if (!HandleError (GettextCatalog.GetString ("Can't remove directory {0}", path), e))
					throw;
				return;
			}
			OnFileRemoved (new FileEventArgs (path, true));
		}
		
		public static void RenameFile (string oldName, string newName)
		{
			Debug.Assert (!String.IsNullOrEmpty (oldName));
			Debug.Assert (!String.IsNullOrEmpty (newName));
			if (Path.GetFileName (oldName) != newName) {
				string newPath = Path.Combine (Path.GetDirectoryName (oldName), newName);
				InternalMoveFile (oldName, newPath);
				OnFileRenamed (new FileCopyEventArgs (oldName, newPath, false));
				OnFileCreated (new FileEventArgs (newPath, false));
				OnFileRemoved (new FileEventArgs (oldName, false));
			}
		}
		
		public static void RenameDirectory (string oldName, string newName)
		{
			Debug.Assert (!String.IsNullOrEmpty (oldName));
			Debug.Assert (!String.IsNullOrEmpty (newName));
			if (Path.GetFileName (oldName) != newName) {
				string newPath = Path.Combine (Path.GetDirectoryName (oldName), newName);
				InternalMoveDirectory (oldName, newPath);
				OnFileRenamed (new FileCopyEventArgs (oldName, newPath, true));
				OnFileCreated (new FileEventArgs (newPath, false));
				OnFileRemoved (new FileEventArgs (oldName, false));
			}
		}
		
		public static void CopyFile (string srcFile, string dstFile)
		{
			Debug.Assert (!String.IsNullOrEmpty (srcFile));
			Debug.Assert (!String.IsNullOrEmpty (dstFile));
			GetFileSystemForPath (dstFile, false).CopyFile (srcFile, dstFile, true);
			OnFileCreated (new FileEventArgs (dstFile, false));
		}

		public static void MoveFile (string srcFile, string dstFile)
		{
			Debug.Assert (!String.IsNullOrEmpty (srcFile));
			Debug.Assert (!String.IsNullOrEmpty (dstFile));
			InternalMoveFile (srcFile, dstFile);
			OnFileCreated (new FileEventArgs (dstFile, false));
			OnFileRemoved (new FileEventArgs (srcFile, false));
		}
		
		static void InternalMoveFile (string srcFile, string dstFile)
		{
			Debug.Assert (!String.IsNullOrEmpty (srcFile));
			Debug.Assert (!String.IsNullOrEmpty (dstFile));
			FileSystemExtension srcExt = GetFileSystemForPath (srcFile, false);
			FileSystemExtension dstExt = GetFileSystemForPath (dstFile, false);
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveFile (srcFile, dstFile);
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyFile (srcFile, dstFile, true);
				srcExt.DeleteFile (srcFile);
			}
		}
		
		public static void CreateDirectory (string path)
		{
			Debug.Assert (!String.IsNullOrEmpty (path));
			GetFileSystemForPath (path, true).CreateDirectory (path);
			OnFileCreated (new FileEventArgs (path, true));
		}
		
		public static void CopyDirectory (string srcPath, string dstPath)
		{
			Debug.Assert (!String.IsNullOrEmpty (srcPath));
			Debug.Assert (!String.IsNullOrEmpty (dstPath));
			GetFileSystemForPath (dstPath, true).CopyDirectory (srcPath, dstPath);
		}
		
		public static void MoveDirectory (string srcPath, string dstPath)
		{
			Debug.Assert (!String.IsNullOrEmpty (srcPath));
			Debug.Assert (!String.IsNullOrEmpty (dstPath));
			InternalMoveDirectory (srcPath, dstPath);
			OnFileCreated (new FileEventArgs (dstPath, true));
			OnFileRemoved (new FileEventArgs (srcPath, true));
		}
		
		static void InternalMoveDirectory (string srcPath, string dstPath)
		{
			Debug.Assert (!String.IsNullOrEmpty (srcPath));
			Debug.Assert (!String.IsNullOrEmpty (dstPath));
			FileSystemExtension srcExt = GetFileSystemForPath (srcPath, true);
			FileSystemExtension dstExt = GetFileSystemForPath (dstPath, true);
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveDirectory (srcPath, dstPath);
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyDirectory (srcPath, dstPath);
				srcExt.DeleteDirectory (srcPath);
			}
		}
		
		public static bool RequestFileEdit (string fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			return GetFileSystemForPath (fileName, false).RequestFileEdit (fileName);
		}
		
		public static void NotifyFileChanged (string fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			try {
				GetFileSystemForPath (fileName, false).NotifyFileChanged (fileName);
				OnFileChanged (new FileEventArgs (fileName, false));
			} catch (Exception ex) {
				LoggingService.LogError ("File change notification failed", ex);
			}
		}
		
		internal static FileSystemExtension GetFileSystemForPath (string path, bool isDirectory)
		{
			Debug.Assert (!String.IsNullOrEmpty (path));
			if (fileSystemChain == null)
				UpdateExtensions ();
			FileSystemExtension nx = fileSystemChain;
			while (nx != null && !nx.CanHandlePath (path, isDirectory))
				nx = nx.Next;
			return nx;
		}
		
		public static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (!Path.IsPathRooted (absPath))
				return absPath;
			
			absPath           = Path.GetFullPath (absPath);
			baseDirectoryPath = Path.GetFullPath (baseDirectoryPath);
			
			string[] bPath = baseDirectoryPath.Split (separators);
			string[] aPath = absPath.Split (separators);
			int indx = 0;
			
			for (; indx < Math.Min (bPath.Length, aPath.Length); indx++) {
				if (!bPath[indx].Equals(aPath[indx]))
					break;
			}
			
			if (indx == 0) 
				return absPath;
			
			StringBuilder result = new StringBuilder ();
			
			for (int i = indx; i < bPath.Length; i++) {
				result.Append ("..");
				if (i + 1 < bPath.Length || aPath.Length - indx > 0)
					result.Append (Path.DirectorySeparatorChar);
			}
			
			
			result.Append (String.Join(Path.DirectorySeparatorChar.ToString(), aPath, indx, aPath.Length - indx));
			if (result.Length == 0)
				return ".";
			return result.ToString ();
		}
		
		public static string RelativeToAbsolutePath (string baseDirectoryPath, string relPath)
		{
			return Path.GetFullPath (Path.Combine (baseDirectoryPath, relPath));
		}
		
		public static bool IsValidPath (string fileName)
		{
			if (String.IsNullOrEmpty (fileName) || fileName.Trim() == string.Empty) 
				return false;
			if (fileName.IndexOfAny (Path.GetInvalidPathChars ()) >= 0)
				return false;
			if(fileName.IndexOfAny("$#@!%^&*?|'\";:}{".ToCharArray()) > -1)
				return false;
			return true;
		}

		public static bool IsValidFileName (string fileName)
		{
			if (String.IsNullOrEmpty (fileName) || fileName.Trim() == string.Empty) 
				return false;
			if (fileName.IndexOfAny (Path.GetInvalidPathChars ()) >= 0)
				return false;
			if(fileName.IndexOfAny("$#@!%^&*/?\\|'\";:}{".ToCharArray()) > -1)
				return false;
			return true;
		}
		
		public static bool IsDirectory (string filename)
		{
			return Directory.Exists (filename) && (File.GetAttributes (filename) & FileAttributes.Directory) != 0;
		}
		
		public static string GetFullPath (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			// Note: It's not required for Path.GetFullPath (path) that path exists.
			return Path.GetFullPath (path); 
		}
		
		public static string CreateTempDirectory ()
		{
			Random rnd = new Random ();
			string result;
			while (true) {
				result = Path.Combine (Path.GetTempPath (), "mdTmpDir" + rnd.Next ());
				if (!Directory.Exists (result))
					break;
			} 
			Directory.CreateDirectory (result);
			return result;
		}

		public static string NormalizeRelativePath (string path)
		{
			string result = path.Trim (Path.DirectorySeparatorChar, ' ');
			while (result.StartsWith ("." + Path.DirectorySeparatorChar)) {
				result = result.Substring (2);
				result = result.Trim (Path.DirectorySeparatorChar);
			}
			return result == "." ? "" : result;
		}
		
		static bool HandleError (string message, Exception ex)
		{
			return errorHandler != null ? errorHandler (message, ex) : false;
		}
		
		public static FileServiceErrorHandler ErrorHandler {
			get { return errorHandler; }
			set { errorHandler = value; }
		}

		public static event EventHandler<FileEventArgs> FileCreated;
		static void OnFileCreated (FileEventArgs args)
		{
			if (FileCreated != null) 
				FileCreated (null, args);
		}
		
		public static event EventHandler<FileCopyEventArgs> FileRenamed;
		static void OnFileRenamed (FileCopyEventArgs args)
		{
			if (FileRenamed != null) 
				FileRenamed (null, args);
		}
		
		public static event EventHandler<FileEventArgs> FileRemoved;
		static void OnFileRemoved (FileEventArgs args)
		{
			if (FileRemoved != null) 
				FileRemoved (null, args);
		}
		
		public static event EventHandler<FileEventArgs> FileChanged;
		static void OnFileChanged (FileEventArgs args)
		{
			if (FileChanged != null) 
				FileChanged (null, args);
		}
	}
	
	public delegate bool FileServiceErrorHandler (string message, Exception ex);
}
