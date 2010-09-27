// 
// GitRepository.cs
//  
// Author:
//       Dale Ragan <dale.ragan@sinesignal.com>
// 
// Copyright (c) 2010 SineSignal, LLC
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

namespace MonoDevelop.VersionControl.Git
{
	public class GitRepository : UrlBasedRepository
	{
		NGit.Repository repo;
		FilePath path;

		public static event EventHandler BranchSelectionChanged;

		public GitRepository ()
		{
			Method = "git";
		}

		public GitRepository (FilePath path, string url)
		{
			this.path = path;
			Url = url;
			repo = new FileRepository (path.Combine (Constants.DOT_GIT));
		}

		public FilePath RootPath {
			get { return path; }
		}

		public override void CopyConfigurationFrom (Repository other)
		{
			GitRepository r = (GitRepository)other;
			path = r.path;
			Url = r.Url;
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
			walk.MarkStart (GetHeadCommit ());
			string filePath = ToGitPath (localFile);
			
			foreach (RevCommit commit in walk) {
				List<RevisionPath> paths = new List<RevisionPath> ();
				foreach (Change change in GetCommitChanges (commit)) {
					if (change.Path != filePath)
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
					RevisionPath p = new RevisionPath (path.Combine (change.Path), ra, null);
					paths.Add (p);
				}
				PersonIdent author = commit.GetAuthorIdent ();
				revs.Add (new GitRevision (this, commit.Id.Name, author.GetWhen().ToLocalTime (), author.GetName (), commit.GetShortMessage (), paths.ToArray ()));
			}
			return revs.ToArray ();
		}

		IEnumerable<Change> GetCommitChanges (RevCommit commit)
		{
			var treeIds = new[] { commit.Tree.Id }.Concat (commit.Parents.Select (c => c.Tree.Id)).ToArray ();
			var walk = new TreeWalk (repo);
			walk.Reset (treeIds);
			walk.Recursive = true;
			walk.Filter = AndTreeFilter.Create (AndTreeFilter.ANY_DIFF, AndTreeFilter.ALL);
			
			return CalculateCommitDiff (walk, new[] { commit }.Concat (commit.Parents).ToArray ());
		}

