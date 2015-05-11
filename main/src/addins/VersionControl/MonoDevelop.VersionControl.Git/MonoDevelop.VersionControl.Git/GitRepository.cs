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
using NGit;
using NGit.Storage.File;
using NGit.Revwalk;
using NGit.Api;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Dircache;
using NGit.Transport;
using NGit.Diff;
using NGit.Merge;
using NGit.Submodule;

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

		public GitRepository (VersionControlSystem vcs, FilePath path, string url) : base (vcs)
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
				if (!string.IsNullOrEmpty (u.GetHost ()))
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
				
		static RevCommit GetHeadCommit (NGit.Repository repository)
		{
			RevWalk rw = new RevWalk (repository);
			ObjectId headId = repository.Resolve (Constants.HEAD);
			if (headId == null)
				return null;
			return rw.ParseCommit (headId);
		}

		public StashCollection GetStashes ()
		{
			return GetStashes (RootRepository);
		}

		public static StashCollection GetStashes (NGit.Repository repository)
		{
			return new StashCollection (repository);
		}

		IEnumerable<string> GetSubmodulePaths ()
		{
			if (!File.Exists (RootPath.Combine (Constants.DOT_GIT_MODULES)))
				return new List<string> ();

			var lines = File.ReadAllLines (RootPath.Combine (Constants.DOT_GIT_MODULES));
			// Parses .gitmodules to get all submodules paths.
			var res = lines.Where (l => l.Contains ("path = ")).Select (l => l.Substring (l.LastIndexOf (" ", StringComparison.Ordinal) + 1));
			return res;
		}

		DateTime cachedSubmoduleTime = DateTime.MinValue;
		NGit.Repository[] cachedSubmodules = new NGit.Repository[0];
		NGit.Repository[] CachedSubmodules {
			get {
				var submoduleWriteTime = File.GetLastWriteTimeUtc(RootPath.Combine(Constants.DOT_GIT_MODULES));
				if (cachedSubmoduleTime != submoduleWriteTime) {
					cachedSubmoduleTime = submoduleWriteTime;
					var submoduleStatus = new NGit.Api.Git (RootRepository).SubmoduleStatus ();
					foreach (var submodule in GetSubmodulePaths ())
						submoduleStatus.AddPath (submodule);

					cachedSubmodules = submoduleStatus.Call ()
						.Select(s => SubmoduleWalk.GetSubmoduleRepository (RootRepository, s.Key))
						.Where(s => s != null)	// TODO: Make this so we can tell user to clone them.
						.ToArray ();
				}
				return cachedSubmodules;
			}
		}

		NGit.Repository GetRepository (FilePath localPath)
		{
			return GroupByRepository (new [] { localPath }).First ().Key;
		}

		IEnumerable<IGrouping<NGit.Repository, FilePath>> GroupByRepository (IEnumerable<FilePath> files)
		{
			var cache = CachedSubmodules;
			return files.GroupBy (f => cache.FirstOrDefault (s => {
				var fullPath = s.WorkTree.ToString ();
				return f.IsChildPathOf (fullPath) || f.FullPath == fullPath;
			}) ?? RootRepository);
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
				walk.SetTreeFilter (FollowFilter.Create (path));
			walk.MarkStart (hc);
			
			foreach (RevCommit commit in walk) {
				PersonIdent author = commit.GetAuthorIdent ();
				GitRevision rev = new GitRevision (this, repository, commit.Id.Name, author.GetWhen().ToLocalTime (), author.GetName (), commit.GetFullMessage ());
				rev.Email = author.GetEmailAddress ();
				rev.ShortMessage = commit.GetShortMessage ();
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
			foreach (var entry in GitUtil.GetCommitChanges (rev.GitRepository, rev.Commit)) {
				if (entry.GetChangeType () == DiffEntry.ChangeType.ADD)
					paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.GetNewPath ()), RevisionAction.Add, null));
				if (entry.GetChangeType () == DiffEntry.ChangeType.DELETE)
					paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.GetOldPath ()), RevisionAction.Delete, null));
				if (entry.GetChangeType () == DiffEntry.ChangeType.MODIFY)
					paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.GetNewPath ()), RevisionAction.Modify, null));
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
		Dictionary<NGit.Repository, GitRevision> versionInfoCacheRevision = new Dictionary<NGit.Repository, GitRevision> ();
		Dictionary<NGit.Repository, GitRevision> versionInfoCacheEmptyRevision = new Dictionary<NGit.Repository, GitRevision> ();
		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, IEnumerable<FilePath> localFileNames, bool getRemoteStatus, bool recursive)
		{
			List<VersionInfo> versions = new List<VersionInfo> ();
			HashSet<FilePath> existingFiles = new HashSet<FilePath> ();
			HashSet<FilePath> nonVersionedMissingFiles = new HashSet<FilePath> ();
			
			if (localFileNames != null) {
				var localFiles = new List<FilePath> ();
				foreach (var group in GroupByRepository (localFileNames)) {
					var repository = group.Key;
					GitRevision arev;
					if (!versionInfoCacheEmptyRevision.TryGetValue (repository, out arev)) {
						arev = new GitRevision (this, repository, "");
						versionInfoCacheEmptyRevision.Add (repository, arev);
					}
					foreach (var p in group) {
						if (Directory.Exists (p)) {
							if (recursive)
								versions.AddRange (GetDirectoryVersionInfo (p, getRemoteStatus, true));
							versions.Add (new VersionInfo (p, "", true, VersionStatus.Unmodified, arev, VersionStatus.Unmodified, null));
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
					GitRevision arev;
					if (!versionInfoCacheEmptyRevision.TryGetValue (repository, out arev)) {
						arev = new GitRevision (this, repository, "");
						versionInfoCacheEmptyRevision.Add (repository, arev);
					}
					foreach (var p in group)
						versions.Add (new VersionInfo (p, "", true, VersionStatus.Unmodified, arev, VersionStatus.Unmodified, null));
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
				RevCommit headCommit = GetHeadCommit (repository);
				if (headCommit != null) {
					if (!versionInfoCacheRevision.TryGetValue (repository, out rev)) {
						rev = new GitRevision (this, repository, headCommit.Id.Name);
						versionInfoCacheRevision.Add (repository, rev);
					} else if (rev.ToString () != headCommit.Id.Name) {
						rev = new GitRevision (this, repository, headCommit.Id.Name);
						versionInfoCacheRevision [repository] = rev;
					}
				}

				GetDirectoryVersionInfoCore (repository, rev, group, existingFiles, nonVersionedMissingFiles, versions);
				
				// Existing files for which git did not report a status are supposed to be tracked
				foreach (FilePath file in existingFiles.Where (f => group.Contains (f))) {
					VersionInfo vi = new VersionInfo (file, "", false, VersionStatus.Unmodified, rev, VersionStatus.Unmodified, null);
					versions.Add (vi);
				}
			}


			// Non existing files for which git did not report an status are unversioned
			foreach (FilePath file in nonVersionedMissingFiles)
				versions.Add (VersionInfo.CreateUnversioned (file, false));

			return versions.ToArray ();
		}

		static void GetDirectoryVersionInfoCore (NGit.Repository repository, GitRevision rev, IEnumerable<FilePath> localPaths, HashSet<FilePath> existingFiles, HashSet<FilePath> nonVersionedMissingFiles, List<VersionInfo> versions)
		{
			var filteredStatus = new FilteredStatus (repository, repository.ToGitPath (localPaths));
			var status = filteredStatus.Call ();
			HashSet<string> added = new HashSet<string> ();
			Action<IEnumerable<string>, VersionStatus> AddFiles = delegate (IEnumerable<string> files, VersionStatus fstatus) {
				foreach (string file in files) {
					if (!added.Add (file))
						continue;
					FilePath statFile = repository.FromGitPath (file);
					existingFiles.Remove (statFile.CanonicalPath);
					nonVersionedMissingFiles.Remove (statFile.CanonicalPath);
					versions.Add (new VersionInfo (statFile, "", false, fstatus, rev, fstatus == VersionStatus.Ignored ? VersionStatus.Unversioned : VersionStatus.Unmodified, null));
				}
			};

			AddFiles (status.GetAdded (), VersionStatus.ScheduledAdd);
			AddFiles (status.GetChanged (), VersionStatus.Modified);
			AddFiles (status.GetModified (), VersionStatus.Modified);
			AddFiles (status.GetRemoved (), VersionStatus.ScheduledDelete);
			AddFiles (status.GetMissing (), VersionStatus.ScheduledDelete);
			AddFiles (status.GetConflicting (), VersionStatus.Conflicted);
			AddFiles (status.GetUntracked (), VersionStatus.Unversioned);
			AddFiles (filteredStatus.GetIgnoredNotInIndex (), VersionStatus.Ignored);
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

			directories.AddRange (Directory.GetDirectories (dir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
				.Select (f => new FilePath (f)));

			files.UnionWith (Directory.GetFiles (dir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
				.Select (f => new FilePath (f).CanonicalPath));
		}

		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
		{
			// Initialize the repository
			RootRepository = GitUtil.Init (localPath, Url);
			NGit.Api.Git git = new NGit.Api.Git (RootRepository);
			try {
				var refs = git.Fetch ().Call ().GetAdvertisedRefs ();
				if (refs.Count > 0) {
					throw new UserException ("The remote repository already contains branches. Publishing is only possible to an empty repository");
				}
			} catch {
				try {
					RootRepository.Close ();
				} catch {
				
				}
				if (Directory.Exists (RootRepository.Directory))
					Directory.Delete (RootRepository.Directory, true);
				RootRepository = null;
				throw;
			}

			RootPath = localPath;
			// Add the project files
			ChangeSet cs = CreateChangeSet (localPath);
			var cmd = git.Add ();
			foreach (FilePath fp in files) {
				cmd.AddFilepattern (RootRepository.ToGitPath (fp));
				cs.AddFile (fp);
			}
			cmd.Call ();
			
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
			var fetch = new NGit.Api.Git (RootRepository).Fetch ();
			using (var gm = new GitMonitor (monitor)) {
				fetch.SetRemote (remote);
				fetch.SetProgressMonitor (gm);
				fetch.Call ();
			}
			monitor.Step (1);
		}

		bool GetSubmodulesToUpdate (List<string> updateSubmodules)
		{
			List<string> dirtySubmodules = new List<string> ();

			// Iterate submodules and do status.
			// SubmoduleStatus does not report changes for dirty submodules.
			foreach (var submodule in CachedSubmodules) {
				var submoduleGit = new NGit.Api.Git (submodule);
				var statusCommand = submoduleGit.Status ();
				var status = statusCommand.Call ();

				if (status.IsClean ())
					updateSubmodules.Add (RootRepository.ToGitPath (submodule.WorkTree.GetAbsolutePath ()));
				else
					dirtySubmodules.Add (RootRepository.ToGitPath (submodule.WorkTree.GetAbsolutePath ()));
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

		public void Rebase (string upstreamRef, GitUpdateOptions options, IProgressMonitor monitor)
		{
			StashCollection stashes = GitUtil.GetStashes (RootRepository);
			Stash stash = null;
			NGit.Api.Git git = new NGit.Api.Git (RootRepository);
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
					var submoduleUpdate = git.SubmoduleUpdate ();
					foreach (var submodule in UpdateSubmodules)
						submoduleUpdate.AddPath (submodule);

					submoduleUpdate.Call ();
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

				RebaseCommand rebase = git.Rebase ();
				rebase.SetOperation (RebaseCommand.Operation.BEGIN);
				rebase.SetUpstream (upstreamRef);
				var gmonitor = new GitMonitor (monitor);
				rebase.SetProgressMonitor (gmonitor);
				
				bool aborted = false;
				
				try {
					var result = rebase.Call ();
					while (!aborted && result.GetStatus () == RebaseResult.Status.STOPPED) {
						rebase = git.Rebase ();
						rebase.SetProgressMonitor (gmonitor);
						rebase.SetOperation (RebaseCommand.Operation.CONTINUE);
						bool commitChanges = true;
						var conflicts = GitUtil.GetConflictedFiles (RootRepository);
						foreach (string conflictFile in conflicts) {
							ConflictResult res = ResolveConflict (RootRepository.FromGitPath (conflictFile));
							if (res == ConflictResult.Abort) {
								aborted = true;
								commitChanges = false;
								rebase.SetOperation (RebaseCommand.Operation.ABORT);
								break;
							} else if (res == ConflictResult.Skip) {
								rebase.SetOperation (RebaseCommand.Operation.SKIP);
								commitChanges = false;
								break;
							}
						}
						if (commitChanges) {
							NGit.Api.AddCommand cmd = git.Add ();
							foreach (string conflictFile in conflicts)
								cmd.AddFilepattern (conflictFile);
							cmd.Call ();
						}
						result = rebase.Call ();
					}

					if ((options & GitUpdateOptions.UpdateSubmodules) == GitUpdateOptions.UpdateSubmodules) {
						monitor.Log.WriteLine (GettextCatalog.GetString ("Updating repository submodules"));
						var submoduleUpdate = git.SubmoduleUpdate ();
						foreach (var submodule in UpdateSubmodules)
							submoduleUpdate.AddPath (submodule);

						submoduleUpdate.Call ();
					}
				} catch {
					if (!aborted) {
						rebase = git.Rebase ();
						rebase.SetOperation (RebaseCommand.Operation.ABORT);
						rebase.SetProgressMonitor (gmonitor);
						rebase.Call ();
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

		public void Merge (string branch, GitUpdateOptions options, IProgressMonitor monitor)
		{
			IEnumerable<DiffEntry> statusList = null;
			Stash stash = null;
			StashCollection stashes = GetStashes (RootRepository);
			NGit.Api.Git git = new NGit.Api.Git (RootRepository);

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Merging"), 5);
				List<string> UpdateSubmodules = new List<string> ();

				// TODO: Fix stash so we don't have to do update before the main repo update.
				if ((options & GitUpdateOptions.UpdateSubmodules) == GitUpdateOptions.UpdateSubmodules) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Checking repository submodules"));
					if (!GetSubmodulesToUpdate (UpdateSubmodules))
						return;

					monitor.Log.WriteLine (GettextCatalog.GetString ("Updating repository submodules"));
					var submoduleUpdate = git.SubmoduleUpdate ();
					foreach (var submodule in UpdateSubmodules)
						submoduleUpdate.AddPath (submodule);

					submoduleUpdate.Call ();
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
				
				ObjectId branchId = RootRepository.Resolve (branch);

				MergeCommandResult mergeResult = git.Merge ().SetStrategy (MergeStrategy.RESOLVE).Include (branchId).Call ();
				if (mergeResult.GetMergeStatus () == MergeStatus.CONFLICTING || mergeResult.GetMergeStatus () == MergeStatus.FAILED) {
					var conflicts = mergeResult.GetConflicts ();
					bool commit = true;
					if (conflicts != null) {
						foreach (string conflictFile in conflicts.Keys) {
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
						git.Commit ().Call ();
				}

				if ((options & GitUpdateOptions.UpdateSubmodules) == GitUpdateOptions.UpdateSubmodules) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Updating repository submodules"));
					var submoduleUpdate = git.SubmoduleUpdate ();
					foreach (var submodule in UpdateSubmodules)
						submoduleUpdate.AddPath (submodule);

					submoduleUpdate.Call ();
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
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (monitor, statusList);
		}

		static ConflictResult ResolveConflict (string file)
		{
			ConflictResult res = ConflictResult.Abort;
			DispatchService.GuiSyncDispatch (delegate {
				ConflictResolutionDialog dlg = new ConflictResolutionDialog ();
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
			
			NGit.Api.Git git = new NGit.Api.Git (RootRepository);
			NGit.Api.CommitCommand commit = git.Commit ();
			commit.SetMessage (message);

			if (changeSet.ExtendedProperties.Contains ("Git.AuthorName")) {
				commit.SetAuthor ((string)changeSet.ExtendedProperties ["Git.AuthorName"], (string)changeSet.ExtendedProperties ["Git.AuthorEmail"]);
			}
			
			foreach (string path in GetFilesInPaths (changeSet.Items.Select (i => i.LocalPath)))
				commit.SetOnly (path);
			
			commit.Call ();
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
			FileTreeIterator iter = new FileTreeIterator (dir.FullName, RootRepository.FileSystem, WorkingTreeOptions.KEY.Parse(RootRepository.GetConfig()));
			while (!iter.Eof) {
				var file = iter.GetEntryFile ();
				if (file != null && !iter.IsEntryIgnored ())
					yield return file.GetPath ();
				iter.Next (1);
			}
		}

		public bool IsUserInfoDefault ()
		{
			UserConfig config = RootRepository.GetConfig ().Get (UserConfig.KEY);
			return config.IsCommitterNameImplicit () && config.IsCommitterEmailImplicit ();
		}
		
		public void GetUserInfo (out string name, out string email)
		{
			UserConfig config = RootRepository.GetConfig ().Get (UserConfig.KEY);
			name = config.GetCommitterName ();
			email = config.GetCommitterEmail ();
		}

		public void SetUserInfo (string name, string email)
		{
			StoredConfig config = RootRepository.GetConfig ();
			config.SetString ("user", null, "name", name);
			config.SetString ("user", null, "email", email);
			config.Save ();
		}

		protected override void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			CloneCommand cmd = NGit.Api.Git.CloneRepository ();
			cmd.SetURI (Url);
			cmd.SetRemote ("origin");
			cmd.SetBranch ("refs/heads/master");
			cmd.SetDirectory ((string)targetLocalPath);
			cmd.SetCloneSubmodules (true);
			using (var gm = new GitMonitor (monitor, 4)) {
				cmd.SetProgressMonitor (gm);
				try {
					cmd.Call ();
				} catch (NGit.Api.Errors.JGitInternalException e) {
					// We cancelled and NGit throws.
					// Or URL is wrong.
					if (e.InnerException is NGit.Errors.MissingObjectException ||
						e.InnerException is NGit.Errors.TransportException ||
						e.InnerException is NGit.Errors.NotSupportedException) {
						FileService.DeleteDirectory (targetLocalPath);
						throw new GitException ("Failed to clone", e.InnerException);
					}
				}
			}
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			// Replace with NGit.Api.Git.Reset ()
			// FIXME: we lack info about what happened to files
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				var files = group.ToArray ();

			var c = GetHeadCommit (repository);
			RevTree tree = c != null ? c.Tree : null;
			
			List<FilePath> changedFiles = new List<FilePath> ();
			List<FilePath> removedFiles = new List<FilePath> ();
			
			monitor.BeginTask (GettextCatalog.GetString ("Reverting files"), 3);
			monitor.BeginStepTask (GettextCatalog.GetString ("Reverting files"), files.Length, 2);
			
			DirCache dc = repository.LockDirCache ();
			DirCacheBuilder builder = dc.Builder ();
			
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
					
					TreeWalk tw = tree != null ? TreeWalk.ForPath (repository, p, tree) : null;
					if (tw == null) {
						// Removed from the index
					}
					else {
						// Add new entries
						
						TreeWalk r;
						if (tw.IsSubtree) {
							// It's a directory. Make sure we remove existing index entries of this directory
							foldersToRemove.Add (p);
							
							// We have to iterate through all folder files. We need a new iterator since the
							// existing rw is not recursive
							r = new TreeWalk(repository);
							r.Reset (tree);
							r.Filter = PathFilterGroup.CreateFromStrings(new string[]{p});
							r.Recursive = true;
							r.Next ();
						} else {
							r = tw;
						}
						
						do {
							// There can be more than one entry if reverting a whole directory
							string rpath = repository.FromGitPath (r.PathString);
							DirCacheEntry e = new DirCacheEntry (r.PathString);
							e.SetObjectId (r.GetObjectId (0));
							e.FileMode = r.GetFileMode (0);
							if (!Directory.Exists (Path.GetDirectoryName (rpath)))
								Directory.CreateDirectory (rpath);
							DirCacheCheckout.CheckoutEntry (repository, rpath, e);
							builder.Add (e);
							changedFiles.Add (rpath);
						} while (r.Next ());
					}
					monitor.Step (1);
				}
				
				// Add entries we want to keep
				int count = dc.GetEntryCount ();
				for (int n=0; n<count; n++) {
					DirCacheEntry e = dc.GetEntry (n);
					string path = e.PathString;
					if (!entriesToRemove.Contains (path) && !foldersToRemove.Any (f => IsSubpath (f,path)))
						builder.Add (e);
				}
				
				builder.Commit ();
			}
			catch {
				dc.Unlock ();
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
			throw new NotSupportedException ();
		}

		protected override void OnRevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			NGit.Repository repo = GetRepository (localPath);
			NGit.Api.Git git = new NGit.Api.Git (repo);
			GitRevision gitRev = (GitRevision)revision;

			// Rewrite file data from selected revision.
			foreach (var path in GetFilesInPaths (new [] { localPath }))
				MonoDevelop.Projects.Text.TextFile.WriteFile (repo.FromGitPath (path), GetCommitTextContent (gitRev.Commit, path), null);

			monitor.ReportSuccess (GettextCatalog.GetString ("Successfully reverted {0} to revision {1}", localPath, gitRev));
		}


		protected override void OnAdd (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				var files = group;

				NGit.Api.Git git = new NGit.Api.Git (repository);
				var cmd = git.Add ();
				foreach (FilePath fp in files)
					cmd.AddFilepattern (repository.ToGitPath (fp));
				cmd.Call ();
			}
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

				var git = new NGit.Api.Git (repository);
				RmCommand rm = git.Rm ();
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

					rm.AddFilepattern (repository.ToGitPath (f));
				}

				try {
					rm.Call ();
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
			ObjectId id = repository.Resolve (revision.ToString ());
			RevWalk rw = new RevWalk (repository);
			RevCommit c = rw.ParseCommit (id);
			return c == null ? string.Empty : GetCommitTextContent (c, repositoryPath);
		}

		public override DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			try {
				var repository = GetRepository (baseLocalPath);
				if ((versionInfo.Status & VersionStatus.ScheduledAdd) != 0) {
					var ctxt = GetFileContent (versionInfo.LocalPath);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, GenerateDiff (EmptyContent, ctxt, repository));
				} else if ((versionInfo.Status & VersionStatus.ScheduledDelete) != 0) {
					var ctxt = GetCommitContent (GetHeadCommit (repository), versionInfo.LocalPath);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, GenerateDiff (ctxt, EmptyContent, repository));
				} else if ((versionInfo.Status & VersionStatus.Modified) != 0 || (versionInfo.Status & VersionStatus.Conflicted) != 0) {
					var ctxt1 = GetCommitContent (GetHeadCommit (repository), versionInfo.LocalPath);
					var ctxt2 = GetFileContent (versionInfo.LocalPath);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, GenerateDiff (ctxt1, ctxt2, repository));
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
			TreeWalk tw = TreeWalk.ForPath (repository, file.IsAbsolute ? repository.ToGitPath (file) : (string)file, c.Tree);
			if (tw == null)
				return EmptyContent;
			ObjectId id = tw.GetObjectId (0);
			return repository.ObjectDatabase.Open (id).GetBytes ();
		}

		string GetCommitTextContent (RevCommit c, FilePath file)
		{
			var content = GetCommitContent (c, file);
			if (RawText.IsBinary (content))
				return String.Empty;
			return Mono.TextEditor.Utils.TextFileUtility.GetText (content);
		}
		
		static string GenerateDiff (byte[] data1, byte[] data2, NGit.Repository repo)
		{
			if (RawText.IsBinary (data1) || RawText.IsBinary (data2)) {
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

			var edits = DiffAlgorithm.GetAlgorithm (DiffAlgorithm.SupportedAlgorithm.MYERS)
					.Diff (RawTextComparator.DEFAULT, text1, text2);
			MemoryStream s = new MemoryStream ();
			var formatter = new DiffFormatter (s);
			formatter.SetRepository (repo);
			formatter.Format (edits, text1, text2);
			return Encoding.UTF8.GetString (s.ToArray ());
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
			IEnumerable<PushResult> res;

			var push = new NGit.Api.Git (RootRepository).Push ();

			// We only have one pushed branch.
			push.SetRemote (remote).SetRefSpecs (new RefSpec (remoteRef));
			using (var gm = new GitMonitor (monitor)) {
				push.SetProgressMonitor (gm);

				try {
					res = push.Call ();
				} catch (NGit.Api.Errors.JGitInternalException e) {
					if (e.InnerException is NGit.Errors.TransportException)
						throw new GitException ("Failed to push", e.InnerException);
					throw;
				}
			}

			foreach (var pr in res) {
				var remoteUpdate = pr.GetRemoteUpdate (remoteRef);

				switch (remoteUpdate.GetStatus ()) {
					case RemoteRefUpdate.Status.UP_TO_DATE: monitor.ReportSuccess (GettextCatalog.GetString ("Remote branch is up to date.")); break;
					case RemoteRefUpdate.Status.REJECTED_NODELETE: monitor.ReportError (GettextCatalog.GetString ("The server is configured to deny deletion of the branch"), null); break;
					case RemoteRefUpdate.Status.REJECTED_NONFASTFORWARD: monitor.ReportError (GettextCatalog.GetString ("The update is a non-fast-forward update. Merge the remote changes before pushing again."), null); break;
					case RemoteRefUpdate.Status.OK:
						monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
						// Update the remote branch
						ObjectId headId = remoteUpdate.GetNewObjectId ();
						RefUpdate updateRef = RootRepository.UpdateRef (Constants.R_REMOTES + remote + "/" + remoteBranch);
						updateRef.SetNewObjectId(headId);
						updateRef.Update();
						break;
					default:
						string msg = remoteUpdate.GetMessage ();
						msg = !string.IsNullOrEmpty (msg) ? msg : GettextCatalog.GetString ("Push operation failed");
						monitor.ReportError (msg, null);
						break;
				}
			}
		}

		public void CreateBranchFromCommit (string name, RevCommit id)
		{
			var create = new NGit.Api.Git (RootRepository).BranchCreate ();
			if (id != null)
				create.SetStartPoint (id);
			create.SetName (name);
			create.Call ();
		}

		public void CreateBranch (string name, string trackSource)
		{
			var create = new NGit.Api.Git (RootRepository).BranchCreate ();
			if (!String.IsNullOrEmpty (trackSource))
				create.SetStartPoint (trackSource);
			create.SetName (name);
			create.Call ();
		}

		public void SetBranchTrackSource (string name, string trackSource)
		{
			GitUtil.SetUpstreamSource (RootRepository, name, trackSource);
		}

		public void RemoveBranch (string name)
		{
			var git = new NGit.Api.Git (RootRepository);
			git.BranchDelete ().SetBranchNames (name).SetForce (true).Call ();
		}

		public void RenameBranch (string name, string newName)
		{
			var git = new NGit.Api.Git (RootRepository);
			git.BranchRename ().SetOldName (name).SetNewName (newName).Call ();
		}

		public IEnumerable<RemoteSource> GetRemotes ()
		{
			StoredConfig cfg = RootRepository.GetConfig ();
			foreach (RemoteConfig rc in RemoteConfig.GetAllRemoteConfigs (cfg))
				yield return new RemoteSource (cfg, rc);
		}
		
		public bool IsBranchMerged (string branchName)
		{
			// check if a branch is merged into HEAD
			RevWalk walk = new RevWalk(RootRepository);
			RevCommit tip = walk.ParseCommit(RootRepository.Resolve(Constants.HEAD));
			Ref currentRef = RootRepository.GetRef(branchName);
			if (currentRef == null)
				return true;
			RevCommit @base = walk.ParseCommit(RootRepository.Resolve(branchName));
			return walk.IsMergedInto(@base, tip);
		}

		public void RenameRemote (string name, string newName)
		{
			StoredConfig cfg = RootRepository.GetConfig ();
			RemoteConfig rem = RemoteConfig.GetAllRemoteConfigs (cfg).FirstOrDefault (r => r.Name == name);
			if (rem != null) {
				rem.Name = newName;
				rem.Update (cfg);
			}
		}

		public void AddRemote (RemoteSource remote, bool importTags)
		{
			if (string.IsNullOrEmpty (remote.Name))
				throw new InvalidOperationException ("Name not set");
			
			StoredConfig c = RootRepository.GetConfig ();
			RemoteConfig rc = new RemoteConfig (c, remote.Name);
			c.Save ();
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
			remote.cfg.Save ();
		}

		public void RemoveRemote (string name)
		{
			StoredConfig cfg = RootRepository.GetConfig ();
			RemoteConfig rem = RemoteConfig.GetAllRemoteConfigs (cfg).FirstOrDefault (r => r.Name == name);
			if (rem != null) {
				cfg.UnsetSection ("remote", name);
				cfg.Save ();
			}
		}

		public IEnumerable<Branch> GetBranches ()
		{
			var list = new NGit.Api.Git (RootRepository).BranchList ().SetListMode (ListBranchCommand.ListMode.HEAD);
			foreach (var item in list.Call ()) {
				string name = NGit.Repository.ShortenRefName (item.GetName ());
				Branch br = new Branch ();
				br.Name = name;
				br.Tracking = GitUtil.GetUpstreamSource (RootRepository, name);
				yield return br;
			}
		}

		public IEnumerable<string> GetTags ()
		{
			var list = new NGit.Api.Git (RootRepository).TagList ();
			foreach (var item in list.Call ()) {
				string name = NGit.Repository.ShortenRefName (item.GetName ());
				yield return name;
			}
		}

		public void AddTag (string name, Revision rev, string message)
		{
			var addTag = new NGit.Api.Git (RootRepository).Tag ();
			var gitRev = (GitRevision)rev;

			addTag.SetName (name).SetMessage (message).SetObjectId (gitRev.Commit);
			addTag.SetObjectId (gitRev.Commit).SetTagger (new PersonIdent (RootRepository));
			addTag.Call ();
		}

		public void RemoveTag (string name)
		{
			var deleteTag = new NGit.Api.Git (RootRepository).TagDelete ();
			deleteTag.SetTags (name).Call ();
		}

		public void PushAllTags ()
		{
			var pushTags = new NGit.Api.Git (RootRepository).Push ();
			pushTags.SetPushTags ().Call ();
		}

		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			var list = new NGit.Api.Git (RootRepository).BranchList ().SetListMode (ListBranchCommand.ListMode.REMOTE);
			foreach (var item in list.Call ()) {
				string name = NGit.Repository.ShortenRefName (item.GetName ());
				if (name.StartsWith (remoteName + "/", StringComparison.Ordinal))
					yield return name.Substring (remoteName.Length + 1);
			}
		}

		public string GetCurrentBranch ()
		{
			return RootRepository.GetBranch ();
		}

		public void SwitchToBranch (IProgressMonitor monitor, string branch)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), GitService.StashUnstashWhenSwitchingBranches ? 4 : 2);
			
			// Get a list of files that are different in the target branch
			IEnumerable<DiffEntry> statusList = GitUtil.GetChangedFiles (RootRepository, branch);
			
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

			// Replace with NGit.Api.Git ().Checkout ()
			// Switch to the target branch
			var checkout = new NGit.Api.Git (RootRepository).Checkout ();
			checkout.SetName (branch);
			try {
				checkout.Call ();
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
			
			NotifyFileChanges (monitor, statusList);
			
			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);
			
			monitor.EndTask ();
		}

		void NotifyFileChanges (IProgressMonitor monitor, IEnumerable<DiffEntry> statusList)
		{
			List<DiffEntry> changes = new List<DiffEntry> (statusList);

			// Files added to source branch not present to target branch.
			var removed = changes.Where (c => c.GetChangeType () == DiffEntry.ChangeType.ADD).Select (c => GetRepository (c.GetNewPath ()).FromGitPath (c.GetNewPath ())).ToList ();
			var modified = changes.Where (c => c.GetChangeType () != DiffEntry.ChangeType.ADD).Select (c => GetRepository (c.GetNewPath ()).FromGitPath (c.GetNewPath ())).ToList ();
			
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
			ObjectId cid1 = RootRepository.Resolve (remote + "/" + branch);
			ObjectId cid2 = RootRepository.Resolve (RootRepository.GetBranch ());
			RevWalk	rw = new RevWalk (RootRepository);
			RevCommit c1 = rw.ParseCommit (cid1);
			RevCommit c2 = rw.ParseCommit (cid2);
			
			foreach (var change in GitUtil.CompareCommits (RootRepository, c1, c2)) {
				VersionStatus status;
				switch (change.GetChangeType ()) {
				case DiffEntry.ChangeType.ADD:
					status = VersionStatus.ScheduledAdd;
					break;
				case DiffEntry.ChangeType.DELETE:
					status = VersionStatus.ScheduledDelete;
					break;
				default:
					status = VersionStatus.Modified;
					break;
				}
				VersionInfo vi = new VersionInfo (RootRepository.FromGitPath (change.GetNewPath ()), "", false, status, null, VersionStatus.Unmodified, null);
				cset.AddFile (vi);
			}
			return cset;
		}

		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			ObjectId cid1 = RootRepository.Resolve (remote + "/" + branch);
			ObjectId cid2 = RootRepository.Resolve (RootRepository.GetBranch ());
			RevWalk rw = new RevWalk (RootRepository);
			RevCommit c1 = rw.ParseCommit (cid1);
			RevCommit c2 = rw.ParseCommit (cid2);
			
			List<DiffInfo> diffs = new List<DiffInfo> ();
			foreach (var change in GitUtil.CompareCommits (RootRepository, c1, c2)) {
				string diff;
				switch (change.GetChangeType ()) {
				case DiffEntry.ChangeType.DELETE:
					diff = GenerateDiff (GetCommitContent (c2, change.GetOldPath ()), EmptyContent, RootRepository);
					break;
				case DiffEntry.ChangeType.ADD:
					diff = GenerateDiff (EmptyContent, GetCommitContent (c1, change.GetNewPath ()), RootRepository);
					break;
				default:
					diff = GenerateDiff (GetCommitContent (c1, change.GetNewPath ()), GetCommitContent (c2, change.GetNewPath ()), RootRepository);
					break;
				}
				DiffInfo di = new DiffInfo (RootPath, RootRepository.FromGitPath (change.GetNewPath ()), diff);
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

			var git = new NGit.Api.Git (repository);
			var result = git.Blame ().SetFollowFileRenames (true).SetFilePath (repository.ToGitPath (repositoryPath)).Call ();
			result.ComputeAll ();

			List<Annotation> list = new List<Annotation> ();
			for (int i = 0; i < result.GetResultContents ().Size (); i++) {
				var commit = result.GetSourceCommit (i);
				var author = result.GetSourceAuthor (i);
				if (commit != null && author != null) {
					var commitTime = new DateTime (1970, 1, 1).AddSeconds (commit.CommitTime);
					string authorName = author.GetName () ?? commit.GetAuthorIdent ().GetName ();
					string email = author.GetEmailAddress () ?? commit.GetAuthorIdent ().GetEmailAddress ();
					list.Add (new Annotation (commit.Name, authorName, commitTime, String.Format ("<{0}>", email)));
				} else {
					list.Add (new Annotation (GettextCatalog.GetString ("working copy"), "<uncommitted>", DateTime.Now));
				}
			}
			return list.ToArray ();
		}
		
		internal GitRevision GetPreviousRevisionFor (GitRevision revision)
		{
			ObjectId id = revision.GitRepository.Resolve (revision + "^");
			if (id == null)
				return null;
			return new GitRevision (this, revision.GitRepository, id.Name);
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

		public NGit.Repository GitRepository {
			get; private set;
		}

		public GitRevision (Repository repo, NGit.Repository gitRepository, string rev) : base(repo)
		{
			this.rev = rev;
			GitRepository = gitRepository;
		}

		public GitRevision (Repository repo, NGit.Repository gitRepository, string rev, DateTime time, string author, string message) : base(repo, time, author, message)
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
			Name = rem.Name;
			FetchUrl = rem.URIs.Select (u => u.ToString ()).FirstOrDefault ();
			PushUrl = rem.PushURIs.Select (u => u.ToString ()).FirstOrDefault ();
			if (string.IsNullOrEmpty (PushUrl))
				PushUrl = FetchUrl;
		}

		internal void Update ()
		{
			RepoRemote.Name = Name;
			
			var list = new List<URIish> (RepoRemote.URIs);
			list.ForEach (u => RepoRemote.RemoveURI (u));
			
			list = new List<URIish> (RepoRemote.PushURIs);
			list.ForEach (u => RepoRemote.RemovePushURI (u));
			
			RepoRemote.AddURI (new URIish (FetchUrl));
			if (!string.IsNullOrEmpty (PushUrl) && PushUrl != FetchUrl)
				RepoRemote.AddPushURI (new URIish (PushUrl));
			RepoRemote.Update (cfg);
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
		
		public override void Start (int totalTasks)
		{
			monitorStarted = true;
			currentStep = 0;
			currentWork = totalTasksOverride != -1 ? totalTasksOverride : (totalTasks > 0 ? totalTasks : 1);
			totalTasksOverride = -1;
			monitor.BeginTask (null, currentWork);
		}
		
		
		public override void BeginTask (string title, int totalWork)
		{
			if (taskStarted)
				EndTask ();
			
			taskStarted = true;
			currentStep = 0;
			currentWork = totalWork > 0 ? totalWork : 1;
			monitor.BeginTask (title, currentWork);
		}
		
		
		public override void Update (int completed)
		{
			currentStep += completed;
			if (currentStep >= (currentWork / 100)) {
				monitor.Step (currentStep);
				currentStep = 0;
			}
		}
		
		
		public override void EndTask ()
		{
			taskStarted = false;
			monitor.EndTask ();
			monitor.Step (1);
		}
		
		
		public override bool IsCancelled ()
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
		
		public override DirCache ReadDirCache ()
		{
			DirCache dc = null;
			if (dirCacheRef != null)
				dc = dirCacheRef.Target as DirCache;
			if (dc != null) {
				DateTime wt = File.GetLastWriteTime (GetIndexFile ());
				if (wt == dirCacheTimestamp)
					return dc;
			}
			dirCacheTimestamp = File.GetLastWriteTime (GetIndexFile ());
			dc = base.ReadDirCache ();
			dirCacheRef = new WeakReference (dc);
			return dc;
		}
	}
}

