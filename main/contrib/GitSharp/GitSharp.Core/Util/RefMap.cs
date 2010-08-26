/*
 * Copyright (C) 2010, Google Inc.
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using System.Collections.ObjectModel;
using System.Text;

namespace GitSharp.Core.Util
{
    /// <summary>
    /// Specialized Map to present a {@code RefDatabase} namespace.
    /// <para/>
    /// Although not declared as a {@link java.util.SortedMap}, iterators from this
    /// map's projections always return references in {@link RefComparator} ordering.
    /// The map's internal representation is a sorted array of {@link Ref} objects,
    /// which means lookup and replacement is O(log N), while insertion and removal
    /// can be as expensive as O(N + log N) while the list expands or contracts.
    /// Since this is not a general map implementation, all entries must be keyed by
    /// the reference name.
    /// <para/>
    /// This class is really intended as a helper for {@code RefDatabase}, which
    /// needs to perform a merge-join of three sorted {@link RefList}s in order to
    /// present the unified namespace of the packed-refs file, the loose refs/
    /// directory tree, and the resolved form of any symbolic references.
    /// </summary>
    public class RefMap : IDictionary<string, Ref>
    {
        /// <summary>
        /// Prefix denoting the reference subspace this map contains.
        /// <para/>
        /// All reference names in this map must start with this prefix. If the
        /// prefix is not the empty string, it must end with a '/'.
        /// </summary>
        private readonly string _prefix;

        /// <summary>
        /// Immutable collection of the packed references at construction time.
        /// </summary>
        private RefList<Ref> _packed;

        /// <summary>
        /// Immutable collection of the loose references at construction time.
        /// <para/>
        /// If an entry appears here and in {@link #packed}, this entry must take
        /// precedence, as its more current. Symbolic references in this collection
        /// are typically unresolved, so they only tell us who their target is, but
        /// not the current value of the target.
        /// </summary>
        private RefList<Ref> _loose;

        /// <summary>
        /// Immutable collection of resolved symbolic references.
        /// <para/>
        /// This collection contains only the symbolic references we were able to
        /// resolve at map construction time. Other loose references must be read
        /// from {@link #loose}. Every entry in this list must be matched by an entry
        /// in {@code loose}, otherwise it might be omitted by the map.
        /// </summary>
        private RefList<Ref> _resolved;

        private int _size;

        private bool _sizeIsValid;

        private EntrySet _entrySet;

        /// <summary>
        /// Construct an empty map with a small initial capacity.
        /// </summary>
        public RefMap()
        {
            _prefix = "";
            _packed = RefList<Ref>.emptyList();
            _loose = RefList<Ref>.emptyList();
            _resolved = RefList<Ref>.emptyList();
        }

        /// <summary>
        /// Construct a map to merge 3 collections together.
        /// </summary>
        /// <param name="prefix">
        /// prefix used to slice the lists down. Only references whose
        /// names start with this prefix will appear to reside in the map.
        /// Must not be null, use {@code ""} (the empty string) to select
        /// all list items.
        /// </param>
        /// <param name="packed">
        /// items from the packed reference list, this is the last list
        /// searched.
        /// </param>
        /// <param name="loose">
        /// items from the loose reference list, this list overrides
        /// {@code packed} if a name appears in both.
        /// </param>
        /// <param name="resolved">
        /// resolved symbolic references. This list overrides the prior
        /// list {@code loose}, if an item appears in both. Items in this
        /// list <b>must</b> also appear in {@code loose}.
        /// </param>
        public RefMap(string prefix, RefList<Ref> packed, RefList<Ref> loose,
                      RefList<Ref> resolved)
        {
            this._prefix = prefix;
            this._packed = packed;
            this._loose = loose;
            this._resolved = resolved;
        }

        public bool containsKey(string name)
        {
            return get(name) != null;
        }

        public Ref get(string key)
        {
            string name = toRefName(key);
            Ref @ref = _resolved.get(name);
            if (@ref == null)
                @ref = _loose.get(name);
            if (@ref == null)
                @ref = _packed.get(name);
            return @ref;
        }

        public Ref put(string keyName, Ref value)
        {
            string name = toRefName(keyName);

            if (!name.Equals(value.Name))
                throw new ArgumentException("keyName");

            if (!_resolved.isEmpty())
            {
                // Collapse the resolved list into the loose list so we
                // can discard it and stop joining the two together.
                foreach (Ref @ref in _resolved)
                    _loose = _loose.put(@ref);
                _resolved = RefList<Ref>.emptyList();
            }

            int idx = _loose.find(name);
            if (0 <= idx)
            {
                Ref prior = _loose.get(name);
                _loose = _loose.set(idx, value);
                return prior;
            }
            else
            {
                Ref prior = get(keyName);
                _loose = _loose.add(idx, value);
                _sizeIsValid = false;
                return prior;
            }
        }

        public Ref remove(string key)
        {
            string name = toRefName(key);
            Ref res = null;
            int idx;
            if (0 <= (idx = _packed.find(name)))
            {
                res = _packed.get(name);
                _packed = _packed.remove(idx);
                _sizeIsValid = false;
            }
            if (0 <= (idx = _loose.find(name)))
            {
                res = _loose.get(name);
                _loose = _loose.remove(idx);
                _sizeIsValid = false;
            }
            if (0 <= (idx = _resolved.find(name)))
            {
                res = _resolved.get(name);
                _resolved = _resolved.remove(idx);
                _sizeIsValid = false;
            }
            return res;
        }

        public bool isEmpty()
        {
            return entrySet().isEmpty();
        }

        public EntrySet entrySet()
        {
            if (_entrySet == null)
            {
                _entrySet = new EntrySet(this);
            }

            return _entrySet;
        }

        public class RefSet : IIterable<Ref>
        {
            private readonly EntrySet _entrySet;

            public RefSet(EntrySet entrySet)
            {
                _entrySet = entrySet;
            }

            public IEnumerator<Ref> GetEnumerator()
            {
                return iterator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IteratorBase<Ref> iterator()
            {
                return new LambdaConverterIterator<Ent, Ref>(_entrySet.iterator(), ent => ent.getValue());
            }

            public int size()
            {
                return _entrySet.size();
            }

            public Ref get(int index)
            {
                Ent ent = _entrySet.get(index);
                return ent.getValue();
            }
        }

        public class EntrySet : IIterable<Ent>
        {
            private readonly RefMap _refMap;

            public EntrySet(RefMap refMap)
            {
                _refMap = refMap;
            }

            public IEnumerator<Ent> GetEnumerator()
            {
                return iterator();
            }

            public IteratorBase<Ent> iterator()
            {
                return new SetIterator(_refMap);
            }

            public int size()
            {
                if (!_refMap._sizeIsValid)
                {
                    _refMap._size = 0;
                    IteratorBase<Ent> i = _refMap.entrySet().iterator();
                    for (; i.hasNext(); i.next())
                        _refMap._size++;
                    _refMap._sizeIsValid = true;
                }
                return _refMap._size;
            }

            public bool isEmpty()
            {
                if (_refMap._sizeIsValid)
                    return 0 == _refMap._size;
                return !iterator().hasNext();
            }

            public void clear()
            {
                _refMap._packed = RefList<Ref>.emptyList();
                _refMap._loose = RefList<Ref>.emptyList();
                _refMap._resolved = RefList<Ref>.emptyList();
                _refMap._size = 0;
                _refMap._sizeIsValid = true;
            }

            public Ent get(int index)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerator<KeyValuePair<string, Ref>> GetEnumerator()
        {
            return new LambdaConverterIterator<Ent, KeyValuePair<string, Ref>>(entrySet().iterator(), (ent) => new KeyValuePair<string, Ref>(ent.getKey(), ent.getValue()));
        }

        public override string ToString()
        {
            var r = new StringBuilder();
            bool first = true;
            r.Append('[');
            foreach (Ref @ref in values())
            {
                if (first)
                    first = false;
                else
                    r.Append(", ");
                r.Append(@ref);
            }
            r.Append(']');
            return r.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IIterable<Ref> values()
        {
            return new RefSet(entrySet());
        }

        private string toRefName(string name)
        {
            if (0 < _prefix.Length)
                name = _prefix + name;
            return name;
        }

        private string toMapKey(Ref @ref)
        {
            string name = @ref.Name;
            if (0 < _prefix.Length)
                name = name.Substring(_prefix.Length);
            return name;
        }

        private class SetIterator : IteratorBase<Ent>
        {
            private readonly RefMap _refMap;
            private int packedIdx;

            private int looseIdx;

            private int resolvedIdx;

            private Ent _next;

            public SetIterator(RefMap refMap)
            {
                _refMap = refMap;
                if (0 < _refMap._prefix.Length)
                {
                    packedIdx = -(_refMap._packed.find(_refMap._prefix) + 1);
                    looseIdx = -(_refMap._loose.find(_refMap._prefix) + 1);
                    resolvedIdx = -(_refMap._resolved.find(_refMap._prefix) + 1);
                }
            }

            public override bool hasNext()
            {
                if (_next == null)
                    _next = peek();
                return _next != null;
            }

            protected override Ent InnerNext()
            {
                if (hasNext())
                {
                    Ent r = _next;
                    _next = peek();
                    return r;
                }
                throw new IndexOutOfRangeException();
            }

            private Ent peek()
            {
                if (packedIdx < _refMap._packed.size() && looseIdx < _refMap._loose.size())
                {
                    Ref p = _refMap._packed.get(packedIdx);
                    Ref l = _refMap._loose.get(looseIdx);
                    int cmp = RefComparator.compareTo(p, l);
                    if (cmp < 0)
                    {
                        packedIdx++;
                        return toEntry(p);
                    }

                    if (cmp == 0)
                        packedIdx++;
                    looseIdx++;
                    return toEntry(resolveLoose(l));
                }

                if (looseIdx < _refMap._loose.size())
                    return toEntry(resolveLoose(_refMap._loose.get(looseIdx++)));
                if (packedIdx < _refMap._packed.size())
                    return toEntry(_refMap._packed.get(packedIdx++));
                return null;
            }

            private Ref resolveLoose(Ref l)
            {
                if (resolvedIdx < _refMap._resolved.size())
                {
                    Ref r = _refMap._resolved.get(resolvedIdx);
                    int cmp = RefComparator.compareTo(l, r);
                    if (cmp == 0)
                    {
                        resolvedIdx++;
                        return r;
                    }
                    else if (cmp > 0)
                    {
                        // WTF, we have a symbolic entry but no match
                        // in the loose collection. That's an error.
                        throw new InvalidOperationException();
                    }
                }
                return l;
            }

            private Ent toEntry(Ref p)
            {
                if (p.Name.StartsWith(_refMap._prefix))
                    return new Ent(_refMap, p);
                packedIdx = _refMap._packed.size();
                looseIdx = _refMap._loose.size();
                resolvedIdx = _refMap._resolved.size();
                return null;
            }
        }

        public class Ent// implements Entry<string, Ref> 
        {
            private readonly RefMap _refMap;
            private Ref @ref;

            public Ent(RefMap refMap, Ref @ref)
            {
                _refMap = refMap;
                this.@ref = @ref;
            }

            public string getKey()
            {
                return _refMap.toMapKey(@ref);
            }

            public Ref getValue()
            {
                return @ref;
            }

            public Ref setValue(Ref value)
            {
                Ref prior = _refMap.put(getKey(), value);
                @ref = value;
                return prior;
            }

            public override int GetHashCode()
            {
                return getKey().GetHashCode();
            }

            public override bool Equals(Object obj)
            {
                if (obj is Ent)
                {
                    Object key = ((Ent)obj).getKey();
                    Object val = ((Ent)obj).getValue();
                    if (key is string && val is Ref)
                    {
                        Ref r = (Ref)val;
                        if (r.Name.Equals(@ref.Name))
                        {
                            ObjectId a = r.ObjectId;
                            ObjectId b = @ref.ObjectId;
                            if (a != null && b != null && AnyObjectId.equals(a, b))
                                return true;
                        }
                    }
                }

                return false;
            }

            public override string ToString()
            {
                return @ref.ToString();
            }
        }

        public int size()
        {
            return entrySet().size();
        }

        public void clear()
        {
            entrySet().clear();
        }

        public IIterable<string> keySet()
        {
            var keys = new List<string>();

            foreach (Ent ent in entrySet())
            {
                keys.Add(ent.getKey());
            }

            return new BasicIterable<string>(keys);
        }

        public void Add(KeyValuePair<string, Ref> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, Ref> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, Ref>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, Ref> item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return size(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool ContainsKey(string key)
        {
            return containsKey(key);
        }

        public void Add(string key, Ref value)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
        	Ref val;

			if(TryGetValue(key, out val))
			{
				remove(key);
				return true;
			}

        	return false;
        }

        public bool TryGetValue(string key, out Ref value)
        {
            value = get(key);
            return (value != null);
        }

        public Ref this[string key]
        {
            get { return get(key); }
            set { throw new NotImplementedException(); }
        }

        public ICollection<string> Keys
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<Ref> Values
        {
            get { return new List<Ref>(values()).AsReadOnly(); }
        }

    }


}