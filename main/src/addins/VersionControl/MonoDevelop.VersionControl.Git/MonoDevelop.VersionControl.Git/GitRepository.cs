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
using LibGit2Sharp;
using GLib;

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

		public LibGit2Sharp.Repository RootRepository {
			get; set;
		}

		public static event EventHandler BranchSelectionChanged;

		public GitRepository ()
		{
			Url = "git://";
		}

		static string GetRepositoryPath (string path)
		{
			return LibGit2Sharp.Repository.Discover (path);
		}

		public GitRepository (FilePath path, string url)
		{
			path = GetRepositoryPath (path);
			RootPath = path;
			if (!string.IsNullOrEmpty (path))
				RootRepository = new LibGit2Sharp.Repository (path);
			Url = url;
		}
		
		public override void Dispose ()
		{
			if (VersionControlSystem != null)
				((GitVersionControl)VersionControlSystem).UnregisterRepo (this);

			if (RootRepository != null)
				RootRepository.Dispose ();
			foreach (var rep in cachedSubmodules)
				rep.Item2.Dispose ();
			base.Dispose ();
		}
		
		public override string[] SupportedProtocols {
			get {
				return new string[] {"git", /*"ssh",*/ "http", "https", /*"ftp", "ftps", "rsync",*/ "file"};
			}
		}

		public override bool IsUrlValid (string url)
		{
			return Remote.IsSupportedUrl (url);
		}
		
		/*public override string[] SupportedNonUrlProtocols {
			get {
				return new string[] {"ssh/scp"};
			}
		}
		
		public override string Protocol {
			get {
				string p = base.Protocol;
				if (p != null)
					return p;
				return IsUrlValid (Url) ? "ssh/scp" : null;
			}
		}*/

		public override void CopyConfigurationFrom (Repository other)
		{
			base.CopyConfigurationFrom (other);

			GitRepository r = (GitRepository)other;
			RootPath = r.RootPath;
			if (!RootPath.IsNullOrEmpty)
				RootRepository = new LibGit2Sharp.Repository (RootPath);
		}

		public override string LocationDescription {
			get { return Url ?? RootPath; }
		}

		public override bool AllowLocking {
			get { return false; }
		}

		public override string GetBaseText (FilePath localFile)
		{
			Commit c = GetHeadCommit (GetRepository (localFile));
			if (c == null)
				return string.Empty;
			return GetCommitTextContent (c, localFile);
		}
				
		static Commit GetHeadCommit (LibGit2Sharp.Repository repository)
		{
			return repository.Head.Tip;
		}

		public StashCollection GetStashes ()
		{
			return RootRepository.Stashes;
		}

		public CherryPickResult ApplyStash (Stash stash)
		{
			var name = RootRepository.Config.Get<string> ("user.name").Value;
			var email = RootRepository.Config.Get<string> ("user.email").Value;
			CherryPickResult mr = RootRepository.CherryPick (stash.Index, new Signature (name, email, DateTimeOffset.Now));

			foreach (var change in RootRepository.Diff.Compare<TreeChanges> (stash.Index.Tree, stash.Base.Tree))
				RootRepository.Index.Stage (change.Path);
			return mr;
		}

		public CherryPickResult PopStash ()
		{
			int i = RootRepository.Stashes.Count () - 1;
			var stash = RootRepository.Stashes [i];
			CherryPickResult mr = ApplyStash (stash);
			RootRepository.Stashes.Remove (i);

			return mr;
		}

		public void CreateStash (string message)
		{
			RootRepository.Stashes.Add (GetSignature (), message);
			RootRepository.Reset (ResetMode.Hard);
		}

		internal Signature GetSignature()
		{
			string name;
			string email;
			GetUserInfo (out name, out email);
			return new Signature (name, email, DateTimeOffset.Now);
		}

		DateTime cachedSubmoduleTime = DateTime.MinValue;
		Tuple<FilePath, LibGit2Sharp.Repository>[] cachedSubmodules = new Tuple<FilePath, LibGit2Sharp.Repository>[0];
		Tuple<FilePath, LibGit2Sharp.Repository>[] CachedSubmodules {
			get {
				var submoduleWriteTime = File.GetLastWriteTimeUtc(RootPath.Combine(".gitmodules"));
				if (cachedSubmoduleTime != submoduleWriteTime) {
					cachedSubmoduleTime = submoduleWriteTime;
					cachedSubmodules = RootRepository.Submodules.Select (s => {
						var fp = new FilePath (Path.Combine (RootRepository.Info.WorkingDirectory, s.Path.Replace ('/', Path.DirectorySeparatorChar))).CanonicalPath;
						return new Tuple<FilePath, LibGit2Sharp.Repository> (fp, new LibGit2Sharp.Repository (fp));
					}).ToArray ();
				}
				return cachedSubmodules;
			}
		}

		LibGit2Sharp.Repository GetRepository (FilePath localPath)
		{
			return GroupByRepository (new [] { localPath }).First ().Key;
		}

		IEnumerable<IGrouping<LibGit2Sharp.Repository, FilePath>> GroupByRepository (IEnumerable<FilePath> files)
		{
			var cache = CachedSubmodules;
			return files.GroupBy (f => {
				var res = cache.FirstOrDefault (s => f.IsChildPathOf (s.Item1) || f.FullPath == s.Item1);
				return res != null ? res.Item2 : RootRepository;
			});
		}

		protected override Revision[] OnGetHistory (FilePath localFile, Revision since)
		{
			List<Revision> revs = new List<Revision> ();

			var repository = GetRepository (localFile);
			localFile = repository.ToGitPath (localFile);
			var sinceRev = (GitRevision)since;
			var hc = GetHeadCommit (repository);
			if (hc == null)
				return new GitRevision [0];

			// TODO: Replace with queryby when it's implemented.
			var commits = repository.Commits.Where (c => c.Parents.Count() == 1 && c.Tree [localFile] != null &&
					(c.Parents.FirstOrDefault ().Tree [localFile] == null ||
					c.Tree[localFile].Target.Id != c.Parents.FirstOrDefault ().Tree [localFile].Target.Id));

			foreach (var commit in commits) {
				var author = commit.Author;
				var rev = new GitRevision (this, repository, commit.Id.Sha, author.When.LocalDateTime, author.Name, commit.Message);
				rev.Email = author.Email;
				rev.ShortMessage = commit.MessageShort;
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
			var parent = rev.Commit.Parents.FirstOrDefault ();
			var changes = rev.GitRepository.Diff.Compare <TreeChanges>(parent != null ? parent.Tree : null, rev.Commit.Tree);

			foreach (var entry in changes.Added)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
			foreach (var entry in changes.Copied)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
			foreach (var entry in changes.Deleted)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.OldPath), RevisionAction.Delete, null));
			foreach (var entry in changes.Renamed)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Replace, null));
			foreach (var entry in changes.Modified)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));
			foreach (var entry in changes.TypeChanged)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));

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
		Dictionary<LibGit2Sharp.Repository, GitRevision> versionInfoCacheRevision = new Dictionary<LibGit2Sharp.Repository, GitRevision> ();
		Dictionary<LibGit2Sharp.Repository, GitRevision> versionInfoCacheEmptyRevision = new Dictionary<LibGit2Sharp.Repository, GitRevision> ();
		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, IEnumerable<FilePath> localFileNames, bool getRemoteStatus, bool recursive)
		{
			var versions = new List<VersionInfo> ();
			
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
							versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
						}
						else
							localFiles.Add (p);
					}
				}
				// No files to check, we are done
				if (localFiles.Count == 0)
					return versions.ToArray ();
				
				localFileNames = localFiles;
			} else {
				var directories = new List<FilePath> ();
				var files = new List<FilePath> ();

				CollectFiles (files, directories, localDirectory, recursive);
				foreach (var group in GroupByRepository (directories)) {
					var repository = group.Key;
					GitRevision arev;
					if (!versionInfoCacheEmptyRevision.TryGetValue (repository, out arev)) {
						arev = new GitRevision (this, repository, "");
						versionInfoCacheEmptyRevision.Add (repository, arev);
					}
					foreach (var p in group)
						versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
				}
				localFileNames = files;
			}

			foreach (var group in GroupByRepository (localFileNames)) {
				var repository = group.Key;

				GitRevision rev = null;
				Commit headCommit = GetHeadCommit (repository);
				if (headCommit != null) {
					if (!versionInfoCacheRevision.TryGetValue (repository, out rev)) {
						rev = new GitRevision (this, repository, headCommit.Id.Sha);
						versionInfoCacheRevision.Add (repository, rev);
					} else if (rev.ToString () != headCommit.Id.Sha) {
						rev = new GitRevision (this, repository, headCommit.Id.Sha);
						versionInfoCacheRevision [repository] = rev;
					}
				}

				GetDirectoryVersionInfoCore (repository, rev, group.ToList (), versions);
			}

			return versions.ToArray ();
		}

		static void GetDirectoryVersionInfoCore (LibGit2Sharp.Repository repository, GitRevision rev, List<FilePath> localPaths, List<VersionInfo> versions)
		{
			foreach (var file in repository.ToGitPath (localPaths)) {
				var status = repository.Index.RetrieveStatus (file);
				var fstatus = VersionStatus.Versioned;

				if ((status & FileStatus.Added) != 0)
					fstatus |= VersionStatus.ScheduledAdd;
				else if ((status & (FileStatus.Removed | FileStatus.Missing)) != 0)
					fstatus |= VersionStatus.ScheduledDelete;
				else if ((status & (FileStatus.TypeChanged | FileStatus.Modified)) != 0)
					fstatus |= VersionStatus.Modified;
				else if ((status & (FileStatus.RenamedInIndex | FileStatus.RenamedInWorkDir)) != 0)
					fstatus |= VersionStatus.ScheduledReplace;
				else if ((status & (FileStatus.Nonexistent | FileStatus.Untracked)) != 0)
					fstatus = VersionStatus.Unversioned;
				else if ((status & FileStatus.Ignored) != 0)
					fstatus = VersionStatus.Ignored;

				versions.Add (new VersionInfo (repository.FromGitPath (file), "", false, fstatus, rev, fstatus == VersionStatus.Ignored ? VersionStatus.Unversioned : VersionStatus.Versioned, null));
			}

			foreach (var conflict in repository.Index.Conflicts)
				versions.Add (new VersionInfo (conflict.Ours.Path, "", false, VersionStatus.Versioned | VersionStatus.Conflicted, rev, VersionStatus.Versioned, null));
		}
		
		protected override VersionControlOperation GetSupportedOperations (VersionInfo vinfo)
		{
			VersionControlOperation ops = base.GetSupportedOperations (vinfo);
			if (GetCurrentRemote () == null)
				ops &= ~VersionControlOperation.Update;
			if (vinfo.IsVersioned && !vinfo.IsDirectory)
				ops |= VersionControlOperation.Annotate;
			if (!vinfo.IsVersioned && vinfo.IsDirectory)
				ops &= ~VersionControlOperation.Add;
			return ops;
		}

		static void CollectFiles (List<FilePath> files, List<FilePath> directories, FilePath dir, bool recursive)
		{
			if (!Directory.Exists (dir))
				return;

			directories.AddRange (Directory.GetDirectories (dir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
				.Select (f => new FilePath (f)));

			files.AddRange (Directory.GetFiles (dir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
				.Select (f => new FilePath (f).CanonicalPath));
		}

		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
		{
			// Initialize the repository
			RootRepository = new LibGit2Sharp.Repository (LibGit2Sharp.Repository.Init (localPath));
			RootPath = localPath.Combine (".git");
			RootRepository.Network.Remotes.Add ("origin", Url);

			// Add the project files
			ChangeSet cs = CreateChangeSet (localPath);
			foreach (FilePath fp in files) {
				RootRepository.Index.Stage (RootRepository.ToGitPath (fp));
				cs.AddFile (fp);
			}

			// Create the initial commit
			cs.GlobalComment = message;
			Commit (cs, monitor);

			RootRepository.Branches.Update (RootRepository.Branches ["master"], branch => branch.TrackedBranch = "refs/remotes/origin/master");

			RootRepository.Network.Push (RootRepository.Head, new PushOptions {
				OnPushStatusError = delegate (PushStatusError e) {
					RootRepository.Dispose ();
					RootRepository = null;
					if (RootPath.IsDirectory)
						Directory.Delete (RootPath, true);
					throw new UserException (e.Message);
				}
			});

			return this;
		}
		
		protected override void OnUpdate (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			// TODO: Make it work differently for submodules.
			monitor.BeginTask (GettextCatalog.GetString ("Updating"), 5);
			Fetch (monitor);

			if (RootRepository.Head.IsTracking) {
				GitUpdateOptions options = GitService.StashUnstashWhenUpdating ? GitUpdateOptions.NormalUpdate : GitUpdateOptions.UpdateSubmodules;
				if (GitService.UseRebaseOptionWhenPulling)
					Merge (RootRepository.Head.TrackedBranch.Name, options, monitor);
				else
					Rebase (RootRepository.Head.TrackedBranch.Name, options, monitor);
			}

			monitor.EndTask ();
		}

		public void Fetch (IProgressMonitor monitor)
		{
			RootRepository.Fetch (RootRepository.Head.Remote.Name, new FetchOptions {
				OnProgress = null,
				OnTransferProgress = null,
				OnUpdateTips = null,
			});
		}

		public void Rebase (string branch, GitUpdateOptions options, IProgressMonitor monitor)
		{
			/*StashCollection stashes = GitUtil.GetStashes (RootRepository);
			Stash stash = null;
			NGit.Api.Git git = new NGit.Api.Git (RootRepository);
			try
			{
				monitor.BeginTask (GettextCatalog.GetString ("Rebasing"), 5);
				List<string> UpdateSubmodules = new List<string> ();

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
						var conflicts = RootRepository.Index.Conflicts;
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
			}*/
		}

		public void Merge (string branch, GitUpdateOptions options, IProgressMonitor monitor)
		{
			Stash stash = null;
			StashCollection stashes = GetStashes ();

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Merging"), 5);
				List<string> UpdateSubmodules = new List<string> ();
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
					CreateStash (GetStashName ("_tmp_"));
					monitor.Step (1);
				}
				
				// Apply changes
				MergeResult mergeResult = RootRepository.Merge (branch, GetSignature (), new MergeOptions {
					CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
					OnCheckoutNotify = (path, flags) => {
						FileService.NotifyFileChanged (RootRepository.FromGitPath (path));
						return true;
					}
				});

				if (mergeResult.Status == MergeStatus.Conflicts) {
					bool commit = true;
					foreach (var conflictFile in RootRepository.Index.Conflicts) {
						ConflictResult res = ResolveConflict (RootRepository.FromGitPath (conflictFile.Ancestor.Path));
						if (res == ConflictResult.Abort) {
							RootRepository.Reset (ResetMode.Hard);
							commit = false;
							break;
						} else if (res == ConflictResult.Skip) {
							Revert (RootRepository.FromGitPath (conflictFile.Ancestor.Path), false, monitor);
							break;
						}
					}
					RootRepository.Commit ("TODO: Fix this message.");
				}
			} finally {
				if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges)
					monitor.Step (1);
				
				// Restore local changes
				if (stash != null) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					ApplyStash (stash);
					stashes.Remove (stash);
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
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

			var repo = (GitRepository)changeSet.Repository;
			repo.RootRepository.Index.Stage (changeSet.Items.Select (i => i.LocalPath).ToPathStrings ());

			if (changeSet.ExtendedProperties.Contains ("Git.AuthorName"))
				repo.RootRepository.Commit (message, new Signature (
					(string)changeSet.ExtendedProperties ["Git.AuthorName"],
					(string)changeSet.ExtendedProperties ["Git.AuthorEmail"],
					DateTimeOffset.Now));
			else
				repo.RootRepository.Commit (message);
		}

		public bool IsUserInfoDefault ()
		{
			var name = RootRepository.Config.Get<string> ("user.name");
			var email = RootRepository.Config.Get<string> ("user.email");
			return name == null && email == null;
		}
		
		public void GetUserInfo (out string name, out string email)
		{
			name = RootRepository.Config.Get<string> ("user.name").Value;
			email = RootRepository.Config.Get<string> ("user.email").Value;
		}

		public void SetUserInfo (string name, string email)
		{
			RootRepository.Config.Set ("user.name", name);
			RootRepository.Config.Set ("user.email", email);
		}

		protected override void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			RootPath = LibGit2Sharp.Repository.Clone (Url, targetLocalPath, new CloneOptions {
				// TODO:
				Credentials = null,
				OnCheckoutProgress = null,
				OnTransferProgress = null
			});
			RootRepository = new LibGit2Sharp.Repository (RootPath);
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				monitor.BeginTask (GettextCatalog.GetString ("Reverting files"), 1);

				repository.CheckoutPaths ("HEAD", group.ToPathStrings (), new CheckoutOptions {
					CheckoutModifiers = CheckoutModifiers.Force,
					CheckoutNotifyFlags = CheckoutNotifyFlags.Updated | CheckoutNotifyFlags.Untracked,
					OnCheckoutNotify = delegate (string path, CheckoutNotifyFlags notifyFlags) {
						var file = repository.FromGitPath (path);
						if ((notifyFlags & CheckoutNotifyFlags.Untracked) != 0)
							FileService.NotifyFileRemoved (file);
						else
							FileService.NotifyFileChanged (file);
						return true;
					}
				});
				monitor.EndTask ();
			}
		}

		protected override void OnRevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}

		protected override void OnRevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			LibGit2Sharp.Repository repo = GetRepository (localPath);
			GitRevision gitRev = (GitRevision)revision;

			// Rewrite file data from selected revision.
			repo.CheckoutPaths (gitRev.Commit.Sha, new [] { repo.ToGitPath (localPath) }, new CheckoutOptions {
				CheckoutModifiers = CheckoutModifiers.Force
			});

			monitor.ReportSuccess (GettextCatalog.GetString ("Successfully reverted {0} to revision {1}", localPath, gitRev));
		}

		protected override void OnAdd (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				var files = group.Where (f => !f.IsDirectory);
				if (files.Any ())
					repository.Index.Stage (repository.ToGitPath (files));
			}
		}

		protected override void OnDeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor, bool keepLocal)
		{
			DeleteCore (localPaths, keepLocal);

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
			DeleteCore (localPaths, keepLocal);

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

		void DeleteCore (FilePath[] localPaths, bool keepLocal)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				var files = group;

				repository.Index.Remove (repository.ToGitPath (files), !keepLocal);
			}
		}

		protected override string OnGetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			var gitRev = (GitRevision)revision;
			var blob = (Blob)gitRev.Commit [gitRev.GitRepository.ToGitPath (repositoryPath)].Target;
			return blob.GetContentText ();
		}

		public override DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			try {
				var repository = GetRepository (baseLocalPath);
				var patch = repository.Diff.Compare<Patch> (new [] { versionInfo.LocalPath }.ToPathStrings ());
				return new DiffInfo (baseLocalPath, versionInfo.LocalPath, patch.Content);
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

		byte[] GetCommitContent (Commit c, FilePath file)
		{
			var repository = GetRepository (file);
			var blob = (Blob)c [repository.ToGitPath (file)].Target;
			using (var stream = blob.GetContentStream ())
			using (var ms = new MemoryStream ()) {
				stream.CopyTo (ms);
				return ms.ToArray ();
			}
		}

		string GetCommitTextContent (Commit c, FilePath file)
		{
			var repository = GetRepository (file);
			var blob = (Blob)c [repository.ToGitPath (file)].Target;
			if (blob.IsBinary)
				return String.Empty;
			return blob.GetContentText ();
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
			bool success = true;
			RootRepository.Network.Push (RootRepository.Network.Remotes [remote], "refs/heads/" + remoteBranch,	new PushOptions {
				OnPushStatusError = delegate (PushStatusError pushStatusErrors) {
					monitor.ReportError (pushStatusErrors.Message, null);
					success = false;
				}
			});

			if (!success)
				return;

			monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
		}

		public void CreateBranchFromCommit (string name, Commit id)
		{
			RootRepository.Branches.Add (name, id);
		}

		public void CreateBranch (string name, string trackSource)
		{
			RootRepository.Branches.Update (RootRepository.Branches.Add (name, RootRepository.Head.Tip),
				bu => bu.Remote = trackSource);
		}

		public void SetBranchTrackSource (string name, string trackSource)
		{
			var branch = RootRepository.Branches [name];
			if (branch != null) {
				RootRepository.Branches.Update (branch,	bu => bu.TrackedBranch = trackSource);
			} else
				RootRepository.Branches.Add (name, trackSource);
		}

		public void RemoveBranch (string name)
		{
			RootRepository.Branches.Remove (name);
		}

		public void RenameBranch (string name, string newName)
		{
			RootRepository.Branches.Rename (name, newName, true);
		}

		public IEnumerable<Remote> GetRemotes ()
		{
			return RootRepository.Network.Remotes;
		}
		
		public bool IsBranchMerged (string branchName)
		{
			// check if a branch is merged into HEAD
			var tip = RootRepository.Branches [branchName].Tip.Sha;
			return RootRepository.Commits.Any (c => c.Sha == tip);
		}

		public void RenameRemote (string name, string newName)
		{
			RootRepository.Network.Remotes.Rename (name, newName);
		}

		public void AddRemote (string name, string url, bool importTags)
		{
			if (string.IsNullOrEmpty (name))
				throw new InvalidOperationException ("Name not set");

			RootRepository.Network.Remotes.Update (RootRepository.Network.Remotes.Add (name, url),
				r => r.TagFetchMode = importTags ? TagFetchMode.All : TagFetchMode.Auto);
		}

		public void RemoveRemote (string name)
		{
			RootRepository.Network.Remotes.Remove (name);
		}

		public IEnumerable<Branch> GetBranches ()
		{
			return RootRepository.Branches.Where (b => !b.IsRemote);
		}

		public IEnumerable<string> GetTags ()
		{
			return RootRepository.Tags.Select (t => t.Name);
		}

		public void AddTag (string name, Revision rev, string message)
		{
			var gitRev = (GitRevision)rev;
			RootRepository.Tags.Add (name, gitRev.Commit, GetSignature(), message);
		}

		public void RemoveTag (string name)
		{
			RootRepository.Tags.Remove (name);
		}

		public void PushAllTags ()
		{
			// TODO:
			// https://github.com/libgit2/libgit2sharp/issues/104#issuecomment-13393975
		}

		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			return RootRepository.Branches.Where (b => b.IsRemote && b.Remote.Name == remoteName).Select (b => b.Name.Substring (b.Name.IndexOf ('/') + 1));
		}

		public string GetCurrentBranch ()
		{
			return RootRepository.Head.Name;
		}

		public void SwitchToBranch (IProgressMonitor monitor, string branch)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), GitService.StashUnstashWhenSwitchingBranches ? 4 : 2);

			// Get a list of files that are different in the target branch
			var statusList = GitUtil.GetChangedFiles (RootRepository, branch);

			if (GitService.StashUnstashWhenSwitchingBranches) {
				// Remove the stash for this branch, if exists
				string currentBranch = GetCurrentBranch ();
				var stash = GetStashForBranch (RootRepository.Stashes, branch);
				if (stash != null)
					RootRepository.Stashes.Remove (stash);

				CreateStash (currentBranch);
				monitor.Step (1);
			}

			try {
				RootRepository.Reset (ResetMode.Hard);
				RootRepository.Checkout (branch, new CheckoutOptions {
					OnCheckoutNotify = null
				});
			} finally {
				// Restore the branch stash
				if (GitService.StashUnstashWhenSwitchingBranches) {
					var stash = GetStashForBranch (RootRepository.Stashes, branch);
					if (stash != null)
						RootRepository.Merge (stash.Index, GetSignature ());
					monitor.Step (1);
				}
			}
			// Notify file changes
			NotifyFileChanges (monitor, statusList);
			
			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);

			monitor.EndTask ();
		}

		void NotifyFileChanges (IProgressMonitor monitor,TreeChanges statusList)
		{
			// Files added to source branch not present to target branch.
			var removed = statusList.Where (c => c.Status == ChangeKind.Added).Select (c => GetRepository (c.Path).FromGitPath (c.Path)).ToList ();
			var modified = statusList.Where (c => c.Status != ChangeKind.Added).Select (c => GetRepository (c.Path).FromGitPath (c.Path)).ToList ();
			
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
				if (ss.Name.IndexOf (sn, StringComparison.InvariantCulture) != -1)
					return ss;
			}
			return null;
		}

		public ChangeSet GetPushChangeSet (string remote, string branch)
		{
			ChangeSet cset = CreateChangeSet (RootPath);

			Commit reference = RootRepository.Branches [remote + "/" + branch].Tip;
			Commit compared = RootRepository.Head.Tip;
			
			foreach (var change in GitUtil.CompareCommits (RootRepository, reference, compared)) {
				VersionStatus status;
				switch (change.Status) {
				case ChangeKind.Added:
				case ChangeKind.Copied:
					status = VersionStatus.ScheduledAdd;
					break;
				case ChangeKind.Deleted:
					status = VersionStatus.ScheduledDelete;
					break;
				case ChangeKind.Renamed:
					status = VersionStatus.ScheduledReplace;
					break;
				default:
					status = VersionStatus.Modified;
					break;
				}
				VersionInfo vi = new VersionInfo (RootRepository.FromGitPath (change.Path), "", false, status | VersionStatus.Versioned, null, VersionStatus.Versioned, null);
				cset.AddFile (vi);
			}
			return cset;
		}

		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			Commit reference = RootRepository.Branches[remote + "/" + branch].Tip;
			Commit compared = RootRepository.Head.Tip;
			
			List<DiffInfo> diffs = new List<DiffInfo> ();
			var patch = RootRepository.Diff.Compare<Patch> (reference.Tree, compared.Tree);
			foreach (var change in GitUtil.CompareCommits (RootRepository, reference, compared)) {
				string path;
				switch (change.Status) {
				case ChangeKind.Deleted:
				case ChangeKind.Renamed:
					path = change.OldPath;
					break;
				default:
					path = change.Path;
					break;
				}
				diffs.Add (new DiffInfo (RootPath, RootRepository.FromGitPath (path), patch[path].Patch));
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
			Commit hc = GetHeadCommit (repository);
			if (hc == null)
				return new Annotation [0];

			int lines = File.ReadAllLines (repositoryPath).Length;
			var list = new List<Annotation> (lines);
			var working = new Annotation (GettextCatalog.GetString ("working copy"), "<uncommitted>", DateTime.Now);
			for (int i = 0; i < lines; ++i)
				list.Add (working);

			foreach (var hunk in repository.Blame (repository.ToGitPath (repositoryPath))) {
				var commit = hunk.FinalCommit;
				var author = hunk.FinalSignature;
				working = new Annotation (commit.Sha, author.Name, author.When.LocalDateTime, String.Format ("<{0}>", author.Email));
				for (int i = 0; i < hunk.LineCount; ++i)
					list [hunk.FinalStartLineNumber + i] = working;
			}

			return list.ToArray ();
		}
		
		internal GitRevision GetPreviousRevisionFor (GitRevision revision)
		{
			var id = revision.Commit.Parents.FirstOrDefault ();
			return id == null ? null : new GitRevision (this, revision.GitRepository, id.Sha) {
				Commit = id
			};
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
			RootRepository.Index.Stage (".gitignore");
			RootRepository.Ignore.AddTemporaryRules (RootRepository.ToGitPath (localPath));
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
			RootRepository.Index.Stage (".gitignore");
			RootRepository.Ignore.ResetAllTemporaryRules ();
		}
	}
	
	public class GitRevision: Revision
	{
		readonly string rev;
		
		internal Commit Commit { get; set; }
		internal FilePath FileForChanges { get; set; }

		public LibGit2Sharp.Repository GitRepository {
			get; private set;
		}

		public GitRevision (Repository repo, LibGit2Sharp.Repository gitRepository, string rev) : base(repo)
		{
			this.rev = rev;
			GitRepository = gitRepository;
		}

		public GitRevision (Repository repo, LibGit2Sharp.Repository gitRepository, string rev, DateTime time, string author, string message) : base(repo, time, author, message)
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
}

