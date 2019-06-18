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
using MonoDevelop.Core.Web;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace MonoDevelop.Core
{
	public static class FileService
	{
		internal enum EventDataKind
		{
			Created,
			Changed,
			Copied,
			Moved,
			Removed,
			Renamed,
		}

		delegate bool PathCharsAreEqualDelegate (char a, char b);

		static PathCharsAreEqualDelegate PathCharsAreEqual = Platform.IsWindows || Platform.IsMac ?
			(PathCharsAreEqualDelegate) PathCharsAreEqualCaseInsensitive :
			(PathCharsAreEqualDelegate) PathCharsAreEqualCaseSensitive;

		const string addinFileSystemExtensionPath = "/MonoDevelop/Core/FileSystemExtensions";

		static FileServiceErrorHandler errorHandler;

		static readonly EventQueue eventQueue = new EventQueue ();

		static readonly string applicationRootPath = Path.Combine (PropertyService.EntryAssemblyPath, "..");
		public static string ApplicationRootPath {
			get {
				return applicationRootPath;
			}
		}

		static class FileSystemExtensions
		{
			public static FileSystemExtension Chain => fileSystemChain;

			static FileSystemExtension fileSystemChain;
			static readonly FileSystemExtension defaultExtension = new DefaultFileSystemExtension ();

			static FileSystemExtensions ()
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

				var extensions = AddinManager.GetExtensionObjects (addinFileSystemExtensionPath, typeof (FileSystemExtension)).Cast<FileSystemExtension> ().ToArray ();
				for (int n = 0; n < extensions.Length - 1; n++) {
					extensions [n].Next = extensions [n + 1];
				}

				if (extensions.Length > 0) {
					extensions [extensions.Length - 1].Next = defaultExtension;
					fileSystemChain = extensions [0];
				} else {
					fileSystemChain = defaultExtension;
				}
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
				OnFileChanged (new FileEventArgs (files, false));
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
			FileSystemExtension nx = FileSystemExtensions.Chain;
			while (nx != null && !nx.CanHandlePath (path, isDirectory))
				nx = nx.Next;
			return nx;
		}

		internal static void NotifyFileCreated (string fileName)
		{
			OnFileCreated (new FileEventArgs (fileName, false));
		}

		/// <summary>
		/// Special case files renamed externally to prevent the IDE modifying the project.
		/// </summary>
		internal static void NotifyFileRenamedExternally (string oldPath, string newPath)
		{
			OnFileRenamed (new FileCopyEventArgs (oldPath, newPath, false) { IsExternal = true });
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
				while (i < maxLen && path [i] == Path.DirectorySeparatorChar)
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
			FreezeEvents ();
			try {
				//Replace fails if the target file doesn't exist, so in that case try a simple move
				if (!File.Exists (destFile)) {
					try {
						File.Move (sourceFile, destFile);
						return;
					} catch {
					}
				}

				File.Replace (sourceFile, destFile, null);
			} finally {
				ThawEvents ();
			}
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
				case Errno.EXDEV:
					throw new IOException (GettextCatalog.GetString("Invalid file move accross filesystem boundaries."));
				default:
					throw new IOException ("Error:" + Stdlib.GetLastError ());
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
			AsyncEvents.OnFileCreated (args);

			foreach (FileEventInfo fi in args) {
				if (fi.IsDirectory)
					Counters.DirectoriesCreated++;
				else
					Counters.FilesCreated++;
			}

			eventQueue.RaiseEvent (EventDataKind.Created, args);
		}

		public static event EventHandler<FileCopyEventArgs> FileCopied;
		static void OnFileCopied (FileCopyEventArgs args)
		{
			eventQueue.RaiseEvent (EventDataKind.Copied, args);
		}

		public static event EventHandler<FileCopyEventArgs> FileMoved;
		static void OnFileMoved (FileCopyEventArgs args)
		{
			eventQueue.RaiseEvent (EventDataKind.Moved, args);
		}

		public static event EventHandler<FileCopyEventArgs> FileRenamed;
		static void OnFileRenamed (FileCopyEventArgs args)
		{
			AsyncEvents.OnFileRenamed (args);

			foreach (FileEventInfo fi in args) {
				if (fi.IsDirectory)
					Counters.DirectoriesRenamed++;
				else
					Counters.FilesRenamed++;
			}

			eventQueue.RaiseEvent (EventDataKind.Renamed, args);
		}

		public static event EventHandler<FileEventArgs> FileRemoved;
		static void OnFileRemoved (FileEventArgs args)
		{
			AsyncEvents.OnFileRemoved (args);

			foreach (FileEventInfo fi in args) {
				if (fi.IsDirectory)
					Counters.DirectoriesRemoved++;
				else
					Counters.FilesRemoved++;
			}

			eventQueue.RaiseEvent (EventDataKind.Removed, args);
		}

		public static event EventHandler<FileEventArgs> FileChanged;
		static void OnFileChanged (FileEventArgs args)
		{
			Counters.FileChangeNotifications++;
			eventQueue.RaiseEvent (EventDataKind.Changed, args);
		}

		public static async Task<bool> UpdateDownloadedCacheFile (string url, string cacheFile,
			Func<Stream,bool> validateDownload = null, CancellationToken ct = default (CancellationToken))
		{
			bool deleteTempFile = true;
			var tempFile = cacheFile + ".temp";
			HttpClient client = null;
			try {
				client = HttpClientProvider.CreateHttpClient (url);
				//check to see if the online file has been modified since it was last downloaded
				var localNewsXml = new FileInfo (cacheFile);
				if (localNewsXml.Exists)
					client.DefaultRequestHeaders.IfModifiedSince = localNewsXml.LastWriteTime;
				using (var response = await client.GetAsync (url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait (false)) {

					ct.ThrowIfCancellationRequested ();

					//TODO: limit this size in case open wifi hotspots provide junk data
					if (response.StatusCode == HttpStatusCode.OK) {
						using (var fs = File.Create (tempFile))
							await response.Content.CopyToAsync (fs);
					} else if (response.StatusCode == HttpStatusCode.NotModified) {
						return false;
					} else {
						LoggingService.LogWarning ("FileService.UpdateDownloadedCacheFile. Unexpected status code {0}", response.StatusCode);
						return false;
					}
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
				client?.Dispose ();
				if (deleteTempFile) {
					try {
						File.Delete (tempFile);
					} catch (Exception ex) {
						LoggingService.LogError ("Failed to delete temp download file", ex);
					}
				}
			}
		}

		internal static object GetHandler (EventDataKind kind)
		{
			switch (kind)
			{
			case EventDataKind.Changed:
				return FileChanged;
			case EventDataKind.Copied:
				return FileCopied;
			case EventDataKind.Created:
				return FileCreated;
			case EventDataKind.Moved:
				return FileMoved;
			case EventDataKind.Removed:
				return FileRemoved;
			case EventDataKind.Renamed:
				return FileRenamed;
			default:
				throw new InvalidOperationException ();
			}
		}

		/// <summary>
		/// File watcher events - these are not fired on the UI thread.
		/// </summary>
		public static AsyncEvents AsyncEvents { get; } = new AsyncEvents ();
	}

	class EventQueue
	{
		static void RaiseSync (FileService.EventDataKind kind, FileEventArgs args)
		{
			var handler = FileService.GetHandler (kind);
			if (handler == null)
				return;

			// Ugly, but it saves us the problem of having to deal with generic event handlers without covariance.
			if (args is FileCopyEventArgs copyArgs) {
				if (handler is EventHandler<FileCopyEventArgs> copyHandler) {
					copyHandler.Invoke (null, copyArgs);
					return;
				}
				throw new InvalidOperationException ();
			}

			if (handler is EventHandler<FileEventArgs> fileHandler)
				fileHandler.Invoke (null, args);
			else
				throw new InvalidOperationException ();
		}

		[DebuggerDisplay("{DebuggerDisplay,nq}")]
		internal class FileEventData : EventData
		{
			public FileService.EventDataKind Kind;
			public FileEventArgs Args;

			public override void Invoke () => RaiseSync (Kind, Args);

			public override bool MergeArgs (EventData other)
			{
				bool shouldMerge = false;

				if (other is FileEventData next && Kind == next.Kind) {
					shouldMerge = true;

					if (next.Args is FileCopyEventArgs nextArgs && Args is FileCopyEventArgs thisArgs)
						shouldMerge &= nextArgs.IsExternal == thisArgs.IsExternal;

					if (shouldMerge) {
						next.Args.MergeWith (Args);
					}
				}

				return shouldMerge;
			}

			private string DebuggerDisplay => string.Format (
				"{0}: {1}",
				Kind.ToString (),
				string.Join (
					", ",
					Args.Select (x => x.SourceFile == x.TargetFile
						? x.FileName.ToString ()
						: string.Format ("{0} -> {1}", x.SourceFile.ToString (), x.TargetFile.ToString ())
					)
				)
			);
		}

		sealed class EmptyEventData : EventData
		{
			public static EmptyEventData Instance = new EmptyEventData ();

			public override void Invoke () { }
			public override bool MergeArgs (EventData other) => false;
		}

		internal abstract class EventData
		{
			public abstract void Invoke ();
			public abstract bool MergeArgs (EventData other);
		}

		readonly object lockObject = new object ();
		Processor processor = new Processor ();

		int frozen;

		public void Freeze ()
		{
			lock (lockObject) {
				frozen++;
			}
		}

		public void Thaw ()
		{
			Processor pendingProcess;
			lock (lockObject) {
				if (--frozen != 0 || processor.Events.Count == 0)
					return;

				pendingProcess = processor;
				processor = new Processor ();
			}

			pendingProcess.Merge ();

			// Trigger notifications
			Runtime.RunInMainThread (() => {
				foreach (var ev in pendingProcess.Events)
					ev.Invoke ();
			}).Ignore ();

			Task.Run (() => {
				foreach (var ev in pendingProcess.Events) {
					if (!(ev is FileEventData fev) || fev.Kind != FileService.EventDataKind.Changed)
						continue;

					foreach (var fsFiles in fev.Args.GroupBy (f => FileService.GetFileSystemForPath (f.FileName, false)))
						fsFiles.Key.NotifyFilesChanged (fsFiles.Select (x => x.FileName));
				}
			}).Ignore ();
		}

		public void RaiseEvent (FileService.EventDataKind kind, FileEventArgs args)
		{
			lock (lockObject) {
				if (frozen > 0) {
					var ed = new FileEventData {
						Kind = kind,
						Args = args,
					};
					processor.Queue (ed);
					return;
				}
			}

			if (kind == FileService.EventDataKind.Changed) {
				foreach (var fsFiles in args.GroupBy (f => FileService.GetFileSystemForPath (f.FileName, false)))
					fsFiles.Key.NotifyFilesChanged (fsFiles.Select (x => x.FileName));
			}

			if (Runtime.IsMainThread) {
				RaiseSync (kind, args);
			} else {
				Runtime.MainSynchronizationContext.Post (state => {
					var (k, a) = (ValueTuple<FileService.EventDataKind, FileEventArgs>)state;
					RaiseSync (k, a);
				}, (kind, args));
			}
		}

		internal class Processor
		{
			internal enum FileState
			{
				None,
				Created,
				Changed,
				CreatedOrChanged,
				Removed,
				RemovedViaRename,
			}

			FileEventStateMachine fsm = new FileEventStateMachine ();
			public List<FileEventData> ToQueue = new List<FileEventData> ();
			public List<EventData> Events => fsm.Events;

			internal class FileEventStateMachine
			{
				public readonly List<EventData> Events = new List<EventData> ();
				readonly Dictionary<FilePath, FileEventState> values = new Dictionary<FilePath, FileEventState> ();

				public int Queue (EventData ev)
				{
					int index = Events.Count;
					Events.Add (ev);
					return index;
				}

				public void Set (FilePath path, int eventIndex, int fileIndex, bool isSource, FileState newState)
				{
					if (!TryGet(path, out var state)) {
						values [path] = state = new FileEventState ();
					}

					if (eventIndex >= Events.Count)
						throw new ArgumentOutOfRangeException (nameof (eventIndex));

					if (fileIndex >= ((FileEventData)Events[eventIndex]).Args.Count)
						throw new ArgumentOutOfRangeException (nameof (fileIndex));

					state.Indices.Add ((eventIndex, fileIndex, isSource));
					state.FinalState = newState;
				}

				public bool TryGet (FilePath path, out FileEventState state) => values.TryGetValue (path, out state);

				public void RemoveLastEventData (FilePath path)
				{
					if (!TryGet (path, out var state))
						return;

					var index = state.Indices.Count - 1;
					if (index < 0)
						return;

					var (eventIndex, fileIndex, isSource) = state.Indices [index];

					var ev = (FileEventData)Events [eventIndex];
					ev.Args.RemoveAt (fileIndex);

					state.Indices.RemoveAt (index);

					if (--index < 0) {
						values.Remove (path);
					} else {
						(eventIndex, fileIndex, isSource) = state.Indices [index];
						ev = (FileEventData)Events [eventIndex];

						var (sourceState, targetState) = GetStateFromKind (ev.Kind);
						state.FinalState = isSource ? sourceState : targetState;
					}
				}
			}

			internal class FileEventState
			{
				public FileState FinalState { get; set; }
				public List<(int EventIndex, int FileIndex, bool IsSource)> Indices { get; } = new List<(int, int, bool)> ();
			}

			static (FileState SourceState, FileState TargetState) GetStateFromKind (FileService.EventDataKind kind)
			{
				var sourceState = FileState.None;
				var targetState = FileState.None;

				switch (kind) {
				case FileService.EventDataKind.Changed:
					// source == target
					targetState = FileState.Changed;
					break;
				case FileService.EventDataKind.Copied:
					// source is not touched
					targetState = FileState.CreatedOrChanged;
					break;
				case FileService.EventDataKind.Created:
					// source == target
					targetState = FileState.Created;
					break;
				case FileService.EventDataKind.Moved:
				case FileService.EventDataKind.Renamed:
					sourceState = FileState.RemovedViaRename;
					targetState = FileState.CreatedOrChanged;
					break;
				case FileService.EventDataKind.Removed:
					// source == target
					targetState = FileState.Removed;
					break;
				}

				return (sourceState, targetState);
			}

			public void Queue (FileEventData data)
			{
				var args = data.Args;

				int eventIndex = fsm.Queue (data);

				var (sourceState, targetState) = GetStateFromKind (data.Kind);

				// We only need to handle target file here.
				for (int i = 0; i < args.Count; ++i) {
					var arg = args [i];

					// Don't want to handle directories for now.
					if (arg.IsDirectory)
						continue;

					if (args is FileCopyEventArgs) {
						// Remove the target removal, it's going to be replaced.
						if (targetState == FileState.CreatedOrChanged && fsm.TryGet (arg.TargetFile, out var state) && state.FinalState == FileState.Removed) {
							// Only set one when removed/renamed
							fsm.RemoveLastEventData (arg.TargetFile);
						}

						if (sourceState != FileState.None && HandleStateChange (arg.SourceFile, arg.IsDirectory, sourceState, args, eventIndex, ref i))
							fsm.Set (arg.SourceFile, eventIndex, i, false, sourceState);
					}

					if (!HandleStateChange (arg.TargetFile, arg.IsDirectory, targetState, args, eventIndex, ref i))
						fsm.Set (arg.TargetFile, eventIndex, i, false, targetState);
				}

				if (ToQueue.Count != 0) {
					var temp = new List<FileEventData> ();

					(temp, ToQueue) = (ToQueue, temp);

					foreach (var item in temp) {
						Queue (item);
					}
				}
			}

			bool HandleStateChange (FilePath path, bool isDirectory, FileState targetState, FileEventArgs args, int eventIndex, ref int i)
			{
				if (!fsm.TryGet (path, out var state))
					return false;

				var oldState = state.FinalState;

				switch (targetState) {
				case FileState.Changed:
					if (oldState == FileState.Changed) {
						// Changed + Changed => Changed
						Discard (args, ref i);
						return true;
					}
					break;

				case FileState.Created:
					if (oldState == FileState.Removed) {
						// Remove -> Created => queue new Changed
						Discard (args, ref i);
						fsm.RemoveLastEventData (path);

						ToQueue.Add (new FileEventData {
							Args = new FileEventArgs (path, isDirectory),
							Kind = FileService.EventDataKind.Changed
						});
						return true;
					}

					if (oldState == FileState.Created) {
						// Remove -> Created => Changed
						Discard (args, ref i);
						return true;
					}
					break;

				case FileState.Removed:
					if (oldState == FileState.Created) {
						// Created -> Remove => NOP
						Discard (args, ref i);
						fsm.RemoveLastEventData (path);
						return true;
					}

					if (oldState == FileState.Changed) {
						// Changed + Remove -> Remove
						fsm.RemoveLastEventData (path);

						// Reduce more if we can here.
						return HandleStateChange (path, isDirectory, targetState, args, eventIndex, ref i);
					}

					if (oldState == FileState.CreatedOrChanged) {
						// The old file was someone renaming something to it.

						// Rename a -> a.tmp <DISCARD>
						// Rename b -> a
						// Remove a.tmp <DISCARD>

						// Look for a -> a.tmp
						if (GetLastRenameEventIgnoringChanged (state, path, 0, out var actualTarget) && fsm.TryGet (actualTarget, out var sourceState)) {
							// Look for b -> a
							if (GetLastRenameEventIgnoringChanged (sourceState, actualTarget, 0, out var actualSource)) {
								fsm.RemoveLastEventData (path);
								Discard (args, ref i);
								return true;
							}
						}
					}

					break;
				}

				return false;
			}

			bool GetLastRenameEventIgnoringChanged (FileEventState state, FilePath path, int index, out FilePath result)
			{
				result = FilePath.Empty;

				var indices = state.Indices;
				if (indices.Count > index) {
					var targetIndex = indices [indices.Count - 1 - index];
					var resultState = (FileEventData)fsm.Events [targetIndex.EventIndex];

					if (resultState.Kind == FileService.EventDataKind.Changed)
						return GetLastRenameEventIgnoringChanged (state, path, index + 1, out result);

					if (resultState.Kind != FileService.EventDataKind.Renamed && resultState.Kind != FileService.EventDataKind.Moved)
						return false;

					var args = resultState.Args;
					for (int i = 0; i < args.Count; ++i) {
						var arg = args [i];
						if (arg.TargetFile == path) {
							// Found the file which this was renamed from.
							result = arg.SourceFile;
							return true;
						}
					}
				}

				return false;
			}

			static void Discard (FileEventArgs args, ref int i)
			{
				args.RemoveAt (i);
				i--;
			}

			public void Merge ()
			{
				// Remove all the events which had no items.
				Events.RemoveAll (x => ((FileEventData)x).Args.Count == 0);

				// Merge similar events to trigger fewer notifications.
				var previous = Events.Count > 0 ? Events [0] : null;
				EventData current = null;

				for (int n = 1; n < Events.Count; n++, previous = current) {
					current = Events [n];

					if (previous.MergeArgs (current))
						Events [n - 1] = EmptyEventData.Instance;
				}
			}
		}
	}

	public class AsyncEvents
	{
		public event EventHandler<FileEventArgs> FileCreated;
		public event EventHandler<FileEventArgs> FileRemoved;
		public event EventHandler<FileCopyEventArgs> FileRenamed;

		internal void OnFileCreated (FileEventArgs args)
		{
			FileCreated?.Invoke (this, Clone (args));
		}

		internal void OnFileRemoved (FileEventArgs args)
		{
			FileRemoved?.Invoke (this, Clone (args));
		}

		internal void OnFileRenamed (FileCopyEventArgs args)
		{
			FileRenamed?.Invoke (this, Clone (args));
		}

		static T Clone<T> (T args) where T : FileEventArgs, new()
		{
			var result = new T ();
			result.AddRange (args);
			return result;
		}
	}

	public delegate bool FileServiceErrorHandler (string message, Exception ex);
}
