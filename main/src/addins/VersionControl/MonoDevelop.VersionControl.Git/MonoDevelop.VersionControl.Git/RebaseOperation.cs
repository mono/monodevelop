// 
// RebaseOperation.cs
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
	public class RebaseOperation
	{
		NGit.Repository repo;
		RevWalk rw;
		bool starting;
		string branch;
		string upstreamRef;
		RevCommit oldHead;
		MergeCommandResult mergeResult;
		List<RevCommit> commitChain;
		int currentMergeIndex;
		RevCommit currentMergeCommit;
		ObjectId lastGoodHead;
		bool aborted;
		
		public RebaseOperation (NGit.Repository repo, string upstreamRef)
		{
			this.repo = repo;
			this.upstreamRef = upstreamRef;
			rw = new RevWalk (repo);
			branch = repo.GetBranch ();
			starting = true;
		}
		
		public bool Rebase ()
		{
			NGit.Api.Git git = new NGit.Api.Git (repo);
			
			if (aborted)
				return false;
			
			if (starting) {
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
				
				// Checkout the upstream commit
				// Reset head to upstream
				GitUtil.HardReset (repo, upstreamRef);
				
				string rebaseDir = Path.Combine (repo.Directory, "rebase-apply");
				if (!Directory.Exists (rebaseDir))
					Directory.CreateDirectory (rebaseDir);
				
				string rebasingFile = Path.Combine (rebaseDir, "rebasing");
				if (!File.Exists (rebasingFile))
					File.WriteAllBytes (rebasingFile, new byte[0]);
				
				starting = false;
			}
			else {
				// Conflicts resolved. Continue.
				NGit.Api.AddCommand cmd = git.Add ();
				var conflicts = LastMergeResult.GetConflicts ();
				foreach (string conflictFile in conflicts.Keys) {
					cmd.AddFilepattern (conflictFile);
				}
				cmd.Call ();
				NGit.Api.CommitCommand commit = git.Commit ();
				commit.SetMessage (currentMergeCommit.GetFullMessage ());
				commit.SetAuthor (currentMergeCommit.GetAuthorIdent ());
				commit.SetCommitter (currentMergeCommit.GetCommitterIdent ());
				commit.Call();
			}
			
			// Merge commit by commit until the current head
			
			while (currentMergeIndex < commitChain.Count) {
				currentMergeCommit = commitChain[currentMergeIndex++];
				mergeResult = git.Merge ().SetStrategy (MergeStrategy.RESOLVE).Include (currentMergeCommit).Call ();
				if (mergeResult.GetMergeStatus () == MergeStatus.CONFLICTING || mergeResult.GetMergeStatus () == MergeStatus.FAILED)
					return false;
				lastGoodHead = mergeResult.GetNewHead ();
			}
			
			CleanRebaseFile ();
			return true;
		}
		
		public MergeCommandResult LastMergeResult {
			get { return mergeResult; }
		}
		
		public void Abort ()
		{
			aborted = true;
			GitUtil.HardReset (repo, oldHead);
			CleanRebaseFile ();
		}
		
		public void Skip ()
		{
			GitUtil.HardReset (repo, lastGoodHead);
		}
		
		void CleanRebaseFile ()
		{
			string rebaseDir = Path.Combine (repo.Directory, "rebase-apply");
			if (Directory.Exists (rebaseDir))
				Directory.Delete (rebaseDir, true);
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
