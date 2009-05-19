
using System;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	public delegate void FileUpdateEventHandler (object sender, FileUpdateEventArgs args);

	public class FileUpdateEventArgs: EventArgs
	{
		FilePath filePath;
		Repository repo;
		bool isDirectory;

		internal FileUpdateEventArgs (Repository repo, FilePath filePath, bool isDirectory)
		{
			this.filePath = filePath;
			this.repo = repo;
			this.isDirectory = isDirectory;
		}
		
		public FilePath FilePath {
			get { return filePath; }
		}
		
		public Repository Repository {
			get { return repo; }
		}
		
		public bool IsDirectory {
			get { return isDirectory; }
		}
	}
}
