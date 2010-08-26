/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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

namespace GitSharp.Core.TreeWalk
{
    /// <summary>
	/// Iterator over an empty tree (a directory with no files).
    /// </summary>
    public class EmptyTreeIterator : AbstractTreeIterator
    {
        /// <summary>
		/// Create a new iterator with no parent.
        /// </summary>
        public EmptyTreeIterator()
        {
            // Create a root empty tree.
        }

		/// <summary>
		/// Create an iterator for a subtree of an existing iterator.
		/// The caller is responsible for setting up the path of the child iterator.
		/// </summary>
		/// <param name="parentIterator">Parent tree iterator.</param>
        public EmptyTreeIterator(AbstractTreeIterator parentIterator)
            : base(parentIterator)
        {
            PathLen = PathOffset;
        }

		/// <summary>
		/// Create an iterator for a subtree of an existing iterator.
		/// The caller is responsible for setting up the path of the child iterator.
		/// </summary>
		/// <param name="parent">Parent tree iterator.</param>
		/// <param name="childPath">
		/// Path array to be used by the child iterator. This path must
		/// contain the path from the top of the walk to the first child
		/// and must end with a '/'.
		/// </param>
		/// <param name="childPathOffset">
		/// position within <paramref name="childPath"/> where the child can
		/// insert its data. The value at
		/// <code><paramref name="childPath"/>[<paramref name="childPathOffset"/>-1]</code> 
		/// must be '/'.
		/// </param>
        public EmptyTreeIterator(AbstractTreeIterator parent, byte[] childPath, int childPathOffset)
            : base(parent, childPath, childPathOffset)
        {
            PathLen = childPathOffset - 1;
        }

        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
        {
            return new EmptyTreeIterator(this);
        }

        public override ObjectId getEntryObjectId()
        {
            return ObjectId.ZeroId;
        }

        public override byte[] idBuffer()
        {
            return ZeroId;
        }

        public override int idOffset()
        {
            return 0;
        }

        public override bool first()
        {
            return true;
        }

        public override bool eof()
        {
            return true;
        }

        public override void next(int delta)
        {
            // Do nothing.
        }

        public override void back(int delta)
        {
            // Do nothing.
        }

        public override void stopWalk()
        {
            if (Parent != null)
            {
            	Parent.stopWalk();
            }
        }
    }
}