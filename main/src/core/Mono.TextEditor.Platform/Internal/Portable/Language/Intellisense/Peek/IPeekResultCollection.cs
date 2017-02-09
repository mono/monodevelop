// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a collection of <see cref="IPeekResult"/>s populated by content-type specific <see cref="IPeekResultSource"/>
    /// implementations when they are being queried for <see cref="IPeekResult"/>s.
    /// </summary>
    public interface IPeekResultCollection
    {
        /// <summary>
        /// Gets the number of elements contained in the <see cref="IPeekResultCollection"/>.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///  Adds an item to the <see cref="IPeekResultCollection"/>.
        /// </summary>
        /// <param name="peekResult">The object to add to the <see cref="IPeekResultCollection"/>.</param>
        void Add(IPeekResult peekResult);

        /// <summary>
        /// Removes all results from the <see cref="IPeekResultCollection"/>.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the <see cref="IPeekResultCollection"/> contains a specific result.
        /// </summary>
        /// <param name="peekResult">The object to locate in the <see cref="IPeekResultCollection"/>.</param>
        /// <returns><c>true</c> if the result is found in the <see cref="IPeekResultCollection"/>; <c>false</c> otherwise.</returns>
        bool Contains(IPeekResult peekResult);

        /// <summary>
        /// Inserts a result into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the result should be inserted.</param>
        /// <param name="peekResult">The result to insert.</param>
        void Insert(int index, IPeekResult peekResult);

        /// <summary>
        /// Finds the index of the result or returns -1 if the result was not found.
        /// </summary>
        /// <param name="peekResult">The result to search for in the list.</param>
        /// <param name="startAt">The start index for the search.</param>
        /// <returns>The index of the result in the list, or -1 if the result was not found.</returns>
        int IndexOf(IPeekResult peekResult, int startAt);

        /// <summary>
        /// Moves the result at the specified index to a new location in the collection.
        /// </summary>
        /// <param name="oldIndex">The zero-based index specifying the location of the result to be moved.</param>
        /// <param name="newIndex">The zero-based index specifying the new location of the result.</param>
        /// <remarks>This method inserts the result in the new location.</remarks>
        void Move(int oldIndex, int newIndex);

        /// <summary>
        /// Removes the first occurrence of a specific result from the <see cref="IPeekResultCollection"/>.
        /// </summary>
        /// <param name="item">The result to remove from the <see cref="IPeekResultCollection"/></param>
        /// <returns><c>true</c> if the result was successfully removed from the <see cref="IPeekResultCollection"/>; <c>false</c> otherwise.
        /// This method also returns <c>false</c> if the result is not found in the <see cref="IPeekResultCollection"/>.</returns>
        bool Remove(IPeekResult item);

        /// <summary>
        /// Removes the result at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the result to remove.</param>
        void RemoveAt(int index);

        /// <summary>
        /// Gets or sets the result at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The result at the specified index.</returns>
        IPeekResult this[int index] { get; set; }
    }
}
