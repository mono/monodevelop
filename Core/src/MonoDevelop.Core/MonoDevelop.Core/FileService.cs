// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Xml;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.FileSystem;

namespace MonoDevelop.Core
{
	public class FileService
	{
		FileServiceErrorHandler errorHandler;
		const string FileSystemExtensionPath = "/Workspace/FileSystemExtensions";
		
		FileSystemExtension fileSystemChain;
		FileSystemExtension defaultExtension = new DefaultFileSystemExtension ();
		
		readonly static char[] separators = { Path.DirectorySeparatorChar, Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar };
		string sharpDevelopRootPath;
		
		public FileService()
		{
			sharpDevelopRootPath = PropertyService.EntryAssemblyDirectory + Path.DirectorySeparatorChar + "..";
			Runtime.AddInService.ExtensionChanged += delegate (string path) {
				if (path == FileSystemExtensionPath)
					UpdateExtensions ();
			};
		}
		
		void UpdateExtensions ()
		{
			if (!Runtime.Initialized) {
				fileSystemChain = defaultExtension;
				return;
			}
			
			FileSystemExtension[] extensions = (FileSystemExtension[]) Runtime.AddInService.GetTreeItems (FileSystemExtensionPath, typeof(FileSystemExtension));
			for (int n=0; n<extensions.Length - 1; n++)
				extensions [n].Next = extensions [n + 1];

			if (extensions.Length > 0) {
				extensions [extensions.Length - 1].Next = defaultExtension;
				fileSystemChain = extensions [0];
			} else
				fileSystemChain = defaultExtension;
		}
		
		public string SharpDevelopRootPath {
			get {
				return sharpDevelopRootPath;
			}
		}
		
		public void DeleteFile (string fileName)
		{
			try {
				GetFileSystemForPath (fileName, false).DeleteFile (fileName);
			} catch (Exception e) {
				if (!HandleError (GettextCatalog.GetString ("Can't remove file {0}"), e))
					throw;
				return;
			}
			OnFileRemoved (new FileEventArgs (fileName, false));
		}
		
		public void DeleteDirectory (string path)
		{
			try {
				GetFileSystemForPath (path, true).DeleteDirectory (path);
			} catch (Exception e) {
				if (!HandleError (String.Format (GettextCatalog.GetString ("Can't remove directory {0}"), path), e))
					throw;
				return;
			}
			OnFileRemoved (new FileEventArgs (path, true));
		}
		
		public void RenameFile (string filePath, string newName)
		{
			if (Path.GetFileName (filePath) != newName) {
				string newPath = Path.Combine (Path.GetDirectoryName (filePath), newName);
				MoveFile (filePath, newPath);
				OnFileRenamed (new FileEventArgs (filePath, newPath, false));
			}
		}
		
		public void RenameDirectory (string path, string newName)
		{
			if (Path.GetFileName (path) != newName) {
				string newPath = Path.Combine (Path.GetDirectoryName (path), newName);
				MoveDirectory (path, newPath);
				OnFileRenamed (new FileEventArgs (path, newPath, true));
			}
		}
		
		public void CopyFile (string sourcePath, string destPath)
		{
			GetFileSystemForPath (destPath, false).CopyFile (sourcePath, destPath, true);
			OnFileCreated (new FileEventArgs (destPath, false));
		}

		public void MoveFile (string sourcePath, string destPath)
		{
			FileSystemExtension srcExt = GetFileSystemForPath (sourcePath, false);
			FileSystemExtension dstExt = GetFileSystemForPath (destPath, false);
			
			Console.WriteLine ("SRC:" + srcExt + " " + srcExt.GetHashCode ());
			Console.WriteLine ("DST:" + dstExt + " " + dstExt.GetHashCode ());
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveFile (sourcePath, destPath);
				OnFileCreated (new FileEventArgs (destPath, false));
				OnFileRemoved (new FileEventArgs (destPath, false));
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyFile (sourcePath, destPath, true);
				OnFileCreated (new FileEventArgs (destPath, false));
				srcExt.DeleteFile (sourcePath);
				OnFileRemoved (new FileEventArgs (destPath, false));
			}
		}
		
