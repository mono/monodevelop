// 
// GitCommands.cs
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
using System;
using System.IO;
using System.Linq;
using NGit;
using System.Collections.Generic;
using MonoDevelop.Core;
using NGit.Errors;
using NGit.Api.Errors;
using NGit.Dircache;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Api;
using NGit.Merge;
using NGit.Storage.File;
using NGit.Transport;

namespace MonoDevelop.VersionControl.Git
{
	public static class GitUtil
	{
		public static string ToGitPath (this NGit.Repository repo, FilePath filePath)
		{
			return filePath.FullPath.ToRelative (repo.WorkTree.ToString ()).ToString ().Replace ('\\', '/');
		}

		public static FilePath FromGitPath (this NGit.Repository repo, string filePath)
		{
			filePath = filePath.Replace ('/', Path.DirectorySeparatorChar);
			return Path.Combine (repo.WorkTree, filePath);
		}
		
		/// <summary>
		/// Compares two commits and returns a list of files that have changed
		/// </summary>
		public static IEnumerable<Change> CompareCommits (NGit.Repository repo, RevCommit reference, RevCommit compared)
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

			return CalculateCommitDiff (repo, walk, new[] { reference, compared });
		}
		
		/// <summary>
		/// Returns a list of files that have changed in a commit
		/// </summary>
		public static IEnumerable<Change> GetCommitChanges (NGit.Repository repo, RevCommit commit)
		{
			var treeIds = new[] { commit.Tree.Id }.Concat (commit.Parents.Select (c => c.Tree.Id)).ToArray ();
			var walk = new TreeWalk (repo);
			walk.Reset (treeIds);
			walk.Recursive = true;
			walk.Filter = AndTreeFilter.Create (AndTreeFilter.ANY_DIFF, AndTreeFilter.ALL);
			
			return CalculateCommitDiff (repo, walk, new[] { commit }.Concat (commit.Parents).ToArray ());
		}

		static IEnumerable<Change> CalculateCommitDiff (NGit.Repository repo, TreeWalk walk, RevCommit[] commits)
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
		
		public static RepositoryStatus GetDirectoryStatus (NGit.Repository repo, string dir, bool recursive)
		{
			return new RepositoryStatus (repo, null, repo.ToGitPath (dir), recursive);
		}
		
		public static RepositoryStatus GetFileStatus (NGit.Repository repo, string fileName)
		{
			return new RepositoryStatus (repo, repo.ToGitPath (fileName), null, false);
		}
		
		public static ObjectId CreateCommit (NGit.Repository rep, string message, IList<ObjectId> parents, ObjectId indexTreeId, PersonIdent author, PersonIdent committer)
		{
			try {
				ObjectInserter odi = rep.NewObjectInserter ();
				try {
					// Create a Commit object, populate it and write it
					NGit.CommitBuilder commit = new NGit.CommitBuilder ();
					commit.Committer = committer;
					commit.Author = author;
					commit.Message = message;
					commit.SetParentIds (parents);
					commit.TreeId = indexTreeId;
					ObjectId commitId = odi.Insert (commit);
					odi.Flush ();
					return commitId;
				} finally {
					odi.Release ();
				}
			} catch (UnmergedPathException) {
				// since UnmergedPathException is a subclass of IOException
				// which should not be wrapped by a JGitInternalException we
				// have to catch and re-throw it here
				throw;
			} catch (IOException e) {
				throw new JGitInternalException ("Commit failed", e);
			}
		}
		
		public static void HardReset (NGit.Repository repo, string toRef)
		{
			ObjectId newHead = repo.Resolve (toRef);
			HardReset (repo, newHead);
		}
		
		public static void HardReset (NGit.Repository repo, ObjectId newHead)
		{
			DirCache dc = repo.LockDirCache ();
			
			try {
				// Reset head to upstream
				RefUpdate ru = repo.UpdateRef (Constants.HEAD);
				ru.SetNewObjectId (newHead);
				RefUpdate.Result rc = ru.Update ();
	
				switch (rc) {
				case RefUpdate.Result.NO_CHANGE:
				case RefUpdate.Result.NEW:
				case RefUpdate.Result.FAST_FORWARD:
					break;
				
				case RefUpdate.Result.REJECTED:
				case RefUpdate.Result.LOCK_FAILURE:
					throw new ConcurrentRefUpdateException (JGitText.Get ().couldNotLockHEAD, ru.GetRef (), rc);
	
				default:
					throw new JGitInternalException ("Reference update failed: " + rc);
				}
				
				RevWalk rw = new RevWalk (repo);
				RevCommit c = rw.ParseCommit (newHead);
				DirCacheCheckout checkout = new DirCacheCheckout (repo, null, dc, c.Tree);
				checkout.Checkout ();
			} catch {
				dc.Unlock ();
				throw;
			}
		}
		
