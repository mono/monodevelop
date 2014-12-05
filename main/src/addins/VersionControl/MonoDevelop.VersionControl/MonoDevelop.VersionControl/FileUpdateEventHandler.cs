
using System.Linq;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl
{
	public delegate void FileUpdateEventHandler (object sender, FileUpdateEventArgs args);

	public class FileUpdateEventArgs: EventArgsChain<FileUpdateEventInfo>
	{
		public FileUpdateEventArgs ()
		{
		}

		public FileUpdateEventArgs (Repository repo, params FilePath[] filePaths)
		{
			foreach (var p in filePaths)
				Add (new FileUpdateEventInfo (repo, p, false));
		}
		
		public FileUpdateEventArgs (Repository repo, FilePath filePath, bool isDirectory)
		{
			Add (new FileUpdateEventInfo (repo, filePath, isDirectory));
		}
		
		public IEnumerable<IGrouping<FilePath,FileUpdateEventInfo>> GroupByDirectory ()
		{
			return this.GroupBy (i => i.IsDirectory ? i.FilePath : i.FilePath.ParentDirectory);
		}
	}
	
	public class FileUpdateEventInfo
	{
		public FileUpdateEventInfo (Repository repo, FilePath filePath, bool isDirectory)
		{
			FilePath = filePath;
			Repository = repo;
			IsDirectory = isDirectory;
		}
		
		public FilePath FilePath {
			get;
			private set;
		}
		
		public Repository Repository {
			get;
			private set;
		}
		
		public bool IsDirectory {
			get;
			private set;
		}
	}
}
