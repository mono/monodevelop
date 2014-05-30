//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.WindowsAPICodePack.Shell.Resources;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Listens for changes in/on a ShellObject and raises events when they occur.
    /// This class supports all items under the shell namespace including
    /// files, folders and virtual folders (libraries, search results and network items), etc.
    /// </summary>
    public class ShellObjectWatcher : IDisposable
    {
        private ShellObject _shellObject;
        private bool _recursive;

        private ChangeNotifyEventManager _manager = new ChangeNotifyEventManager();
        private IntPtr _listenerHandle;
        private uint _message;

        private uint _registrationId;
        private volatile bool _running;

        private SynchronizationContext _context = SynchronizationContext.Current;

        /// <summary>
        /// Creates the ShellObjectWatcher for the given ShellObject
        /// </summary>
        /// <param name="shellObject">The ShellObject to monitor</param>
        /// <param name="recursive">Whether to listen for changes recursively (for when monitoring a container)</param>
        public ShellObjectWatcher(ShellObject shellObject, bool recursive)
        {
            if (shellObject == null)
            {
                throw new ArgumentNullException("shellObject");
            }

            if (_context == null)
            {
                _context = new SynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(_context);
            }

            _shellObject = shellObject;
            this._recursive = recursive;

            var result = MessageListenerFilter.Register(OnWindowMessageReceived);
            _listenerHandle = result.WindowHandle;
            _message = result.Message;
        }

        /// <summary>
        /// Gets whether the watcher is currently running.
        /// </summary>
        public bool Running
        {
            get { return _running; }
            private set { _running = value; }
        }

        /// <summary>
        /// Start the watcher and begin receiving change notifications.        
        /// <remarks>
        /// If the watcher is running, has no effect.
        /// Registration for notifications should be done before this is called.
        /// </remarks>
        /// </summary>
        public void Start()
        {
            if (Running) { return; }

            #region Registration
            ShellNativeMethods.SHChangeNotifyEntry entry = new ShellNativeMethods.SHChangeNotifyEntry();
            entry.recursively = _recursive;

            entry.pIdl = _shellObject.PIDL;

            _registrationId = ShellNativeMethods.SHChangeNotifyRegister(
                _listenerHandle,
                ShellNativeMethods.ShellChangeNotifyEventSource.ShellLevel | ShellNativeMethods.ShellChangeNotifyEventSource.InterruptLevel | ShellNativeMethods.ShellChangeNotifyEventSource.NewDelivery,
                 _manager.RegisteredTypes, //ShellObjectChangeTypes.AllEventsMask,
                _message,
                1,
                ref entry);

            if (_registrationId == 0)
            {
                throw new Win32Exception(LocalizedMessages.ShellObjectWatcherRegisterFailed);
            }
            #endregion

            Running = true;
        }

        /// <summary>
        /// Stop the watcher and prevent further notifications from being received.
        /// <remarks>If the watcher is not running, this has no effect.</remarks>
        /// </summary>
        public void Stop()
        {
            if (!Running) { return; }
            if (_registrationId > 0)
            {
                ShellNativeMethods.SHChangeNotifyDeregister(_registrationId);
                _registrationId = 0;
            }
            Running = false;
        }

        private void OnWindowMessageReceived(WindowMessageEventArgs e)
        {
            if (e.Message.Msg == _message)
            {
                _context.Send(x => ProcessChangeNotificationEvent(e), null);
            }
        }

        private void ThrowIfRunning()
        {
            if (Running)
            {
                throw new InvalidOperationException(LocalizedMessages.ShellObjectWatcherUnableToChangeEvents);
            }
        }

        /// <summary>
        /// Processes all change notifications sent by the Windows Shell.
        /// </summary>
        /// <param name="e">The windows message representing the notification event</param>
        protected virtual void ProcessChangeNotificationEvent(WindowMessageEventArgs e)
        {
            if (!Running) { return; }
            if (e == null) { throw new ArgumentNullException("e"); }

            ChangeNotifyLock notifyLock = new ChangeNotifyLock(e.Message);

            ShellObjectNotificationEventArgs args = null;
            switch (notifyLock.ChangeType)
            {
                case ShellObjectChangeTypes.DirectoryRename:
                case ShellObjectChangeTypes.ItemRename:
                    args = new ShellObjectRenamedEventArgs(notifyLock);
                    break;
                case ShellObjectChangeTypes.SystemImageUpdate:
                    args = new SystemImageUpdatedEventArgs(notifyLock);
                    break;
                default:
                    args = new ShellObjectChangedEventArgs(notifyLock);
                    break;
            }

            _manager.Invoke(this, notifyLock.ChangeType, args);
        }

        #region Change Events

        #region Mask Events
        /// <summary>
        /// Raised when any event occurs.
        /// </summary>
        public event EventHandler<ShellObjectNotificationEventArgs> AllEvents
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.AllEventsMask, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.AllEventsMask, value);
            }
        }

        /// <summary>
        /// Raised when global events occur.
        /// </summary>
        public event EventHandler<ShellObjectNotificationEventArgs> GlobalEvents
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.GlobalEventsMask, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.GlobalEventsMask, value);
            }
        }

        /// <summary>
        /// Raised when disk events occur.
        /// </summary>
        public event EventHandler<ShellObjectNotificationEventArgs> DiskEvents
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.DiskEventsMask, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.DiskEventsMask, value);
            }
        }
        #endregion

        #region Single Events
        /// <summary>
        /// Raised when an item is renamed.
        /// </summary>
        public event EventHandler<ShellObjectRenamedEventArgs> ItemRenamed
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.ItemRename, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.ItemRename, value);
            }
        }

        /// <summary>
        /// Raised when an item is created.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> ItemCreated
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.ItemCreate, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.ItemCreate, value);
            }
        }

        /// <summary>
        /// Raised when an item is deleted.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> ItemDeleted
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.ItemDelete, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.ItemDelete, value);
            }
        }

        /// <summary>
        /// Raised when an item is updated.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> Updated
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.Update, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.Update, value);
            }
        }

        /// <summary>
        /// Raised when a directory is updated.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> DirectoryUpdated
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.DirectoryContentsUpdate, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.DirectoryContentsUpdate, value);
            }
        }

        /// <summary>
        /// Raised when a directory is renamed.
        /// </summary>
        public event EventHandler<ShellObjectRenamedEventArgs> DirectoryRenamed
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.DirectoryRename, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.DirectoryRename, value);
            }
        }

        /// <summary>
        /// Raised when a directory is created.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> DirectoryCreated
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.DirectoryCreate, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.DirectoryCreate, value);
            }
        }

        /// <summary>
        /// Raised when a directory is deleted.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> DirectoryDeleted
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.DirectoryDelete, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.DirectoryDelete, value);
            }
        }

        /// <summary>
        /// Raised when media is inserted.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> MediaInserted
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.MediaInsert, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.MediaInsert, value);
            }
        }

        /// <summary>
        /// Raised when media is removed.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> MediaRemoved
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.MediaRemove, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.MediaRemove, value);
            }
        }

        /// <summary>
        /// Raised when a drive is added.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> DriveAdded
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.DriveAdd, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.DriveAdd, value);
            }
        }

        /// <summary>
        /// Raised when a drive is removed.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> DriveRemoved
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.DriveRemove, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.DriveRemove, value);
            }
        }

        /// <summary>
        /// Raised when a folder is shared on a network.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> FolderNetworkShared
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.NetShare, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.NetShare, value);
            }
        }

        /// <summary>
        /// Raised when a folder is unshared from the network.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> FolderNetworkUnshared
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.NetUnshare, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.NetUnshare, value);
            }
        }

        /// <summary>
        /// Raised when a server is disconnected.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> ServerDisconnected
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.ServerDisconnect, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.ServerDisconnect, value);
            }
        }

        /// <summary>
        /// Raised when a system image is changed.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> SystemImageChanged
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.SystemImageUpdate, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.SystemImageUpdate, value);
            }
        }

        /// <summary>
        /// Raised when free space changes.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> FreeSpaceChanged
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.FreeSpace, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.FreeSpace, value);
            }
        }

        /// <summary>
        /// Raised when a file type association changes.
        /// </summary>
        public event EventHandler<ShellObjectChangedEventArgs> FileTypeAssociationChanged
        {
            add
            {
                ThrowIfRunning();
                _manager.Register(ShellObjectChangeTypes.AssociationChange, value);
            }
            remove
            {
                ThrowIfRunning();
                _manager.Unregister(ShellObjectChangeTypes.AssociationChange, value);
            }
        }
        #endregion

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes ShellObjectWatcher
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            Stop();
            _manager.UnregisterAll();

            if (_listenerHandle != IntPtr.Zero)
            {
                MessageListenerFilter.Unregister(_listenerHandle, _message);
            }
        }

        /// <summary>
        /// Disposes ShellObjectWatcher.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer for ShellObjectWatcher
        /// </summary>
        ~ShellObjectWatcher()
        {
            Dispose(false);
        }

        #endregion
    }


}
