// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a relationship between <see cref="IPeekableItem"/>s and <see cref="IPeekResult"/>s.
    /// </summary>
    public interface IPeekRelationship
    {
        /// <summary>
        /// Gets the non-localized uniquely-identifying name of this relationship.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a localizable description of this relationship used for displaying it to the user.
        /// </summary>
        string DisplayName { get; }
    }
}
