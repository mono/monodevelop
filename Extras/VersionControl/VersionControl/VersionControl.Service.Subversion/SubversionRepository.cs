
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace VersionControl.Service.Subversion
{
	class SubversionRepository: UrlBasedRepository
	{
		public SubversionRepository ()
		{
			Method = "svn";
		}
		
		public SubversionRepository (SubversionVersionControl vcs, string url): base (vcs)
		{
			Url = url;
		}
		
		public override bool HasChildRepositories {
			get { return true; }
		}
		
		public override IEnumerable<Repository> ChildRepositories {
			get {
				List<Repository> list = new List<Repository> ();
				
				foreach (SvnClient.DirEnt ent in Svn.List (Url, false)) {
					if (ent.IsDirectory) {
						SubversionRepository rep = new SubversionRepository (Svn, Url + "/" + ent.Name);
						rep.Name = ent.Name;
						list.Add (rep);
					}
				}
				return list;
			}
		}
		
		SubversionVersionControl Svn {
			get { return (SubversionVersionControl) VersionControlSystem; }
		}
		
		public override bool IsModified (string sourcefile)
		{
			return Svn.IsDiffAvailable (this, sourcefile);
		}
		
		public override bool IsVersioned (string sourcefile)
		{
			return Svn.IsVersioned (sourcefile);
		}
		
		public override bool CanAdd (string sourcepath)
		{
			return Svn.CanAdd (this, sourcepath);
		}
		
		public override bool CanCommit (string localPath)
		{
			return Svn.CanCommit (this, localPath);
		}
		
		public override string GetPathToBaseText (string sourcefile)
		{
			return Svn.GetPathToBaseText (sourcefile);
		}
		
		public override string GetTextAtRevision (string repositoryPath, Revision revision)
		{
			return Svn.GetTextAtRevision (repositoryPath, revision);
		}
		
		public override Revision[] GetHistory (string sourcefile, Revision since)
		{
			return Svn.GetHistory (this, sourcefile, since);
		}
		
		public override VersionInfo GetVersionInfo (string localPath, bool getRemoteStatus)
		{
			return Svn.GetVersionInfo (this, localPath, getRemoteStatus);
		}
		
		public override VersionInfo[] GetDirectoryVersionInfo (string sourcepath, bool getRemoteStatus, bool recursive)
		{
			return Svn.GetDirectoryVersionInfo (this, sourcepath, getRemoteStatus, recursive);
		}

		public override Repository Publish (string serverPath, string localPath, string[] files, string message, IProgressMonitor monitor)
		{
			string url = Url;
			if (!serverPath.StartsWith ("/"))
				url += "/";
			url += serverPath;
			
			string[] paths = new string[] {url};
			
			CreateDirectory (paths, message, monitor);
			Svn.Checkout (this.Url + "/" + serverPath, localPath, null, true, monitor);

			Hashtable dirs = new Hashtable ();
			PublishDir (dirs, localPath, false, monitor);

			foreach (string file in files) {
				PublishDir (dirs, Path.GetDirectoryName (file), true, monitor);
				Add (file, false, monitor);
			}
			
			ChangeSet cset = CreateChangeSet (localPath);
			cset.AddFile (localPath);
			cset.GlobalComment = message;
			Commit (cset, monitor);
			
			return new SubversionRepository (Svn, paths[0]);
		}

		void PublishDir (Hashtable dirs, string dir, bool rec, IProgressMonitor monitor)
		{
			while (dir [dir.Length - 1] == Path.DirectorySeparatorChar)
				dir = dir.Substring (0, dir.Length - 1);

			if (dirs.ContainsKey (dir))
				return;

			dirs [dir] = dir;
			if (rec) {
				PublishDir (dirs, Path.GetDirectoryName (dir), true, monitor);
				Add (dir, false, monitor);
			}
		}

		public override void Update (string path, bool recurse, IProgressMonitor monitor)
		{
			Svn.Update (path, recurse, monitor);
		}
		
		public override void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
			ArrayList list = new ArrayList ();
			foreach (ChangeSetItem it in changeSet.Items)
				list.Add (it.LocalPath);
			Svn.Commit ((string[])list.ToArray (typeof(string)), changeSet.GlobalComment, monitor);
		}
		
		void CreateDirectory (string[] paths, string message, IProgressMonitor monitor)
		{
			Svn.Mkdir (paths, message, monitor);
		}
		
		public override void Checkout (string path, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			Svn.Checkout (this.Url, path, rev, recurse, monitor);
		}
		
		public override void Revert (string localPath, bool recurse, IProgressMonitor monitor)
		{
			Svn.Revert (new string[] {localPath}, recurse, monitor);
		}
		
		public override void Add (string path, bool recurse, IProgressMonitor monitor)
		{
			Svn.Add (path, recurse, monitor);
		}
		
		public override void Move (string srcPath, string destPath, Revision revision, bool force, IProgressMonitor monitor)
		{
			Svn.Move (srcPath, destPath, revision, force, monitor);
		}
		
		public override void Delete (string path, bool force, IProgressMonitor monitor)
		{
			Svn.Delete (path, force, monitor);
		}
		
		public override DiffInfo[] PathDiff (string baseLocalPath, string[] localPaths)
		{
			if (localPaths != null) {
				ArrayList list = new ArrayList ();
				foreach (string path in localPaths) {
					string diff = Svn.PathDiff (path, false);
					if (diff == null)
						continue;
					try {
						list.AddRange (GenerateUnifiedDiffInfo (diff, path, new string [] { path }));
					} finally {
						File.Delete (diff);
					}
				}
				return (DiffInfo[]) list.ToArray (typeof(DiffInfo));
			} else {
				string diff = Svn.PathDiff (baseLocalPath, true);
				try {
					return GenerateUnifiedDiffInfo (diff, baseLocalPath, null);
				} finally {
					File.Delete (diff);
				}
			}
		}
	}
}
