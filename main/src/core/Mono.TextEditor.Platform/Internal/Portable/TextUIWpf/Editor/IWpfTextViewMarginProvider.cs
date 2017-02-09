// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Creates an <see cref="IWpfTextViewMargin"/> for a given <see cref="IWpfTextViewHost"/>.  
    /// </summary>
    /// <remarks><para>This is a MEF component part, and should be exported with the following attribute:
    /// <code>[Export(typeof(IWpfTextViewMarginProvider))]</code>
    /// Exporters must supply an MarginContainerAttribute, TextViewRoleAttribute,
    /// and NameAttribute.</para>
    /// <para>Exporters should provide one or more OrderAttributes, ContentTypeAttributes, and/or TextViewRoleAttributes.
    /// </para>
    /// <para>Exporters may provide zero or more ReplacesAttributes.</para>
    /// </remarks>
    public interface IWpfTextViewMarginProvider
    {
        /// <summary>
        /// Creates an <see cref="IWpfTextViewMargin"/> for the given <see cref="IWpfTextViewHost"/>.
        /// </summary>d\
        /// <param name="wpfTextViewHost">The <see cref="IWpfTextViewHost"/> for which to create the <see cref="IWpfTextViewMargin"/>.</param>
        /// <param name="marginContainer">The margin that will contain the newly-created margin.</param>
        /// <returns>The <see cref="IWpfTextViewMargin"/>.  
        /// The value may be null if this <see cref="IWpfTextViewMarginProvider"/> does not participate for this context.</returns>
        IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer);
    }
}
