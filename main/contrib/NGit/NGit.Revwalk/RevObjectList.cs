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
using NGit;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>
	/// An ordered list of
	/// <see cref="RevObject">RevObject</see>
	/// subclasses.
	/// </summary>
	/// <?></?>
	public class RevObjectList<E> : AbstractList<E> where E:RevObject
	{
		internal const int BLOCK_SHIFT = 8;

		internal const int BLOCK_SIZE = 1 << BLOCK_SHIFT;

		/// <summary>Items stored in this list.</summary>
		/// <remarks>
		/// Items stored in this list.
		/// <p>
		/// If
		/// <see cref="RevObjectListBlock.shift">RevObjectListBlock.shift</see>
		/// = 0 this block holds the list elements; otherwise
		/// it holds pointers to other
		/// <see cref="RevObjectListBlock">RevObjectListBlock</see>
		/// instances which use a shift that
		/// is
		/// <see cref="RevObjectList{E}.BLOCK_SHIFT">RevObjectList&lt;E&gt;.BLOCK_SHIFT</see>
		/// smaller.
		/// </remarks>
		internal RevObjectListBlock contents = new RevObjectListBlock(0);

		/// <summary>Current number of elements in the list.</summary>
		/// <remarks>Current number of elements in the list.</remarks>
		protected internal int size = 0;

		/// <summary>Create an empty object list.</summary>
		/// <remarks>Create an empty object list.</remarks>
		public RevObjectList()
		{
		}

		// Initialized above.
		public override void Add(int index, E element)
		{
			if (index != size)
			{
				throw new NotSupportedException(MessageFormat.Format(JGitText.Get().unsupportedOperationNotAddAtEnd
					, index));
			}
			Set(index, element);
			size++;
		}

		public override E Set(int index, E element)
		{
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
			object old = s.contents[index];
			s.contents[index] = element;
			return (E)old;
		}

		public override E Get(int index)
		{
			RevObjectListBlock s = contents;
			if (index >> s.shift >= 1024)
			{
				return null;
			}
			while (s != null && s.shift > 0)
			{
				int i = index >> s.shift;
				index -= i << s.shift;
				s = (RevObjectListBlock)s.contents[i];
			}
			return s != null ? (E)s.contents[index] : null;
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
			contents = new RevObjectListBlock(0);
			size = 0;
		}
	}

	/// <summary>One level of contents, either an intermediate level or a leaf level.</summary>
	/// <remarks>One level of contents, either an intermediate level or a leaf level.</remarks>
	internal class RevObjectListBlock
	{
		internal readonly object[] contents = new object[RevObjectList<RevObject>.BLOCK_SIZE];

		internal readonly int shift;

		internal RevObjectListBlock(int s)
		{
			shift = s;
		}
	}
}
