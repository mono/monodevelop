
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
		FilePath filePath;
		Repository repo;
		bool isDirectory;

		public FileUpdateEventInfo (Repository repo, FilePath filePath, bool isDirectory)
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
