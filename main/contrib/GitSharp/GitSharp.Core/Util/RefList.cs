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
    /// Specialized variant of an ArrayList to support a {@code RefDatabase}.
    /// <para/>
    /// This list is a hybrid of a Map&lt;String,Ref&gt; and of a List&lt;Ref&gt;. It
    /// tracks reference instances by name by keeping them sorted and performing
    /// binary search to locate an entry. Lookup time is O(log N), but addition and
    /// removal is O(N + log N) due to the list expansion or contraction costs.
    /// <para/>
    /// This list type is copy-on-write. Mutation methods return a new copy of the
    /// list, leaving {@code this} unmodified. As a result we cannot easily implement
    /// the {@link java.util.List} interface contract.
    /// </summary>
    /// <typeparam name="T">the type of reference being stored in the collection.</typeparam>
    public class RefList<T> : IIterable<T> where T : Ref
    {
        private static RefList<T> EMPTY = new RefList<T>(new Ref[0], 0);

        /// <returns>an empty unmodifiable reference list.</returns>
        public static RefList<T> emptyList()
        {
            return EMPTY;
        }

        private readonly Ref[] _list;

        private readonly int _cnt;

        public RefList(Ref[] list, int cnt)
        {
            this._list = list;
            this._cnt = cnt;
        }

        /// <summary>
        /// Initialize this list to use the same backing array as another list.
        /// </summary>
        /// <param name="src">the source list</param>
        public RefList(RefList<T> src)
        {
            this._list = src._list;
            this._cnt = src._cnt;
        }

        public IteratorBase<T> iterator()
        {
            return new BasicIterator<T>(this);
        }

        /// <returns>this cast as an immutable, standard {@link java.util.List}.</returns>
        public ICollection<Ref> asList()
        {
            List<Ref> r = new List<Ref>(_list).GetRange(0, _cnt);
            return new ReadOnlyCollection<Ref>(r);
        }

        /// <returns>number of items in this list.</returns>
        public int size()
        {
            return _cnt;
        }

        /// <returns>true if the size of this list is 0.</returns>
        public bool isEmpty()
        {
            return _cnt == 0;
        }

        /// <summary>
        /// Locate an entry by name.
        /// </summary>
        /// <param name="name">the name of the reference to find.</param>
        /// <returns>
        /// the index the reference is at. If the entry is not present
        /// returns a negative value. The insertion position for the given
        /// name can be computed from {@code -(index + 1)}.
        /// </returns>
        public int find(string name)
        {
            int high = _cnt;
            if (high == 0)
                return -1;
            int low = 0;
            do
            {
                int mid = (int)((uint)(low + high)) >> 1;
                int cmp = RefComparator.compareTo(_list[mid], name);
                if (cmp < 0)
                    low = mid + 1;
                else if (cmp == 0)
                    return mid;
                else
                    high = mid;
            } while (low < high);
            return -(low + 1);
        }

        /// <summary>
        /// Determine if a reference is present.
        /// </summary>
        /// <param name="name">name of the reference to find.</param>
        /// <returns>true if the reference is present; false if it is not.</returns>
        public bool contains(string name)
        {
            return 0 <= find(name);
        }

        /// <summary>
        /// Get a reference object by name.
        /// </summary>
        /// <param name="name">the name of the reference.</param>
        /// <returns>the reference object; null if it does not exist in this list.</returns>
        public T get(string name)
        {
            int idx = find(name);
            return 0 <= idx ? get(idx) : default(T);
        }

        /// <summary>
        /// Get the reference at a particular index.
        /// </summary>
        /// <param name="idx">the index to obtain. Must be {@code 0 &lt;= idx &lt; size()}.</param>
        /// <returns>the reference value, never null.</returns>
        public T get(int idx)
        {
            return (T)_list[idx];
        }

        /// <summary>
        /// Obtain a builder initialized with the first {@code n} elements.
        /// <para/>
        /// Copies the first {@code n} elements from this list into a new builder,
        /// which can be used by the caller to add additional elements.
        /// </summary>
        /// <param name="n">the number of elements to copy.</param>
        /// <returns>a new builder with the first {@code n} elements already added.</returns>
        public Builder<T> copy(int n)
        {
            Builder<T> r = new Builder<T>(Math.Max(16, n));
            r.addAll(_list, 0, n);
            return r;
        }

        /// <summary>
        /// Obtain a new copy of the list after changing one element.
        /// <para/>
        /// This list instance is not affected by the replacement. Because this
        /// method copies the entire list, it runs in O(N) time.
        /// </summary>
        /// <param name="idx">index of the element to change.</param>
        /// <param name="ref">the new value, must not be null.</param>
        /// <returns>copy of this list, after replacing {@code idx} with {@code ref}.</returns>
        public RefList<T> set(int idx, T @ref)
        {
            Ref[] newList = new Ref[_cnt];
            System.Array.Copy(_list, 0, newList, 0, _cnt);
            newList[idx] = @ref;
            return new RefList<T>(newList, _cnt);
        }

        /// <summary>
        /// Add an item at a specific index.
        /// <para/>
        /// This list instance is not affected by the addition. Because this method
        /// copies the entire list, it runs in O(N) time.
        /// </summary>
        /// <param name="idx">
        /// position to add the item at. If negative the method assumes it
        /// was a direct return value from <see cref="find"/> and will
        /// adjust it to the correct position.
        /// </param>
        /// <param name="ref">the new reference to insert.</param>
        /// <returns>copy of this list, after making space for and adding {@code ref}.</returns>
        public RefList<T> add(int idx, T @ref)
        {
            if (idx < 0)
                idx = -(idx + 1);

            Ref[] newList = new Ref[_cnt + 1];
            if (0 < idx)
                System.Array.Copy(_list, 0, newList, 0, idx);
            newList[idx] = @ref;
            if (idx < _cnt)
                System.Array.Copy(_list, idx, newList, idx + 1, _cnt - idx);
            return new RefList<T>(newList, _cnt + 1);
        }

        /// <summary>
        /// Remove an item at a specific index.
        /// <para/>
        /// This list instance is not affected by the addition. Because this method
        /// copies the entire list, it runs in O(N) time.
        /// </summary>
        /// <param name="idx">position to remove the item from.</param>
        /// <returns>copy of this list, after making removing the item at {@code idx}.</returns>
        public RefList<T> remove(int idx)
        {
            if (_cnt == 1)
                return emptyList();
            var newList = new Ref[_cnt - 1];
            if (0 < idx)
                Array.Copy(_list, 0, newList, 0, idx);
            if (idx + 1 < _cnt)
                Array.Copy(_list, idx + 1, newList, idx, _cnt - (idx + 1));
            return new RefList<T>(newList, _cnt - 1);
        }

        /// <summary>
        /// Store a reference, adding or replacing as necessary.
        /// <para/>
        /// This list instance is not affected by the store. The correct position is
        /// determined, and the item is added if missing, or replaced if existing.
        /// Because this method copies the entire list, it runs in O(N + log N) time.
        /// </summary>
        /// <param name="ref">the reference to store.</param>
        /// <returns>copy of this list, after performing the addition or replacement.</returns>
        public RefList<T> put(T @ref)
        {
            int idx = find(@ref.Name);
            if (0 <= idx)
                return set(idx, @ref);
            return add(idx, @ref);
        }


        public IEnumerator<T> GetEnumerator()
        {
            return iterator();
        }

        public override String ToString()
        {
            var r = new StringBuilder();
            r.Append('[');
            if (_cnt > 0)
            {
                r.Append(_list[0]);
                for (int i = 1; i < _cnt; i++)
                {
                    r.Append(", ");
                    r.Append(_list[i]);
                }
            }
            r.Append(']');
            return r.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Builder to facilitate fast construction of an immutable RefList.
        /// </summary>
        /// <typeparam name="TRef">type of reference being stored.</typeparam>
        public class Builder<TRef> where TRef : Ref
        {
            private Ref[] _list;

            private int _size;

            /// <summary>
            /// Create an empty list ready for items to be added.
            /// </summary>
            public Builder() :
                this(16)
            { }

            /// <summary>
            /// Create an empty list with at least the specified capacity.
            /// </summary>
            /// <param name="capacity">the new capacity.</param>
            public Builder(int capacity)
            {
                _list = new Ref[capacity];
            }

            /// <returns>number of items in this builder's internal collection.</returns>
            public int size()
            {
                return _size;
            }

            /// <summary>
            /// Get the reference at a particular index.
            /// </summary>
            /// <param name="idx">the index to obtain. Must be {@code 0 &lt;= idx &lt; size()}.</param>
            /// <returns>the reference value, never null.</returns>
            public TRef get(int idx)
            {
                return (TRef)_list[idx];
            }

            /// <summary>
            /// Remove an item at a specific index.
            /// </summary>
            /// <param name="idx">position to remove the item from.</param>
            public void remove(int idx)
            {
                Array.Copy(_list, idx + 1, _list, idx, _size - (idx + 1));
                _size--;
            }

            /// <summary>
            /// Add the reference to the end of the array.
            /// <para/>
            /// References must be added in sort order, or the array must be sorted
            /// after additions are complete using {@link #sort()}.
            /// </summary>
            /// <param name="ref"></param>
            public void add(TRef @ref)
            {
                if (_list.Length == _size)
                {
                    var n = new Ref[_size * 2];
                    Array.Copy(_list, 0, n, 0, _size);
                    _list = n;
                }
                _list[_size++] = @ref;
            }

            /// <summary>
            /// Add all items from a source array.
            /// <para/>
            /// References must be added in sort order, or the array must be sorted
            /// after additions are complete using <see cref="sort"/>.
            /// </summary>
            /// <param name="src">the source array.</param>
            /// <param name="off">position within {@code src} to start copying from.</param>
            /// <param name="cnt">number of items to copy from {@code src}.</param>
            public void addAll(Ref[] src, int off, int cnt)
            {
                if (_list.Length < _size + cnt)
                {
                    var n = new Ref[Math.Max(_size * 2, _size + cnt)];
                    Array.Copy(_list, 0, n, 0, _size);
                    _list = n;
                }
                Array.Copy(src, off, _list, _size, cnt);
                _size += cnt;
            }

            /// <summary>
            /// Replace a single existing element.
            /// </summary>
            /// <param name="idx">index, must have already been added previously.</param>
            /// <param name="ref">the new reference.</param>
            public void set(int idx, TRef @ref)
            {
                _list[idx] = @ref;
            }

            /// <summary>
            /// Sort the list's backing array in-place.
            /// </summary>
            public void sort()
            {
                Array.Sort(_list, 0, _size, RefComparator.INSTANCE);
            }

            /// <returns>an unmodifiable list using this collection's backing array.</returns>
            public RefList<TRef> toRefList()
            {
                return new RefList<TRef>(_list, _size);
            }

            public override string ToString()
            {
                return toRefList().ToString();
            }
        }
    }
}