// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Formatting
{
    using System.ComponentModel.Composition;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;

    /// <summary>
    /// Creates formatted text sources.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IFormattedTextSourceFactoryService factory = null;
    /// </remarks>
    public interface IFormattedTextSourceFactoryService 
    {
        /// <summary>
        /// Creates an <see cref="IFormattedLineSource"/> for the given view configuration.
        /// </summary>
        /// <param name="sourceTextSnapshot">The text snapshot for the source buffer.</param>
        /// <param name="visualBufferSnapshot">The text snapshot for the visual buffer.</param>
        /// <param name="tabSize">The number of spaces between each tab stop.</param>
        /// <param name="baseIndent">The base indentation for all lines.</param>
        /// <param name="wordWrapWidth">The word wrap width in logical pixels.</param>
        /// <param name="maxAutoIndent">The maximum amount to auto-indent wrapped lines.</param>
        /// <param name="useDisplayMode">Use WPF Display TextFormattingMode for formatting text.</param>
        /// <param name="aggregateClassifier">The aggregate of all classifiers on the view.</param>
        /// <param name="sequencer">The text and adornment sequencer for the view. If null, there are no space negotiating adornments.</param>
        /// <param name="classificationFormatMap">The classification format map to use while formatting text.</param>
        /// <returns>A new text formatting source for that snapshot.</returns>
        IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot,
                                    ITextSnapshot visualBufferSnapshot,
                                    int tabSize,
                                    double baseIndent,
                                    double wordWrapWidth,
                                    double maxAutoIndent,
                                    bool useDisplayMode,
                                    IClassifier aggregateClassifier,
                                    ITextAndAdornmentSequencer sequencer,
                                    IClassificationFormatMap classificationFormatMap);

        
        /// <summary>
        /// Creates an <see cref="IFormattedLineSource"/> for the given view configuration.
        /// </summary>
        /// <param name="sourceTextSnapshot">The text snapshot for the source buffer.</param>
        /// <param name="visualBufferSnapshot">The text snapshot for the visual buffer.</param>
        /// <param name="tabSize">The number of spaces between each tab stop.</param>
        /// <param name="baseIndent">The base indentation for all lines.</param>
        /// <param name="wordWrapWidth">The word wrap width in logical pixels.</param>
        /// <param name="maxAutoIndent">The maximum amount to auto-indent wrapped lines.</param>
        /// <param name="useDisplayMode">Use WPF Display TextFormattingMode for formatting text.</param>
        /// <param name="aggregateClassifier">The aggregate of all classifiers on the view.</param>
        /// <param name="sequencer">The text and adornment sequencer for the view. If null, there are no space negotiating adornments.</param>
        /// <param name="classificationFormatMap">The classification format map to use while formatting text.</param>
        /// <param name="isViewWrapEnabled">Whether word wrap glyphs are enabled for wrapped lines.</param>
        /// <returns>A new text formatting source for that snapshot.</returns>
        IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot,
                                    ITextSnapshot visualBufferSnapshot,
                                    int tabSize,
                                    double baseIndent,
                                    double wordWrapWidth,
                                    double maxAutoIndent,
                                    bool useDisplayMode,
                                    IClassifier aggregateClassifier,
                                    ITextAndAdornmentSequencer sequencer,
                                    IClassificationFormatMap classificationFormatMap, 
                                    bool isViewWrapEnabled);   

        /// <summary>
        /// Creates an <see cref="IFormattedLineSource"/> for the given view configuration.
        /// </summary>
        /// <param name="sourceTextSnapshot">The text snapshot for the source buffer.</param>
        /// <param name="visualBufferSnapshot">The text snapshot for the visual buffer.</param>
        /// <param name="tabSize">The number of spaces between each tab stop.</param>
        /// <param name="baseIndent">The base indentation for all lines.</param>
        /// <param name="wordWrapWidth">The word wrap width in logical pixels.</param>
        /// <param name="maxAutoIndent">The maximum amount to auto-indent wrapped lines.</param>
        /// <param name="useDisplayMode">Use WPF Display TextFormattingMode for formatting text.</param>
        /// <param name="sequencer">The text and adornment sequencer for the view. If null, there are no space negotiating adornments.</param>
        /// <param name="classificationFormatMap">The classification format map to use while formatting text.</param>
        /// <returns>A new text formatting source for that snapshot.</returns>
        /// <remarks>This method is equivalent to calling the Create(...) method above with an aggregate classifier that
        /// never classifies any text.</remarks>
        IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot,
                                    ITextSnapshot visualBufferSnapshot,
                                    int tabSize,
                                    double baseIndent,
                                    double wordWrapWidth,
                                    double maxAutoIndent,
                                    bool useDisplayMode,
                                    ITextAndAdornmentSequencer sequencer,
                                    IClassificationFormatMap classificationFormatMap);
    }
}
