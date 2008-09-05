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
		
		public override bool IsInstalled {
			get {
				return SvnClient.IsInstalled;
			}
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
				if (!IsVersioned (path))
					return null;
				string url = Client.GetPathUrl (path);
				return new SubversionRepository (this, url);
			} catch (Exception ex) {
				// No SVN
				LoggingService.LogError (ex.ToString ());
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
				// Directory check is needed since directory links may look like versioned files
				return File.Exists(GetTextBase(sourcefile)) && !Directory.Exists (sourcefile)
					&& IsVersioned(sourcefile)
					&& GetVersionInfo (repo, sourcefile, false).HasLocalChange (VersionStatus.Modified);
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
			return !ver.IsVersioned;
		}
		
		public string GetPathToBaseText (string sourcefile) {
			return GetTextBase (sourcefile);
		}
		
		public Revision[] GetHistory (Repository repo, string sourcefile, Revision since) {
			ArrayList revs = new ArrayList();
			
			LibSvnClient.Rev startrev;
			string curPath;
			
			startrev = LibSvnClient.Rev.Working;
			curPath = Client.GetPathUrl(sourcefile);
			
			LibSvnClient.Rev sincerev = LibSvnClient.Rev.First;
			if (since != null)
				sincerev = ((SvnRevision) since).ForApi ();
			
			foreach (LibSvnClient.LogEnt log in Client.Log (sourcefile, startrev, sincerev)) {
				ArrayList changedPaths = new ArrayList ();
				foreach (LibSvnClient.LogEntChangedPath chg in log.ChangedPaths) {
					RevisionPath rp = new RevisionPath (chg.Path, chg.Action, chg.ActionDesc);
					changedPaths.Add (rp);
				}
				
				SvnRevision d = new SvnRevision (repo, log.Revision, log.Time, log.Author, log.Message, (RevisionPath[])changedPaths.ToArray(typeof(RevisionPath)));
				revs.Add(d);
				
				// Be aware of the change in path resulting from a copy.
				// Not sure if this works.
				foreach (LibSvnClient.LogEntChangedPath chg in log.ChangedPaths) {
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
			// Check for directory before checking for file, since directory links may appear as files
			if (Directory.Exists (GetDirectoryDotSvn (localPath)) || Directory.Exists (localPath))
				return GetDirStatus (repo, localPath, getRemoteStatus);
			else if (File.Exists (GetTextBase(localPath)) || File.Exists (localPath))
				return GetFileStatus (repo, localPath, getRemoteStatus);
			else
				return null;
		}
		
		private VersionInfo GetFileStatus (Repository repo, string sourcefile, bool getRemoteStatus)
		{
			// If the directory is not versioned, there is no version info
			if (!Directory.Exists (GetDirectoryDotSvn (Path.GetDirectoryName (sourcefile))))
				return null;
			
			IList statuses = Client.Status (sourcefile, LibSvnClient.Rev.Head, false, false, getRemoteStatus);
			if (statuses.Count == 0)
				throw new ArgumentException("Path '" + sourcefile + "' does not exist in the repository.");
			
			LibSvnClient.StatusEnt ent;
			if (statuses.Count != 1)
				throw new ArgumentException("Path '" + sourcefile + "' does not refer to a file in the repository.");
			
			ent = (LibSvnClient.StatusEnt) statuses[0];
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
				
			IList statuses = Client.Status (parent, LibSvnClient.Rev.Head, false, false, getRemoteStatus);
			foreach (LibSvnClient.StatusEnt ent in statuses) {
				if (ent.LocalFilePath == localPath)
					return CreateNode (ent, repo);
			}
			return null;
		}
		
		public VersionInfo[] GetDirectoryVersionInfo (Repository repo, string sourcepath, bool getRemoteStatus, bool recursive) {
			IList ents = Client.Status (sourcepath, LibSvnClient.Rev.Head, recursive, true, getRemoteStatus);
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
			Client.Checkout (url, path, LibSvnClient.Rev.Head, recurse, monitor);
		}
		
		public void Revert (string[] paths, bool recurse, IProgressMonitor monitor) {
			Client.Revert (paths, recurse, monitor);
		}
		
		public void RevertRevision (string path, Revision revision, IProgressMonitor monitor) {
			SvnRevision svnRev = (SvnRevision)revision;
			Client.RevertRevision (path, svnRev.Rev);
		}
		
		public void RevertToRevision (string path, Revision revision, IProgressMonitor monitor) {
			SvnRevision svnRev = (SvnRevision)revision;
			Client.RevertToRevision (path, svnRev.Rev);
		}
		
		public void Add(string path, bool recurse, IProgressMonitor monitor) {
			Client.Add(path, recurse, monitor);
		}
		
		public void Delete(string path, bool force, IProgressMonitor monitor) {
			Client.Delete(path, force, monitor);
		}
		
		public IList List(string path, bool recurse) {
			return Client.List(path, recurse, LibSvnClient.Rev.Head);
		}
		
		public void Move(string srcPath, string destPath, bool force, IProgressMonitor monitor) {
			Client.Move (srcPath, destPath, LibSvnClient.Rev.Head, force, monitor);
		}
		
		public void Lock (IProgressMonitor monitor, string comment, bool stealLock, params string[] paths)
		{
			Client.Lock (monitor, comment, stealLock, paths);
		}
		
		public void Unlock (IProgressMonitor monitor, bool breakLock, params string[] paths)
		{
			Client.Unlock (monitor, breakLock, paths);
		}
		
		public string PathDiff (string path, bool recursive, bool remoteDiff)
		{
			if (remoteDiff)
				return Client.PathDiff (path, LibSvnClient.Rev.Head, path, LibSvnClient.Rev.Working, recursive);
			else
				return Client.PathDiff (path, LibSvnClient.Rev.Base, path, LibSvnClient.Rev.Working, recursive);
		}
		
		private VersionInfo CreateNode (LibSvnClient.StatusEnt ent, Repository repo) 
		{
			VersionStatus rs = VersionStatus.Unversioned;
			Revision rr = null;
			
			if (ent.RemoteTextStatus != LibSvnClient.VersionStatus.EMPTY) {
				rs = ConvertStatus (LibSvnClient.NodeSchedule.Normal, ent.RemoteTextStatus);
				rr = new SvnRevision (repo, ent.LastCommitRevision, ent.LastCommitDate,
				                      ent.LastCommitAuthor, "(unavailable)", null);
			}

			VersionStatus status = ConvertStatus (ent.Schedule, ent.TextStatus);
			
			bool readOnly = File.Exists (ent.LocalFilePath) && (File.GetAttributes (ent.LocalFilePath) & FileAttributes.ReadOnly) != 0;
			
			if (ent.RepoLocked) {
				status |= VersionStatus.LockRequired;
				if (ent.LockOwned)
					status |= VersionStatus.LockOwned;
				else
					status |= VersionStatus.Locked;
			} else if (readOnly)
				status |= VersionStatus.LockRequired;

			VersionInfo ret = new VersionInfo (ent.LocalFilePath, ent.Url, ent.IsDirectory,
			                                   status, new SvnRevision (repo, ent.Revision),
			                                   rs, rr);
			return ret;
		}
		
		private VersionInfo[] CreateNodes (Repository repo, IList ent) {
			VersionInfo[] ret = new VersionInfo[ent.Count];
			for (int i = 0; i < ent.Count; i++)
				ret[i] = CreateNode ((LibSvnClient.StatusEnt) ent[i], repo);
			return ret;
		}
		
		private VersionStatus ConvertStatus (LibSvnClient.NodeSchedule schedule, LibSvnClient.VersionStatus status) {
			switch (schedule) {
				case LibSvnClient.NodeSchedule.Add: return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				case LibSvnClient.NodeSchedule.Delete: return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				case LibSvnClient.NodeSchedule.Replace: return VersionStatus.Versioned | VersionStatus.ScheduledReplace;
			}
			
			switch (status) {
				case LibSvnClient.VersionStatus.None: return VersionStatus.Versioned;
				case LibSvnClient.VersionStatus.Normal: return VersionStatus.Versioned;
				case LibSvnClient.VersionStatus.Unversioned: return VersionStatus.Unversioned;
				case LibSvnClient.VersionStatus.Modified: return VersionStatus.Versioned | VersionStatus.Modified;
				case LibSvnClient.VersionStatus.Merged: return VersionStatus.Versioned | VersionStatus.Modified;
				case LibSvnClient.VersionStatus.Conflicted: return VersionStatus.Versioned | VersionStatus.Conflicted;
				case LibSvnClient.VersionStatus.Ignored: return VersionStatus.Unversioned | VersionStatus.Ignored;
				case LibSvnClient.VersionStatus.Obstructed: return VersionStatus.Versioned;
				case LibSvnClient.VersionStatus.Added: return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				case LibSvnClient.VersionStatus.Deleted: return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				case LibSvnClient.VersionStatus.Replaced: return VersionStatus.Versioned | VersionStatus.ScheduledReplace;
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
			
			public SvnRevision (Repository repo, int rev, DateTime time, string author, string message, RevisionPath[] changedFiles)
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
			
			public LibSvnClient.Rev ForApi () 
			{
				return LibSvnClient.Rev.Number (Rev);
			}
		}

	}
	

	public class SubversionException : ApplicationException {
		public SubversionException(string message) : base(message) { }
	}
}
