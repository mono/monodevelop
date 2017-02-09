// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Formatting
{
    using System.Windows.Media.TextFormatting;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Creates <see cref="TextParagraphProperties"/> classes to be used when lines on the view are being formatted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextFormattingParagraphPropertiesFactoryService factory = null;
    /// </para>
    /// <para>
    /// This component is content type specific and should be annotated with one or more <see cref="ContentTypeAttribute"/>s.
    /// </para>
    /// </remarks>
    public interface ITextParagraphPropertiesFactoryService
    {
        /// <summary>
        /// Creates a <see cref="TextParagraphProperties"/> for the provided configuration.
        /// </summary>
        /// <param name="formattedLineSource">The <see cref="IFormattedLineSource"/> that's performing the formatting of the line. You can access useful properties about the ongoing formatting operation from this object.</param>
        /// <param name="textProperties">The <see cref="TextFormattingRunProperties"/> of the line for which <see cref="TextParagraphProperties"/> are to be provided. This paramter can be used to obtain formatting information about the textual contents of the line.</param>
        /// <param name="line">The <see cref="IMappingSpan"/> corresponding to the line that's being formatted/rendered.</param>
        /// <param name="lineStart">The <see cref="IMappingPoint"/> corresponding to the beginning of the line segment that's being formatted. This paramter is relevant for word-wrap scenarios where a single <see cref="ITextSnapshotLine"/> results in multiple formatted/rendered lines on the view.</param>
        /// <param name="lineSegment">The segment number of the line segment that's been currently formatted. This is a zero-based index and is applicable to word-wrapped lines. If a line is word-wrapped into 4 segments, you will receive 4 calls for the line with lineSegments of 0, 1, 2, and 3.</param>
        /// <returns>A <see cref="TextParagraphProperties"/> to be used when the line is being formatted.</returns>
        /// <remarks>Please note that you can return a <see cref="TextFormattingParagraphProperties"/> which has a convenient set of basic properties defined.</remarks>
        TextParagraphProperties Create(IFormattedLineSource formattedLineSource, TextFormattingRunProperties textProperties, IMappingSpan line, IMappingPoint lineStart, int lineSegment);
    }
}
