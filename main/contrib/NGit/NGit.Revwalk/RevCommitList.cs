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
using System.IO;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>
	/// An ordered list of
	/// <see cref="RevCommit">RevCommit</see>
	/// subclasses.
	/// </summary>
	/// <?></?>
	public class RevCommitList<E> : RevObjectList<E> where E:RevCommit
	{
		private RevWalk walker;

		public override void Clear()
		{
			base.Clear();
			walker = null;
		}

		/// <summary>Apply a flag to all commits matching the specified filter.</summary>
		/// <remarks>
		/// Apply a flag to all commits matching the specified filter.
		/// <p>
		/// Same as <code>applyFlag(matching, flag, 0, size())</code>, but without
		/// the incremental behavior.
		/// </remarks>
		/// <param name="matching">
		/// the filter to test commits with. If the filter includes a
		/// commit it will have the flag set; if the filter does not
		/// include the commit the flag will be unset.
		/// </param>
		/// <param name="flag">
		/// the flag to apply (or remove). Applications are responsible
		/// for allocating this flag from the source RevWalk.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// revision filter needed to read additional objects, but an
		/// error occurred while reading the pack files or loose objects
		/// of the repository.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// revision filter needed to read additional objects, but an
		/// object was not of the correct type. Repository corruption may
		/// have occurred.
		/// </exception>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// revision filter needed to read additional objects, but an
		/// object that should be present was not found. Repository
		/// corruption may have occurred.
		/// </exception>
		public virtual void ApplyFlag(RevFilter matching, RevFlag flag)
		{
			ApplyFlag(matching, flag, 0, Count);
		}

		/// <summary>Apply a flag to all commits matching the specified filter.</summary>
		/// <remarks>
		/// Apply a flag to all commits matching the specified filter.
		/// <p>
		/// This version allows incremental testing and application, such as from a
		/// background thread that needs to periodically halt processing and send
		/// updates to the UI.
		/// </remarks>
		/// <param name="matching">
		/// the filter to test commits with. If the filter includes a
		/// commit it will have the flag set; if the filter does not
		/// include the commit the flag will be unset.
		/// </param>
		/// <param name="flag">
		/// the flag to apply (or remove). Applications are responsible
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
		/// <exception cref="System.IO.IOException">
		/// revision filter needed to read additional objects, but an
		/// error occurred while reading the pack files or loose objects
		/// of the repository.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// revision filter needed to read additional objects, but an
		/// object was not of the correct type. Repository corruption may
		/// have occurred.
		/// </exception>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// revision filter needed to read additional objects, but an
		/// object that should be present was not found. Repository
		/// corruption may have occurred.
		/// </exception>
		public virtual void ApplyFlag(RevFilter matching, RevFlag flag, int rangeBegin, int
			 rangeEnd)
		{
			RevWalk w = flag.GetRevWalk();
			rangeEnd = Math.Min(rangeEnd, Count);
			while (rangeBegin < rangeEnd)
			{
				int index = rangeBegin;
				RevObjectListBlock s = contents;
				while (s.shift > 0)
				{
					int i = index >> s.shift;
					index -= i << s.shift;
					s = (RevObjectListBlock)s.contents[i];
				}
				while (rangeBegin++ < rangeEnd && index < BLOCK_SIZE)
				{
					RevCommit c = (RevCommit)s.contents[index++];
					if (matching.Include(w, c))
					{
						c.Add(flag);
					}
					else
					{
						c.Remove(flag);
					}
				}
			}
		}

		/// <summary>Remove the given flag from all commits.</summary>
		/// <remarks>
		/// Remove the given flag from all commits.
		/// <p>
		/// Same as <code>clearFlag(flag, 0, size())</code>, but without the
		/// incremental behavior.
		/// </remarks>
		/// <param name="flag">
		/// the flag to remove. Applications are responsible for
		/// allocating this flag from the source RevWalk.
		/// </param>
		public virtual void ClearFlag(RevFlag flag)
		{
			ClearFlag(flag, 0, Count);
		}

		/// <summary>Remove the given flag from all commits.</summary>
		/// <remarks>
		/// Remove the given flag from all commits.
		/// <p>
		/// This method is actually implemented in terms of:
		/// <code>applyFlag(RevFilter.NONE, flag, rangeBegin, rangeEnd)</code>.
		/// </remarks>
		/// <param name="flag">
		/// the flag to remove. Applications are responsible for
		/// allocating this flag from the source RevWalk.
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
		public virtual void ClearFlag(RevFlag flag, int rangeBegin, int rangeEnd)
		{
			try
			{
				ApplyFlag(RevFilter.NONE, flag, rangeBegin, rangeEnd);
			}
			catch (IOException)
			{
			}
		}

		// Never happen. The filter we use does not throw any
		// exceptions, for any reason.
		/// <summary>Find the next commit that has the given flag set.</summary>
		/// <remarks>Find the next commit that has the given flag set.</remarks>
		/// <param name="flag">the flag to test commits against.</param>
		/// <param name="begin">
		/// first commit index to test at. Applications may wish to begin
		/// at 0, to test the first commit in the list.
		/// </param>
		/// <returns>
		/// index of the first commit at or after index <code>begin</code>
		/// that has the specified flag set on it; -1 if no match is found.
		/// </returns>
		public virtual int IndexOf(RevFlag flag, int begin)
		{
			while (begin < Count)
			{
				int index = begin;
				RevObjectListBlock s = contents;
				while (s.shift > 0)
				{
					int i = index >> s.shift;
					index -= i << s.shift;
					s = (RevObjectListBlock)s.contents[i];
				}
				while (begin++ < Count && index < BLOCK_SIZE)
				{
					RevCommit c = (RevCommit)s.contents[index++];
					if (c.Has(flag))
					{
						return begin;
					}
				}
			}
			return -1;
		}

		/// <summary>Find the next commit that has the given flag set.</summary>
		/// <remarks>Find the next commit that has the given flag set.</remarks>
		/// <param name="flag">the flag to test commits against.</param>
		/// <param name="begin">
		/// first commit index to test at. Applications may wish to begin
		/// at <code>size()-1</code>, to test the last commit in the
		/// list.
		/// </param>
		/// <returns>
		/// index of the first commit at or before index <code>begin</code>
		/// that has the specified flag set on it; -1 if no match is found.
		/// </returns>
		public virtual int LastIndexOf(RevFlag flag, int begin)
		{
			begin = Math.Min(begin, Count - 1);
			while (begin >= 0)
			{
				int index = begin;
				RevObjectListBlock s = contents;
				while (s.shift > 0)
				{
					int i = index >> s.shift;
					index -= i << s.shift;
					s = (RevObjectListBlock)s.contents[i];
				}
				while (begin-- >= 0 && index >= 0)
				{
					RevCommit c = (RevCommit)s.contents[index--];
					if (c.Has(flag))
					{
						return begin;
					}
				}
			}
			return -1;
		}

		/// <summary>Set the revision walker this list populates itself from.</summary>
		/// <remarks>Set the revision walker this list populates itself from.</remarks>
		/// <param name="w">the walker to populate from.</param>
		/// <seealso cref="RevCommitList{E}.FillTo(int)">RevCommitList&lt;E&gt;.FillTo(int)</seealso>
		public virtual void Source(RevWalk w)
		{
			walker = w;
		}

		/// <summary>Is this list still pending more items?</summary>
		/// <returns>
		/// true if
		/// <see cref="RevCommitList{E}.FillTo(int)">RevCommitList&lt;E&gt;.FillTo(int)</see>
		/// might be able to extend the list
		/// size when called.
		/// </returns>
		public virtual bool IsPending()
		{
			return walker != null;
		}

		/// <summary>Ensure this list contains at least a specified number of commits.</summary>
		/// <remarks>
		/// Ensure this list contains at least a specified number of commits.
		/// <p>
		/// The revision walker specified by
		/// <see cref="RevCommitList{E}.Source(RevWalk)">RevCommitList&lt;E&gt;.Source(RevWalk)
		/// 	</see>
		/// is pumped until
		/// the given number of commits are contained in this list. If there are
		/// fewer total commits available from the walk then the method will return
		/// early. Callers can test the final size of the list by
		/// <see cref="RevObjectList{E}.Count()">RevObjectList&lt;E&gt;.Count()</see>
		/// to
		/// determine if the high water mark specified was met.
		/// </remarks>
		/// <param name="highMark">
		/// number of commits the caller wants this list to contain when
		/// the fill operation is complete.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// see
		/// <see cref="RevWalk.Next()">RevWalk.Next()</see>
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// see
		/// <see cref="RevWalk.Next()">RevWalk.Next()</see>
		/// </exception>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// see
		/// <see cref="RevWalk.Next()">RevWalk.Next()</see>
		/// </exception>
		public virtual void FillTo(int highMark)
		{
			if (walker == null || size > highMark)
			{
				return;
			}
			RevCommit c = walker.Next();
			if (c == null)
			{
				walker = null;
				return;
			}
			Enter(size, (E)c);
			AddItem((E)c);
			while (size <= highMark)
			{
				int index = size;
				RevObjectListBlock s = contents;
				while (index >> s.shift >= BLOCK_SIZE)
				{
					s = new RevObjectListBlock(s.shift + BLOCK_SHIFT);
					s.contents[0] = contents;
					contents = s;
				}
				while (s.shift > 0)
				{
					int i = index >> s.shift;
					index -= i << s.shift;
					if (s.contents[i] == null)
					{
						s.contents[i] = new RevObjectListBlock(s.shift - BLOCK_SHIFT);
					}
					s = (RevObjectListBlock)s.contents[i];
				}
				object[] dst = s.contents;
				while (size <= highMark && index < BLOCK_SIZE)
				{
					c = walker.Next();
					if (c == null)
					{
						walker = null;
						return;
					}
					Enter(size++, (E)c);
					dst[index++] = c;
				}
			}
		}

		/// <summary>Optional callback invoked when commits enter the list by fillTo.</summary>
		/// <remarks>
		/// Optional callback invoked when commits enter the list by fillTo.
		/// <p>
		/// This method is only called during
		/// <see cref="RevCommitList{E}.FillTo(int)">RevCommitList&lt;E&gt;.FillTo(int)</see>
		/// .
		/// </remarks>
		/// <param name="index">the list position this object will appear at.</param>
		/// <param name="e">the object being added (or set) into the list.</param>
		protected internal virtual void Enter(int index, E e)
		{
		}
		// Do nothing by default.
	}
}
