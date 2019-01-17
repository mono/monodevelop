//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Event arguments for when the user is notified of items
    /// that have been removed from the taskbar destination list
    /// </summary>
    public class UserRemovedJumpListItemsEventArgs : EventArgs
    {
        private readonly IEnumerable _removedItems;

        internal UserRemovedJumpListItemsEventArgs(IEnumerable RemovedItems)
        {
            _removedItems = RemovedItems;
        }

        /// <summary>
        /// The collection of removed items based on path.
        /// </summary>
        public IEnumerable RemovedItems
        {
            get { return _removedItems; }
        }
    }
}
