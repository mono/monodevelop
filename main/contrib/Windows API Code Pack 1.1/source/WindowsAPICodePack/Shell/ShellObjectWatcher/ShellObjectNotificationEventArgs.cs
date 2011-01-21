using System;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Base class for the Event Args for change notifications raised by <typeparamref name="ShellObjectWatcher"/>.
    /// </summary>
    public class ShellObjectNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// The type of the change that happened to the ShellObject
        /// </summary>
        public ShellObjectChangeTypes ChangeType { get; private set; }

        /// <summary>
        /// True if the event was raised as a result of a system interrupt.
        /// </summary>
        public bool FromSystemInterrupt { get; private set; }

        internal ShellObjectNotificationEventArgs(ChangeNotifyLock notifyLock)
        {
            ChangeType = notifyLock.ChangeType;
            FromSystemInterrupt = notifyLock.FromSystemInterrupt;
        }
    }

    /// <summary>
    /// The data that describes a ShellObject event with a single path parameter
    /// </summary>
    public class ShellObjectChangedEventArgs : ShellObjectNotificationEventArgs
    {
        /// <summary>
        /// The path of the shell object
        /// </summary>
        public string Path { get; private set; }

        internal ShellObjectChangedEventArgs(ChangeNotifyLock notifyLock)
            : base(notifyLock)
        {
            Path = notifyLock.ItemName;
        }
    }

    /// <summary>
    /// The data that describes a ShellObject renamed event
    /// </summary>
    public class ShellObjectRenamedEventArgs : ShellObjectChangedEventArgs
    {
        /// <summary>
        /// The new path of the shell object
        /// </summary>
        public string NewPath { get; private set; }

        internal ShellObjectRenamedEventArgs(ChangeNotifyLock notifyLock)
            : base(notifyLock)
        {
            NewPath = notifyLock.ItemName2;
        }
    }

    /// <summary>
    /// The data that describes a SystemImageUpdated event.
    /// </summary>
    public class SystemImageUpdatedEventArgs : ShellObjectNotificationEventArgs
    {
        /// <summary>
        /// Gets the index of the system image that has been updated.
        /// </summary>
        public int ImageIndex { get; private set; }

        internal SystemImageUpdatedEventArgs(ChangeNotifyLock notifyLock)
            : base(notifyLock)
        {
            ImageIndex = notifyLock.ImageIndex;
        }
    }


}
