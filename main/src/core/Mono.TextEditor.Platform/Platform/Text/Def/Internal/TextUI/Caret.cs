//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Manipulates the on-screen caret in the editor.
    /// </summary>
    public abstract class Caret : DisplayTextPoint
    {
        /// <summary>
        /// When implemented in a derived class, moves the caret to the next character.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToNextCharacter(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the previous character.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToPreviousCharacter(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the beginning of the previous line in the buffer.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToBeginningOfPreviousLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the beginning of the next line in the buffer.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToBeginningOfNextLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the beginning of the previous line in the view.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <remarks>
        /// If the caret is on the first line of the file, the caret is moved to the beginning of the line.
        /// </remarks>
        public abstract void MoveToBeginningOfPreviousViewLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the beginning of the next line in the view.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <remarks>
        /// If the caret is on the last line of the file, the caret is moved to the end of the line.
        /// </remarks>
        public abstract void MoveToBeginningOfNextViewLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret one line up, preserving its horizontal position.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToPreviousLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret one line down, preserving its horizontal position.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToNextLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret one page up.
        /// </summary>
        public abstract void MovePageUp();

        /// <summary>
        ///  When implemented in a derived class, moves the caret one page down.
        /// </summary>
        public abstract void MovePageDown();

        /// <summary>
        ///  When implemented in a derived class, moves the caret one page up.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MovePageUp(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret one page down.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MovePageDown(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the end of the line in the buffer.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToEndOfLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the start of the line in the buffer.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToStartOfLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the end of the line in the view.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToEndOfViewLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the start of the line in the view.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToStartOfViewLine(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the position and optionally extends the selection
        /// if necessary.
        /// </summary>
        /// <param name="position">
        /// The position to place the caret.
        /// </param>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than 0 or greater than the line number of the last line in the TextBuffer.</exception>
        public abstract void MoveTo(int position, bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the start of the specified line.
        /// </summary>
        /// <param name="lineNumber">
        /// The line number to which to move the caret.
        /// </param>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than 0 or greater than the line number of the last line in the TextBuffer.</exception>
        public abstract void MoveToLine(int lineNumber, bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to an offset from the start of the specified line.
        /// </summary>
        /// <param name="lineNumber">
        /// The line number to which to move the caret.
        /// </param>
        /// <param name="offset">
        /// The number of characters from the start of the line at which the caret should be moved.
        /// </param>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <remarks>If <paramref name="offset"/> exceeds the length of the line and virtual space is enabled, the caret will be
        /// positioned in virtual space. Otherwise the caret will be placed at the end of the line.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero 
        /// or greater than the line number of the last line in the text buffer, or
        /// <paramref name="offset"/> is less than zero.</exception>
        public abstract void MoveToLine(int lineNumber, int offset, bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the start of the document.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToStartOfDocument(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the end of the document.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToEndOfDocument(bool extendSelection);

        /// <summary>
        /// When implemented in a derived class,m oves the caret to the end of the current word, or to the beginning of the
        /// next word if it is already at the end of the current word.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToNextWord(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, moves the caret to the start of the current word, or to the end of the
        /// previous word if it is already at the start of the current word.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        public abstract void MoveToPreviousWord(bool extendSelection);

        /// <summary>
        ///  When implemented in a derived class, ensures that the caret is visible on the screen.
        /// </summary>
        public abstract void EnsureVisible();

        /// <summary>
        ///  When implemented in a derived class, gets advanced caret functionality.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract ITextCaret AdvancedCaret
        {
            get;
        }
    }
}