		private static IEnumerable<Change> CalculateCommitDiff (TreeWalk walk, RevCommit[] commits)
		{
			while (walk.Next ()) {
				int m0 = walk.GetRawMode (0);
				if (walk.TreeCount == 2) {
					int m1 = walk.GetRawMode (1);
					var change = new Change { ReferenceCommit = commits[0], ComparedCommit = commits[1], ReferencePermissions = walk.GetFileMode (0).GetBits (), ComparedPermissions = walk.GetFileMode (1).GetBits (), Name = walk.NameString, Path = walk.PathString };
					if (m0 != 0 && m1 == 0) {
						change.ChangeType = ChangeType.Added;
						change.ComparedObject = walk.GetObjectId (0);
					} else if (m0 == 0 && m1 != 0) {
						change.ChangeType = ChangeType.Deleted;
						change.ReferenceObject = walk.GetObjectId (0);
					} else if (m0 != m1 && walk.IdEqual (0, 1)) {
						change.ChangeType = ChangeType.TypeChanged;
						change.ReferenceObject = walk.GetObjectId (0);
						change.ComparedObject = walk.GetObjectId (1);
					} else {
						change.ChangeType = ChangeType.Modified;
						change.ReferenceObject = walk.GetObjectId (0);
						change.ComparedObject = walk.GetObjectId (1);
					}
					yield return change;
				} else {
					var raw_modes = new int[walk.TreeCount - 1];
					for (int i = 0; i < walk.TreeCount - 1; i++)
						raw_modes[i] = walk.GetRawMode (i + 1);
					//ComparedCommit = compared,
					var change = new Change { ReferenceCommit = commits[0], Name = walk.NameString, Path = walk.PathString };
					if (m0 != 0 && raw_modes.All (m1 => m1 == 0)) {
						change.ChangeType = ChangeType.Added;
						change.ComparedObject = walk.GetObjectId (0);
						yield return change;
					} else if (m0 == 0 && raw_modes.Any (m1 => m1 != 0)) {
						change.ChangeType = ChangeType.Deleted;
						yield return change;
					// TODO: not sure if this condition suffices in some special cases.
					} else if (raw_modes.Select ((m1, i) => new { Mode = m1, Index = i + 1 }).All (x => !walk.IdEqual (0, x.Index))) {
						change.ChangeType = ChangeType.Modified;
						change.ReferenceObject = walk.GetObjectId (0);
						yield return change;
					} else if (raw_modes.Select ((m1, i) => new { Mode = m1, Index = i + 1 }).Any (x => m0 != x.Mode && walk.IdEqual (0, x.Index))) {
						change.ChangeType = ChangeType.TypeChanged;
						change.ReferenceObject = walk.GetObjectId (0);
						yield return change;
					}
				}
			}
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

		StringReader RunCommand (string cmd, bool checkExitCode)
		{
			return RunCommand (cmd, checkExitCode, null);
		}

		StringReader RunCommand (string cmd, bool checkExitCode, IProgressMonitor monitor)
		{
			#if DEBUG_GIT
			Console.WriteLine ("> git " + cmd);
			#endif
			var psi = new System.Diagnostics.ProcessStartInfo (GitVersionControl.GitExe, "--no-pager " + cmd) { WorkingDirectory = path, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = false, UseShellExecute = false };
			if (PropertyService.IsWindows) {
				//msysgit needs this to read global config
				psi.EnvironmentVariables["HOME"] = Environment.GetEnvironmentVariable ("USERPROFILE");
			}
			
			StringWriter outw = new StringWriter ();
			ProcessWrapper proc = Runtime.ProcessService.StartProcess (psi, outw, outw, null);
			proc.WaitForOutput ();
			if (monitor != null)
				monitor.Log.Write (outw.ToString ());
			#if DEBUG_GIT
			Console.WriteLine (outw.ToString ());
			#endif
			if (checkExitCode && proc.ExitCode != 0)
				throw new InvalidOperationException ("Git operation failed");
			return new StringReader (outw.ToString ());
		}

		List<string> ToList (StringReader sr)
		{
			List<string> list = new List<string> ();
			string line;
			while ((line = sr.ReadLine ()) != null)
				list.Add (line);
			return list;
		}

		public override VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			return GetDirectoryVersionInfo (localDirectory, null, getRemoteStatus, recursive);
		}

		string ToGitPath (FilePath filePath)
		{
			return filePath.FullPath.ToRelative (path).ToString ().Replace ('\\', '/');
		}

		string ToCmdPath (FilePath filePath)
		{
			return "\"" + filePath.ToRelative (path) + "\"";
		}

		string ToCmdPathList (IEnumerable<FilePath> paths)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (FilePath it in paths)
				sb.Append (' ').Append (ToCmdPath (it));
			return sb.ToString ();
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
			
			GitRevision rev = new GitRevision (this, GetHeadCommit ().Id.Name);
			List<VersionInfo> versions = new List<VersionInfo> ();
			FilePath p = fileName != null ? localDirectory.Combine (fileName) : localDirectory;
			p = p.CanonicalPath;
			
			RepositoryStatus status;
			if (fileName != null)
				status = new RepositoryStatus (repo, ToGitPath (p), null, false);
			else
				status = new RepositoryStatus (repo, null, ToGitPath (localDirectory), recursive);
			
			Action<IEnumerable<string>, VersionStatus> AddFiles = delegate(IEnumerable<string> files, VersionStatus fstatus) {
				foreach (string file in files) {
					FilePath statFile = FromGitPath (file);
					existingFiles.Remove (statFile.CanonicalPath);
					VersionInfo vi = new VersionInfo (statFile, "", false, fstatus, rev, VersionStatus.Versioned, null);
					versions.Add (vi);
				}
			};
			
