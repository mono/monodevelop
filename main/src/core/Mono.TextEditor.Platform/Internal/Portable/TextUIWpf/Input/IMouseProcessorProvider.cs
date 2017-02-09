// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Creates an <see cref="IMouseProcessor"/> for a <see cref="IWpfTextView"/>.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IMouseProcessorProvider))]
    /// Exporters must supply a NameAttribute, a ContentTypeAttribute, at least one TextViewRoleAttribute, and optionally an OrderAttribute.
    /// </remarks>
    public interface IMouseProcessorProvider
    {
        /// <summary>
        /// Creates an <see cref="IMouseProcessor"/> for a <see cref="IWpfTextView"/>.
        /// </summary>
        /// <param name="wpfTextView">The <see cref="IWpfTextView"/> for which to create the <see cref="IMouseProcessor"/>.</param>
        /// <returns>The created <see cref="IMouseProcessor"/>.
        /// The value may be null if this <see cref="IMouseProcessorProvider"/> does not wish to participate in the current context.</returns>
        IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView);
    }
}
