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
using GitSharp.Core.TreeWalk;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.RevWalk
{
    /// <summary>
    /// Specialized subclass of RevWalk to include trees, blobs and tags.
    /// <para />
    /// Unlike RevWalk this subclass is able to remember starting roots that include
    /// annotated tags, or arbitrary trees or blobs. Once commit generation is
    /// complete and all commits have been popped by the application, individual
    /// annotated tag, tree and blob objects can be popped through the additional
    /// method <see cref="nextObject"/>.
    /// <para />
    /// Tree and blob objects reachable from interesting commits are automatically
    /// scheduled for inclusion in the results of <see cref="nextObject"/>, returning
    /// each object exactly once. Objects are sorted and returned according to the
    /// the commits that reference them and the order they appear within a tree.
    /// Ordering can be affected by changing the <see cref="RevSort"/> used to order 
    /// the commits that are returned first.
    /// </summary>
    public class ObjectWalk : RevWalk
    {
        /// <summary>
        /// Indicates a non-RevCommit is in <see cref="PendingObjects"/>.
        /// <para />
        /// We can safely reuse <see cref="RevWalk.REWRITE"/> here for the same value as it
        /// is only set on RevCommit and <see cref="PendingObjects"/> never has RevCommit
        /// instances inserted into it.
        /// </summary>
        private const int InPending = REWRITE;

        private CanonicalTreeParser _treeWalk;
        private BlockObjQueue _pendingObjects;
        private RevTree _currentTree;
        private RevObject last;

        /// <summary>
        /// Create a new revision and object walker for a given repository.
        /// </summary>
        /// <param name="repo">
        /// The repository the walker will obtain data from.
        /// </param>
        public ObjectWalk(Repository repo)
            : base(repo)
        {
            _pendingObjects = new BlockObjQueue();
            _treeWalk = new CanonicalTreeParser();
        }

        /// <summary>
        /// Mark an object or commit to start graph traversal from. 
        /// <para />
        /// Callers are encouraged to use <see cref="RevWalk.parseAny(AnyObjectId)"/>
        /// instead of <see cref="RevWalk.lookupAny(AnyObjectId, int)"/>, as this method
        /// requires the object to be parsed before it can be added as a root for the
        /// traversal.
        /// <para />
        /// The method will automatically parse an unparsed object, but error
        /// handling may be more difficult for the application to explain why a
        /// RevObject is not actually valid. The object pool of this walker would
        /// also be 'poisoned' by the invalid <see cref="RevObject"/>.
        /// <para />
        /// This method will automatically call <see cref="RevWalk.markStart(RevCommit)"/>
        /// if passed RevCommit instance, or a <see cref="RevTag"/> that directly (or indirectly)
        /// references a <see cref="RevCommit"/>.
        /// </summary>
        /// <param name="o">
        /// The object to start traversing from. The object passed must be
        /// from this same revision walker.
        /// </param>
        /// <exception cref="MissingObjectException">
        /// The object supplied is not available from the object
        /// database. This usually indicates the supplied object is
        /// invalid, but the reference was constructed during an earlier
        /// invocation to <see cref="RevWalk.lookupAny(AnyObjectId, int)"/>.
        /// </exception>
        /// <exception cref="IncorrectObjectTypeException">
        /// The object was not parsed yet and it was discovered during
        /// parsing that it is not actually the type of the instance
        /// passed in. This usually indicates the caller used the wrong
        /// type in a <see cref="RevWalk.lookupAny(AnyObjectId, int)"/> call.
        /// </exception>
        /// <exception cref="Exception">
        /// A pack file or loose object could not be Read.
        /// </exception>
        public void markStart(RevObject o)
        {
            RevTag oTag = (o as RevTag);
            while (oTag != null)
            {
                AddObject(o);
                o = oTag.getObject();
                parseHeaders(o);
            }

            RevCommit oComm = (o as RevCommit);
            if (oComm != null)
            {
                base.markStart(oComm);
            }
            else
            {
                AddObject(o);
            }
        }

        /// <summary>
        /// Mark an object to not produce in the output.
        /// <para />
        /// Uninteresting objects denote not just themselves but also their entire
        /// reachable chain, back until the merge base of an uninteresting commit and
        /// an otherwise interesting commit.
        /// <para />
        /// Callers are encouraged to use <see cref="RevWalk.parseAny(AnyObjectId)"/>
        /// instead of <see cref="RevWalk.lookupAny(AnyObjectId, int)"/>, as this method
        /// requires the object to be parsed before it can be added as a root for the
        /// traversal.
        /// <para />
        /// The method will automatically parse an unparsed object, but error
        /// handling may be more difficult for the application to explain why a
        /// RevObject is not actually valid. The object pool of this walker would
        /// also be 'poisoned' by the invalid <see cref="RevObject"/>.
        /// <para />
        /// This method will automatically call <see cref="RevWalk.markStart(RevCommit)"/>
        /// if passed RevCommit instance, or a <see cref="RevTag"/> that directly (or indirectly)
        /// references a <see cref="RevCommit"/>.
        /// </summary>
        /// <param name="o">
        /// The object to start traversing from. The object passed must be
        /// from this same revision walker.
        /// </param>
        /// <exception cref="MissingObjectException">
        /// The object supplied is not available from the object
        /// database. This usually indicates the supplied object is
        /// invalid, but the reference was constructed during an earlier
        /// invocation to <see cref="RevWalk.lookupAny(AnyObjectId, int)"/>.
        /// </exception>
        /// <exception cref="IncorrectObjectTypeException">
        /// The object was not parsed yet and it was discovered during
        /// parsing that it is not actually the type of the instance
        /// passed in. This usually indicates the caller used the wrong
        /// type in a <see cref="RevWalk.lookupAny(AnyObjectId, int)"/> call.
        /// </exception>
        /// <exception cref="Exception">
        /// A pack file or loose object could not be Read.
        /// </exception>
        public void markUninteresting(RevObject o)
        {
            RevTag oTag = (o as RevTag);
            while (oTag != null)
            {
                o.Flags |= UNINTERESTING;
                if (hasRevSort(RevSort.BOUNDARY))
                {
                    AddObject(o);
                }
                o = oTag.getObject();
                parseHeaders(o);
            }

            RevCommit oComm = (o as RevCommit);
            if (oComm != null)
            {
                base.markUninteresting(oComm);
            }
            else if (o is RevTree)
            {
                MarkTreeUninteresting(o);
            }
            else
            {
                o.Flags |= UNINTERESTING;
            }

            if (o.Type != Constants.OBJ_COMMIT && hasRevSort(RevSort.BOUNDARY))
            {
                AddObject(o);
            }
        }

        public override RevCommit next()
        {
            while (true)
            {
                RevCommit r = base.next();

                if (r == null) return null;

                if ((r.Flags & UNINTERESTING) != 0)
                {
                    MarkTreeUninteresting(r.Tree);

                    if (hasRevSort(RevSort.BOUNDARY))
                    {
                        _pendingObjects.add(r.Tree);
                        return r;
                    }

                    continue;
                }

                _pendingObjects.add(r.Tree);

                return r;
            }
        }

        /// <summary>
        /// Pop the next most recent object.
        /// </summary>
        /// <returns>next most recent object; null if traversal is over.</returns>
        /// <exception cref="MissingObjectException">
        /// One or or more of the next objects are not available from the
        /// object database, but were thought to be candidates for
        /// traversal. This usually indicates a broken link.
        /// </exception>
        /// <exception cref="IncorrectObjectTypeException">
        /// One or or more of the objects in a tree do not match the type indicated.
        /// </exception>
        /// <exception cref="Exception">
        /// A pack file or loose object could not be Read.
        /// </exception>
        public RevObject nextObject()
        {
            if (last != null)
            {
                _treeWalk = last is RevTree ? enter(last) : _treeWalk.next();
            }


            while (!_treeWalk.eof())
            {
                FileMode mode = _treeWalk.EntryFileMode;

                switch ((int)mode.ObjectType)
                {
                    case Constants.OBJ_BLOB:
                        _treeWalk.getEntryObjectId(IdBuffer);

                        RevBlob blob = lookupBlob(IdBuffer);
                        if ((blob.Flags & SEEN) != 0) break;

                        blob.Flags |= SEEN;
                        if (ShouldSkipObject(blob)) break;

                        last = blob;
                        return blob;

                    case Constants.OBJ_TREE:
                        _treeWalk.getEntryObjectId(IdBuffer);

                        RevTree tree = lookupTree(IdBuffer);
                        if ((tree.Flags & SEEN) != 0) break;

                        tree.Flags |= SEEN;
                        if (ShouldSkipObject(tree)) break;

                        last = tree;
                        return tree;

                    default:
                        if (FileMode.GitLink.Equals(mode.Bits)) break;
                        _treeWalk.getEntryObjectId(IdBuffer);

                        throw new CorruptObjectException("Invalid mode " + mode
                                + " for " + IdBuffer.Name + " '"
                                + _treeWalk.EntryPathString + "' in "
                                + _currentTree.Name + ".");
                }

                _treeWalk = _treeWalk.next();
            }

            last = null;
            while (true)
            {
                RevObject obj = _pendingObjects.next();
                if (obj == null) return null;
                if ((obj.Flags & SEEN) != 0) continue;

                obj.Flags |= SEEN;
                if (ShouldSkipObject(obj)) continue;

                RevTree oTree = (obj as RevTree);
                if (oTree != null)
                {
                    _currentTree = oTree;
                    _treeWalk = _treeWalk.resetRoot(Repository, _currentTree, WindowCursor);
                }

                return obj;
            }
        }

        private CanonicalTreeParser enter(RevObject tree)
        {
            CanonicalTreeParser p = _treeWalk.createSubtreeIterator0(Repository, tree, WindowCursor);
            if (p.eof())
            {
                // We can't tolerate the subtree being an empty tree, as
                // that will break us out early before we visit all names.
                // If it is, advance to the parent's next record.
                //
                return _treeWalk.next();
            }
            return p;
        }

        private bool ShouldSkipObject(RevObject o)
        {
            return (o.Flags & UNINTERESTING) != 0 && !hasRevSort(RevSort.BOUNDARY);
        }

        /// <summary>
        /// Verify all interesting objects are available, and reachable.
        /// <para />
        /// Callers should populate starting points and ending points with
        /// <see cref="markStart(RevObject)"/> and <see cref="markUninteresting(RevObject)"/>
        /// and then use this method to verify all objects between those two points
        /// exist in the repository and are readable.
        /// <para />
        /// This method returns successfully if everything is connected; it throws an
        /// exception if there is a connectivity problem. The exception message
        /// provides some detail about the connectivity failure.
        /// </summary>
        /// <exception cref="MissingObjectException">
        /// One or or more of the next objects are not available from the
        /// object database, but were thought to be candidates for
        /// traversal. This usually indicates a broken link.
        /// </exception>
        /// <exception cref="IncorrectObjectTypeException">
        /// One or or more of the objects in a tree do not match the type
        /// indicated.
        /// </exception>
        /// <exception cref="Exception">
        /// A pack file or loose object could not be Read.
        /// </exception>
        public void checkConnectivity()
        {
            while (true)
            {
                RevCommit c = next();
                if (c == null) break;
            }

            while (true)
            {
                RevObject o = nextObject();
                if (o == null) break;

                if (o is RevBlob && !Repository.HasObject(o))
                {
                    throw new MissingObjectException(o, Constants.TYPE_BLOB);
                }
            }
        }

        /// <summary>
        /// Get the current object's complete path.
        /// <para />
        /// This method is not very efficient and is primarily meant for debugging
        /// and output generation. Applications should try to avoid calling it,
        /// and if invoked do so only once per interesting entry, where the name is
        /// absolutely required for correct function.
        /// </summary>
        /// <returns>
        /// Complete path of the current entry, from the root of the
        /// repository. If the current entry is in a subtree there will be at
        /// least one '/' in the returned string. Null if the current entry
        /// has no path, such as for annotated tags or root level trees.
        /// </returns>
        public string PathString
        {
            get { return last != null ? _treeWalk.EntryPathString : null; }
        }

        public override void Dispose()
        {
            base.Dispose();
            _pendingObjects = new BlockObjQueue();
            _treeWalk = new CanonicalTreeParser();
            _currentTree = null;
            last = null;
        }

        internal override void reset(int retainFlags)
        {
            base.reset(retainFlags);
            _pendingObjects = new BlockObjQueue();
            _treeWalk = new CanonicalTreeParser();
            _currentTree = null;
            last = null;
        }

        private void AddObject(RevObject obj)
        {
            if ((obj.Flags & InPending) != 0) return;

            obj.Flags |= InPending;
            _pendingObjects.add(obj);
        }

        private void MarkTreeUninteresting(RevObject tree)
        {
            if ((tree.Flags & UNINTERESTING) != 0) return;
            tree.Flags |= UNINTERESTING;

            _treeWalk = _treeWalk.resetRoot(Repository, tree, WindowCursor);
            while (!_treeWalk.eof())
            {
                FileMode mode = _treeWalk.EntryFileMode;
                var sType = (int)mode.ObjectType;

                switch (sType)
                {
                    case Constants.OBJ_BLOB:
                        _treeWalk.getEntryObjectId(IdBuffer);
                        lookupBlob(IdBuffer).Flags |= UNINTERESTING;
                        break;

                    case Constants.OBJ_TREE:
                        _treeWalk.getEntryObjectId(IdBuffer);
                        RevTree t = lookupTree(IdBuffer);
                        if ((t.Flags & UNINTERESTING) == 0)
                        {
                            t.Flags |= UNINTERESTING;
                            _treeWalk = _treeWalk.createSubtreeIterator0(Repository, t, WindowCursor);
                            continue;
                        }
                        break;

                    default:
                        if (FileMode.GitLink == FileMode.FromBits(mode.Bits)) break;
                        _treeWalk.getEntryObjectId(IdBuffer);

                        throw new CorruptObjectException("Invalid mode " + mode
                                + " for " + IdBuffer + " "
                                + _treeWalk.EntryPathString + " in " + tree + ".");
                }

                _treeWalk = _treeWalk.next();
            }
        }

        public CanonicalTreeParser TreeWalk
        {
            get { return _treeWalk; }
        }

        public BlockObjQueue PendingObjects
        {
            get { return _pendingObjects; }
        }
    }
}