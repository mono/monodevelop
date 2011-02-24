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

namespace MonoDevelop.VersionControl.Git
{
	public class GitRepository : UrlBasedRepository
	{
		NGit.Repository repo;
		FilePath path;

		public static event EventHandler BranchSelectionChanged;

		public GitRepository ()
		{
			Url = "git://";
		}

		public GitRepository (FilePath path, string url)
		{
			this.path = path;
			Url = url;
			repo = new FileRepository (path.Combine (Constants.DOT_GIT));
		}
		
		public override string[] SupportedProtocols {
			get {
				return new string[] {"git", "ssh", "http", "https", "ftp", "ftps", "rsync"};
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

		public FilePath RootPath {
			get { return path; }
		}

		public override void CopyConfigurationFrom (Repository other)
		{
			base.CopyConfigurationFrom (other);
			GitRepository r = (GitRepository)other;
			path = r.path;
			if (r.repo != null)
				repo = new FileRepository (path);
		}

		public override string LocationDescription {
			get { return Url ?? path; }
		}

		public override bool AllowLocking {
			get { return false; }
		}

		public override string GetBaseText (FilePath localFile)
		{
			RevCommit c = GetHeadCommit ();
			if (c == null)
				return string.Empty;
			return GetCommitContent (c, localFile);
		}
				
		RevCommit GetHeadCommit ()
		{
			RevWalk rw = new RevWalk (repo);
			ObjectId headId = repo.Resolve (Constants.HEAD);
			if (headId == null)
				return null;
			return rw.ParseCommit (headId);
		}

		public override Revision[] GetHistory (FilePath localFile, Revision since)
		{
			List<Revision> revs = new List<Revision> ();
			
			RevWalk walk = new RevWalk (repo);
			var hc = GetHeadCommit ();
			if (hc == null)
				return new GitRevision [0];
			walk.MarkStart (hc);
			
			foreach (RevCommit commit in walk) {
				List<RevisionPath> paths = new List<RevisionPath> ();
				foreach (Change change in GitUtil.GetCommitChanges (repo, commit)) {
					FilePath cpath = FromGitPath (change.Path);
					if (cpath != localFile && !cpath.IsChildPathOf (localFile))
						continue;
					RevisionAction ra;
					switch (change.ChangeType) {
					case ChangeType.Added:
						ra = RevisionAction.Add;
						break;
					case ChangeType.Deleted:
						ra = RevisionAction.Delete;
						break;
					default:
						ra = RevisionAction.Modify;
						break;
					}
					RevisionPath p = new RevisionPath (cpath, ra, null);
					paths.Add (p);
				}
				if (paths.Count > 0) {
					PersonIdent author = commit.GetAuthorIdent ();
					GitRevision rev = new GitRevision (this, commit.Id.Name, author.GetWhen().ToLocalTime (), author.GetName (), commit.GetFullMessage (), paths.ToArray ());
					rev.Email = author.GetEmailAddress ();
					rev.ShortMessage = commit.GetShortMessage ();
					revs.Add (rev);
				}
			}
			return revs.ToArray ();
		}



		public override VersionInfo GetVersionInfo (FilePath localPath, bool getRemoteStatus)
		{
			if (Directory.Exists (localPath)) {
				GitRevision rev = new GitRevision (this, "");
				return new VersionInfo (localPath, "", true, VersionStatus.Versioned, rev, VersionStatus.Versioned, null);
			} else {
				VersionInfo[] infos = GetDirectoryVersionInfo (localPath.ParentDirectory, localPath.FileName, getRemoteStatus, false);
				if (infos.Length == 1)
					return infos[0];
				else
					return null;
			}
		}

		public override VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			return GetDirectoryVersionInfo (localDirectory, null, getRemoteStatus, recursive);
		}

		string ToGitPath (FilePath filePath)
		{
			if (!filePath.IsAbsolute)
				return filePath;
			return filePath.FullPath.ToRelative (path).ToString ().Replace ('\\', '/');
		}

		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, string fileName, bool getRemoteStatus, bool recursive)
		{
			HashSet<FilePath> existingFiles = new HashSet<FilePath> ();
			if (fileName != null) {
				FilePath fp = localDirectory.Combine (fileName).CanonicalPath;
				if (File.Exists (fp))
					existingFiles.Add (fp);
			} else
				CollectFiles (existingFiles, localDirectory, recursive);
			
			GitRevision rev;
			var headCommit = GetHeadCommit ();
			if (headCommit != null)
				rev = new GitRevision (this, headCommit.Id.Name);
			else
				rev = null;
			List<VersionInfo> versions = new List<VersionInfo> ();
			FilePath p = fileName != null ? localDirectory.Combine (fileName) : localDirectory;
			p = p.CanonicalPath;
			
			RepositoryStatus status;
			if (fileName != null)
				status = GitUtil.GetFileStatus (repo, p);
			else
				status = GitUtil.GetDirectoryStatus (repo, localDirectory, recursive);
			
			HashSet<string> added = new HashSet<string> ();
			
			Action<IEnumerable<string>, VersionStatus> AddFiles = delegate(IEnumerable<string> files, VersionStatus fstatus) {
				foreach (string file in files) {
					if (!added.Add (file))
						continue;
					FilePath statFile = FromGitPath (file);
					existingFiles.Remove (statFile.CanonicalPath);
					VersionInfo vi = new VersionInfo (statFile, "", false, fstatus, rev, VersionStatus.Versioned, null);
					versions.Add (vi);
				}
			};
			
			AddFiles (status.Added, VersionStatus.Versioned | VersionStatus.ScheduledAdd);
			AddFiles (status.Modified, VersionStatus.Versioned | VersionStatus.Modified);
			AddFiles (status.Removed, VersionStatus.Versioned | VersionStatus.ScheduledDelete);
			AddFiles (status.Missing, VersionStatus.Versioned | VersionStatus.ScheduledDelete);
			AddFiles (status.MergeConflict, VersionStatus.Versioned | VersionStatus.Conflicted);
			AddFiles (status.Untracked, VersionStatus.Unversioned);
			
						/*				else if (staged == 'R') {
					// Renamed files are in reality files delete+added to a different location.
					existingFiles.Remove (srcFile.CanonicalPath);
					VersionInfo rvi = new VersionInfo (srcFile, "", false, VersionStatus.Versioned | VersionStatus.ScheduledDelete, rev, VersionStatus.Versioned, null);
					versions.Add (rvi);
					status = VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				}
				else*/

			// Files for which git did not report an status are supposed to be tracked
			foreach (FilePath file in existingFiles) {
				VersionInfo vi = new VersionInfo (file, "", false, VersionStatus.Versioned, rev, VersionStatus.Versioned, null);
				versions.Add (vi);
			}
			
			return versions.ToArray ();
		}

