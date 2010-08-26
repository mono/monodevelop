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
using System.IO;
using GitSharp.Core.RevWalk.Filter;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// An ordered list of <see cref="RevCommit"/> subclasses.
	/// </summary>
	/// <typeparam name="T">type of subclass of RevCommit the list is storing.</typeparam>
	public class RevCommitList<T> : RevObjectList<T>, IDisposable
		where T : RevCommit
	{
		private RevWalk _walker;

		public override void clear()
		{
			base.clear();
			_walker = null;
		}

		/// <summary>
		/// Apply a flag to all commits matching the specified filter.
		/// 
		/// <code>applyFlag(matching, flag, 0, size())</code>, but without
		/// the incremental behavior.
		/// </summary>
		/// <param name="matching">
		/// the filter to test commits with. If the filter includes a
		/// commit it will have the flag set; if the filter does not
		/// include the commit the flag will be unset.
		/// </param>
		/// <param name="flag">
		/// revision filter needed to Read additional objects, but an
		/// error occurred while reading the pack files or loose objects
		/// of the repository.
		/// </param>
		public void applyFlag(RevFilter matching, RevFlag flag)
		{
			applyFlag(matching, flag, 0, Size);
		}

		/// <summary>
		/// Apply a flag to all commits matching the specified filter.
		/// 
		/// This version allows incremental testing and application, such as from a
		/// background thread that needs to periodically halt processing and send
		/// updates to the UI.
		/// </summary>
		/// <param name="matching">
		/// the filter to test commits with. If the filter includes a
		/// commit it will have the flag set; if the filter does not
		/// include the commit the flag will be unset.
		/// </param>
		/// <param name="flag">
		/// the flag to Apply (or remove). Applications are responsible
		/// for allocating this flag from the source RevWalk.
		/// </param>
		/// <param name="rangeBegin">
		/// first commit within the list to begin testing at, inclusive.
		/// Must not be negative, but may be beyond the end of the list.
		/// </param>
		/// <param name="rangeEnd">
		/// last commit within the list to end testing at, exclusive. If
		/// smaller than or equal to <code>rangeBegin</code> then no
		/// commits will be tested.
		/// </param>
		/// <remarks>
		/// Revision filter needed to Read additional objects, but an
		/// error occurred while reading the pack files or loose objects
		/// of the repository.
		/// </remarks>
		public void applyFlag(RevFilter matching, RevFlag flag, int rangeBegin, int rangeEnd)
		{
			RevWalk w = flag.Walker;
			rangeEnd = Math.Min(rangeEnd, Size);
			while (rangeBegin < rangeEnd)
			{
				int index = rangeBegin;
				Block s = Contents;

				while (s.Shift > 0)
				{
					int i = index >> s.Shift;
					index -= i << s.Shift;
					s = (Block)s.Contents[i];
				}

				while (rangeBegin++ < rangeEnd && index < BLOCK_SIZE)
				{
					var c = (RevCommit)s.Contents[index++];

					if (matching.include(w, c))
					{
						c.add(flag);
					}
					else
					{
						c.remove(flag);
					}
				}
			}
		}

		/// <summary>
		/// Remove the given flag from all commits.
		/// 
		/// Same as <code>clearFlag(flag, 0, size())</code>, but without the
		/// incremental behavior.
		/// </summary>
		/// <param name="flag">the flag to remove. Applications are responsible for
		/// allocating this flag from the source <see cref="RevWalk"/>.</param>
		public void clearFlag(RevFlag flag)
		{
			clearFlag(flag, 0, Size);
		}

		/// <summary>
		/// Remove the given flag from all commits.
		/// 
		/// This method is actually implemented in terms of:
		/// <code>applyFlag(RevFilter.NONE, flag, rangeBegin, rangeEnd)</code>.
		/// </summary>
		/// <param name="flag">
		/// The flag to remove. Applications are responsible for
		/// allocating this flag from the source <see cref="RevWalk"/>.
		/// </param>
		/// <param name="rangeBegin">
		/// First commit within the list to begin testing at, inclusive.
		/// Must not be negative, but may be beyond the end of the list.
		/// </param>
		/// <param name="rangeEnd">
		/// Last commit within the list to end testing at, exclusive. If
		/// smaller than or equal to <code>rangeBegin</code> then no
		/// commits will be tested.
		/// </param>
		public void clearFlag(RevFlag flag, int rangeBegin, int rangeEnd)
		{
			try
			{
				applyFlag(RevFilter.NONE, flag, rangeBegin, rangeEnd);
			}
			catch (IOException)
			{
				// Never happen. The filter we use does not throw any
				// exceptions, for any reason.
			}
		}

		/// <summary>
		/// Find the next commit that has the given flag set.
		/// </summary>
		/// <param name="flag">the flag to test commits against.</param>
		/// <param name="begin">
		/// First commit index to test at. Applications may wish to begin
		/// at 0, to test the first commit in the list.
		/// </param>
		/// <returns>
		/// Index of the first commit at or After index <code>begin</code>
		/// that has the specified flag set on it; -1 if no match is found.
		/// </returns>
		public int indexOf(RevFlag flag, int begin)
		{
			while (begin < Size)
			{
				int index = begin;
				Block s = Contents;
				while (s.Shift > 0)
				{
					int i = index >> s.Shift;
					index -= i << s.Shift;
					s = (Block)s.Contents[i];
				}

				while (begin++ < Size && index < BLOCK_SIZE)
				{
					var c = (RevCommit)s.Contents[index++];
					if (c.has(flag))
					{
						return begin;
					}
				}
			}

			return -1;
		}

		/// <summary>
		/// Find the next commit that has the given flag set.
		/// </summary>
		/// <param name="flag">the flag to test commits against.</param>
		/// <param name="begin">
		/// First commit index to test at. Applications may wish to begin
		/// at <code>size()-1</code>, to test the last commit in the
		/// list.</param>
		/// <returns>
		/// Index of the first commit at or before index <code>begin</code>
		/// that has the specified flag set on it; -1 if no match is found.
		/// </returns>
		public int LastIndexOf(RevFlag flag, int begin)
		{
			begin = Math.Min(begin, Size - 1);
			while (begin >= 0)
			{
				int index = begin;
				Block s = Contents;
				while (s.Shift > 0)
				{
					int i = index >> s.Shift;
					index -= i << s.Shift;
					s = (Block)s.Contents[i];
				}

				while (begin-- >= 0 && index >= 0)
				{
					var c = (RevCommit)s.Contents[index--];
					if (c.has(flag))
					{
						return begin;
					}
				}
			}

			return -1;
		}

		/// <summary>
		/// Set the revision walker this list populates itself from.
		/// </summary>
		/// <param name="walker">the walker to populate from.</param>
		public virtual void Source(RevWalk walker)
		{
			_walker = walker;
		}

		/// <summary>
		/// Is this list still pending more items?
		/// </summary>
		/// <returns>
		/// true if <see cref="fillTo(int)"/> might be able to extend the list
		/// size when called.
		/// </returns>
		public bool IsPending
		{
			get { return _walker != null; }
		}

		/// <summary>
		/// Ensure this list contains at least a specified number of commits.
		/// 
		/// The revision walker specified by <see cref="Source(RevWalk)"/> is pumped until
		/// the given number of commits are contained in this list. If there are
		/// fewer total commits available from the walk then the method will return
		/// early. Callers can test the  size of the list by <see cref="RevObjectList{T}.Size"/> to
		/// determine if the high water mark specified was met.
		/// </summary>
		/// <param name="highMark">
		/// Number of commits the caller wants this list to contain when
		/// the fill operation is complete.
		/// </param>
		public void fillTo(int highMark)
		{
			if (_walker == null || Size > highMark) return;

			Generator p = _walker.Pending;
			T c = (T)p.next();
			if (c == null)
			{
				_walker.Pending = EndGenerator.Instance;
				_walker = null;
				return;
			}

			enter(Size, c);
			add(c);
			p = _walker.Pending;

			while (Size <= highMark)
			{
				int index = Size;
				Block s = Contents;

				while (index >> s.Shift >= BLOCK_SIZE)
				{
					s = new Block(s.Shift + BLOCK_SHIFT);
					s.Contents[0] = Contents;
					Contents = s;
				}

				while (s.Shift > 0)
				{
					int i = index >> s.Shift;
					index -= i << s.Shift;
					if (s.Contents[i] == null)
					{
						s.Contents[i] = new Block(s.Shift - BLOCK_SHIFT);
					}
					s = (Block)s.Contents[i];
				}

				object[] dst = s.Contents;
				while (Size <= highMark && index < BLOCK_SIZE)
				{
					c = (T)p.next();
					if (c == null)
					{
						_walker.Pending = EndGenerator.Instance;
						_walker = null;
						return;
					}
					enter(Size++, c);
					dst[index++] = c;
				}
			}
		}

		/// <summary>
		/// Optional callback invoked when commits enter the list by fillTo.
		/// 
		/// This method is only called during <see cref="fillTo(int)"/>.
		/// </summary>
		/// <param name="index">the list position this object will appear at.</param>
		/// <param name="t">the object being added (or set) into the list.</param>
		protected virtual void enter(int index, T t)
		{
			// Do nothing by default.
		}
		
		public void Dispose ()
		{
			if (_walker != null)			{
			    _walker.Dispose();
			}
		}
		
	}
}