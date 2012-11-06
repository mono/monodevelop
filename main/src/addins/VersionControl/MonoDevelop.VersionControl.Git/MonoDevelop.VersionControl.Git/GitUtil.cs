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
using Mono.TextEditor.Utils;
using NGit.Internal;

namespace MonoDevelop.VersionControl.Git
{
	internal static class GitUtil
	{
		public static string ToGitPath (this NGit.Repository repo, FilePath filePath)
		{
			return filePath.FullPath.ToRelative (repo.WorkTree.ToString ()).ToString ().Replace ('\\', '/');
		}

		public static IEnumerable<string> ToGitPath (this NGit.Repository repo, IEnumerable<FilePath> filePaths)
		{
			foreach (var p in filePaths)
				yield return ToGitPath (repo, p);
		}

		public static FilePath FromGitPath (this NGit.Repository repo, string filePath)
		{
			filePath = filePath.Replace ('/', Path.DirectorySeparatorChar);
			return Path.Combine (repo.WorkTree, filePath);
		}
		
		public static List<string> GetConflictedFiles (NGit.Repository repo)
		{
			List<string> list = new List<string> ();
			TreeWalk treeWalk = new TreeWalk (repo);
			treeWalk.Reset ();
			treeWalk.Recursive = true;
			DirCache dc = repo.ReadDirCache ();
			treeWalk.AddTree (new DirCacheIterator (dc));
			while (treeWalk.Next()) {
				DirCacheIterator dirCacheIterator = treeWalk.GetTree<DirCacheIterator>(0);
				var ce = dirCacheIterator.GetDirCacheEntry ();
				if (ce != null && ce.Stage == 1)
					list.Add (ce.PathString);
			}
			return list;
		}
		
		/// <summary>
		/// Compares two commits and returns a list of files that have changed
		/// </summary>
		public static IEnumerable<DiffEntry> CompareCommits (NGit.Repository repo, RevCommit reference, RevCommit compared)
		{
			var changes = new List<DiffEntry>();
			if (reference == null && compared == null)
				return changes;
			ObjectId refTree = (reference != null ? reference.Tree.Id : ObjectId.ZeroId);
			ObjectId comparedTree = (compared != null ? compared.Tree.Id : ObjectId.ZeroId);
			return CompareCommits (repo, refTree, comparedTree);
		}
		
		/// <summary>
		/// Returns a list of files that have changed in a commit
		/// </summary>
		public static IEnumerable<DiffEntry> GetCommitChanges (NGit.Repository repo, RevCommit commit)
		{
			var rev = commit.ToObjectId ();
			var prev = repo.Resolve (commit.Name + "^") ?? ObjectId.ZeroId;
			return CompareCommits (repo, rev, prev);
		}
		
