// TextEditor.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.Ide.Gui
{
	public class TextEditor
	{
		IBookmarkBuffer bookmarkBuffer;
		IEditableTextBuffer textBuffer;
		IEncodedTextContent encodedTextContent;
		ICompletionWidget completionWidget;
		IClipboardHandler clipboardHandler;
		string newLine = null;
		// All line and column numbers are 1-based
		
		public static TextEditor GetTextEditor (IBaseViewContent content)
		{
			IEditableTextBuffer tb = (IEditableTextBuffer) content.GetContent (typeof(IEditableTextBuffer));
			if (tb == null)
				return null;
			
			TextEditor ed = new TextEditor ();
			ed.textBuffer = tb;
			ed.bookmarkBuffer = (IBookmarkBuffer) content.GetContent (typeof(IBookmarkBuffer));
			ed.encodedTextContent = (IEncodedTextContent) content.GetContent (typeof(IEncodedTextContent));
			ed.completionWidget = (ICompletionWidget) content.GetContent (typeof(ICompletionWidget));
			ed.clipboardHandler = (IClipboardHandler) content.GetContent (typeof(IClipboardHandler));
			return ed;
		}
		
		public bool SupportsBookmarks {
			get { return bookmarkBuffer != null; }
		}
		
		public void SetBookmarked (int position, bool mark)
		{
			if (bookmarkBuffer != null)
				bookmarkBuffer.SetBookmarked (position, mark);
		}
		
		public bool IsBookmarked (int position)
		{
			if (bookmarkBuffer != null)
				return bookmarkBuffer.IsBookmarked (position);
			else
				return false;
		}
		
		public void PrevBookmark ()
		{
			if (bookmarkBuffer != null)
				bookmarkBuffer.PrevBookmark ();
		}
		
		public void NextBookmark ()
		{
			if (bookmarkBuffer != null)
				bookmarkBuffer.NextBookmark ();
		}
		
		public void ClearBookmarks ()
		{
			if (bookmarkBuffer != null)
				bookmarkBuffer.ClearBookmarks ();
		}
		
		public IClipboardHandler ClipboardHandler {
			get { return clipboardHandler; }
		}
		
		public void Undo()
		{
			textBuffer.Undo ();
		}
		
		public void Redo()
		{
			textBuffer.Redo ();
		}
		
		public void BeginAtomicUndo ()
		{
			textBuffer.BeginAtomicUndo ();
		}
		
		public void EndAtomicUndo ()
		{
			textBuffer.EndAtomicUndo ();
		}
		
		public string SelectedText {
			get { return textBuffer.SelectedText; } 
			set { textBuffer.SelectedText = value; }
		}
		
		public event EventHandler<TextChangedEventArgs> TextChanged {
			add { textBuffer.TextChanged += value; }
			remove { textBuffer.TextChanged -= value; }
		}
		
		public int CursorPosition { 
			get { return textBuffer.CursorPosition; }
			set { textBuffer.CursorPosition = value; }
		}

		public int SelectionStartPosition {
			get { return textBuffer.SelectionStartPosition; }
		}
		
		public int SelectionEndPosition {
			get { return textBuffer.SelectionEndPosition; }
		}
		
		public int CursorLine {
			get {
				int line, column;
				textBuffer.GetLineColumnFromPosition (textBuffer.CursorPosition, out line, out column);
				return line;
			}
			set {
				textBuffer.CursorPosition = textBuffer.GetPositionFromLineColumn (value, CursorColumn);
			}
		}
		
		public int CursorColumn {
			get {
				int line, column;
				textBuffer.GetLineColumnFromPosition (textBuffer.CursorPosition, out line, out column);
				return column;
			}
			set {
				textBuffer.CursorPosition = textBuffer.GetPositionFromLineColumn (CursorLine, value);
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			textBuffer.Select (startPosition, endPosition);
		}
		
		public void ShowPosition (int position)
		{
			textBuffer.ShowPosition (position);
		}
		
		public void ShowCursorPosition ()
		{
			textBuffer.ShowPosition (textBuffer.CursorPosition);
		}
		
		public string Text {
			get { return textBuffer.Text; }
		}
		
		public int TextLength {
			get { return textBuffer.Length; }
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return textBuffer.GetText (startPosition, endPosition);
		}
		
		public char GetCharAt (int position)
		{
			return textBuffer.GetCharAt (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return textBuffer.GetPositionFromLineColumn (line, column);
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			textBuffer.GetLineColumnFromPosition (position, out line, out column);
		}
		
		public int InsertText (int position, string text)
		{
			return textBuffer.InsertText (position, text);
		}
		
		public void DeleteText (int position, int length)
		{
			textBuffer.DeleteText (position, length);
		}
		
		public void DeleteLine (int line)
		{
			int pos1 = GetPositionFromLineColumn (line, 1);
			int pos2 = GetPositionFromLineColumn (line + 1, 1);
			if (pos2 == -1)
				DeleteText (pos1, TextLength - pos1);
			else
				DeleteText (pos1, pos2 - pos1);
		}
		
		public void JumpTo (int line, int col)
		{
			if (textBuffer != null)
				textBuffer.SetCaretTo (line, col);
			else {
				int pos = GetPositionFromLineColumn (line, col);
				CursorPosition = pos;
			}
		}
		
		public string SourceEncoding {
			get {
				if (encodedTextContent != null)
					return encodedTextContent.SourceEncoding;
				else
					return null;
			}
		}
		
		public int LineCount {
			get {
				int line, column;
				GetLineColumnFromPosition (this.TextLength - 1, out line, out column);
				return line + 1;
			}
		}
		
		public string NewLine
		{
			get {
				if (newLine == null) {
					int pos1 = GetPositionFromLineColumn (1, 1);
					if (pos1 > 1)
						newLine = GetCharAt (pos1 - 1).ToString ();
					if (pos1 > 2) {
						char c1 = GetCharAt (pos1 - 2);
						if (c1 == '\n' || c1 == '\r')
							newLine = c1 + newLine;
					}
					if (newLine == null)
						newLine = Environment.NewLine;
				}
				return newLine;
			}
		}
		
		public int GetLineLength (int line)
		{
			int pos1 = GetPositionFromLineColumn (line, 1);
			int pos2 = GetPositionFromLineColumn (line + 1, 1);
			if (pos2 == -1)
				return TextLength - pos1;
			pos2--;
			while (pos2 >= pos1 && (GetCharAt (pos2) == '\n' || GetCharAt (pos2) == '\r'))
				pos2--;
			return pos2 - pos1 + 1;
		}
		
		public string GetLineText (int line)
		{
			int len = GetLineLength (line);
			int pos = GetPositionFromLineColumn (line, 1);
			return GetText (pos, pos + len);
		}
		
		public void ReplaceLine (int line, string txt)
		{
			int len = GetLineLength (line);
			int pos = GetPositionFromLineColumn (line, 1);
			textBuffer.DeleteText (pos, len);
			textBuffer.InsertText (pos, txt);
		}
		
		public int GetClosingBraceForLine (int line, out int openingLine)
		{
			int offset = textBuffer.GetPositionFromLineColumn (line, 1);
			offset = MonoDevelop.Projects.Gui.Completion.TextUtilities.SearchBracketBackward (completionWidget, offset, '{', '}');
			if (offset == -1) {
				openingLine = -1;
				return -1;
			}
				
			int col;
			textBuffer.GetLineColumnFromPosition (offset, out openingLine, out col);
			return offset;
		}
		
		public static char GetMatchingBrace (char ch)
		{
			switch (ch) {
			case '(':
				return ')';
			case ')':
				return '(';
			case '{':
				return '}';
			case '}':
				return '{';
			case '[':
				return ']';
			case ']':
				return '[';
			}
			throw new System.ArgumentException (ch.ToString (), "ch");
		}
		public static bool IsBrace (char ch)
		{
			return ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']';
		}
		public static bool IsOpenBrace (char ch)
		{
			return ch == '(' || ch == '{' || ch == '[';
		}

		public int SearchBracketForward (int offset, char openBracket, char closingBracket)
		{
			return MonoDevelop.Projects.Gui.Completion.TextUtilities.SearchBracketForward (completionWidget, offset, openBracket, closingBracket);
		}
		
		public int SearchBracketBackward (int offset, char openBracket, char closingBracket)
		{
			return MonoDevelop.Projects.Gui.Completion.TextUtilities.SearchBracketBackward (completionWidget, offset, openBracket, closingBracket);
		}

		public CodeCompletionContext CurrentCodeCompletionContext {
			get {
				if (completionWidget == null)
					return null;
				return completionWidget.CreateCodeCompletionContext (this.CursorPosition);
			}
		}
		
		public int SearchChar (int startPos, char searchChar)
		{
			bool isInString = false, isInChar = false;
			bool isInLineComment  = false, isInBlockComment = false;
			
			for (int pos = startPos; pos < TextLength; pos++) {
				char ch = GetCharAt (pos);
				switch (ch) {
					case '\r':
					case '\n':
						isInLineComment = false;
						break;
					case '/':
						if (isInBlockComment) {
							if (pos > 0 && GetCharAt (pos - 1) == '*') 
								isInBlockComment = false;
						} else  if (!isInString && !isInChar && pos + 1 < TextLength) {
							char nextChar = GetCharAt (pos + 1);
							if (nextChar == '/')
								isInLineComment = true;
							if (!isInLineComment && nextChar == '*')
								isInBlockComment = true;
						}
						break;
					case '"':
						if (!(isInChar || isInLineComment || isInBlockComment)) 
							isInString = !isInString;
						break;
					case '\'':
						if (!(isInString || isInLineComment || isInBlockComment)) 
							isInChar = !isInChar;
						break;
					default :
						if (ch == searchChar) {
							if (!(isInString || isInChar || isInLineComment || isInBlockComment))
								return pos;
						}
						break;
				}
			}
			return -1;
		}
		
		public void GotoMatchingBrace ()
		{
			int offset = textBuffer.CursorPosition;
			if (!IsBrace (textBuffer.GetCharAt (offset)))
				offset--;
			
			if (offset >= 0 && IsBrace (textBuffer.GetCharAt (offset))) {
				char open = textBuffer.GetCharAt (offset);
				char close = open;
				bool searchForward = IsOpenBrace (open);
				if (searchForward) {
					close = GetMatchingBrace (open);
					offset = SearchBracketForward (offset + 1, open, close);
				} else {
					open  = GetMatchingBrace (open);
					offset = SearchBracketBackward (offset - 1, open, close);
				}
				if (offset >= 0)
					textBuffer.CursorPosition = offset;
			}
		}
	}
}
