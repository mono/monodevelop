// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a set of suggested action category names.
    /// </summary>
    public interface ISuggestedActionCategorySet : IEnumerable<string>
    {
        /// <summary>
        /// Determines whether the given suggested action category is a member of the set.
        /// </summary>
        bool Contains(string categoryName);
    }
}
