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
	/// Raw value equality is tested when comparing two ObjectIds (or subclasses),
	/// not reference equality and not <code>.equals(Object)</code> equality. This
	/// allows subclasses to override <code>equals</code> to supply their own
	/// extended semantics.
	/// </summary>
	/// <?></?>
	public class ObjectIdSubclassMap<V> : Iterable<V> where V:ObjectId
	{
		private int size;

		private V[] obj_hash;

		/// <summary>Create an empty map.</summary>
		/// <remarks>Create an empty map.</remarks>
		public ObjectIdSubclassMap()
		{
			obj_hash = CreateArray(32);
		}

		/// <summary>Remove all entries from this map.</summary>
		/// <remarks>Remove all entries from this map.</remarks>
		public virtual void Clear()
		{
			size = 0;
			obj_hash = CreateArray(32);
		}

		/// <summary>Lookup an existing mapping.</summary>
		/// <remarks>Lookup an existing mapping.</remarks>
		/// <param name="toFind">the object identifier to find.</param>
		/// <returns>the instance mapped to toFind, or null if no mapping exists.</returns>
		public virtual V Get(AnyObjectId toFind)
		{
			int i = Index(toFind);
			V obj;
			while ((obj = obj_hash[i]) != null)
			{
				if (AnyObjectId.Equals(obj, toFind))
				{
					return obj;
				}
				if (++i == obj_hash.Length)
				{
					i = 0;
				}
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
		/// mapping prior to adding a new mapping.
		/// </remarks>
		/// <param name="newValue">the object to store.</param>
		/// <?></?>
		public virtual void Add<Q>(Q newValue) where Q:V
		{
			if (obj_hash.Length - 1 <= size * 2)
			{
				Grow();
			}
			Insert(newValue);
			size++;
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
			return new _Iterator_145(this);
		}

		private sealed class _Iterator_145 : Sharpen.Iterator<V>
		{
			public _Iterator_145(ObjectIdSubclassMap<V> _enclosing)
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
				while (this.i < this._enclosing.obj_hash.Length)
				{
					V v = this._enclosing.obj_hash[this.i++];
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

		private int Index(AnyObjectId id)
		{
			return ((int)(((uint)id.w1) >> 1)) % obj_hash.Length;
		}

		private void Insert(V newValue)
		{
			int j = Index(newValue);
			while (obj_hash[j] != null)
			{
				if (++j >= obj_hash.Length)
				{
					j = 0;
				}
			}
			obj_hash[j] = newValue;
		}

		private void Grow()
		{
			V[] old_hash = obj_hash;
			int old_hash_size = obj_hash.Length;
			obj_hash = CreateArray(2 * old_hash_size);
			for (int i = 0; i < old_hash_size; i++)
			{
				V obj = old_hash[i];
				if (obj != null)
				{
					Insert(obj);
				}
			}
		}

		private V[] CreateArray(int sz)
		{
			return new V[sz];
		}
	}
}
