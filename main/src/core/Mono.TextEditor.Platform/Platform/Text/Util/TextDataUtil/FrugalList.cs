#undef STATS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Text.Utilities
{

#if STATS
    static class Stats
    {
        public static int[] sizes = new int[20];
    }
#endif

    /// <summary>
    /// <para>
    /// This implementation is intended for lists that are usually empty or have a single element.
    /// The element type may be a struct or a class, but lists of structs are by far the most common
    /// in the editor. We store the head of the list in a local field and allocate an array for the tail of the
    /// list only if it the list has length greater than one. Thus singleton and empty lists require only
    /// a single object (not counting elements of the list), as compared to the BCL version of list, 
    /// which allocates two objects for a list with a single member.
    /// </para>
    /// <para>
    /// Do not use this implementation for lists that you know will have length greater than two; the
    /// platform list implementation will be more space efficient.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the list element.</typeparam>
    public class FrugalList<T> : IList<T>
    {
        const int InitialTailSize = 2;                  // initial size of array list

        static List<T> UnitaryTail = new List<T>(0);    // marker that the FrugalList has length one.

        T head;          // first element of list
        List<T> tail;    // the balance

#if STATS
        ~FrugalList()
        {
            Stats.sizes[Math.Min(19, this.count)]++;
        }
#endif
        #region Construction
        // there is no constructor that takes a capacity because we are using 
        // the tail field to tell us something about the current count of elements
        // in the list. If you are tempted to provide a capacity, and it's less than two,
        // go ahead and use this list without a capacity. If it's greater than two, you should
        // be using a regular list, which will be more frugal.

        public FrugalList()
        {
        }

        public FrugalList(IList<T> elements)
        {
            if (elements == null)
            {
                throw new ArgumentNullException("elements");
            }
            switch (elements.Count)
            {
                case 0:
                    break;
                case 1:
                    this.head = elements[0];
                    this.tail = UnitaryTail;
                    break;
                default:
                    this.head = elements[0];
                    this.tail = new List<T>(InitialTailSize);
                    for (int i = 1; i < elements.Count; ++i)
                    {
                        this.tail.Add(elements[i]);
                    }
                    break;
            }
        }
        #endregion

        public int Count
        {
            get
            {
                if (this.tail == null)
                {
                    return 0;
                }
                else
                {
                    return 1 + this.tail.Count;
                }
            }
        }

        public void AddRange(IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            for (int i = 0; i < list.Count; ++i)
            {
                Add(list[i]);
            }
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            int removed = 0;
            for (int i = Count - 1; i >= 0; --i)
            {
                if (match(this[i]))
                {
                    removed++;
                    RemoveAt(i);
                }
            }
            return removed;
        }

        public void Add(T item)
        {
            if (Count == 0)
            {
                this.head = item;
                this.tail = UnitaryTail;
            }
            else
            {
                Debug.Assert(this.tail != null);
                if (this.tail == UnitaryTail)
                {
                    this.tail = new List<T>(InitialTailSize);
                }
                this.tail.Add(item);
            }
        }

        public int IndexOf(T item)
        {
            if (Count > 0)
            {
                if (EqualityComparer<T>.Default.Equals(this.head, item))
                {
                    return 0;
                }
                else
                {
                    int indexOf = this.tail.IndexOf(item);
                    return indexOf >= 0 ? indexOf + 1 : -1;
                }
            }
            else
            {
                return -1;
            }
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (index == 0)
            {
                switch (Count)
                {
                    case 0:
                        this.tail = UnitaryTail;
                        break;
                    case 1:
                        this.tail = new List<T>(InitialTailSize);
                        this.tail.Add(this.head);
                        break;
                    default:
                        this.tail.Insert(0, this.head);
                        break;
                }
                this.head = item;
            }
            else
            {
                Debug.Assert(Count > 0);
                if (this.tail == UnitaryTail)
                {
                    this.tail = new List<T>(InitialTailSize);
                }
                this.tail.Insert(index - 1, item);
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            int count = Count;
            if (index == 0)
            {
                if (count == 1)
                {
                    this.head = default(T);
                    this.tail = null;
                }
                else
                {
                    this.head = this.tail[0];
                    if (count == 2)
                    {
                        this.tail = UnitaryTail;
                    }
                    else
                    {
                        this.tail.RemoveAt(0);
                    }
                }
            }
            else if (count == 2)
            {
                Debug.Assert(index == 1);
                this.tail = UnitaryTail;
            }
            else
            {
                this.tail.RemoveAt(index - 1);
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (index == 0)
                {
                    return this.head;
                }
                else
                {
                    return this.tail[index - 1];
                }
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (index == 0)
                {
                    this.head = value;
                }
                else
                {
                    this.tail[index - 1] = value;
                }
            }
        }

        public void Clear()
        {
            this.head = default(T);
            this.tail = null;
        }

        public bool Contains(T item)
        {
            int count = Count;
            if (count > 0)
            {
                if (EqualityComparer<T>.Default.Equals(this.head, item))
                {
                    return true;
                }
                if (count > 1)
                {
                    return this.tail.Contains(item);
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            int count = Count;
            if (count > 0)
            {
                // let array indexing do the index checks
                array[arrayIndex++] = this.head;
                if (count > 1)
                {
                    this.tail.CopyTo(array, arrayIndex);
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            if (Count > 0)
            {
                if (EqualityComparer<T>.Default.Equals(this.head, item))
                {
                    RemoveAt(0);
                    return true;
                }
                else
                {
                    return this.tail.Remove(item);
                }
            }
            return false;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new FrugalEnumerator(this);
        }

        public FrugalEnumerator GetEnumerator()
        {
            return new FrugalEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new FrugalEnumerator(this);
        }

        public struct FrugalEnumerator : IEnumerator, IEnumerator<T>
        {
            FrugalList<T> list;
            int position;

            public FrugalEnumerator(FrugalList<T> list)
            {
                this.list = list;
                this.position = -1;
            }

            public T Current
            {
                get
                {
                    // if position is -1, then the behavior of Current is unspecified.
                    // for us it will throw an indexing exception.
                    return this.list[this.position];
                }
            }

            object IEnumerator.Current
            {
                get { return this.list[this.position]; }
            }

            public bool MoveNext()
            {
                if (this.position < this.list.Count - 1)
                {
                    this.position++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                this.position = -1;
            }

            public void Dispose()
            {
            }
        }
    }

}
