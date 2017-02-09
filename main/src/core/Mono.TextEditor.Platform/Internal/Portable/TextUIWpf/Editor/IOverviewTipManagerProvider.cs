// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.OverviewMargin
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides an <see cref="IOverviewTipManager"/>.
    /// </summary>
    /// <remarks>Implementors of this interface are MEF component parts, and should be exported with the following attributes:
    /// <code>
    /// [Export(typeof(IOverviewTipManagerProvider))]
    /// [TextViewRole(...)]
    /// [ContentType(...)]
    /// [Name(...)]
    /// [Order(After = ..., Before = ...)]
    /// </code>
    /// </remarks>
    public interface IOverviewTipManagerProvider
    {
        /// <summary>
        /// Gets the <see cref="IOverviewTipManager"/> for the given text view host.
        /// </summary>
        /// <param name="host">The IWpfTextViewHost for which the manager is being created.</param>
        /// <returns>An <see cref="IOverviewTipManager"/> for the given host.</returns>
        IOverviewTipManager GetOverviewTipManager(IWpfTextViewHost host);
    }
}