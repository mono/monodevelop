
using System;

namespace MonoDevelop.VersionControl
{
	public delegate void FileUpdateEventHandler (object sender, FileUpdateEventArgs args);

	public class FileUpdateEventArgs: EventArgs
	{
		string filePath;
		Repository repo;
		bool isDirectory;

		internal FileUpdateEventArgs (Repository repo, string filePath, bool isDirectory)
		{
			this.filePath = filePath;
			this.repo = repo;
			this.isDirectory = isDirectory;
		}
		
		public string FilePath {
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
