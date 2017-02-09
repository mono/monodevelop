// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a priority of <see cref="SuggestedActionSet"/>, that is used to order suggestions when
    /// presenting them to a user.
    /// </summary>
    public enum SuggestedActionSetPriority
    {
        /// <summary>
        /// No particular priority.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Low priority suggestion.
        /// </summary>
        Low = 1,
        
        /// <summary>
        /// Medium priority suggestion.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High priority suggestion.
        /// </summary>
        High = 3
    }
}
