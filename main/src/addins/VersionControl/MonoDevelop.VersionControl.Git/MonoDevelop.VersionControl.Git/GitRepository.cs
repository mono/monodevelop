// 
// GitRepository.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

//#define DEBUG_GIT

using System;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide;
using org.eclipse.jgit.lib;
using org.eclipse.jgit.@internal.storage.file;
using org.eclipse.jgit.revwalk;
using org.eclipse.jgit.api;
using org.eclipse.jgit.treewalk;
using org.eclipse.jgit.treewalk.filter;
using org.eclipse.jgit.dircache;
using org.eclipse.jgit.transport;
using org.eclipse.jgit.diff;
using org.eclipse.jgit.merge;
using org.eclipse.jgit.submodule;
using JRepository = org.eclipse.jgit.lib.Repository;

namespace MonoDevelop.VersionControl.Git
{
	[Flags]
	public enum GitUpdateOptions
	{
		None = 0x0,
		SaveLocalChanges = 0x1,
		UpdateSubmodules = 0x2,
		NormalUpdate = SaveLocalChanges | UpdateSubmodules,
	}

	public sealed class GitRepository : UrlBasedRepository
	{
		static readonly byte[] EmptyContent = new byte[0];

		internal LocalGitRepository RootRepository {
			get; set;
		}

		public static event EventHandler BranchSelectionChanged;

		public GitRepository ()
		{
			Url = "git://";
		}

		public GitRepository (FilePath path, string url)
		{
			RootPath = path;
			RootRepository = new LocalGitRepository (path.Combine (Constants.DOT_GIT));
			Url = url;
		}
		
		public override void Dispose ()
		{
			((GitVersionControl)VersionControlSystem).UnregisterRepo (this);
			base.Dispose ();
		}
		
		public override string[] SupportedProtocols {
			get {
				return new string[] {"git", "ssh", "http", "https", "ftp", "ftps", "rsync", "file"};
			}
		}
		
		public override string[] SupportedNonUrlProtocols {
			get {
				return new string[] {"ssh/scp"};
			}
		}
		
		public override bool IsUrlValid (string url)
		{
			try {
				URIish u = new URIish (url);
				if (!string.IsNullOrEmpty (u.getHost ()))
					return true;
			} catch {
			}
			return base.IsUrlValid (url);
		}
		
		public override string Protocol {
			get {
				string p = base.Protocol;
				if (p != null)
					return p;
				return IsUrlValid (Url) ? "ssh/scp" : null;
			}
		}

		public override void CopyConfigurationFrom (Repository other)
		{
			base.CopyConfigurationFrom (other);

			GitRepository r = (GitRepository)other;
			RootPath = r.RootPath;
			if (!RootPath.IsNullOrEmpty)
				RootRepository = new LocalGitRepository (RootPath);
		}

		public override string LocationDescription {
			get { return Url ?? RootPath; }
		}

		public override bool AllowLocking {
			get { return false; }
		}

		public override string GetBaseText (FilePath localFile)
		{
			RevCommit c = GetHeadCommit (GetRepository (localFile));
			if (c == null)
				return string.Empty;
			return GetCommitTextContent (c, localFile);
		}
				
		static RevCommit GetHeadCommit (JRepository repository)
		{
			RevWalk rw = new RevWalk (repository);
			ObjectId headId = repository.resolve (Constants.HEAD);
			if (headId == null)
				return null;
			return rw.parseCommit (headId);
		}

		public StashCollection GetStashes ()
		{
			return GetStashes (RootRepository);
		}

		public static StashCollection GetStashes (JRepository repository)
		{
			return new StashCollection (repository);
		}

		DateTime cachedSubmoduleTime = DateTime.MinValue;
		Tuple<FilePath, JRepository>[] cachedSubmodules = new Tuple<FilePath, JRepository>[0];
		Tuple<FilePath, JRepository>[] CachedSubmodules {
			get {
				var submoduleWriteTime = File.GetLastWriteTimeUtc(RootPath.Combine(".gitmodule"));
				if (cachedSubmoduleTime != submoduleWriteTime) {
					cachedSubmoduleTime = submoduleWriteTime;
					var submoduleStatus = new org.eclipse.jgit.api.Git (RootRepository)
						.submoduleStatus ()
						.call ();

					var list = new List<string> (submoduleStatus.size ());
					var iterator = submoduleStatus.keySet ().iterator ();
					while (iterator.hasNext ())
						list.Add ((string)iterator.next ());

					cachedSubmodules = list.Select (
						s => Tuple.Create ((FilePath)s, SubmoduleWalk.getSubmoduleRepository (RootRepository, s))).ToArray ();
				}
				return cachedSubmodules;
			}
		}

		JRepository GetRepository (FilePath localPath)
		{
			return GroupByRepository (new [] { localPath }).First ().Key;
		}

		IEnumerable<IGrouping<JRepository, FilePath>> GroupByRepository (IEnumerable<FilePath> files)
		{
			var cache = CachedSubmodules;
			return files.GroupBy (f => cache.Where (s => {
					var fullPath = s.Item1.ToAbsolute (RootPath);
					return f.IsChildPathOf (fullPath) || f.CanonicalPath == fullPath.CanonicalPath;
				}).Select (s => s.Item2).FirstOrDefault () ?? RootRepository);
		}

		protected override Revision[] OnGetHistory (FilePath localFile, Revision since)
		{
			List<Revision> revs = new List<Revision> ();
		
			var repository = GetRepository (localFile);
			var hc = GetHeadCommit (repository);
			if (hc == null)
				return new GitRevision [0];

			RevWalk walk = new RevWalk (repository);
			string path = repository.ToGitPath (localFile);
			if (path != ".")
				walk.setTreeFilter (FollowFilter.create (path, (DiffConfig)repository.getConfig ().get (DiffConfig.KEY)));
			walk.markStart (hc);
			
			foreach (RevCommit commit in walk) {
				PersonIdent author = commit.getAuthorIdent ();
				var dt = new DateTime (author.getWhen ().getTime ());
				GitRevision rev = new GitRevision (this, repository, commit.getId().getName(), dt.ToLocalTime (), author.getName (), commit.getFullMessage ());
				rev.Email = author.getEmailAddress ();
				rev.ShortMessage = commit.getShortMessage ();
				rev.Commit = commit;
				rev.FileForChanges = localFile;
				revs.Add (rev);
			}
			return revs.ToArray ();
		}
		
