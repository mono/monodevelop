// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media.TextFormatting;

    /// <summary>
    /// Provides text formatting properties.
    /// </summary>
    public class TextFormattingParagraphProperties : TextParagraphProperties
    {
        private TextFormattingRunProperties _defaultTextRunProperties;
        private double _defaultIncrementalTab;

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingParagraphProperties"/>.
        /// </summary>
        /// <param name="defaultTextRunProperties">The default properties for the paragraph.</param>
        /// <remarks>This sets the tab size to 4 * the FontRenderingEmSize.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="defaultTextRunProperties"/> is null.</exception>
        public TextFormattingParagraphProperties(TextFormattingRunProperties defaultTextRunProperties)
        {
            if (defaultTextRunProperties == null)
                throw new ArgumentNullException("defaultTextRunProperties");

            _defaultTextRunProperties = defaultTextRunProperties;
            _defaultIncrementalTab = defaultTextRunProperties.FontRenderingEmSize * 4.0;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingParagraphProperties"/>.
        /// </summary>
        /// <param name="defaultTextRunProperties">The default properties for the paragraph.</param>
        /// <param name="defaultTabSize">The tab size in pixels.</param>
        /// <exception cref="ArgumentNullException"><paramref name="defaultTextRunProperties"/> or <paramref name="defaultTabSize"/> is null.</exception>
        public TextFormattingParagraphProperties(TextFormattingRunProperties defaultTextRunProperties, double defaultTabSize)
        {
            if (defaultTextRunProperties == null)
                throw new ArgumentNullException("defaultTextRunProperties");

            if (double.IsNaN(defaultTabSize) || (defaultTabSize <= 0.0))
                throw new ArgumentNullException("defaultTabSize");

            _defaultTextRunProperties = defaultTextRunProperties;
            _defaultIncrementalTab = defaultTabSize;
        }

        #region TextParagraphProperties Members

        /// <summary>
        /// Gets the default incremental tab.
        /// </summary>
        public override double DefaultIncrementalTab
        {
            get
            {
                return _defaultIncrementalTab;
            }
        }

        /// <summary>
        /// Gets the default text run properties.
        /// </summary>
        public override TextRunProperties DefaultTextRunProperties
        {
            get
            {
                return _defaultTextRunProperties;
            }
        }

        /// <summary>
        /// Determines whether this is the first line in a paragraph.
        /// </summary>
        public override bool FirstLineInParagraph
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the flow direction.
        /// </summary>
        public override FlowDirection FlowDirection
        {
            get
            {
                return FlowDirection.LeftToRight;
            }
        }

        /// <summary>
        /// Gets the text alignment.
        /// </summary>
        public override TextAlignment TextAlignment
        {
            get
            {
                return TextAlignment.Left;
            }
        }

        /// <summary>
        /// Gets the size of the indent.
        /// </summary>
        public override double Indent
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the line height.
        /// </summary>
        public override sealed double LineHeight
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the text marker properties.
        /// </summary>
        public override TextMarkerProperties TextMarkerProperties
        {
            get
            {
                return default(TextMarkerProperties);
            }
        }

        /// <summary>
        /// Gets the text wrapping.
        /// </summary>
        public override sealed TextWrapping TextWrapping
        {
            get
            {
                // We always set this to Wrap - that way, the View implementation has control over how long
                // it wants the line to continue
                return TextWrapping.Wrap;
            }
        }

        #endregion // TextParagraphProperties Members
    }
}