		public static void Checkout (NGit.Repository repo, RevCommit commit, string working_directory)
		{
			DirCacheCheckout co = new DirCacheCheckout (repo, null, repo.ReadDirCache (), commit.Tree);
			co.Checkout ();
		}
		
		public static StashCollection GetStashes (NGit.Repository repo)
		{
			return new StashCollection (repo);
		}
		
		public static List<string> GetChangedFiles (NGit.Repository repo, string refRev)
		{
			// Get a list of files that are different in the target branch
			RevWalk rw = new RevWalk (repo);
			ObjectId remCommitId = repo.Resolve (refRev);
			if (remCommitId == null)
				return null;
			RevCommit remCommit = rw.ParseCommit (remCommitId);
			
			ObjectId headId = repo.Resolve (Constants.HEAD);
			if (headId == null)
				return null;
			RevCommit headCommit = rw.ParseCommit (headId);
			
			IEnumerable<Change> changes = GitUtil.CompareCommits (repo, headCommit, remCommit);
			return new List<string> (changes.Select (c => c.Path));
		}
		
		public static string GetUpstreamSource (NGit.Repository repo, string branch)
		{
			StoredConfig config = repo.GetConfig ();
			string remote = config.GetString ("branch", branch, "remote");
			string rbranch = config.GetString ("branch", branch, "merge");
			if (string.IsNullOrEmpty (rbranch))
				return null;
			if (rbranch.StartsWith (Constants.R_HEADS))
				rbranch = rbranch.Substring (Constants.R_HEADS.Length);
			else if (rbranch.StartsWith (Constants.R_TAGS))
				rbranch = rbranch.Substring (Constants.R_TAGS.Length);
			if (!string.IsNullOrEmpty (remote) && remote != ".")
				return remote + "/" + rbranch;
			else
				return rbranch;
		}
		
		public static void SetUpstreamSource (NGit.Repository repo, string branch, string remoteBranch)
		{
			StoredConfig config = repo.GetConfig ();
			if (string.IsNullOrEmpty (remoteBranch)) {
				config.UnsetSection ("branch", branch);
				config.Save ();
				return;
			}
			
			int i = remoteBranch.IndexOf ('/');
			string upBranch;
			if (i == -1) {
				var tags = repo.GetTags ();
				if (tags.ContainsKey (remoteBranch))
					upBranch = Constants.R_TAGS + remoteBranch;
				else
					upBranch = Constants.R_HEADS + remoteBranch;
				config.SetString ("branch", branch, "remote", ".");
			} else {
				upBranch = Constants.R_HEADS + remoteBranch.Substring (i + 1);
				config.SetString ("branch", branch, "remote", remoteBranch.Substring (0, i));
			}
			config.SetString ("branch", branch, "merge", upBranch);
			config.Save ();
		}
		
