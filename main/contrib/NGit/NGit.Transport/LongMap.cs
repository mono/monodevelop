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

using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// Simple Map<long,Object> helper for
	/// <see cref="PackParser">PackParser</see>
	/// .
	/// </summary>
	/// <?></?>
	internal sealed class LongMap<V>
	{
		private const float LOAD_FACTOR = 0.75f;

		private LongMapNode<V>[] table;

		/// <summary>Number of entries currently in the map.</summary>
		/// <remarks>Number of entries currently in the map.</remarks>
		private int size;

		/// <summary>
		/// Next
		/// <see cref="LongMap{V}.size">LongMap&lt;V&gt;.size</see>
		/// to trigger a
		/// <see cref="LongMap{V}.Grow()">LongMap&lt;V&gt;.Grow()</see>
		/// .
		/// </summary>
		private int growAt;

		public LongMap()
		{
			table = CreateArray<V>(64);
			growAt = (int)(table.Length * LOAD_FACTOR);
		}

		internal bool ContainsKey(long key)
		{
			return Get(key) != null;
		}

		internal V Get(long key)
		{
			for (LongMapNode<V> n = table[Index(key)]; n != null; n = n.next)
			{
				if (n.key == key)
				{
					return n.value;
				}
			}
			return default(V);
		}

		internal V Remove(long key)
		{
			LongMapNode<V> n = table[Index(key)];
			LongMapNode<V> prior = null;
			while (n != null)
			{
				if (n.key == key)
				{
					if (prior == null)
					{
						table[Index(key)] = n.next;
					}
					else
					{
						prior.next = n.next;
					}
					size--;
					return n.value;
				}
				prior = n;
				n = n.next;
			}
			return default(V);
		}

		internal V Put(long key, V value)
		{
			for (LongMapNode<V> n = table[Index(key)]; n != null; n = n.next)
			{
				if (n.key == key)
				{
					V o = n.value;
					n.value = value;
					return o;
				}
			}
			if (++size == growAt)
			{
				Grow();
			}
			Insert(new LongMapNode<V>(key, value));
			return default(V);
		}

		private void Insert(LongMapNode<V> n)
		{
			int idx = Index(n.key);
			n.next = table[idx];
			table[idx] = n;
		}

		private void Grow()
		{
			LongMapNode<V>[] oldTable = table;
			int oldSize = table.Length;
			table = CreateArray<V>(oldSize << 1);
			growAt = (int)(table.Length * LOAD_FACTOR);
			for (int i = 0; i < oldSize; i++)
			{
				LongMapNode<V> e = oldTable[i];
				while (e != null)
				{
					LongMapNode<V> n = e.next;
					Insert(e);
					e = n;
				}
			}
		}

		private int Index(long key)
		{
			int h = (int)(((uint)((int)key)) >> 1);
			h ^= ((int)(((uint)h) >> 20)) ^ ((int)(((uint)h) >> 12));
			return h & (table.Length - 1);
		}

		private static LongMapNode<V>[] CreateArray<V>(int sz)
		{
			return new LongMapNode<V>[sz];
		}
	}

	class LongMapNode<V>
	{
		internal readonly long key;

		internal V value;

		internal LongMapNode<V> next;

		internal LongMapNode(long k, V v)
		{
			key = k;
			value = v;
		}
	}
}
