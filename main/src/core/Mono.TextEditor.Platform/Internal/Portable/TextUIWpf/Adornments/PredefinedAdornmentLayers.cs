// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Differencing;

    /// <summary>
    /// This static class defines the names of the adornment layers
    /// predefined by the editor.
    /// </summary>
    public static class PredefinedAdornmentLayers
    {
        /// <summary>
        /// The outlining layer contains the collapse hint adornment.
        /// </summary>
        public const string Outlining = "Outlining";

        /// <summary>
        /// The squiggle layer contains the squiggle adornment used to indicate errors.
        /// </summary>
        public const string Squiggle = "Squiggle";

        /// <summary>
        /// The selection layer hosts the selection and provisional highlight text caret for IME input mode.
        /// </summary>
        public const string Selection = "SelectionAndProvisionHighlight";

        /// <summary>
        /// The caret layer contains the text caret.
        /// </summary>
        public const string Caret = "Caret";

        /// <summary>
        /// The text layer contains the textual content of the editor.
        /// </summary>
        public const string Text = "Text";

        /// <summary>
        /// The text marker layer contains the text markers provided by the <see cref="ITextMarkerProviderFactory"/> classes.
        /// </summary>
        public const string TextMarker = "TextMarker";

        /// <summary>
        /// The current line highlighter layer containst the current line highlighter adronment.
        /// </summary>
        public const string CurrentLineHighlighter = "CurrentLineHighlighter";

        /// <summary>
        /// The layer used to draw the line differences in the views created by the <see cref="IDifferenceViewer"/>.
        /// </summary>
        public const string DifferenceChanges = "DifferenceChanges";

        /// <summary>
        /// The layer used to draw the word differences in the views created by the <see cref="IDifferenceViewer"/>.
        /// </summary>
        public const string DifferenceWordChanges = "DifferenceWordChanges";

        /// <summary>
        /// The layer used to draw hashmarks in to align blocks in the <see cref="IDifferenceViewer"/> split view.
        /// </summary>
        public const string DifferenceSpace = "DifferenceSpace";

        /// <summary>
        /// Name of the layer used to draw the Peek or other adornments placed between lines of text.
        /// </summary>
        public const string InterLine = "Inter Line Adornment";

        /// <summary>
        /// Name of the layer used to draw the closing brace adornment for brace completion.
        /// </summary>
        public const string BraceCompletion = "BraceCompletion";

        /// <summary>
        /// Name of the layer used to draw the vertical structural block adornments.
        /// </summary>
        public const string BlockStructure = "BlockStructure";
    }
}