		public static FileRepository Clone (string targetLocalPath, string url, IProgressMonitor monitor)
		{
			// Initialize
			
			FileRepository repo = new FileRepository (Path.Combine (targetLocalPath, ".git"));
			repo.Create ();
			
			string branch = "master";
			string remoteName = "origin";
			
			RefUpdate head = repo.UpdateRef (Constants.HEAD);
			head.DisableRefLog ();
			head.Link (branch);
			
			RemoteConfig remoteConfig = new RemoteConfig (repo.GetConfig (), remoteName);
			remoteConfig.AddURI (new URIish (url));
			
			string dst = Constants.R_REMOTES + remoteConfig.Name;
			RefSpec wcrs = new RefSpec();
			wcrs = wcrs.SetForceUpdate (true);
			wcrs = wcrs.SetSourceDestination (Constants.R_HEADS	+ "*", dst + "/*");
			
			remoteConfig.AddFetchRefSpec (wcrs);
	
			// we're setting up for a clone with a checkout
			repo.GetConfig().SetBoolean ("core", null, "bare", false);
	
			remoteConfig.Update (repo.GetConfig());
	
			// branch is like 'Constants.R_HEADS + branchName', we need only
			// the 'branchName' part
			String branchName = branch.Substring (Constants.R_HEADS.Length);
	
			// setup the default remote branch for branchName
			repo.GetConfig().SetString("branch", branchName, "remote", remoteName);
			repo.GetConfig().SetString("branch", branchName, "merge", branch);
	
			repo.GetConfig().Save();
			
			// Fetch
			
			Transport tn = Transport.Open (repo, remoteName);
			FetchResult r;

			try {
				r = tn.Fetch(new GitMonitor (monitor), null);
			}
			finally {
				tn.Close ();
			}
			
			// Checkout

			DirCache dc = repo.LockDirCache ();
			try {
				RevWalk rw = new RevWalk (repo);
				ObjectId remCommitId = repo.Resolve (remoteName + "/" + branch);
				RevCommit remCommit = rw.ParseCommit (remCommitId);
				DirCacheCheckout co = new DirCacheCheckout (repo, null, dc, remCommit.Tree);
				co.Checkout ();
			} catch {
				dc.Unlock ();
				throw;
			}
			
			return repo;
		}
	}
	
	public class RebaseOperation
	{
		NGit.Repository repo;
		RevWalk rw;
		bool starting;
		string upstreamRef;
		RevCommit oldHead;
		MergeCommandResult mergeResult;
		List<RevCommit> commitChain;
		int currentMergeIndex;
		ObjectId lastGoodHead;
		
		public RebaseOperation (NGit.Repository repo, string upstreamRef)
		{
			this.repo = repo;
			this.upstreamRef = upstreamRef;
			rw = new RevWalk (repo);
			starting = true;
		}
		
		public bool Rebase ()
		{
			if (starting) {
				// Reset head to upstream
				GitUtil.HardReset (repo, upstreamRef);
				
				ObjectId headId = repo.Resolve (Constants.HEAD + "^{commit}");
				RevCommit headCommit = rw.ParseCommit (headId);
				oldHead = headCommit;
				ObjectId upstreamId = repo.Resolve (upstreamRef);
				RevCommit upstreamCommit = rw.ParseCommit (upstreamId);
				
				oldHead = headCommit;
				lastGoodHead = upstreamId;
			
				// Find the first commit that diverged from upstream
				int fd;
				commitChain = FindDivergentCommitChain (repo, upstreamCommit, headCommit, 0, int.MaxValue, out fd);
				if (commitChain == null)
					throw new InvalidOperationException ("Current branch and upstream branch don't have a common ancestor");
				currentMergeIndex = 0;
			}
			
			// Merge commit by commit until the current head
			
			NGit.Api.Git git = new NGit.Api.Git (repo);
			while (currentMergeIndex < commitChain.Count) {
				mergeResult = git.Merge ().SetStrategy (MergeStrategy.RESOLVE).Include (commitChain[currentMergeIndex++]).Call ();
				if (mergeResult.GetMergeStatus () == MergeStatus.CONFLICTING || mergeResult.GetMergeStatus () == MergeStatus.FAILED)
					return false;
				lastGoodHead = mergeResult.GetNewHead ();
			}
			return true;
		}
		
		public MergeCommandResult LastMergeResult {
			get { return mergeResult; }
		}
		
		public void Abort ()
		{
			GitUtil.HardReset (repo, oldHead);
		}
		
		public void Skip ()
		{
			GitUtil.HardReset (repo, lastGoodHead);
		}
		
		static List<RevCommit> FindDivergentCommitChain (NGit.Repository repo, RevCommit upstream, RevCommit branch, int currentDistance, int maxDistance, out int foundDistance)
		{
			foundDistance = -1;
			
			if (currentDistance + 1 >= maxDistance)
				return null;
			
			RevWalk rw = new RevWalk (repo);
			foreach (RevCommit pc in branch.Parents) {
				if (rw.IsMergedInto (pc, upstream)) {
					foundDistance = currentDistance + 1;
					List<RevCommit> commitChain = new List<RevCommit> ();
					commitChain.Add (branch);
					return commitChain;
				}
			}
			
			List<RevCommit> foundCommitChain = null;
			
			foreach (RevCommit pc in branch.Parents) {
				List<RevCommit> commitChain = FindDivergentCommitChain (repo, upstream, pc, currentDistance + 1, maxDistance, out foundDistance);
				if (commitChain != null) {
					maxDistance = foundDistance;
					foundCommitChain = commitChain;
				}
			}
			if (foundCommitChain != null) {
				foundCommitChain.Add (branch);
				return foundCommitChain;
			}
			
			return null;
		}
	}
}

