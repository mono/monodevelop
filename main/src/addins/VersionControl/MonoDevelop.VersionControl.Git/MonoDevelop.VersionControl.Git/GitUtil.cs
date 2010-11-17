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
using NGit.Diff;

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
			DirCache dc = null;
			
			try {
				// Reset head to upstream
				RefUpdate ru = repo.UpdateRef (Constants.HEAD);
				ru.SetNewObjectId (newHead);
				ru.SetForceUpdate (true);
				RefUpdate.Result rc = ru.Update ();
	
				switch (rc) {
				case RefUpdate.Result.NO_CHANGE:
				case RefUpdate.Result.NEW:
				case RefUpdate.Result.FAST_FORWARD:
				case RefUpdate.Result.FORCED:
					break;
				
				case RefUpdate.Result.REJECTED:
				case RefUpdate.Result.LOCK_FAILURE:
					throw new ConcurrentRefUpdateException (JGitText.Get ().couldNotLockHEAD, ru.GetRef (), rc);
	
				default:
					throw new JGitInternalException ("Reference update failed: " + rc);
				}
				
				dc = repo.LockDirCache ();
				RevWalk rw = new RevWalk (repo);
				RevCommit c = rw.ParseCommit (newHead);
				DirCacheCheckout checkout = new DirCacheCheckout (repo, null, dc, c.Tree);
				checkout.Checkout ();
			} catch {
				if (dc != null)
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
		
		public static IEnumerable<Change> GetChangedFiles (NGit.Repository repo, string refRev)
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
			
			return GitUtil.CompareCommits (repo, headCommit, remCommit);
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
		
		public static RevCommit[] Blame (NGit.Repository repo, RevCommit c, string file)
		{
			TreeWalk tw = TreeWalk.ForPath (repo, ToGitPath (repo, file), c.Tree);
			if (tw == null)
				return new RevCommit [0];
			ObjectId id = tw.GetObjectId (0);
			byte[] data = repo.ObjectDatabase.Open (id).GetBytes ();
			
			int lineCount = NGit.Util.RawParseUtils.LineMap (data, 0, data.Length).Size ();
			RevCommit[] lines = new RevCommit [lineCount];
			var curText = new RawText (data);
			RevCommit prevAncestor = c;
			
			ObjectId prevObjectId = null;
			RevCommit prevCommit = null;
			int emptyLines = lineCount;
			RevWalk rw = new RevWalk (repo);
			
			foreach (ObjectId ancestorId in c.Parents) {
				RevCommit ancestor = rw.ParseCommit (ancestorId);
				tw = TreeWalk.ForPath (repo, ToGitPath (repo, file), ancestor.Tree);
				if (prevCommit != null && (tw == null || tw.GetObjectId (0) != prevObjectId)) {
					if (prevObjectId == null)
						break;
					byte[] prevData = repo.ObjectDatabase.Open (prevObjectId).GetBytes ();
					var prevText = new RawText (prevData);
					var differ = MyersDiff<RawText>.INSTANCE;
					foreach (Edit e in differ.Diff (RawTextComparator.DEFAULT, prevText, curText)) {
						for (int n = e.GetBeginB (); n < e.GetEndB (); n++) {
							if (lines [n] == null) {
								lines [n] = prevCommit;
								emptyLines--;
							}
						}
					}
					if (tw == null || emptyLines <= 0)
						break;
				}
				prevCommit = ancestor;
				prevObjectId = tw != null ? tw.GetObjectId (0) : null;
			}
			for (int n=0; n<lines.Length; n++)
				if (lines [n] == null)
					lines [n] = prevAncestor;
			return lines;
		}
		
		public static MergeCommandResult CherryPick (NGit.Repository repo, RevCommit srcCommit)
		{
			RevCommit newHead = null;
			RevWalk revWalk = new RevWalk(repo);
			try
			{
				// get the head commit
				Ref headRef = repo.GetRef(Constants.HEAD);
				if (headRef == null)
				{
					throw new NoHeadException(JGitText.Get().commitOnRepoWithoutHEADCurrentlyNotSupported
						);
				}
				RevCommit headCommit = revWalk.ParseCommit(headRef.GetObjectId());
				
				// get the parent of the commit to cherry-pick
				if (srcCommit.ParentCount != 1)
				{
					throw new MultipleParentsNotAllowedException(JGitText.Get().canOnlyCherryPickCommitsWithOneParent
						);
				}
				RevCommit srcParent = srcCommit.GetParent(0);
				revWalk.ParseHeaders(srcParent);
				ResolveMerger merger = (ResolveMerger)((ThreeWayMerger)MergeStrategy.RESOLVE.NewMerger
					(repo));
				merger.SetWorkingTreeIterator(new FileTreeIterator(repo));
				merger.SetBase(srcParent.Tree);
				
				bool noProblems;
				IDictionary<string, MergeResult<NGit.Diff.Sequence>> lowLevelResults = null;
				IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths = null;
				if (merger is ResolveMerger)
				{
					ResolveMerger resolveMerger = (ResolveMerger)merger;
					resolveMerger.SetCommitNames(new string[] { "BASE", "HEAD", srcCommit.Name });
					resolveMerger.SetWorkingTreeIterator(new FileTreeIterator(repo));
					noProblems = merger.Merge(headCommit, srcCommit);
					lowLevelResults = resolveMerger.GetMergeResults();
					failingPaths = resolveMerger.GetFailingPathes();
				}
				else
				{
					noProblems = merger.Merge(headCommit, srcCommit);
				}
				
				if (noProblems)
				{
					DirCacheCheckout dco = new DirCacheCheckout(repo, headCommit.Tree, repo.LockDirCache
						(), merger.GetResultTreeId());
					dco.SetFailOnConflict(true);
					dco.Checkout();
					newHead = new NGit.Api.Git(repo).Commit().SetMessage(srcCommit.GetFullMessage()
						).SetAuthor(srcCommit.GetAuthorIdent()).Call();
					return new MergeCommandResult(newHead.Id, null, new ObjectId[] { headCommit.Id, srcCommit
						.Id }, MergeStatus.MERGED, MergeStrategy.RESOLVE, null, null);
				}
				else
				{
					if (failingPaths != null)
					{
						return new MergeCommandResult(null, merger.GetBaseCommit(0, 1), new ObjectId[] { 
							headCommit.Id, srcCommit.Id }, MergeStatus.FAILED, MergeStrategy.RESOLVE, lowLevelResults
							, null);
					}
					else
					{
						return new MergeCommandResult(null, merger.GetBaseCommit(0, 1), new ObjectId[] { 
							headCommit.Id, srcCommit.Id }, MergeStatus.CONFLICTING, MergeStrategy.RESOLVE, lowLevelResults
							, null);
					}
				}
			}
			catch (IOException e)
			{
				throw new Exception ("Commit failed", e);
			}
			finally
			{
				revWalk.Release();
			}
		}

	}
}

