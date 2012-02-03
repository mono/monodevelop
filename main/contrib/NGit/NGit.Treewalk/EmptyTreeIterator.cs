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

using NGit;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Treewalk
{
	/// <summary>Iterator over an empty tree (a directory with no files).</summary>
	/// <remarks>Iterator over an empty tree (a directory with no files).</remarks>
	public class EmptyTreeIterator : AbstractTreeIterator
	{
		/// <summary>Create a new iterator with no parent.</summary>
		/// <remarks>Create a new iterator with no parent.</remarks>
		public EmptyTreeIterator()
		{
		}

		protected internal EmptyTreeIterator(AbstractTreeIterator p) : base(p)
		{
			// Create a root empty tree.
			pathLen = pathOffset;
		}

		/// <summary>Create an iterator for a subtree of an existing iterator.</summary>
		/// <remarks>
		/// Create an iterator for a subtree of an existing iterator.
		/// <p>
		/// The caller is responsible for setting up the path of the child iterator.
		/// </remarks>
		/// <param name="p">parent tree iterator.</param>
		/// <param name="childPath">
		/// path array to be used by the child iterator. This path must
		/// contain the path from the top of the walk to the first child
		/// and must end with a '/'.
		/// </param>
		/// <param name="childPathOffset">
		/// position within <code>childPath</code> where the child can
		/// insert its data. The value at
		/// <code>childPath[childPathOffset-1]</code> must be '/'.
		/// </param>
		protected internal EmptyTreeIterator(AbstractTreeIterator p, byte[] childPath, int
			 childPathOffset) : base(p, childPath, childPathOffset)
		{
			pathLen = childPathOffset - 1;
		}

		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override AbstractTreeIterator CreateSubtreeIterator(ObjectReader reader)
		{
			return new NGit.Treewalk.EmptyTreeIterator(this);
		}

		public override bool HasId
		{
			get
			{
				return false;
			}
		}

		public override ObjectId EntryObjectId
		{
			get
			{
				return ObjectId.ZeroId;
			}
		}

		public override byte[] IdBuffer
		{
			get
			{
				return zeroid;
			}
		}

		public override int IdOffset
		{
			get
			{
				return 0;
			}
		}

		public override void Reset()
		{
		}

		public override bool First
		{
			get
			{
				// Do nothing.
				return true;
			}
		}

		public override bool Eof
		{
			get
			{
				return true;
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		public override void Next(int delta)
		{
		}

		// Do nothing.
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		public override void Back(int delta)
		{
		}

		// Do nothing.
		public override void StopWalk()
		{
			if (parent != null)
			{
				parent.StopWalk();
			}
		}
	}
}