		public static IEnumerable<DiffEntry> CompareCommits (NGit.Repository repo, AnyObjectId reference, ObjectId compared)
		{
			var diff = new NGit.Api.Git (repo).Diff ();

			var firstTree = new CanonicalTreeParser ();
			firstTree.Reset (repo.NewObjectReader (), new RevWalk (repo).ParseTree (reference));
			diff.SetNewTree (firstTree);
			
			if (compared != ObjectId.ZeroId) {
				var secondTree = new CanonicalTreeParser ();
				secondTree.Reset (repo.NewObjectReader (), new RevWalk (repo).ParseTree (compared));

				if (compared != ObjectId.ZeroId)
					diff.SetOldTree (secondTree);
			}
			return diff.Call ();
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
		
		public static IEnumerable<DiffEntry> GetChangedFiles (NGit.Repository repo, string refRev)
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
		
		public static LocalGitRepository Init (string targetLocalPath, string url, IProgressMonitor monitor)
		{
			InitCommand ci = new InitCommand ();
			ci.SetDirectory (targetLocalPath);
			ci.Call ();
			LocalGitRepository repo = new LocalGitRepository (Path.Combine (targetLocalPath, Constants.DOT_GIT));
			
			string branch = Constants.R_HEADS + "master";
			
			RefUpdate head = repo.UpdateRef (Constants.HEAD);
			head.DisableRefLog ();
			head.Link (branch);
			
			if (url != null) {
				RemoteConfig remoteConfig = new RemoteConfig (repo.GetConfig (), "origin");
				remoteConfig.AddURI (new URIish (url));
				
				string dst = Constants.R_REMOTES + remoteConfig.Name;
				RefSpec wcrs = new RefSpec();
				wcrs = wcrs.SetForceUpdate (true);
				wcrs = wcrs.SetSourceDestination (Constants.R_HEADS	+ "*", dst + "/*");
				
				remoteConfig.AddFetchRefSpec (wcrs);
				remoteConfig.Update (repo.GetConfig());
			}
	
			// we're setting up for a clone with a checkout
			repo.GetConfig().SetBoolean ("core", null, "bare", false);
	
			repo.GetConfig().Save();
			return repo;
		}

		public static MergeCommandResult MergeTrees (NGit.ProgressMonitor monitor, NGit.Repository repo, RevCommit srcBase, RevCommit srcCommit, string sourceDisplayName, bool commitResult)
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
				
				ResolveMerger merger = (ResolveMerger)((ThreeWayMerger)MergeStrategy.RESOLVE.NewMerger
					(repo));
				
				merger.SetWorkingTreeIterator(new FileTreeIterator(repo));
				
				merger.SetBase(srcBase);
				
				bool noProblems;
				IDictionary<string, MergeResult<NGit.Diff.Sequence>> lowLevelResults = null;
				IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths = null;
				IList<string> modifiedFiles = null;
				
				ResolveMerger resolveMerger = (ResolveMerger)merger;
				resolveMerger.SetCommitNames(new string[] { "BASE", "HEAD", sourceDisplayName });
				noProblems = merger.Merge(headCommit, srcCommit);
				lowLevelResults = resolveMerger.GetMergeResults();
				modifiedFiles = resolveMerger.GetModifiedFiles();
				failingPaths = resolveMerger.GetFailingPaths();
				
				if (monitor != null)
					monitor.Update (50);
				
				if (noProblems)
				{
					if (modifiedFiles != null && modifiedFiles.Count == 0) {
						return new MergeCommandResult(headCommit, null, new ObjectId[] { headCommit.Id, srcCommit
							.Id }, MergeStatus.ALREADY_UP_TO_DATE, MergeStrategy.RESOLVE, null, null);
					}
					DirCacheCheckout dco = new DirCacheCheckout(repo, headCommit.Tree, repo.LockDirCache
						(), merger.GetResultTreeId());
					dco.SetFailOnConflict(true);
					dco.Checkout();
					if (commitResult) {
						newHead = new NGit.Api.Git(repo).Commit().SetMessage(srcCommit.GetFullMessage()
							).SetAuthor(srcCommit.GetAuthorIdent()).Call();
						return new MergeCommandResult(newHead.Id, null, new ObjectId[] { headCommit.Id, srcCommit
							.Id }, MergeStatus.MERGED, MergeStrategy.RESOLVE, null, null);
					} else {
						return new MergeCommandResult(headCommit, null, new ObjectId[] { headCommit.Id, srcCommit
							.Id }, MergeStatus.MERGED, MergeStrategy.RESOLVE, null, null);
					}
				}
				else
				{
					if (failingPaths != null)
					{
						return new MergeCommandResult(null, merger.GetBaseCommit(0, 1), new ObjectId[] { 
							headCommit.Id, srcCommit.Id }, MergeStatus.FAILED, MergeStrategy.RESOLVE, lowLevelResults
							, failingPaths, null);
					}
					else
					{
						return new MergeCommandResult(null, merger.GetBaseCommit(0, 1), new ObjectId[] { 
							headCommit.Id, srcCommit.Id }, MergeStatus.CONFLICTING, MergeStrategy.RESOLVE, lowLevelResults
							, null);
					}
				}
			}
			finally
			{
				revWalk.Release();
			}
		}
	}
}

