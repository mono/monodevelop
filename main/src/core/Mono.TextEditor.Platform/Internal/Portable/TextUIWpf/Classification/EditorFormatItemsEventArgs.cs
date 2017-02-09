// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Provides information for the TagsChanged event.
    /// Returns the span of changed tags as a mapping span.
    /// </summary>
    public class FormatItemsEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the formatting items that have changed.
        /// </summary>
        public ReadOnlyCollection<string> ChangedItems { get; private set; }

        /// <summary>
        /// Initializes a new instance of a <see cref="FormatItemsEventArgs"/>.
        /// </summary>
        /// <param name="items">A collection of the items that have changed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        public FormatItemsEventArgs(ReadOnlyCollection<string> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            ChangedItems = items;
        }
    }
}
