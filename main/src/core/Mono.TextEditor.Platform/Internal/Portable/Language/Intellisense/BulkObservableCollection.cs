////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
#if TARGET_VS
using System.Windows.Threading;
#else
using System;
using System.Diagnostics;
using System.Threading;
#endif

namespace Microsoft.VisualStudio.Language.Intellisense
{
	/// <summary>
	/// Represents an ObservableCollection that allows the AddRange operation.
	/// </summary>
	public class BulkObservableCollection<T> : ObservableCollection<T>
	{
		private delegate void AddRangeCallback(IList<T> items);
		private delegate void SetItemCallback(int index, T item);
		private delegate void RemoveItemCallback(int index);
		private delegate void ClearItemsCallback();
		private delegate void InsertItemCallback(int index, T item);
		private delegate void MoveItemCallback(int oldIndex, int newIndex);

		private int _rangeOperationCount = 0;
		private bool _collectionChangedDuringRangeOperation = false;
#if TARGET_VS
        private Dispatcher _dispatcher;
#else
		// MONO: TODO: For now, only allow creation/modification on same thread
		private Thread _creationThread;

#endif
		private ReadOnlyObservableCollection<T> _readOnlyAccessor;

		private static readonly PropertyChangedEventArgs _countChanged = new PropertyChangedEventArgs("Count");
		private static readonly PropertyChangedEventArgs _indexerChanged = new PropertyChangedEventArgs("Item[]");
		private static readonly NotifyCollectionChangedEventArgs _resetChange = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

		/// <summary>
		/// Initializes a new instance of a <see cref="BulkObservableCollection&lt;T&gt;"/>.
		/// </summary>
		public BulkObservableCollection()
		{
#if TARGET_VS
            _dispatcher = Dispatcher.CurrentDispatcher;
#else
			_creationThread = Thread.CurrentThread;
#endif
		}

		/// <summary>
		/// Adds a list of items to the ObservableCollection without firing an event for each item.
		/// </summary>
		/// <param name="items">A list of items to add.</param>
		public void AddRange(IEnumerable<T> items)
		{
			// If we were given null, nothing to do.
			if ((items == null) || !items.Any())
			{
				return;
			}

#if TARGET_VS
            if (_dispatcher.CheckAccess())
#else
			if (Thread.CurrentThread == _creationThread)
#endif
			{
				try
				{
					this.BeginBulkOperation();
					_collectionChangedDuringRangeOperation = true;

					foreach (T item in items)
					{
						// Call down to the underlying collection to ensure that no collection changed event is generated (we've marked ourselves dirty above so a reset event will be raised at the end of the bulk operation).
						base.Items.Add(item);
					}
				}
				finally
				{
					this.EndBulkOperation();
				}
			}
			else
			{
#if TARGET_VS
                _dispatcher.BeginInvoke(DispatcherPriority.Send, new AddRangeCallback(this.AddRange), items);    
#else
				Debug.Fail("Bulk Observable Collection isn't able to handle cross thread changes, yet");
				throw new Exception();
#endif
			}
		}

		/// <summary>
		/// Suspends change events on the collection in order to perform a bulk change operation.
		/// </summary>
		public void BeginBulkOperation()
		{
			_rangeOperationCount++;
			_collectionChangedDuringRangeOperation = false;
		}

		/// <summary>
		/// Restores change events on the collection after a bulk change operation has been completed.
		/// </summary>
		public void EndBulkOperation()
		{
			if ((_rangeOperationCount > 0) && (--_rangeOperationCount == 0) && _collectionChangedDuringRangeOperation)
			{
				// Assume that, as a result of a change, the count & indexer changes (which are the only two properties that could change).
				this.OnPropertyChanged(_countChanged);
				this.OnPropertyChanged(_indexerChanged);

				this.OnCollectionChanged(_resetChange);
			}
		}

		/// <summary>
		/// Gets a read-only version of the collection.
		/// </summary>
		/// <returns>A read-only version of the collection.</returns>
		public ReadOnlyObservableCollection<T> AsReadOnly()
		{
			if (_readOnlyAccessor == null)
			{
				_readOnlyAccessor = new ReadOnlyObservableCollection<T>(this);
			}

			return _readOnlyAccessor;
		}

		/// <summary>
		/// Occurs when a property on the collection has changed.
		/// </summary>
		/// <param name="e">The event arguments.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (_rangeOperationCount == 0)
			{
				base.OnPropertyChanged(e);
			}
			else
			{
				_collectionChangedDuringRangeOperation = true;
			}
		}

