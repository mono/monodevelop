//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Operations;

    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

    internal sealed class DefaultTextPointPrimitive : TextPoint
    {
        private TextBuffer _textBuffer;
        private ITrackingPoint _trackingPoint;
        private IEditorOptions _editorOptions;
        private ITextStructureNavigator _textStructureNavigator;
        private ITextSearchService _findLogic;
        private IBufferPrimitivesFactoryService _bufferPrimitivesFactory;

        public DefaultTextPointPrimitive(
            TextBuffer textBuffer,
            int position,
            ITextSearchService findLogic,
            IEditorOptions editorOptions,
            ITextStructureNavigator textStructureNavigator,
            IBufferPrimitivesFactoryService bufferPrimitivesFactory)
        {
            if ((position < 0) ||
                (position > textBuffer.AdvancedTextBuffer.CurrentSnapshot.Length))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            _textBuffer = textBuffer;
            _trackingPoint = _textBuffer.AdvancedTextBuffer.CurrentSnapshot.CreateTrackingPoint(0, PointTrackingMode.Positive);
            _editorOptions = editorOptions;
            _textStructureNavigator = textStructureNavigator;
            _findLogic = findLogic;
            _bufferPrimitivesFactory = bufferPrimitivesFactory;

            MoveTo(position);
        }

        public override TextBuffer TextBuffer
        {
            get { return _textBuffer; }
        }

        public override int CurrentPosition
        {
            get { return _trackingPoint.GetPosition(_textBuffer.AdvancedTextBuffer.CurrentSnapshot); }
        }

        public override int Column
        {
            get
            {
                SnapshotPoint currentLocation = _trackingPoint.GetPoint(_textBuffer.AdvancedTextBuffer.CurrentSnapshot);

                ITextSnapshotLine line = currentLocation.GetContainingLine();

                if (line.Start != currentLocation.Position)
                {
                    string lineText = _textBuffer.AdvancedTextBuffer.CurrentSnapshot.GetText(Span.FromBounds(line.Start, currentLocation.Position));

                    StringInfo lineTextInfo = new StringInfo(lineText);

                    int column = 0;
                    for (int i = 0; i < lineTextInfo.LengthInTextElements; i++)
                    {
                        string textElement = lineTextInfo.SubstringByTextElements(i, 1);
                        if (textElement == "\t")
                        {
                            // If there is a tab in the text, then the column automatically jumps
                            // to the next tab stop.
                            int tabSize = _editorOptions.GetTabSize();
                            column = ((column / tabSize) + 1) * tabSize;
                        }
                        else
                        {
                            column++;
                        }
                    }

                    return column;
                }

                return 0;
            }
        }

        public override bool DeleteNext()
        {
            string nextCharacter = GetNextCharacter();
            return PrimitivesUtilities.Delete(_textBuffer.AdvancedTextBuffer, CurrentPosition, nextCharacter.Length);
        }

        public override bool DeletePrevious()
        {
            ITextSnapshot snapshot = _textBuffer.AdvancedTextBuffer.CurrentSnapshot;
            int previousPosition = CurrentPosition - 1;

            if (previousPosition < 0)
            {
                return true;
            }

            int index = previousPosition;
            char currentCharacter = snapshot[previousPosition];

            // By default VS (and many other apps) will delete only the last character
            // of a combining character sequence.  The one exception to this rule is
            // surrogate pais which we are handling here.
            if (char.GetUnicodeCategory(currentCharacter) == UnicodeCategory.Surrogate)
            {
                index--;
            }

            if (index > 0)
            {
                if (currentCharacter == '\n')
                {
                    if (snapshot[previousPosition - 1] == '\r')
                    {
                        index--;
                    }
                }
            }

            return PrimitivesUtilities.Delete(_textBuffer.AdvancedTextBuffer, index, previousPosition - index + 1);
        }

        public override TextRange GetCurrentWord()
        {
            // If the line is blank, just return a blank range for the line
            SnapshotPoint currentPoint = AdvancedTextPoint;
            ITextSnapshotLine line = currentPoint.GetContainingLine();
            if (currentPoint == line.Start && line.Length == 0)
            {
                return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, this, this);
            }

            TextExtent textExtent = GetTextExtent(CurrentPosition);

            // If the word is not significant, then see if there is a significant word right before this one,
            // and return that instead to mimic VS 9 behavior.
            if (!textExtent.IsSignificant)
            {
                if ((textExtent.Span.Start > 0) && (textExtent.Span.Length > 0))
                {
                    if (textExtent.Span.Start == CurrentPosition)
                    {
                        if (!char.IsWhiteSpace(_textBuffer.AdvancedTextBuffer.CurrentSnapshot[textExtent.Span.Start - 1]))
                        {
                            textExtent = GetTextExtent(textExtent.Span.Start - 1);
                        }
                    }
                    else if (CurrentPosition == EndOfLine)
                    {
                        // If this text point is on the end of the line, then check to see if there is a word
                        // just before the whitespace.
                        if (!char.IsWhiteSpace(_textBuffer.AdvancedTextBuffer.CurrentSnapshot[textExtent.Span.Start - 1]))
                        {
                            TextExtent newExtent = new TextExtent(GetTextExtent(textExtent.Span.Start - 1));
                            textExtent = new TextExtent(new SnapshotSpan(newExtent.Span.Start, textExtent.Span.End), true);
                        }
                    }
                }
            }

            TextPoint startPoint = Clone();
            startPoint.MoveTo(textExtent.Span.Start);
            TextPoint endPoint = Clone();
            endPoint.MoveTo(textExtent.Span.End);

            return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, startPoint, endPoint);
        }

        private TextExtent GetTextExtent(int position)
        {
            return _textStructureNavigator.GetExtentOfWord(new SnapshotPoint(_textBuffer.AdvancedTextBuffer.CurrentSnapshot, position));
        }

        public override TextRange GetNextWord()
        {
            TextExtent currentWord = GetTextExtent(CurrentPosition);

            if (currentWord.Span.End < _textBuffer.AdvancedTextBuffer.CurrentSnapshot.Length)
            {
                // If the current point is at the end of the line, look for the next word on the next line
                if ((CurrentPosition == EndOfLine) && (CurrentPosition != _textBuffer.AdvancedTextBuffer.CurrentSnapshot.Length))
                {
                    TextPoint textPoint = Clone();
                    textPoint.MoveToBeginningOfNextLine();
                    textPoint.MoveTo(textPoint.StartOfLine);
                    TextExtent wordOnNextLine = GetTextExtent(textPoint.CurrentPosition);
                    if (wordOnNextLine.IsSignificant)
                    {
                        return textPoint.GetCurrentWord();
                    }
                    else if (wordOnNextLine.Span.End >= textPoint.EndOfLine)
                    {
                        return textPoint.GetTextRange(textPoint.EndOfLine);
                    }
                    return textPoint.GetNextWord();
                }
                // By default, VS stops at line breaks when determing word
                // boundaries.
                else if (ShouldStopAtEndOfLine(currentWord.Span.End) || IsCurrentWordABlankLine(currentWord))
                {
                    return GetTextRange(currentWord.Span.End);
                }

                TextExtent nextWord = GetTextExtent(currentWord.Span.End);

                if (!nextWord.IsSignificant)
                {
                    nextWord = GetTextExtent(nextWord.Span.End);
                }

                int start = nextWord.Span.Start;
                int end = nextWord.Span.End;

                // The text structure navigator can return a word with whitespace attached at the end.
                // Handle that case here.
                start = Math.Max(start, currentWord.Span.End);

                TextPoint startPoint = Clone();
                TextPoint endPoint = Clone();
                startPoint.MoveTo(start);
                endPoint.MoveTo(end);

                return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, startPoint, endPoint);
            }

            return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, TextBuffer.GetEndPoint(), TextBuffer.GetEndPoint());
        }

        public override TextRange GetPreviousWord()
        {
            TextRange currentWord = GetCurrentWord();

            if (currentWord.GetStartPoint().CurrentPosition > 0)
            {
                // By default, VS stops at line breaks when determing word
                // boundaries.
                if ((currentWord.GetStartPoint().CurrentPosition == StartOfLine) &&
                    (CurrentPosition != StartOfLine))
                {
                    return GetTextRange(currentWord.GetStartPoint());
                }

                // If the point is at the end of a word that is not whitespace, it is possible
                // that the "current word" is also the previous word in standard VS.
                if ((currentWord.GetEndPoint().CurrentPosition == CurrentPosition) &&
                    (!currentWord.IsEmpty))
                {
                    return currentWord;
                }

                TextPoint pointInPreviousWord = currentWord.GetStartPoint();
                pointInPreviousWord.MoveTo(pointInPreviousWord.CurrentPosition - 1);

                TextRange previousWord = pointInPreviousWord.GetCurrentWord();

                if (previousWord.GetStartPoint().CurrentPosition > 0)
                {
                    if (ShouldContinuePastPreviousWord(previousWord))
                    {
                        pointInPreviousWord.MoveTo(previousWord.GetStartPoint().CurrentPosition - 1);
                        previousWord = pointInPreviousWord.GetCurrentWord();
                    }
                }

                return previousWord;
            }

            return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, TextBuffer.GetStartPoint(), TextBuffer.GetStartPoint());
        }

        public override TextRange GetTextRange(TextPoint otherPoint)
        {
            if (otherPoint == null)
            {
                throw new ArgumentNullException("otherPoint");
            }

            if (otherPoint.TextBuffer != TextBuffer)
            {
                throw new ArgumentException(Strings.OtherPointFromWrongBuffer);
            }

            return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, this.Clone(), otherPoint);
        }

        public override TextRange GetTextRange(int otherPosition)
        {
            if ((otherPosition < 0) || (otherPosition > TextBuffer.AdvancedTextBuffer.CurrentSnapshot.Length))
            {
                throw new ArgumentOutOfRangeException("otherPosition");
            }

            TextPoint otherPoint = this.Clone();
            otherPoint.MoveTo(otherPosition);

            return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, this.Clone(), otherPoint);
        }

        public override bool InsertNewLine()
        {
            string lineBreak = null;
            if (_editorOptions.GetReplicateNewLineCharacter())
            {
                ITextSnapshot snapshot = _textBuffer.AdvancedTextBuffer.CurrentSnapshot;
                int position = _trackingPoint.GetPosition(snapshot);
                ITextSnapshotLine currentSnapshotLine = snapshot.GetLineFromPosition(position);
                if (currentSnapshotLine.LineBreakLength > 0)
                {
                    // use the same line ending as the current line
                    lineBreak = currentSnapshotLine.GetLineBreakText();
                }
                else
                {
                    // we are on the last line of the buffer
                    if (snapshot.LineCount > 1)
                    {
                        // use the same line ending as the penultimate line in the buffer
                        lineBreak = snapshot.GetLineFromLineNumber(snapshot.LineCount - 2).GetLineBreakText();
                    }
                }
            }
            return InsertText(lineBreak ?? _editorOptions.GetNewLineCharacter());
        }

        public override bool InsertIndent()
        {
            int tabSize = _editorOptions.GetTabSize();
            int spacesToInsert = tabSize - (Column % tabSize);
            string indentToInsert = _editorOptions.IsConvertTabsToSpacesEnabled() ? new string(' ', spacesToInsert) : "\t";

            return InsertText(indentToInsert);
        }

        public override bool InsertText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (text.Length > 0)
            {
                return PrimitivesUtilities.Insert(_textBuffer.AdvancedTextBuffer, _trackingPoint.GetPosition(_textBuffer.AdvancedTextBuffer.CurrentSnapshot), text);
            }
            else
            {
                return true;
            }
        }

        public override int LineNumber
        {
            get
            {
                return _trackingPoint.GetPoint(_textBuffer.AdvancedTextBuffer.CurrentSnapshot).GetContainingLine().LineNumber;
            }
        }

        public override int StartOfLine
        {
            get
            {
                return _trackingPoint.GetPoint(_textBuffer.AdvancedTextBuffer.CurrentSnapshot).GetContainingLine().Start;
            }
        }

        public override int EndOfLine
        {
            get
            {
                return _trackingPoint.GetPoint(_textBuffer.AdvancedTextBuffer.CurrentSnapshot).GetContainingLine().End;
            }
        }

        public override bool RemovePreviousIndent()
        {
            if (Column > 0)
            {
                int tabSize = _editorOptions.GetTabSize();

                int previousTabStop = Column - tabSize;
                if (Column % tabSize > 0)
                {
                    previousTabStop = (Column / tabSize) * tabSize;
                }

                int positionToDeleteTo = CurrentPosition;

                TextPoint newPoint = Clone();
                for (int i = CurrentPosition - 1; newPoint.Column >= previousTabStop; i--)
                {
                    newPoint.MoveTo(i);
                    string character = newPoint.GetNextCharacter();
                    if (character != " " && character != "\t")
                    {
                        break;
                    }

                    positionToDeleteTo = i;

                    if (newPoint.Column == previousTabStop)
                    {
                        break;
                    }
                }

                return PrimitivesUtilities.Delete(_textBuffer.AdvancedTextBuffer, Span.FromBounds(positionToDeleteTo, CurrentPosition));
            }
            else
            {
                return true;
            }
        }

        public override bool TransposeCharacter()
        {
            int insertionIndex = CurrentPosition;

            ITextSnapshotLine line = _textBuffer.AdvancedTextBuffer.CurrentSnapshot.GetLineFromPosition(insertionIndex);

            string lineText = line.GetText();

            if (StringInfo.ParseCombiningCharacters(lineText).Length < 2)
            {
                return true;
            }

            Span textElementLeftSpan, textElementRightSpan;
            string textElementLeft, textElementRight;
            int linePosition = CurrentPosition - StartOfLine;

            // We're at the start of a line
            if (insertionIndex == line.Start)
            {
                textElementLeft = StringInfo.GetNextTextElement(lineText, linePosition);
                textElementLeftSpan = new Span(linePosition + line.Start, textElementLeft.Length);
                textElementRight = StringInfo.GetNextTextElement(lineText, linePosition + textElementLeft.Length);
                textElementRightSpan = new Span(textElementLeftSpan.End, textElementRight.Length);
            }
            // We're at the end of a line
            else if (insertionIndex == line.End)
            {
                textElementRight = StringInfo.GetNextTextElement(lineText, linePosition - 1);
                textElementRightSpan = new Span(linePosition - 1 + line.Start, textElementRight.Length);
                textElementLeft = StringInfo.GetNextTextElement(lineText, textElementRightSpan.Start - line.Start - 1);
                textElementLeftSpan = new Span(textElementRightSpan.Start - 1, textElementRight.Length);
            }
            // We're at the middle of a line
            else
            {
                textElementRight = StringInfo.GetNextTextElement(lineText, linePosition);
                textElementRightSpan = new Span(linePosition + line.Start, textElementRight.Length);
                textElementLeft = StringInfo.GetNextTextElement(lineText, textElementRightSpan.Start - line.Start - 1);
                textElementLeftSpan = new Span(textElementRightSpan.Start - 1, textElementRight.Length);
            }

            string transposedText = textElementRight + textElementLeft;

            return PrimitivesUtilities.Replace(_textBuffer.AdvancedTextBuffer, new Span(textElementLeftSpan.Start, transposedText.Length), transposedText);
        }

        public override bool TransposeLine()
        {
            ITextSnapshot currentSnapshot = _textBuffer.AdvancedTextBuffer.CurrentSnapshot;

            // If there is only a single line in this buffer, don't do anything.
            if (currentSnapshot.LineCount <= 1)
            {
                return true;
            }

            ITextSnapshotLine currentLine = currentSnapshot.GetLineFromPosition(CurrentPosition);

            ITextSnapshotLine nextLine = null;

            if (currentLine.LineNumber == currentSnapshot.LineCount - 1)
            {
                // If the caret is on the last line of the buffer, transpose the next to last line 
                // with the last one.
                nextLine = currentSnapshot.GetLineFromLineNumber(currentLine.LineNumber - 1);
            }
            else
            {
                nextLine = currentSnapshot.GetLineFromLineNumber(currentLine.LineNumber + 1);
            }

            return TransposeLine(nextLine.LineNumber);
        }

        public override bool TransposeLine(int lineNumber)
        {
            if ((lineNumber < 0) || (lineNumber > _textBuffer.AdvancedTextBuffer.CurrentSnapshot.LineCount))
            {
                throw new ArgumentOutOfRangeException("lineNumber");
            }

            ITextSnapshot currentSnapshot = _textBuffer.AdvancedTextBuffer.CurrentSnapshot;

            // If there is only a single line in this buffer, don't do anything.
            if (currentSnapshot.LineCount <= 1)
            {
                return true;
            }

            ITextSnapshotLine currentLine = currentSnapshot.GetLineFromPosition(CurrentPosition);
            ITextSnapshotLine nextLine = currentSnapshot.GetLineFromLineNumber(lineNumber);

            using (ITextEdit edit = _textBuffer.AdvancedTextBuffer.CreateEdit())
            {
                // Make sure that both replaces succeed before applying
                if ((edit.Replace(currentLine.Extent, nextLine.GetText())) &&
                    (edit.Replace(nextLine.Extent, currentLine.GetText())))
                {
                    edit.Apply();
                    if (edit.Canceled)
                        return false;
                }
                else
                {
                    edit.Cancel();
                    return false;
                }
            }

            int newLineNumber = nextLine.LineNumber;
            if (currentLine.LineNumber == currentSnapshot.LineCount - 1)
            {
                newLineNumber = currentLine.LineNumber;
            }

            MoveTo(_textBuffer.AdvancedTextBuffer.CurrentSnapshot.GetLineFromLineNumber(newLineNumber).Start);

            return true;
        }

        public override SnapshotPoint AdvancedTextPoint
        {
            get { return _trackingPoint.GetPoint(_textBuffer.AdvancedTextBuffer.CurrentSnapshot); }
        }

        public override string GetNextCharacter()
        {
            ITextSnapshot snapshot = _textBuffer.AdvancedTextBuffer.CurrentSnapshot;
            int currentPosition = CurrentPosition;

            if (currentPosition == snapshot.Length)
            {
                return string.Empty;
            }

            int index = currentPosition;
            string characterText = char.ToString(snapshot[currentPosition]);

            while (++index < snapshot.Length)
            {
                string newText = characterText + snapshot[index];
                if (StringInfo.GetNextTextElement(newText).Length <= characterText.Length)
                    break;

                characterText = newText;
            }

            return characterText;
        }

        public override string GetPreviousCharacter()
        {
            ITextSnapshot snapshot = _textBuffer.AdvancedTextBuffer.CurrentSnapshot;
            int currentPosition = CurrentPosition;

            if (currentPosition == 0)
            {
                return string.Empty;
            }

            int index = currentPosition - 1;
            string characterText = char.ToString(snapshot[index]);

            while (--index >= 0)
            {
                string newText = snapshot[index] + characterText;
                if (StringInfo.GetNextTextElement(newText).Length <= characterText.Length)
                    break;

                characterText = newText;
            }

            return characterText;
        }

        public override TextRange Find(string pattern, TextPoint endPoint)
        {
            ValidateFindParameters(pattern, endPoint);
            return Find(pattern, FindOptions.None, endPoint);
        }

        public override TextRange Find(string pattern)
        {
            ValidateFindParameters(pattern, this);
            return Find(pattern, FindOptions.None);
        }

        public override TextRange Find(string pattern, FindOptions findOptions)
        {
            ValidateFindParameters(pattern, this);
            return FindMatch(pattern, findOptions, null);
        }

        public override TextRange Find(string pattern, FindOptions findOptions, TextPoint endPoint)
        {
            ValidateFindParameters(pattern, endPoint);
            return FindMatch(pattern, findOptions, endPoint);
        }

        private TextRange FindMatch(string pattern, FindOptions findOptions, TextPoint endPoint)
        {
            if (pattern.Length == 0)
            {
                return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, this.Clone(), this.Clone());
            }

            FindData findData = new FindData(pattern, _textBuffer.AdvancedTextBuffer.CurrentSnapshot);
            findData.FindOptions = findOptions;

            bool wrapAround = endPoint == null;

            bool searchReverse = ((findData.FindOptions & FindOptions.SearchReverse) == FindOptions.SearchReverse);

            // In the case of a wrap-around search, we can start the search at this point always.  In the case 
            // where we are searching a range in reverse, we have to start at the end point.
            int searchStartPosition = (searchReverse && !wrapAround) ? endPoint.CurrentPosition : CurrentPosition;

            SnapshotSpan? snapshotSpan = _findLogic.FindNext(searchStartPosition, wrapAround, findData);

            if (snapshotSpan.HasValue)
            {
                if ((endPoint == null) || (snapshotSpan.Value.End <= endPoint.CurrentPosition))
                {
                    return _bufferPrimitivesFactory.CreateTextRange(_textBuffer,
                        _bufferPrimitivesFactory.CreateTextPoint(_textBuffer, snapshotSpan.Value.Start),
                        _bufferPrimitivesFactory.CreateTextPoint(_textBuffer, snapshotSpan.Value.End));
                }
            }

            return _bufferPrimitivesFactory.CreateTextRange(_textBuffer, this.Clone(), this.Clone());
        }

        public override Collection<TextRange> FindAll(string pattern, TextPoint endPoint)
        {
            return FindAll(pattern, FindOptions.None, endPoint);
        }

        public override Collection<TextRange> FindAll(string pattern)
        {
            return FindAll(pattern, FindOptions.None);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions)
        {
            ValidateFindParameters(pattern, this);

            if (pattern.Length == 0)
            {
                return new Collection<TextRange>();
            }

            FindData findData = new FindData(pattern, _textBuffer.AdvancedTextBuffer.CurrentSnapshot);
            findData.FindOptions = findOptions;

            // Assume that FindAll returns matches sorted starting at the beginning
            // of the snapshot.
            Collection<SnapshotSpan> matches = _findLogic.FindAll(findData);

            List<TextRange> ranges = new List<TextRange>();
            Collection<TextRange> beforeRanges = new Collection<TextRange>();

            foreach (SnapshotSpan span in matches)
            {
                TextRange textRange = _bufferPrimitivesFactory.CreateTextRange(_textBuffer,
                    _bufferPrimitivesFactory.CreateTextPoint(_textBuffer, span.Start),
                    _bufferPrimitivesFactory.CreateTextPoint(_textBuffer, span.End));

                if (textRange.GetStartPoint().CurrentPosition < CurrentPosition)
                {
                    beforeRanges.Add(textRange);
                }
                else
                {
                    ranges.Add(textRange);
                }
            }

            ranges.AddRange(beforeRanges);

            return new Collection<TextRange>(ranges);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions, TextPoint endPoint)
        {
            ValidateFindParameters(pattern, endPoint);

            Collection<TextRange> matchingRanges = new Collection<TextRange>();
            Collection<TextRange> textRanges = FindAll(pattern, findOptions);

            foreach (TextRange textRange in textRanges)
            {
                if ((textRange.GetStartPoint().CurrentPosition >= CurrentPosition) &&
                    (textRange.GetEndPoint().CurrentPosition <= endPoint.CurrentPosition))
                {
                    matchingRanges.Add(textRange);
                }
            }

            return matchingRanges;
        }

        private void ValidateFindParameters(string pattern, TextPoint endPoint)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }
            if (endPoint.TextBuffer != TextBuffer)
            {
                throw new ArgumentException(Strings.OtherPointFromWrongBuffer);
            }
        }

        public override void MoveTo(int position)
        {
            position = FindStartOfCombiningSequence(_textBuffer.AdvancedTextBuffer.CurrentSnapshot, position);

            _trackingPoint = _textBuffer.AdvancedTextBuffer.CurrentSnapshot.CreateTrackingPoint(position, PointTrackingMode.Positive);
        }

        /// <summary>
        /// Given a snapshot and position, find the start of the first combining sequence that is less
        /// than or equal to the given position.  In the 99% case, the position is already at the start
        /// of a sequence, but we want to make sure it isn't in the middle of a surrogate pair or between
        /// a base character/surrogate pair and its associated combining characters (which may also
        /// be surrogate pairs).
        /// </summary>
        /// <returns>The start of position of the combining sequence, usually <paramref name="position"/> again.</returns>
        private static int FindStartOfCombiningSequence(ITextSnapshot snapshot, int position)
        {
            if ((position < 0) ||
                (position > snapshot.Length))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            // If this is the end of the snapshot, we don't need to check anything.
            if (position == snapshot.Length)
                return position;

            // 99% case:
            // Most of the time, the given character is neither a combining character nor a surrogate codepoint.
            // In that case, we don't need to walk around the snapshot looking for the start of a combining
            // character sequence.
            UnicodeCategory categoryAtPosition = CharUnicodeInfo.GetUnicodeCategory(snapshot[position]);
            if (!IsPotentialCombiningCharacter(categoryAtPosition))
                return position;

            // If we're at the start of a line, we're already in a good position.
            var line = snapshot.GetLineFromPosition(position);
            if (position == line.Start)
                return position;

            // If we've made it all the way here, we have a potential combining character sequence.
            // We've already determined that the current character is a potential combining character,
            // so start at one character previous.
            int endPosition = position + 1;
            int lineStart = line.Start;
            position--;

            for (; position > lineStart; position--)
            {
                char ch = snapshot[position];
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);

                // We can skip potential combining characters (we'll gather them all up once we
                // hit a base character).
                if (IsPotentialCombiningCharacter(category))
                {
                    continue;
                }

                // At this point, the character is likely a base.  Use ParseCombiningCharacters to find the sequences
                // in the text up to this point.

                string upToCurrentPosition = snapshot.GetText(Span.FromBounds(position, endPosition));
                int[] sequences = StringInfo.ParseCombiningCharacters(upToCurrentPosition);

                // If we've found more than one combining character sequence, use the last one as our
                // position and stop looping.
                if (sequences.Length > 1)
                {
                    return (position) + sequences[sequences.Length - 1];
                }
            }

            // If we walked the position back to the start of the line, do one last combining character sequence check and use the result of that
            if (position == line.Start)
            {
                string upToCurrentPosition = snapshot.GetText(Span.FromBounds(position, endPosition));
                int[] sequences = StringInfo.ParseCombiningCharacters(upToCurrentPosition);
                if (sequences.Length > 1)
                {
                    return position + sequences[sequences.Length - 1];
                }
            }

            return position;
        }

        /// <summary>
        /// Determine if the character is potentially a part of a combining sequence.  This includes
        /// the known combining character classes (M) and surrogates, as the MSDN documentation for
        /// .NET Unicode support notes that it may consider surrogates to be combining characters.
        /// </summary>
        private static bool IsPotentialCombiningCharacter(UnicodeCategory category)
        {
            return category == UnicodeCategory.EnclosingMark ||
                   category == UnicodeCategory.NonSpacingMark ||
                   category == UnicodeCategory.SpacingCombiningMark ||
                   category == UnicodeCategory.Surrogate;
        }

        public override void MoveToNextCharacter()
        {
            string nextCharacter = GetNextCharacter();
            MoveTo(CurrentPosition + nextCharacter.Length);
        }

        public override void MoveToPreviousCharacter()
        {
            string previousCharacter = GetPreviousCharacter();

            MoveTo(CurrentPosition - previousCharacter.Length);
        }

        protected override TextPoint CloneInternal()
        {
            return new DefaultTextPointPrimitive(_textBuffer, CurrentPosition, _findLogic, _editorOptions, _textStructureNavigator, _bufferPrimitivesFactory);
        }

        public override void MoveToLine(int lineNumber)
        {
            if ((lineNumber < 0) ||
                (lineNumber > _textBuffer.AdvancedTextBuffer.CurrentSnapshot.LineCount))
            {
                throw new ArgumentOutOfRangeException("lineNumber");
            }

            ITextSnapshotLine line = _textBuffer.AdvancedTextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);

            MoveTo(line.Start);
        }

        public override void MoveToEndOfLine()
        {
            MoveTo(EndOfLine);
        }

        public override void MoveToStartOfLine()
        {
            int firstNonWhitespaceCharacter = GetFirstNonWhiteSpaceCharacterOnLine().CurrentPosition;
            int currentPosition = CurrentPosition;
            int startOfLine = StartOfLine;
            if ((currentPosition == firstNonWhitespaceCharacter) ||
                (firstNonWhitespaceCharacter == EndOfLine))
            {
                MoveTo(startOfLine);
            }
            else if ((currentPosition > firstNonWhitespaceCharacter) ||
                (currentPosition == 0))
            {
                MoveTo(firstNonWhitespaceCharacter);
            }
        }

        public override void MoveToEndOfDocument()
        {
            MoveTo(_textBuffer.AdvancedTextBuffer.CurrentSnapshot.Length);
        }

        public override void MoveToStartOfDocument()
        {
            MoveTo(0);
        }

        public override void MoveToBeginningOfNextLine()
        {
            MoveTo(AdvancedTextPoint.GetContainingLine().EndIncludingLineBreak);
        }

        public override void MoveToBeginningOfPreviousLine()
        {
            ITextSnapshotLine line = AdvancedTextPoint.GetContainingLine();

            if (line.LineNumber != 0)
            {
                MoveTo(_textBuffer.AdvancedTextBuffer.CurrentSnapshot.GetLineFromLineNumber(line.LineNumber - 1).Start);
            }
            else
            {
                MoveToStartOfLine();
            }
        }

        public override void MoveToNextWord()
        {
            if (CurrentPosition != _textBuffer.AdvancedTextBuffer.CurrentSnapshot.Length)
            {
                TextRange nextWord = GetNextWord();

                if (nextWord.GetStartPoint().CurrentPosition == CurrentPosition)
                {
                    MoveTo(nextWord.GetEndPoint().CurrentPosition);
                }
                else
                {
                    MoveTo(nextWord.GetStartPoint().CurrentPosition);
                }
            }
        }

        public override void MoveToPreviousWord()
        {
            if (CurrentPosition != 0)
            {
                TextRange currentWord = GetCurrentWord();
                if (CurrentPosition != currentWord.GetStartPoint().CurrentPosition)
                {
                    MoveTo(currentWord.GetStartPoint().CurrentPosition);
                }
                else
                {
                    TextRange previousWord = GetPreviousWord();

                    MoveTo(previousWord.GetStartPoint().CurrentPosition);
                }
            }
        }

        public override TextPoint GetFirstNonWhiteSpaceCharacterOnLine()
        {
            int firstNonWhitespaceCharacterOnLine = StartOfLine;
            for (; firstNonWhitespaceCharacterOnLine < EndOfLine; firstNonWhitespaceCharacterOnLine++)
            {
                if (!char.IsWhiteSpace(AdvancedTextPoint.Snapshot[firstNonWhitespaceCharacterOnLine]))
                {
                    break;
                }
            }
            return _bufferPrimitivesFactory.CreateTextPoint(TextBuffer, firstNonWhitespaceCharacterOnLine);
        }

        /// <summary>
        /// Determine if this text point's current word is a blank line on the next line.
        /// </summary>
        /// <param name="currentWord">The current word to check.</param>
        private bool IsCurrentWordABlankLine(TextExtent currentWord)
        {
            TextPoint endOfCurrentWord = Clone();
            endOfCurrentWord.MoveTo(currentWord.Span.End);
            return (EndOfLine == CurrentPosition) && (currentWord.Span.End == endOfCurrentWord.EndOfLine);
        }

        /// <summary>
        /// Determine if a the next word should stop at the end of the line.
        /// </summary>
        private bool ShouldStopAtEndOfLine(int endOfWord)
        {
            // If the current word ends at the end of the line and the current position is not the end of the line,
            // then the word movement should stop at the end of the line.
            return (endOfWord == EndOfLine) && (EndOfLine > CurrentPosition);
        }

        /// <summary>
        /// Determine if the checking of the previous word should go past the previous word.
        /// </summary>
        /// <param name="previousWord">The current previous word.</param>
        private static bool ShouldContinuePastPreviousWord(TextRange previousWord)
        {
            // If the previous word is whitespace, and the previous word is not a blank line
            // then it should be included in the previous word.
            return char.IsWhiteSpace(previousWord.GetStartPoint().GetNextCharacter()[0]) &&
                                        previousWord.GetStartPoint().CurrentPosition != previousWord.GetStartPoint().StartOfLine;
        }
    }
}