		public void CreateDirectory (string path)
		{
			GetFileSystemForPath (path, true).CreateDirectory (path);
			OnFileCreated (new FileEventArgs (path, true));
		}
		
		public void CopyDirectory (string sourcePath, string destPath)
		{
			GetFileSystemForPath (destPath, true).CopyDirectory (sourcePath, destPath);
		}
		
		public void MoveDirectory (string sourcePath, string destPath)
		{
			FileSystemExtension srcExt = GetFileSystemForPath (sourcePath, true);
			FileSystemExtension dstExt = GetFileSystemForPath (destPath, true);
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveDirectory (sourcePath, destPath);
				OnFileCreated (new FileEventArgs (destPath, true));
				OnFileRemoved (new FileEventArgs (sourcePath, true));
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyDirectory (sourcePath, destPath);
				srcExt.DeleteDirectory (sourcePath);
				OnFileRemoved (new FileEventArgs (destPath, true));
			}
		}
		
		public bool RequestFileEdit (string file)
		{
			return GetFileSystemForPath (file, false).RequestFileEdit (file);
		}
		
		public void NotifyFileChanged (string path)
		{
			GetFileSystemForPath (path, false).NotifyFileChanged (path);
			OnFileChanged (new FileEventArgs (path, false));
		}
		
		internal FileSystemExtension GetFileSystemForPath (string path, bool isDirectory)
		{
			if (fileSystemChain == null)
				UpdateExtensions ();
			FileSystemExtension nx = fileSystemChain;
			while (nx != null && !nx.CanHandlePath (path, isDirectory))
				nx = nx.Next;
			return nx;
		}
		
		
		public StringCollection SearchDirectory(string directory, string filemask, bool searchSubdirectories)
		{
			StringCollection collection = new StringCollection();
			SearchDirectory(directory, filemask, collection, searchSubdirectories);
			return collection;
		}
		
		public StringCollection SearchDirectory(string directory, string filemask)
		{
			return SearchDirectory (directory, filemask, true);
		}
		
		/// <summary>
		/// Finds all files which are valid to the mask <code>filemask</code> in the path
		/// <code>directory</code> and all subdirectories (if searchSubdirectories
		/// is true. The found files are added to the StringCollection 
		/// <code>collection</code>.
		/// </summary>
		void SearchDirectory (string directory, string filemask, StringCollection collection, bool searchSubdirectories)
		{
			string[] file = Directory.GetFiles(directory, filemask);
			foreach (string f in file) {
				collection.Add(f);
			}
			
			if (searchSubdirectories) {
				string[] dir = Directory.GetDirectories(directory);
				foreach (string d in dir) {
					SearchDirectory(d, filemask, collection, searchSubdirectories);
				}
			}
		}
		
		/// <summary>
		/// Converts a given absolute path and a given base path to a path that leads
		/// from the base path to the absoulte path. (as a relative path)
		/// </summary>
		public string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (! Path.IsPathRooted (absPath))
				return absPath;
			
			absPath = Path.GetFullPath (absPath);
			baseDirectoryPath = Path.GetFullPath (baseDirectoryPath);
			
			string[] bPath = baseDirectoryPath.Split (separators);
			string[] aPath = absPath.Split (separators);
			int indx = 0;
			for(; indx < Math.Min(bPath.Length, aPath.Length); ++indx){
				if(!bPath[indx].Equals(aPath[indx]))
					break;
			}
			
			if (indx == 0) {
				return absPath;
			}
			
			string erg = "";
			
			if(indx == bPath.Length) {
				erg += "." + Path.DirectorySeparatorChar;
			} else {
				for (int i = indx; i < bPath.Length; ++i) {
					erg += ".." + Path.DirectorySeparatorChar;
				}
			}
			erg += String.Join(Path.DirectorySeparatorChar.ToString(), aPath, indx, aPath.Length-indx);
			
			return erg;
		}
		
		/// <summary>
		/// Converts a given relative path and a given base path to a path that leads
		/// to the relative path absoulte.
		/// </summary>
		public string RelativeToAbsolutePath (string baseDirectoryPath, string relPath)
		{			
			return Path.GetFullPath (baseDirectoryPath + Path.DirectorySeparatorChar + relPath);
		}
		
		/// <summary>
		/// This method checks the file fileName if it is valid.
		/// </summary>
		public bool IsValidFileName (string fileName)
		{
			// Fixme: 260 is the hardcoded maximal length for a path on my Windows XP system
			//        I can't find a .NET property or method for determining this variable.
			if (String.IsNullOrEmpty (fileName) || fileName.Length >= 260) {
				return false;
			}
			
			// platform independent : check for invalid path chars
			foreach (char invalidChar in Path.GetInvalidPathChars()) {
				if (fileName.IndexOf(invalidChar) >= 0) {
					return false;
				}
			}
			
			// platform dependend : Check for invalid file names (DOS)
			// this routine checks for follwing bad file names :
			// CON, PRN, AUX, NUL, COM1-9 and LPT1-9
			
			string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
			if (nameWithoutExtension != null) {
				nameWithoutExtension = nameWithoutExtension.ToUpper();
			}
			
			if (nameWithoutExtension == "CON" ||
			    nameWithoutExtension == "PRN" ||
			    nameWithoutExtension == "AUX" ||
			    nameWithoutExtension == "NUL") {
		    	
		    	return false;
		    }
			    
		    char ch = nameWithoutExtension.Length == 4 ? nameWithoutExtension[3] : '\0';
			
			return !((nameWithoutExtension.StartsWith("COM") ||
			          nameWithoutExtension.StartsWith("LPT")) &&
			          Char.IsDigit(ch));
		}
		
		public bool IsDirectory (string filename)
		{
			if (!Directory.Exists(filename)) {
				return false;
			}
			FileAttributes attr = File.GetAttributes(filename);
			return (attr & FileAttributes.Directory) != 0;
		}
		
		/// <summary>
		/// Returns directoryName + "\\" (Win32) when directoryname doesn't end with
		/// "\\"
		/// </summary>
		public string GetDirectoryNameWithSeparator (string directoryName)
		{
			if (directoryName == null) return "";
			
			if (directoryName.EndsWith(Path.DirectorySeparatorChar.ToString())) {
				return directoryName;
			}
			return directoryName + Path.DirectorySeparatorChar;
		}
		
		
		bool HandleError (string message, Exception ex)
		{
			if (errorHandler != null)
				return errorHandler (message, ex);
			else
				return false;
		}
		
		protected virtual void OnFileCreated (FileEventArgs e)
		{
			if (FileCreated != null) {
				FileCreated (this, e);
			}
		}
		
		protected virtual void OnFileRemoved (FileEventArgs e)
		{
			if (FileRemoved != null) {
				FileRemoved(this, e);
			}
		}

		protected virtual void OnFileRenamed (FileEventArgs e)
		{
			if (FileRenamed != null) {
				FileRenamed(this, e);
			}
		}

		protected virtual void OnFileChanged (FileEventArgs e)
		{
			if (FileChanged != null) {
				FileChanged(this, e);
			}
		}
		
		public FileServiceErrorHandler ErrorHandler {
			get { return errorHandler; }
			set { errorHandler = value; }
		}

		public event FileEventHandler FileCreated;
		public event FileEventHandler FileRenamed;
		public event FileEventHandler FileRemoved;
		public event FileEventHandler FileChanged;
	}
	
	public delegate bool FileServiceErrorHandler (string message, Exception ex);
}
