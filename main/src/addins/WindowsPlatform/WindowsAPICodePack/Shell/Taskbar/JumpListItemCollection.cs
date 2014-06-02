//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Represents a collection of jump list items.
    /// </summary>
    /// <typeparam name="T">The type of elements in this collection.</typeparam>
    internal class JumpListItemCollection<T> : ICollection<T>, INotifyCollectionChanged
    {
        private List<T> items = new List<T>();

        /// <summary>
        /// Occurs anytime a change is made to the underlying collection.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };

        /// <summary>
        /// Gets or sets a value that determines if this collection is read-only.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets a count of the items currently in this collection.
        /// </summary>
        public int Count
        {
            get { return items.Count; }
        }

        /// <summary>
        /// Adds the specified item to this collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            items.Add(item);

            // Trigger CollectionChanged event
            CollectionChanged(
                this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    item));
        }

        /// <summary>
        /// Removes the first instance of the specified item from the collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><b>true</b> if an item was removed, otherwise <b>false</b> if no items were removed.</returns>
        public bool Remove(T item)
        {
            bool removed = items.Remove(item);

            if (removed == true)
            {
                // Trigger CollectionChanged event
                CollectionChanged(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        0));
            }

            return removed;
        }

        /// <summary>
        /// Clears all items from this collection.
        /// </summary>
        public void Clear()
        {
            items.Clear();

            // Trigger CollectionChanged event
            CollectionChanged(
                this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Determines if this collection contains the specified item.
        /// </summary>
        /// <param name="item">The search item.</param>
        /// <returns><b>true</b> if an item was found, otherwise <b>false</b>.</returns>
        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        /// <summary>
        /// Copies this collection to a compatible one-dimensional array,
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The array name.</param>
        /// <param name="index">The index of the starting element.</param>
        public void CopyTo(T[] array, int index)
        {
            items.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An enumerator to iterate through this collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection of a specified type.
        /// </summary>
        /// <returns>An enumerator to iterate through this collection.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }
}
