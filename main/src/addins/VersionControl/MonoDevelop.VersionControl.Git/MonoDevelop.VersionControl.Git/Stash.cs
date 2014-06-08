// 
// Stash.cs
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

using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	public static class StashCollectionExtensions
	{
		internal static void Remove (this StashCollection sc, Stash stash)
		{
			int i = 0;
			foreach (var s in sc) {
				if (s.Name == stash.Name) {
					sc.Remove (i);
					break;
				}
				++i;
			}
		}

		/*
		public Stash Create (ProgressMonitor monitor)
		{
			return Create (monitor, null);
		}
		
		public Stash Create (ProgressMonitor monitor, string message)
		{
			/*if (monitor != null) {
				monitor.Start (1);
				monitor.BeginTask ("Stashing changes", 100);
			}
			
			UserConfig config = _repo.GetConfig ().Get (UserConfig.KEY);
			RevWalk rw = new RevWalk (_repo);
			ObjectId headId = _repo.Resolve (Constants.HEAD);
			var parent = rw.ParseCommit (headId);
			
			PersonIdent author = new PersonIdent(config.GetAuthorName () ?? "unknown", config.GetAuthorEmail () ?? "unknown@(none).");
			
			if (string.IsNullOrEmpty (message)) {
				// Use the commit summary as message
				message = parent.Abbreviate (7) + " " + parent.GetShortMessage ();
				int i = message.IndexOfAny (new char[] { '\r', '\n' });
				if (i != -1)
					message = message.Substring (0, i);
			}
			
			// Create the index tree commit
			ObjectInserter inserter = _repo.NewObjectInserter ();
			DirCache dc = _repo.ReadDirCache ();
			
			if (monitor != null)
				monitor.Update (10);
				
			var tree_id = dc.WriteTree (inserter);
			inserter.Release ();
			
			if (monitor != null)
				monitor.Update (10);
			
			string commitMsg = "index on " + _repo.GetBranch () + ": " + message;
			ObjectId indexCommit = GitUtil.CreateCommit (_repo, commitMsg + "\n", new ObjectId[] {headId}, tree_id, author, author);

			if (monitor != null)
				monitor.Update (20);
			
			// Create the working dir commit
			tree_id = WriteWorkingDirectoryTree (parent.Tree, dc);
			commitMsg = "WIP on " + _repo.GetBranch () + ": " + message;
			var wipCommit = GitUtil.CreateCommit(_repo, commitMsg + "\n", new ObjectId[] { headId, indexCommit }, tree_id, author, author);
			
			if (monitor != null)
				monitor.Update (20);
			
			string prevCommit = null;
			FileInfo sf = StashRefFile;
			if (sf.Exists)
				prevCommit = File.ReadAllText (sf.FullName).Trim (' ','\t','\r','\n');
			
			Stash s = new Stash (prevCommit, wipCommit.Name, author, commitMsg);
			
			FileInfo stashLog = StashLogFile;
			File.AppendAllText (stashLog.FullName, s.FullLine + "\n");
			File.WriteAllText (sf.FullName, s.CommitId + "\n");
			
			if (monitor != null)
				monitor.Update (5);
			
			// Wipe all local changes
			_repo.Reset (LibGit2Sharp.ResetMode.Hard, _repo.Head.Tip);
			
			monitor.EndTask ();
			s.StashCollection = this;
			return s;
		}
		
		public MergeCommandResult Pop (ProgressMonitor monitor)
		{
			List<Stash> stashes = ReadStashes ();
			Stash last = stashes.Last ();
			MergeCommandResult res = last.Apply (monitor);
			if (res.GetMergeStatus () != MergeStatus.FAILED && res.GetMergeStatus () != MergeStatus.NOT_SUPPORTED)
				Remove (stashes, last);
			return res;
		}
		
		void Remove (List<Stash> stashes, Stash s)
		{
			int i = stashes.FindIndex (st => st.CommitId == s.CommitId);
			if (i != -1) {
				stashes.RemoveAt (i);
				Stash next = stashes.FirstOrDefault (ns => ns.PrevStashCommitId == s.CommitId);
				if (next != null)
					next.PrevStashCommitId = s.PrevStashCommitId;
				if (stashes.Count == 0) {
					// No more stashes. The ref and log files can be deleted.
					StashRefFile.Delete ();
					StashLogFile.Delete ();
					return;
				}
				WriteStashes (stashes);
				if (i == stashes.Count) {
					// We deleted the head. Write the new head.
					File.WriteAllText (StashRefFile.FullName, stashes.Last ().CommitId + "\n");
				}
			}
		}
*/
	}
}