		protected override RevisionPath[] OnGetRevisionChanges (Revision revision)
		{
			GitRevision rev = (GitRevision) revision;
			if (rev.Commit == null)
				return new RevisionPath [0];

			List<RevisionPath> paths = new List<RevisionPath> ();
			var li = GitUtil.GetCommitChanges (rev.GitRepository, rev.Commit).listIterator ();
			while (li.hasNext ()) {
				var entry = (DiffEntry)li.next ();
				if (entry.getChangeType () == DiffEntry.ChangeType.ADD)
					paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.getNewPath ()), RevisionAction.Add, null));
				if (entry.getChangeType () == DiffEntry.ChangeType.DELETE)
					paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.getOldPath ()), RevisionAction.Delete, null));
				if (entry.getChangeType () == DiffEntry.ChangeType.MODIFY)
					paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.getNewPath ()), RevisionAction.Modify, null));
			}
			return paths.ToArray ();
		}

		protected override IEnumerable<VersionInfo> OnGetVersionInfo (IEnumerable<FilePath> paths, bool getRemoteStatus)
		{
			return GetDirectoryVersionInfo (FilePath.Null, paths, getRemoteStatus, false);
		}

		protected override VersionInfo[] OnGetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			return GetDirectoryVersionInfo (localDirectory, null, getRemoteStatus, recursive);
		}

		// Used for checking if we will dupe data.
		// This way we reduce the number of GitRevisions created and RevWalks done.
		JRepository versionInfoCacheRepository;
		GitRevision versionInfoCacheRevision;
		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, IEnumerable<FilePath> localFileNames, bool getRemoteStatus, bool recursive)
		{
			List<VersionInfo> versions = new List<VersionInfo> ();
			HashSet<FilePath> existingFiles = new HashSet<FilePath> ();
			HashSet<FilePath> nonVersionedMissingFiles = new HashSet<FilePath> ();
			
			if (localFileNames != null) {
				var localFiles = new List<FilePath> ();
				foreach (var group in GroupByRepository (localFileNames)) {
					var repository = group.Key;
					var arev = new GitRevision (this, repository, "");
					foreach (var p in group) {
						if (Directory.Exists (p)) {
							if (recursive)
								versions.AddRange (GetDirectoryVersionInfo (p, getRemoteStatus, true));
							versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
						}
						else {
							localFiles.Add (p);
							if (File.Exists (p))
								existingFiles.Add (p.CanonicalPath);
							else
								nonVersionedMissingFiles.Add (p.CanonicalPath);
						}
					}
				}
				// No files to check, we are done
				if (localFiles.Count == 0)
					return versions.ToArray ();
				
				localFileNames = localFiles;
			} else {
				List<FilePath> directories = new List<FilePath> ();
				CollectFiles (existingFiles, directories, localDirectory, recursive);
				foreach (var group in GroupByRepository (directories)) {
					var repository = group.Key;
					var arev = new GitRevision (this, repository, "");
					foreach (var p in group)
						versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
				}
			}

			IEnumerable<FilePath> paths;
			if (localFileNames == null) {
				if (recursive) {
					paths = new [] { localDirectory };
				} else {
					if (Directory.Exists (localDirectory))
						paths = Directory.GetFiles (localDirectory).Select (f => (FilePath)f);
					else
						paths = new FilePath [0];
				}
			} else {
				paths = localFileNames;
			}

			foreach (var group in GroupByRepository (paths)) {
				var repository = group.Key;

				GitRevision rev = null;
				if (versionInfoCacheRepository == null || versionInfoCacheRepository != repository) {
					versionInfoCacheRepository = repository;
					RevCommit headCommit = GetHeadCommit (repository);
					if (headCommit != null) {
						rev = new GitRevision (this, repository, headCommit.getId().getName());
						versionInfoCacheRevision = rev;
					}
				} else
					rev = versionInfoCacheRevision;

				GetDirectoryVersionInfoCore (repository, rev, group, existingFiles, nonVersionedMissingFiles, versions);
				
				// Existing files for which git did not report a status are supposed to be tracked
				foreach (FilePath file in existingFiles.Where (f => group.Contains (f))) {
					VersionInfo vi = new VersionInfo (file, "", false, VersionStatus.Versioned, rev, VersionStatus.Versioned, null);
					versions.Add (vi);
				}
			}


			// Non existing files for which git did not report an status are unversioned
			foreach (FilePath file in nonVersionedMissingFiles)
				versions.Add (VersionInfo.CreateUnversioned (file, false));

			return versions.ToArray ();
		}

		static void GetDirectoryVersionInfoCore (JRepository repository, GitRevision rev, IEnumerable<FilePath> localPaths, HashSet<FilePath> existingFiles, HashSet<FilePath> nonVersionedMissingFiles, List<VersionInfo> versions)
		{
			var filteredStatus = new org.eclipse.jgit.api.Git (repository).status ();
			foreach (var path in repository.ToGitPath (localPaths))
				if (path != ".")
					filteredStatus.addPath (path);

			var status = filteredStatus.call ();
			HashSet<string> added = new HashSet<string> ();
			Action<java.util.Set, VersionStatus> AddFiles = delegate (java.util.Set files, VersionStatus fstatus) {
				var iterator = files.iterator ();
				while (iterator.hasNext ()) {
					string file = (string)iterator.next();
					if (!added.Add (file))
						continue;
					FilePath statFile = repository.FromGitPath (file);
					existingFiles.Remove (statFile.CanonicalPath);
					nonVersionedMissingFiles.Remove (statFile.CanonicalPath);
					versions.Add (new VersionInfo (statFile, "", false, fstatus, rev, fstatus == VersionStatus.Ignored ? VersionStatus.Unversioned : VersionStatus.Versioned, null));
				}
			};

			AddFiles (status.getAdded (), VersionStatus.Versioned | VersionStatus.ScheduledAdd);
			AddFiles (status.getChanged (), VersionStatus.Versioned | VersionStatus.Modified);
			AddFiles (status.getModified (), VersionStatus.Versioned | VersionStatus.Modified);
			AddFiles (status.getRemoved (), VersionStatus.Versioned | VersionStatus.ScheduledDelete);
			AddFiles (status.getMissing (), VersionStatus.Versioned | VersionStatus.ScheduledDelete);
			AddFiles (status.getConflicting (), VersionStatus.Versioned | VersionStatus.Conflicted);
			AddFiles (status.getUntracked (), VersionStatus.Unversioned);
			AddFiles (status.getIgnoredNotInIndex (), VersionStatus.Ignored);
		}
		
		protected override VersionControlOperation GetSupportedOperations (VersionInfo vinfo)
		{
			VersionControlOperation ops = base.GetSupportedOperations (vinfo);
			if (GetCurrentRemote () == null)
				ops &= ~VersionControlOperation.Update;
			if (vinfo.IsVersioned && !vinfo.IsDirectory)
				ops |= VersionControlOperation.Annotate;
			return ops;
		}

		static void CollectFiles (HashSet<FilePath> files, List<FilePath> directories, FilePath dir, bool recursive)
		{
			if (!Directory.Exists (dir))
				return;
			foreach (string file in Directory.GetFiles (dir))
				files.Add (new FilePath (file).CanonicalPath);

			foreach (string sub in Directory.GetDirectories (dir)) {
				directories.Add (new FilePath (sub));
				if (recursive)
					CollectFiles (files, directories, sub, true);
			}
		}

		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
		{
			// Initialize the repository
			RootRepository = GitUtil.Init (localPath, Url);
			var git = new org.eclipse.jgit.api.Git (RootRepository);
			try {
				var refs = git.fetch ().call ().getAdvertisedRefs ();
				if (refs.size () > 0) {
					throw new UserException ("The remote repository already contains branches. Publishing is only possible to an empty repository");
				}
			} catch {
				try {
					RootRepository.close ();
				} catch {
				
				}
				if (Directory.Exists (RootRepository.getDirectory ().toString ()))
					Directory.Delete (RootRepository.getDirectory ().toString (), true);
				RootRepository = null;
				throw;
			}

			RootPath = localPath;
			// Add the project files
			ChangeSet cs = CreateChangeSet (localPath);
			var cmd = git.add ();
			foreach (FilePath fp in files) {
				cmd.addFilepattern (RootRepository.ToGitPath (fp));
				cs.AddFile (fp);
			}
			cmd.call ();
			
			// Create the initial commit
			cs.GlobalComment = message;
			Commit (cs, monitor);

			// Push to remote repo
			Push (monitor, "origin", "master");

			return this;
		}
		
		protected override void OnUpdate (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			IEnumerable<DiffEntry> statusList = null;
			
			monitor.BeginTask (GettextCatalog.GetString ("Updating"), 5);

			// Fetch remote commits
			Fetch (monitor);
			string upstreamRef = GitUtil.GetUpstreamSource (RootRepository, GetCurrentBranch ());
			if (upstreamRef == null)
				upstreamRef = GetCurrentRemote () + "/" + GetCurrentBranch ();

			GitUpdateOptions options = GitService.StashUnstashWhenUpdating ? GitUpdateOptions.NormalUpdate : GitUpdateOptions.UpdateSubmodules;
			if (GitService.UseRebaseOptionWhenPulling)
				Rebase (upstreamRef, options, monitor);
			else
				Merge (upstreamRef, options, monitor);

			monitor.Step (1);
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (monitor, statusList);
			
			monitor.EndTask ();
		}

		public void Fetch (IProgressMonitor monitor)
		{
			string remote = GetCurrentRemote ();
			if (remote == null)
				throw new InvalidOperationException ("No remotes defined");

			monitor.Log.WriteLine (GettextCatalog.GetString ("Fetching from '{0}'", remote));
			var fetch = new org.eclipse.jgit.api.Git (RootRepository).fetch ();
			using (var gm = new GitMonitor (monitor)) {
				fetch.setRemote (remote);
				fetch.setProgressMonitor (gm);
				fetch.call ();
			}
			monitor.Step (1);
		}

		bool GetSubmodulesToUpdate (List<string> updateSubmodules)
		{
			List<string> dirtySubmodules = new List<string> ();

			// Iterate submodules and do status.
			// SubmoduleStatus does not report changes for dirty submodules.
			foreach (var submodule in CachedSubmodules) {
				var submoduleGit = new org.eclipse.jgit.api.Git (submodule.Item2);
				var statusCommand = submoduleGit.status ();
				var status = statusCommand.call ();

				if (status.isClean ())
					updateSubmodules.Add (submodule.Item1);
				else
					dirtySubmodules.Add (submodule.Item1);
			}

			if (dirtySubmodules.Count != 0) {
				StringBuilder submodules = new StringBuilder (Environment.NewLine + Environment.NewLine);
				foreach (var item in dirtySubmodules)
					submodules.AppendLine (item);

				AlertButton response = MessageService.GenericAlert (
					MonoDevelop.Ide.Gui.Stock.Question,
					GettextCatalog.GetString ("You have local changes in the submodules below"),
					GettextCatalog.GetString ("Do you want continue? Detached HEADs will have their changes lost.{0}", submodules.ToString ()),
					new AlertButton[] {
							AlertButton.No,
							new AlertButton ("Only unchanged"),
							AlertButton.Yes
						}
				);

				if (response == AlertButton.No)
					return false;

				if (response == AlertButton.Yes)
					updateSubmodules.AddRange (dirtySubmodules);
			}
			return true;
		}

		[Obsolete ("Will be removed. Please use the one with GitUpdateOptions flags.")]
		public void Rebase (string upstreamRef, bool saveLocalChanges, IProgressMonitor monitor)
		{
			Rebase (upstreamRef, saveLocalChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor);
		}

		public void Rebase (string upstreamRef, GitUpdateOptions options, IProgressMonitor monitor)
		{
			StashCollection stashes = GitUtil.GetStashes (RootRepository);
			Stash stash = null;
			var git = new org.eclipse.jgit.api.Git (RootRepository);
			try
			{
				monitor.BeginTask (GettextCatalog.GetString ("Rebasing"), 5);
				List<string> UpdateSubmodules = new List<string> ();

				// TODO: Fix stash so we don't have to do update before the main repo update.
				if ((options & GitUpdateOptions.UpdateSubmodules) == GitUpdateOptions.UpdateSubmodules) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Checking repository submodules"));
					if (!GetSubmodulesToUpdate (UpdateSubmodules))
						return;

					monitor.Log.WriteLine (GettextCatalog.GetString ("Updating repository submodules"));
					var submoduleUpdate = git.submoduleUpdate ();
					foreach (var submodule in UpdateSubmodules)
						submoduleUpdate.addPath (submodule);

					submoduleUpdate.call ();
					monitor.Step (1);
				}

				if ((options & GitUpdateOptions.SaveLocalChanges) != GitUpdateOptions.SaveLocalChanges) {
					const VersionStatus unclean = VersionStatus.Modified | VersionStatus.ScheduledAdd | VersionStatus.ScheduledDelete;
					bool modified = false;
					if (GetDirectoryVersionInfo (RootPath, false, true).Any (v => (v.Status & unclean) != VersionStatus.Unversioned))
						modified = true;

					if (modified) {
						if (MessageService.GenericAlert (
							MonoDevelop.Ide.Gui.Stock.Question,
							GettextCatalog.GetString ("You have uncommitted changes"),
							GettextCatalog.GetString ("What do you want to do?"),
							AlertButton.Cancel,
							new AlertButton ("Stash")) == AlertButton.Cancel)
							return;

						options |= GitUpdateOptions.SaveLocalChanges;
					}
				}

				if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Saving local changes"));
					using (var gm = new GitMonitor (monitor))
						stash = stashes.Create (gm, GetStashName ("_tmp_"));
					monitor.Step (1);
				}

				RebaseCommand rebase = git.rebase ();
				rebase.setOperation (RebaseCommand.Operation.BEGIN);
				rebase.setUpstream (upstreamRef);
				var gmonitor = new GitMonitor (monitor);
				rebase.setProgressMonitor (gmonitor);
				
				bool aborted = false;
				
				try {
					var result = rebase.call ();
					while (!aborted && result.getStatus () == RebaseResult.Status.STOPPED) {
						rebase = git.rebase ();
						rebase.setProgressMonitor (gmonitor);
						rebase.setOperation (RebaseCommand.Operation.CONTINUE);
						bool commitChanges = true;
						var conflicts = GitUtil.GetConflictedFiles (RootRepository);
						foreach (string conflictFile in conflicts) {
							ConflictResult res = ResolveConflict (RootRepository.FromGitPath (conflictFile));
							if (res == ConflictResult.Abort) {
								aborted = true;
								commitChanges = false;
								rebase.setOperation (RebaseCommand.Operation.ABORT);
								break;
							} else if (res == ConflictResult.Skip) {
								rebase.setOperation (RebaseCommand.Operation.SKIP);
								commitChanges = false;
								break;
							}
						}
						if (commitChanges) {
							var cmd = git.add ();
							foreach (string conflictFile in conflicts)
								cmd.addFilepattern (conflictFile);
							cmd.call ();
						}
						result = rebase.call ();
					}

					if ((options & GitUpdateOptions.UpdateSubmodules) == GitUpdateOptions.UpdateSubmodules) {
						monitor.Log.WriteLine (GettextCatalog.GetString ("Updating repository submodules"));
						var submoduleUpdate = git.submoduleUpdate ();
						foreach (var submodule in UpdateSubmodules)
							submoduleUpdate.addPath (submodule);

						submoduleUpdate.call ();
					}
				} catch {
					if (!aborted) {
						rebase = git.rebase ();
						rebase.setOperation (RebaseCommand.Operation.ABORT);
						rebase.setProgressMonitor (gmonitor);
						rebase.call ();
					}
					throw;
				} finally {
					gmonitor.Dispose ();
				}
				
			} finally {
				if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges)
					monitor.Step (1);
				
				// Restore local changes
				if (stash != null) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					using (var gm = new GitMonitor (monitor))
						stash.Apply (gm);
					stashes.Remove (stash);
				}
				monitor.EndTask ();
			}
		}

		[Obsolete ("Will be removed. Please use the one with GitUpdateOptions flags.")]
		public void Merge (string upstreamRef, bool saveLocalChanges, IProgressMonitor monitor)
		{
			Merge (upstreamRef, saveLocalChanges ? GitUpdateOptions.SaveLocalChanges : GitUpdateOptions.None, monitor);
		}

		public void Merge (string branch, GitUpdateOptions options, IProgressMonitor monitor)
		{
			java.util.List statusList = null;
			Stash stash = null;
			StashCollection stashes = GetStashes (RootRepository);
			var git = new org.eclipse.jgit.api.Git (RootRepository);

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Merging"), 5);
				List<string> UpdateSubmodules = new List<string> ();

				// TODO: Fix stash so we don't have to do update before the main repo update.
				if ((options & GitUpdateOptions.UpdateSubmodules) == GitUpdateOptions.UpdateSubmodules) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Checking repository submodules"));
					if (!GetSubmodulesToUpdate (UpdateSubmodules))
						return;

					monitor.Log.WriteLine (GettextCatalog.GetString ("Updating repository submodules"));
					var submoduleUpdate = git.submoduleUpdate ();
					foreach (var submodule in UpdateSubmodules)
						submoduleUpdate.addPath (submodule);

					submoduleUpdate.call ();
					monitor.Step (1);
				}

				// Get a list of files that are different in the target branch
				statusList = GitUtil.GetChangedFiles (RootRepository, branch);
				monitor.Step (1);

				if ((options & GitUpdateOptions.SaveLocalChanges) != GitUpdateOptions.SaveLocalChanges) {
					const VersionStatus unclean = VersionStatus.Modified | VersionStatus.ScheduledAdd | VersionStatus.ScheduledDelete;
					bool modified = false;
					if (GetDirectoryVersionInfo (RootPath, false, true).Any (v => (v.Status & unclean) != VersionStatus.Unversioned))
						modified = true;

					if (modified) {
						if (MessageService.GenericAlert (
							MonoDevelop.Ide.Gui.Stock.Question,
							GettextCatalog.GetString ("You have uncommitted changes"),
							GettextCatalog.GetString ("What do you want to do?"),
							AlertButton.Cancel,
							new AlertButton ("Stash")) == AlertButton.Cancel)
							return;

						options |= GitUpdateOptions.SaveLocalChanges;
					}
				}

				if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Saving local changes"));
					using (var gm = new GitMonitor (monitor))
						stash = stashes.Create (gm, GetStashName ("_tmp_"));
					monitor.Step (1);
				}
				
				// Apply changes
				
				ObjectId branchId = RootRepository.resolve (branch);

				org.eclipse.jgit.api.MergeResult mergeResult = git.merge ().setStrategy (MergeStrategy.RESOLVE).include (branchId).call ();
				if (mergeResult.getMergeStatus () == org.eclipse.jgit.api.MergeResult.MergeStatus.CONFLICTING ||
					mergeResult.getMergeStatus () == org.eclipse.jgit.api.MergeResult.MergeStatus.FAILED) {
					var conflicts = mergeResult.getConflicts ();
					bool commit = true;
					if (conflicts != null) {
						var it = conflicts.keySet ().iterator ();
						while (it.hasNext ()) {
							string conflictFile = (string)it.next ();
							ConflictResult res = ResolveConflict (RootRepository.FromGitPath (conflictFile));
							if (res == ConflictResult.Abort) {
								GitUtil.HardReset (RootRepository, GetHeadCommit (RootRepository));
								commit = false;
								break;
							} else if (res == ConflictResult.Skip) {
								Revert (RootRepository.FromGitPath (conflictFile), false, monitor);
								break;
							}
						}
					}
					if (commit)
						git.commit ().call ();
				}

				if ((options & GitUpdateOptions.UpdateSubmodules) == GitUpdateOptions.UpdateSubmodules) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Updating repository submodules"));
					var submoduleUpdate = git.submoduleUpdate ();
					foreach (var submodule in CachedSubmodules)
						submoduleUpdate.addPath (submodule.Item1);

					submoduleUpdate.call ();
					monitor.Step (1);
				}
			} finally {
				if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges)
					monitor.Step (1);
				
				// Restore local changes
				if (stash != null) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					using (var gm = new GitMonitor (monitor))
						stash.Apply (gm);
					stashes.Remove (stash);
					monitor.Step (1);
				}
				monitor.EndTask ();
			}

			var list = new List<DiffEntry> (statusList.size ());
			var iterator = statusList.listIterator ();
			while (iterator.hasNext ())
				list.Add ((DiffEntry)iterator.next ());

			// Notify changes
			if (statusList != null)
				NotifyFileChanges (monitor, list);
		}

		static ConflictResult ResolveConflict (string file)
		{
			ConflictResult res = ConflictResult.Abort;
			DispatchService.GuiSyncDispatch (delegate {
				ConflictResolutionDialog dlg = new ConflictResolutionDialog ();
				dlg.Load (file);
				try {
					dlg.Load (file);
					var dres = (Gtk.ResponseType) MessageService.RunCustomDialog (dlg);
					dlg.Hide ();
					switch (dres) {
					case Gtk.ResponseType.Cancel:
						res = ConflictResult.Abort;
						break;
					case Gtk.ResponseType.Close:
						res = ConflictResult.Skip;
						break;
					case Gtk.ResponseType.Ok:
						res = ConflictResult.Continue;
						dlg.Save (file);
						break;
					}
				} finally {
					dlg.Destroy ();
				}
			});
			return res;
		}

		protected override void OnCommit (ChangeSet changeSet, IProgressMonitor monitor)
		{
			string message = changeSet.GlobalComment;
			if (string.IsNullOrEmpty (message))
				throw new ArgumentException ("Commit message must not be null or empty!", "message");
			
			var git = new org.eclipse.jgit.api.Git (RootRepository);
			var commit = git.commit ();
			commit.setMessage (message);

			if (changeSet.ExtendedProperties.Contains ("Git.AuthorName")) {
				commit.setAuthor ((string)changeSet.ExtendedProperties ["Git.AuthorName"], (string)changeSet.ExtendedProperties ["Git.AuthorEmail"]);
			}
			
			foreach (string path in GetFilesInPaths (changeSet.Items.Select (i => i.LocalPath)))
				commit.setOnly (path);
			
			commit.call ();
		}
		
		List<string> GetFilesInPaths (IEnumerable<FilePath> paths)
		{
			// Expand directory paths into file paths. Convert paths to full paths.
			List<string> filePaths = new List<string> ();
			foreach (var path in paths) {
				DirectoryInfo dir = new DirectoryInfo (path);
				if (dir.Exists)
					filePaths.AddRange (GetDirectoryFiles (dir));
				else
					filePaths.Add (RootRepository.ToGitPath (path));
			}
			return filePaths;
		}
		
		IEnumerable<string> GetDirectoryFiles (DirectoryInfo dir)
		{
			FileTreeIterator iter = new FileTreeIterator (new java.io.File (dir.FullName), RootRepository.getFS (),
				(WorkingTreeOptions)WorkingTreeOptions.KEY.parse(RootRepository.getConfig()));
			while (!iter.eof()) {
				var file = iter.getEntryFile ();
				if (file != null && !iter.isEntryIgnored ())
					yield return file.getPath ();
				iter.next (1);
			}
		}

		public bool IsUserInfoDefault ()
		{
			UserConfig config = (UserConfig)RootRepository.getConfig ().get (UserConfig.KEY);
			return config.isCommitterNameImplicit () && config.isCommitterEmailImplicit ();
		}
		
		public void GetUserInfo (out string name, out string email)
		{
			UserConfig config = (UserConfig)RootRepository.getConfig ().get (UserConfig.KEY);
			name = config.getCommitterName ();
			email = config.getCommitterEmail ();
		}

		public void SetUserInfo (string name, string email)
		{
			StoredConfig config = RootRepository.getConfig ();
			config.setString ("user", null, "name", name);
			config.setString ("user", null, "email", email);
			config.save ();
		}

		protected override void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			var cmd = org.eclipse.jgit.api.Git.cloneRepository ();
			cmd.setURI (Url);
			cmd.setRemote ("origin");
			cmd.setBranch ("refs/heads/master");
			cmd.setDirectory (new java.io.File (targetLocalPath));
			cmd.setCloneSubmodules (true);
			using (var gm = new GitMonitor (monitor, 4)) {
				cmd.setProgressMonitor (gm);
				try {
					cmd.call ();
				} catch (Exception e) {
					e = ikvm.runtime.Util.mapException (e);
					if (e is org.eclipse.jgit.api.errors.JGitInternalException) {
						// We cancelled and NGit throws.
						// Or URL is wrong.
						if (e.InnerException is org.eclipse.jgit.errors.MissingObjectException ||
							e.InnerException is org.eclipse.jgit.errors.TransportException ||
							e.InnerException is org.eclipse.jgit.errors.NotSupportedException) {
							FileService.DeleteDirectory (targetLocalPath);
							throw new VersionControlException (e.InnerException.Message);
							return;
						}
					}
					throw;
				}
			}
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			// Replace with org.eclipse.jgit.api.Git.Reset ()
			// FIXME: we lack info about what happened to files
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				var files = group.ToArray ();

			var c = GetHeadCommit (repository);
			RevTree tree = c != null ? c.getTree () : null;
			
			List<FilePath> changedFiles = new List<FilePath> ();
			List<FilePath> removedFiles = new List<FilePath> ();
			
			monitor.BeginTask (GettextCatalog.GetString ("Reverting files"), 3);
			monitor.BeginStepTask (GettextCatalog.GetString ("Reverting files"), files.Length, 2);
			
			DirCache dc = repository.lockDirCache ();
			DirCacheBuilder builder = dc.builder ();
			
			try {
				HashSet<string> entriesToRemove = new HashSet<string> ();
				HashSet<string> foldersToRemove = new HashSet<string> ();
				
				// Add the new entries
				foreach (FilePath fp in files) {
					string p = repository.ToGitPath (fp);
					
					// Register entries to be removed from the index
					if (Directory.Exists (fp))
						foldersToRemove.Add (p);
					else
						entriesToRemove.Add (p);
					
					TreeWalk tw = tree != null ? TreeWalk.forPath (repository, p, tree) : null;
					if (tw == null) {
						// Removed from the index
					}
					else {
						// Add new entries
						
						TreeWalk r;
						if (tw.isSubtree ()) {
							// It's a directory. Make sure we remove existing index entries of this directory
							foldersToRemove.Add (p);
							
							// We have to iterate through all folder files. We need a new iterator since the
							// existing rw is not recursive
							r = new TreeWalk(repository);
							r.reset (tree);
							r.setFilter (PathFilterGroup.createFromStrings(new string[]{p}));
							r.setRecursive (true);
							r.next ();
						} else {
							r = tw;
						}
						
						do {
							// There can be more than one entry if reverting a whole directory
							string rpath = repository.FromGitPath (r.getPathString());
							DirCacheEntry e = new DirCacheEntry (r.getPathString());
							e.setObjectId (r.getObjectId (0));
							e.setFileMode (r.getFileMode (0));
							if (!Directory.Exists (Path.GetDirectoryName (rpath)))
								Directory.CreateDirectory (rpath);
							DirCacheCheckout.checkoutEntry (repository, new java.io.File (rpath), e);
							builder.add (e);
							changedFiles.Add (rpath);
						} while (r.next ());
					}
					monitor.Step (1);
				}
				
				// Add entries we want to keep
				int count = dc.getEntryCount ();
				for (int n=0; n<count; n++) {
					DirCacheEntry e = dc.getEntry (n);
					string path = e.getPathString ();
					if (!entriesToRemove.Contains (path) && !foldersToRemove.Any (f => IsSubpath (f,path)))
						builder.add (e);
				}
				
				builder.commit ();
			}
			catch {
				dc.unlock ();
				throw;
			}
			
			monitor.EndTask ();
			monitor.BeginTask (null, files.Length);

			foreach (FilePath p in changedFiles) {
				FileService.NotifyFileChanged (p, true);
				monitor.Step (1);
			}
			foreach (FilePath p in removedFiles) {
				FileService.NotifyFileRemoved (p);
				monitor.Step (1);
			}
			monitor.EndTask ();
			}
		}
		
		static bool IsSubpath (string basePath, string childPath)
		{
			// FIXME: use FilePath?
			if (basePath [basePath.Length - 1] == '/')
				return childPath.StartsWith (basePath, StringComparison.InvariantCulture);
			return childPath.StartsWith (basePath + "/", StringComparison.InvariantCulture);
		}

		protected override void OnRevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			var git = new org.eclipse.jgit.api.Git (GetRepository (localPath));
			var gitRev = (GitRevision)revision;
			var revert = git.revert ().include (gitRev.Commit.toObjectId ());
			revert.call ();

			var revertResult = revert.getFailingResult ();
			if (revertResult == null) {
				monitor.ReportSuccess (GettextCatalog.GetString ("Revision {0} successfully reverted", gitRev));
			} else {
				var errorMessage = GettextCatalog.GetString ("Could not revert commit {0}", gitRev);
				var description = new StringBuilder (GettextCatalog.GetString ("The following files had merge conflicts")).AppendLine ();
				var iterator = revertResult.getFailingPaths ().keySet ().iterator ();
				while (iterator.hasNext ())
					description.AppendLine ((string)iterator.next ());

				monitor.ReportError (errorMessage, new UserException (errorMessage, description.ToString ()));
			} 
		}


		protected override void OnRevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			JRepository repo = GetRepository (localPath);
			var git = new org.eclipse.jgit.api.Git (repo);
			GitRevision gitRev = (GitRevision)revision;

			// Rewrite file data from selected revision.
			foreach (var path in GetFilesInPaths (new FilePath[] { localPath })) {
				MonoDevelop.Projects.Text.TextFile.WriteFile (path, GetCommitTextContent (gitRev.Commit, path), null);
			}

			monitor.ReportSuccess (GettextCatalog.GetString ("Successfully reverted {0} to revision {1}", localPath, gitRev));
		}


		protected override void OnAdd (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				var files = group;

				var git = new org.eclipse.jgit.api.Git (repository);
				var cmd = git.add ();
				foreach (FilePath fp in files)
					cmd.addFilepattern (repository.ToGitPath (fp));
				cmd.call ();
			}
		}

		[Obsolete ("Use the overload with keepLocal parameter")]
		protected override void OnDeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
		}

		protected override void OnDeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor, bool keepLocal)
		{
			DeleteCore (localPaths, force, monitor, keepLocal);

			foreach (var path in localPaths) {
				if (keepLocal) {
					// Undo addition of files.
					VersionInfo info = GetVersionInfo (path, VersionInfoQueryFlags.IgnoreCache);
					if (info != null && info.HasLocalChange (VersionStatus.ScheduledAdd)) {
						// Revert addition.
						Revert (path, false, monitor);
					}
				} else {
					// Untracked files are not deleted by the rm command, so delete them now
					if (File.Exists (path))
						File.Delete (path);
				}
			}
		}

		[Obsolete ("Use the overload with keepLocal parameter")]
		protected override void OnDeleteDirectories (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
		}

		protected override void OnDeleteDirectories (FilePath[] localPaths, bool force, IProgressMonitor monitor, bool keepLocal)
		{
			DeleteCore (localPaths, force, monitor, keepLocal);

			foreach (var path in localPaths) {
				if (keepLocal) {
					// Undo addition of directories and files.
					foreach (var info in GetDirectoryVersionInfo (path, false, true)) {
						if (info != null && info.HasLocalChange (VersionStatus.ScheduledAdd)) {
							// Revert addition.
							Revert (path, false, monitor);
						}
					}
				} else {
					// Untracked files are not deleted by the rm command, so delete them now
					foreach (var f in localPaths)
						if (Directory.Exists (f))
							Directory.Delete (f, true);
				}
			}
		}

		void DeleteCore (FilePath[] localPaths, bool force, IProgressMonitor monitor, bool keepLocal)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				List<FilePath> backupFiles = new List<FilePath> ();
				var repository = group.Key;
				var files = group;

				var git = new org.eclipse.jgit.api.Git (repository);
				RmCommand rm = git.rm ();
				foreach (var f in files) {
					// Create backups.
					if (keepLocal) {
						string newPath = "";
						if (f.IsDirectory) {
							newPath = FileService.CreateTempDirectory ();
							FileService.CopyDirectory (f, newPath);
						} else {
							newPath = Path.GetTempFileName ();
							File.Copy (f, newPath, true);
						}
						backupFiles.Add (newPath);
					}

					rm.addFilepattern (repository.ToGitPath (f));
				}

				try {
					rm.call ();
				} finally {
					// Restore backups.
					if (keepLocal) {
						foreach (var backup in backupFiles) {
							if (backup.IsDirectory)
								FileService.MoveDirectory (backup, files.ElementAt (backupFiles.IndexOf (backup)));
							else
								File.Move (backup, files.ElementAt (backupFiles.IndexOf (backup)));
						}
					}
				}
			}
		}

		protected override string OnGetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			var repository = GetRepository (repositoryPath);
			ObjectId id = repository.resolve (revision.ToString ());
			RevWalk rw = new RevWalk (repository);
			RevCommit c = rw.parseCommit (id);
			return c == null ? string.Empty : GetCommitTextContent (c, repositoryPath);
		}

		public override DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			try {
				var repository = GetRepository (baseLocalPath);
				if ((versionInfo.Status & VersionStatus.ScheduledAdd) != 0) {
					var ctxt = GetFileContent (versionInfo.LocalPath);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, GenerateDiff (EmptyContent, ctxt));
				} else if ((versionInfo.Status & VersionStatus.ScheduledDelete) != 0) {
					var ctxt = GetCommitContent (GetHeadCommit (repository), versionInfo.LocalPath);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, GenerateDiff (ctxt, EmptyContent));
				} else if ((versionInfo.Status & VersionStatus.Modified) != 0 || (versionInfo.Status & VersionStatus.Conflicted) != 0) {
					var ctxt1 = GetCommitContent (GetHeadCommit (repository), versionInfo.LocalPath);
					var ctxt2 = GetFileContent (versionInfo.LocalPath);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, GenerateDiff (ctxt1, ctxt2));
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not get diff for file '" + versionInfo.LocalPath + "'", ex);
			}
			return null;	
		}
		
		public override DiffInfo[] PathDiff (FilePath baseLocalPath, FilePath[] localPaths, bool remoteDiff)
		{
			List<DiffInfo> diffs = new List<DiffInfo> ();
			VersionInfo[] vinfos = GetDirectoryVersionInfo (baseLocalPath, localPaths, false, true);
			foreach (VersionInfo vi in vinfos) {
				var diff = GenerateDiff (baseLocalPath, vi);
				if (diff != null)
					diffs.Add (diff);
			}
			return diffs.ToArray ();
		}
		
		static byte[] GetFileContent (string file)
		{
			return File.ReadAllBytes (file);
		}

		byte[] GetCommitContent (RevCommit c, FilePath file)
		{
			var repository = GetRepository (file);
			TreeWalk tw = TreeWalk.forPath (repository, repository.ToGitPath (file), c.getTree ());
			if (tw == null)
				return EmptyContent;
			ObjectId id = tw.getObjectId (0);
			return repository.getObjectDatabase ().open (id).getBytes ();
		}

		string GetCommitTextContent (RevCommit c, FilePath file)
		{
			var content = GetCommitContent (c, file);
			if (RawText.isBinary (content))
				return null;
			return Mono.TextEditor.Utils.TextFileUtility.GetText (content);
		}
		
		static string GenerateDiff (byte[] data1, byte[] data2)
		{
			if (RawText.isBinary (data1) || RawText.isBinary (data2)) {
				if (data1.Length != data2.Length)
					return GettextCatalog.GetString (" Binary files differ");
				if (data1.Length == data2.Length) {
					for (int n=0; n<data1.Length; n++) {
						if (data1[n] != data2[n])
							return GettextCatalog.GetString (" Binary files differ");
					}
				}
				return string.Empty;
			}
			var text1 = new RawText (data1);
			var text2 = new RawText (data2);

			var edits = DiffAlgorithm.getAlgorithm (DiffAlgorithm.SupportedAlgorithm.MYERS)
					.diff (RawTextComparator.DEFAULT, text1, text2);
			var s = new java.io.ByteArrayOutputStream ();
			var formatter = new DiffFormatter (s);
			formatter.format (edits, text1, text2);
			return Encoding.UTF8.GetString (s.toByteArray ());
		}

		DiffInfo[] GetUnifiedDiffInfo (string diffContent, FilePath basePath, FilePath[] localPaths)
		{
			basePath = basePath.FullPath;
			List<DiffInfo> list = new List<DiffInfo> ();
			using (StringReader sr = new StringReader (diffContent)) {
				string line;
				StringBuilder content = new StringBuilder ();
				string fileName = null;
				
				while ((line = sr.ReadLine ()) != null) {
					if (line.StartsWith ("+++ ", StringComparison.Ordinal) || line.StartsWith ("--- ", StringComparison.Ordinal)) {
						string newFile = RootPath.Combine (line.Substring (6));
						if (fileName != null && fileName != newFile) {
							list.Add (new DiffInfo (basePath, fileName, content.ToString ().Trim ('\n')));
							content = new StringBuilder ();
						}
						fileName = newFile;
					} else if (!line.StartsWith ("diff", StringComparison.Ordinal) && !line.StartsWith ("index", StringComparison.Ordinal)) {
						content.Append (line).Append ('\n');
					}
				}
				if (fileName != null) {
					list.Add (new DiffInfo (basePath, fileName, content.ToString ().Trim ('\n')));
				}
			}
			return list.ToArray ();
		}

		public string GetCurrentRemote ()
		{
			List<string> remotes = new List<string> (GetRemotes ().Select (r => r.Name));
			if (remotes.Count == 0)
				return null;
			
			if (remotes.Contains ("origin"))
				return "origin";

			return remotes[0];
		}

		public void Push (IProgressMonitor monitor, string remote, string remoteBranch)
		{
			string remoteRef = "refs/heads/" + remoteBranch;
			java.lang.Iterable res;

			var push = new org.eclipse.jgit.api.Git (RootRepository).push ();

			// We only have one pushed branch.
			push.setRemote (remote).setRefSpecs (new RefSpec (remoteRef));
			using (var gm = new GitMonitor (monitor)) {
				push.setProgressMonitor (gm);

				try {
					res = push.call ();
				} catch (Exception e) {
					e = ikvm.runtime.Util.mapException (e);
					if (e is org.eclipse.jgit.api.errors.JGitInternalException) {
						if (e.InnerException is org.eclipse.jgit.errors.TransportException) {
							throw new VersionControlException (e.InnerException.Message);
						}
					}
					throw;
				}
			}

			var iterator = res.iterator ();
			while (iterator.hasNext ()) {
				var pr = (PushResult)iterator.next ();
				var remoteUpdate = pr.getRemoteUpdate (remoteRef);

				var status = remoteUpdate.getStatus ();
				if (status == RemoteRefUpdate.Status.UP_TO_DATE)
					monitor.ReportSuccess (GettextCatalog.GetString ("Remote branch is up to date."));
				else if (status == RemoteRefUpdate.Status.REJECTED_NODELETE)
					monitor.ReportError (GettextCatalog.GetString ("The server is configured to deny deletion of the branch"), null);
				else if (status == RemoteRefUpdate.Status.REJECTED_NONFASTFORWARD)
					monitor.ReportError (GettextCatalog.GetString ("The update is a non-fast-forward update. Merge the remote changes before pushing again."), null);
				else if (status == RemoteRefUpdate.Status.OK) {
					monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
					// Update the remote branch
					ObjectId headId = remoteUpdate.getNewObjectId ();
					RefUpdate updateRef = RootRepository.updateRef (Constants.R_REMOTES + remote + "/" + remoteBranch);
					updateRef.setNewObjectId(headId);
					updateRef.update();
				} else {
					string msg = remoteUpdate.getMessage ();
					msg = !string.IsNullOrEmpty (msg) ? msg : GettextCatalog.GetString ("Push operation failed");
					monitor.ReportError (msg, null);
				}
			}
		}

		public void CreateBranchFromCommit (string name, RevCommit id)
		{
			var create = new org.eclipse.jgit.api.Git (RootRepository).branchCreate ();
			if (id != null)
				create.setStartPoint (id);
			create.setName (name);
			create.call ();
		}

		public void CreateBranch (string name, string trackSource)
		{
			var create = new org.eclipse.jgit.api.Git (RootRepository).branchCreate ();
			if (!String.IsNullOrEmpty (trackSource))
				create.setStartPoint (trackSource);
			create.setName (name);
			create.call ();
		}

		public void SetBranchTrackSource (string name, string trackSource)
		{
			GitUtil.SetUpstreamSource (RootRepository, name, trackSource);
		}

		public void RemoveBranch (string name)
		{
			var git = new org.eclipse.jgit.api.Git (RootRepository);
			git.branchDelete ().setBranchNames (name).setForce (true).call ();
		}

		public void RenameBranch (string name, string newName)
		{
			var git = new org.eclipse.jgit.api.Git (RootRepository);
			git.branchRename ().setOldName (name).setNewName (newName).call ();
		}

		public IEnumerable<RemoteSource> GetRemotes ()
		{
			StoredConfig cfg = RootRepository.getConfig ();
			var iterator = RemoteConfig.getAllRemoteConfigs (cfg).listIterator ();
			while (iterator.hasNext ())
				yield return new RemoteSource (cfg, (RemoteConfig)iterator.next ());
		}
		
		public bool IsBranchMerged (string branchName)
		{
			// check if a branch is merged into HEAD
			RevWalk walk = new RevWalk(RootRepository);
			RevCommit tip = walk.parseCommit(RootRepository.resolve(Constants.HEAD));
			Ref currentRef = RootRepository.getRef(branchName);
			if (currentRef == null)
				return true;
			RevCommit @base = walk.parseCommit(RootRepository.resolve(branchName));
			return walk.isMergedInto(@base, tip);
		}

		public void RenameRemote (string name, string newName)
		{
			StoredConfig cfg = RootRepository.getConfig ();
			RemoteConfig.getAllRemoteConfigs (cfg);
			var iterator = RemoteConfig.getAllRemoteConfigs (cfg).listIterator ();
			while (iterator.hasNext ()) {
				var rem = (RemoteConfig)iterator.next ();
				if (rem.getName () == name) {
					rem.setName (newName);
					rem.update (cfg);
					break;
				}
			}
		}

		public void AddRemote (RemoteSource remote, bool importTags)
		{
			if (string.IsNullOrEmpty (remote.Name))
				throw new InvalidOperationException ("Name not set");
			
			StoredConfig c = RootRepository.getConfig ();
			RemoteConfig rc = new RemoteConfig (c, remote.Name);
			c.save ();
			remote.RepoRemote = rc;
			remote.cfg = c;
			UpdateRemote (remote);
		}

		public void UpdateRemote (RemoteSource remote)
		{
			if (string.IsNullOrEmpty (remote.FetchUrl))
				throw new InvalidOperationException ("Fetch url can't be empty");
			
			if (remote.RepoRemote == null)
				throw new InvalidOperationException ("Remote not created");
			
			remote.Update ();
			remote.cfg.save ();
		}

		public void RemoveRemote (string name)
		{
			StoredConfig cfg = RootRepository.getConfig ();
			var iterator = RemoteConfig.getAllRemoteConfigs (cfg).listIterator ();
			while (iterator.hasNext ()) {
				var rem = (RemoteConfig)iterator.next ();
				if (rem.getName () == name) {
					cfg.unsetSection ("remote", name);
					cfg.save ();
					break;
				}
			}
		}

		public IEnumerable<Branch> GetBranches ()
		{
			var list = new org.eclipse.jgit.api.Git (RootRepository).branchList ().setListMode (null);
			var iterator = list.call ().listIterator ();
			while (iterator.hasNext ()) {
				var item = (Ref)iterator.next ();
				string name = JRepository.shortenRefName (item.getName ());
				Branch br = new Branch ();
				br.Name = name;
				br.Tracking = GitUtil.GetUpstreamSource (RootRepository, name);
				yield return br;
			}
		}

		public IEnumerable<string> GetTags ()
		{
			var list = new org.eclipse.jgit.api.Git (RootRepository).tagList ();
			var iterator = list.call ().listIterator ();
			while (iterator.hasNext ()) {
				var item = (Ref)iterator.next ();
				string name = JRepository.shortenRefName (item.getName ());
				yield return name;
			}
		}

		public void AddTag (string name, Revision rev, string message)
		{
			var addTag = new org.eclipse.jgit.api.Git (RootRepository).tag ();
			var gitRev = (GitRevision)rev;

			addTag.setName (name).setMessage (message).setObjectId (gitRev.Commit);
			addTag.setObjectId (gitRev.Commit).setTagger (new PersonIdent (RootRepository));
			addTag.call ();
		}

		public void RemoveTag (string name)
		{
			var deleteTag = new org.eclipse.jgit.api.Git (RootRepository).tagDelete ();
			deleteTag.setTags (name).call ();
		}

		public void PushAllTags ()
		{
			var pushTags = new org.eclipse.jgit.api.Git (RootRepository).push ();
			pushTags.setPushTags ().call ();
		}

		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			var list = new org.eclipse.jgit.api.Git (RootRepository).branchList ().setListMode (ListBranchCommand.ListMode.REMOTE);
			var iterator = list.call ().listIterator ();
			while (iterator.hasNext ()) {
				var item = (Ref)iterator.next ();
				string name = JRepository.shortenRefName (item.getName ());
				if (name.StartsWith (remoteName + "/", StringComparison.Ordinal))
					yield return name.Substring (remoteName.Length + 1);
			}
		}

		public string GetCurrentBranch ()
		{
			return RootRepository.getBranch ();
		}

		public void SwitchToBranch (IProgressMonitor monitor, string branch)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), GitService.StashUnstashWhenSwitchingBranches ? 4 : 2);
			
			// Get a list of files that are different in the target branch
			java.util.List statusList = GitUtil.GetChangedFiles (RootRepository, branch);
			StashCollection stashes = null;
			Stash stash = null;
			
			if (GitService.StashUnstashWhenSwitchingBranches) {
				stashes = GitUtil.GetStashes (RootRepository);
				
				// Remove the stash for this branch, if exists
				string currentBranch = GetCurrentBranch ();
				stash = GetStashForBranch (stashes, currentBranch);
				if (stash != null)
					stashes.Remove (stash);
				
				// Create a new stash for the branch. This allows switching branches
				// without losing local changes
				using (var gm = new GitMonitor (monitor))
					stash = stashes.Create (gm, GetStashName (currentBranch));
			
				monitor.Step (1);
			}

			// Replace with org.eclipse.jgit.api.Git ().Checkout ()
			// Switch to the target branch
			var checkout = new org.eclipse.jgit.api.Git (RootRepository).checkout ();
			checkout.setName (branch);
			try {
				checkout.call ();
			} finally {
				// Restore the branch stash
				if (GitService.StashUnstashWhenSwitchingBranches) {
					stash = GetStashForBranch (stashes, branch);
					if (stash != null) {
						using (var gm = new GitMonitor (monitor))
							stash.Apply (gm);
						stashes.Remove (stash);
					}
					monitor.Step (1);
				}
			}

			// Notify file changes
			var list = new List<DiffEntry> (statusList.size ());
			var iterator = statusList.listIterator ();
			while (iterator.hasNext ())
				list.Add ((DiffEntry)iterator.next ());
			NotifyFileChanges (monitor, list);
			
			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);
			
			monitor.EndTask ();
		}

		void NotifyFileChanges (IProgressMonitor monitor, IEnumerable<DiffEntry> statusList)
		{
			List<DiffEntry> changes = new List<DiffEntry> (statusList);

			// Files added to source branch not present to target branch.
			var removed = changes.Where (c => c.getChangeType () == DiffEntry.ChangeType.ADD).Select (c => GetRepository (c.getNewPath ()).FromGitPath (c.getNewPath ())).ToList ();
			var modified = changes.Where (c => c.getChangeType () != DiffEntry.ChangeType.ADD).Select (c => GetRepository (c.getNewPath ()).FromGitPath (c.getNewPath ())).ToList ();
			
			monitor.BeginTask (GettextCatalog.GetString ("Updating solution"), removed.Count + modified.Count);

			FileService.NotifyFilesChanged (modified, true);
			monitor.Step (modified.Count);
			
			FileService.NotifyFilesRemoved (removed);
			monitor.Step (removed.Count);
			
			monitor.EndTask ();
		}

		static string GetStashName (string branchName)
		{
			return "__MD_" + branchName;
		}
		
		public static string GetStashBranchName (string stashName)
		{
			if (stashName.StartsWith ("__MD_", StringComparison.Ordinal))
				return stashName.Substring (5);

			return null;
		}

		static Stash GetStashForBranch (StashCollection stashes, string branchName)
		{
			string sn = GetStashName (branchName);
			foreach (Stash ss in stashes) {
				if (ss.Comment.IndexOf (sn, StringComparison.InvariantCulture) != -1)
					return ss;
			}
			return null;
		}

		public ChangeSet GetPushChangeSet (string remote, string branch)
		{
			ChangeSet cset = CreateChangeSet (RootPath);
			ObjectId cid1 = RootRepository.resolve (remote + "/" + branch);
			ObjectId cid2 = RootRepository.resolve (RootRepository.getBranch ());
			RevWalk	rw = new RevWalk (RootRepository);
			RevCommit c1 = rw.parseCommit (cid1);
			RevCommit c2 = rw.parseCommit (cid2);

			var li = GitUtil.CompareCommits (RootRepository, c2, c1).listIterator ();
			while (li.hasNext ()) {
				var change = (DiffEntry)li.next ();
				VersionStatus status;
				var type = change.getChangeType ();
				if (type == DiffEntry.ChangeType.ADD)
					status = VersionStatus.ScheduledAdd;
				else if (type == DiffEntry.ChangeType.DELETE)
					status = VersionStatus.ScheduledDelete;
				else
					status = VersionStatus.Modified;
				VersionInfo vi = new VersionInfo (RootRepository.FromGitPath (change.getNewPath ()), "", false, status | VersionStatus.Versioned, null, VersionStatus.Versioned, null);
				cset.AddFile (vi);
			}
			return cset;
		}

		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			ObjectId cid1 = RootRepository.resolve (remote + "/" + branch);
			ObjectId cid2 = RootRepository.resolve (RootRepository.getBranch ());
			RevWalk rw = new RevWalk (RootRepository);
			RevCommit c1 = rw.parseCommit (cid1);
			RevCommit c2 = rw.parseCommit (cid2);
			
			List<DiffInfo> diffs = new List<DiffInfo> ();
			var li = GitUtil.CompareCommits (RootRepository, c2, c1).listIterator ();
			while (li.hasNext ()) {
				var change = (DiffEntry)li.next ();
				string diff;
				var type = change.getChangeType ();

				if (type == DiffEntry.ChangeType.DELETE)
					diff = GenerateDiff (EmptyContent, GetCommitContent (c2, change.getOldPath ()));
				else if (type == DiffEntry.ChangeType.ADD)
					diff = GenerateDiff (GetCommitContent (c1, change.getNewPath ()), EmptyContent);
				else
					diff = GenerateDiff (GetCommitContent (c1, change.getNewPath ()), GetCommitContent (c2, change.getNewPath ()));
				DiffInfo di = new DiffInfo (RootPath, RootRepository.FromGitPath (change.getNewPath ()), diff);
				diffs.Add (di);
			}
			return diffs.ToArray ();
		}

		protected override void OnMoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			VersionInfo vi = GetVersionInfo (localSrcPath, VersionInfoQueryFlags.IgnoreCache);
			if (vi == null || !vi.IsVersioned) {
				base.OnMoveFile (localSrcPath, localDestPath, force, monitor);
				return;
			}
			base.OnMoveFile (localSrcPath, localDestPath, force, monitor);
			Add (localDestPath, false, monitor);
			
			if ((vi.Status & VersionStatus.ScheduledAdd) != 0)
				Revert (localSrcPath, false, monitor);
		}

		protected override void OnMoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			VersionInfo[] versionedFiles = GetDirectoryVersionInfo (localSrcPath, false, true);
			base.OnMoveDirectory (localSrcPath, localDestPath, force, monitor);
			monitor.BeginTask ("Moving files", versionedFiles.Length);
			foreach (VersionInfo vif in versionedFiles) {
				if (vif.IsDirectory)
					continue;
				FilePath newDestPath = vif.LocalPath.ToRelative (localSrcPath).ToAbsolute (localDestPath);
				Add (newDestPath, false, monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}

		public override Annotation[] GetAnnotations (FilePath repositoryPath)
		{
			var repository = GetRepository (repositoryPath);
			RevCommit hc = GetHeadCommit (repository);
			if (hc == null)
				return new Annotation [0];

			var git = new org.eclipse.jgit.api.Git (repository);
			var result = git.blame ().setFollowFileRenames (true).setFilePath (repository.ToGitPath (repositoryPath)).call ();
			result.computeAll ();

			List<Annotation> list = new List<Annotation> ();
			for (int i = 0; i < result.getResultContents ().size (); i++) {
				var commit = result.getSourceCommit (i);
				var author = result.getSourceAuthor (i);
				if (commit != null && author != null) {
					var commitTime = new DateTime (1970, 1, 1).AddSeconds (commit.getCommitTime ());
					string authorName = author.getName () ?? commit.getAuthorIdent ().getName ();
					string email = author.getEmailAddress () ?? commit.getAuthorIdent ().getEmailAddress ();
					list.Add (new Annotation (commit.getName (), authorName, commitTime, String.Format ("<{0}>", email)));
				} else {
					list.Add (new Annotation (GettextCatalog.GetString ("working copy"), "<uncommitted>", DateTime.Now));
				}
			}
			return list.ToArray ();
		}
		
		internal GitRevision GetPreviousRevisionFor (GitRevision revision)
		{
			ObjectId id = revision.GitRepository.resolve (revision + "^");
			if (id == null)
				return null;
			return new GitRevision (this, revision.GitRepository, id.getName ());
		}

		protected override void OnIgnore (FilePath[] localPath)
		{
			List<FilePath> ignored = new List<FilePath> ();
			string gitignore = RootPath + Path.DirectorySeparatorChar + ".gitignore";
			string txt;
			if (File.Exists (gitignore)) {
				using (StreamReader br = new StreamReader (gitignore)) {
					while ((txt = br.ReadLine ()) != null) {
						ignored.Add (txt);
					}
				}
			}

			StringBuilder sb = new StringBuilder ();
			foreach (var path in localPath.Except (ignored))
				sb.AppendLine (RootRepository.ToGitPath (path));

			File.AppendAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", sb.ToString ());
		}

		protected override void OnUnignore (FilePath[] localPath)
		{
			List<string> ignored = new List<string> ();
			string gitignore = RootPath + Path.DirectorySeparatorChar + ".gitignore";
			string txt;
			if (File.Exists (gitignore)) {
				using (StreamReader br = new StreamReader (RootPath + Path.DirectorySeparatorChar + ".gitignore")) {
					while ((txt = br.ReadLine ()) != null) {
						ignored.Add (txt);
					}
				}
			}

			StringBuilder sb = new StringBuilder ();
			foreach (var path in ignored.Except (RootRepository.ToGitPath (localPath)))
				sb.AppendLine (path);

			File.WriteAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", sb.ToString ());
		}
	}
	
	public class GitRevision: Revision
	{
		readonly string rev;
		
		internal RevCommit Commit { get; set; }
		internal FilePath FileForChanges { get; set; }

		public JRepository GitRepository {
			get; private set;
		}

		public GitRevision (Repository repo, JRepository gitRepository, string rev) : base(repo)
		{
			this.rev = rev;
			GitRepository = gitRepository;
		}

		public GitRevision (Repository repo, JRepository gitRepository, string rev, DateTime time, string author, string message) : base(repo, time, author, message)
		{
			this.rev = rev;
			GitRepository = gitRepository;
		}

		public override string ToString ()
		{
			return rev;
		}
		
		public override string ShortName {
			get {
				return rev.Length > 10 ? rev.Substring (0, 10) : rev;
			}
		}

		public override Revision GetPrevious ()
		{
			return ((GitRepository) this.Repository).GetPreviousRevisionFor (this);
		}
	}

	public class Branch
	{
		public string Name { get; internal set; }
		public string Tracking { get; internal set; }
	}

	public class RemoteSource
	{
		internal RemoteConfig RepoRemote;
		internal StoredConfig cfg;

		public RemoteSource ()
		{
		}

		internal RemoteSource (StoredConfig cfg, RemoteConfig rem)
		{
			this.cfg = cfg;
			RepoRemote = rem;
			Name = rem.getName();

			FetchUrl = rem.getURIs ().isEmpty () ? null : ((URIish)rem.getURIs ().get (0)).toString ();
			PushUrl = rem.getPushURIs().isEmpty () ? null : ((URIish)rem.getURIs ().get (0)).toString ();
			if (string.IsNullOrEmpty (PushUrl))
				PushUrl = FetchUrl;
		}

		internal void Update ()
		{
			RepoRemote.setName (Name);

			var list = new List<URIish> (RepoRemote.getURIs ().size ());
			var li = RepoRemote.getURIs ().listIterator ();
			while (li.hasNext ())
				list.Add ((URIish)li.next ());
			list.ForEach (u => RepoRemote.removeURI (u));

			list = new List<URIish> (RepoRemote.getPushURIs ().size ());
			li = RepoRemote.getPushURIs ().listIterator ();
			while (li.hasNext ())
				list.Add ((URIish)li.next ());
			list.ForEach (u => RepoRemote.removePushURI (u));
			
			RepoRemote.addURI (new URIish (FetchUrl));
			if (!string.IsNullOrEmpty (PushUrl) && PushUrl != FetchUrl)
				RepoRemote.addPushURI (new URIish (PushUrl));
			RepoRemote.update (cfg);
		}

		public string Name { get; internal set; }
		public string FetchUrl { get; internal set; }
		public string PushUrl { get; internal set; }
	}
	
	class GitMonitor: ProgressMonitor, IDisposable
	{
		readonly IProgressMonitor monitor;
		int currentWork;
		int currentStep;
		bool taskStarted;
		int totalTasksOverride = -1;
		bool monitorStarted;
		
		public GitMonitor (IProgressMonitor monitor)
		{
			this.monitor = monitor;
		}
		
		public GitMonitor (IProgressMonitor monitor, int totalTasksOverride)
		{
			this.monitor = monitor;
			this.totalTasksOverride = totalTasksOverride;
		}
		
		public void start (int totalTasks)
		{
			monitorStarted = true;
			currentStep = 0;
			currentWork = totalTasksOverride != -1 ? totalTasksOverride : (totalTasks > 0 ? totalTasks : 1);
			totalTasksOverride = -1;
			monitor.BeginTask (null, currentWork);
		}


		public void beginTask (string title, int totalWork)
		{
			if (taskStarted)
				endTask ();
			
			taskStarted = true;
			currentStep = 0;
			currentWork = totalWork > 0 ? totalWork : 1;
			monitor.BeginTask (title, currentWork);
		}
		
		
		public void update (int completed)
		{
			currentStep += completed;
			if (currentStep >= (currentWork / 100)) {
				monitor.Step (currentStep);
				currentStep = 0;
			}
		}
		
		
		public void endTask ()
		{
			taskStarted = false;
			monitor.EndTask ();
			monitor.Step (1);
		}
		
		
		public bool isCancelled ()
		{
			return monitor.IsCancelRequested;
		}
		
		public void Dispose ()
		{
			if (monitorStarted)
				monitor.EndTask ();
		}
	}
	
	class LocalGitRepository: FileRepository
	{
		WeakReference dirCacheRef;
		DateTime dirCacheTimestamp;
		
		public LocalGitRepository (string path): base (path)
		{
		}
		
		public override DirCache readDirCache ()
		{
			DirCache dc = null;
			if (dirCacheRef != null)
				dc = dirCacheRef.Target as DirCache;
			if (dc != null) {
				DateTime wt = File.GetLastWriteTime (getIndexFile ().toString ());
				if (wt == dirCacheTimestamp)
					return dc;
			}
			dirCacheTimestamp = File.GetLastWriteTime (getIndexFile ().toString ());
			dc = base.readDirCache ();
			dirCacheRef = new WeakReference (dc);
			return dc;
		}
	}
}

