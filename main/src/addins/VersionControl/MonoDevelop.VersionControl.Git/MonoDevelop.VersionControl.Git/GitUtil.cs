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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core;
using org.eclipse.jgit.api;
using org.eclipse.jgit.api.errors;
using org.eclipse.jgit.errors;
using org.eclipse.jgit.diff;
using org.eclipse.jgit.dircache;
using org.eclipse.jgit.@internal;
using org.eclipse.jgit.lib;
using org.eclipse.jgit.merge;
using org.eclipse.jgit.revwalk;
using org.eclipse.jgit.transport;
using org.eclipse.jgit.treewalk;
using JRepository = org.eclipse.jgit.lib.Repository;
using System;
using System.Linq;
using java.lang;

namespace MonoDevelop.VersionControl.Git
{
	internal static class GitUtil
	{
		public static string ToGitPath (this JRepository repo, FilePath filePath)
		{
			return filePath.FullPath.ToRelative (repo.getWorkTree().toString ()).ToString ().Replace ('\\', '/');
		}

		public static IEnumerable<string> ToGitPath (this JRepository repo, IEnumerable<FilePath> filePaths)
		{
			foreach (var p in filePaths)
				yield return ToGitPath (repo, p);
		}

		public static FilePath FromGitPath (this JRepository repo, string filePath)
		{
			filePath = filePath.Replace ('/', Path.DirectorySeparatorChar);
			return Path.Combine (repo.getWorkTree().toString (), filePath);
		}
		
		public static List<string> GetConflictedFiles (JRepository repo)
		{
			List<string> list = new List<string> ();
			TreeWalk treeWalk = new TreeWalk (repo);
			treeWalk.reset ();
			treeWalk.setRecursive (true);
			DirCache dc = repo.readDirCache ();
			treeWalk.addTree (new DirCacheIterator (dc));
			while (treeWalk.next()) {
				DirCacheIterator dirCacheIterator = (DirCacheIterator)treeWalk.getTree(0, (Class)typeof(DirCacheIterator));
				var ce = dirCacheIterator.getDirCacheEntry ();
				if (ce != null && ce.getStage() == 1)
					list.Add (ce.getPathString());
			}
			return list;
		}
		
		/// <summary>
		/// Compares two commits and returns a list of files that have changed
		/// </summary>
		public static java.util.List CompareCommits (JRepository repo, RevCommit reference, RevCommit compared)
		{
			var changes = new java.util.ArrayList ();
			if (reference == null && compared == null)
				return changes;
			ObjectId refTree = (reference != null ? reference.getTree().getId() : ObjectId.zeroId ());
			ObjectId comparedTree = (compared != null ? compared.getTree().getId() : ObjectId.zeroId ());
			return CompareCommits (repo, refTree, comparedTree);
		}
		
		/// <summary>
		/// Returns a list of files that have changed in a commit
		/// </summary>
		public static java.util.List GetCommitChanges (JRepository repo, RevCommit commit)
		{
			var rev = commit.toObjectId ();
			var prev = repo.resolve (commit.getName () + "^") ?? ObjectId.zeroId ();
			return CompareCommits (repo, rev, prev);
		}
		
		public static java.util.List CompareCommits (JRepository repo, AnyObjectId reference, ObjectId compared)
		{
			var diff = new MyersDiff (repo);

			var firstTree = new CanonicalTreeParser ();
			firstTree.reset (repo.newObjectReader (), new RevWalk (repo).parseTree (reference));
			diff.setNewTree (firstTree);
			
			if (compared != ObjectId.zeroId ()) {
				var secondTree = new CanonicalTreeParser ();
				secondTree.reset (repo.newObjectReader (), new RevWalk (repo).parseTree (compared));

				if (compared != ObjectId.zeroId ())
					diff.setOldTree (secondTree);
			}
			return (java.util.List)diff.call ();
		}

