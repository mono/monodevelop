/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Jonas Fonseca <fonseca@diku.dk>
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// An ordered list of <see cref="RevObject"/> subclasses.
	/// </summary>
	/// <typeparam name="T">
	/// Type of subclass of RevObject the list is storing.
	/// </typeparam>
	public class RevObjectList<T> : IEnumerable<T> // [henon] was AbstractList
	where T : RevObject
	{
		public static int BLOCK_SHIFT = 8;
		public static int BLOCK_SIZE = 1 << BLOCK_SHIFT;

		/// <summary>
		/// Items stored in this list.
		/// <para>
		/// If <see cref="Block.Shift"/> = 0 this block holds the list elements; otherwise
		/// it holds pointers to other {@link Block} instances which use a shift that
		/// is <see cref="BLOCK_SHIFT"/> smaller.
		/// </para>
		/// </summary>
		protected Block Contents
		{
			get;
			set;
		}

		/// <summary>
		/// Create an empty object list.
		/// </summary>
		public RevObjectList()
		{
			clear();
		}

		/// <summary>
		/// Current number of elements in the list.
		/// </summary>
		protected int Size
		{
			get;
			set;
		}

		public void add(int index, T element)
		{
			if (index != Size)
			{
				throw new InvalidOperationException("Not add-at-end: " + index);
			}

			set(index, element);
			Size++;
		}

		public void add(T element)
		{
			add(Size, element);
		}

		public T set(int index, T element)
		{
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
			object old = s.Contents[index];
			s.Contents[index] = element;
			return (T)old;
		}

		public T get(int index)
		{
			Block s = Contents;

			if (index >> s.Shift >= 1024)
			{
				return null;
			}

			while (s != null && s.Shift > 0)
			{
				int i = index >> s.Shift;
				index -= i << s.Shift;
				s = (Block)s.Contents[i];
			}

			return s != null ? (T)s.Contents[index] : null;
		}

		public virtual void clear()
		{
			Contents = new Block(0);
			Size = 0;
		}

		/// <summary>
		/// One level of contents, either an intermediate level or a leaf level.
		/// </summary>
		public class Block : IEnumerable<T>
		{
			public object[] Contents { get; private set; }
			public int Shift { get; private set; }

			public Block(int s)
			{
				Contents = new object[BLOCK_SIZE];
				Shift = s;
			}

			#region Implementation of IEnumerable<object>

			/// <summary>
			/// Returns an enumerator that iterates through the collection.
			/// </summary>
			/// <returns>
			/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
			/// </returns>
			/// <filterpriority>1</filterpriority>
			public IEnumerator<T> GetEnumerator()
			{
				foreach (object o in Contents)
				{
					if (o == null) continue;
					if (o is Block)
					{
						Block s = (Block)o;
						foreach (object os in s.Contents)
						{
							if (os == null) continue;
							yield return (T)os;
						}
					}
					else
					{
						yield return (T)o;
					}
				}
			}

			#endregion

			#region Implementation of IEnumerable

			/// <summary>
			/// Returns an enumerator that iterates through a collection.
			/// </summary>
			/// <returns>
			/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
			/// </returns>
			/// <filterpriority>2</filterpriority>
			IEnumerator IEnumerable.GetEnumerator()
			{
				return Contents.GetEnumerator();
			}

			#endregion
		}

		#region Implementation of IEnumerable<T>

		public IEnumerator<T> GetEnumerator()
		{
			return Contents.GetEnumerator();
		}

		#endregion

		#region Implementation of IEnumerable

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Contents.GetEnumerator();
		}

		#endregion
	}
}