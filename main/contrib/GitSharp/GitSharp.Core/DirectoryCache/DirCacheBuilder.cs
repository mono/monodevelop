/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.DirectoryCache
{
    /// <summary>
    /// Updates a <seealso cref="DirCache"/> by adding individual <seealso cref="DirCacheEntry"/>s.
    /// <para />
    /// A builder always starts from a clean slate and appends in every single
    /// <seealso cref="DirCacheEntry" /> which the final updated index must have to reflect
    /// its new content.
    /// <para />
    /// For maximum performance applications should add entries in path name order.
    /// Adding entries out of order is permitted, however a final sorting pass will
    /// be implicitly performed during <seealso cref="finish()"/> to correct any out-of-order
    /// entries. Duplicate detection is also delayed until the sorting is complete.
    /// </summary>
    /// <seealso cref="DirCacheEditor"/>
    public class DirCacheBuilder : BaseDirCacheEditor
    {
        private bool _sorted;

        /// <summary>
        /// Construct a new builder.
        /// </summary>
        /// <param name="dc">
        /// the cache this builder will eventually update.
        /// </param>
        /// <param name="ecnt">
        /// Estimated number of entries the builder will have upon
        /// completion. This sizes the initial entry table.
        /// </param>
        public DirCacheBuilder(DirCache dc, int ecnt)
            : base(dc, ecnt)
        {
        }

        /// <summary>
        /// Append one entry into the resulting entry list.
        /// <para />
        /// The entry is placed at the end of the entry list. If the entry causes the
        /// list to now be incorrectly sorted a final sorting phase will be
        /// automatically enabled within <seealso cref="finish()"/>.
        /// <para />
        /// The internal entry table is automatically expanded if there is
        /// insufficient space for the new addition.
        /// </summary>
        /// <param name="newEntry">the new entry to add.</param>
        public void add(DirCacheEntry newEntry)
        {
            if (newEntry.getRawMode() == 0)
            {
                throw new ArgumentException("FileMode not set for path "
                    + newEntry.getPathString());
            }

            BeforeAdd(newEntry);
            FastAdd(newEntry);
        }

        ///	<summary>
        /// Add a range of existing entries from the destination cache.
        /// <para />
        /// The entries are placed at the end of the entry list. If any of the
        /// entries causes the list to now be incorrectly sorted a final sorting
        /// phase will be automatically enabled within <seealso cref="finish()"/>.
        /// <para />
        /// This method copies from the destination cache, which has not yet been
        /// updated with this editor's new table. So all offsets into the destination
        /// cache are not affected by any updates that may be currently taking place
        /// in this editor.
        /// <para />
        /// The internal entry table is automatically expanded if there is
        /// insufficient space for the new additions.
        /// </summary>
        /// <param name="pos">
        /// First entry to copy from the destination cache.
        /// </param>
        /// <param name="cnt">Number of entries to copy.</param>
        public void keep(int pos, int cnt)
        {
            BeforeAdd(Cache.getEntry(pos));
            FastKeep(pos, cnt);
        }

        ///	<summary>
        /// Recursively add an entire tree into this builder.
        /// <para />
        /// If pathPrefix is "a/b" and the tree contains file "c" then the resulting
        /// DirCacheEntry will have the path "a/b/c".
        /// <para />
        /// All entries are inserted at stage 0, therefore assuming that the
        /// application will not insert any other paths with the same pathPrefix.
        /// </summary>
        /// <param name="pathPrefix">
        /// UTF-8 encoded prefix to mount the tree's entries at. If the
        /// path does not end with '/' one will be automatically inserted
        /// as necessary.
        /// </param>
        /// <param name="stage">Stage of the entries when adding them.</param>
        /// <param name="db">
        /// Repository the tree(s) will be read from during recursive
        /// traversal. This must be the same repository that the resulting
        /// <see cref="DirCache"/> would be written out to (or used in) otherwise 
        /// the caller is simply asking for deferred MissingObjectExceptions.
        /// </param>
        /// <param name="tree">
        /// The tree to recursively add. This tree's contents will appear
        /// under <paramref name="pathPrefix"/>. The ObjectId must be that of a
        /// tree; the caller is responsible for dereferencing a tag or
        /// commit (if necessary).
        /// </param>
        /// <exception cref="IOException">
        /// A tree cannot be read to iterate through its entries.
        /// </exception>
        public void addTree(byte[] pathPrefix, int stage, Repository db, AnyObjectId tree)
        {
            var tw = new TreeWalk.TreeWalk(db);
            tw.reset();
            var curs = new WindowCursor();
            try
            {
                tw.addTree(new CanonicalTreeParser(pathPrefix, db, tree.ToObjectId(), curs));
            }
            finally
            {
                curs.Release();
            }
            tw.Recursive = true;

            if (!tw.next()) return;

            DirCacheEntry newEntry = ToEntry(stage, tw);
            BeforeAdd(newEntry);
            FastAdd(newEntry);
            while (tw.next())
            {
                FastAdd(ToEntry(stage, tw));
            }
        }

        private static DirCacheEntry ToEntry(int stage, TreeWalk.TreeWalk tw)
        {
            var e = new DirCacheEntry(tw.getRawPath(), stage);
            var iterator = tw.getTree<AbstractTreeIterator>(0, typeof(AbstractTreeIterator));
            e.setFileMode(tw.getFileMode(0));
            e.setObjectIdFromRaw(iterator.idBuffer(), iterator.idOffset());
            return e;
        }

        public override void finish()
        {
            if (!_sorted)
            {
                Resort();
            }
            Replace();
        }

        private void BeforeAdd(DirCacheEntry newEntry)
        {
            if (_sorted && EntryCnt > 0)
            {
                DirCacheEntry lastEntry = Entries[EntryCnt - 1];
                int cr = DirCache.Compare(lastEntry, newEntry);
                if (cr > 0)
                {
                    // The new entry sorts before the old entry; we are
                    // no longer sorted correctly. We'll need to redo
                    // the sorting before we can close out the build.
                    //
                    _sorted = false;
                }
                else if (cr == 0)
                {
                    // Same file path; we can only insert this if the
                    // stages won't be violated.
                    //
                    int peStage = lastEntry.getStage();
                    int dceStage = newEntry.getStage();
                    if (peStage == dceStage)
                        throw Bad(newEntry, "Duplicate stages not allowed");
                    if (peStage == 0 || dceStage == 0)
                        throw Bad(newEntry, "Mixed stages not allowed");
                    if (peStage > dceStage)
                        _sorted = false;
                }
            }
        }

        private void Resort()
        {
            Array.Sort(Entries, 0, EntryCnt, new GenericComparer<DirCacheEntry>(DirCache.EntryComparer));

            for (int entryIdx = 1; entryIdx < EntryCnt; entryIdx++)
            {
                DirCacheEntry pe = Entries[entryIdx - 1];
                DirCacheEntry ce = Entries[entryIdx];
                int cr = DirCache.Compare(pe, ce);
                if (cr == 0)
                {
                    // Same file path; we can only allow this if the stages
                    // are 1-3 and no 0 exists.
                    //
                    int peStage = pe.getStage();
                    int ceStage = ce.getStage();

                    if (peStage == ceStage)
                    {
                        throw Bad(ce, "Duplicate stages not allowed");
                    }

                    if (peStage == 0 || ceStage == 0)
                    {
                        throw Bad(ce, "Mixed stages not allowed");
                    }
                }
            }

            _sorted = true;
        }

        private static InvalidOperationException Bad(DirCacheEntry a, string msg)
        {
            return new InvalidOperationException(msg + ": " + a.getStage() + " " + a.getPathString());
        }
    }
}