		public static ObjectId CreateCommit (JRepository rep, string message, IList<ObjectId> parents, ObjectId indexTreeId, PersonIdent author, PersonIdent committer)
		{
			try {
				ObjectInserter odi = rep.newObjectInserter ();
				try {
					// Create a Commit object, populate it and write it
					CommitBuilder commit = new CommitBuilder ();
					commit.setCommitter (committer);
					commit.setAuthor (author);
					commit.setMessage (message);
					commit.setParentIds (parents.ToArray ());
					commit.setTreeId (indexTreeId);
					ObjectId commitId = odi.insert (commit);
					odi.flush ();
					return commitId;
				} finally {
					odi.release ();
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
		
		public static void HardReset (JRepository repo, string toRef)
		{
			ObjectId newHead = repo.resolve (toRef);
			HardReset (repo, newHead);
		}
		
		public static void HardReset (JRepository repo, ObjectId newHead)
		{
			DirCache dc = null;
			
			try {
				// Reset head to upstream
				RefUpdate ru = repo.updateRef (Constants.HEAD);
				ru.setNewObjectId (newHead);
				ru.setForceUpdate (true);
				RefUpdate.Result rc = ru.update ();
	
				if (rc == RefUpdate.Result.REJECTED || rc == RefUpdate.Result.LOCK_FAILURE)
					throw new ConcurrentRefUpdateException (JGitText.get ().couldNotLockHEAD, ru.getRef (), rc);
				if (rc != RefUpdate.Result.NO_CHANGE && rc != RefUpdate.Result.NEW && rc != RefUpdate.Result.FAST_FORWARD && rc != RefUpdate.Result.FORCED)
					throw new JGitInternalException ("Reference update failed: " + rc);
				
				dc = repo.lockDirCache ();
				RevWalk rw = new RevWalk (repo);
				RevCommit c = rw.parseCommit (newHead);
				DirCacheCheckout checkout = new DirCacheCheckout (repo, null, dc, c.getTree ());
				checkout.checkout ();
			} catch {
				if (dc != null)
					dc.unlock ();
				throw;
			}
		}
		
		public static StashCollection GetStashes (JRepository repo)
		{
			return new StashCollection (repo);
		}
		
		public static java.util.List GetChangedFiles (JRepository repo, string refRev)
		{
			// Get a list of files that are different in the target branch
			RevWalk rw = new RevWalk (repo);
			ObjectId remCommitId = repo.resolve (refRev);
			if (remCommitId == null)
				return null;
			RevCommit remCommit = rw.parseCommit (remCommitId);
			
			ObjectId headId = repo.resolve (Constants.HEAD);
			if (headId == null)
				return null;
			RevCommit headCommit = rw.parseCommit (headId);
			
			return GitUtil.CompareCommits (repo, headCommit, remCommit);
		}
		
		public static string GetUpstreamSource (JRepository repo, string branch)
		{
			StoredConfig config = repo.getConfig ();
			string remote = config.getString ("branch", branch, "remote");
			string rbranch = config.getString ("branch", branch, "merge");
			if (string.IsNullOrEmpty (rbranch))
				return null;
			if (rbranch.StartsWith (Constants.R_HEADS, System.StringComparison.Ordinal))
				rbranch = rbranch.Substring (Constants.R_HEADS.Length);
			else if (rbranch.StartsWith (Constants.R_TAGS, System.StringComparison.Ordinal))
				rbranch = rbranch.Substring (Constants.R_TAGS.Length);
			if (!string.IsNullOrEmpty (remote) && remote != ".")
				return remote + "/" + rbranch;
			else
				return rbranch;
		}
		
		public static void SetUpstreamSource (JRepository repo, string branch, string remoteBranch)
		{
			StoredConfig config = repo.getConfig ();
			if (string.IsNullOrEmpty (remoteBranch)) {
				config.unsetSection ("branch", branch);
				config.save ();
				return;
			}
			
			int i = remoteBranch.IndexOf ('/');
			string upBranch;
			if (i == -1) {
				var tags = repo.getTags ();
				if (tags.containsKey (remoteBranch))
					upBranch = Constants.R_TAGS + remoteBranch;
				else
					upBranch = Constants.R_HEADS + remoteBranch;
				config.setString ("branch", branch, "remote", ".");
			} else {
				upBranch = Constants.R_HEADS + remoteBranch.Substring (i + 1);
				config.setString ("branch", branch, "remote", remoteBranch.Substring (0, i));
			}
			config.setString ("branch", branch, "merge", upBranch);
			config.save ();
		}
		
		public static LocalGitRepository Init (string targetLocalPath, string url)
		{
			InitCommand ci = new InitCommand ();
			ci.setDirectory (new java.io.File (targetLocalPath));
			ci.call ();
			LocalGitRepository repo = new LocalGitRepository (Path.Combine (targetLocalPath, Constants.DOT_GIT));
			
			string branch = Constants.R_HEADS + "master";
			
			RefUpdate head = repo.updateRef (Constants.HEAD);
			head.disableRefLog ();
			head.link (branch);
			
			if (url != null) {
				RemoteConfig remoteConfig = new RemoteConfig (repo.getConfig (), "origin");
				remoteConfig.addURI (new URIish (url));
				
				string dst = Constants.R_REMOTES + remoteConfig.getName ();
				RefSpec wcrs = new RefSpec();
				wcrs = wcrs.setForceUpdate (true);
				wcrs = wcrs.setSourceDestination (Constants.R_HEADS	+ "*", dst + "/*");
				
				remoteConfig.addFetchRefSpec (wcrs);
				remoteConfig.update (repo.getConfig());
			}
	
			// we're setting up for a clone with a checkout
			repo.getConfig().setBoolean ("core", null, "bare", false);
	
			repo.getConfig().save();
			return repo;
		}

		public static org.eclipse.jgit.api.MergeResult MergeTrees (ProgressMonitor monitor, JRepository repo, RevCommit srcBase, RevCommit srcCommit, string sourceDisplayName, bool commitResult)
		{
			RevCommit newHead;
			RevWalk revWalk = new RevWalk(repo);
			try
			{
				// get the head commit
				Ref headRef = repo.getRef(Constants.HEAD);
				if (headRef == null)
				{
					throw new NoHeadException(JGitText.get().commitOnRepoWithoutHEADCurrentlyNotSupported
						);
				}
				RevCommit headCommit = revWalk.parseCommit(headRef.getObjectId());
				
				ResolveMerger merger = (ResolveMerger)((ThreeWayMerger)MergeStrategy.RESOLVE.newMerger
					(repo));
				
				merger.setWorkingTreeIterator(new FileTreeIterator(repo));
				
				merger.setBase(srcBase);
				
				bool noProblems;

				java.util.Map lowLevelResults = null;
				java.util.Map failingPaths = null;
				java.util.List modifiedFiles = null;

				ResolveMerger resolveMerger = merger;
				resolveMerger.setCommitNames(new string[] { "BASE", "HEAD", sourceDisplayName });
				noProblems = merger.merge(headCommit, srcCommit);
				lowLevelResults = resolveMerger.getMergeResults();
				modifiedFiles = resolveMerger.getModifiedFiles();
				failingPaths = resolveMerger.getFailingPaths();
				
				if (monitor != null)
					monitor.update (50);
				
				if (noProblems)
				{
					if (modifiedFiles != null && modifiedFiles.size () == 0) {
						return new org.eclipse.jgit.api.MergeResult(headCommit, null, new ObjectId[] { headCommit.getId (), srcCommit.getId ()
								}, org.eclipse.jgit.api.MergeResult.MergeStatus.ALREADY_UP_TO_DATE, MergeStrategy.RESOLVE, null, null);
					}
					DirCacheCheckout dco = new DirCacheCheckout(repo, headCommit.getTree (), repo.lockDirCache (), 
						merger.getResultTreeId());
					dco.setFailOnConflict(true);
					dco.checkout();
					if (commitResult) {
						newHead = new org.eclipse.jgit.api.Git(repo).commit().setMessage(srcCommit.getFullMessage()
						).setAuthor(srcCommit.getAuthorIdent()).call();
						return new org.eclipse.jgit.api.MergeResult (newHead.getId (), null, new ObjectId[] { headCommit.getId (),
							srcCommit.getId () }, org.eclipse.jgit.api.MergeResult.MergeStatus.MERGED, MergeStrategy.RESOLVE, null, null);
					} else {
						return new org.eclipse.jgit.api.MergeResult(headCommit, null, new ObjectId[] { headCommit.getId(), srcCommit.getId () },
							org.eclipse.jgit.api.MergeResult.MergeStatus.MERGED, MergeStrategy.RESOLVE, null, null);
					}
				}
				else
				{
					if (failingPaths != null)
					{
						return new org.eclipse.jgit.api.MergeResult(null, merger.getBaseCommit(0, 1), new ObjectId[] { 
							headCommit.getId (), srcCommit.getId () }, org.eclipse.jgit.api.MergeResult.MergeStatus.FAILED,
							MergeStrategy.RESOLVE, lowLevelResults, failingPaths, null);
					}
					else
					{
						return new org.eclipse.jgit.api.MergeResult(null, merger.getBaseCommit(0, 1), new ObjectId[] { 
							headCommit.getId (), srcCommit.getId () }, org.eclipse.jgit.api.MergeResult.MergeStatus.CONFLICTING,
							MergeStrategy.RESOLVE, lowLevelResults, null);
					}
				}
			}
			finally
			{
				revWalk.release();
			}
		}

		internal static bool IsGitRepository (this FilePath path)
		{
			// Maybe check if it has a HEAD file? But this check should be enough.
			var newPath = path.Combine (".git");
			return Directory.Exists (newPath) && Directory.Exists (newPath.Combine ("objects")) && Directory.Exists (newPath.Combine ("refs"));
		}

		public static bool IsValidBranchName (string name)
		{
			// List from: https://github.com/git/git/blob/master/refs.c#L21
			if (name.StartsWith (".", StringComparison.Ordinal) ||
				name.EndsWith ("/", StringComparison.Ordinal) ||
				name.EndsWith (".lock", StringComparison.Ordinal))
				return false;

			if (name.Contains (" ") || name.Contains ("~") || name.Contains ("..") || name.Contains ("^") ||
				name.Contains (":") || name.Contains ("\\") || name.Contains ("?") || name.Contains ("["))
				return false;
			return true;
		}
	}
}

