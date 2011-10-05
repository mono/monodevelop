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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>
	/// Specialized Map to present a
	/// <code>RefDatabase</code>
	/// namespace.
	/// <p>
	/// Although not declared as a
	/// <see cref="Sharpen.SortedMap{K, V}">Sharpen.SortedMap&lt;K, V&gt;</see>
	/// , iterators from this
	/// map's projections always return references in
	/// <see cref="NGit.RefComparator">NGit.RefComparator</see>
	/// ordering.
	/// The map's internal representation is a sorted array of
	/// <see cref="NGit.Ref">NGit.Ref</see>
	/// objects,
	/// which means lookup and replacement is O(log N), while insertion and removal
	/// can be as expensive as O(N + log N) while the list expands or contracts.
	/// Since this is not a general map implementation, all entries must be keyed by
	/// the reference name.
	/// <p>
	/// This class is really intended as a helper for
	/// <code>RefDatabase</code>
	/// , which
	/// needs to perform a merge-join of three sorted
	/// <see cref="RefList{T}">RefList&lt;T&gt;</see>
	/// s in order to
	/// present the unified namespace of the packed-refs file, the loose refs/
	/// directory tree, and the resolved form of any symbolic references.
	/// </summary>
	public class RefMap : AbstractMap<string, Ref>
	{
		/// <summary>Prefix denoting the reference subspace this map contains.</summary>
		/// <remarks>
		/// Prefix denoting the reference subspace this map contains.
		/// <p>
		/// All reference names in this map must start with this prefix. If the
		/// prefix is not the empty string, it must end with a '/'.
		/// </remarks>
		private readonly string prefix;

		/// <summary>Immutable collection of the packed references at construction time.</summary>
		/// <remarks>Immutable collection of the packed references at construction time.</remarks>
		private RefList<Ref> packed;

		/// <summary>Immutable collection of the loose references at construction time.</summary>
		/// <remarks>
		/// Immutable collection of the loose references at construction time.
		/// <p>
		/// If an entry appears here and in
		/// <see cref="packed">packed</see>
		/// , this entry must take
		/// precedence, as its more current. Symbolic references in this collection
		/// are typically unresolved, so they only tell us who their target is, but
		/// not the current value of the target.
		/// </remarks>
		private RefList<Ref> loose;

		/// <summary>Immutable collection of resolved symbolic references.</summary>
		/// <remarks>
		/// Immutable collection of resolved symbolic references.
		/// <p>
		/// This collection contains only the symbolic references we were able to
		/// resolve at map construction time. Other loose references must be read
		/// from
		/// <see cref="loose">loose</see>
		/// . Every entry in this list must be matched by an entry
		/// in
		/// <code>loose</code>
		/// , otherwise it might be omitted by the map.
		/// </remarks>
		private RefList<Ref> resolved;

		private int size;

		private bool sizeIsValid;

		private ICollection<KeyValuePair<string, Ref>> entrySet;

		/// <summary>Construct an empty map with a small initial capacity.</summary>
		/// <remarks>Construct an empty map with a small initial capacity.</remarks>
		public RefMap()
		{
			prefix = string.Empty;
			packed = RefList.EmptyList();
			loose = RefList.EmptyList();
			resolved = RefList.EmptyList();
		}

		/// <summary>Construct a map to merge 3 collections together.</summary>
		/// <remarks>Construct a map to merge 3 collections together.</remarks>
		/// <param name="prefix">
		/// prefix used to slice the lists down. Only references whose
		/// names start with this prefix will appear to reside in the map.
		/// Must not be null, use
		/// <code>""</code>
		/// (the empty string) to select
		/// all list items.
		/// </param>
		/// <param name="packed">
		/// items from the packed reference list, this is the last list
		/// searched.
		/// </param>
		/// <param name="loose">
		/// items from the loose reference list, this list overrides
		/// <code>packed</code>
		/// if a name appears in both.
		/// </param>
		/// <param name="resolved">
		/// resolved symbolic references. This list overrides the prior
		/// list
		/// <code>loose</code>
		/// , if an item appears in both. Items in this
		/// list <b>must</b> also appear in
		/// <code>loose</code>
		/// .
		/// </param>
		public RefMap(string prefix, RefList<Ref> packed, RefList<Ref> loose, RefList<Ref
			> resolved)
		{
			this.prefix = prefix;
			this.packed = (RefList<Ref>)packed;
			this.loose = (RefList<Ref>)loose;
			this.resolved = (RefList<Ref>)resolved;
		}

		public override bool ContainsKey(object name)
		{
			return Get(name) != null;
		}

		public override Ref Get(object key)
		{
			string name = ToRefName((string)key);
			Ref @ref = resolved.Get(name);
			if (@ref == null)
			{
				@ref = loose.Get(name);
			}
			if (@ref == null)
			{
				@ref = packed.Get(name);
			}
			return @ref;
		}

		public override Ref Put(string keyName, Ref value)
		{
			string name = ToRefName(keyName);
			if (!name.Equals(value.GetName()))
			{
				throw new ArgumentException();
			}
			if (!resolved.IsEmpty())
			{
				// Collapse the resolved list into the loose list so we
				// can discard it and stop joining the two together.
				foreach (Ref @ref in resolved)
				{
					loose = loose.Put(@ref);
				}
				resolved = RefList.EmptyList();
			}
			int idx = loose.Find(name);
			if (0 <= idx)
			{
				Ref prior = loose.Get(name);
				loose = loose.Set(idx, value);
				return prior;
			}
			else
			{
				Ref prior = Get(keyName);
				loose = loose.Add(idx, value);
				sizeIsValid = false;
				return prior;
			}
		}

		public override Ref Remove(object key)
		{
			string name = ToRefName((string)key);
			Ref res = null;
			int idx;
			if (0 <= (idx = packed.Find(name)))
			{
				res = packed.Get(name);
				packed = packed.Remove(idx);
				sizeIsValid = false;
			}
			if (0 <= (idx = loose.Find(name)))
			{
				res = loose.Get(name);
				loose = loose.Remove(idx);
				sizeIsValid = false;
			}
			if (0 <= (idx = resolved.Find(name)))
			{
				res = resolved.Get(name);
				resolved = resolved.Remove(idx);
				sizeIsValid = false;
			}
			return res;
		}

		public override bool IsEmpty()
		{
			return EntrySet().IsEmpty();
		}

		public override ICollection<KeyValuePair<string, Ref>> EntrySet()
		{
			if (entrySet == null)
			{
				entrySet = new _AbstractSet_223(this);
			}
			return entrySet;
		}

		private sealed class _AbstractSet_223 : AbstractSet<KeyValuePair<string, Ref>>
		{
			public _AbstractSet_223(RefMap _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override Iterator<KeyValuePair<string, Ref>> Iterator()
			{
				return new RefMap.SetIterator(_enclosing);
			}

			public override int Count
			{
				get
				{
					if (!this._enclosing.sizeIsValid)
					{
						this._enclosing.size = 0;
						Iterator<KeyValuePair<string,Ref>> i = this._enclosing.EntrySet().Iterator();
						for (; i.HasNext(); i.Next())
						{
							this._enclosing.size++;
						}
						this._enclosing.sizeIsValid = true;
					}
					return this._enclosing.size;
				}
			}

			public override bool IsEmpty()
			{
				if (this._enclosing.sizeIsValid)
				{
					return 0 == this._enclosing.size;
				}
				return !this.Iterator().HasNext();
			}

			public override void Clear()
			{
				this._enclosing.packed = RefList.EmptyList();
				this._enclosing.loose = RefList.EmptyList();
				this._enclosing.resolved = RefList.EmptyList();
				this._enclosing.size = 0;
				this._enclosing.sizeIsValid = true;
			}

			private readonly RefMap _enclosing;
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			bool first = true;
			r.Append('[');
			foreach (Ref @ref in Values)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					r.Append(", ");
				}
				r.Append(@ref);
			}
			r.Append(']');
			return r.ToString();
		}

		private string ToRefName(string name)
		{
			if (0 < prefix.Length)
			{
				name = prefix + name;
			}
			return name;
		}

		private string ToMapKey(Ref @ref)
		{
			string name = @ref.GetName();
			if (0 < prefix.Length)
			{
				name = Sharpen.Runtime.Substring(name, prefix.Length);
			}
			return name;
		}

		private class SetIterator : Iterator<KeyValuePair<string, Ref>>
		{
			private int packedIdx;

			private int looseIdx;

			private int resolvedIdx;

			private Ent next;

			public SetIterator(RefMap _enclosing)
			{
				this._enclosing = _enclosing;
				if (0 < this._enclosing.prefix.Length)
				{
					this.packedIdx = -(this._enclosing.packed.Find(this._enclosing.prefix) + 1);
					this.looseIdx = -(this._enclosing.loose.Find(this._enclosing.prefix) + 1);
					this.resolvedIdx = -(this._enclosing.resolved.Find(this._enclosing.prefix) + 1);
				}
			}

			public override bool HasNext()
			{
				if (this.next == null)
				{
					this.next = this.Peek();
				}
				return this.next != null;
			}

			public override KeyValuePair<string, Ref> Next()
			{
				if (this.HasNext())
				{
					Ent r = this.next;
					this.next = this.Peek();
					return r;
				}
				throw new NoSuchElementException();
			}

			public virtual Ent Peek()
			{
				if (this.packedIdx < this._enclosing.packed.Size() && this.looseIdx < this._enclosing
					.loose.Size())
				{
					Ref p = this._enclosing.packed.Get(this.packedIdx);
					Ref l = this._enclosing.loose.Get(this.looseIdx);
					int cmp = RefComparator.CompareTo(p, l);
					if (cmp < 0)
					{
						this.packedIdx++;
						return this.ToEntry(p);
					}
					if (cmp == 0)
					{
						this.packedIdx++;
					}
					this.looseIdx++;
					return this.ToEntry(this.ResolveLoose(l));
				}
				if (this.looseIdx < this._enclosing.loose.Size())
				{
					return this.ToEntry(this.ResolveLoose(this._enclosing.loose.Get(this.looseIdx++))
						);
				}
				if (this.packedIdx < this._enclosing.packed.Size())
				{
					return this.ToEntry(this._enclosing.packed.Get(this.packedIdx++));
				}
				return null;
			}

			private Ref ResolveLoose(Ref l)
			{
				if (this.resolvedIdx < this._enclosing.resolved.Size())
				{
					Ref r = this._enclosing.resolved.Get(this.resolvedIdx);
					int cmp = RefComparator.CompareTo(l, r);
					if (cmp == 0)
					{
						this.resolvedIdx++;
						return r;
					}
					else
					{
						if (cmp > 0)
						{
							// WTF, we have a symbolic entry but no match
							// in the loose collection. That's an error.
							throw new InvalidOperationException();
						}
					}
				}
				return l;
			}

			private RefMap.Ent ToEntry(Ref p)
			{
				if (p.GetName().StartsWith(this._enclosing.prefix))
				{
					return new RefMap.Ent(_enclosing, p);
				}
				this.packedIdx = this._enclosing.packed.Size();
				this.looseIdx = this._enclosing.loose.Size();
				this.resolvedIdx = this._enclosing.resolved.Size();
				return null;
			}

			public override void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly RefMap _enclosing;
		}

		private class Ent
		{
			private Ref @ref;

			internal Ent(RefMap _enclosing, Ref @ref)
			{
				this._enclosing = _enclosing;
				this.@ref = @ref;
			}

			public virtual string Key
			{
				get
				{
					return this._enclosing.ToMapKey(this.@ref);
				}
			}

			public virtual Ref Value
			{
				get
				{
					return this.@ref;
				}
			}

			public virtual Ref SetValue(Ref value)
			{
				Ref prior = this._enclosing.Put(this.Key, value);
				this.@ref = value;
				return prior;
			}

			public override int GetHashCode()
			{
				return this.Key.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (obj is DictionaryEntry)
				{
					object key = ((DictionaryEntry)obj).Key;
					object val = ((DictionaryEntry)obj).Value;
					if (key is string && val is Ref)
					{
						Ref r = (Ref)val;
						if (r.GetName().Equals(this.@ref.GetName()))
						{
							ObjectId a = r.GetObjectId();
							ObjectId b = this.@ref.GetObjectId();
							if (a != null && b != null && AnyObjectId.Equals(a, b))
							{
								return true;
							}
						}
					}
				}
				return false;
			}

			public static implicit operator KeyValuePair<string, Ref>(RefMap.Ent t)
			{
				return new KeyValuePair<string, Ref>(t.Key, t.Value);
			}
			
			public override string ToString()
			{
				return this.@ref.ToString();
			}

			private readonly RefMap _enclosing;
		}
	}
}
