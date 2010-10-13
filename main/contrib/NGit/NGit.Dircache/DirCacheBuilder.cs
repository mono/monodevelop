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
using NGit;
using NGit.Dircache;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>
	/// Updates a
	/// <see cref="DirCache">DirCache</see>
	/// by adding individual
	/// <see cref="DirCacheEntry">DirCacheEntry</see>
	/// s.
	/// <p>
	/// A builder always starts from a clean slate and appends in every single
	/// <code>DirCacheEntry</code> which the final updated index must have to reflect
	/// its new content.
	/// <p>
	/// For maximum performance applications should add entries in path name order.
	/// Adding entries out of order is permitted, however a final sorting pass will
	/// be implicitly performed during
	/// <see cref="Finish()">Finish()</see>
	/// to correct any out-of-order
	/// entries. Duplicate detection is also delayed until the sorting is complete.
	/// </summary>
	/// <seealso cref="DirCacheEditor">DirCacheEditor</seealso>
	public class DirCacheBuilder : BaseDirCacheEditor
	{
		private bool sorted;

		/// <summary>Construct a new builder.</summary>
		/// <remarks>Construct a new builder.</remarks>
		/// <param name="dc">the cache this builder will eventually update.</param>
		/// <param name="ecnt">
		/// estimated number of entries the builder will have upon
		/// completion. This sizes the initial entry table.
		/// </param>
		protected internal DirCacheBuilder(DirCache dc, int ecnt) : base(dc, ecnt)
		{
		}

		/// <summary>Append one entry into the resulting entry list.</summary>
		/// <remarks>
		/// Append one entry into the resulting entry list.
		/// <p>
		/// The entry is placed at the end of the entry list. If the entry causes the
		/// list to now be incorrectly sorted a final sorting phase will be
		/// automatically enabled within
		/// <see cref="Finish()">Finish()</see>
		/// .
		/// <p>
		/// The internal entry table is automatically expanded if there is
		/// insufficient space for the new addition.
		/// </remarks>
		/// <param name="newEntry">the new entry to add.</param>
		/// <exception cref="System.ArgumentException">If the FileMode of the entry was not set by the caller.
		/// 	</exception>
		public virtual void Add(DirCacheEntry newEntry)
		{
			if (newEntry.GetRawMode() == 0)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().fileModeNotSetForPath
					, newEntry.GetPathString()));
			}
			BeforeAdd(newEntry);
			FastAdd(newEntry);
		}

		/// <summary>Add a range of existing entries from the destination cache.</summary>
		/// <remarks>
		/// Add a range of existing entries from the destination cache.
		/// <p>
		/// The entries are placed at the end of the entry list. If any of the
		/// entries causes the list to now be incorrectly sorted a final sorting
		/// phase will be automatically enabled within
		/// <see cref="Finish()">Finish()</see>
		/// .
		/// <p>
		/// This method copies from the destination cache, which has not yet been
		/// updated with this editor's new table. So all offsets into the destination
		/// cache are not affected by any updates that may be currently taking place
		/// in this editor.
		/// <p>
		/// The internal entry table is automatically expanded if there is
		/// insufficient space for the new additions.
		/// </remarks>
		/// <param name="pos">first entry to copy from the destination cache.</param>
		/// <param name="cnt">number of entries to copy.</param>
		public virtual void Keep(int pos, int cnt)
		{
			BeforeAdd(cache.GetEntry(pos));
			FastKeep(pos, cnt);
		}

		/// <summary>Recursively add an entire tree into this builder.</summary>
		/// <remarks>
		/// Recursively add an entire tree into this builder.
		/// <p>
		/// If pathPrefix is "a/b" and the tree contains file "c" then the resulting
		/// DirCacheEntry will have the path "a/b/c".
		/// <p>
		/// All entries are inserted at stage 0, therefore assuming that the
		/// application will not insert any other paths with the same pathPrefix.
		/// </remarks>
		/// <param name="pathPrefix">
		/// UTF-8 encoded prefix to mount the tree's entries at. If the
		/// path does not end with '/' one will be automatically inserted
		/// as necessary.
		/// </param>
		/// <param name="stage">stage of the entries when adding them.</param>
		/// <param name="reader">
		/// reader the tree(s) will be read from during recursive
		/// traversal. This must be the same repository that the resulting
		/// DirCache would be written out to (or used in) otherwise the
		/// caller is simply asking for deferred MissingObjectExceptions.
		/// Caller is responsible for releasing this reader when done.
		/// </param>
		/// <param name="tree">
		/// the tree to recursively add. This tree's contents will appear
		/// under <code>pathPrefix</code>. The ObjectId must be that of a
		/// tree; the caller is responsible for dereferencing a tag or
		/// commit (if necessary).
		/// </param>
		/// <exception cref="System.IO.IOException">a tree cannot be read to iterate through its entries.
		/// 	</exception>
		public virtual void AddTree(byte[] pathPrefix, int stage, ObjectReader reader, AnyObjectId
			 tree)
		{
			TreeWalk tw = new TreeWalk(reader);
			tw.Reset();
			tw.AddTree(new CanonicalTreeParser(pathPrefix, reader, tree.ToObjectId()));
			tw.Recursive = true;
			if (tw.Next())
			{
				DirCacheEntry newEntry = ToEntry(stage, tw);
				BeforeAdd(newEntry);
				FastAdd(newEntry);
				while (tw.Next())
				{
					FastAdd(ToEntry(stage, tw));
				}
			}
		}

		private DirCacheEntry ToEntry(int stage, TreeWalk tw)
		{
			DirCacheEntry e = new DirCacheEntry(tw.RawPath, stage);
			AbstractTreeIterator i;
			i = tw.GetTree<AbstractTreeIterator>(0);
			e.SetFileMode(tw.GetFileMode(0));
			e.SetObjectIdFromRaw(i.IdBuffer(), i.IdOffset());
			return e;
		}

		public override void Finish()
		{
			if (!sorted)
			{
				Resort();
			}
			Replace();
		}

		private void BeforeAdd(DirCacheEntry newEntry)
		{
			if (sorted && entryCnt > 0)
			{
				DirCacheEntry lastEntry = entries[entryCnt - 1];
				int cr = DirCache.Cmp(lastEntry, newEntry);
				if (cr > 0)
				{
					// The new entry sorts before the old entry; we are
					// no longer sorted correctly. We'll need to redo
					// the sorting before we can close out the build.
					//
					sorted = false;
				}
				else
				{
					if (cr == 0)
					{
						// Same file path; we can only insert this if the
						// stages won't be violated.
						//
						int peStage = lastEntry.GetStage();
						int dceStage = newEntry.GetStage();
						if (peStage == dceStage)
						{
							throw Bad(newEntry, JGitText.Get().duplicateStagesNotAllowed);
						}
						if (peStage == 0 || dceStage == 0)
						{
							throw Bad(newEntry, JGitText.Get().mixedStagesNotAllowed);
						}
						if (peStage > dceStage)
						{
							sorted = false;
						}
					}
				}
			}
		}

		private void Resort()
		{
			Arrays.Sort(entries, 0, entryCnt, DirCache.ENT_CMP);
			for (int entryIdx = 1; entryIdx < entryCnt; entryIdx++)
			{
				DirCacheEntry pe = entries[entryIdx - 1];
				DirCacheEntry ce = entries[entryIdx];
				int cr = DirCache.Cmp(pe, ce);
				if (cr == 0)
				{
					// Same file path; we can only allow this if the stages
					// are 1-3 and no 0 exists.
					//
					int peStage = pe.GetStage();
					int ceStage = ce.GetStage();
					if (peStage == ceStage)
					{
						throw Bad(ce, JGitText.Get().duplicateStagesNotAllowed);
					}
					if (peStage == 0 || ceStage == 0)
					{
						throw Bad(ce, JGitText.Get().mixedStagesNotAllowed);
					}
				}
			}
			sorted = true;
		}

		private static InvalidOperationException Bad(DirCacheEntry a, string msg)
		{
			return new InvalidOperationException(msg + ": " + a.GetStage() + " " + a.GetPathString
				());
		}
	}
}
