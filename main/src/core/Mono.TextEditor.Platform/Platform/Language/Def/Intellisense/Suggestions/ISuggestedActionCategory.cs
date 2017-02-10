// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public interface ISuggestedActionCategory
    {
        /// <summary>
        /// The unique name of the <see cref="ISuggestedActionCategory"/>.
        /// </summary>
        /// <value>This name must be unique, and must not be null.</value>
        /// <remarks>Comparisons performed on this name are case-insensitive.</remarks>
        string CategoryName { get; }

        /// <summary>
        /// The localized display name of the <see cref="ISuggestedActionCategory"/>.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Returns <c>true</c> if this <see cref="ISuggestedActionCategory"/>
        /// derives from the category specified by <paramref name="category"/>.
        /// </summary>
        /// <param name="category">The name of the base category.</param>
        /// <returns><c>true</c> if this category derives from the one specified by <paramref name="category"/>, otherwise <c>false</c>.</returns>
        bool IsOfCategory(string category);

        /// <summary>
        /// The set of all categories from which the current <see cref="ISuggestedActionCategory"></see> is derived.
        /// </summary>
        /// <value>This value is never null, though it may be an empty set.</value>
        IEnumerable<ISuggestedActionCategory> BaseCategories { get; }
    }
}
