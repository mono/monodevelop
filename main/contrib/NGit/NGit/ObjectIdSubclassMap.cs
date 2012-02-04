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
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Fast, efficient map specifically for
	/// <see cref="ObjectId">ObjectId</see>
	/// subclasses.
	/// <p>
	/// This map provides an efficient translation from any ObjectId instance to a
	/// cached subclass of ObjectId that has the same value.
	/// <p>
	/// If object instances are stored in only one map,
	/// <see cref="ObjectIdOwnerMap{V}">ObjectIdOwnerMap&lt;V&gt;</see>
	/// is a
	/// more efficient implementation.
	/// </summary>
	/// <?></?>
	public class ObjectIdSubclassMap<V> : Iterable<V> where V:ObjectId
	{
		private const int INITIAL_TABLE_SIZE = 2048;

		private int size;

		private int grow;

		private int mask;

		private V[] table;

		/// <summary>Create an empty map.</summary>
		/// <remarks>Create an empty map.</remarks>
		public ObjectIdSubclassMap()
		{
			InitTable(INITIAL_TABLE_SIZE);
		}

		/// <summary>Remove all entries from this map.</summary>
		/// <remarks>Remove all entries from this map.</remarks>
		public virtual void Clear()
		{
			size = 0;
			InitTable(INITIAL_TABLE_SIZE);
		}

		/// <summary>Lookup an existing mapping.</summary>
		/// <remarks>Lookup an existing mapping.</remarks>
		/// <param name="toFind">the object identifier to find.</param>
		/// <returns>the instance mapped to toFind, or null if no mapping exists.</returns>
		public virtual V Get(AnyObjectId toFind)
		{
			int msk = mask;
			int i = toFind.w1 & msk;
			V[] tbl = table;
			V obj;
			while ((obj = tbl[i]) != null)
			{
				if (AnyObjectId.Equals(obj, toFind))
				{
					return obj;
				}
				i = (i + 1) & msk;
			}
			return null;
		}

		/// <summary>Returns true if this map contains the specified object.</summary>
		/// <remarks>Returns true if this map contains the specified object.</remarks>
		/// <param name="toFind">object to find.</param>
		/// <returns>true if the mapping exists for this object; false otherwise.</returns>
		public virtual bool Contains(AnyObjectId toFind)
		{
			return Get(toFind) != null;
		}

		/// <summary>Store an object for future lookup.</summary>
		/// <remarks>
		/// Store an object for future lookup.
		/// <p>
		/// An existing mapping for <b>must not</b> be in this map. Callers must
		/// first call
		/// <see cref="ObjectIdSubclassMap{V}.Get(AnyObjectId)">ObjectIdSubclassMap&lt;V&gt;.Get(AnyObjectId)
		/// 	</see>
		/// to verify there is no current
		/// mapping prior to adding a new mapping, or use
		/// <see cref="ObjectIdSubclassMap{V}.AddIfAbsent{Q}(ObjectId)">ObjectIdSubclassMap&lt;V&gt;.AddIfAbsent&lt;Q&gt;(ObjectId)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="newValue">the object to store.</param>
		/// <?></?>
		public virtual void Add<Q>(Q newValue) where Q:V
		{
			if (++size == grow)
			{
				Grow();
			}
			Insert(newValue);
		}

		/// <summary>Store an object for future lookup.</summary>
		/// <remarks>
		/// Store an object for future lookup.
		/// <p>
		/// Stores
		/// <code>newValue</code>
		/// , but only if there is not already an object for
		/// the same object name. Callers can tell if the value is new by checking
		/// the return value with reference equality:
		/// <pre>
		/// V obj = ...;
		/// boolean wasNew = map.addIfAbsent(obj) == obj;
		/// </pre>
		/// </remarks>
		/// <param name="newValue">the object to store.</param>
		/// <returns>
		/// 
		/// <code>newValue</code>
		/// if stored, or the prior value already stored and
		/// that would have been returned had the caller used
		/// <code>get(newValue)</code>
		/// first.
		/// </returns>
		/// <?></?>
		public virtual V AddIfAbsent<Q>(Q newValue) where Q:V
		{
			int msk = mask;
			int i = ((ObjectId)newValue).w1 & msk;
			V[] tbl = table;
			V obj;
			while ((obj = tbl[i]) != null)
			{
				if (AnyObjectId.Equals(obj, newValue))
				{
					return obj;
				}
				i = (i + 1) & msk;
			}
			if (++size == grow)
			{
				Grow();
				Insert(newValue);
			}
			else
			{
				tbl[i] = newValue;
			}
			return newValue;
		}

		/// <returns>number of objects in map</returns>
		public virtual int Size()
		{
			return size;
		}

		/// <returns>
		/// true if
		/// <see cref="ObjectIdSubclassMap{V}.Size()">ObjectIdSubclassMap&lt;V&gt;.Size()</see>
		/// is 0.
		/// </returns>
		public virtual bool IsEmpty()
		{
			return size == 0;
		}

		public override Sharpen.Iterator<V> Iterator()
		{
			return new _Iterator_190(this);
		}

		private sealed class _Iterator_190 : Sharpen.Iterator<V>
		{
			public _Iterator_190(ObjectIdSubclassMap<V> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private int found;

			private int i;

			public override bool HasNext()
			{
				return this.found < this._enclosing.size;
			}

			public override V Next()
			{
				while (this.i < this._enclosing.table.Length)
				{
					V v = this._enclosing.table[this.i++];
					if (v != null)
					{
						this.found++;
						return v;
					}
				}
				throw new NoSuchElementException();
			}

			public override void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly ObjectIdSubclassMap<V> _enclosing;
		}

		private void Insert(V newValue)
		{
			int msk = mask;
			int j = newValue.w1 & msk;
			V[] tbl = table;
			while (tbl[j] != null)
			{
				j = (j + 1) & msk;
			}
			tbl[j] = newValue;
		}

		private void Grow()
		{
			V[] oldTable = table;
			int oldSize = table.Length;
			InitTable(oldSize << 1);
			for (int i = 0; i < oldSize; i++)
			{
				V obj = oldTable[i];
				if (obj != null)
				{
					Insert(obj);
				}
			}
		}

		private void InitTable(int sz)
		{
			grow = sz >> 1;
			mask = sz - 1;
			table = CreateArray(sz);
		}

		private V[] CreateArray(int sz)
		{
			return new V[sz];
		}
	}
}
