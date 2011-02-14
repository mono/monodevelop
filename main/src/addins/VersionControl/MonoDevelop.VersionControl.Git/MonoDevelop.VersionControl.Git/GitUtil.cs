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
		
		public static FileRepository Init (string targetLocalPath, string url, IProgressMonitor monitor)
		{
			InitCommand ci = new InitCommand ();
			ci.SetDirectory (targetLocalPath);
			var git = ci.Call ();
			FileRepository repo = (FileRepository) git.GetRepository ();
			
			string branch = Constants.R_HEADS + "master";
			
			RefUpdate head = repo.UpdateRef (Constants.HEAD);
			head.DisableRefLog ();
			head.Link (branch);
			
			RemoteConfig remoteConfig = new RemoteConfig (repo.GetConfig (), "origin");
			remoteConfig.AddURI (new URIish (url));
			
			string dst = Constants.R_REMOTES + remoteConfig.Name;
			RefSpec wcrs = new RefSpec();
			wcrs = wcrs.SetForceUpdate (true);
			wcrs = wcrs.SetSourceDestination (Constants.R_HEADS	+ "*", dst + "/*");
			
			remoteConfig.AddFetchRefSpec (wcrs);
	
			// we're setting up for a clone with a checkout
			repo.GetConfig().SetBoolean ("core", null, "bare", false);
	
			remoteConfig.Update (repo.GetConfig());
	
			repo.GetConfig().Save();
			return repo;
		}
		
		public static RevCommit[] Blame (NGit.Repository repo, RevCommit commit, string file)
		{
			string localFile = ToGitPath (repo, file);
			TreeWalk tw = TreeWalk.ForPath (repo, localFile, commit.Tree);
			if (tw == null)
				return new RevCommit [0];
			int totalLines = GetFileLineCount (repo, tw);
			int lineCount = totalLines;			
			RevCommit[] lines = new RevCommit [lineCount];
			RevWalk revWalker = new RevWalk (repo);
			revWalker.MarkStart (commit);
			List<RevCommit> commitHistory = new List<RevCommit>();
			FilePath localCpath = FromGitPath (repo, localFile);
			
			foreach (RevCommit ancestorCommit in revWalker) {
				foreach (Change change in GetCommitChanges (repo, ancestorCommit)) {
					FilePath cpath = FromGitPath (repo, change.Path);
					if (change.ChangeType != ChangeType.Deleted && (localCpath == cpath || cpath.IsChildPathOf (localCpath)))
					{
						commitHistory.Add(ancestorCommit);
						break;
					}
				}
			}
			
			int historySize = commitHistory.Count;
			
			if (historySize > 1) {
				RevCommit recentCommit = commitHistory[0];
				RawText latestRawText = GetRawText (repo, localFile, recentCommit);
				
				for (int i = 1; i < historySize; i++) {
					RevCommit ancestorCommit = commitHistory[i];
					RawText ancestorRawText = GetRawText (repo, localFile, ancestorCommit);
					lineCount -= SetBlameLines(repo, lines, recentCommit, latestRawText, ancestorRawText);
					recentCommit = ancestorCommit;
					
					if (lineCount <= 0)
					{
						break;
					}
				}
				
				if (lineCount > 0) {
					RevCommit firstCommit = commitHistory[historySize - 1];
					
					for (int i = 0; i < totalLines; i++) {
						if (lines[i] == null) {
							lines[i] = firstCommit;
						}
					}
				}
			} else if (historySize == 1) {
				RevCommit firstCommit = commitHistory[0];
					
				for (int i = 0; i < totalLines; i++) {
					lines[i] = firstCommit;
				}
			}
			
			return lines;
		}
		
		static int GetFileLineCount (NGit.Repository repo, TreeWalk tw) {
			ObjectId id = tw.GetObjectId (0);
			byte[] data = repo.ObjectDatabase.Open (id).GetBytes ();			
			return new RawText (data).Size();
		}
		
		static RawText GetRawText(NGit.Repository repo, string file, RevCommit commit) {
			TreeWalk tw = TreeWalk.ForPath (repo, file, commit.Tree);
			if (tw == null)
				return new RawText (new byte[0]);
			ObjectId objectID = tw.GetObjectId(0);
			byte[] data = repo.ObjectDatabase.Open (objectID).GetBytes ();
			return new RawText (data);
		}

		static int SetBlameLines (NGit.Repository repo, RevCommit[] lines, RevCommit commit, RawText curText, RawText ancestorText)
		{
			int lineCount = 0;
			IEnumerable<Hunk> diffHunks = GetDiffHunks (curText, ancestorText);

			foreach (Hunk e in diffHunks) {
				int basePosition = e.InsertStart - 1;
				for (int i = 0; i < e.Inserted; i++) {
					int lineNum = basePosition + i;
					if (lines [lineNum] == null) {
						lines [lineNum] = commit;
						lineCount++;
					}
				}
			}
			
			return lineCount;
		}
		
		static IEnumerable<Hunk> GetDiffHunks (RawText curText, RawText ancestorText)
		{
			Dictionary<string, int> codeDictionary = new Dictionary<string, int> ();
			int codeCounter = 0;
			int[] ancestorDiffCodes = GetDiffCodes (ref codeCounter, codeDictionary, ancestorText);
			int[] currentDiffCodes = GetDiffCodes (ref codeCounter, codeDictionary, curText);
			return Diff.GetDiff<int> (ancestorDiffCodes, currentDiffCodes);
		}

		static int[] GetDiffCodes (ref int codeCounter, Dictionary<string, int> codeDictionary, RawText text)
		{
			int lineCount = text.Size ();
			int[] result = new int[lineCount];
			string[] lines = GetLineStrings (text);
			for (int i = 0; i < lineCount; i++) {
				string lineText = lines [i];
				int curCode;
				if (!codeDictionary.TryGetValue (lineText, out curCode)) {
					codeDictionary [lineText] = curCode = ++codeCounter;
				}
				result [i] = curCode;
			}
			return result;
		}

		static string[] GetLineStrings (RawText text)
		{
			int lineCount = text.Size ();
			string[] lines = new string[lineCount];

			for (int i = 0; i < lineCount; i++) {
				lines [i] = text.GetString (i);
			}

			return lines;
		}

		static int FillRemainingBlame (RevCommit[] lines, RevCommit commit)
		{
			int lineCount = 0;
			
			for (int n=0; n<lines.Length; n++) {
				if (lines [n] == null) {
					lines [n] = commit;
					lineCount++;
				}
			}
			
			return lineCount;
		}
		
		public static MergeCommandResult MergeTrees (NGit.Repository repo, RevCommit srcBase, RevCommit srcCommit, string sourceDisplayName, bool commitResult)
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
				
				// CherryPick command sets the working tree, but this should not be necessary, and when setting it
				// untracked files are deleted during the merge
				// merger.SetWorkingTreeIterator(new FileTreeIterator(repo));
				
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
			finally
			{
				revWalk.Release();
			}
		}
		
	}
	
	class RevisionObjectIdPair
	{
		public RevisionObjectIdPair(RevCommit revision, ObjectId objectId)
		{
			this.Commit = revision;
			this.ObjectId = objectId;
		}
		
		public RevCommit Commit { get; private set; }
		public ObjectId ObjectId { get; private set; }
	}
}

