/*
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
using System.Text;

namespace GitSharp.Core.Util
{
    /// <summary>
    /// Java style iterator with remove capability (which is not supported by IEnumerator).
    /// This iterator is able to iterate over a list without being corrupted by removal of elements
    /// via the remove() method.
    /// </summary>
    public class ListIterator<T>
    {
        protected List<T> list;
        protected int index = -1;
        protected bool can_remove = false;

        public ListIterator(List<T> list)
        {
            this.list = list;
        }

        public virtual bool hasNext()
        {
            if (index >= list.Count - 1)
                return false;
            return true;
        }

        public virtual T next()
        {
            if (index >= list.Count)
                throw new InvalidOperationException();
            can_remove = true;
            return list[index++];
        }

        public virtual void remove()
        {
            if (index >= list.Count || index == -1)
                throw new InvalidOperationException("Index is out of bounds of underlying list! " + index);
            if (!can_remove)
                throw new InvalidOperationException("Can not remove (twice), call next first!");
            can_remove = false; // <--- remove can only be called once per call to next
            list.RemoveAt(index);
            index--;
        }
    }

    public class LinkedListIterator<T> : IIterator<T>
    {
        private readonly LinkedList<T> _list;
        private bool _canRemove;
        private LinkedListNode<T> _current;
        private LinkedListNode<T> _next;

        public LinkedListIterator(LinkedList<T> list)
        {
            _list = list;
            _current = null;
            _next = list.First;
        }

        public virtual bool hasNext()
        {
            if (_next == null)
                return false;

            return true;
        }

        public virtual T next()
        {
            if (!hasNext())
                throw new IndexOutOfRangeException();
            
            _current = _next;

            _next = _current == null ? null : _current.Next;

            _canRemove = true;
            return _current.Value;
        }

        public virtual void remove()
        {
            if (_current == null)
                throw new IndexOutOfRangeException();
            if (!_canRemove)
                throw new InvalidOperationException("Can not remove (twice), call next first!");
            _canRemove = false; // <--- remove can only be called once per call to next
            _list.Remove(_current);
            _current = null;
        }
    }

    public interface IIterable<T> : IEnumerable<T>
    {
        IteratorBase<T> iterator();
        int size();
        T get(int index);
    }

    public class BasicIterable<T> : IIterable<T>
    {
        private readonly IList<T> _entries;

        public BasicIterable(IList<T> entries)
        {
            _entries = entries;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return iterator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IteratorBase<T> iterator()
        {
            return new BasicIterator<T>(this);
        }

        public int size()
        {
            return _entries.Count;
        }

        public T get(int index)
        {
            return _entries[index];
        }
    }

    public class BasicIterator<T> : IteratorBase<T>
    {
        private readonly IIterable<T> _iterable;
        private int _index;

        public BasicIterator(IIterable<T> iterable)
        {
            _iterable = iterable;
        }

        public override bool hasNext()
        {
            return _index < _iterable.size();
        }

        protected override T InnerNext()
        {
            return _iterable.get(_index++);
        }
    }

    public class LambdaConverterIterator<TInput, TOutput> : IteratorBase<TOutput>
    {
        private readonly IteratorBase<TInput> _iterator;
        private readonly Func<TInput, TOutput> _converter;

        public LambdaConverterIterator(IteratorBase<TInput> iterator, Func<TInput, TOutput> converter)
        {
            _iterator = iterator;
            _converter = converter;
        }

        public override bool hasNext()
        {
            return _iterator.hasNext();
        }

        protected override TOutput InnerNext()
        {
            TInput entry = _iterator.next();
            TOutput converted = _converter(entry);
            return converted;
        }
    }

    public interface IIterator<T>
    {
        bool hasNext();
        T next();
        void remove();
    }

    public abstract class IteratorBase<T> : IEnumerator<T>, IIterator<T>
    {
        private T _current;

        public abstract bool hasNext();

        public T next()
        {
            _current = InnerNext();
            return _current;
        }

        public virtual void remove()
        {
            throw new NotSupportedException();
        }

        protected abstract T InnerNext();

        public bool MoveNext()
        {
            if (!hasNext())
            {
                return false;
            }

            next();
            return true;
        }

        public virtual void Reset()
        {
            throw new NotSupportedException();
        }

        public T Current
        {
            get { return _current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public virtual void Dispose()
        {
            // nothing to dispose.
        }
    }
}
