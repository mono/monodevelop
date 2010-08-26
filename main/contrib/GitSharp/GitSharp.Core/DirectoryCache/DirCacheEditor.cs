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
using System.Collections.Generic;

namespace GitSharp.Core.DirectoryCache
{
    /// <summary>
    /// Updates a <see cref="DirCache"/> by supplying discrete edit commands.
    /// <para/>
    /// An editor updates a <see cref="DirCache"/> by taking a list of
    /// <see cref="PathEdit"/> commands and executing them against the entries
    /// of the destination cache to produce a new cache. This edit style allows
    /// applications to insert a few commands and then have the editor compute
    /// the proper entry indexes necessary to perform an efficient in-order
    /// update of the index records. This can be easier to use than
    /// <see cref="DirCacheBuilder"/>.
    /// </summary>
    /// <seealso cref="DirCacheBuilder"/>
    public class DirCacheEditor : BaseDirCacheEditor
    {
        private static readonly Comparison<PathEdit> EditComparison = (o1, o2) =>
        {
            byte[] a = o1.Path;
            byte[] b = o2.Path;
            return DirCache.Compare(a, a.Length, b, b.Length);
        };

        private readonly List<PathEdit> _edits;

        /// <summary>
        /// Construct a new editor.
        /// </summary>
        /// <param name="dirCache">
        /// The cache this editor will eventually update.
        /// </param>
        /// <param name="entryCount">
        /// Estimated number of entries the editor will have upon
        /// completion. This sizes the initial entry table.
        /// </param>
        public DirCacheEditor(DirCache dirCache, int entryCount)
            : base(dirCache, entryCount)
        {
            _edits = new List<PathEdit>();
        }

        /// <summary>
        /// Append one edit command to the list of commands to be applied.
        /// <para />
        /// Edit commands may be added in any order chosen by the application. They
        /// are automatically rearranged by the builder to provide the most efficient
        /// update possible.
        /// </summary>
        /// <param name="edit">Another edit command.</param>
        public void add(PathEdit edit)
        {
            _edits.Add(edit);
        }

        public override bool commit()
        {
            if (_edits.Count == 0) // isEmpty()
            {
                // No changes? Don't rewrite the index.
                //
                Cache.unlock();
                return true;
            }
            return base.commit();
        }

        public override void finish()
        {
            if (_edits.Count <= 0) return;
            ApplyEdits();
            Replace();
        }

        private void ApplyEdits()
        {
            _edits.Sort(EditComparison);

            int maxIdx = Cache.getEntryCount();
            int lastIdx = 0;
            foreach (PathEdit e in _edits)
            {
                int eIdx = Cache.findEntry(e.Path, e.Path.Length);
                bool missing = eIdx < 0;
                if (eIdx < 0)
                {
                    eIdx = -(eIdx + 1);
                }
                int cnt = Math.Min(eIdx, maxIdx) - lastIdx;
                if (cnt > 0)
                {
                    FastKeep(lastIdx, cnt);
                }
                lastIdx = missing ? eIdx : Cache.nextEntry(eIdx);

                if (e is DeletePath) continue;
                if (e is DeleteTree)
                {
                    lastIdx = Cache.nextEntry(e.Path, e.Path.Length, eIdx);
                    continue;
                }

                DirCacheEntry ent;
                if (missing)
                {
                    ent = new DirCacheEntry(e.Path);
                    e.Apply(ent);
                    if (ent.getRawMode() == 0)
                        throw new ArgumentException("FileMode not set"
                                + " for path " + ent.getPathString());
                }
                else
                {
                    ent = Cache.getEntry(eIdx);
                    e.Apply(ent);
                }
                FastAdd(ent);
            }

            int count = maxIdx - lastIdx;
            if (count > 0)
            {
                FastKeep(lastIdx, count);
            }
        }

        #region Nested Types

        /// <summary>
        /// Any index record update.
        /// <para />
        /// Applications should subclass and provide their own implementation for the
        /// <see cref="Apply"/> method. The editor will invoke apply once
        /// for each record in the index which matches the path name. If there are
        /// multiple records (for example in stages 1, 2 and 3), the edit instance
        /// will be called multiple times, once for each stage.
        /// </summary>
        public abstract class PathEdit
        {
            private readonly byte[] _path;

            /// <summary>
            /// Create a new update command by path name.
            /// </summary>
            /// <param name="entryPath">path of the file within the repository.</param>
            protected PathEdit(string entryPath)
            {
                _path = Constants.encode(entryPath);
            }

            /// <summary>
            /// Create a new update command for an existing entry instance.
            /// </summary>
            /// <param name="ent">
            /// Entry instance to match path of. Only the path of this
            /// entry is actually considered during command evaluation.
            /// </param>
            protected PathEdit(DirCacheEntry ent)
            {
                _path = ent.Path;
            }

            public byte[] Path
            {
                get { return _path; }
            }

            /// <summary>
            /// Apply the update to a single cache entry matching the path.
            /// <para />
            /// After apply is invoked the entry is added to the output table, and
            /// will be included in the new index.
            /// </summary>
            /// <param name="ent">
            /// The entry being processed. All fields are zeroed out if
            /// the path is a new path in the index.
            /// </param>
            public abstract void Apply(DirCacheEntry ent);
        }

        /// <summary>
        /// Deletes a single file entry from the index.
        /// <para />
        /// This deletion command removes only a single file at the given location,
        /// but removes multiple stages (if present) for that path. To remove a
        /// complete subtree use <see cref="DeleteTree"/> instead.
        /// </summary>
        /// <seealso cref="DeleteTree"/>
        public class DeletePath : PathEdit
        {
            /// <summary>
            /// Create a new deletion command by path name.
            /// </summary>
            /// <param name="entryPath">
            /// Path of the file within the repository.
            /// </param>
            public DeletePath(string entryPath)
                : base(entryPath)
            {
            }

            ///	<summary>
            /// Create a new deletion command for an existing entry instance.
            /// </summary>
            /// <param name="ent">
            /// Entry instance to remove. Only the path of this entry is
            /// actually considered during command evaluation.
            /// </param>
            public DeletePath(DirCacheEntry ent)
                : base(ent)
            {
            }

            public override void Apply(DirCacheEntry ent)
            {
                throw new NotSupportedException("No apply in delete");
            }
        }

        ///	<summary>
        /// Recursively deletes all paths under a subtree.
        /// <para />
        /// This deletion command is more generic than <seealso cref="DeletePath"/> as it can
        /// remove all records which appear recursively under the same subtree.
        /// Multiple stages are removed (if present) for any deleted entry.
        /// <para />
        /// This command will not remove a single file entry. To remove a single file
        /// use <seealso cref="DeletePath"/>.
        /// </summary>
        /// <seealso cref="DeletePath"/>
        public class DeleteTree : PathEdit
        {
            ///	<summary>
            /// Create a new tree deletion command by path name.
            /// </summary>
            /// <param name="entryPath">
            /// Path of the subtree within the repository. If the path
            /// does not end with "/" a "/" is implicitly added to ensure
            /// only the subtree's contents are matched by the command.
            /// </param>
            public DeleteTree(string entryPath)
                : base(entryPath.EndsWith("/") ? entryPath : entryPath + "/")
            {
            }

            public override void Apply(DirCacheEntry ent)
            {
                throw new NotSupportedException("No apply in delete");
            }
        }

        #endregion
    }
}