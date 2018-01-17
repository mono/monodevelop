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
using Mono.Unix.Native;
using MonoDevelop.Core.FileSystem;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace MonoDevelop.Core
{
	public static class FileService
	{
		delegate bool PathCharsAreEqualDelegate (char a, char b);

		static PathCharsAreEqualDelegate PathCharsAreEqual = Platform.IsWindows || Platform.IsMac ?
			(PathCharsAreEqualDelegate) PathCharsAreEqualCaseInsensitive :
			(PathCharsAreEqualDelegate) PathCharsAreEqualCaseSensitive;

		const string addinFileSystemExtensionPath = "/MonoDevelop/Core/FileSystemExtensions";

		static FileServiceErrorHandler errorHandler;

		static FileSystemExtension fileSystemChain;
		static readonly FileSystemExtension defaultExtension = new DefaultFileSystemExtension ();

		static readonly EventQueue eventQueue = new EventQueue ();

		static readonly string applicationRootPath = Path.Combine (PropertyService.EntryAssemblyPath, "..");
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
			UpdateExtensions ();
		}

		static void UpdateExtensions ()
		{
			if (!Runtime.Initialized) {
				fileSystemChain = defaultExtension;
				return;
			}

			var extensions = AddinManager.GetExtensionObjects (addinFileSystemExtensionPath, typeof(FileSystemExtension)).Cast<FileSystemExtension> ().ToArray ();
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

		/// <summary>
		/// Returns true if the folder is in a case sensitive file system
		/// </summary>
		public static bool IsFolderCaseSensitive (FilePath path)
		{
			var testFile = path.Combine (Guid.NewGuid ().ToString ().ToLower ());
			try {
				File.WriteAllText (testFile, "");
				return !File.Exists (testFile.ToString ().ToUpper ());
			} catch (Exception ex) {
				// Don't crashh
				LoggingService.LogError ("IsFolderCaseSensitive failed", ex);
				return false;
			} finally {
				File.Delete (testFile);
			}
		}

		/// <summary>
		/// Gets the real name of a file. In case insensitive file systems the name may have a different case.
		/// </summary>
		public static string GetPhysicalFileName (FilePath path)
		{
			if (!File.Exists (path) || IsFolderCaseSensitive (path.ParentDirectory))
				return path.FileName;
			var file = Directory.EnumerateFiles (path.ParentDirectory).FirstOrDefault (f => string.Equals (path.FileName, Path.GetFileName (f), StringComparison.CurrentCultureIgnoreCase));
			return file ?? path.FileName;
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
				var newPath = ((FilePath)oldName).ParentDirectory.Combine (newName);
				InternalRenameFile (oldName, newName);
				OnFileRenamed (new FileCopyEventArgs (oldName, newPath, false));
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
			OnFileCopied (new FileCopyEventArgs (srcFile, dstFile, false));
			OnFileCreated (new FileEventArgs (dstFile, false));
		}

		public static void MoveFile (string srcFile, string dstFile)
		{
			Debug.Assert (!String.IsNullOrEmpty (srcFile));
			Debug.Assert (!String.IsNullOrEmpty (dstFile));
			InternalMoveFile (srcFile, dstFile);
			OnFileMoved (new FileCopyEventArgs (srcFile, dstFile, false));
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

		static void InternalRenameFile (string srcFile, string newName)
		{
			Debug.Assert (!string.IsNullOrEmpty (srcFile));
			Debug.Assert (!string.IsNullOrEmpty (newName));
			FileSystemExtension srcExt = GetFileSystemForPath (srcFile, false);

			srcExt.RenameFile (srcFile, newName);
		}

		public static void CreateDirectory (string path)
		{
			if (!Directory.Exists (path)) {
				Debug.Assert (!String.IsNullOrEmpty (path));
				GetFileSystemForPath (path, true).CreateDirectory (path);
				OnFileCreated (new FileEventArgs (path, true));
			}
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
			OnFileMoved (new FileCopyEventArgs (srcPath, dstPath, true));
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

		public static FileWriteableState GetWriteableState (FilePath fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			try {
				return GetFileSystemForPath (fileName, false).GetWriteableState (fileName);
			} catch (Exception ex) {
				LoggingService.LogError ("File can't be written", ex);
				return FileWriteableState.Unknown;
			}
		}

		/// <summary>
		/// Requests permission for modifying a file
		/// </summary>
		/// <param name="fileName">The file to be modified</param>
		/// <param name="throwIfFails">If set to false, it will catch the exception that would've been thrown.</param>
		/// <remarks>This method must be called before trying to write any file. It throws an exception if permission is not granted.</remarks>
		public static bool RequestFileEdit (FilePath fileName, bool throwIfFails = true)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			try {
				GetFileSystemForPath (fileName, false).RequestFileEdit (fileName);
				return true;
			} catch (Exception ex) {
				if (throwIfFails)
					throw;
				LoggingService.LogError ("File can't be written", ex);
				return false;
			}
		}


		public static void NotifyFileChanged (FilePath fileName)
		{
			NotifyFileChanged (fileName, false);
		}

		public static void NotifyFileChanged (FilePath fileName, bool autoReload)
		{
			NotifyFilesChanged (new FilePath[] { fileName }, autoReload);
		}

		public static void NotifyFilesChanged (IEnumerable<FilePath> files)
		{
			NotifyFilesChanged (files, false);
		}

		public static void NotifyFilesChanged (IEnumerable<FilePath> files, bool autoReload)
		{
			try {
				foreach (var fsFiles in files.GroupBy (f => GetFileSystemForPath (f, false)))
					fsFiles.Key.NotifyFilesChanged (fsFiles);
				OnFileChanged (new FileEventArgs (files, false, autoReload));
			} catch (Exception ex) {
				LoggingService.LogError ("File change notification failed", ex);
			}
		}

		public static void NotifyFileRemoved (string fileName)
		{
			NotifyFilesRemoved (new FilePath[] { fileName });
		}

		public static void NotifyFilesRemoved (IEnumerable<FilePath> files)
		{
			try {
				OnFileRemoved (new FileEventArgs (files, false));
			} catch (Exception ex) {
				LoggingService.LogError ("File remove notification failed", ex);
			}
		}

		internal static void NotifyDirectoryRenamed (string oldPath, string newPath)
		{
			try {
				OnFileRenamed (new FileCopyEventArgs (oldPath, newPath, true));
				OnFileCreated (new FileEventArgs (newPath, true));
				OnFileRemoved (new FileEventArgs (oldPath, true));
			} catch (Exception ex) {
				LoggingService.LogError ("Directory rename notification failed", ex);
			}
		}

		internal static FileSystemExtension GetFileSystemForPath (string path, bool isDirectory)
		{
			Debug.Assert (!String.IsNullOrEmpty (path));
			FileSystemExtension nx = fileSystemChain;
			while (nx != null && !nx.CanHandlePath (path, isDirectory))
				nx = nx.Next;
			return nx;
		}

/*
		readonly static char[] separators = { Path.DirectorySeparatorChar, Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar };
		public static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (!Path.IsPathRooted (absPath))
				return absPath;

			absPath           = Path.GetFullPath (absPath);
			baseDirectoryPath = Path.GetFullPath (baseDirectoryPath.TrimEnd (Path.DirectorySeparatorChar));

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
		}*/

		static bool IsSeparator (char ch)
		{
			return ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar || ch == Path.VolumeSeparatorChar;
		}

		static char ToOrdinalIgnoreCase (char c)
		{
			return (((uint) c - 'a') <= ((uint) 'z' - 'a')) ? (char) (c - 0x20) : c;
		}

		static bool PathCharsAreEqualCaseInsensitive (char a, char b)
		{
			a = ToOrdinalIgnoreCase (a);
			b = ToOrdinalIgnoreCase (b);

			return a == b;
		}

		static bool PathCharsAreEqualCaseSensitive (char a, char b)
		{
			return a == b;
		}

		public unsafe static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (!Path.IsPathRooted (absPath) || string.IsNullOrEmpty (baseDirectoryPath))
				return absPath;

			absPath = GetFullPath (absPath);
			baseDirectoryPath = GetFullPath (baseDirectoryPath).TrimEnd (Path.DirectorySeparatorChar);

			fixed (char* bPtr = baseDirectoryPath, aPtr = absPath) {
				var bEnd = bPtr + baseDirectoryPath.Length;
				var aEnd = aPtr + absPath.Length;
				char* lastStartA = aEnd;
				char* lastStartB = bEnd;

				int indx = 0;
				// search common base path
				var a = aPtr;
				var b = bPtr;
				while (a < aEnd) {
					if (!PathCharsAreEqual (*a, *b))
						break;
					if (IsSeparator (*a)) {
						indx++;
						lastStartA = a + 1;
						lastStartB = b;
					}
					a++;
					b++;
					if (b >= bEnd) {
						if (a >= aEnd || IsSeparator (*a)) {
							indx++;
							lastStartA = a + 1;
							lastStartB = b;
						}
						break;
					}
				}
				if (indx == 0)
					return absPath;

				if (lastStartA >= aEnd)
					return ".";

				// handle case a: some/path b: some/path/deeper...
				if (a >= aEnd) {
					if (IsSeparator (*b)) {
						lastStartA = aEnd;
						lastStartB = b;
					}
				}

				// look how many levels to go up into the base path
				int goUpCount = 0;
				while (lastStartB < bEnd) {
					if (IsSeparator (*lastStartB))
						goUpCount++;
					lastStartB++;
				}
				int remainingPathLength = (int)(aEnd - lastStartA);
				int size = 0;
				if (goUpCount > 0)
					size = goUpCount * 2 + goUpCount - 1;
				if (remainingPathLength > 0) {
					if (goUpCount > 0)
						size++;
					size += remainingPathLength;
				}
				
				var result = new char [size];
				fixed (char* rPtr = result) {
					// go paths up
					var r = rPtr;
					for (int i = 0; i < goUpCount; i++) {
						*(r++) = '.';
						*(r++) = '.';
						if (i != goUpCount - 1 || remainingPathLength > 0) // If there is no remaining path, there is no need for a trailing slash
							*(r++) = Path.DirectorySeparatorChar;
					}
					// copy the remaining absulute path
					while (lastStartA < aEnd)
						*(r++) = *(lastStartA++);
				}
				return new string (result);
			}
		}

		public static string RelativeToAbsolutePath (string baseDirectoryPath, string relPath)
		{
			return Path.GetFullPath (Path.Combine (baseDirectoryPath, relPath));
		}

		public static bool IsValidPath (string fileName)
		{
			if (string.IsNullOrWhiteSpace (fileName))
				return false;
			if (fileName.IndexOfAny (FilePath.GetInvalidPathChars ()) >= 0)
				return false;
			return true;
		}


		public static bool IsValidFileName (string fileName)
		{
			if (string.IsNullOrWhiteSpace (fileName))
				return false;
			if (fileName.IndexOfAny (FilePath.GetInvalidFileNameChars ()) >= 0)
				return false;
			return true;
		}

		public static bool IsDirectory (string filename)
		{
			return Directory.Exists (filename);
		}

		public static string GetFullPath (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			if (!Platform.IsWindows || path.IndexOf ('*') == -1)
				return Path.GetFullPath (path);
			else {
				// On Windows, GetFullPath doesn't work if the path contains wildcards.
				path = path.Replace ("*", wildcardMarker);
				path = Path.GetFullPath (path);
				return path.Replace (wildcardMarker, "*");
			}
		}

		static readonly string wildcardMarker = "_" + Guid.NewGuid ().ToString () + "_";

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
			if (path.Length == 0)
				return string.Empty;
			
			int i;
			for (i = 0; i < path.Length; ++i) {
				if (path [i] != Path.DirectorySeparatorChar && path [i] != ' ')
					break;
			}

			var maxLen = path.Length - 1;
			while (i < maxLen) {
				if (path [i] != '.' || path [i + 1] != Path.DirectorySeparatorChar)
					break;
				
				i += 2;
				while (i < maxLen && i == Path.DirectorySeparatorChar)
					i++;
			}

			int j;
			for (j = maxLen; j > i; --j) {
				if (path [j] != Path.DirectorySeparatorChar) {
					j++;
					break;
				}
			}

			if (j - i == 1 && path [i] == '.')
				return string.Empty;

			return path.Substring (i, j - i);
		}

		// Atomic rename of a file. It does not fire events.
		public static void SystemRename (string sourceFile, string destFile)
		{
			if (string.IsNullOrEmpty (sourceFile))
				throw new ArgumentException ("sourceFile");

			if (string.IsNullOrEmpty (destFile))
				throw new ArgumentException ("destFile");

			if (Platform.IsWindows) {
				WindowsRename (sourceFile, destFile);
			} else {
				UnixRename (sourceFile, destFile);
			}
		}

		static void WindowsRename (string sourceFile, string destFile)
		{
			//Replace fails if the target file doesn't exist, so in that case try a simple move
			if (!File.Exists (destFile)) {
				try {
					File.Move (sourceFile, destFile);
					return;
				} catch {
				}
			}

			File.Replace (sourceFile, destFile, null);
		}

		static void UnixRename (string sourceFile, string destFile)
		{
			if (Stdlib.rename (sourceFile, destFile) != 0) {
				switch (Stdlib.GetLastError ()) {
				case Errno.EACCES:
				case Errno.EPERM:
					throw new UnauthorizedAccessException ();
				case Errno.EINVAL:
					throw new InvalidOperationException ();
				case Errno.ENOTDIR:
					throw new DirectoryNotFoundException ();
				case Errno.ENOENT:
					throw new FileNotFoundException ();
				case Errno.ENAMETOOLONG:
					throw new PathTooLongException ();
				default:
					throw new IOException ();
				}
			}
		}

		/// <summary>
		/// Renames a directory
		/// </summary>
		/// <param name="sourceDir">Source directory</param>
		/// <param name="destDir">Destination directory</param>
		/// <remarks>
		/// It works like Directory.Move, but it supports changing the case of a directory name in case-insensitive file systems
		/// </remarks>
		public static void SystemDirectoryRename (string sourceDir, string destDir)
		{
			if (Directory.Exists (destDir) && string.Equals (Path.GetFullPath (sourceDir), Path.GetFullPath (destDir), StringComparison.CurrentCultureIgnoreCase)) {
				// If the destination directory exists but we can't find it with the provided name casing, then it means we are just changing the case
				var existingDir = Directory.EnumerateDirectories (Path.GetDirectoryName (destDir), Path.GetFileName (destDir)).FirstOrDefault ();
				if (existingDir == null || (Path.GetFileName (existingDir) == Path.GetFileName (sourceDir))) {
					var temp = destDir + ".renaming";
					int n = 0;
					while (Directory.Exists (temp) || File.Exists (temp))
						temp = destDir + ".renaming_" + (n++);
					Directory.Move (sourceDir, temp);
					try {
						Directory.Move (temp, destDir);
					} catch {
						Directory.Move (temp, sourceDir);
					}
					return;
				}
			}
			Directory.Move (sourceDir, destDir);
		}

		/// <summary>
		/// Removes the directory if it's empty.
		/// </summary>
		public static void RemoveDirectoryIfEmpty (string directory)
		{
			if (Directory.Exists (directory) && !Directory.EnumerateFiles (directory).Any ())
				Directory.Delete (directory);
		}

		/// <summary>
		/// Makes the path separators native.
		/// </summary>
		public static string MakePathSeparatorsNative (string path)
		{
			if (string.IsNullOrEmpty (path))
				return path;
			char c = Path.DirectorySeparatorChar == '\\'? '/' : '\\';
			return path.Replace (c, Path.DirectorySeparatorChar);
		}

		static bool HandleError (string message, Exception ex)
		{
			return errorHandler != null ? errorHandler (message, ex) : false;
		}

		public static FileServiceErrorHandler ErrorHandler {
			get { return errorHandler; }
			set { errorHandler = value; }
		}

		public static void FreezeEvents ()
		{
			eventQueue.Freeze ();
		}

		public static void ThawEvents ()
		{
			eventQueue.Thaw ();
		}

		public static event EventHandler<FileEventArgs> FileCreated;
		static void OnFileCreated (FileEventArgs args)
		{
			foreach (FileEventInfo fi in args) {
				if (fi.IsDirectory)
					Counters.DirectoriesCreated++;
				else
					Counters.FilesCreated++;
			}

			eventQueue.RaiseEvent (FileCreated, args);
		}

		public static event EventHandler<FileCopyEventArgs> FileCopied;
		static void OnFileCopied (FileCopyEventArgs args)
		{
			eventQueue.RaiseEvent (FileCopied, args);
		}

		public static event EventHandler<FileCopyEventArgs> FileMoved;
		static void OnFileMoved (FileCopyEventArgs args)
		{
			eventQueue.RaiseEvent (FileMoved, args);
		}

		public static event EventHandler<FileCopyEventArgs> FileRenamed;
		static void OnFileRenamed (FileCopyEventArgs args)
		{
			foreach (FileCopyEventInfo fi in args) {
				if (fi.IsDirectory)
					Counters.DirectoriesRenamed++;
				else
					Counters.FilesRenamed++;
			}

			eventQueue.RaiseEvent (FileRenamed, args);
		}

		public static event EventHandler<FileEventArgs> FileRemoved;
		static void OnFileRemoved (FileEventArgs args)
		{
			foreach (FileEventInfo fi in args) {
				if (fi.IsDirectory)
					Counters.DirectoriesRemoved++;
				else
					Counters.FilesRemoved++;
			}

			eventQueue.RaiseEvent (FileRemoved, args);
		}

		public static event EventHandler<FileEventArgs> FileChanged;
		static void OnFileChanged (FileEventArgs args)
		{
			Counters.FileChangeNotifications++;
			eventQueue.RaiseEvent (FileChanged, null, args);
		}

		public static Task<bool> UpdateDownloadedCacheFile (string url, string cacheFile,
			Func<Stream,bool> validateDownload = null, CancellationToken ct = default (CancellationToken))
		{
			return WebRequestHelper.GetResponseAsync (
				() => (HttpWebRequest)WebRequest.Create (url),
				r => {
					//check to see if the online file has been modified since it was last downloaded
					var localNewsXml = new FileInfo (cacheFile);
					if (localNewsXml.Exists)
						r.IfModifiedSince = localNewsXml.LastWriteTime;
				},
				ct
			).ContinueWith (t => {
				bool deleteTempFile = true;
				var tempFile = cacheFile + ".temp";

				try {
					ct.ThrowIfCancellationRequested ();

					if (t.IsFaulted) {
						var wex = t.Exception.Flatten ().InnerException as WebException;
						if (wex != null) {
							var resp = wex.Response as HttpWebResponse;
							if (resp != null && resp.StatusCode == HttpStatusCode.NotModified)
								return false;
						}
					}

					//TODO: limit this size in case open wifi hotspots provide junk data
					var response = t.Result;
					if (response.StatusCode == HttpStatusCode.OK) {
						using (var fs = File.Create (tempFile))
								response.GetResponseStream ().CopyTo (fs, 2048);
					}

					//check the document is valid, might get bad ones from wifi hotspots etc
					if (validateDownload != null) {
						ct.ThrowIfCancellationRequested ();

						using (var f = File.OpenRead (tempFile)) {
							bool validated;
							try {
								validated = validateDownload (f);
							} catch (Exception ex) {
								throw new Exception ("Failed to validate downloaded file", ex);
							}
							if (!validated) {
								throw new Exception ("Failed to validate downloaded file");
							}
						}
					}

					ct.ThrowIfCancellationRequested ();

					SystemRename (tempFile, cacheFile);
					deleteTempFile = false;
					return true;
				} finally {
					if (deleteTempFile) {
						try {
							File.Delete (tempFile);
						} catch (Exception ex) {
							LoggingService.LogError ("Failed to delete temp download file", ex);
						}
					}
				}
			}, ct);
		}
	}

	class EventQueue
	{
		class EventData<TArgs> : EventData where TArgs:EventArgs
		{
			public EventHandler<TArgs> Delegate;
			public TArgs Args;
			public object ThisObject;

			public override void Invoke ()
			{
				Delegate?.Invoke (ThisObject, Args);
			}

			public override bool ShouldMerge (EventData other)
			{
				var next = (EventData<TArgs>)other;
				return (next.Args.GetType () == Args.GetType ()) && next.Delegate == Delegate && next.ThisObject == ThisObject;
			}

			public override bool IsChainArgs ()
			{
				return Args is IEventArgsChain;
			}

			public override void MergeArgs (EventData other)
			{
				var next = (EventData<TArgs>)other;
				((IEventArgsChain)next.Args).MergeWith ((IEventArgsChain)Args);
			}
		}

		abstract class EventData
		{
			public abstract void Invoke ();
			public abstract bool ShouldMerge (EventData other);
			public abstract void MergeArgs (EventData other);
			public abstract bool IsChainArgs ();
		}

		List<EventData> events = new List<EventData> ();
		readonly object lockObject = new object ();

		int frozen;
		object defaultSourceObject;

		public EventQueue ()
		{
		}

		public EventQueue (object defaultSourceObject)
		{
			this.defaultSourceObject = defaultSourceObject;
		}

		public void Freeze ()
		{
			lock (lockObject) {
				frozen++;
			}
		}

		public void Thaw ()
		{
			List<EventData> pendingEvents = null;
			lock (events) {
				if (--frozen == 0) {
					pendingEvents = events;
					events = new List<EventData> ();
				}
			}
			if (pendingEvents != null) {
				for (int n=0; n<pendingEvents.Count; n++) {
					EventData ev = pendingEvents [n];
					if (ev.IsChainArgs ()) {
						EventData next = n < pendingEvents.Count - 1 ? pendingEvents [n + 1] : null;
						if (next != null && ev.ShouldMerge (next)) {
							next.MergeArgs (ev);
							continue;
						}
					}
					ev.Invoke ();
				}
			}
		}

		public void RaiseEvent<TArgs> (EventHandler<TArgs> del, TArgs args) where TArgs : EventArgs
		{
			RaiseEvent (del, defaultSourceObject, args);
		}

		public void RaiseEvent<TArgs> (EventHandler<TArgs> del, object thisObj, TArgs args) where TArgs:EventArgs
		{
			lock (lockObject) {
				if (frozen > 0) {
					var ed = new EventData<TArgs> ();
					ed.Delegate = del;
					ed.ThisObject = thisObj;
					ed.Args = args;
					events.Add (ed);
					return;
				}
			}
			if (del != null) {
				if (Runtime.IsMainThread) {
					del.Invoke (thisObj, args);
				} else {
					Runtime.MainSynchronizationContext.Post (state => {
						var (del1, thisObj1, args1) = (ValueTuple<EventHandler<TArgs>, object, TArgs>)state;
						del1.Invoke (thisObj1, args1);
					}, (del, thisObj, args));
				}
			}
		}
	}

	public delegate bool FileServiceErrorHandler (string message, Exception ex);
}
