using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Microsoft.VisualStudio.Text.Utilities
{
    internal class TransformedDispatcherCollection<TSourceCollection, TSourceElement, TTargetElement> : ReadOnlyCollection<TTargetElement>, INotifyCollectionChanged, INotifyPropertyChanged, IWeakEventListener, IDisposable
        where TSourceCollection : class, IEnumerable<TSourceElement>, INotifyCollectionChanged
    {
        #region Fields
        private readonly Dispatcher dispatcher;
        private readonly TSourceCollection sourceCollection;
        private readonly Func<TSourceElement, TTargetElement> setup;
        private readonly Action<TTargetElement> teardown;
        private bool disposed;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new transformed collection wrapping a source collection.
        /// </summary>
        /// <param name="sourceCollection">The source collection that this collection wraps.</param>
        /// <param name="setup">The logic for creating a transformed element from a source element.</param>
        /// <param name="teardown">The logic for destroying a transformed element when removed from the transformed collection.</param>
        public TransformedDispatcherCollection(Dispatcher dispatcher, TSourceCollection sourceCollection, Func<TSourceElement, TTargetElement> setup, Action<TTargetElement> teardown = null) :
            base(new List<TTargetElement>(sourceCollection.Select(setup)))
        {
            ArgumentValidation.NotNull(dispatcher, "dispatcher");
            ArgumentValidation.NotNull(sourceCollection, "sourceCollection");
            ArgumentValidation.NotNull(setup, "setup");

            this.setup = setup;
            this.teardown = teardown;

            this.dispatcher = dispatcher;
            this.sourceCollection = sourceCollection;
            CollectionChangedEventManager.AddListener(this.sourceCollection, this);
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Public Methods
        /// <summary>
        /// Tears down all elements of the transformed collection, clears the transformed collection, and stops listening to change events on the source collection.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (!this.ReceiveWeakEvent(managerType, sender, e))
            {
                Debug.Fail("Weak event was not handled");
                return false;
            }

            return true;
        }
        #endregion

        #region Protected Methods
        protected TSourceCollection SourceCollection
        {
            get
            {
                return this.sourceCollection;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // Cleanup managed resources
                    CollectionChangedEventManager.RemoveListener(this.sourceCollection, this);

                    if (this.teardown != null)
                    {
                        foreach (var target in this.Items)
                        {
                            this.teardown(target);
                        }
                    }

                    this.Items.Clear();
                }

                // Cleanup unmanaged resources

                // Mark the object as disposed
                this.disposed = true;
            }
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }

        protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (object.ReferenceEquals(sender, this.sourceCollection))
            {
                var collectionChangedEventArgs = e as NotifyCollectionChangedEventArgs;
                if (collectionChangedEventArgs != null)
                {
                    this.OnSourceCollectionChanged(sender, collectionChangedEventArgs);
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Private Methods

        private async void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TSourceElement[] snapshot = null;
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                snapshot = this.sourceCollection.ToArray();
            }

            await CheckAccessInvokeAsync(() => UpdateCollectionOnDispatcherThread(e, snapshot));
        }

        private void UpdateCollectionOnDispatcherThread(NotifyCollectionChangedEventArgs e, TSourceElement[] newElementsSnapshot)
        {
            NotifyCollectionChangedEventArgs collectionChangedEventArgs = null;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (this.teardown != null)
                {
                    foreach (var target in this.Items)
                    {
                        this.teardown(target);
                    }
                }

                this.Items.Clear();

                foreach (var source in newElementsSnapshot)
                {
                    this.Items.Add(this.setup(source));
                }

                collectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            }
            else
            {
                List<object> oldItems = null;
                if (e.OldItems != null)
                {
                    oldItems = new List<object>();
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        TTargetElement target = this.Items[e.OldStartingIndex];
                        oldItems.Add(target);

                        if (this.teardown != null)
                        {
                            this.teardown(target);
                        }

                        this.Items.RemoveAt(e.OldStartingIndex);
                    }
                }

                List<object> newItems = null;
                if (e.NewItems != null)
                {
                    newItems = new List<object>();
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        TTargetElement target = this.setup((TSourceElement)e.NewItems[i]);
                        newItems.Add(target);
                        this.Items.Insert(i + e.NewStartingIndex, target);
                    }
                }

                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    collectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, e.OldStartingIndex);
                }
                else if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    collectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, e.NewStartingIndex);
                }
                else if (e.Action == NotifyCollectionChangedAction.Move)
                {
                    collectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, newItems, e.NewStartingIndex, e.OldStartingIndex);
                }
                else if (e.Action == NotifyCollectionChangedAction.Replace)
                {
                    collectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, e.NewStartingIndex);
                }
            }

            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            this.OnCollectionChanged(collectionChangedEventArgs);
        }

        /// <summary>
        /// Executes the specified action on a thread associated with object's dispatcher.
        /// This invokes a InvokeAsync on the Dispatcher, does not wait for the action
        /// to complete -- returns immediately.
        /// </summary>
        /// <param name="action">An action to execute.</param>
        /// <returns>A task that completes when action has completed.</returns>
        private async Task CheckAccessInvokeAsync(Action action)
        {
            ArgumentValidation.NotNull(action, "action");

            if (this.dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                await this.dispatcher.InvokeAsync(action, DispatcherPriority.Normal);
            }
        }
        #endregion
    }
}
