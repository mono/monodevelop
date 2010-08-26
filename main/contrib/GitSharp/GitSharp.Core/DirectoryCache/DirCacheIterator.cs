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
using GitSharp.Core.Util;

namespace GitSharp.Core.DirectoryCache
{
	/// <summary>
	/// Iterate a <see cref="DirCache"/> as part of a <see cref="TreeWalk"/>.
	/// <para/>
	/// This is an iterator to adapt a loaded <see cref="DirCache"/> instance (such as
	/// Read from an existing <code>.git/index</code> file) to the tree structure
	/// used by a <see cref="TreeWalk"/>, making it possible for applications to walk
	/// over any combination of tree objects already in the object database, index
	/// files, or working directories.
	/// </summary>
	/// <seealso cref="TreeWalk"/>
	public class DirCacheIterator : AbstractTreeIterator
	{
		private int _pointer;
		private int _nextSubtreePos;
		private DirCacheEntry _currentEntry;
		private DirCacheTree _currentSubtree;

		/// <summary>
		/// Create a new iterator for an already loaded DirCache instance.
		/// <para/>
		/// The iterator implementation may copy part of the cache's data during
		/// construction, so the cache must be Read in prior to creating the
		/// iterator.
		/// </summary>
		/// <param name="dc">
		/// The cache to walk. It must be already loaded into memory.
		/// </param>
		public DirCacheIterator(DirCache dc)
		{
			Cache = dc;
			Tree = dc.getCacheTree(true);
			TreeStart = 0;
			TreeEnd = Tree.getEntrySpan();
			SubtreeId = new byte[Constants.OBJECT_ID_LENGTH];

			if (!eof())
			{
				ParseEntry();
			}
		}

		public DirCacheIterator(DirCacheIterator parentIterator, DirCacheTree cacheTree)
			: base(parentIterator, parentIterator.Path, parentIterator.PathLen + 1)
		{
			if ( parentIterator == null)
			{
				throw new System.ArgumentNullException("parentIterator");
			}
			Cache = parentIterator.Cache;
			Tree = cacheTree;
			TreeStart = parentIterator._pointer;
			TreeEnd = TreeStart + Tree.getEntrySpan();
			SubtreeId = parentIterator.SubtreeId;
			_pointer = parentIterator._pointer;
			ParseEntry();
		}

		public override AbstractTreeIterator createSubtreeIterator(Repository repo)
		{
			if (_currentSubtree == null)
			{
				throw new IncorrectObjectTypeException(getEntryObjectId(), Constants.TYPE_TREE);
			}

			return new DirCacheIterator(this, _currentSubtree);
		}

		public override EmptyTreeIterator createEmptyTreeIterator()
		{
			var newPath = new byte[Math.Max(PathLen + 1, DEFAULT_PATH_SIZE)];
			Array.Copy(Path, 0, newPath, 0, PathLen);
			newPath[PathLen] = (byte)'/';
			return new EmptyTreeIterator(this, newPath, PathLen + 1);
		}

		public override byte[] idBuffer()
		{
			if (_currentSubtree != null)
			{
				return SubtreeId;
			}

			if (_currentEntry != null)
			{
				return _currentEntry.idBuffer();
			}

			return ZeroId;
		}

		public override int idOffset()
		{
			if (_currentSubtree != null)
			{
				return 0;
			}

			if (_currentEntry != null)
			{
				return _currentEntry.idOffset();
			}

			return 0;
		}

		public override bool first()
		{
			return _pointer == TreeStart;
		}

		public override bool eof()
		{
			return _pointer == TreeEnd;
		}

		public override void next(int delta)
		{
			while (--delta >= 0)
			{
				if (_currentSubtree != null)
				{
					_pointer += _currentSubtree.getEntrySpan();
				}
				else
				{
					_pointer++;
				}

				if (eof()) break;

				ParseEntry();
			}
		}

		public override void back(int delta)
		{
			while (--delta >= 0)
			{
				if (_currentSubtree != null)
				{
					_nextSubtreePos--;
				}

				_pointer--;
				ParseEntry();
				
				if (_currentSubtree != null)
				{
					_pointer -= _currentSubtree.getEntrySpan() - 1;
				}
			}
		}

		private void ParseEntry()
		{
			_currentEntry = Cache.getEntry(_pointer);
			byte[] cep = _currentEntry.Path;

			if (_nextSubtreePos != Tree.getChildCount())
			{
				DirCacheTree s = Tree.getChild(_nextSubtreePos);
				if (s.contains(cep, PathOffset, cep.Length))
				{
					// The current position is the first file of this subtree.
					// Use the subtree instead as the current position.
					//
					_currentSubtree = s;
					_nextSubtreePos++;

					if (s.isValid())
					{
						s.getObjectId().copyRawTo(SubtreeId, 0);
					}
					else
					{
						SubtreeId.Fill((byte)0);
					}
					
					Mode = FileMode.Tree.Bits;

					Path = cep;
					PathLen = PathOffset + s.nameLength();
					return;
				}
			}

			// The current position is a file/symlink/gitlink so we
			// do not have a subtree located here.
			//
			Mode = _currentEntry.getRawMode();
			Path = cep;
			PathLen = cep.Length;
			_currentSubtree = null;
		}

		/// <summary>
		/// The cache this iterator was created to walk.
		/// </summary>
		public DirCache Cache { get; private set; }

		/// <summary>
		/// The tree this iterator is walking.
		/// </summary>
		public DirCacheTree Tree { get; private set; }

		/// <summary>
		/// First position in this tree.
		/// </summary>
		public int TreeStart { get; private set; }

		/// <summary>
		/// Last position in this tree.
		/// </summary>
		public int TreeEnd { get; private set; }

		/// <summary>
		/// Special buffer to hold the <see cref="ObjectId"/> of <see cref="CurrentSubtree"/>.
		/// </summary>
		public byte[] SubtreeId { get; private set; }

		/// <summary>
		/// Index of entry within <see cref="Cache"/>.
		/// </summary>
		public int Pointer
		{
			get { return _pointer; }
		}

		/// <summary>
		/// Next subtree to consider within <see cref="Tree"/>.
		/// </summary>
		public int NextSubtreePos
		{
			get { return _nextSubtreePos; }
		}

		/// <summary>
		/// The current file entry from <see cref="Cache"/>.
		/// </summary>
		public DirCacheEntry CurrentEntry
		{
			get { return _currentEntry; }
		}

		/// <summary>
		/// The subtree containing <see cref="CurrentEntry"/> if this is first entry.
		/// </summary>
		public DirCacheTree CurrentSubtree
		{
			get { return _currentSubtree; }
		}

		/// <summary>
		/// Get the DirCacheEntry for the current file.
		/// </summary>
		/// <returns>
		/// The current cache entry, if this iterator is positioned on a
		/// non-tree.
		/// </returns>
		public DirCacheEntry getDirCacheEntry()
		{
			return _currentSubtree == null ? _currentEntry : null;
		}
	}
}