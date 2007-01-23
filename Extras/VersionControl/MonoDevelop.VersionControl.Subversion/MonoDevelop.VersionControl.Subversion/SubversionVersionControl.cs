using System;
using System.Collections;
using System.IO;

using System.Runtime.InteropServices;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion
{
	class SubversionVersionControl : VersionControlSystem
	{
		SvnClient client;
		readonly string[] protocolsSvn = {"svn", "svn+ssh", "http", "https", "file"};
		
		public override string Name {
			get { return "Subversion"; }
		}
		
		protected override Repository OnCreateRepositoryInstance ()
		{
			return new SubversionRepository ();
		}
		
		public override Gtk.Widget CreateRepositoryEditor (Repository repo)
		{
			return new UrlBasedRepositoryEditor ((SubversionRepository)repo, protocolsSvn);
		}
		
		public override Repository GetRepositoryReference (string path, string id)
		{
			try {
				string url = Client.GetPathUrl (path);
				return new SubversionRepository (this, url);
			} catch {
				// No SVN
				return null;
			}
		}
		
		public override void StoreRepositoryReference (Repository repo, string path, string id)
		{
			// Nothing to do
		}
		
		SvnClient Client {
			get {
				if (client == null) client = new SvnClient();
				return client;
			}
		}
	
		string GetTextBase(string sourcefile) {
			return Path.Combine(
				Path.Combine(
					Path.Combine(
						Path.GetDirectoryName(sourcefile),
						 ".svn"),
					"text-base"),
				Path.GetFileName(sourcefile) + ".svn-base"); 
		}
	
		internal static string GetDirectoryDotSvn (string sourcepath) {
			return Path.Combine(sourcepath, ".svn");
		}
		
		public bool IsDiffAvailable (Repository repo, string sourcefile) {
			try {
				return File.Exists(GetTextBase(sourcefile))
					&& IsVersioned(sourcefile)
					&& GetVersionInfo (repo, sourcefile, false).Status == VersionStatus.Modified;
			} catch {
				// GetVersionInfo may throw an exception
				return false;
			}
		}
		
		public bool IsVersioned (string sourcefile)
		{
			return File.Exists (GetTextBase (sourcefile))
				|| Directory.Exists (GetDirectoryDotSvn (sourcefile));
		}

		public bool CanCommit (Repository repo, string sourcepath)
		{
			if (Directory.Exists (sourcepath) && Directory.Exists (GetDirectoryDotSvn (sourcepath)))
				return true;
			if (GetVersionInfo (repo, sourcepath, false) != null)
				return true;
			return false;
		}
		
		public bool CanAdd (Repository repo, string sourcepath)
		{
			// Do some trivial checks
			
			if (!Directory.Exists (GetDirectoryDotSvn (Path.GetDirectoryName (sourcepath))))
				return false;

			if (File.Exists (sourcepath)) {
				if (File.Exists (GetTextBase (sourcepath)))
					return false;
			} else if (Directory.Exists (sourcepath)) {
				if (Directory.Exists (GetTextBase (sourcepath)))
					return false;
			} else
				return false;
				
			// Allow adding only if the path is not already scheduled for adding
			
			VersionInfo ver = this.GetVersionInfo (repo, sourcepath, false);
			if (ver == null)
				return true;
			return ver.Status == VersionStatus.Unversioned;
		}
		
		public string GetPathToBaseText(string sourcefile) {
			return GetTextBase(sourcefile);
		}
		
		public Revision[] GetHistory (Repository repo, string sourcefile, Revision since) {
			ArrayList revs = new ArrayList();
			
			SvnClient.Rev startrev;
			string curPath;

			// We need to know the URL and last committed revision of
			// sourcefile.  We can GetVersionInfo the sourcefile, unless
			// this is the root directory of the repository, in which
			// case GetVersionInfo will throw an exception
			try {
				VersionInfo node = GetVersionInfo (repo, sourcefile, false);
				startrev = ((SvnRevision)node.Revision).ForApi();
				curPath = node.RepositoryPath;
			} catch {
				startrev = SvnClient.Rev.Head;
				curPath = Client.GetPathUrl(sourcefile);
			} 
			
			SvnClient.Rev sincerev = SvnClient.Rev.First;
			if (since != null)
				sincerev = ((SvnRevision)since).ForApi();

			foreach (SvnClient.LogEnt log in Client.Log(sourcefile, startrev, sincerev)) {
				ArrayList changedPaths = new ArrayList();
				foreach (SvnClient.LogEntChangedPath chg in log.ChangedPaths) {
					changedPaths.Add(chg.Path + " (" + chg.Action + ")");
				}
				
				SvnRevision d = new SvnRevision (repo, log.Revision, log.Time, log.Author, log.Message, (string[])changedPaths.ToArray(typeof(string)));
				revs.Add(d);

				// Be aware of the change in path resulting from a copy.
				// Not sure if this works.
				foreach (SvnClient.LogEntChangedPath chg in log.ChangedPaths) {
					if (curPath.EndsWith(chg.Path) && chg.CopyFromPath != null) {
						curPath = curPath.Substring(0, curPath.Length-chg.Path.Length) + chg.CopyFromPath;
						break;
					}
				}
			}
			return (Revision[])revs.ToArray(typeof(Revision));
		}
		
		public string GetTextAtRevision (string repositoryPath, Revision revision) 
		{
			return Client.Cat (repositoryPath, ((SvnRevision)revision).ForApi() );
		}
		
		public VersionInfo GetVersionInfo (Repository repo, string localPath, bool getRemoteStatus)
		{
			if (File.Exists (GetTextBase(localPath)) || File.Exists (localPath))
				return GetFileStatus (repo, localPath, getRemoteStatus);
			else if (Directory.Exists (GetDirectoryDotSvn (localPath)) || Directory.Exists (localPath))
				return GetDirStatus (repo, localPath, getRemoteStatus);
			else
				return null;
		}
		
		private VersionInfo GetFileStatus (Repository repo, string sourcefile, bool getRemoteStatus)
		{
			// If the directory is not versioned, there is no version info
			if (!Directory.Exists (GetDirectoryDotSvn (Path.GetDirectoryName (sourcefile))))
				return null;
				
			IList statuses = Client.Status(sourcefile, SvnClient.Rev.Head, false, false, getRemoteStatus);
			if (statuses.Count == 0)
				throw new ArgumentException("Path '" + sourcefile + "' does not exist in the repository.");
				
			SvnClient.StatusEnt ent;
			if (statuses.Count != 1)
				throw new ArgumentException("Path '" + sourcefile + "' does not refer to a file in the repository.");
			ent = (SvnClient.StatusEnt)statuses[0];
			if (ent.IsDirectory)
				throw new ArgumentException("Path '" + sourcefile + "' does not refer to a file.");
			return CreateNode (ent, repo);
		}
		
		private VersionInfo GetDirStatus (Repository repo, string localPath, bool getRemoteStatus)
		{
			string parent = Path.GetDirectoryName (localPath);
			
			// If the directory is not versioned, there is no version info
			if (!Directory.Exists (GetDirectoryDotSvn (parent)))
				return null;
				
			IList statuses = Client.Status (parent, SvnClient.Rev.Head, false, false, getRemoteStatus);
			foreach (SvnClient.StatusEnt ent in statuses) {
				if (ent.LocalFilePath == localPath)
					return CreateNode (ent, repo);
			}
			return null;
		}
		
		public VersionInfo[] GetDirectoryVersionInfo (Repository repo, string sourcepath, bool getRemoteStatus, bool recursive) {
			IList ents = Client.Status(sourcepath, SvnClient.Rev.Head, recursive, true, getRemoteStatus);
			return CreateNodes (repo, ents);
		}
		
		public void Update(string path, bool recurse, IProgressMonitor monitor) {
			Client.Update(path, recurse, monitor);
		}
		
		public void Commit(string[] paths, string message, IProgressMonitor monitor) {
			Client.Commit(paths, message, monitor);
		}
		
		public void Mkdir(string[] paths, string message, IProgressMonitor monitor) {
			Client.Mkdir(paths, message, monitor);
		}
		
		public void Checkout (string url, string path, Revision rev, bool recurse, IProgressMonitor monitor) {
			Client.Checkout (url, path, SvnClient.Rev.Head, recurse, monitor);
		}
		
		public void Revert (string[] paths, bool recurse, IProgressMonitor monitor) {
			Client.Revert (paths, recurse, monitor);
		}
		
		public void Add(string path, bool recurse, IProgressMonitor monitor) {
			Client.Add(path, recurse, monitor);
		}
		
		public void Delete(string path, bool force, IProgressMonitor monitor) {
			Client.Delete(path, force, monitor);
		}
		
		public IList List(string path, bool recurse) {
			return Client.List(path, recurse, SvnClient.Rev.Head);
		}
		
		public void Move(string srcPath, string destPath, bool force, IProgressMonitor monitor) {
			Client.Move (srcPath, destPath, SvnClient.Rev.Head, force, monitor);
		}
		
		public string PathDiff (string path, bool recursive)
		{
			return Client.PathDiff (path, SvnClient.Rev.Base, path, SvnClient.Rev.Working, recursive);
		}
		
		private VersionInfo CreateNode (SvnClient.StatusEnt ent, Repository repo) 
		{
			VersionStatus rs = VersionStatus.Unversioned;
			Revision rr = null;
			
			if (ent.RemoteTextStatus != SvnClient.VersionStatus.EMPTY) {
				rs = ConvertStatus(SvnClient.NodeSchedule.Normal, ent.RemoteTextStatus);
				rr = new SvnRevision (repo, 
					ent.LastCommitRevision, 
					ent.LastCommitDate,
					ent.LastCommitAuthor,
					"(unavailable)",
					null
				);
			}
			
			VersionInfo ret = new VersionInfo(
				ent.LocalFilePath,
				ent.Url,
				ent.IsDirectory,
				ConvertStatus(ent.Schedule, ent.TextStatus),
				new SvnRevision(repo, ent.Revision),
				rs,
				rr
			);
			
			return ret;
		}
		
		private VersionInfo[] CreateNodes (Repository repo, IList ent) {
			VersionInfo[] ret = new VersionInfo[ent.Count];
			for (int i = 0; i < ent.Count; i++)
				ret[i] = CreateNode((SvnClient.StatusEnt)ent[i], repo);
			return ret;
		}
		
		private VersionStatus ConvertStatus(SvnClient.NodeSchedule schedule, SvnClient.VersionStatus status) {
			switch (schedule) {
				case SvnClient.NodeSchedule.Add: return VersionStatus.ScheduledAdd;
				case SvnClient.NodeSchedule.Delete: return VersionStatus.ScheduledDelete;
				case SvnClient.NodeSchedule.Replace: return VersionStatus.ScheduledReplace;
			}

			switch (status) {
				case SvnClient.VersionStatus.None: return VersionStatus.Unchanged;
				case SvnClient.VersionStatus.Normal: return VersionStatus.Unchanged;
				case SvnClient.VersionStatus.Unversioned: return VersionStatus.Unversioned;
				case SvnClient.VersionStatus.Modified: return VersionStatus.Modified;
				case SvnClient.VersionStatus.Merged: return VersionStatus.Modified;
				case SvnClient.VersionStatus.Conflicted: return VersionStatus.Conflicted;
				case SvnClient.VersionStatus.Ignored: return VersionStatus.UnversionedIgnored;
				case SvnClient.VersionStatus.Obstructed: return VersionStatus.Obstructed;
			}
			return VersionStatus.Unversioned;
		}
		
		private class SvnRevision : Revision
		{
			public readonly int Rev;
			
			public SvnRevision (Repository repo, int rev): base (repo) 
			{
				Rev = rev;
			}
			
			public SvnRevision (Repository repo, int rev, DateTime time, string author, string message, string[] changedFiles)
			: base (repo, time, author, message, changedFiles)
			{
				Rev = rev;
			}
			
			public override string ToString()
			{
				return Rev.ToString();
			}
		
			public override Revision GetPrevious()
			{
				return new SvnRevision (Repository, Rev-1);
			}
			
			public SvnClient.Rev ForApi() 
			{
				return SvnClient.Rev.Number(Rev);
			}
		}

	}
	

	public class SubversionException : ApplicationException {
		public SubversionException(string message) : base(message) { }
	}
}
