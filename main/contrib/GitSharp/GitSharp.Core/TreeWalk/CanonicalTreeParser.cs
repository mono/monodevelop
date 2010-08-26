/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.TreeWalk
{
    /// <summary>
    /// Parses raw Git trees from the canonical semi-text/semi-binary format.
    /// </summary>
    public class CanonicalTreeParser : AbstractTreeIterator
    {
        private static readonly byte[] Empty = { };

        private byte[] _raw;

        /** First offset within {@link #_raw} of the prior entry. */
        private int _prevPtr;

        /** First offset within {@link #_raw} of the current entry's data. */
        private int _currPtr;

        /** Offset one past the current entry (first byte of next entry). */
        private int _nextPtr;

        /// <summary>
        /// Create a new parser.
        /// </summary>
        public CanonicalTreeParser()
        {
            reset(Empty);
        }

        /**
         * Create a new parser for a tree appearing in a subset of a repository.
         *
         * @param prefix
         *            position of this iterator in the repository tree. The value
         *            may be null or the empty array to indicate the prefix is the
         *            root of the repository. A trailing slash ('/') is
         *            automatically appended if the prefix does not end in '/'.
         * @param repo
         *            repository to load the tree data from.
         * @param treeId
         *            identity of the tree being parsed; used only in exception
         *            messages if data corruption is found.
         * @param curs
         *            a window cursor to use during data access from the repository.
         * @throws MissingObjectException
         *             the object supplied is not available from the repository.
         * @throws IncorrectObjectTypeException
         *             the object supplied as an argument is not actually a tree and
         *             cannot be parsed as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be Read.
         */
        public CanonicalTreeParser(byte[] prefix, Repository repo, AnyObjectId treeId, WindowCursor curs)
            : base(prefix)
        {
            reset(repo, treeId, curs);
        }

        private CanonicalTreeParser(AbstractTreeIterator p)
            : base(p)
        {
        }

        /**
         * Reset this parser to walk through the given tree data.
         * 
         * @param treeData
         *            the raw tree content.
         */
        public void reset(byte[] treeData)
        {
            _raw = treeData;
            _prevPtr = -1;
            _currPtr = 0;
            if (eof())
            {
                _nextPtr = 0;
            }
            else
            {
                ParseEntry();
            }
        }

        /**
         * Reset this parser to walk through the given tree.
         * 
         * @param repo
         *            repository to load the tree data from.
         * @param id
         *            identity of the tree being parsed; used only in exception
         *            messages if data corruption is found.
         * @param curs
         *            window cursor to use during repository access.
         * @return the root level parser.
         * @throws MissingObjectException
         *             the object supplied is not available from the repository.
         * @throws IncorrectObjectTypeException
         *             the object supplied as an argument is not actually a tree and
         *             cannot be parsed as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be Read.
         */
        public CanonicalTreeParser resetRoot(Repository repo, AnyObjectId id, WindowCursor curs)
        {
            CanonicalTreeParser p = this;
            while (p.Parent != null)
            {
                p = (CanonicalTreeParser)p.Parent;
            }
            p.reset(repo, id, curs);
            return p;
        }

        /// <summary>
        /// Return this iterator, or its parent, if the tree is at eof.
        /// </summary>
        /// <returns></returns>
        public CanonicalTreeParser next()
        {
            CanonicalTreeParser iterator = this;
            while (true)
            {
                if (iterator._nextPtr == iterator._raw.Length)
                {
                    // This parser has reached EOF, return to the parent.
                    if (iterator._parent == null)
                    {
                        iterator._currPtr = iterator._nextPtr;
                        return iterator;
                    }
                    iterator = (CanonicalTreeParser)iterator.Parent;
                    continue;
                }

                iterator._prevPtr = iterator._currPtr;
                iterator._currPtr = iterator._nextPtr;
                iterator.ParseEntry();
                return iterator;
            }
        }

        /**
         * Reset this parser to walk through the given tree.
         *
         * @param repo
         *            repository to load the tree data from.
         * @param id
         *            identity of the tree being parsed; used only in exception
         *            messages if data corruption is found.
         * @param curs
         *            window cursor to use during repository access.
         * @throws MissingObjectException
         *             the object supplied is not available from the repository.
         * @throws IncorrectObjectTypeException
         *             the object supplied as an argument is not actually a tree and
         *             cannot be parsed as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be Read.
         */
        public void reset(Repository repo, AnyObjectId id, WindowCursor curs)
        {
            if (repo == null)
                throw new ArgumentNullException("repo");
            ObjectLoader ldr = repo.OpenObject(curs, id);
            if (ldr == null)
            {
                ObjectId me = id.ToObjectId();
                throw new MissingObjectException(me, Constants.TYPE_TREE);
            }
            byte[] subtreeData = ldr.CachedBytes;
            if (ldr.Type != Constants.OBJ_TREE)
            {
                ObjectId me = id.ToObjectId();
                throw new IncorrectObjectTypeException(me, Constants.TYPE_TREE);
            }
            reset(subtreeData);
        }

        public new CanonicalTreeParser createSubtreeIterator(Repository repo, MutableObjectId idBuffer, WindowCursor curs)
        {
            if (idBuffer == null)
                throw new ArgumentNullException("idBuffer");
            idBuffer.FromRaw(this.idBuffer(), idOffset());
            if (FileMode.Tree != EntryFileMode)
            {
                ObjectId me = idBuffer.ToObjectId();
                throw new IncorrectObjectTypeException(me, Constants.TYPE_TREE);
            }
            return createSubtreeIterator0(repo, idBuffer, curs);
        }

        /**
         * Back door to quickly Create a subtree iterator for any subtree.
         * <para />
         * Don't use this unless you are ObjectWalk. The method is meant to be
         * called only once the current entry has been identified as a tree and its
         * identity has been converted into an ObjectId.
         *
         * @param repo
         *            repository to load the tree data from.
         * @param id
         *            ObjectId of the tree to open.
         * @param curs
         *            window cursor to use during repository access.
         * @return a new parser that walks over the current subtree.
         * @throws IOException
         *             a loose object or pack file could not be Read.
         */
        public CanonicalTreeParser createSubtreeIterator0(Repository repo, AnyObjectId id, WindowCursor curs) // [henon] createSubtreeIterator0 <--- not a typo!
        {
            var p = new CanonicalTreeParser(this);
            p.reset(repo, id, curs);
            return p;
        }

        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
        {
            var curs = new WindowCursor();
            try
            {
                return createSubtreeIterator(repo, new MutableObjectId(), curs);
            }
            finally
            {
                curs.Release();
            }
        }

        public override byte[] idBuffer()
        {
            return _raw;
        }

        public override int idOffset()
        {
            return _nextPtr - Constants.OBJECT_ID_LENGTH;
        }

        public override bool first()
        {
            return _currPtr == 0;
        }

        public override bool eof()
        {
            return _currPtr == _raw.Length;
        }

        public override void next(int delta)
        {
            if (delta == 1)
            {
                // Moving forward one is the most common case.
                //
                _prevPtr = _currPtr;
                _currPtr = _nextPtr;
                if (!eof())
                {
                    ParseEntry();
                }

                return;
            }

            // Fast skip over records, then parse the last one.
            //
            int end = _raw.Length;
            int ptr = _nextPtr;
            while (--delta > 0 && ptr != end)
            {
                _prevPtr = ptr;
                while (_raw[ptr] != 0)
                {
                    ptr++;
                }

                ptr += Constants.OBJECT_ID_LENGTH + 1;
            }

            if (delta != 0)
            {
                throw new IndexOutOfRangeException(delta.ToString());
            }

            _currPtr = ptr;
            if (!eof())
            {
                ParseEntry();
            }
        }

        public override void back(int delta)
        {
            if (delta == 1 && 0 <= _prevPtr)
            {
                // Moving back one is common in NameTreeWalk, as the average tree
                // won't have D/F type conflicts to study.
                //
                _currPtr = _prevPtr;
                _prevPtr = -1;
                if (!eof())
                {
                    ParseEntry();
                }
                return;
            }

            if (delta <= 0)
            {
                throw new IndexOutOfRangeException(delta.ToString());
            }

            // Fast skip through the records, from the beginning of the tree.
            // There is no reliable way to Read the tree backwards, so we must
            // parse all over again from the beginning. We hold the last "delta"
            // positions in a buffer, so we can find the correct position later.
            //
            var trace = new int[delta + 1];
            trace.Fill(-1);
            int ptr = 0;
            while (ptr != _currPtr)
            {
                Array.Copy(trace, 1, trace, 0, delta);
                trace[delta] = ptr;
                while (_raw[ptr] != 0)
                {
                    ptr++;
                }

                ptr += Constants.OBJECT_ID_LENGTH + 1;
            }

            if (trace[1] == -1)
            {
                throw new IndexOutOfRangeException(delta.ToString());
            }

            _prevPtr = trace[0];
            _currPtr = trace[1];
            ParseEntry();
        }

        private void ParseEntry()
        {
            int ptr = _currPtr;
            byte c = _raw[ptr++];
            int tmp = c - (byte)'0';
            for (; ; )
            {
                c = _raw[ptr++];
                if (' ' == c)
                    break;
                tmp <<= 3;
                tmp += c - (byte)'0';
            }
            Mode = tmp;

            tmp = PathOffset;

            for (; ; tmp++)
            {
                c = _raw[ptr++];
                if (c == 0)
                    break;
                try
                {
                    Path[tmp] = c;
                }
                catch (IndexOutOfRangeException)
                {
                    growPath(tmp);
                    Path[tmp] = c;
                }
            }

            PathLen = tmp;
            _nextPtr = ptr + Constants.OBJECT_ID_LENGTH;
        }
    }
}