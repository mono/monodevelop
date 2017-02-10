// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Formatting
{

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Generates RTF-formatted text from a collection of snapshot spans.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part and should be imported using the following attribute:
    /// [Import(typeof(IRtfBuilderService))] 
    /// </remarks>
    public interface IRtfBuilderService
    {

        /// <summary>
        /// Gets an RTF string containing the formatted text of the snapshot spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <param name="delimiter">
        /// A delimiter string to be inserted between the RTF generated code for the <see cref="SnapshotSpan"/>s in the <see cref="NormalizedSnapshotSpanCollection"/>.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans, string delimiter);

        /// <summary>
        /// Gets an RTF string containing the formatted text of the snapshot spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans. A new line "\par" rtf keyword will be placed between the provided
        /// <see cref="SnapshotSpan"/>s.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans);

        /// <summary>
        /// Gets an RTF string that contains the formatted text of the spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans, 
        /// with the characteristics and formatting properties of <paramref name="textView"/>.
        /// All the snapshot spans must belong to <paramref name="textView"/>.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <param name="textView">
        /// The <see cref="ITextView"/> that contains the snapshot spans.
        /// </param>
        /// <param name="delimiter">
        /// A delimiter string to be inserted between the RTF generated code for the <see cref="SnapshotSpan"/>s in the <see cref="NormalizedSnapshotSpanCollection"/>.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans, ITextView textView, string delimiter);

        /// <summary>
        /// Gets an RTF string that contains the formatted text of the spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans, 
        /// with the characteristics and formatting properties of <paramref name="textView"/>.
        /// All the snapshot spans must belong to <paramref name="textView"/>. A new line "\par" rtf keyword will be 
        /// placed between the provided <see cref="SnapshotSpan"/>s.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <param name="textView">
        /// The <see cref="ITextView"/> that contains the snapshot spans.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans, ITextView textView);
    }
}
