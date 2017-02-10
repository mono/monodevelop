////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines the types of matching that can be done on completion items.
    /// </summary>
    public enum CompletionMatchType
    {
        /// <summary>
        /// Match the display text of the completion.
        /// </summary>
        MatchDisplayText,

        /// <summary>
        /// Match the insertion text of the completion.
        /// </summary>
        MatchInsertionText
    }
}
