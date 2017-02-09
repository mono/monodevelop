// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents the completion status of querying LightBulb providers for suggested actions.
    /// </summary>
    public enum QuerySuggestedActionCompletionStatus
    {
        /// <summary>
        /// Querying LightBulb providers for suggested actions completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Querying LightBulb providers for suggested actions was cancelled.
        /// </summary>
        Canceled
    }
}
