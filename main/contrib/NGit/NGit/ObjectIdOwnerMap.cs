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
	/// Fast, efficient map for
	/// <see cref="ObjectId">ObjectId</see>
	/// subclasses in only one map.
	/// <p>
	/// To use this map type, applications must have their entry value type extend
	/// from
	/// <see cref="Entry">Entry</see>
	/// , which itself extends from ObjectId.
	/// <p>
	/// Object instances may only be stored in <b>ONE</b> ObjectIdOwnerMap. This
	/// restriction exists because the map stores internal map state within each
	/// object instance. If an instance is be placed in another ObjectIdOwnerMap it
	/// could corrupt one or both map's internal state.
	/// <p>
	/// If an object instance must be in more than one map, applications may use
	/// ObjectIdOwnerMap for one of the maps, and
	/// <see cref="ObjectIdSubclassMap{V}">ObjectIdSubclassMap&lt;V&gt;</see>
	/// for the
	/// other map(s). It is encouraged to use ObjectIdOwnerMap for the map that is
	/// accessed most often, as this implementation runs faster than the more general
	/// ObjectIdSubclassMap implementation.
	/// </summary>
	/// <?></?>
	public class ObjectIdOwnerMap<V> : Iterable<V> where V:ObjectIdOwnerMap.Entry
	{
		/// <summary>Size of the initial directory, will grow as necessary.</summary>
		/// <remarks>Size of the initial directory, will grow as necessary.</remarks>
		private const int INITIAL_DIRECTORY = 1024;

		/// <summary>Number of bits in a segment's index.</summary>
		/// <remarks>Number of bits in a segment's index. Segments are 2^11 in size.</remarks>
		private const int SEGMENT_BITS = 11;

		private const int SEGMENT_SHIFT = 32 - SEGMENT_BITS;

		/// <summary>Top level directory of the segments.</summary>
		/// <remarks>
		/// Top level directory of the segments.
		/// <p>
		/// The low
		/// <see cref="ObjectIdOwnerMap{V}.bits">ObjectIdOwnerMap&lt;V&gt;.bits</see>
		/// of the SHA-1 are used to select the segment from
		/// this directory. Each segment is constant sized at 2^SEGMENT_BITS.
		/// </remarks>
		private V[][] directory;

		/// <summary>Total number of objects in this map.</summary>
		/// <remarks>Total number of objects in this map.</remarks>
		private int size;

		/// <summary>
		/// The map doubles in capacity when
		/// <see cref="ObjectIdOwnerMap{V}.size">ObjectIdOwnerMap&lt;V&gt;.size</see>
		/// reaches this target.
		/// </summary>
		private int grow;

		/// <summary>
		/// Number of low bits used to form the index into
		/// <see cref="ObjectIdOwnerMap{V}.directory">ObjectIdOwnerMap&lt;V&gt;.directory</see>
		/// .
		/// </summary>
		private int bits;

		/// <summary>
		/// Low bit mask to index into
		/// <see cref="ObjectIdOwnerMap{V}.directory">ObjectIdOwnerMap&lt;V&gt;.directory</see>
		/// ,
		/// <code>2^bits-1</code>
		/// .
		/// </summary>
		private int mask;

		/// <summary>Create an empty map.</summary>
		/// <remarks>Create an empty map.</remarks>
		public ObjectIdOwnerMap()
		{
			bits = 0;
			mask = 0;
			grow = ComputeGrowAt(bits);
			directory = new V[INITIAL_DIRECTORY][];
			directory[0] = NewSegment();
		}

		/// <summary>Remove all entries from this map.</summary>
		/// <remarks>Remove all entries from this map.</remarks>
		public virtual void Clear()
		{
			size = 0;
			foreach (V[] tbl in directory)
			{
				if (tbl == null)
				{
					break;
				}
				Arrays.Fill(tbl, null);
			}
		}

		/// <summary>Lookup an existing mapping.</summary>
		/// <remarks>Lookup an existing mapping.</remarks>
		/// <param name="toFind">the object identifier to find.</param>
		/// <returns>the instance mapped to toFind, or null if no mapping exists.</returns>
		public virtual V Get(AnyObjectId toFind)
		{
			int h = toFind.w1;
			V obj = directory[h & mask][(int)(((uint)h) >> SEGMENT_SHIFT)];
			for (; obj != null; obj = (V)obj.next)
			{
				if (Equals(obj, toFind))
				{
					return obj;
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
		/// <see cref="ObjectIdOwnerMap{V}.Get(AnyObjectId)">ObjectIdOwnerMap&lt;V&gt;.Get(AnyObjectId)
		/// 	</see>
		/// to verify there is no current
		/// mapping prior to adding a new mapping, or use
		/// <see cref="ObjectIdOwnerMap{V}.AddIfAbsent{Q}(Entry)">ObjectIdOwnerMap&lt;V&gt;.AddIfAbsent&lt;Q&gt;(Entry)
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
			int h = ((V)newValue).w1;
			V[] table = directory[h & mask];
			h = (int)(((uint)h) >> SEGMENT_SHIFT);
			newValue.next = table[h];
			table[h] = newValue;
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
			int h = ((V)newValue).w1;
			V[] table = directory[h & mask];
			h = (int)(((uint)h) >> SEGMENT_SHIFT);
			for (V obj = table[h]; obj != null; obj = (V)obj.next)
			{
				if (Equals(obj, newValue))
				{
					return obj;
				}
			}
			newValue.next = table[h];
			table[h] = newValue;
			if (++size == grow)
			{
				Grow();
			}
			return newValue;
		}

		/// <returns>number of objects in this map.</returns>
		public virtual int Size()
		{
			return size;
		}

		/// <returns>
		/// true if
		/// <see cref="ObjectIdOwnerMap{V}.Size()">ObjectIdOwnerMap&lt;V&gt;.Size()</see>
		/// is 0.
		/// </returns>
		public virtual bool IsEmpty()
		{
			return size == 0;
		}

		public override Sharpen.Iterator<V> Iterator()
		{
			return new _Iterator_223(this);
		}

		private sealed class _Iterator_223 : Sharpen.Iterator<V>
		{
			public _Iterator_223(ObjectIdOwnerMap<V> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private int found;

			private int dirIdx;

			private int tblIdx;

			private V nextv;

			public override bool HasNext()
			{
				return this.found < this._enclosing.size;
			}

			public override V Next()
			{
				if (this.nextv != null)
				{
					return this.Foundv(this.nextv);
				}
				for (; ; )
				{
					V[] table = this._enclosing.directory[this.dirIdx];
					if (this.tblIdx == table.Length)
					{
						if (++this.dirIdx >= (1 << this._enclosing.bits))
						{
							throw new NoSuchElementException();
						}
						table = this._enclosing.directory[this.dirIdx];
						this.tblIdx = 0;
					}
					while (this.tblIdx < table.Length)
					{
						V v = table[this.tblIdx++];
						if (v != null)
						{
							return this.Foundv(v);
						}
					}
				}
			}

			private V Foundv(V v)
			{
				this.found++;
				this.nextv = (V)v.next;
				return v;
			}

			public override void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly ObjectIdOwnerMap<V> _enclosing;
		}

		private void Grow()
		{
			int oldDirLen = 1 << bits;
			int s = 1 << bits;
			bits++;
			mask = (1 << bits) - 1;
			grow = ComputeGrowAt(bits);
			// Quadruple the directory if it needs to expand. Expanding the
			// directory is expensive because it generates garbage, so try
			// to avoid doing it often.
			//
			int newDirLen = 1 << bits;
			if (directory.Length < newDirLen)
			{
				V[][] newDir = (V[][])new ObjectIdOwnerMap.Entry[newDirLen << 1][];
				System.Array.Copy(directory, 0, newDir, 0, oldDirLen);
				directory = newDir;
			}
			// For every bucket of every old segment, split the chain between
			// the old segment and the new segment's corresponding bucket. To
			// select between them use the lowest bit that was just added into
			// the mask above. This causes the table to double in capacity.
			//
			for (int dirIdx = 0; dirIdx < oldDirLen; dirIdx++)
			{
				V[] oldTable = directory[dirIdx];
				V[] newTable = NewSegment();
				for (int i = 0; i < oldTable.Length; i++)
				{
					V chain0 = null;
					V chain1 = null;
					V next;
					for (V obj = oldTable[i]; obj != null; obj = next)
					{
						next = (V)obj.next;
						if ((obj.w1 & s) == 0)
						{
							obj.next = chain0;
							chain0 = obj;
						}
						else
						{
							obj.next = chain1;
							chain1 = obj;
						}
					}
					oldTable[i] = chain0;
					newTable[i] = chain1;
				}
				directory[oldDirLen + dirIdx] = newTable;
			}
		}

		private V[] NewSegment()
		{
			return new V[1 << SEGMENT_BITS];
		}

		private static int ComputeGrowAt(int bits)
		{
			return 1 << (bits + SEGMENT_BITS);
		}

		private static bool Equals(AnyObjectId firstObjectId, AnyObjectId secondObjectId)
		{
			return firstObjectId.w2 == secondObjectId.w2 && firstObjectId.w3 == secondObjectId
				.w3 && firstObjectId.w4 == secondObjectId.w4 && firstObjectId.w5 == secondObjectId
				.w5 && firstObjectId.w1 == secondObjectId.w1;
		}
	}
	
	public class ObjectIdOwnerMap
	{
		/// <summary>
		/// Type of entry stored in the
		/// <see cref="ObjectIdOwnerMap{V}">ObjectIdOwnerMap&lt;V&gt;</see>
		/// .
		/// </summary>
		[System.Serializable]
		public abstract class Entry : ObjectId
		{
			internal ObjectIdOwnerMap.Entry next;

			/// <summary>Initialize this entry with a specific ObjectId.</summary>
			/// <remarks>Initialize this entry with a specific ObjectId.</remarks>
			/// <param name="id">the id the entry represents.</param>
			protected internal Entry(AnyObjectId id) : base(id)
			{
			}
		}
	}
}