		/// <summary>
		/// Occurs when the collection has changed.
		/// </summary>
		/// <param name="e">The event arguments.</param>
		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (_rangeOperationCount == 0)
			{
				base.OnCollectionChanged(e);
			}
			else
			{
				_collectionChangedDuringRangeOperation = true;
			}
		}

		/// <summary>
		/// Replaces the item at the specified index.
		/// </summary>
		/// <param name="index">The place at which to replace the item.</param>
		/// <param name="item">The item to replace.</param>
		protected override void SetItem(int index, T item)
		{
#if TARGET_VS
            if (_dispatcher.CheckAccess())
#else
			if (Thread.CurrentThread == _creationThread)
#endif
			{
				base.SetItem(index, item);
			}
			else
			{
#if TARGET_VS
                _dispatcher.BeginInvoke(DispatcherPriority.Send, new SetItemCallback(this.SetItem), index, new object[] { item });
#else
				Debug.Fail("Bulk Observable Collection isn't able to handle cross thread changes, yet");
				throw new Exception();
#endif
			}
		}

		/// <summary>
		/// Inserts an item at the specified index.
		/// </summary>
		/// <param name="index">The location at which to insert the item.</param>
		/// <param name="item">The item to insert.</param>
		protected override void InsertItem(int index, T item)
		{
#if TARGET_VS
            if (_dispatcher.CheckAccess())
#else
			if (Thread.CurrentThread == _creationThread)
#endif
			{
				// Dev12 #619282. During range operation avoid calling base.InsertItem() because it would allocate
				// NotifyCollectionChangedEventArgs object for each item, but this.OnCollectionChanged would not fire 
				// the event during range operation.
				if (_rangeOperationCount == 0)
				{
					base.InsertItem(index, item);
				}
				else
				{
					base.Items.Insert(index, item);
					_collectionChangedDuringRangeOperation = true;
				}
			}
			else
			{
#if TARGET_VS
                _dispatcher.BeginInvoke(DispatcherPriority.Send, new InsertItemCallback(this.InsertItem), index, new object[] { item });
#else
				Debug.Fail("Bulk Observable Collection isn't able to handle cross thread changes, yet");
				throw new Exception();
#endif
			}
		}

		/// <summary>
		/// Moves the item from one location to another.
		/// </summary>
		/// <param name="oldIndex">The original location.</param>
		/// <param name="newIndex">The new location.</param>
		protected override void MoveItem(int oldIndex, int newIndex)
		{
#if TARGET_VS
            if (_dispatcher.CheckAccess())
#else
			if (Thread.CurrentThread == _creationThread)
#endif
			{
				base.MoveItem(oldIndex, newIndex);
			}
			else
			{
#if TARGET_VS
                _dispatcher.BeginInvoke(DispatcherPriority.Send, new MoveItemCallback(this.MoveItem), oldIndex, new object[] { newIndex });
#else
				Debug.Fail("Bulk Observable Collection isn't able to handle cross thread changes, yet");
				throw new Exception();
#endif
			}
		}

		/// <summary>
		/// Removes an item from the collection at the specified location.
		/// </summary>
		/// <param name="index">The location at which to remove the item.</param>
		protected override void RemoveItem(int index)
		{
#if TARGET_VS
            if (_dispatcher.CheckAccess())
#else
			if (Thread.CurrentThread == _creationThread)
#endif
			{
				base.RemoveItem(index);
			}
			else
			{
#if TARGET_VS
                _dispatcher.BeginInvoke(DispatcherPriority.Send, new RemoveItemCallback(this.RemoveItem), index);
#else
				Debug.Fail("Bulk Observable Collection isn't able to handle cross thread changes, yet");
				throw new Exception();
#endif
			}
		}

		/// <summary>
		/// Removes all items from the collection.
		/// </summary>
		protected override void ClearItems()
		{
#if TARGET_VS
            if (_dispatcher.CheckAccess())
#else
			if (Thread.CurrentThread == _creationThread)
#endif
			{
				if (this.Count > 0)
				{
					base.ClearItems();
				}
			}
			else
			{
#if TARGET_VS
                _dispatcher.BeginInvoke(DispatcherPriority.Send, new ClearItemsCallback(this.ClearItems));
#else
				Debug.Fail("Bulk Observable Collection isn't able to handle cross thread changes, yet");
				throw new Exception();
#endif
			}
		}
	}
}
