namespace Sharpen
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using Mono.Unix;

	public class FilePath
	{
		static bool RunningOnLinux = !Environment.OSVersion.Platform.ToString ().StartsWith ("Win");
		
		private string path;
		private static long tempCounter;

		public FilePath ()
		{
		}

		public FilePath (string path)
		{
			this.path = path;
		}

		public FilePath (FilePath other, string child)
		{
			this.path = Path.Combine (other.path, child);
		}

		public FilePath (string other, string child)
		{
			this.path = Path.Combine (other, child);
		}
		
		public static implicit operator FilePath (string name)
		{
			return new FilePath (name);
		}

		public static implicit operator string (FilePath filePath)
		{
			return filePath.path;
		}
		
		public override bool Equals (object obj)
		{
			FilePath other = obj as FilePath;
			if (other == null)
				return false;
			return GetCanonicalPath () == other.GetCanonicalPath ();
		}
		
		public override int GetHashCode ()
		{
			return path.GetHashCode ();
		}

		public bool CanWrite ()
		{
			if (RunningOnLinux) {
				var info = GetUnixFileInfo (path);
				return info != null && info.CanAccess (Mono.Unix.Native.AccessModes.W_OK);
			}
			
			return ((File.GetAttributes (path) & FileAttributes.ReadOnly) == 0);
		}

		public bool CreateNewFile ()
		{
			if (Exists ())
				return false;
			File.OpenWrite (path).Close ();
			return true;
		}

		public static FilePath CreateTempFile ()
		{
			return new FilePath (Path.GetTempFileName ());
		}

		public static FilePath CreateTempFile (string prefix, string suffix)
		{
			return CreateTempFile (prefix, suffix, null);
		}

		public static FilePath CreateTempFile (string prefix, string suffix, FilePath directory)
		{
			string file;
			if (prefix == null) {
				throw new ArgumentNullException ("prefix");
			}
			if (prefix.Length < 3) {
				throw new ArgumentException ("prefix must have at least 3 characters");
			}
			string str = (directory == null) ? Path.GetTempPath () : directory.GetPath ();
			do {
				file = Path.Combine (str, prefix + Interlocked.Increment (ref tempCounter) + suffix);
			} while (File.Exists (file));
			
			new FileOutputStream (file).Close ();
			return new FilePath (file);
		}

		public bool Delete ()
		{
			try {
				if (RunningOnLinux) {
					var info = GetUnixFileInfo (path);
					if (info != null && info.Exists) {
						try {
							info.Delete ();
							return true;
						} catch {
							// If the directory is not empty we return false. JGit relies on this
							return false;
						}
					}
					return false;
				}

				if (Directory.Exists (path)) {
					if (Directory.GetFileSystemEntries (path).Length != 0)
						return false;
					MakeDirWritable (path);
					Directory.Delete (path, true);
                    return true;
                }
                else if (File.Exists(path)) {
					MakeFileWritable (path);
					File.Delete (path);
                    return true;
                }
				return false;
			} catch (Exception exception) {
				Console.WriteLine (exception);
				return false;
			}
		}

		public void DeleteOnExit ()
		{
		}

		public bool Exists ()
		{
			if (RunningOnLinux) {
				var info = GetUnixFileInfo (path);
				return info != null && info.Exists;
			}

			return (File.Exists (path) || Directory.Exists (path));
		}

		public FilePath GetAbsoluteFile ()
		{
			return new FilePath (Path.GetFullPath (path));
		}

		public string GetAbsolutePath ()
		{
			return Path.GetFullPath (path);
		}

		public FilePath GetCanonicalFile ()
		{
			return new FilePath (GetCanonicalPath ());
		}

		public string GetCanonicalPath ()
		{
			string p = Path.GetFullPath (path);
			p.TrimEnd (Path.DirectorySeparatorChar);
			return p;
		}

		public string GetName ()
		{
			return Path.GetFileName (path);
		}

		public FilePath GetParentFile ()
		{
			return new FilePath (Path.GetDirectoryName (path));
		}

		public string GetPath ()
		{
			return path;
		}

		public bool IsAbsolute ()
		{
			return Path.IsPathRooted (path);
		}

		public bool IsDirectory ()
		{
			try {
				if (RunningOnLinux) {
					var info = GetUnixFileInfo (path);
					return info != null && info.Exists && info.FileType == FileTypes.Directory;
				}
			} catch (DirectoryNotFoundException) {
				// If the file /foo/bar exists and we query to see if /foo/bar/baz exists, we get a
				// DirectoryNotFound exception for Mono.Unix. In this case the directory definitely
				// does not exist.
				return false;
			}
			return Directory.Exists (path);
		}

		public bool IsFile ()
		{
			if (RunningOnLinux) {
				var info = GetUnixFileInfo (path);
				return info != null && info.Exists && (info.FileType == FileTypes.RegularFile || info.FileType == FileTypes.SymbolicLink);
			}

			return File.Exists (path);
		}

		public long LastModified ()
		{
            if (RunningOnLinux) {
				var info = GetUnixFileInfo (path);
				return info != null && info.Exists ? info.LastWriteTimeUtc.ToMillisecondsSinceEpoch() : 0;
            }

            var info2 = new FileInfo(path);
			return info2.Exists ? info2.LastWriteTimeUtc.ToMillisecondsSinceEpoch() : 0;
		}

		public long Length ()
		{
			if (RunningOnLinux) {
				var info = GetUnixFileInfo (path);
				return info != null && info.Exists ? info.Length : 0;
			}

			// If you call .Length on a file that doesn't exist, an exception is thrown
			var info2 = new FileInfo (path);
			return info2.Exists ? info2.Length : 0;
		}

		public string[] List ()
		{
			return List (null);
		}

		public string[] List (FilenameFilter filter)
		{
			try {
				if (IsFile ())
					return null;
				List<string> list = new List<string> ();
				foreach (string filePth in Directory.GetFileSystemEntries (path)) {
					string fileName = Path.GetFileName (filePth);
					if ((filter == null) || filter.Accept (this, fileName)) {
						list.Add (fileName);
					}
				}
				return list.ToArray ();
			} catch {
				return null;
			}
		}

		public FilePath[] ListFiles ()
		{
			try {
				if (IsFile ())
					return null;
				List<FilePath> list = new List<FilePath> ();
				foreach (string filePath in Directory.GetFileSystemEntries (path)) {
					list.Add (new FilePath (filePath));
				}
				return list.ToArray ();
			} catch {
				return null;
			}
		}

		private void MakeDirWritable (string dir)
		{
			foreach (string file in Directory.GetFiles (dir)) {
				MakeFileWritable (file);
			}
			foreach (string subdir in Directory.GetDirectories (dir)) {
				MakeDirWritable (subdir);
			}
		}

		private void MakeFileWritable (string file)
		{
			if (RunningOnLinux) {
				var info = GetUnixFileInfo (file);
				if (info != null)
					info.FileAccessPermissions |= (FileAccessPermissions.GroupWrite | FileAccessPermissions.OtherWrite | FileAccessPermissions.UserWrite);
				return;
			}

			FileAttributes fileAttributes = File.GetAttributes (file);
			if ((fileAttributes & FileAttributes.ReadOnly) != 0) {
				fileAttributes &= ~FileAttributes.ReadOnly;
				File.SetAttributes (file, fileAttributes);
			}
		}

		public bool Mkdir ()
		{
			try {
				if (Directory.Exists (path))
					return false;
				Directory.CreateDirectory (path);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public bool Mkdirs ()
		{
			try {
				if (Directory.Exists (path))
					return false;
				Directory.CreateDirectory (this.path);
				return true;
			} catch {
				return false;
			}
		}

		public bool RenameTo (FilePath file)
		{
			return RenameTo (file.path);
		}

		public bool RenameTo (string name)
		{
			try {
				if (RunningOnLinux) {
					var symlink = GetUnixFileInfo (path) as UnixSymbolicLinkInfo;
					if (symlink != null) {
						var newFile = new UnixSymbolicLinkInfo (name);
						newFile.CreateSymbolicLinkTo (symlink.ContentsPath);
					}
				}

				File.Move (path, name);
				return true;
			} catch {
				return false;
			}
		}

		public void SetLastModified (long milis)
		{
			// FIXME: I don't know how to change the modified time on a symlink
			DateTime utcDateTime = Extensions.MillisToDateTimeOffset (milis, 0L).UtcDateTime;
			File.SetLastWriteTimeUtc (path, utcDateTime);
		}

		public void SetReadOnly ()
		{
			if (RunningOnLinux) {
				var info = GetUnixFileInfo (path);
				if (info != null)
					info.FileAccessPermissions &= ~ (FileAccessPermissions.GroupWrite | FileAccessPermissions.OtherWrite | FileAccessPermissions.UserWrite);
				return;
			}

			var fileAttributes = File.GetAttributes (this.path) | FileAttributes.ReadOnly;
			File.SetAttributes (path, fileAttributes);
		}
		
		public Uri ToURI ()
		{
			return new Uri (path);
		}
		
		// Don't change the case of this method, since ngit does reflection on it
		public bool canExecute ()
		{
			if (RunningOnLinux) {
				UnixFileInfo fi = new UnixFileInfo (path);
				if (!fi.Exists)
					return false;
				return 0 != (fi.FileAccessPermissions & (FileAccessPermissions.UserExecute | FileAccessPermissions.GroupExecute | FileAccessPermissions.OtherExecute));
			}

			return false;
		}
		
		// Don't change the case of this method, since ngit does reflection on it
		public bool setExecutable (bool exec)
		{
			if (RunningOnLinux) {
				UnixFileInfo fi = new UnixFileInfo (path);
				FileAccessPermissions perms = fi.FileAccessPermissions;
				if (exec) {
					if (perms.HasFlag (FileAccessPermissions.UserRead))
						perms |= FileAccessPermissions.UserExecute;
					if (perms.HasFlag (FileAccessPermissions.OtherRead))
						perms |= FileAccessPermissions.OtherExecute;
					if ((perms.HasFlag (FileAccessPermissions.GroupRead)))
						perms |= FileAccessPermissions.GroupExecute;
				} else {
					if (perms.HasFlag (FileAccessPermissions.UserRead))
						perms &= ~FileAccessPermissions.UserExecute;
					if (perms.HasFlag (FileAccessPermissions.OtherRead))
						perms &= ~FileAccessPermissions.OtherExecute;
					if ((perms.HasFlag (FileAccessPermissions.GroupRead)))
						perms &= ~FileAccessPermissions.GroupExecute;
				}
				fi.FileAccessPermissions = perms;
				return true;
			}

			return false;
		}
		
		public string GetParent ()
		{
			string p = Path.GetDirectoryName (path);
			if (string.IsNullOrEmpty(p) || p == path)
				return null;
			else
				return p;
		}

		public override string ToString ()
		{
			return path;
		}

		static UnixFileSystemInfo GetUnixFileInfo (string path)
		{
			try {
				return Mono.Unix.UnixFileInfo.GetFileSystemEntry (path);
			} catch (DirectoryNotFoundException ex) {
				// If we have a file /foo/bar and probe the path /foo/bar/baz, we get a DirectoryNotFound exception
				// because 'bar' is a file and therefore 'baz' cannot possibly exist. This is annoying.
				var inner = ex.InnerException as UnixIOException;
				if (inner != null && inner.ErrorCode == Mono.Unix.Native.Errno.ENOTDIR)
					return null;
				throw;
			}
		}
		
		static internal string pathSeparator {
			get { return Path.PathSeparator.ToString (); }
		}

		static internal char pathSeparatorChar {
			get { return Path.PathSeparator; }
		}

		static internal char separatorChar {
			get { return Path.DirectorySeparatorChar; }
		}

		static internal string separator {
			get { return Path.DirectorySeparatorChar.ToString (); }
		}
	}
}
