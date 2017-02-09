// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a provider of suggested actions for a span of text in a <see cref="ITextBuffer" />.
    /// <see cref="ISuggestedActionsSource"/> instances are created by <see cref="ISuggestedActionsSourceProvider"/> 
    /// MEF components matching text buffer's content type.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedActionsSource : IDisposable, ITelemetryIdProvider<Guid>
    {
        /// <summary>
        /// Raised when a list of available suggested actions have changed.
        /// </summary>
        event EventHandler<EventArgs> SuggestedActionsChanged;

        /// <summary>
        /// Synchronously returns a list of suggested actions for a given span of text.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="range">A span of text in the <see cref="ITextBuffer" /> over which to return suggested actions.</param>
        /// <param name="cancellationToken">A cancellation token that allows to cancel getting list of suggested actions.</param>
        /// <returns>A list of suggested actions or null if no actions can be suggested for a given span of text in the <see cref="ITextBuffer" />.</returns>
        IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously indicates whether this provider can provide any suggested actions for a given span of text in the <see cref="ITextBuffer" />.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="range">A span of text in the <see cref="ITextBuffer" /> over which to check for suggested actions.</param>
        /// <param name="cancellationToken">A cancellation token that allows to cancel checking for suggested actions.</param>
        /// <returns>A <see cref="Task"/> instance that when completed returns true if any suggested
        /// actions can be provided or false otherwise.</returns>
        Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken);
    }
}
