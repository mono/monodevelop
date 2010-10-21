/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Diff;
using NGit.Dircache;
using NGit.Errors;
using NGit.Merge;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>A three-way merger performing a content-merge if necessary</summary>
	public class ResolveMerger : ThreeWayMerger
	{
		/// <summary>
		/// If the merge fails abnormally (means: not because of unresolved
		/// conflicts) this enum is used to explain why it failed
		/// </summary>
		public enum MergeFailureReason
		{
			DIRTY_INDEX,
			DIRTY_WORKTREE
		}

		private NameConflictTreeWalk tw;

		private string[] commitNames;

		private const int T_BASE = 0;

		private const int T_OURS = 1;

		private const int T_THEIRS = 2;

		private const int T_INDEX = 3;

		private const int T_FILE = 4;

		private DirCacheBuilder builder;

		private ObjectId resultTree;

		private IList<string> unmergedPathes = new AList<string>();

		private IList<string> modifiedFiles = new List<string>();

		private IDictionary<string, DirCacheEntry> toBeCheckedOut = new Dictionary<string
			, DirCacheEntry>();

		private IDictionary<string, MergeResult<Sequence>> mergeResults = new Dictionary<
			string, MergeResult<Sequence>>();

		private IDictionary<string, ResolveMerger.MergeFailureReason> failingPathes = new 
			Dictionary<string, ResolveMerger.MergeFailureReason>();

		private ObjectInserter oi;

		private bool enterSubtree;

		private bool inCore;

		private DirCache dircache;

		private WorkingTreeIterator workingTreeIterator;

		/// <param name="local"></param>
		/// <param name="inCore"></param>
		protected internal ResolveMerger(Repository local, bool inCore) : base(local)
		{
			commitNames = new string[] { "BASE", "OURS", "THEIRS" };
			oi = GetObjectInserter();
			this.inCore = inCore;
			if (inCore)
			{
				dircache = DirCache.NewInCore();
			}
		}

		/// <param name="local"></param>
		protected internal ResolveMerger(Repository local) : this(local, false)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override bool MergeImpl()
		{
			bool implicitDirCache = false;
			if (dircache == null)
			{
				dircache = GetRepository().LockDirCache();
				implicitDirCache = true;
			}
			try
			{
				builder = dircache.Builder();
				DirCacheBuildIterator buildIt = new DirCacheBuildIterator(builder);
				tw = new NameConflictTreeWalk(db);
				tw.Reset();
				tw.AddTree(MergeBase());
				tw.AddTree(sourceTrees[0]);
				tw.AddTree(sourceTrees[1]);
				tw.AddTree(buildIt);
				if (workingTreeIterator != null)
				{
					tw.AddTree(workingTreeIterator);
				}
				while (tw.Next())
				{
					if (!ProcessEntry(tw.GetTree<CanonicalTreeParser>(T_BASE), tw.GetTree<CanonicalTreeParser
						>(T_OURS), tw.GetTree<CanonicalTreeParser>(T_THEIRS), tw.GetTree<DirCacheBuildIterator
						>(T_INDEX), (workingTreeIterator == null) ? null : tw.GetTree<WorkingTreeIterator
						>(T_FILE)))
					{
						CleanUp();
						return false;
					}
					if (tw.IsSubtree && enterSubtree)
					{
						tw.EnterSubtree();
					}
				}
				if (!inCore)
				{
					// All content-merges are successfully done. If we can now write the
					// new index we are on quite safe ground. Even if the checkout of
					// files coming from "theirs" fails the user can work around such
					// failures by checking out the index again.
					if (!builder.Commit())
					{
						CleanUp();
						throw new IndexWriteException();
					}
					builder = null;
					// No problem found. The only thing left to be done is to checkout
					// all files from "theirs" which have been selected to go into the
					// new index.
					Checkout();
				}
				else
				{
					builder.Finish();
					builder = null;
				}
				if (GetUnmergedPathes().IsEmpty())
				{
					resultTree = dircache.WriteTree(oi);
					return true;
				}
				else
				{
					resultTree = null;
					return false;
				}
			}
			finally
			{
				if (implicitDirCache)
				{
					dircache.Unlock();
				}
			}
		}

		/// <exception cref="NGit.Errors.NoWorkTreeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void Checkout()
		{
			foreach (KeyValuePair<string, DirCacheEntry> entry in toBeCheckedOut.EntrySet())
			{
				FilePath f = new FilePath(db.WorkTree, entry.Key);
				CreateDir(f.GetParentFile());
				DirCacheCheckout.CheckoutEntry(db, f, entry.Value, true);
				modifiedFiles.AddItem(entry.Key);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CreateDir(FilePath f)
		{
			if (!f.IsDirectory() && !f.Mkdirs())
			{
				FilePath p = f;
				while (p != null && !p.Exists())
				{
					p = p.GetParentFile();
				}
				if (p == null || p.IsDirectory())
				{
					throw new IOException(JGitText.Get().cannotCreateDirectory);
				}
				p.Delete();
				if (!f.Mkdirs())
				{
					throw new IOException(JGitText.Get().cannotCreateDirectory);
				}
			}
		}

		/// <summary>Reverts the worktree after an unsuccessful merge.</summary>
		/// <remarks>
		/// Reverts the worktree after an unsuccessful merge. We know that for all
		/// modified files the old content was in the old index and the index
		/// contained only stage 0. In case if inCore operation just clear
		/// the history of modified files.
		/// </remarks>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">NGit.Errors.CorruptObjectException
		/// 	</exception>
		/// <exception cref="NGit.Errors.NoWorkTreeException">NGit.Errors.NoWorkTreeException
		/// 	</exception>
		private void CleanUp()
		{
			if (inCore)
			{
				modifiedFiles.Clear();
				return;
			}
			DirCache dc = db.ReadDirCache();
			ObjectReader or = db.ObjectDatabase.NewReader();
			Iterator<string> mpathsIt = modifiedFiles.Iterator();
			while (mpathsIt.HasNext())
			{
				string mpath = mpathsIt.Next();
				DirCacheEntry entry = dc.GetEntry(mpath);
				FileOutputStream fos = new FileOutputStream(new FilePath(db.WorkTree, mpath));
				try
				{
					or.Open(entry.GetObjectId()).CopyTo(fos);
				}
				finally
				{
					fos.Close();
				}
				mpathsIt.Remove();
			}
		}

		/// <summary>adds a new path with the specified stage to the index builder</summary>
		/// <param name="path"></param>
		/// <param name="p"></param>
		/// <param name="stage"></param>
		/// <returns>the entry which was added to the index</returns>
		private DirCacheEntry Add(byte[] path, CanonicalTreeParser p, int stage)
		{
			if (p != null && !p.GetEntryFileMode().Equals(FileMode.TREE))
			{
				DirCacheEntry e = new DirCacheEntry(path, stage);
				e.SetFileMode(p.GetEntryFileMode());
				e.SetObjectId(p.GetEntryObjectId());
				builder.Add(e);
				return e;
			}
			return null;
		}

		/// <summary>Processes one path and tries to merge.</summary>
		/// <remarks>
		/// Processes one path and tries to merge. This method will do all do all
		/// trivial (not content) merges and will also detect if a merge will fail.
		/// The merge will fail when one of the following is true
		/// <ul>
		/// <li>the index entry does not match the entry in ours. When merging one
		/// branch into the current HEAD, ours will point to HEAD and theirs will
		/// point to the other branch. It is assumed that the index matches the HEAD
		/// because it will only not match HEAD if it was populated before the merge
		/// operation. But the merge commit should not accidentally contain
		/// modifications done before the merge. Check the &lt;a href=
		/// "http://www.kernel.org/pub/software/scm/git/docs/git-read-tree.html#_3_way_merge"
		/// &gt;git read-tree</a> documentation for further explanations.</li>
		/// <li>A conflict was detected and the working-tree file is dirty. When a
		/// conflict is detected the content-merge algorithm will try to write a
		/// merged version into the working-tree. If the file is dirty we would
		/// override unsaved data.</li>
		/// </remarks>
		/// <param name="base">the common base for ours and theirs</param>
		/// <param name="ours">
		/// the ours side of the merge. When merging a branch into the
		/// HEAD ours will point to HEAD
		/// </param>
		/// <param name="theirs">
		/// the theirs side of the merge. When merging a branch into the
		/// current HEAD theirs will point to the branch which is merged
		/// into HEAD.
		/// </param>
		/// <param name="index">the index entry</param>
		/// <param name="work">the file in the working tree</param>
		/// <returns>
		/// <code>false</code> if the merge will fail because the index entry
		/// didn't match ours or the working-dir file was dirty and a
		/// conflict occured
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">NGit.Errors.CorruptObjectException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		private bool ProcessEntry(CanonicalTreeParser @base, CanonicalTreeParser ours, CanonicalTreeParser
			 theirs, DirCacheBuildIterator index, WorkingTreeIterator work)
		{
			enterSubtree = true;
			int modeO = tw.GetRawMode(T_OURS);
			int modeI = tw.GetRawMode(T_INDEX);
			// Each index entry has to match ours, means: it has to be clean
			if (NonTree(modeI) && !(tw.IdEqual(T_INDEX, T_OURS) && modeO == modeI))
			{
				failingPathes.Put(tw.PathString, ResolveMerger.MergeFailureReason.DIRTY_INDEX);
				return false;
			}
			int modeT = tw.GetRawMode(T_THEIRS);
			if (NonTree(modeO) && modeO == modeT && tw.IdEqual(T_OURS, T_THEIRS))
			{
				// ours and theirs are equal: it doesn'nt matter
				// which one we choose. OURS is choosen here.
				Add(tw.RawPath, ours, DirCacheEntry.STAGE_0);
				// no checkout needed!
				return true;
			}
			int modeB = tw.GetRawMode(T_BASE);
			if (NonTree(modeO) && modeB == modeT && tw.IdEqual(T_BASE, T_THEIRS))
			{
				// THEIRS was not changed compared to base. All changes must be in
				// OURS. Choose OURS.
				Add(tw.RawPath, ours, DirCacheEntry.STAGE_0);
				return true;
			}
			if (NonTree(modeT) && modeB == modeO && tw.IdEqual(T_BASE, T_OURS))
			{
				// OURS was not changed compared to base. All changes must be in
				// THEIRS. Choose THEIRS.
				DirCacheEntry e = Add(tw.RawPath, theirs, DirCacheEntry.STAGE_0);
				if (e != null)
				{
					toBeCheckedOut.Put(tw.PathString, e);
				}
				return true;
			}
			if (tw.IsSubtree)
			{
				// file/folder conflicts: here I want to detect only file/folder
				// conflict between ours and theirs. file/folder conflicts between
				// base/index/workingTree and something else are not relevant or
				// detected later
				if (NonTree(modeO) && !NonTree(modeT))
				{
					if (NonTree(modeB))
					{
						Add(tw.RawPath, @base, DirCacheEntry.STAGE_1);
					}
					Add(tw.RawPath, ours, DirCacheEntry.STAGE_2);
					unmergedPathes.AddItem(tw.PathString);
					enterSubtree = false;
					return true;
				}
				if (NonTree(modeT) && !NonTree(modeO))
				{
					if (NonTree(modeB))
					{
						Add(tw.RawPath, @base, DirCacheEntry.STAGE_1);
					}
					Add(tw.RawPath, theirs, DirCacheEntry.STAGE_3);
					unmergedPathes.AddItem(tw.PathString);
					enterSubtree = false;
					return true;
				}
				// ours and theirs are both folders or both files (and treewalk
				// tells us we are in a subtree because of index or working-dir).
				// If they are both folders no content-merge is required - we can
				// return here.
				if (!NonTree(modeO))
				{
					return true;
				}
			}
			// ours and theirs are both files, just fall out of the if block
			// and do the content merge
			if (NonTree(modeO) && NonTree(modeT))
			{
				if (!inCore)
				{
					// We are going to update the worktree. Make sure the worktree
					// is not modified
					if (work != null && (!NonTree(work.GetEntryRawMode()) || work.IsModified(index.GetDirCacheEntry
						(), true, true, db.FileSystem)))
					{
						failingPathes.Put(tw.PathString, ResolveMerger.MergeFailureReason.DIRTY_WORKTREE);
						return false;
					}
				}
				if (!ContentMerge(@base, ours, theirs))
				{
					unmergedPathes.AddItem(tw.PathString);
				}
				modifiedFiles.AddItem(tw.PathString);
			}
			return true;
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		/// <exception cref="System.InvalidOperationException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private bool ContentMerge(CanonicalTreeParser @base, CanonicalTreeParser ours, CanonicalTreeParser
			 theirs)
		{
			MergeFormatter fmt = new MergeFormatter();
			// do the merge
			MergeResult<RawText> result = MergeAlgorithm.Merge(RawTextComparator.DEFAULT, GetRawText
				(@base.GetEntryObjectId(), db), GetRawText(ours.GetEntryObjectId(), db), GetRawText
				(theirs.GetEntryObjectId(), db));
			FilePath of = null;
			FileOutputStream fos;
			if (!inCore)
			{
				FilePath workTree = db.WorkTree;
				if (workTree == null)
				{
					// TODO: This should be handled by WorkingTreeIterators which
					// support write operations
					throw new NotSupportedException();
				}
				of = new FilePath(workTree, tw.PathString);
				fos = new FileOutputStream(of);
				try
				{
					fmt.FormatMerge(fos, result, Arrays.AsList(commitNames), Constants.CHARACTER_ENCODING
						);
				}
				finally
				{
					fos.Close();
				}
			}
			else
			{
				if (!result.ContainsConflicts())
				{
					// When working inCore, only trivial merges can be handled,
					// so we generate objects only in conflict free cases
					of = FilePath.CreateTempFile("merge_", "_temp", null);
					fos = new FileOutputStream(of);
					try
					{
						fmt.FormatMerge(fos, result, Arrays.AsList(commitNames), Constants.CHARACTER_ENCODING
							);
					}
					finally
					{
						fos.Close();
					}
				}
			}
			if (result.ContainsConflicts())
			{
				// a conflict occured, the file will contain conflict markers
				// the index will be populated with the three stages and only the
				// workdir (if used) contains the halfways merged content
				Add(tw.RawPath, @base, DirCacheEntry.STAGE_1);
				Add(tw.RawPath, ours, DirCacheEntry.STAGE_2);
				Add(tw.RawPath, theirs, DirCacheEntry.STAGE_3);
				mergeResults.Put(tw.PathString, result.Upcast ());
				return false;
			}
			else
			{
				// no conflict occured, the file will contain fully merged content.
				// the index will be populated with the new merged version
				DirCacheEntry dce = new DirCacheEntry(tw.PathString);
				dce.SetFileMode(tw.GetFileMode(0));
				dce.SetLastModified(of.LastModified());
				dce.SetLength((int)of.Length());
				InputStream @is = new FileInputStream(of);
				try
				{
					dce.SetObjectId(oi.Insert(Constants.OBJ_BLOB, of.Length(), @is));
				}
				finally
				{
					@is.Close();
					if (inCore)
					{
						of.Delete();
					}
				}
				builder.Add(dce);
				return true;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static RawText GetRawText(ObjectId id, Repository db)
		{
			if (id.Equals(ObjectId.ZeroId))
			{
				return new RawText(new byte[] {  });
			}
			return new RawText(db.Open(id, Constants.OBJ_BLOB).GetCachedBytes());
		}

		private static bool NonTree(int mode)
		{
			return mode != 0 && !FileMode.TREE.Equals(mode);
		}

		public override ObjectId GetResultTreeId()
		{
			return (resultTree == null) ? null : resultTree.ToObjectId();
		}

		/// <param name="commitNames">
		/// the names of the commits as they would appear in conflict
		/// markers
		/// </param>
		public virtual void SetCommitNames(string[] commitNames)
		{
			this.commitNames = commitNames;
		}

		/// <returns>
		/// the names of the commits as they would appear in conflict
		/// markers.
		/// </returns>
		public virtual string[] GetCommitNames()
		{
			return commitNames;
		}

		/// <returns>
		/// the paths with conflicts. This is a subset of the files listed
		/// by
		/// <see cref="GetModifiedFiles()">GetModifiedFiles()</see>
		/// </returns>
		public virtual IList<string> GetUnmergedPathes()
		{
			return unmergedPathes;
		}

		/// <returns>
		/// the paths of files which have been modified by this merge. A
		/// file will be modified if a content-merge works on this path or if
		/// the merge algorithm decides to take the theirs-version. This is a
		/// superset of the files listed by
		/// <see cref="GetUnmergedPathes()">GetUnmergedPathes()</see>
		/// .
		/// </returns>
		public virtual IList<string> GetModifiedFiles()
		{
			return modifiedFiles;
		}

		/// <returns>
		/// a map which maps the paths of files which have to be checked out
		/// because the merge created new fully-merged content for this file
		/// into the index. This means: the merge wrote a new stage 0 entry
		/// for this path.
		/// </returns>
		public virtual IDictionary<string, DirCacheEntry> GetToBeCheckedOut()
		{
			return toBeCheckedOut;
		}

		/// <returns>the mergeResults</returns>
		public virtual IDictionary<string, MergeResult<Sequence>> GetMergeResults()
		{
			return mergeResults;
		}

		/// <returns>
		/// lists paths causing this merge to fail abnormally (not because of
		/// a conflict). <code>null</code> is returned if this merge didn't
		/// fail abnormally.
		/// </returns>
		public virtual IDictionary<string, ResolveMerger.MergeFailureReason> GetFailingPathes
			()
		{
			return (failingPathes.Count == 0) ? null : failingPathes;
		}

		/// <summary>Sets the DirCache which shall be used by this merger.</summary>
		/// <remarks>
		/// Sets the DirCache which shall be used by this merger. If the DirCache is
		/// not set explicitly this merger will implicitly get and lock a default
		/// DirCache. If the DirCache is explicitly set the caller is responsible to
		/// lock it in advance. Finally the merger will call
		/// <see cref="NGit.Dircache.DirCache.Commit()">NGit.Dircache.DirCache.Commit()</see>
		/// which requires that the DirCache is locked. If
		/// the
		/// <see cref="MergeImpl()">MergeImpl()</see>
		/// returns without throwing an exception the lock
		/// will be released. In case of exceptions the caller is responsible to
		/// release the lock.
		/// </remarks>
		/// <param name="dc">the DirCache to set</param>
		public virtual void SetDirCache(DirCache dc)
		{
			this.dircache = dc;
		}

		/// <summary>Sets the WorkingTreeIterator to be used by this merger.</summary>
		/// <remarks>
		/// Sets the WorkingTreeIterator to be used by this merger. If no
		/// WorkingTreeIterator is set this merger will ignore the working tree and
		/// fail if a content merge is necessary.
		/// <p>
		/// TODO: enhance WorkingTreeIterator to support write operations. Then this
		/// merger will be able to merge with a different working tree abstraction.
		/// </remarks>
		/// <param name="workingTreeIterator">the workingTreeIt to set</param>
		public virtual void SetWorkingTreeIterator(WorkingTreeIterator workingTreeIterator
			)
		{
			this.workingTreeIterator = workingTreeIterator;
		}
	}
}
