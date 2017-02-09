// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an object (for instance a symbol in a document) that can be a source of a <see cref="IPeekSession"/>.
    /// </summary>
    /// <remarks>
    /// Content-type specific Peek providers define concrete <see cref="IPeekableItem"/> implementations holding onto
    /// all the context they need to provide <see cref="IPeekResult"/>s for the item referenced at the
    /// <see cref="IPeekSession"/>'s trigger point and relationship.
    /// When an <see cref="IPeekSession"/> is triggered, <see cref="IPeekableItemSource"/>s matching the document's
    /// content type will be called in order (defined by the content type specificity and Order attributes) to analyze 
    /// the <see cref="IPeekSession"/>'s trigger point and relationship. 
    /// If an <see cref="IPeekableItemSource"/> recognizes the trigger point as a location of an item the provider can provide
    /// <see cref="IPeekResult"/>s for the relationship, it creates its concrete <see cref="IPeekableItem"/> instance
    /// capturing all the necessary context and adds it to the <see cref="IPeekSession"/>. Then the Peek provider will 
    /// be called to provide <see cref="IPeekResult"/>s given this <see cref="IPeekableItem"/> instance.
    /// </remarks>
    public interface IPeekableItem
    {
        /// <summary>
        /// Defines the localized string used for displaying this item to the user.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets an enumeration of all relationships supported by this <see cref="IPeekableItem"/> instance.
        /// </summary>
        IEnumerable<IPeekRelationship> Relationships { get; }

        /// <summary>
        /// Gets or creates an <see cref="IPeekResultSource"/> instance representing a source of results of querying this
        /// <see cref="IPeekableItem"/> for the given relationship.
        /// </summary>
        /// <param name="relationshipName">The case insenitive name of the relationship to be queried for results.</param>
        /// <returns>A valid <see cref="IPeekResultSource"/> instance or null if this <see cref="IPeekableItem"/> instance 
        /// can not provide results for the given relationship.</returns>
        IPeekResultSource GetOrCreateResultSource(string relationshipName);
    }
}
