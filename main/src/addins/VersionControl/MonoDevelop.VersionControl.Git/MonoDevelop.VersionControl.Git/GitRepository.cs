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
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using MonoDevelop.Ide;
using NGit;
using NGit.Storage.File;
using NGit.Revwalk;
using NGit.Api;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Dircache;
using NGit.Transport;
using NGit.Errors;
using NGit.Api.Errors;
using NGit.Diff;
using NGit.Merge;
using Mono.TextEditor;
using NGit.Submodule;

namespace MonoDevelop.VersionControl.Git
{
	public class GitRepository : UrlBasedRepository
	{
		static readonly byte[] EmptyContent = new byte[0];

		LocalGitRepository RootRepository {
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
				NGit.Transport.URIish u = new NGit.Transport.URIish (url);
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

		public FilePath RootPath {
			get; private set;
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
				
		RevCommit GetHeadCommit (NGit.Repository repository)
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

		public StashCollection GetStashes (NGit.Repository repository)
		{
			return new StashCollection (repository);
		}

		DateTime cachedSubmoduleTime = DateTime.MinValue;
		Tuple<FilePath, NGit.Repository>[] cachedSubmodules = new Tuple<FilePath, NGit.Repository>[0];
		Tuple<FilePath, NGit.Repository>[] CachedSubmodules {
			get {
				var submoduleWriteTime = File.GetLastWriteTimeUtc(RootPath.Combine(".gitmodule"));
				if (cachedSubmoduleTime != submoduleWriteTime) {
					cachedSubmoduleTime = submoduleWriteTime;
					cachedSubmodules = new NGit.Api.Git (RootRepository)
					.SubmoduleStatus ()
					.Call ()
					.Select(s => Tuple.Create ((FilePath) s.Key, SubmoduleWalk.GetSubmoduleRepository (RootRepository, s.Key)))
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
			return files.GroupBy (f => {
				return cache
					.Where (s => {
						var fullPath = s.Item1.ToAbsolute (RootPath);
						return f.IsChildPathOf (fullPath) || f.CanonicalPath == fullPath.CanonicalPath;
					})
					.Select (s => s.Item2)
					.FirstOrDefault () ?? RootRepository;
			});
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
							else
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
				CollectFiles (existingFiles, localDirectory, recursive);
			}

			IEnumerable<FilePath> paths;
			if (localFileNames == null) {
				if (recursive) {
					paths = new [] { localDirectory };
				} else {
					if (Directory.Exists (localDirectory))
						paths = Directory.GetFiles (localDirectory).Select (f => (FilePath)f).ToArray ();
					else
						paths = new FilePath [0];
				}
			} else {
				paths = localFileNames.Select (f => f).ToArray ();
			}

			foreach (var group in paths.GroupBy (p => GetRepository (p))) {
				var repository = group.Key;
				var files = group.ToArray ();
				
				GitRevision rev;
				var headCommit = GetHeadCommit (repository);
				if (headCommit != null)
					rev = new GitRevision (this, repository, headCommit.Id.Name);
				else
					rev = null;

				GetDirectoryVersionInfoCore (repository, rev, files.ToArray (), existingFiles, nonVersionedMissingFiles, versions);
				
				// Existing files for which git did not report an status are supposed to be tracked
				foreach (FilePath file in existingFiles.Where (f => files.Contains (f))) {
					VersionInfo vi = new VersionInfo (file, "", false, VersionStatus.Versioned, rev, VersionStatus.Versioned, null);
					versions.Add (vi);
				}
			}


			// Non existing files for which git did not report an status are unversioned
			foreach (FilePath file in nonVersionedMissingFiles)
				versions.Add (VersionInfo.CreateUnversioned (file, false));

			return versions.ToArray ();
		}

		void GetDirectoryVersionInfoCore (NGit.Repository repository, GitRevision rev, FilePath [] localPaths, HashSet<FilePath> existingFiles, HashSet<FilePath> nonVersionedMissingFiles, List<VersionInfo> versions)
		{
			
			var status = new FilteredStatus (repository, repository.ToGitPath (localPaths)).Call (); 
			HashSet<string> added = new HashSet<string> ();
			Action<IEnumerable<string>, VersionStatus> AddFiles = delegate(IEnumerable<string> files, VersionStatus fstatus) {
				foreach (string file in files) {
					if (!added.Add (file))
						continue;
					FilePath statFile = repository.FromGitPath (file);
					existingFiles.Remove (statFile.CanonicalPath);
					nonVersionedMissingFiles.Remove (statFile.CanonicalPath);
					versions.Add (new VersionInfo (statFile, "", false, fstatus, rev, VersionStatus.Versioned, null));
				}
			};
			
			AddFiles (status.GetAdded (), VersionStatus.Versioned | VersionStatus.ScheduledAdd);
			AddFiles (status.GetChanged (), VersionStatus.Versioned | VersionStatus.Modified);
			AddFiles (status.GetModified (), VersionStatus.Versioned | VersionStatus.Modified);
			AddFiles (status.GetRemoved (), VersionStatus.Versioned | VersionStatus.ScheduledDelete);
			AddFiles (status.GetMissing (), VersionStatus.Versioned | VersionStatus.ScheduledDelete);
			AddFiles (status.GetConflicting (), VersionStatus.Versioned | VersionStatus.Conflicted);
			AddFiles (status.GetUntracked (), VersionStatus.Unversioned);
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

		void CollectFiles (HashSet<FilePath> files, FilePath dir, bool recursive)
		{
			if (!Directory.Exists (dir))
				return;
			foreach (string file in Directory.GetFiles (dir))
				files.Add (new FilePath (file).CanonicalPath);
			if (recursive) {
				foreach (string sub in Directory.GetDirectories (dir))
					CollectFiles (files, sub, true);
			}
		}

		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
		{
			// Initialize the repository
			RootRepository = GitUtil.Init (localPath, Url, monitor);
			NGit.Api.Git git = new NGit.Api.Git (RootRepository);
			try {
				var refs = git.Fetch ().Call ().GetAdvertisedRefs ();
				if (refs.Count > 0) {
					throw new UserException ("The remote repository already contains branches. MonoDevelop can only publish to an empty repository");
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
			string remote = GetCurrentRemote ();
			if (remote == null)
				throw new InvalidOperationException ("No remotes defined");
			monitor.Log.WriteLine (GettextCatalog.GetString ("Fetching from '{0}'", remote));
			RemoteConfig remoteConfig = new RemoteConfig (RootRepository.GetConfig (), remote);
			Transport tn = Transport.Open (RootRepository, remoteConfig);
			using (var gm = new GitMonitor (monitor))
				tn.Fetch (gm, null);
			monitor.Step (1);
			
			string upstreamRef = GitUtil.GetUpstreamSource (RootRepository, GetCurrentBranch ());
			if (upstreamRef == null)
				upstreamRef = GetCurrentRemote () + "/" + GetCurrentBranch ();
			
			if (GitService.UseRebaseOptionWhenPulling)
				Rebase (upstreamRef, GitService.StashUnstashWhenUpdating, monitor);
			else
				Merge (upstreamRef, GitService.StashUnstashWhenUpdating, monitor);

			monitor.Step (1);
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (monitor, statusList);
			
			monitor.EndTask ();
		}

		public void Rebase (string upstreamRef, bool saveLocalChanges, IProgressMonitor monitor)
		{
			StashCollection stashes = GitUtil.GetStashes (RootRepository);
			Stash stash = null;
			
			try
			{
				if (saveLocalChanges) {
					monitor.BeginTask (GettextCatalog.GetString ("Rebasing"), 3);
					monitor.Log.WriteLine (GettextCatalog.GetString ("Saving local changes"));
					using (var gm = new GitMonitor (monitor))
						stash = stashes.Create (gm, GetStashName ("_tmp_"));
					monitor.Step (1);
				}
				
				NGit.Api.Git git = new NGit.Api.Git (RootRepository);
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
				if (saveLocalChanges)
					monitor.Step (1);
				
				// Restore local changes
				if (stash != null) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					using (var gm = new GitMonitor (monitor))
						stash.Apply (gm);
					stashes.Remove (stash);
					monitor.EndTask ();
				}
			}			
		}
		
		public void Merge (string branch, bool saveLocalChanges, IProgressMonitor monitor)
		{
			IEnumerable<DiffEntry> statusList = null;
			Stash stash = null;
			StashCollection stashes = GetStashes (RootRepository);
			monitor.BeginTask (null, 4);
			
			try {
				// Get a list of files that are different in the target branch
				statusList = GitUtil.GetChangedFiles (RootRepository, branch);
				monitor.Step (1);
				
				if (saveLocalChanges) {
					monitor.BeginTask (GettextCatalog.GetString ("Merging"), 3);
					monitor.Log.WriteLine (GettextCatalog.GetString ("Saving local changes"));
					using (var gm = new GitMonitor (monitor))
						stash = stashes.Create (gm, GetStashName ("_tmp_"));
					monitor.Step (1);
				}
				
				// Apply changes
				
				ObjectId branchId = RootRepository.Resolve (branch);
				
				NGit.Api.Git git = new NGit.Api.Git (RootRepository);
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
				
			} finally {
				if (saveLocalChanges)
					monitor.Step (1);
				
				// Restore local changes
				if (stash != null) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					using (var gm = new GitMonitor (monitor))
						stash.Apply (gm);
					stashes.Remove (stash);
					monitor.EndTask ();
				}
			}
			monitor.Step (1);
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (monitor, statusList);
			
			monitor.EndTask ();
		}

		ConflictResult ResolveConflict (string file)
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
		
		public void GetUserInfo (out string name, out string email)
		{
			UserConfig config = RootRepository.GetConfig ().Get (UserConfig.KEY);
			name = config.GetCommitterName ();
			email = config.GetCommitterEmail ();
		}

		public void SetUserInfo (string name, string email)
		{
			NGit.StoredConfig config = RootRepository.GetConfig ();
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
			using (var gm = new GitMonitor (monitor, 4)) {
				cmd.SetProgressMonitor (gm);
				cmd.Call ();
			}
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			foreach (var group in localPaths.GroupBy (f => GetRepository (f))) {
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
							r = new NGit.Treewalk.TreeWalk(repository);
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
				FileService.NotifyFileChanged (p);
				monitor.Step (1);
			}
			foreach (FilePath p in removedFiles) {
				FileService.NotifyFileRemoved (p);
				monitor.Step (1);
			}
			monitor.EndTask ();
			}
		}
		
		bool IsSubpath (string basePath, string childPath)
		{
			if (basePath [basePath.Length - 1] == '/')
				return childPath.StartsWith (basePath);
			return childPath.StartsWith (basePath + "/");
		}

		protected override void OnRevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			var git = new NGit.Api.Git (GetRepository (localPath));
			var gitRev = (GitRevision)revision;
			var revert = git.Revert ().Include (gitRev.Commit.ToObjectId ());
			var newRevision = revert.Call ();

			var revertResult = revert.GetFailingResult ();
			if (revertResult == null) {
				monitor.ReportSuccess (GettextCatalog.GetString ("Revision {0} successfully reverted", gitRev));
			} else {
				var errorMessage = GettextCatalog.GetString ("Could not revert commit {0}", gitRev);
				var description = GettextCatalog.GetString ("The following files had merge conflicts");
				description += Environment.NewLine + string.Join (Environment.NewLine, revertResult.GetFailingPaths ().Keys);
				monitor.ReportError (errorMessage, new UserException (errorMessage, description));
			} 
		}


		protected override void OnRevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			throw new System.NotImplementedException ();
		}


		protected override void OnAdd (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			foreach (var group in localPaths.GroupBy (p => GetRepository (p))) {
				var repository = group.Key;
				var files = group;

				NGit.Api.Git git = new NGit.Api.Git (repository);
				var cmd = git.Add ();
				foreach (FilePath fp in files)
					cmd.AddFilepattern (repository.ToGitPath (fp));
				cmd.Call ();
			}
		}
		
		protected override void OnDeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			DeleteCore (localPaths, force, monitor);
			// Untracked files are not deleted by the rm command, so delete them now
			foreach (var f in localPaths)
				if (File.Exists (f))
					File.Delete (f);
		}
		