		void CollectFiles (HashSet<FilePath> files, FilePath dir, bool recursive)
		{
			foreach (string file in Directory.GetFiles (dir))
				files.Add (new FilePath (file).CanonicalPath);
			if (recursive) {
				foreach (string sub in Directory.GetDirectories (dir))
					CollectFiles (files, sub, true);
			}
		}

		public override Repository Publish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
		{
			// Initialize the repository
			repo = GitUtil.Init (localPath, Url, monitor);
			path = localPath;
			
			// Add the project files
			ChangeSet cs = CreateChangeSet (localPath);
			NGit.Api.Git git = new NGit.Api.Git (repo);
			var cmd = git.Add ();
			foreach (FilePath fp in files) {
				cmd.AddFilepattern (ToGitPath (fp));
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
		
		public override bool CanUpdate (FilePath localPath)
		{
			return base.CanUpdate (localPath) && GetCurrentRemote () != null;
		}

		public override void Update (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			IEnumerable<Change> statusList = null;
			
			StashCollection stashes = GitUtil.GetStashes (repo);
			Stash stash = null;
			
			monitor.BeginTask (GettextCatalog.GetString ("Updating"), 5);
			
			try {
				// Fetch remote commits
				string remote = GetCurrentRemote ();
				if (remote == null)
					throw new InvalidOperationException ("No remotes defined");
				monitor.Log.WriteLine (GettextCatalog.GetString ("Fetching from '{0}'", remote));
				RemoteConfig remoteConfig = new RemoteConfig (repo.GetConfig (), remote);
				Transport tn = Transport.Open (repo, remoteConfig);
				tn.Fetch (new GitMonitor (monitor), null);
				monitor.Step (1);
				
				string upstreamRef = GitUtil.GetUpstreamSource (repo, GetCurrentBranch ());
				if (upstreamRef == null)
					upstreamRef = GetCurrentRemote () + "/" + GetCurrentBranch ();
				
				ObjectId upstreamId = repo.Resolve (upstreamRef);
				if (upstreamId == null)
					throw new UserException (GettextCatalog.GetString ("Branch '{0}' not found. Please set a valid upstream reference to be tracked by branch '{1}'", upstreamRef, GetCurrentBranch ()));
				
				// Get a list of files that are different in the target branch
				statusList = GitUtil.GetChangedFiles (repo, upstreamRef);
				monitor.Step (1);
				
				// Save local changes
				monitor.Log.WriteLine (GettextCatalog.GetString ("Saving local changes"));
				stash = stashes.Create (GetStashName ("_tmp_"));
				monitor.Step (1);
				
				GitMonitor gmonitor = new GitMonitor (monitor);
				
				NGit.Api.Git git = new NGit.Api.Git (repo);
				RebaseCommand rebase = git.Rebase ();
				rebase.SetOperation (RebaseCommand.Operation.BEGIN);
				rebase.SetUpstream (upstreamRef);
				rebase.SetProgressMonitor (gmonitor);
				bool aborted = false;
				
				try {
					var result = rebase.Call ();
					while (!aborted && result.GetStatus () == RebaseResult.Status.STOPPED) {
						rebase = git.Rebase ();
						rebase.SetProgressMonitor (gmonitor);
						rebase.SetOperation (RebaseCommand.Operation.CONTINUE);
						bool commitChanges = true;
						var conflicts = GitUtil.GetConflictedFiles (repo);
						foreach (string conflictFile in conflicts) {
							ConflictResult res = ResolveConflict (FromGitPath (conflictFile));
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
				}
				
			} finally {
				// Restore local changes
				if (stash != null) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					stash.Apply ();
					stashes.Remove (stash);
				}
			}
			monitor.Step (1);
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (monitor, statusList);
			
			monitor.EndTask ();
		}

		public void Merge (string branch, IProgressMonitor monitor)
		{
			IEnumerable<Change> statusList = null;
			Stash stash = null;
			StashCollection stashes = new StashCollection (repo);
			monitor.BeginTask (null, 4);
			
			try {
				// Get a list of files that are different in the target branch
				statusList = GitUtil.GetChangedFiles (repo, branch);
				monitor.Step (1);
				
				// Save local changes
				stash = stashes.Create (GetStashName ("_tmp_"));
				monitor.Step (1);
				
				// Apply changes
				
				ObjectId branchId = repo.Resolve (branch);
				
				NGit.Api.Git git = new NGit.Api.Git (repo);
				MergeCommandResult mergeResult = git.Merge ().SetStrategy (MergeStrategy.RESOLVE).Include (branchId).Call ();
				if (mergeResult.GetMergeStatus () == MergeStatus.CONFLICTING || mergeResult.GetMergeStatus () == MergeStatus.FAILED) {
					var conflicts = mergeResult.GetConflicts ();
					bool commit = true;
					foreach (string conflictFile in conflicts.Keys) {
						ConflictResult res = ResolveConflict (FromGitPath (conflictFile));
						if (res == ConflictResult.Abort) {
							GitUtil.HardReset (repo, GetHeadCommit ());
							commit = false;
							break;
						} else if (res == ConflictResult.Skip) {
							Revert (FromGitPath (conflictFile), false, monitor);
							break;
						}
					}
					if (commit)
						git.Commit ().Call ();
				}
				
			} finally {
				// Restore local changes
				if (stash != null) {
					stash.Apply ();
					stashes.Remove (stash);
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

		public override void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
			PersonIdent author = new PersonIdent (repo);
			PersonIdent committer = new PersonIdent (repo);
			string message = changeSet.GlobalComment;
			
			if (string.IsNullOrEmpty (message))
				throw new ArgumentException ("Commit message must not be null or empty!", "message");
			if (string.IsNullOrEmpty (author.GetName ()))
				throw new ArgumentException ("Author name must not be null or empty!", "author");
			
			RepositoryState state = repo.GetRepositoryState ();
			if (!state.CanCommit ()) {
				throw new WrongRepositoryStateException ("Cannot commit with repository in state: " + state);
			}
		
			try {
				Ref head = repo.GetRef (Constants.HEAD);
				if (head == null)
					throw new InvalidOperationException ("No HEAD");
				
				List<ObjectId> parents = new List<ObjectId>();
				
				// determine the current HEAD and the commit it is referring to
				ObjectId headId = repo.Resolve (Constants.HEAD + "^{commit}");
				if (headId != null)
					parents.Insert (0, headId);
				
				ObjectInserter odi = repo.NewObjectInserter ();
				try {
					List<string> filePaths = GetFilesInPaths (changeSet.Items.Select (i => i.LocalPath));
					ObjectId indexTreeId = CreateCommitTree (filePaths);
					
					ObjectId commitId = GitUtil.CreateCommit (repo, message, parents, indexTreeId, author, committer);
					
					RevWalk revWalk = new RevWalk (repo);
					try {
						RevCommit revCommit = revWalk.ParseCommit (commitId);
						RefUpdate ru = repo.UpdateRef (Constants.HEAD);
						ru.SetNewObjectId (commitId);
						ru.SetRefLogMessage ("commit : " + revCommit.GetShortMessage (), false);
						ru.SetExpectedOldObjectId (headId);
						RefUpdate.Result rc = ru.Update ();
						switch (rc) {
						case RefUpdate.Result.NEW:
						case RefUpdate.Result.FAST_FORWARD:
						{
							Unstage (filePaths);
							if (state == RepositoryState.MERGING_RESOLVED) {
								// Commit was successful. Now delete the files
								// used for merge commits
								repo.WriteMergeCommitMsg (null);
								repo.WriteMergeHeads (null);
							}
							return;
						}
						
						case RefUpdate.Result.REJECTED:
						case RefUpdate.Result.LOCK_FAILURE:
							throw new ConcurrentRefUpdateException (JGitText.Get ().couldNotLockHEAD, ru.GetRef (), rc);

						default:
							throw new JGitInternalException ("Reference update failed");
						}
					} finally {
						revWalk.Release ();
					}
				} finally {
					odi.Release ();
				}
			} catch (UnmergedPathException) {
				// since UnmergedPathException is a subclass of IOException
				// which should not be wrapped by a JGitInternalException we
				// have to catch and re-throw it here
				throw;
			} catch (IOException e) {
				throw new JGitInternalException (JGitText.Get ().exceptionCaughtDuringExecutionOfCommitCommand, e);
			}
		}		

		ObjectId CreateCommitTree (IEnumerable<string> files)
		{
			int basePathLength = Path.GetFullPath (this.path).TrimEnd ('/','\\').Length;
			
			// Expand directory paths into file paths. Convert paths to full paths.
			List<string> filePaths = new List<string> ();
			foreach (var path in files) {
				string fullPath = path;
				if (!Path.IsPathRooted (fullPath))
					fullPath = Path.Combine (path, fullPath);
				fullPath = Path.GetFullPath (fullPath).TrimEnd ('/','\\');
				DirectoryInfo dir = new DirectoryInfo (fullPath);
				if (dir.Exists)
					filePaths.AddRange (GetDirectoryFiles (dir));
				else
					filePaths.Add (fullPath);
			}
			
			// Read the tree of the last commit. We are going to update it.
			var hc = GetHeadCommit ();
			NGit.Tree tree = hc != null ? repo.MapTree (hc) : new Tree (repo);

			// Keep a list of trees that have been modified, since they have to be written.
			HashSet<NGit.Tree> modifiedTrees = new HashSet<NGit.Tree> ();
			
			// Update the tree
			foreach (string fullPath in filePaths) {
				string relPath = fullPath.Substring (basePathLength + 1).Replace ('\\','/');				
				NGit.TreeEntry treeEntry = tree.FindBlobMember (relPath);
				
				if (File.Exists (fullPath)) {
					// Looks like an old directory is now a file. Delete the subtree and create a new entry for the file.
					if (treeEntry != null && !(treeEntry is FileTreeEntry))
						treeEntry.Delete ();

					FileTreeEntry fileEntry = treeEntry as FileTreeEntry;
					var inserter = repo.ObjectDatabase.NewInserter ();
					ObjectId id;
					try {
						FileStream fs = new FileStream (fullPath, System.IO.FileMode.Open, FileAccess.Read);
						id = inserter.Insert(Constants.OBJ_BLOB, fs.Length, fs);
						inserter.Flush();
					}
					finally {
						inserter.Release ();
					}
					
					bool executable = repo.FileSystem.CanExecute (fullPath);
					if (fileEntry == null) {
						// It's a new file. Add it.
						fileEntry = (FileTreeEntry) tree.AddFile (relPath);
						treeEntry = fileEntry;
					} else if (fileEntry.GetId () == id && executable == fileEntry.IsExecutable ()) {
						// Same file, ignore it
						continue;
					}
					
					fileEntry.SetId (id);
					fileEntry.SetExecutable (executable);
				}
				else {
					// Deleted file or directory. Remove from the tree
					if (treeEntry != null) {
						NGit.Tree ptree = treeEntry.GetParent ();
						treeEntry.Delete ();
						// Remove the subtree if it's now empty
						while (ptree != null && ptree.MemberCount() == 0) {
							NGit.Tree nextParent = ptree.GetParent ();
							ptree.Delete ();
							ptree = nextParent;
						}
					}
					else
						continue; // Already deleted.
				}
				modifiedTrees.Add (treeEntry.GetParent ());
			}

			// check if tree is different from current commit's tree
			if (modifiedTrees.Count == 0)
				throw new InvalidOperationException("There are no changes to commit");
			
			// Create new trees if there is any change
			return SaveTree (tree, modifiedTrees);
		}
		
		ObjectId SaveTree (NGit.Tree tree, HashSet<NGit.Tree> modifiedTrees)
		{
			// Saves tree that have been modified (that is, which are in the provided list or
			// which have child trees that have been modified)
			
			bool childModified = false;
			foreach (var te in tree.Members ()) {
				NGit.Tree childTree = te as NGit.Tree;
				if (childTree != null) {
					ObjectId newId = SaveTree (childTree, modifiedTrees);
					if (newId != null) {
						childTree.SetId (newId);
						childModified = true;
					}
				}
			}
			if (childModified || modifiedTrees.Contains (tree)) {
				var writer = repo.ObjectDatabase.NewInserter ();
				try {
					return writer.Insert (Constants.OBJ_TREE, tree.Format ());
				} finally {
					writer.Release ();
				}
			} else
				return null;
		}
		
		List<string> GetFilesInPaths (IEnumerable<FilePath> paths)
		{
			int basePathLength = Path.GetFullPath (this.path).TrimEnd ('/','\\').Length;
			
			// Expand directory paths into file paths. Convert paths to full paths.
			List<string> filePaths = new List<string> ();
			foreach (var path in paths) {
				string fullPath = path;
				if (!Path.IsPathRooted (fullPath))
					fullPath = Path.Combine (path, fullPath);
				fullPath = Path.GetFullPath (fullPath).TrimEnd ('/','\\');
				DirectoryInfo dir = new DirectoryInfo (fullPath);
				if (dir.Exists)
					filePaths.AddRange (GetDirectoryFiles (dir));
				else
					filePaths.Add (fullPath);
			}
			return filePaths;
		}
		
		IEnumerable<string> GetDirectoryFiles (DirectoryInfo dir)
		{
			FileTreeIterator iter = new FileTreeIterator (dir.FullName, repo.FileSystem, WorkingTreeOptions.KEY.Parse(repo.GetConfig()));
			while (!iter.Eof) {
				var file = iter.GetEntryFile ();
				if (file != null && !iter.IsEntryIgnored ())
					yield return file.GetPath ();
				iter.Next (1);
			}
		}
		
		void Unstage (IEnumerable<string> files)
		{
			GitIndex index = repo.GetIndex ();
			index.RereadIfNecessary();
			RevWalk rw = new RevWalk (repo);
			
			foreach (var file in files) {
				string path = file;
				GitIndex.Entry e = index.GetEntry (ToGitPath (path));
				if (e != null)
					index.Add (repo.WorkTree, path);
			}
			index.Write();
		}

		public void GetUserInfo (out string name, out string email)
		{
			UserConfig config = repo.GetConfig ().Get (UserConfig.KEY);
			name = config.GetCommitterName ();
			email = config.GetCommitterEmail ();
		}

		public void SetUserInfo (string name, string email)
		{
			NGit.StoredConfig config = repo.GetConfig ();
			config.SetString ("user", null, "name", name);
			config.SetString ("user", null, "email", email);
			config.Save ();
		}

		public override void Checkout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			CloneCommand cmd = NGit.Api.Git.CloneRepository ();
			cmd.SetURI (Url);
			cmd.SetRemote ("origin");
			cmd.SetBranch ("refs/heads/master");
			cmd.SetDirectory ((string)targetLocalPath);
			cmd.SetProgressMonitor (new GitMonitor (monitor));
			cmd.Call ();
		}

		public override void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			RevWalk rw = new RevWalk (repo);
			var c = GetHeadCommit ();
			RevTree tree = c != null ? c.Tree : null;
			
			List<FilePath> changedFiles = new List<FilePath> ();
			List<FilePath> removedFiles = new List<FilePath> ();
			
			monitor.BeginTask (GettextCatalog.GetString ("Revering files"), 3);
			monitor.BeginStepTask (GettextCatalog.GetString ("Revering files"), localPaths.Length, 2);
			
			DirCache dc = repo.LockDirCache ();
			DirCacheEditor editor = dc.Editor ();
			DirCacheBuilder builder = dc.Builder ();
			
			try {
				HashSet<string> entriesToRemove = new HashSet<string> ();
				HashSet<string> foldersToRemove = new HashSet<string> ();
				
				// Add the new entries
				foreach (FilePath fp in localPaths) {
					string p = ToGitPath (fp);
					
					// Register entries to be removed from the index
					if (Directory.Exists (fp))
						foldersToRemove.Add (p);
					else
						entriesToRemove.Add (p);
					
					TreeWalk tw = tree != null ? TreeWalk.ForPath (repo, p, tree) : null;
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
							r = new NGit.Treewalk.TreeWalk(repo);
							r.Reset (tree);
							r.Filter = PathFilterGroup.CreateFromStrings(new string[]{p});
							r.Recursive = true;
							r.Next ();
						} else {
							r = tw;
						}
						
						do {
							// There can be more than one entry if reverting a whole directory
							string rpath = FromGitPath (r.PathString);
							DirCacheEntry e = new DirCacheEntry (r.PathString);
							e.SetObjectId (r.GetObjectId (0));
							e.FileMode = r.GetFileMode (0);
							if (!Directory.Exists (Path.GetDirectoryName (rpath)))
								Directory.CreateDirectory (rpath);
							DirCacheCheckout.CheckoutEntry (repo, rpath, e);
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
			monitor.BeginTask (null, localPaths.Length);

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
		
		bool IsSubpath (string basePath, string childPath)
		{
			if (basePath [basePath.Length - 1] == '/')
				return childPath.StartsWith (basePath);
			return childPath.StartsWith (basePath + "/");
		}

		public override void RevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			throw new System.NotImplementedException ();
		}


		public override void RevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			throw new System.NotImplementedException ();
		}


		public override void Add (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			NGit.Api.Git git = new NGit.Api.Git (repo);
			var cmd = git.Add ();
			foreach (FilePath fp in localPaths)
				cmd.AddFilepattern (ToGitPath (fp));
			cmd.Call ();
		}
		
		public override void DeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			NGit.Api.Git git = new NGit.Api.Git (repo);
			RmCommand rm = git.Rm ();
			foreach (var f in localPaths)
				rm.AddFilepattern (repo.ToGitPath (f));
			rm.Call ();
			
			// Untracked files are not deleted by the rm command, so delete them now
			foreach (var f in localPaths) {
				if (File.Exists (f))
					File.Delete (f);
			}
		}
		
		public override void DeleteDirectories (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			NGit.Api.Git git = new NGit.Api.Git (repo);
			RmCommand rm = git.Rm ();
			foreach (var f in localPaths)
				rm.AddFilepattern (repo.ToGitPath (f));
			rm.Call ();
			
			// Untracked files are not deleted by the rm command, so delete them now
			foreach (var f in localPaths) {
				if (Directory.Exists (f))
					Directory.Delete (f, true);
			}
		}

		public override string GetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			ObjectId id = repo.Resolve (revision.ToString ());
			RevWalk rw = new RevWalk (repo);
			RevCommit c = rw.ParseCommit (id);
			if (c == null)
				return string.Empty;
			else
				return GetCommitContent (c, repositoryPath);
		}

		public override DiffInfo[] PathDiff (FilePath baseLocalPath, FilePath[] localPaths, bool remoteDiff)
		{
			if (localPaths != null) {
				return new DiffInfo[0];
			} else {
				List<DiffInfo> diffs = new List<DiffInfo> ();
				VersionInfo[] vinfos = GetDirectoryVersionInfo (baseLocalPath, null, false, true);
				foreach (VersionInfo vi in vinfos) {
					try {
						if ((vi.Status & VersionStatus.ScheduledAdd) != 0) {
							string ctxt = File.ReadAllText (vi.LocalPath);
							diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff ("", ctxt)));
						} else if ((vi.Status & VersionStatus.ScheduledDelete) != 0) {
							string ctxt = GetCommitContent (GetHeadCommit (), vi.LocalPath);
							diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt, "")));
						} else if ((vi.Status & VersionStatus.Modified) != 0 || (vi.Status & VersionStatus.Conflicted) != 0) {
							string ctxt1 = GetCommitContent (GetHeadCommit (), vi.LocalPath);
							string ctxt2 = File.ReadAllText (vi.LocalPath);
							diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt1, ctxt2)));
						}
					} catch (Exception ex) {
						LoggingService.LogError ("Could not get diff for file '" + vi.LocalPath + "'", ex);
					}
				}
				return diffs.ToArray ();
			}
		}

		string GetCommitContent (RevCommit c, FilePath file)
		{
			TreeWalk tw = TreeWalk.ForPath (repo, ToGitPath (file), c.Tree);
			if (tw == null)
				return string.Empty;
			ObjectId id = tw.GetObjectId (0);
			byte[] data = repo.ObjectDatabase.Open (id).GetBytes ();
			return Encoding.UTF8.GetString (data);
		}

		string GenerateDiff (string s1, string s2)
		{
			var text1 = new NGit.Diff.RawText (Encoding.UTF8.GetBytes (s1));
			var text2 = new NGit.Diff.RawText (Encoding.UTF8.GetBytes (s2));
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
						string newFile = path.Combine (line.Substring (6));
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
			RemoteConfig remoteConfig = new RemoteConfig (repo.GetConfig (), remote);
			Transport tp = Transport.Open (repo, remoteConfig);
			
			string remoteRef = "refs/heads/" + remoteBranch;
			
			RemoteRefUpdate rr = new RemoteRefUpdate (repo, repo.GetBranch (), remoteRef, false, null, null);
			List<RemoteRefUpdate> list = new List<RemoteRefUpdate> ();
			list.Add (rr);
			PushResult res = tp.Push (new GitMonitor (monitor), list);
			switch (rr.GetStatus ()) {
			case RemoteRefUpdate.Status.UP_TO_DATE: monitor.ReportSuccess (GettextCatalog.GetString ("Remote branch is up to date.")); break;
			case RemoteRefUpdate.Status.REJECTED_NODELETE: monitor.ReportError (GettextCatalog.GetString ("The server is configured to deny deletion of the branch"), null); break;
			case RemoteRefUpdate.Status.REJECTED_NONFASTFORWARD: monitor.ReportError (GettextCatalog.GetString ("The update is a non-fast-forward update. Merge the remote changes before pushing again."), null); break;
			case RemoteRefUpdate.Status.OK:
				monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
				// Update the remote branch
				ObjectId headId = rr.GetNewObjectId ();
				RefUpdate updateRef = repo.UpdateRef (Constants.R_REMOTES + remote + "/" + remoteBranch);
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
			ObjectId headId = repo.Resolve (Constants.HEAD);
			RefUpdate updateRef = repo.UpdateRef ("refs/heads/" + name);
			updateRef.SetNewObjectId(headId);
			updateRef.Update();
			GitUtil.SetUpstreamSource (repo, name, trackSource);
		}

		public void SetBranchTrackSource (string name, string trackSource)
		{
			GitUtil.SetUpstreamSource (repo, name, trackSource);
		}

		public void RemoveBranch (string name)
		{
			RefUpdate updateRef = repo.UpdateRef ("refs/heads/" + name);
			updateRef.Delete ();
		}

		public void RenameBranch (string name, string newName)
		{
			RefRename renameRef = repo.RenameRef ("refs/heads/" + name, "refs/heads/" + newName);
			renameRef.Rename ();
		}

		public IEnumerable<RemoteSource> GetRemotes ()
		{
			StoredConfig cfg = repo.GetConfig ();
			foreach (RemoteConfig rc in RemoteConfig.GetAllRemoteConfigs (cfg))
				yield return new RemoteSource (cfg, rc);
		}

		public void RenameRemote (string name, string newName)
		{
			StoredConfig cfg = repo.GetConfig ();
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
			
			StoredConfig c = repo.GetConfig ();
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
			StoredConfig cfg = repo.GetConfig ();
			RemoteConfig rem = RemoteConfig.GetAllRemoteConfigs (cfg).FirstOrDefault (r => r.Name == name);
			if (rem != null) {
				cfg.UnsetSection ("remote", name);
				cfg.Save ();
			}
		}

		public IEnumerable<Branch> GetBranches ()
		{
			IDictionary<string, NGit.Ref> refs = repo.RefDatabase.GetRefs (Constants.R_HEADS);
			foreach (var pair in refs) {
				string name = NGit.Repository.ShortenRefName (pair.Key);
				Branch br = new Branch ();
				br.Name = name;
				br.Tracking = GitUtil.GetUpstreamSource (repo, name);
				yield return br;
			}
		}

		public IEnumerable<string> GetTags ()
		{
			return repo.GetTags ().Keys;
		}

		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			var refs = repo.RefDatabase.GetRefs (Constants.R_REMOTES);
			foreach (var pair in refs) {
				string name = NGit.Repository.ShortenRefName (pair.Key);
				if (name.StartsWith (remoteName + "/"))
					yield return name.Substring (remoteName.Length + 1);
			}
		}

		public string GetCurrentBranch ()
		{
			return repo.GetBranch ();
		}

		public void SwitchToBranch (IProgressMonitor monitor, string branch)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), 4);
			
			StashCollection stashes = GitUtil.GetStashes (repo);
			
			// Remove the stash for this branch, if exists
			string currentBranch = GetCurrentBranch ();
			Stash stash = GetStashForBranch (stashes, currentBranch);
			if (stash != null)
				stashes.Remove (stash);
			
			// Get a list of files that are different in the target branch
			IEnumerable<Change> statusList = GitUtil.GetChangedFiles (repo, branch);
			
			// Create a new stash for the branch. This allows switching branches
			// without losing local changes
			stash = stashes.Create (GetStashName (currentBranch));
			
			monitor.Step (1);
			
			// Switch to the target branch
			DirCache dc = repo.LockDirCache ();
			try {
				RevWalk rw = new RevWalk (repo);
				ObjectId branchHeadId = repo.Resolve (branch);
				if (branchHeadId == null)
					throw new InvalidOperationException ("Branch head commit not found");
				
				RevCommit branchCommit = rw.ParseCommit (branchHeadId);
				DirCacheCheckout checkout = new DirCacheCheckout (repo, null, dc, branchCommit.Tree);
				checkout.Checkout ();
				
				RefUpdate u = repo.UpdateRef(Constants.HEAD);
				u.Link ("refs/heads/" + branch);
				monitor.Step (1);
			} catch {
				dc.Unlock ();
				// If something goes wrong, restore the work tree status
				stash.Apply ();
				stashes.Remove (stash);
				throw;
			}
			
			// Restore the branch stash
			
			stash = GetStashForBranch (stashes, branch);
			if (stash != null) {
				stash.Apply ();
				stashes.Remove (stash);
			}
			monitor.Step (1);
			
			// Notify file changes
			
			NotifyFileChanges (monitor, statusList);
			
			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);
			
			monitor.EndTask ();
		}

		void NotifyFileChanges (IProgressMonitor monitor, IEnumerable<Change> statusList)
		{
			List<Change> changes = new List<Change> (statusList);
			monitor.BeginTask (null, changes.Count);
			foreach (Change change in changes) {
				if (change.ChangeType == ChangeType.Added)
					// File added to source branch not present to target branch.
					FileService.NotifyFileRemoved (FromGitPath (change.Path));
				else
					FileService.NotifyFileChanged (FromGitPath (change.Path));
				monitor.Step (1);
			}
			monitor.EndTask ();
		}

		string GetStashName (string branchName)
		{
			return "__MD_" + branchName;
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
			ChangeSet cset = CreateChangeSet (path);
			ObjectId cid1 = repo.Resolve (remote + "/" + branch);
			ObjectId cid2 = repo.Resolve (repo.GetBranch ());
			RevWalk rw = new RevWalk (repo);
			RevCommit c1 = rw.ParseCommit (cid1);
			RevCommit c2 = rw.ParseCommit (cid2);
			
			foreach (Change change in GitUtil.CompareCommits (repo, c2, c1)) {
				VersionStatus status;
				switch (change.ChangeType) {
				case ChangeType.Added:
					status = VersionStatus.ScheduledAdd;
					break;
				case ChangeType.Deleted:
					status = VersionStatus.ScheduledDelete;
					break;
				default:
					status = VersionStatus.Modified;
					break;
				}
				VersionInfo vi = new VersionInfo (FromGitPath (change.Path), "", false, status | VersionStatus.Versioned, null, VersionStatus.Versioned, null);
				cset.AddFile (vi);
			}
			return cset;
		}
		
		FilePath FromGitPath (string filePath)
		{
			filePath = filePath.Replace ('/', Path.DirectorySeparatorChar);
			return path.Combine (filePath);
		}

		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			ObjectId cid1 = repo.Resolve (remote + "/" + branch);
			ObjectId cid2 = repo.Resolve (repo.GetBranch ());
			RevWalk rw = new RevWalk (repo);
			RevCommit c1 = rw.ParseCommit (cid1);
			RevCommit c2 = rw.ParseCommit (cid2);
			
			List<DiffInfo> diffs = new List<DiffInfo> ();
			foreach (Change change in GitUtil.CompareCommits (repo, c1, c2)) {
				string diff;
				switch (change.ChangeType) {
				case ChangeType.Deleted:
					diff = GenerateDiff ("", GetCommitContent (c2, change.Path));
					break;
				case ChangeType.Added:
					diff = GenerateDiff (GetCommitContent (c1, change.Path), "");
					break;
				default:
					diff = GenerateDiff (GetCommitContent (c1, change.Path), GetCommitContent (c2, change.Path));
					break;
				}
				DiffInfo di = new DiffInfo (path, FromGitPath (change.Path), diff);
				diffs.Add (di);
			}
			return diffs.ToArray ();
		}

		public override void MoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			VersionInfo vi = GetVersionInfo (localSrcPath, false);
			if (vi == null || !vi.IsVersioned) {
				base.MoveFile (localSrcPath, localDestPath, force, monitor);
				return;
			}
			base.MoveFile (localSrcPath, localDestPath, force, monitor);
			Add (localDestPath, false, monitor);
			
			if ((vi.Status & VersionStatus.ScheduledAdd) != 0)
				Revert (localSrcPath, false, monitor);
		}

		public override void MoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			VersionInfo[] versionedFiles = GetDirectoryVersionInfo (localSrcPath, false, true);
			base.MoveDirectory (localSrcPath, localDestPath, force, monitor);
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

		public override bool CanGetAnnotations (FilePath localPath)
		{
			return true;
		}

		public override Annotation[] GetAnnotations (FilePath repositoryPath)
		{
			RevCommit hc = GetHeadCommit ();
			if (hc == null)
				return new Annotation [0];
			RevCommit[] lineCommits = GitUtil.Blame (repo, hc, repositoryPath);
			int lineCount = lineCommits.Length;
			List<Annotation> annotations = new List<Annotation>(lineCount);
			for (int n = 0; n < lineCount; n++) {
				RevCommit c = lineCommits[n];
				Annotation annotation = new Annotation (c.Name, c.GetAuthorIdent ().GetName () + "<" + c.GetAuthorIdent ().GetEmailAddress () + ">", c.GetCommitterIdent ().GetWhen ());
				annotations.Add(annotation);
			}
			
			Document baseDocument = new Document (GetCommitContent (hc, repositoryPath));
			Document workingDocument = new Document (File.ReadAllText (repositoryPath));
			Annotation uncommittedRev = new Annotation("0000000000000000000000000000000000000000", "", new DateTime());
			
			// Based on Subversion code until we support blame on things other than commits
			foreach (var hunk in baseDocument.Diff (workingDocument)) {
				annotations.RemoveRange (hunk.RemoveStart, hunk.Removed);
				int start = hunk.InsertStart - 1; //Line number - 1 = index
				bool insert = (start < annotations.Count);
					
				for (int i = 0; i < hunk.Inserted; ++i) {
					if (insert) {
						annotations.Insert (start, uncommittedRev);
					} else {
						annotations.Add (uncommittedRev);
					}
				}
			}
			
			return annotations.ToArray();
		}
		
		internal GitRevision GetPreviousRevisionFor (GitRevision revision)
		{
			ObjectId id = repo.Resolve (revision.ToString () + "^");
			if (id == null)
				return null;
			return new GitRevision (this, id.Name);
		}
	}
	
	public class GitRevision: Revision
	{
		string rev;

		public GitRevision (Repository repo, string rev) : base(repo)
		{
			this.rev = rev;
		}

		public GitRevision (Repository repo, string rev, DateTime time, string author, string message, RevisionPath[] changedFiles) : base(repo, time, author, message, changedFiles)
		{
			this.rev = rev;
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
	
	class GitMonitor: NGit.ProgressMonitor
	{
		IProgressMonitor monitor;
		
		public GitMonitor (IProgressMonitor monitor)
		{
			this.monitor = monitor;
		}
		
		public override void Start (int totalTasks)
		{
			monitor.BeginTask (null, totalTasks);
		}
		
		
		public override void BeginTask (string title, int totalWork)
		{
			monitor.BeginTask (title, totalWork);
		}
		
		
		public override void Update (int completed)
		{
			monitor.Step (completed);
		}
		
		
		public override void EndTask ()
		{
			monitor.EndTask ();
		}
		
		
		public override bool IsCancelled ()
		{
			return monitor.IsCancelRequested;
		}
	}
}

