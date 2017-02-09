// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Creates a <see cref="KeyProcessor"/> for a given <see cref="IWpfTextView"/>. 
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IKeyProcessorProvider))]
    /// Exporters must supply a NameAttribute, a ContentTypeAttribute, at least one TextViewRoleAttribute, and optionally an OrderAttribute.
    /// </remarks>
    public interface IKeyProcessorProvider
    {
        /// <summary>
        /// Creates a <see cref="KeyProcessor"/> for a given <see cref="IWpfTextView"/>.
        /// </summary>
        /// <param name="wpfTextView">The <see cref="IWpfTextView"/> for which to create the <see cref="KeyProcessor"/>.</param>
        /// <returns>The created <see cref="KeyProcessor"/>.
        /// The value may be null if this <see cref="IKeyProcessorProvider"/> does not wish to participate in the current context.</returns>
        KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView);
    }
}
