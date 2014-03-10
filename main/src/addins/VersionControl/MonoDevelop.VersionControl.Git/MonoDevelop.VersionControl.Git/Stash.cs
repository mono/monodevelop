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

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using org.eclipse.jgit.lib;
using org.eclipse.jgit.api;
using org.eclipse.jgit.revwalk;
using org.eclipse.jgit.dircache;
using org.eclipse.jgit.treewalk;
using JRepository = org.eclipse.jgit.lib.Repository;
using JFileMode = org.eclipse.jgit.lib.FileMode;

namespace MonoDevelop.VersionControl.Git
{
	public class Stash
	{
		internal string CommitId { get; private set; }
		internal string FullLine { get; private set; }
		internal StashCollection StashCollection { get; set; }
		
		/// <summary>
		/// Who created the stash
		/// </summary>
		public PersonIdent Author { get; private set; }
		
		/// <summary>
		/// Timestamp of the stash creation
		/// </summary>
		public DateTimeOffset DateTime { get; private set; }
		
		/// <summary>
		/// Stash comment
		/// </summary>
		public string Comment { get; private set; }
		
		private Stash ()
		{
		}
		
		internal Stash (string prevStashCommitId, string commitId, PersonIdent author, string comment)
		{
			this.PrevStashCommitId = prevStashCommitId;
			this.CommitId = commitId;
			this.Author = author;
			this.DateTime = DateTimeOffset.Now;
			
			// Skip "WIP on master: "
			int i = comment.IndexOf (':');
			this.Comment = comment.Substring (i + 2);			
			
			// Create the text line to be written in the stash log
			
			int secs = (int) (this.DateTime - new DateTimeOffset (1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
			
			TimeSpan ofs = this.DateTime.Offset;
			string tz = string.Format ("{0}{1:00}{2:00}", (ofs.Hours >= 0 ? '+':'-'), Math.Abs (ofs.Hours), Math.Abs (ofs.Minutes));
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (prevStashCommitId ?? new string ('0', 40)).Append (' ');
			sb.Append (commitId).Append (' ');
			sb.Append (author.getName ()).Append (" <").Append (author.getEmailAddress ()).Append ("> ");
			sb.Append (secs).Append (' ').Append (tz).Append ('\t');
			sb.Append (comment);
			FullLine = sb.ToString ();
		}
		
		string prevStashCommitId;
		
		internal string PrevStashCommitId {
			get { return prevStashCommitId; }
			set {
				prevStashCommitId = value;
				if (FullLine != null) {
					if (prevStashCommitId != null)
						FullLine = prevStashCommitId + FullLine.Substring (40);
					else
						FullLine = new string ('0', 40) + FullLine.Substring (40);
				}
			}
		}

		
		internal static Stash Parse (string line)
		{
			// Parses a stash log line and creates a Stash object with the information
			
			Stash s = new Stash ();
			s.PrevStashCommitId = line.Substring (0, 40);
			if (s.PrevStashCommitId.All (c => c == '0')) // And id will all 0 means no parent (first stash of the stack)
				s.PrevStashCommitId = null;
			s.CommitId = line.Substring (41, 40);
			
			string aname = string.Empty;
			string amail = string.Empty;
			
			int i = line.IndexOf ('<');
			if (i != -1) {
				aname = line.Substring (82, i - 82 - 1);
				i++;
				int i2 = line.IndexOf ('>', i);
				if (i2 != -1)
					amail = line.Substring (i, i2 - i);
				
				i2 += 2;
				i = line.IndexOf (' ', i2);
				int secs = int.Parse (line.Substring (i2, i - i2));
				DateTime t = new DateTime (1970, 1, 1) + TimeSpan.FromSeconds (secs);
				string st = t.ToString ("yyyy-MM-ddTHH:mm:ss") + line.Substring (i + 1, 3) + ":" + line.Substring (i + 4, 2);
				s.DateTime = DateTimeOffset.Parse (st, System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
				s.Comment = line.Substring (i + 7);
				i = s.Comment.IndexOf (':');
				if (i != -1)
					s.Comment = s.Comment.Substring (i + 2);			
			}
			s.Author = new PersonIdent (aname, amail);
			s.FullLine = line;
			return s;
		}
		
		public MergeResult Apply (ProgressMonitor monitor)
		{
			return StashCollection.Apply (monitor, this);
		}
	}
	
	public class StashCollection: IEnumerable<Stash>
	{
		readonly JRepository _repo;
		
		internal StashCollection (JRepository repo)
		{
			this._repo = repo;
		}
		
		FileInfo StashLogFile {
			get {
				string stashLog = Path.Combine (_repo.getDirectory ().toString (), "logs");
				stashLog = Path.Combine (stashLog, "refs");
				return new FileInfo (Path.Combine (stashLog, "stash"));
			}
		}
		
		FileInfo StashRefFile {
			get {
				string file = Path.Combine (_repo.getDirectory ().toString (), "refs");
				return new FileInfo (Path.Combine (file, "stash"));
			}
		}
		
		public Stash Create (ProgressMonitor monitor)
		{
			return Create (monitor, null);
		}
		
		public Stash Create (ProgressMonitor monitor, string message)
		{
			if (monitor != null) {
				monitor.start (1);
				monitor.beginTask ("Stashing changes", 100);
			}
			
			UserConfig config = (UserConfig)_repo.getConfig ().get (UserConfig.KEY);
			RevWalk rw = new RevWalk (_repo);
			ObjectId headId = _repo.resolve (Constants.HEAD);
			var parent = rw.parseCommit (headId);
			
			PersonIdent author = new PersonIdent(config.getAuthorName () ?? "unknown", config.getAuthorEmail () ?? "unknown@(none).");
			
			if (string.IsNullOrEmpty (message)) {
				// Use the commit summary as message
				message = parent.abbreviate (7) + " " + parent.getShortMessage ();
				int i = message.IndexOfAny (new char[] { '\r', '\n' });
				if (i != -1)
					message = message.Substring (0, i);
			}
			
			// Create the index tree commit
			ObjectInserter inserter = _repo.newObjectInserter ();
			DirCache dc = _repo.readDirCache ();
			
			if (monitor != null)
				monitor.update (10);
				
			var tree_id = dc.writeTree (inserter);
			inserter.release ();
			
			if (monitor != null)
				monitor.update (10);
			
			string commitMsg = "index on " + _repo.getBranch () + ": " + message;
			ObjectId indexCommit = GitUtil.CreateCommit (_repo, commitMsg + "\n", new ObjectId[] {headId}, tree_id, author, author);

			if (monitor != null)
				monitor.update (20);
			
			// Create the working dir commit
			tree_id = WriteWorkingDirectoryTree (parent.getTree (), dc);
			commitMsg = "WIP on " + _repo.getBranch () + ": " + message;
			var wipCommit = GitUtil.CreateCommit(_repo, commitMsg + "\n", new ObjectId[] { headId, indexCommit }, tree_id, author, author);
			
			if (monitor != null)
				monitor.update (20);
			
			string prevCommit = null;
			FileInfo sf = StashRefFile;
			if (sf.Exists)
				prevCommit = File.ReadAllText (sf.FullName).Trim (' ','\t','\r','\n');
			
			Stash s = new Stash (prevCommit, wipCommit.getName (), author, commitMsg);
			
			FileInfo stashLog = StashLogFile;
			File.AppendAllText (stashLog.FullName, s.FullLine + "\n");
			File.WriteAllText (sf.FullName, s.CommitId + "\n");
			
			if (monitor != null)
				monitor.update (5);
			
			// Wipe all local changes
			GitUtil.HardReset (_repo, Constants.HEAD);
			
			monitor.endTask ();
			s.StashCollection = this;
			return s;
		}
		
		ObjectId WriteWorkingDirectoryTree (RevTree headTree, DirCache index)
		{
			DirCache dc = DirCache.newInCore ();
			DirCacheBuilder cb = dc.builder ();
			
			ObjectInserter oi = _repo.newObjectInserter ();
			try {
				TreeWalk tw = new TreeWalk (_repo);
				tw.reset ();
				tw.addTree (new FileTreeIterator (_repo));
				tw.addTree (headTree);
				tw.addTree (new DirCacheIterator (index));
				
				while (tw.next ()) {
					// Ignore untracked files
					if (tw.isSubtree ())
						tw.enterSubtree ();
					else if (tw.getFileMode (0) != JFileMode.MISSING && (tw.getFileMode (1) != JFileMode.MISSING || tw.getFileMode (2) != JFileMode.MISSING)) {
						WorkingTreeIterator f = (WorkingTreeIterator)tw.getTree(0, (java.lang.Class)typeof(WorkingTreeIterator));
						DirCacheIterator dcIter = (DirCacheIterator)tw.getTree(2, (java.lang.Class)typeof(DirCacheIterator));
						DirCacheEntry currentEntry = dcIter.getDirCacheEntry ();
						DirCacheEntry ce = new DirCacheEntry (tw.getPathString ());
						if (!f.isModified (currentEntry, true)) {
							ce.setLength (currentEntry.getLength ());
							ce.setLastModified (currentEntry.getLastModified ());
							ce.setFileMode (currentEntry.getFileMode ());
							ce.setObjectId (currentEntry.getObjectId ());
						}
						else {
							long sz = f.getEntryLength();
							ce.setLength (sz);
							ce.setLastModified (f.getEntryLastModified());
							ce.setFileMode (f.getEntryFileMode ());
							var data = f.openEntryStream();
							try {
								ce.setObjectId (oi.insert (Constants.OBJ_BLOB, sz, data));
							} finally {
								data.close ();
							}
						}
						cb.add (ce);
					}
				}
				
				cb.finish ();
				return dc.writeTree (oi);
			} finally {
				oi.release ();
			}
		}
		
		internal MergeResult Apply (ProgressMonitor monitor, Stash stash)
		{
			monitor.start (1);
			monitor.beginTask ("Applying stash", 100);
			ObjectId cid = _repo.resolve (stash.CommitId);
			RevWalk rw = new RevWalk (_repo);
			RevCommit wip = rw.parseCommit (cid);
			RevCommit oldHead = wip.getParent (0);
			rw.parseHeaders (oldHead);
			MergeResult res = GitUtil.MergeTrees (monitor, _repo, oldHead, wip, "Stash", false);
			monitor.endTask ();
			return res;
		}
		
		public void Remove (Stash s)
		{
			List<Stash> stashes = ReadStashes ();
			Remove (stashes, s);
		}
		
		public MergeResult Pop (ProgressMonitor monitor)
		{
			List<Stash> stashes = ReadStashes ();
			Stash last = stashes.Last ();
			MergeResult res = last.Apply (monitor);
			if (res.getMergeStatus () != MergeResult.MergeStatus.FAILED &&
				res.getMergeStatus () != MergeResult.MergeStatus.NOT_SUPPORTED)
				Remove (stashes, last);
			return res;
		}
		
		public void Clear ()
		{
			if (StashRefFile.Exists)
				StashRefFile.Delete ();
			if (StashLogFile.Exists)
				StashLogFile.Delete ();
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
		
		public IEnumerator<Stash> GetEnumerator ()
		{
			return ReadStashes ().GetEnumerator ();
		}
		
		List<Stash> ReadStashes ()
		{
			// Reads the registered stashes
			// Results are returned from the bottom to the top of the stack
			
			List<Stash> result = new List<Stash> ();
			FileInfo logFile = StashLogFile;
			if (!logFile.Exists)
				return result;
			
			Dictionary<string,Stash> stashes = new Dictionary<string, Stash> ();
			Stash first = null;
			foreach (string line in File.ReadAllLines (logFile.FullName)) {
				Stash s = Stash.Parse (line);
				s.StashCollection = this;
				if (s.PrevStashCommitId == null)
					first = s;
				else
					stashes.Add (s.PrevStashCommitId, s);
			}
			while (first != null) {
				result.Add (first);
				stashes.TryGetValue (first.CommitId, out first);
			}
			return result;
		}
		
		void WriteStashes (List<Stash> list)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (var s in list) {
				sb.Append (s.FullLine);
				sb.Append ('\n');
			}
			File.WriteAllText (StashLogFile.FullName, sb.ToString ());
		}
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
	}
}