		protected override void OnDeleteDirectories (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			DeleteCore (localPaths, force, monitor);

			// Untracked files are not deleted by the rm command, so delete them now
			foreach (var f in localPaths)
				if (Directory.Exists (f))
					Directory.Delete (f, true);
		}

		void DeleteCore (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			foreach (var group in localPaths.GroupBy (p => GetRepository (p))) {
				var repository = group.Key;
				var files = group;

				var git = new NGit.Api.Git (repository);
				RmCommand rm = git.Rm ();
				foreach (var f in files)
					rm.AddFilepattern (repository.ToGitPath (f));
				rm.Call ();
			}
		}

		protected override string OnGetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			var repository = GetRepository (repositoryPath);
			ObjectId id = repository.Resolve (revision.ToString ());
			RevWalk rw = new RevWalk (repository);
			RevCommit c = rw.ParseCommit (id);
			if (c == null)
				return string.Empty;
			else
				return GetCommitTextContent (c, repositoryPath);
		}

		public override DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo vi)
		{
			try {
				var repository = GetRepository (baseLocalPath);
				if ((vi.Status & VersionStatus.ScheduledAdd) != 0) {
					var ctxt = GetFileContent (vi.LocalPath);
					return new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (EmptyContent, ctxt));
				} else if ((vi.Status & VersionStatus.ScheduledDelete) != 0) {
					var ctxt = GetCommitContent (GetHeadCommit (repository), vi.LocalPath);
					return new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt, EmptyContent));
				} else if ((vi.Status & VersionStatus.Modified) != 0 || (vi.Status & VersionStatus.Conflicted) != 0) {
					var ctxt1 = GetCommitContent (GetHeadCommit (repository), vi.LocalPath);
					var ctxt2 = GetFileContent (vi.LocalPath);
					return new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt1, ctxt2));
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not get diff for file '" + vi.LocalPath + "'", ex);
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
		
		byte[] GetFileContent (string file)
		{
			return File.ReadAllBytes (file);
		}

		byte[] GetCommitContent (RevCommit c, FilePath file)
		{
			var repository = GetRepository (file);
			TreeWalk tw = TreeWalk.ForPath (repository, repository.ToGitPath (file), c.Tree);
			if (tw == null)
				return EmptyContent;
			ObjectId id = tw.GetObjectId (0);
			return repository.ObjectDatabase.Open (id).GetBytes ();
		}

		string GetCommitTextContent (RevCommit c, FilePath file)
		{
			var content = GetCommitContent (c, file);
			if (RawText.IsBinary (content))
				return null;
			return Mono.TextEditor.Utils.TextFileUtility.GetText (content);
		}
		
		string GenerateDiff (byte[] data1, byte[] data2)
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
			var edits = MyersDiff<RawText>.INSTANCE.Diff(RawTextComparator.DEFAULT, text1, text2);
			MemoryStream s = new MemoryStream ();
			var formatter = new NGit.Diff.DiffFormatter (s);
			formatter.Format (edits, text1, text2);
			return Encoding.UTF8.GetString (s.ToArray ());
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
					if (line.StartsWith ("+++ ") || line.StartsWith ("--- ")) {
						string newFile = RootPath.Combine (line.Substring (6));
						if (fileName != null && fileName != newFile) {
							list.Add (new DiffInfo (basePath, fileName, content.ToString ().Trim ('\n')));
							content = new StringBuilder ();
						}
						fileName = newFile;
					} else if (!line.StartsWith ("diff") && !line.StartsWith ("index")) {
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
			else
				return remotes[0];
		}

		public void Push (IProgressMonitor monitor, string remote, string remoteBranch)
		{
			RemoteConfig remoteConfig = new RemoteConfig (RootRepository.GetConfig (), remote);
			Transport tp = Transport.Open (RootRepository, remoteConfig);
			
			string remoteRef = "refs/heads/" + remoteBranch;
			
			RemoteRefUpdate rr = new RemoteRefUpdate (RootRepository, RootRepository.GetBranch (), remoteRef, false, null, null);
			List<RemoteRefUpdate> list = new List<RemoteRefUpdate> ();
			list.Add (rr);
			using (var gm = new GitMonitor (monitor))
				tp.Push (gm, list);
			switch (rr.GetStatus ()) {
			case RemoteRefUpdate.Status.UP_TO_DATE: monitor.ReportSuccess (GettextCatalog.GetString ("Remote branch is up to date.")); break;
			case RemoteRefUpdate.Status.REJECTED_NODELETE: monitor.ReportError (GettextCatalog.GetString ("The server is configured to deny deletion of the branch"), null); break;
			case RemoteRefUpdate.Status.REJECTED_NONFASTFORWARD: monitor.ReportError (GettextCatalog.GetString ("The update is a non-fast-forward update. Merge the remote changes before pushing again."), null); break;
			case RemoteRefUpdate.Status.OK:
				monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
				// Update the remote branch
				ObjectId headId = rr.GetNewObjectId ();
				RefUpdate updateRef = RootRepository.UpdateRef (Constants.R_REMOTES + remote + "/" + remoteBranch);
				updateRef.SetNewObjectId(headId);
				updateRef.Update();
				break;
			default:
				string msg = rr.GetMessage ();
				msg = !string.IsNullOrEmpty (msg) ? msg : GettextCatalog.GetString ("Push operation failed");
				monitor.ReportError (msg, null);
				break;
			}
		}

		public void CreateBranch (string name, string trackSource)
		{
			// If the user did not specify a branch to base the new local
			// branch off, assume they want to create the new branch based
			// on the current HEAD.
			if (string.IsNullOrEmpty (trackSource))
				trackSource = Constants.HEAD;

			ObjectId headId = RootRepository.Resolve (trackSource);
			RefUpdate updateRef = RootRepository.UpdateRef (Constants.R_HEADS + name);
			updateRef.SetNewObjectId(headId);
			updateRef.Update();
			GitUtil.SetUpstreamSource (RootRepository, name, trackSource);
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
			IDictionary<string, NGit.Ref> refs = RootRepository.RefDatabase.GetRefs (Constants.R_HEADS);
			foreach (var pair in refs) {
				string name = NGit.Repository.ShortenRefName (pair.Key);
				Branch br = new Branch ();
				br.Name = name;
				br.Tracking = GitUtil.GetUpstreamSource (RootRepository, name);
				yield return br;
			}
		}

		public IEnumerable<string> GetTags ()
		{
			return RootRepository.GetTags ().Keys;
		}

		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			var refs = RootRepository.RefDatabase.GetRefs (Constants.R_REMOTES);
			foreach (var pair in refs) {
				string name = NGit.Repository.ShortenRefName (pair.Key);
				if (name.StartsWith (remoteName + "/"))
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
			
			// Switch to the target branch
			DirCache dc = RootRepository.LockDirCache ();
			try {
				RevWalk rw = new RevWalk (RootRepository);
				ObjectId branchHeadId = RootRepository.Resolve (branch);
				if (branchHeadId == null)
					throw new InvalidOperationException ("Branch head commit not found");
				
				RevCommit branchCommit = rw.ParseCommit (branchHeadId);
				DirCacheCheckout checkout = new DirCacheCheckout (RootRepository, null, dc, branchCommit.Tree);
				checkout.Checkout ();
				
				RefUpdate u = RootRepository.UpdateRef(Constants.HEAD);
				u.Link ("refs/heads/" + branch);
				monitor.Step (1);
			} catch {
				dc.Unlock ();
				if (GitService.StashUnstashWhenSwitchingBranches) {
					// If something goes wrong, restore the work tree status
					using (var gm = new GitMonitor (monitor))
						stash.Apply (gm);
					stashes.Remove (stash);
				}
				throw;
			}
			
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
			
			FileService.NotifyFilesChanged (modified);
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
			if (stashName.StartsWith ("__MD_"))
				return stashName.Substring (5);
			else
				return null;
		}

		Stash GetStashForBranch (StashCollection stashes, string branchName)
		{
			string sn = GetStashName (branchName);
			foreach (Stash ss in stashes) {
				if (ss.Comment.IndexOf (sn) != -1)
					return ss;
			}
			return null;
		}

		public ChangeSet GetPushChangeSet (string remote, string branch)
		{
			ChangeSet cset = CreateChangeSet (RootPath);
			ObjectId cid1 = RootRepository.Resolve (remote + "/" + branch);
			ObjectId cid2 = RootRepository.Resolve (RootRepository.GetBranch ());
			RevWalk rw = new RevWalk (RootRepository);
			RevCommit c1 = rw.ParseCommit (cid1);
			RevCommit c2 = rw.ParseCommit (cid2);
			
			foreach (var change in GitUtil.CompareCommits (RootRepository, c2, c1)) {
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
				VersionInfo vi = new VersionInfo (RootRepository.FromGitPath (change.GetNewPath ()), "", false, status | VersionStatus.Versioned, null, VersionStatus.Versioned, null);
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
					diff = GenerateDiff (EmptyContent, GetCommitContent (c2, change.GetOldPath ()));
					break;
				case DiffEntry.ChangeType.ADD:
					diff = GenerateDiff (GetCommitContent (c1, change.GetNewPath ()), EmptyContent);
					break;
				default:
					diff = GenerateDiff (GetCommitContent (c1, change.GetNewPath ()), GetCommitContent (c2, change.GetNewPath ()));
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
					string name = string.Format ("{0} <{1}>", author.GetName (), author.GetEmailAddress ());
					var commitTime = new DateTime (1970, 1, 1).AddSeconds (commit.CommitTime);
					list.Add (new Annotation (commit.Name, name, commitTime));
				} else {
					list.Add (new Annotation (new string ('0', 20), "<uncommitted>", DateTime.Now));
				}
			}
			return list.ToArray ();
		}
		
		internal GitRevision GetPreviousRevisionFor (GitRevision revision)
		{
			ObjectId id = revision.GitRepository.Resolve (revision.ToString () + "^");
			if (id == null)
				return null;
			return new GitRevision (this, revision.GitRepository, id.Name);
		}
	}
	
	public class GitRevision: Revision
	{
		string rev;
		
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
				if (rev.Length > 10)
					return rev.Substring (0, 10);
				else
					return rev;
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
	
	class GitMonitor: NGit.ProgressMonitor, IDisposable
	{
		IProgressMonitor monitor;
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

