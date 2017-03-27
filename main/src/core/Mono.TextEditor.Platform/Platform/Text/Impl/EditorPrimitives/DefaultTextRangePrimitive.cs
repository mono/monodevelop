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

    internal sealed class DefaultTextRangePrimitive : TextRange
    {
        private TextPoint _startPoint;
        private TextPoint _endPoint;
        private IEditorOptions _editorOptions;
        private IEditorOptionsFactoryService _editorOptionsProvider;

        internal DefaultTextRangePrimitive(TextPoint startPoint, TextPoint endPoint, IEditorOptionsFactoryService editorOptionsProvider)
        {
            if (startPoint.CurrentPosition < endPoint.CurrentPosition)
            {
                _startPoint = startPoint.Clone();
                _endPoint = endPoint.Clone();
            }
            else
            {
                _endPoint = startPoint.Clone();
                _startPoint = endPoint.Clone();
            }
            _editorOptionsProvider = editorOptionsProvider;
            _editorOptions = _editorOptionsProvider.GetOptions(_startPoint.TextBuffer.AdvancedTextBuffer);
        }

        public override TextPoint GetStartPoint()
        {
            return _startPoint.Clone();
        }

        public override TextPoint GetEndPoint()
        {
            return _endPoint.Clone();
        }

        public override TextBuffer TextBuffer
        {
            get { return _startPoint.TextBuffer; }
        }

        public override SnapshotSpan AdvancedTextRange
        {
            get { return new SnapshotSpan(TextBuffer.AdvancedTextBuffer.CurrentSnapshot, Span.FromBounds(_startPoint.CurrentPosition, _endPoint.CurrentPosition)); }
        }

        public override bool IsEmpty
        {
            get { return _startPoint.CurrentPosition == _endPoint.CurrentPosition; }
        }

        public override bool MakeUppercase()
        {
            return ReplaceText(GetText().ToUpper(CultureInfo.CurrentCulture));
        }

        public override bool MakeLowercase()
        {
            return ReplaceText(GetText().ToLower(CultureInfo.CurrentCulture));
        }

        public override bool Capitalize()
        {
            int startPosition = _startPoint.CurrentPosition;
            if (IsEmpty)
            {
                int endPosition = _endPoint.CurrentPosition;
                TextRange currentWord = _startPoint.GetCurrentWord();
                string nextCharacter = _startPoint.GetNextCharacter();
                if (_startPoint.CurrentPosition == currentWord.GetStartPoint().CurrentPosition)
                {
                    nextCharacter = nextCharacter.ToUpper(CultureInfo.CurrentCulture);
                }
                else
                {
                    nextCharacter = nextCharacter.ToLower(CultureInfo.CurrentCulture);
                }
                if (!PrimitivesUtilities.Replace(TextBuffer.AdvancedTextBuffer, new Span(_startPoint.CurrentPosition, nextCharacter.Length), nextCharacter))
                    return false;
                _endPoint.MoveTo(endPosition);
            }
            else
            {
                using (ITextEdit edit = TextBuffer.AdvancedTextBuffer.CreateEdit())
                {
                    TextRange currentWord = _startPoint.GetCurrentWord();

                    // If the current word extends past this range, go to the next word
                    if (currentWord.GetStartPoint().CurrentPosition < _startPoint.CurrentPosition)
                    {
                        currentWord = currentWord.GetEndPoint().GetNextWord();
                    }

                    while (currentWord.GetStartPoint().CurrentPosition < _endPoint.CurrentPosition)
                    {
                        string wordText = currentWord.GetText();
                        string startElement = StringInfo.GetNextTextElement(wordText);
                        wordText = startElement.ToUpper(CultureInfo.CurrentCulture) + wordText.Substring(startElement.Length).ToLower(CultureInfo.CurrentCulture);
                        if (!edit.Replace(currentWord.AdvancedTextRange.Span, wordText))
                        {
                            edit.Cancel();
                            return false;
                        }

                        currentWord = currentWord.GetEndPoint().GetNextWord();
                    }

                    edit.Apply();

                    if (edit.Canceled)
                        return false;
                }
            }
            _startPoint.MoveTo(startPosition);
            return true;
        }

        public override bool ToggleCase()
        {
            if (IsEmpty)
            {
                TextPoint nextPoint = _startPoint.Clone();
                nextPoint.MoveToNextCharacter();
                TextRange nextCharacter = _startPoint.GetTextRange(nextPoint);
                string nextCharacterString = nextCharacter.GetText();
                if (char.IsUpper(nextCharacterString, 0))
                {
                    nextCharacterString = nextCharacterString.ToLower(CultureInfo.CurrentCulture);
                }
                else
                {
                    nextCharacterString = nextCharacterString.ToUpper(CultureInfo.CurrentCulture);
                }
                return nextCharacter.ReplaceText(nextCharacterString);
            }
            else
            {
                int startPosition = _startPoint.CurrentPosition;
                using (ITextEdit textEdit = TextBuffer.AdvancedTextBuffer.CreateEdit())
                {
                    for (int i = _startPoint.CurrentPosition; i < _endPoint.CurrentPosition; i++)
                    {
                        char newChar = textEdit.Snapshot[i];
                        if (char.IsUpper(newChar))
                        {
                            newChar = char.ToLower(newChar, CultureInfo.CurrentCulture);
                        }
                        else
                        {
                            newChar = char.ToUpper(newChar, CultureInfo.CurrentCulture);
                        }

                        if (!textEdit.Replace(i, 1, newChar.ToString()))
                        {
                            textEdit.Cancel();
                            return false; // break out early if any edit fails to reduce the time of the failure case
                        }
                    }

                    textEdit.Apply();

                    if (textEdit.Canceled)
                        return false;
                }
                _startPoint.MoveTo(startPosition);
            }
            return true;
        }

        public override bool Delete()
        {
            return PrimitivesUtilities.Delete(TextBuffer.AdvancedTextBuffer, Span.FromBounds(_startPoint.CurrentPosition, _endPoint.CurrentPosition));
        }

        public override bool Indent()
        {
            string textToInsert = _editorOptions.IsConvertTabsToSpacesEnabled() ? new string(' ', _editorOptions.GetTabSize()) : "\t";
            
            if (_startPoint.LineNumber == _endPoint.LineNumber)
            {
                return _startPoint.InsertIndent();
            }

            using (ITextEdit edit = TextBuffer.AdvancedTextBuffer.CreateEdit())
            {
                ITextSnapshot snapshot = TextBuffer.AdvancedTextBuffer.CurrentSnapshot;
                for (int i = _startPoint.LineNumber; i <= _endPoint.LineNumber; i++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                    if ((line.Length > 0) &&
                        (line.Start != _endPoint.CurrentPosition))
                    {
                        if (!edit.Insert(line.Start, textToInsert))
                            return false;
                    }
                }

                edit.Apply();

                if (edit.Canceled)
                    return false;
            }

            return true;
        }

        public override bool Unindent()
        {
            if (_startPoint.LineNumber == _endPoint.LineNumber)
            {
                return _startPoint.RemovePreviousIndent();
            }

            using (ITextEdit edit = TextBuffer.AdvancedTextBuffer.CreateEdit())
            {
                ITextSnapshot snapshot = TextBuffer.AdvancedTextBuffer.CurrentSnapshot;

                for (int i = _startPoint.LineNumber; i <= _endPoint.LineNumber; i++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                    if ((line.Length > 0) && (_endPoint.CurrentPosition != line.Start))
                    {
                        if (snapshot[line.Start] == '\t')
                        {
                            if (!edit.Delete(new Span(line.Start, 1)))
                                return false;
                        }
                        else
                        {
                            int spacesToRemove = 0;
                            for (; (line.Start + spacesToRemove < snapshot.Length) && (spacesToRemove < _editorOptions.GetTabSize());
                                spacesToRemove++)
                            {
                                if (snapshot[line.Start + spacesToRemove] != ' ')
                                {
                                    break;
                                }
                            }

                            if (spacesToRemove > 0)
                            {
                                if (!edit.Delete(new Span(line.Start, spacesToRemove)))
                                    return false;
                            }
                        }
                    }
                }

                edit.Apply();

                if (edit.Canceled)
                    return false;
            }

            return true;
        }

        public override TextRange Find(string pattern)
        {
            return Find(pattern, FindOptions.None);
        }

        public override TextRange Find(string pattern, FindOptions findOptions)
        {
            return _startPoint.Find(pattern, findOptions, _endPoint);
        }

        public override Collection<TextRange> FindAll(string pattern)
        {
            return FindAll(pattern, FindOptions.None);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions)
        {
            return _startPoint.FindAll(pattern, findOptions, _endPoint);
        }

        public override bool ReplaceText(string newText)
        {
            if (string.IsNullOrEmpty(newText))
            {
                throw new ArgumentNullException("newText");
            }

            int startPoint = _startPoint.CurrentPosition;

            if (!PrimitivesUtilities.Replace(TextBuffer.AdvancedTextBuffer, Span.FromBounds(_startPoint.CurrentPosition, _endPoint.CurrentPosition), newText))
                return false;

            _startPoint.MoveTo(startPoint);

            return true;
        }

        public override string GetText()
        {
            return TextBuffer.AdvancedTextBuffer.CurrentSnapshot.GetText(Span.FromBounds(_startPoint.CurrentPosition, _endPoint.CurrentPosition));
        }

        protected override TextRange CloneInternal()
        {
            return new DefaultTextRangePrimitive(_startPoint, _endPoint, _editorOptionsProvider);
        }

        public override void SetStart(TextPoint startPoint)
        {
            if (startPoint.TextBuffer != TextBuffer)
            {
                throw new ArgumentException(Strings.StartPointFromWrongBuffer);
            }

            if (startPoint.CurrentPosition > _endPoint.CurrentPosition)
            {
                _startPoint = _endPoint;
                _endPoint = startPoint.Clone();
            }
            else
            {
                _startPoint = startPoint.Clone();
            }
        }

        public override void SetEnd(TextPoint endPoint)
        {
            if (endPoint.TextBuffer != TextBuffer)
            {
                throw new ArgumentException("startPoint");
            }

            if (endPoint.CurrentPosition < _startPoint.CurrentPosition)
            {
                _endPoint = _startPoint;
                _startPoint = endPoint.Clone();
            }
            else
            {
                _endPoint = endPoint.Clone();
            }
        }

        public override void MoveTo(TextRange newRange)
        {
            if (newRange.TextBuffer != TextBuffer)
            {
                throw new ArgumentException(Strings.OtherRangeFromWrongBuffer);
            }

            _startPoint = newRange.GetStartPoint();
            _endPoint = newRange.GetEndPoint();
        }

        protected override IEnumerator<TextPoint> GetEnumeratorInternal()
        {
            for (int position = _startPoint.CurrentPosition; position <= _endPoint.CurrentPosition; position++)
            {
                TextPoint enumeratedPoint = _startPoint.Clone();
                enumeratedPoint.MoveTo(position);

                yield return enumeratedPoint;
            }
        }
    }
}
