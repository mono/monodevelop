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
		const string addinFileSystemExtensionPath = "/MonoDevelop/Core/FileSystemExtensions";

		static FileServiceErrorHandler errorHandler;
		
		static FileSystemExtension fileSystemChain;
		static FileSystemExtension defaultExtension = Platform.IsWindows ? new DefaultFileSystemExtension () : new UnixFileSystemExtension () ;
		
		static EventQueue eventQueue = new EventQueue ();
		
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
			UpdateExtensions ();
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
		
		public static FilePath ResolveFullPath (FilePath path)
		{
			try {
				return GetFileSystemForPath (path, false).ResolveFullPath (path);
			} catch (Exception e) {
				if (!HandleError (GettextCatalog.GetString ("Can't resolve full path {0}", path), e))
					throw;
				return FilePath.Empty;
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
		
		public static void NotifyFileChanged (FilePath fileName)
		{
			NotifyFilesChanged (new FilePath[] { fileName });
		}
		
		public static void NotifyFilesChanged (IEnumerable<FilePath> files)
		{
			try {
				foreach (var fsFiles in files.GroupBy (f => GetFileSystemForPath (f, false)))
					fsFiles.Key.NotifyFilesChanged (fsFiles);
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
		
		public unsafe static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (!Path.IsPathRooted (absPath) || string.IsNullOrEmpty (baseDirectoryPath))
				return absPath;
				
			absPath = Path.GetFullPath (absPath);
			baseDirectoryPath = Path.GetFullPath (baseDirectoryPath).TrimEnd (Path.DirectorySeparatorChar);
			
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
					if (*a != *b)
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
						lastStartA = a + 1;
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
				var size = goUpCount * 2 + goUpCount + aEnd - lastStartA;
				var result = new char [size];
				fixed (char* rPtr = result) {
					// go paths up
					var r = rPtr;
					for (int i = 0; i < goUpCount; i++) {
						*(r++) = '.';
						*(r++) = '.';
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
			if (String.IsNullOrEmpty (fileName) || fileName.Trim() == string.Empty) 
				return false;
			if (fileName.IndexOfAny (Path.GetInvalidPathChars ()) >= 0)
				return false;
			return true;
		}

		public static bool IsValidFileName (string fileName)
		{
			if (String.IsNullOrEmpty (fileName) || fileName.Trim() == string.Empty) 
				return false;
			if (fileName.IndexOfAny (Path.GetInvalidFileNameChars ()) >= 0)
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

		// Atomic rename of a file. It does not fire events.
		public static void SystemRename (string sourceFile, string destFile)
		{
			if (string.IsNullOrEmpty (sourceFile))
				throw new ArgumentException ("sourceFile");

			if (string.IsNullOrEmpty (destFile))
				throw new ArgumentException ("destFile");

			//FIXME: use the atomic System.IO.File.Replace on NTFS
			if (Platform.IsWindows) {
				string wtmp = null;
				if (File.Exists (destFile)) {
					do {
						wtmp = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
					} while (File.Exists (wtmp));

					File.Move (destFile, wtmp);
				}
				try {
					File.Move (sourceFile, destFile);
				}
				catch {
					try {
						if (wtmp != null)
							File.Move (wtmp, destFile);
					}
					catch {
						wtmp = null;
					}
					throw;
				}
				finally {
					if (wtmp != null) {
						try {
							File.Delete (wtmp);
						}
						catch { }
					}
				}
			}
			else {
				if (Syscall.rename (sourceFile, destFile) != 0) {
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
		}
		
		/// <summary>
		/// Removes the directory if it's empty.
		/// </summary>
		public static void RemoveDirectoryIfEmpty (string directory)
		{
			// HACK: we should use EnumerateFiles but it's broken in some Mono releases
			// https://bugzilla.xamarin.com/show_bug.cgi?id=2975
			if (Directory.Exists (directory) && !Directory.GetFiles (directory).Any ())
				Directory.Delete (directory);
		}
		
		/// <summary>
		/// Creates a directory if it does not already exist.
		/// </summary>
		public static void EnsureDirectoryExists (string directory)
		{
			if (!Directory.Exists (directory))
				Directory.CreateDirectory (directory);
		}
		
		/// <summary>
		/// Makes the path separators native.
		/// </summary>
		public static string MakePathSeparatorsNative (string path)
		{
			if (path == null || path.Length == 0)
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

			eventQueue.RaiseEvent (() => FileCreated, args);
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
			
			eventQueue.RaiseEvent (() => FileRenamed, args);
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
			
			eventQueue.RaiseEvent (() => FileRemoved, args);
		}
		
		public static event EventHandler<FileEventArgs> FileChanged;
		static void OnFileChanged (FileEventArgs args)
		{
			Counters.FileChangeNotifications++;
			eventQueue.RaiseEvent (() => FileChanged, null, args);
		}

		public static Task<bool> UpdateDownloadedCacheFile (string url, string cacheFile,
			Func<Stream,bool> validateDownload = null, CancellationToken ct = new CancellationToken ())
		{
			var tcs = new TaskCompletionSource<bool> ();

			//HACK: .NET blocks on DNS in BeginGetResponse, so use a threadpool thread
			// see http://stackoverflow.com/questions/1232139#1232930
			System.Threading.ThreadPool.QueueUserWorkItem ((state) => {
				var request = (HttpWebRequest)WebRequest.Create (url);

				try {
					//check to see if the online file has been modified since it was last downloaded
					var localNewsXml = new FileInfo (cacheFile);
					if (localNewsXml.Exists)
						request.IfModifiedSince = localNewsXml.LastWriteTime;

					request.BeginGetResponse (HandleResponse, new CacheFileDownload {
						Request = request,
						Url = url,
						CacheFile = cacheFile,
						ValidateDownload = validateDownload,
						CancellationToken = ct,
						Tcs = tcs,
					});
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			});

			return tcs.Task;
		}

		class CacheFileDownload
		{
			public HttpWebRequest Request;
			public string CacheFile, Url;
			public Func<Stream,bool> ValidateDownload;
			public CancellationToken CancellationToken;
			public TaskCompletionSource<bool> Tcs;
		}

		static void HandleResponse (IAsyncResult ar)
		{
			var c = (CacheFileDownload) ar.AsyncState;
			bool deleteTempFile = true;
			var tempFile = c.CacheFile + ".temp";

			try {
				if (c.CancellationToken.IsCancellationRequested)
					c.Tcs.TrySetCanceled ();

				try {
					//TODO: limit this size in case open wifi hotspots provide bad data
					var response = (HttpWebResponse) c.Request.EndGetResponse (ar);
					if (response.StatusCode == HttpStatusCode.OK) {
						using (var fs = File.Create (tempFile))
							response.GetResponseStream ().CopyTo (fs, 2048);
					}
				} catch (WebException wex) {
					var httpResp = wex.Response as HttpWebResponse;
					if (httpResp != null) {
						if (httpResp.StatusCode == HttpStatusCode.NotModified) {
							c.Tcs.TrySetResult (false);
							return;
						}
						//is this valid? should we just return the WebException directly?
						else if (httpResp.StatusCode == HttpStatusCode.NotFound) {
							c.Tcs.TrySetException (new FileNotFoundException ("File not found on server", c.Url, wex));
							return;
						}
					}
					throw;
				}

				//check the document is valid, might get bad ones from wifi hotspots etc
				if (c.ValidateDownload != null) {
					if (c.CancellationToken.IsCancellationRequested)
						c.Tcs.TrySetCanceled ();

					using (var f = File.OpenRead (tempFile)) {
						try {
							if (!c.ValidateDownload (f)) {
								c.Tcs.TrySetException (new Exception ("Failed to validate downloaded file"));
								return;
							}
						} catch (Exception ex) {
							c.Tcs.TrySetException (new Exception ("Failed to validate downloaded file", ex));
						}
					}
				}

				if (c.CancellationToken.IsCancellationRequested)
					c.Tcs.TrySetCanceled ();

				SystemRename (tempFile, c.CacheFile);
				deleteTempFile = false;
				c.Tcs.TrySetResult (true);
			} catch (Exception ex) {
				c.Tcs.TrySetException (ex);
			} finally {
				if (deleteTempFile) {
					try {
						File.Delete (tempFile);
					} catch {}
				}
			}
		}
	}
	
	class EventQueue
	{
		class EventData
		{
			public Func<Delegate> Delegate;
			public object ThisObject;
			public EventArgs Args;
		}
		
		List<EventData> events = new List<EventData> ();
		
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
			lock (events) {
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
					Delegate del = ev.Delegate ();
					if (ev.Args is IEventArgsChain) {
						EventData next = n < pendingEvents.Count - 1 ? pendingEvents [n + 1] : null;
						if (next != null && (next.Args.GetType() == ev.Args.GetType ()) && next.Delegate() == del && next.ThisObject == ev.ThisObject) {
							((IEventArgsChain)next.Args).MergeWith ((IEventArgsChain)ev.Args);
							continue;
						}
					}
					if (del != null)
						del.DynamicInvoke (ev.ThisObject, ev.Args);
				}
			}
		}
		
		public void RaiseEvent (Func<Delegate> d, EventArgs args)
		{
			RaiseEvent (d, defaultSourceObject, args);
		}
		
		public void RaiseEvent (Func<Delegate> d, object thisObj, EventArgs args)
		{
			Delegate del = d ();
			lock (events) {
				if (frozen > 0) {
					EventData ed = new EventData ();
					ed.Delegate = d;
					ed.ThisObject = thisObj;
					ed.Args = args;
					events.Add (ed);
					return;
				}
			}
			if (del != null)
				del.DynamicInvoke (thisObj, args);
		}
	}
	
	public delegate bool FileServiceErrorHandler (string message, Exception ex);
}
