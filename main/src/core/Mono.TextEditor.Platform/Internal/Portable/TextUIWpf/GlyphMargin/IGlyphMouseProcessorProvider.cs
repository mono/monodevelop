// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Provides a mouse binding for the glyph margin.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IGlyphMouseProcessorProvider))]
    /// Exporters must supply a NameAttribute, OrderAttribute, 
    /// and at least one ContentTypeAttribute.
    /// </remarks>
	public interface IGlyphMouseProcessorProvider
	{
        /// <summary>
        /// Creates an <see cref="IMouseProcessor"/> for the glyph margin, given a <see cref="IWpfTextViewHost"/> and a <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="wpfTextViewHost">The <see cref="IWpfTextViewHost"/> associated with the glyph margin.</param>
        /// <param name="margin">The <see cref="IWpfTextViewMargin"/>.</param>
        /// <returns>The <see cref="IMouseProcessor"/> for the glyph margin.  
        /// The value may be null if this <see cref="IGlyphMouseProcessorProvider"/> does not participate.</returns>
        IMouseProcessor GetAssociatedMouseProcessor(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin);
	}
}