			AddFiles (status.Added, VersionStatus.Versioned | VersionStatus.ScheduledAdd);
			AddFiles (status.Modified, VersionStatus.Versioned | VersionStatus.Modified);
			AddFiles (status.Removed, VersionStatus.Versioned | VersionStatus.ScheduledDelete);
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
			throw new System.NotImplementedException ();
		}

		public override void Update (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			// TODO needs ssh
			
			List<string> statusList = null;
			
			try {
				// Fetch remote commits
				RunCommand ("fetch " + GetCurrentRemote (), true, monitor);
				
				// Get a list of files that are different in the target branch
				statusList = ToList (RunCommand ("diff " + GetCurrentRemote () + "/" + GetCurrentBranch () + " --name-status", true));
				
				// Save local changes
				RunCommand ("stash save " + GetStashName ("_tmp_"), true);
				
				// Apply changes
				StringReader sr = RunCommand ("rebase " + GetCurrentRemote () + " " + GetCurrentBranch (), false, monitor);
				string conflictFile = null;
				do {
					conflictFile = GetConflictFiles (sr).FirstOrDefault ();
					if (conflictFile != null) {
						ConflictResult res = ResolveConflict (conflictFile);
						if (res == ConflictResult.Abort) {
							sr = RunCommand ("rebase --abort", false, monitor);
							break;
						} else if (res == ConflictResult.Skip)
							sr = RunCommand ("rebase --skip", false, monitor);
						else {
							RunCommand ("add " + ToCmdPath (conflictFile), false, monitor);
							sr = RunCommand ("rebase --continue", false, monitor);
						}
					}
				} while (conflictFile != null);
				
			} finally {
				// Restore local changes
				string sid = GetStashId ("_tmp_");
				if (sid != null)
					RunCommand ("stash pop " + sid, false);
			}
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (statusList);
		}

		public void Merge (string branch, IProgressMonitor monitor)
		{
			List<string> statusList = null;
			
			try {
				// Get a list of files that are different in the target branch
				statusList = ToList (RunCommand ("diff " + branch + " --name-status", true));
				
				// Save local changes
				RunCommand ("stash save " + GetStashName ("_tmp_"), true);
				
				// Apply changes
				StringReader sr = RunCommand ("merge " + branch, false, monitor);
				
				var conflicts = GetConflictFiles (sr);
				if (conflicts.Count != 0) {
					foreach (string conflictFile in conflicts) {
						ConflictResult res = ResolveConflict (conflictFile);
						if (res == ConflictResult.Abort) {
							sr = RunCommand ("reset --merge", false, monitor);
							return;
						} else if (res == ConflictResult.Skip) {
							RunCommand ("reset " + ToCmdPath (conflictFile), false, monitor);
							RunCommand ("reset " + ToCmdPath (conflictFile), false, monitor);
							RunCommand ("checkout " + ToCmdPath (conflictFile), false, monitor);
						} else {
							// Do nothing. The change will be committed with the -a option
						}
					}
					string msg = "Merge branch '" + branch + "'";
					RunCommand ("commit -a -m \"" + msg + "\"", true, monitor);
				}
				
			} finally {
				// Restore local changes
				string sid = GetStashId ("_tmp_");
				if (sid != null)
					RunCommand ("stash pop " + sid, false);
			}
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (statusList);
		}

		List<string> GetConflictFiles (StringReader reader)
		{
			List<string> list = new List<string> ();
			foreach (string line in ToList (reader)) {
				if (line.StartsWith ("CONFLICT ")) {
					string s = "Merge conflict in ";
					int i = line.IndexOf (s) + s.Length;
					list.Add (path.Combine (line.Substring (i)));
				}
			}
			return list;
		}

