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
using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>
	/// Specialized variant of an ArrayList to support a
	/// <code>RefDatabase</code>
	/// .
	/// <p>
	/// This list is a hybrid of a Map&lt;String,Ref&gt; and of a List&lt;Ref&gt;. It
	/// tracks reference instances by name by keeping them sorted and performing
	/// binary search to locate an entry. Lookup time is O(log N), but addition and
	/// removal is O(N + log N) due to the list expansion or contraction costs.
	/// <p>
	/// This list type is copy-on-write. Mutation methods return a new copy of the
	/// list, leaving
	/// <code>this</code>
	/// unmodified. As a result we cannot easily implement
	/// the
	/// <see cref="System.Collections.IList{E}">System.Collections.IList&lt;E&gt;</see>
	/// interface contract.
	/// </summary>
	/// <?></?>
	public class RefList<T> : Iterable<Ref> where T:Ref
	{
		private static readonly NGit.Util.RefList<Ref> EMPTY = new NGit.Util.RefList<Ref>
			(new Ref[0], 0);

		/// <returns>an empty unmodifiable reference list.</returns>
		/// <?></?>
		public static NGit.Util.RefList<T> EmptyList<T>() where T:Ref
		{
			return new RefList<T>(new Ref[0], 0);
		}

		public static NGit.Util.RefList<Ref> EmptyList()
		{
			return RefList<T>.EMPTY;
		}
		
		private readonly Ref[] list;

		private readonly int cnt;

		internal RefList(Ref[] list, int cnt)
		{
			this.list = list;
			this.cnt = cnt;
		}

		/// <summary>Initialize this list to use the same backing array as another list.</summary>
		/// <remarks>Initialize this list to use the same backing array as another list.</remarks>
		/// <param name="src">the source list.</param>
		protected internal RefList(NGit.Util.RefList<T> src)
		{
			this.list = src.list;
			this.cnt = src.cnt;
		}

		public override Sharpen.Iterator<Ref> Iterator()
		{
			return new _Iterator_104(this);
		}

		private sealed class _Iterator_104 : Sharpen.Iterator<Ref>
		{
			public _Iterator_104(RefList<T> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private int idx;

			public override bool HasNext()
			{
				return this.idx < this._enclosing.cnt;
			}

			public override Ref Next()
			{
				if (this.idx < this._enclosing.cnt)
				{
					return this._enclosing.list[this.idx++];
				}
				throw new NoSuchElementException();
			}

			public override void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly RefList<T> _enclosing;
		}

		/// <returns>
		/// this cast as an immutable, standard
		/// <see cref="System.Collections.IList{E}">System.Collections.IList&lt;E&gt;</see>
		/// .
		/// </returns>
		public IList<Ref> AsList()
		{
			IList<Ref> r = Arrays.AsList(list).SubList(0, cnt);
			return Sharpen.Collections.UnmodifiableList(r);
		}

		/// <returns>number of items in this list.</returns>
		public int Size()
		{
			return cnt;
		}

		/// <returns>true if the size of this list is 0.</returns>
		public bool IsEmpty()
		{
			return cnt == 0;
		}

		/// <summary>Locate an entry by name.</summary>
		/// <remarks>Locate an entry by name.</remarks>
		/// <param name="name">the name of the reference to find.</param>
		/// <returns>
		/// the index the reference is at. If the entry is not present
		/// returns a negative value. The insertion position for the given
		/// name can be computed from
		/// <code>-(index + 1)</code>
		/// .
		/// </returns>
		public int Find(string name)
		{
			int high = cnt;
			if (high == 0)
			{
				return -1;
			}
			int low = 0;
			do
			{
				int mid = (int)(((uint)(low + high)) >> 1);
				int cmp = RefComparator.CompareTo(list[mid], name);
				if (cmp < 0)
				{
					low = mid + 1;
				}
				else
				{
					if (cmp == 0)
					{
						return mid;
					}
					else
					{
						high = mid;
					}
				}
			}
			while (low < high);
			return -(low + 1);
		}

		/// <summary>Determine if a reference is present.</summary>
		/// <remarks>Determine if a reference is present.</remarks>
		/// <param name="name">name of the reference to find.</param>
		/// <returns>true if the reference is present; false if it is not.</returns>
		public bool Contains(string name)
		{
			return 0 <= Find(name);
		}

		/// <summary>Get a reference object by name.</summary>
		/// <remarks>Get a reference object by name.</remarks>
		/// <param name="name">the name of the reference.</param>
		/// <returns>the reference object; null if it does not exist in this list.</returns>
		public T Get(string name)
		{
			int idx = Find(name);
			return 0 <= idx ? Get(idx) : default(T);
		}

		/// <summary>Get the reference at a particular index.</summary>
		/// <remarks>Get the reference at a particular index.</remarks>
		/// <param name="idx">
		/// the index to obtain. Must be
		/// <code>0 &lt;= idx &lt; size()</code>
		/// .
		/// </param>
		/// <returns>the reference value, never null.</returns>
		public T Get(int idx)
		{
			return (T)list[idx];
		}

		internal static RefList<Ref> Copy<U>(RefList<U> other) where U: Ref
		{
		    return new RefList<Ref>(other.list, other.cnt);
		}
 
		/// <summary>
		/// Obtain a builder initialized with the first
		/// <code>n</code>
		/// elements.
		/// <p>
		/// Copies the first
		/// <code>n</code>
		/// elements from this list into a new builder,
		/// which can be used by the caller to add additional elements.
		/// </summary>
		/// <param name="n">the number of elements to copy.</param>
		/// <returns>
		/// a new builder with the first
		/// <code>n</code>
		/// elements already added.
		/// </returns>
		public RefListBuilder<T> Copy(int n)
		{
			RefListBuilder<T> r = new RefListBuilder<T>(Math.Max(16, n));
			r.AddAll(list, 0, n);
			return r;
		}

		/// <summary>Obtain a new copy of the list after changing one element.</summary>
		/// <remarks>
		/// Obtain a new copy of the list after changing one element.
		/// <p>
		/// This list instance is not affected by the replacement. Because this
		/// method copies the entire list, it runs in O(N) time.
		/// </remarks>
		/// <param name="idx">index of the element to change.</param>
		/// <param name="ref">the new value, must not be null.</param>
		/// <returns>
		/// copy of this list, after replacing
		/// <code>idx</code>
		/// with
		/// <code>ref</code>
		/// .
		/// </returns>
		public NGit.Util.RefList<T> Set(int idx, T @ref)
		{
			Ref[] newList = new Ref[cnt];
			System.Array.Copy(list, 0, newList, 0, cnt);
			newList[idx] = @ref;
			return new NGit.Util.RefList<T>(newList, cnt);
		}

		/// <summary>Add an item at a specific index.</summary>
		/// <remarks>
		/// Add an item at a specific index.
		/// <p>
		/// This list instance is not affected by the addition. Because this method
		/// copies the entire list, it runs in O(N) time.
		/// </remarks>
		/// <param name="idx">
		/// position to add the item at. If negative the method assumes it
		/// was a direct return value from
		/// <see cref="RefList{T}.Find(string)">RefList&lt;T&gt;.Find(string)</see>
		/// and will
		/// adjust it to the correct position.
		/// </param>
		/// <param name="ref">the new reference to insert.</param>
		/// <returns>
		/// copy of this list, after making space for and adding
		/// <code>ref</code>
		/// .
		/// </returns>
		public NGit.Util.RefList<T> Add(int idx, T @ref)
		{
			if (idx < 0)
			{
				idx = -(idx + 1);
			}
			Ref[] newList = new Ref[cnt + 1];
			if (0 < idx)
			{
				System.Array.Copy(list, 0, newList, 0, idx);
			}
			newList[idx] = @ref;
			if (idx < cnt)
			{
				System.Array.Copy(list, idx, newList, idx + 1, cnt - idx);
			}
			return new NGit.Util.RefList<T>(newList, cnt + 1);
		}

		/// <summary>Remove an item at a specific index.</summary>
		/// <remarks>
		/// Remove an item at a specific index.
		/// <p>
		/// This list instance is not affected by the addition. Because this method
		/// copies the entire list, it runs in O(N) time.
		/// </remarks>
		/// <param name="idx">position to remove the item from.</param>
		/// <returns>
		/// copy of this list, after making removing the item at
		/// <code>idx</code>
		/// .
		/// </returns>
		public NGit.Util.RefList<T> Remove(int idx)
		{
			if (cnt == 1)
			{
				return EmptyList<T>();
			}
			Ref[] newList = new Ref[cnt - 1];
			if (0 < idx)
			{
				System.Array.Copy(list, 0, newList, 0, idx);
			}
			if (idx + 1 < cnt)
			{
				System.Array.Copy(list, idx + 1, newList, idx, cnt - (idx + 1));
			}
			return new NGit.Util.RefList<T>(newList, cnt - 1);
		}

		/// <summary>Store a reference, adding or replacing as necessary.</summary>
		/// <remarks>
		/// Store a reference, adding or replacing as necessary.
		/// <p>
		/// This list instance is not affected by the store. The correct position is
		/// determined, and the item is added if missing, or replaced if existing.
		/// Because this method copies the entire list, it runs in O(N + log N) time.
		/// </remarks>
		/// <param name="ref">the reference to store.</param>
		/// <returns>copy of this list, after performing the addition or replacement.</returns>
		public NGit.Util.RefList<T> Put(T @ref)
		{
			int idx = Find(@ref.GetName());
			if (0 <= idx)
			{
				return Set(idx, @ref);
			}
			return Add(idx, @ref);
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append('[');
			if (cnt > 0)
			{
				r.Append(list[0]);
				for (int i = 1; i < cnt; i++)
				{
					r.Append(", ");
					r.Append(list[i]);
				}
			}
			r.Append(']');
			return r.ToString();
		}
	}

	/// <summary>Builder to facilitate fast construction of an immutable RefList.</summary>
	/// <remarks>Builder to facilitate fast construction of an immutable RefList.</remarks>
	/// <?></?>
	public class RefListBuilder<T> where T:Ref
	{
		private Ref[] list;

		private int size;

		/// <summary>Create an empty list ready for items to be added.</summary>
		/// <remarks>Create an empty list ready for items to be added.</remarks>
		public RefListBuilder() : this(16)
		{
		}

		/// <summary>Create an empty list with at least the specified capacity.</summary>
		/// <remarks>Create an empty list with at least the specified capacity.</remarks>
		/// <param name="capacity">the new capacity.</param>
		public RefListBuilder(int capacity)
		{
			list = new Ref[capacity];
		}

		/// <returns>number of items in this builder's internal collection.</returns>
		public virtual int Size()
		{
			return size;
		}

		/// <summary>Get the reference at a particular index.</summary>
		/// <remarks>Get the reference at a particular index.</remarks>
		/// <param name="idx">
		/// the index to obtain. Must be
		/// <code>0 &lt;= idx &lt; size()</code>
		/// .
		/// </param>
		/// <returns>the reference value, never null.</returns>
		public virtual T Get(int idx)
		{
			return (T)list[idx];
		}

		/// <summary>Remove an item at a specific index.</summary>
		/// <remarks>Remove an item at a specific index.</remarks>
		/// <param name="idx">position to remove the item from.</param>
		public virtual void Remove(int idx)
		{
			System.Array.Copy(list, idx + 1, list, idx, size - (idx + 1));
			size--;
		}

		/// <summary>Add the reference to the end of the array.</summary>
		/// <remarks>
		/// Add the reference to the end of the array.
		/// <p>
		/// References must be added in sort order, or the array must be sorted
		/// after additions are complete using
		/// <see cref="Builder{T}.Sort()">Builder&lt;T&gt;.Sort()</see>
		/// .
		/// </remarks>
		/// <param name="ref"></param>
		public virtual void Add(T @ref)
		{
			if (list.Length == size)
			{
				Ref[] n = new Ref[size * 2];
				System.Array.Copy(list, 0, n, 0, size);
				list = n;
			}
			list[size++] = @ref;
		}

		/// <summary>Add all items from a source array.</summary>
		/// <remarks>
		/// Add all items from a source array.
		/// <p>
		/// References must be added in sort order, or the array must be sorted
		/// after additions are complete using
		/// <see cref="Builder{T}.Sort()">Builder&lt;T&gt;.Sort()</see>
		/// .
		/// </remarks>
		/// <param name="src">the source array.</param>
		/// <param name="off">
		/// position within
		/// <code>src</code>
		/// to start copying from.
		/// </param>
		/// <param name="cnt">
		/// number of items to copy from
		/// <code>src</code>
		/// .
		/// </param>
		public virtual void AddAll(Ref[] src, int off, int cnt)
		{
			if (list.Length < size + cnt)
			{
				Ref[] n = new Ref[Math.Max(size * 2, size + cnt)];
				System.Array.Copy(list, 0, n, 0, size);
				list = n;
			}
			System.Array.Copy(src, off, list, size, cnt);
			size += cnt;
		}

		/// <summary>Replace a single existing element.</summary>
		/// <remarks>Replace a single existing element.</remarks>
		/// <param name="idx">index, must have already been added previously.</param>
		/// <param name="ref">the new reference.</param>
		public virtual void Set(int idx, T @ref)
		{
			list[idx] = @ref;
		}

		/// <summary>Sort the list's backing array in-place.</summary>
		/// <remarks>Sort the list's backing array in-place.</remarks>
		public virtual void Sort()
		{
			Arrays.Sort(list, 0, size, RefComparator.INSTANCE);
		}

		/// <returns>an unmodifiable list using this collection's backing array.</returns>
		public virtual RefList<T> ToRefList()
		{
			return new RefList<T>(list, size);
		}

		public override string ToString()
		{
			return ToRefList().ToString();
		}
	}

	internal class RefList : RefList<Ref>
	{
		// Methods
		public RefList() : base(new Ref[0], 0)
		{
		}
	}
	
	
}
