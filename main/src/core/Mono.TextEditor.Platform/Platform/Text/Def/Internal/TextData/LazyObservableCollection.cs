////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.VisualStudio.Text.Utilities
{
    public delegate TWrapper WrapperCreator<TData, TWrapper>(TData underlyingData, int index);

    /// <summary>
    /// A virtualized data collection that can be used to create wrapper objects on-demand.
    /// </summary>
    public class LazyObservableCollection<TData, TWrapper> :
        IList<TWrapper>,
        IList,
        INotifyCollectionChanged,
        INotifyPropertyChanged,
        IDisposable
        where TWrapper : class
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Private Fields

        private bool _disposed = false;
        private TData _underlyingDataObject;
        private int _count;
        private WrapperCreator<TData, TWrapper> _wrapperCreator;
        private Dictionary<int, TWrapper> _wrapperDictionary = new Dictionary<int, TWrapper>();

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Constructors

        /// <summary>
        /// Constructs a virtualized list over an underlying data object.
        /// </summary>
        /// <param name="underlyingDataObject">The underlying object over which wrappers will be created.</param>
        /// <param name="dataObjectCount">
        /// The number of "items" in the underlying object.  Also the number of wrappers to be created.
        /// </param>
        /// <param name="wrapperCreator">
        /// A delegate that will create wrapper objects given an index into the underlying object.
        /// </param>
        public LazyObservableCollection
            (
            TData underlyingDataObject,
            int dataObjectCount,
            WrapperCreator<TData, TWrapper> wrapperCreator
            )
        {
            _underlyingDataObject = underlyingDataObject;
            _count = dataObjectCount;
            _wrapperCreator = wrapperCreator;
            this.SubscribeToEvents();
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IList<TWrapper> Members

        /// <summary>
        /// Determines the index of a specific item in the <see cref="LazyObservableCollection{TData, TWrapper}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="LazyObservableCollection{TData, TWrapper}"/>.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(TWrapper item)
        {
            if (_disposed)
                return -1;

            // We've got a problem here.  The caller is asking us to find the index of a wrapper.  This could be a wrapper around
            // any underlying data object.  We have to assume that the caller is asking about a wrapper that we previously gave
            // to them.
            if (_wrapperDictionary.ContainsValue(item))
            {
                Dictionary<int, TWrapper>.KeyCollection tempKeys = _wrapperDictionary.Keys;

                foreach (int key in tempKeys)
                {
                    if (item == _wrapperDictionary[key])
                    {
                        return key;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public void Insert(int index, TWrapper item)
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        /// <summary>
        /// Gets the element at the specified index.  Although the set accessor is defined, it will throw an
        /// <see cref="InvalidOperationException"/> when called, as the <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public TWrapper this[int index]
        {
            get
            {
                return this.GetWrapper(index);
            }
            set
            {
                throw new InvalidOperationException("The collection is read-only.");
            }
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IList Members

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public int Add(object value)
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        /// <summary>
        /// Determines whether the <see cref="LazyObservableCollection{TData, TWrapper}"/> contains a specific value.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to locate in the <see cref="LazyObservableCollection{TData, TWrapper}"/>.</param>
        /// <returns>
        /// true if the <see cref="System.Object"/> is found in the <see cref="LazyObservableCollection{TData, TWrapper}"/>; otherwise, false.
        /// </returns>
        public bool Contains(object value)
        {
            TWrapper wrapperObj = value as TWrapper;
            if ((value != null) && (wrapperObj == null))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "value is not of type {0}", typeof(TWrapper).FullName), "value");
            }

            return this.Contains(wrapperObj);
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="LazyObservableCollection{TData, TWrapper}"/>.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to locate in the <see cref="LazyObservableCollection{TData, TWrapper}"/>.</param>
        /// <returns>The index of value if found in the list; otherwise, -1.</returns>
        public int IndexOf(object value)
        {
            TWrapper wrapperObj = value as TWrapper;
            if ((value != null) && (wrapperObj == null))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "value is not of type {0}", typeof(TWrapper).FullName), "value");
            }

            return this.IndexOf(wrapperObj);
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public void Insert(int index, object value)
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="InvalidOperationException"/> has a fixed size.
        /// </summary>
        public bool IsFixedSize
        {
            get { return true; }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public void Remove(object value)
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        /// <summary>
        /// Gets the element at the specified index.  Although the set accessor is defined, it will throw an
        /// <see cref="InvalidOperationException"/> when called, as the <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        object IList.this[int index]
        {
            get
            {
                return ((IList<TWrapper>)this)[index];
            }
            set
            {
                throw new InvalidOperationException("The collection is read-only.");
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="LazyObservableCollection{TData, TWrapper}"/> to an <see cref="System.Array"/>, starting at a
        /// particular <see cref="System.Array"/> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="System.Array"/> that is the destination of the elements copied from
        /// the <see cref="LazyObservableCollection{TData, TWrapper}"/>. The <see cref="System.Array"/> must have zero-based indexing.
        /// </param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(Array array, int index)
        {
            if ((array.Length - index) < this.Count)
            {
                throw new ArgumentException("Array not big enough", "array");
            }

            int i = index;
            foreach (TWrapper wrapper in this)
            {
                array.SetValue(wrapper, i);
                i++;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="LazyObservableCollection{TData, TWrapper}"/> is synchronized (thread safe).
        /// </summary>
        public bool IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="LazyObservableCollection{TData, TWrapper}"/>.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                if (_disposed)
                    return null;

                return ((ICollection)_wrapperDictionary).SyncRoot;
            }
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region ICollection<TWrapper> Members

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public void Add(TWrapper item)
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public void Clear()
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        /// <summary>
        /// Determines whether the <see cref="LazyObservableCollection{TData, TWrapper}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The value to locate in the <see cref="LazyObservableCollection{TData, TWrapper}"/>.</param>
        /// <returns>
        /// true if the value is found in the <see cref="LazyObservableCollection{TData, TWrapper}"/>; otherwise, false.
        /// </returns>
        public bool Contains(TWrapper item)
        {
            if (_disposed)
                return false;

            return _wrapperDictionary.ContainsValue(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="LazyObservableCollection{TData, TWrapper}"/> to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements copied from the
        /// <see cref="LazyObservableCollection{TData, TWrapper}"/>. The array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(TWrapper[] array, int arrayIndex)
        {
            if (_disposed)
                return;

            // Before we do the copy, we have to ensure that the entire underlying collection is wrapped.  That way, the caller
            // will get a full copy of the list, not just the items we've realized so-far.
            this.EnsureEntirelyWrapped();
            _wrapperDictionary.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="LazyObservableCollection{TData, TWrapper}"/>.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when called.  The <see cref="LazyObservableCollection{TData, TWrapper}"/> is read-only.
        /// </summary>
        public bool Remove(TWrapper item)
        {
            throw new InvalidOperationException("The collection is read-only.");
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IEnumerable<TWrapper> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Collections.Generic.IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TWrapper> GetEnumerator()
        {
            return new LazyObservableCollectionEnumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Collections.IEnumerator"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new LazyObservableCollectionEnumerator(this);
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region INotifyCollectionChanged Members

        /// <summary>
        /// Raised when the set of items in the <see cref="LazyObservableCollection{TData, TWrapper}"/> changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IDisposable Members

        /// <summary>
        /// Disposes and releases all wrappers created.  Also releases all references to the underlying object
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Protected Surface

        protected virtual void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                this.UnsubscribeFromEvents();

                // Call dispose on the wrapper items and clean up the list
                DisposeItems(_wrapperDictionary);

                _wrapperDictionary = null;

                // Stop holding-on to the wrapper creator delegate
                _wrapperCreator = null;
            }

            // Stop holding-on to the underlying collection (might be a native COM object, so do this regardless)
            _underlyingDataObject = default(TData);
            _count = 0;
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Public Surface

        /// <summary>
        /// Gets a wrapper object over the underlying object for the specified index.
        /// </summary>
        /// <param name="index">The index for which to obtain a wrapper</param>
        /// <returns>A valid wrapper object for the specified index.</returns>
        public TWrapper GetWrapper(int index)
        {
            if (_disposed)
                throw new ObjectDisposedException("LazyObservableCollection");

            TWrapper wrapper;
            if (_wrapperDictionary.TryGetValue(index, out wrapper))
            {
                return wrapper;
            }

            // We don't yet have a wrapper for this data item.  Go ahead and create one.
            wrapper = _wrapperCreator(_underlyingDataObject, index);
            _wrapperDictionary[index] = wrapper;
            return wrapper;
        }

        /// <summary>
        /// Sets the underlying data object over which this <see cref="LazyObservableCollection{TData, TWrapper}"/> generates
        /// wrapper objects.
        /// </summary>
        /// <param name="newDataObject">The underlying data object over which to generate wrappers.</param>
        /// <param name="count">The number of items in the underlying object</param>
        public void SetUnderlyingDataObject(TData newDataObject, int count)
        {
            // If we're subscribed to INotifyCollectionChanged on the old underlying data object, stop listening.
            this.UnsubscribeFromEvents();

            // Reset the underlying object and listen to the new INotifyCollectionChanged, if it exists.
            _underlyingDataObject = newDataObject;
            _count = count;
            this.SubscribeToEvents();

            // Get rid of all of our old wrappers
            this.NotifyUnderlyingObjectChanged();
        }

        /// <summary>
        /// Notifies the <see cref="LazyObservableCollection{TData, TWrapper}"/> that the underlying object over which the
        /// <see cref="LazyObservableCollection{TData, TWrapper}"/> is based has changed.
        /// </summary>
        /// <remarks>
        /// When the underlying object changes, the <see cref="LazyObservableCollection{TData, TWrapper}"/> resets its wrapper
        /// collection and raises its INotifyCollectionChanged.CollectionChanged event.  The next time wrappers are requested from
        /// the <see cref="LazyObservableCollection{TData, TWrapper}"/>, the wrappers will be re-generated.
        /// </remarks>
        public void NotifyUnderlyingObjectChanged()
        {
            if (_disposed)
                return;

            // Since the underlying collection changed, we can no longer be sure that our wrappers correspond to the right
            // underlying data objects.  Therefore, we'll reset our collection entirely.

            // 1. Keep track of old collection of items
            Dictionary<int, TWrapper> oldItems = _wrapperDictionary;

            // 2. Recreate the collection of items and update count
            IList underlyingList = _underlyingDataObject as IList;
            if (underlyingList != null)
            {
                _count = underlyingList.Count;
            }
            // we want the dictionary to start small, as its future size is little related
            // to its prior size (or the size of the underlying list).
            _wrapperDictionary = new Dictionary<int, TWrapper>();

            // 3. Notify listeners of the change
            this.RaiseCollectionChanged();

            // 4. Call dispose on the old wrapper items
            DisposeItems(oldItems);
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        private void OnUnderlyingCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.NotifyUnderlyingObjectChanged();
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Private Implementation

        private static void DisposeItems(Dictionary<int, TWrapper> collection)
        {
            foreach (TWrapper wrapper in collection.Values)
            {
                IDisposable disposableWrapper = wrapper as IDisposable;
                if (disposableWrapper != null)
                {
                    disposableWrapper.Dispose();
                }
            }
        }

        private void EnsureEntirelyWrapped()
        {
            for (int i = 0; i < this.Count; i++)
            {
                this.GetWrapper(i);
            }
        }

        private void RaiseCollectionChanged()
        {
            NotifyCollectionChangedEventHandler tempHandler = this.CollectionChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            // Also notify consumers of the change to the 'Count' property.
            this.RaisePropertyChanged("Count");
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler tempHandler = this.PropertyChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void SubscribeToEvents()
        {
            INotifyCollectionChanged changingUnderlyingColleciton = _underlyingDataObject as INotifyCollectionChanged;
            if (changingUnderlyingColleciton != null)
            {
                changingUnderlyingColleciton.CollectionChanged += this.OnUnderlyingCollection_CollectionChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            INotifyCollectionChanged changingUnderlyingColleciton = _underlyingDataObject as INotifyCollectionChanged;
            if (changingUnderlyingColleciton != null)
            {
                changingUnderlyingColleciton.CollectionChanged -= this.OnUnderlyingCollection_CollectionChanged;
            }
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Nested Classes

        private class LazyObservableCollectionEnumerator : IEnumerator<TWrapper>
        {
            private int _currentIndex = -1;
            private LazyObservableCollection<TData, TWrapper> _collection;

            /// <summary>
            /// Creates an instance of the <see cref="LazyObservableCollectionEnumerator"/> class.
            /// </summary>
            /// <param name="collection"></param>
            public LazyObservableCollectionEnumerator(LazyObservableCollection<TData, TWrapper> collection)
            {
                _collection = collection;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            public TWrapper Current
            {
                get
                {
                    if (_currentIndex < 0)
                    {
                        throw new InvalidOperationException("Enumerator position before first element in the collection");
                    }
                    return _collection[_currentIndex];
                }
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    if (_currentIndex < 0)
                    {
                        throw new InvalidOperationException("Enumerator position before first element in the collection");
                    }
                    return _collection[_currentIndex];
                }
            }

            /// <summary>
            /// Releases all references to the collection being iterated.
            /// </summary>
            public void Dispose()
            {
                _collection = null;
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of
            /// the collection.
            /// </returns>
            public bool MoveNext()
            {
                if (_currentIndex < _collection.Count - 1)
                {
                    _currentIndex++;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                _currentIndex = -1;
            }
        }

        #endregion
    }
}