		ConflictResult ResolveConflict (string file)
		{
			ConflictResult res = ConflictResult.Abort;
			DispatchService.GuiSyncDispatch (delegate {
				ConflictResolutionDialog dlg = new ConflictResolutionDialog ();
				dlg.Load (file);
				Gtk.ResponseType dres = (Gtk.ResponseType)dlg.Run ();
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
					
					// Create a Commit object, populate it and write it
					NGit.CommitBuilder commit = new NGit.CommitBuilder ();
					commit.Committer = committer;
					commit.Author = author;
					commit.Message = message;
					commit.SetParentIds (parents);
					commit.TreeId = indexTreeId;
					ObjectId commitId = odi.Insert (commit);
					odi.Flush ();
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
			NGit.Tree tree = repo.MapTree (GetHeadCommit ());

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
			FileTreeIterator iter = new FileTreeIterator (dir.FullName, repo.FileSystem, WorkingTreeOptions.CreateConfigurationInstance(repo.GetConfig()));
			while (!iter.Eof ()) {
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
			// TODO
			//GitSharp.Git.Clone (Url, targetLocalPath);
		}

		public override void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			// TODO verify it works
			
			GitIndex index = repo.GetIndex ();
			index.RereadIfNecessary ();
			RevWalk rw = new RevWalk (repo);
			var c = GetHeadCommit ();
			RevTree tree = c.Tree;
			
			foreach (FilePath fp in localPaths) {
				string p = ToGitPath (fp);
				TreeWalk tw = TreeWalk.ForPath (repo, p, tree);
				RevBlob blob = rw.LookupBlob (tw.GetObjectId (0));
				if (blob == null)
					index.Remove (repo.WorkTree, p);
				else {
					MemoryStream ms = new MemoryStream ();
					blob.CopyRawTo (ms);
					index.Add (repo.WorkTree, p, ms.ToArray ());
					index.Write ();
				}
			}

			foreach (FilePath p in localPaths)
				FileService.NotifyFileChanged (p);
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

		public override string GetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			// TODO verify it works
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
					if ((vi.Status & VersionStatus.ScheduledAdd) != 0) {
						string ctxt = File.ReadAllText (vi.LocalPath);
						diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff ("", ctxt)));
					} else if ((vi.Status & VersionStatus.ScheduledDelete) != 0) {
						string ctxt = GetCommitContent (GetHeadCommit (), vi.LocalPath);
						diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt, "")));
					} else if ((vi.Status & VersionStatus.Modified) != 0) {
						string ctxt1 = GetCommitContent (GetHeadCommit (), vi.LocalPath);
						string ctxt2 = File.ReadAllText (vi.LocalPath);
						diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt1, ctxt2)));
					}
				}
				return diffs.ToArray ();
			}
		}

		string GetCommitContent (RevCommit c, FilePath file)
		{
			TreeWalk tw = TreeWalk.ForPath (repo, ToGitPath (file), c.Tree);
			ObjectId id = tw.GetObjectId (0);
			byte[] data = repo.ObjectDatabase.Open (id).GetBytes ();
			return Encoding.UTF8.GetString (data);
		}

		string GenerateDiff (string s1, string s2)
		{
			var text1 = new NGit.Diff.RawText (Encoding.UTF8.GetBytes (s1));
			var text2 = new NGit.Diff.RawText (Encoding.UTF8.GetBytes (s2));
			var edits = new NGit.Diff.MyersDiff (text1, text2).GetEdits ();
			MemoryStream s = new MemoryStream ();
			var formatter = new NGit.Diff.DiffFormatter (s);
			formatter.FormatEdits (text1, text2, edits);
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
				throw new InvalidOperationException ("There are no remote repositories defined");
			
			if (remotes.Contains ("origin"))
				return "origin";
			else
				return remotes[0];
		}

		public void Push (IProgressMonitor monitor, string remote, string remoteBranch)
		{
			// TODO needs ssh
			RunCommand ("push " + remote + " HEAD:" + remoteBranch, true, monitor);
			monitor.ReportSuccess ("Repository successfully pushed");
		}

		public void CreateBranch (string name, string trackSource)
		{
			ObjectId headId = repo.Resolve (Constants.HEAD);
			RefUpdate updateRef = repo.UpdateRef ("refs/heads/" + name);
			updateRef.SetNewObjectId(headId);
			updateRef.Update();
			
			// TODO
/*			GitSharp.Branch b = GitSharp.Branch.Create (repo, name);
			b.UpstreamSource = trackSource;
			repo.Config.Persist ();*/
		}

		public void SetBranchTrackSource (string name, string trackSource)
		{
			// TODO
/*			GitSharp.Branch b;
			if (repo.Branches.TryGetValue (name, out b)) {
				b.UpstreamSource = trackSource;
				repo.Config.Persist ();
			}*/
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
			UpdateRemote (remote);
		}

		public void UpdateRemote (RemoteSource remote)
		{
			if (string.IsNullOrEmpty (remote.FetchUrl))
				throw new InvalidOperationException ("Fetch url can't be empty");
			
			if (remote.RepoRemote == null)
				throw new InvalidOperationException ("Remote not created");
			
			remote.Update ();
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
				string name = repo.ShortenRefName (pair.Key);
				Branch br = new Branch ();
				br.Name = name;
				// TODO
//				br.Tracking = b.Value.UpstreamSource;
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
				string name = repo.ShortenRefName (pair.Key);
				if (name.StartsWith (remoteName + "/"))
					yield return name.Substring (remoteName.Length + 1);
			}
		}

		public string GetCurrentBranch ()
		{
			return repo.GetBranch ();
		}

		public void SwitchToBranch (string branch)
		{
			// TODO Stash!
			
			// Remove the stash for this branch, if exists
			string currentBranch = GetCurrentBranch ();
			string sid = GetStashId (currentBranch);
			if (sid != null)
				RunCommand ("stash drop " + sid, true);
			
			// Get a list of files that are different in the target branch
			var statusList = ToList (RunCommand ("diff " + branch + " --name-status", true));
			
			// Create a new stash for the branch. This allows switching branches
			// without losing local changes
			RunCommand ("stash save " + GetStashName (currentBranch), true);
			
			// Switch to the target branch
			try {
				RunCommand ("checkout " + branch, true);
			} catch {
				// If something goes wrong, restore the work tree status
				RunCommand ("stash pop", true);
				throw;
			}
			
			// Restore the branch stash
			
			sid = GetStashId (branch);
			if (sid != null)
				RunCommand ("stash pop " + sid, true);
			
			// Notify file changes
			
			NotifyFileChanges (statusList);
			
			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);
		}

		void NotifyFileChanges (List<string> statusList)
		{
			foreach (string line in statusList) {
				char s = line[0];
				FilePath file = path.Combine (line.Substring (2));
				if (s == 'A')
					// File added to source branch not present to target branch.
					FileService.NotifyFileRemoved (file);
				else
					FileService.NotifyFileChanged (file);
			}
		}

		string GetStashName (string branchName)
		{
			return "__MD_" + branchName;
		}

		string GetStashId (string branchName)
		{
			string sn = GetStashName (branchName);
			foreach (string ss in ToList (RunCommand ("stash list", true))) {
				if (ss.IndexOf (sn) != -1) {
					int i = ss.IndexOf (':');
					return ss.Substring (0, i);
				}
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
			
			foreach (Change change in CompareCommits (c1, c2)) {
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
		
		IEnumerable<Change> CompareCommits (RevCommit reference, RevCommit compared)
		{
			var changes = new List<Change>();
			if (reference == null && compared == null)
				return changes;
			ObjectId refTree = (reference != null ? reference.Tree.Id : ObjectId.ZeroId);
			ObjectId comparedTree = (compared != null ? compared.Tree.Id : ObjectId.ZeroId);
			var walk = new TreeWalk (repo);
			if (reference == null || compared == null)
				walk.Reset ((reference ?? compared).Tree.Id);
			else
				walk.Reset (new AnyObjectId[] {refTree, comparedTree});
			walk.Recursive = true;
			walk.Filter = AndTreeFilter.Create(TreeFilter.ANY_DIFF, TreeFilter.ALL);

			return CalculateCommitDiff (walk, new[] { reference, compared });
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
			foreach (Change change in CompareCommits (c1, c2)) {
				string diff;
				switch (change.ChangeType) {
				case ChangeType.Added:
					diff = GenerateDiff ("", GetCommitContent (c2, change.Path));
					break;
				case ChangeType.Deleted:
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
			// TODO move command
			if (!IsVersioned (localSrcPath)) {
				base.MoveFile (localSrcPath, localDestPath, force, monitor);
				return;
			}
			RunCommand ("mv " + ToCmdPath (localSrcPath) + " " + ToCmdPath (localDestPath), true, monitor);
		}

		public override void MoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			// TODO move command
			try {
				RunCommand ("mv " + ToCmdPath (localSrcPath) + " " + ToCmdPath (localDestPath), true, monitor);
			} catch {
				// If the move can't be done using git, do a regular move
				base.MoveDirectory (localSrcPath, localDestPath, force, monitor);
			}
		}

		public override bool CanGetAnnotations (FilePath localPath)
		{
			return true;
		}

		public override Annotation[] GetAnnotations (FilePath repositoryPath)
		{
			// TODO
/*			Commit[] lineCommits = repo.CurrentBranch.CurrentCommit.Blame (ToGitPath (repositoryPath));
			Annotation[] lines = new Annotation[lineCommits.Length];
			for (int n = 0; n < lines.Length; n++) {
				Commit c = lineCommits[n];
				lines[n] = new Annotation (c.Hash, c.Author.Name + "<" + c.Author.EmailAddress + ">", c.CommitDate.DateTime);
			}
			return lines;*/
			throw new NotImplementedException ();
			
		}
			/*
			List<Annotation> alist = new List<Annotation> ();
			StringReader sr = RunCommand ("blame -p " + ToCmdPath (repositoryPath), true);
			
			string author = null;
			string mail = null;
			string date = null;
			string tz = null;
				
			
			string line;
			while ((line = sr.ReadLine ()) != null) {
				string[] header = line.Split (' ');
				string rev = header[0];
				int lcount = int.Parse (header[3]);
				
				line = sr.ReadLine ();
				while (line != null && line.Length > 0 && line[0] != '\t') {
					int i = line.IndexOf (' ');
					string val;
					string field;
					if (i != -1) {
						val = line.Substring (i + 1);
						field = line.Substring (0, i);
					} else {
						val = null;
						field = line;
					}
					switch (field) {
					case "author": author = val; break;
					case "author-mail": mail = val; break;
					case "author-time": date = val; break;
					case "author-tz": tz = val; break;
					}
					line = sr.ReadLine ();
				}
				
				// Convert from git date format
				double secs = double.Parse (date);
				DateTime t = new DateTime (1970, 1, 1) + TimeSpan.FromSeconds (secs);
				string st = t.ToString ("yyyy-MM-ddTHH:mm:ss") + tz.Substring (0, 3) + ":" + tz.Substring (3);
				DateTime sdate = DateTime.Parse (st);
				
				string sauthor = author;
				if (!string.IsNullOrEmpty (mail))
					sauthor += " " + mail;
				Annotation a = new Annotation (rev, sauthor, sdate);
				while (lcount-- > 0) {
					alist.Add (a);
					if (lcount > 0) {
						sr.ReadLine (); // Next header
						sr.ReadLine (); // Next line content
					}
				}
			}
			return alist.ToArray ();
			*/			
			}

	class GitRevision : Revision
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

		public override Revision GetPrevious ()
		{
			throw new System.NotImplementedException ();
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
		Config cfg;

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
}

