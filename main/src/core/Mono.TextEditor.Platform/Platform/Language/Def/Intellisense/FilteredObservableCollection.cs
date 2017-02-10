////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public class FilteredObservableCollection<T> : IList, IList<T>, INotifyCollectionChanged
    {
        private IList<T> _underlyingList;
        private bool _isFiltering = false;
        private Predicate<T> _filterPredicate;
        private List<T> _filteredList = new List<T>();

        public FilteredObservableCollection(IList<T> underlyingList)
        {
            if (underlyingList == null)
                throw new ArgumentNullException("underlyingList");
            if (!(underlyingList is INotifyCollectionChanged))
                throw new ArgumentException("Underlying collection must implement INotifyCollectionChanged", "underlyingList");
            if (!(underlyingList is IList))
                throw new ArgumentException("Underlying collection must implement IList", "underlyingList");

            _underlyingList = underlyingList;
            ((INotifyCollectionChanged)_underlyingList).CollectionChanged += this.OnUnderlyingList_CollectionChanged;
        }

        public int Add(object value)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public bool Contains(object value)
        {
            return ((IList<T>)this).Contains((T)value);
        }

        public int IndexOf(object value)
        {
            return ((IList<T>)this).IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList<T>)this)[index];
            }
            set
            {
                throw new InvalidOperationException("FilteredObservableCollections are read-only");
            }
        }

        public void CopyTo(Array array, int index)
        {
            if (_isFiltering)
            {
                if ((array.Length - index) < this.Count)
                {
                    throw new ArgumentException("Array not big enough", "array");
                }

                int i = index;
                foreach (var item in _filteredList)
                {
                    array.SetValue(item, i);
                    i++;
                }
            }
            else
            {
                ((IList)_underlyingList).CopyTo(array, index);
            }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get
            {
                if (_isFiltering)
                    return ((IList)_filteredList).SyncRoot;
                else
                    return ((IList)_underlyingList).SyncRoot;
            }
        }

        public int IndexOf(T item)
        {
            if (_isFiltering)
                return _filteredList.IndexOf(item);
            else
                return _underlyingList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public T this[int index]
        {
            get
            {
                if (_isFiltering)
                    return _filteredList[index];
                else
                    return _underlyingList[index];
            }
            set
            {
                throw new InvalidOperationException("FilteredObservableCollections are read-only");
            }
        }

        public void Add(T item)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public void Clear()
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public bool Contains(T item)
        {
            if (_isFiltering)
                return _filteredList.Contains(item);
            else
                return _underlyingList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_isFiltering)
                _filteredList.CopyTo(array, arrayIndex);
            else
                _underlyingList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                if (_isFiltering)
                    return _filteredList.Count;
                else
                    return _underlyingList.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_isFiltering)
                return _filteredList.GetEnumerator();
            else
                return _underlyingList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_isFiltering)
                return ((IEnumerable)_filteredList).GetEnumerator();
            else
                return ((IEnumerable)_underlyingList).GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Filter(Predicate<T> filterPredicate)
        {
            if (filterPredicate == null)
                throw new ArgumentNullException("filterPredicate");

            _filterPredicate = filterPredicate;
            _isFiltering = true;
            this.UpdateFilteredItems();
            this.RaiseCollectionChanged();
        }

        public void StopFiltering()
        {
            if (_isFiltering)
            {
                _filterPredicate = null;
                _isFiltering = false;
                this.UpdateFilteredItems();
                this.RaiseCollectionChanged();
            }
        }

        private void OnUnderlyingList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFilteredItems();
            this.RaiseCollectionChanged();
        }

        private void RaiseCollectionChanged()
        {
            NotifyCollectionChangedEventHandler tempHandler = this.CollectionChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void UpdateFilteredItems()
        {
            _filteredList.Clear();
            if (_isFiltering)
            {
                foreach (T underlyingItem in _underlyingList)
                {
                    if (_filterPredicate(underlyingItem))
                    {
                        _filteredList.Add(underlyingItem);
                    }
                }
            }
        }
    }
}
