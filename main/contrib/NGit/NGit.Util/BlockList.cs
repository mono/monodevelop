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
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Random access list that allocates entries in blocks.</summary>
	/// <remarks>
	/// Random access list that allocates entries in blocks.
	/// <p>
	/// Unlike
	/// <see cref="System.Collections.ArrayList{E}">System.Collections.ArrayList&lt;E&gt;
	/// 	</see>
	/// , this type does not need to reallocate the
	/// internal array in order to expand the capacity of the list. Access to any
	/// element is constant time, but requires two array lookups instead of one.
	/// <p>
	/// To handle common usages,
	/// <see cref="BlockList{T}.AddItem(object)">BlockList&lt;T&gt;.AddItem(object)</see>
	/// and
	/// <see cref="BlockList{T}.Iterator()">BlockList&lt;T&gt;.Iterator()</see>
	/// use
	/// internal code paths to amortize out the second array lookup, making addition
	/// and simple iteration closer to one array operation per element processed.
	/// <p>
	/// Similar to
	/// <code>ArrayList</code>
	/// , adding or removing from any position except the
	/// end of the list requires O(N) time to copy all elements between the
	/// modification point and the end of the list. Applications are strongly
	/// encouraged to not use this access pattern with this list implementation.
	/// </remarks>
	/// <?></?>
	public class BlockList<T> : AbstractList<T>
	{
		private const int BLOCK_BITS = 10;

		internal const int BLOCK_SIZE = 1 << BLOCK_BITS;

		private const int BLOCK_MASK = BLOCK_SIZE - 1;

		private T[][] directory;

		private int size;

		private int tailDirIdx;

		private int tailBlkIdx;

		private T[] tailBlock;

		/// <summary>Initialize an empty list.</summary>
		/// <remarks>Initialize an empty list.</remarks>
		public BlockList()
		{
			directory = NGit.Util.BlockList<T>.NewDirectory(256);
			directory[0] = NGit.Util.BlockList<T>.NewBlock();
			tailBlock = directory[0];
		}

		/// <summary>Initialize an empty list with an expected capacity.</summary>
		/// <remarks>Initialize an empty list with an expected capacity.</remarks>
		/// <param name="capacity">number of elements expected to be in the list.</param>
		public BlockList(int capacity)
		{
			int dirSize = ToDirectoryIndex(capacity);
			if ((capacity & BLOCK_MASK) != 0 || dirSize == 0)
			{
				dirSize++;
			}
			directory = NGit.Util.BlockList<T>.NewDirectory(dirSize);
			directory[0] = NGit.Util.BlockList<T>.NewBlock();
			tailBlock = directory[0];
		}

		public override int Count
		{
			get
			{
				return size;
			}
		}

		public override void Clear()
		{
			foreach (T[] block in directory)
			{
				if (block != null)
				{
					Arrays.Fill(block, default(T));
				}
			}
			size = 0;
			tailDirIdx = 0;
			tailBlkIdx = 0;
			tailBlock = directory[0];
		}

		public override T Get(int index)
		{
			if (index < 0 || size <= index)
			{
				throw new IndexOutOfRangeException(index.ToString());
			}
			return directory[ToDirectoryIndex(index)][ToBlockIndex(index)];
		}

		public override T Set(int index, T element)
		{
			if (index < 0 || size <= index)
			{
				throw new IndexOutOfRangeException(index.ToString());
			}
			T[] blockRef = directory[ToDirectoryIndex(index)];
			int blockIdx = ToBlockIndex(index);
			T old = blockRef[blockIdx];
			blockRef[blockIdx] = element;
			return old;
		}

		public override bool AddItem(T element)
		{
			int i = tailBlkIdx;
			if (i < BLOCK_SIZE)
			{
				// Fast-path: Append to current tail block.
				tailBlock[i] = element;
				tailBlkIdx = i + 1;
				size++;
				return true;
			}
			// Slow path: Move to the next block, expanding if necessary.
			if (++tailDirIdx == directory.Length)
			{
				T[][] newDir = NGit.Util.BlockList<T>.NewDirectory(directory.Length << 1);
				System.Array.Copy(directory, 0, newDir, 0, directory.Length);
				directory = newDir;
			}
			T[] blockRef = directory[tailDirIdx];
			if (blockRef == null)
			{
				blockRef = NGit.Util.BlockList<T>.NewBlock();
				directory[tailDirIdx] = blockRef;
			}
			blockRef[0] = element;
			tailBlock = blockRef;
			tailBlkIdx = 1;
			size++;
			return true;
		}

		public override void Add(int index, T element)
		{
			if (index == size)
			{
				// Fast-path: append onto the end of the list.
				AddItem(element);
			}
			else
			{
				if (index < 0 || size < index)
				{
					throw new IndexOutOfRangeException(index.ToString());
				}
				else
				{
					// Slow-path: the list needs to expand and insert.
					// Do this the naive way, callers shouldn't abuse
					// this class by entering this code path.
					//
					AddItem(default(T));
					// expand the list by one
					for (int oldIdx = size - 2; index <= oldIdx; oldIdx--)
					{
						Set(oldIdx + 1, this[oldIdx]);
					}
					Set(index, element);
				}
			}
		}

		public override T Remove(int index)
		{
			if (index == size - 1)
			{
				// Fast-path: remove the last element.
				T[] blockRef = directory[ToDirectoryIndex(index)];
				int blockIdx = ToBlockIndex(index);
				T old = blockRef[blockIdx];
				blockRef[blockIdx] = default(T);
				size--;
				if (0 < tailBlkIdx)
				{
					tailBlkIdx--;
				}
				else
				{
					ResetTailBlock();
				}
				return old;
			}
			else
			{
				if (index < 0 || size <= index)
				{
					throw new IndexOutOfRangeException(index.ToString());
				}
				else
				{
					// Slow-path: the list needs to contract and remove.
					// Do this the naive way, callers shouldn't abuse
					// this class by entering this code path.
					//
					T old = this[index];
					for (; index < size - 1; index++)
					{
						Set(index, this[index + 1]);
					}
					Set(size - 1, default(T));
					size--;
					ResetTailBlock();
					return old;
				}
			}
		}

		private void ResetTailBlock()
		{
			tailDirIdx = ToDirectoryIndex(size);
			tailBlkIdx = ToBlockIndex(size);
			tailBlock = directory[tailDirIdx];
		}

		public override Sharpen.Iterator<T> Iterator()
		{
			return new BlockList<T>.MyIterator(this);
		}

		private static int ToDirectoryIndex(int index)
		{
			return (int)(((uint)index) >> BLOCK_BITS);
		}

		private static int ToBlockIndex(int index)
		{
			return index & BLOCK_MASK;
		}

		private static T[][] NewDirectory(int size)
		{
			return new T[size][];
		}

		private static T[] NewBlock()
		{
			return new T[BLOCK_SIZE];
		}

		private class MyIterator : Iterator<T>
		{
			private int index;

			private int dirIdx;

			private int blkIdx;

			private T[] block;

			public override bool HasNext()
			{
				return this.index < this._enclosing.size;
			}

			public override T Next()
			{
				if (this._enclosing.size <= this.index)
				{
					throw new NoSuchElementException();
				}
				T res = this.block[this.blkIdx];
				if (++this.blkIdx == BlockList<T>.BLOCK_SIZE)
				{
					if (++this.dirIdx < this._enclosing.directory.Length)
					{
						this.block = this._enclosing.directory[this.dirIdx];
					}
					else
					{
						this.block = null;
					}
					this.blkIdx = 0;
				}
				this.index++;
				return res;
			}

			public override void Remove()
			{
				if (this.index == 0)
				{
					throw new InvalidOperationException();
				}
				this._enclosing.Remove(--this.index);
				this.dirIdx = BlockList<T>.ToDirectoryIndex(this.index);
				this.blkIdx = BlockList<T>.ToBlockIndex(this.index);
				this.block = this._enclosing.directory[this.dirIdx];
			}

			internal MyIterator(BlockList<T> _enclosing)
			{
				this._enclosing = _enclosing;
				block = this._enclosing.directory[this.dirIdx];
			}

			private readonly BlockList<T> _enclosing;
		}
	}
}
