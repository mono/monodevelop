// 
// ViActions.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.TextEditor.Vi
{
	
	
	public static class ViActions
	{
		public static void MoveToNextEmptyLine (TextEditorData data)
		{
			if (data.Caret.Line == data.Document.LineCount) {
				data.Caret.Offset = data.Document.TextLength;
				return;
			}
			
			int line = data.Caret.Line + 1;
			DocumentLine currentLine = data.Document.GetLine (line);
			while (line <= data.Document.LineCount) {
				line++;
				DocumentLine nextLine = data.Document.GetLine (line);
				if (currentLine.Length != 0 && nextLine.Length == 0) {
					data.Caret.Offset = nextLine.Offset;
					return;
				}
				currentLine = nextLine;
			}
			
			data.Caret.Offset = currentLine.Offset;
		}
		
		public static void MoveToPreviousEmptyLine (TextEditorData data)
		{
			if (data.Caret.Line == DocumentLocation.MinLine) {
				data.Caret.Offset = 0;
				return;
			}
			
			int line = data.Caret.Line - 1;
			DocumentLine currentLine = data.Document.GetLine (line);
			while (line > DocumentLocation.MinLine) {
				line--;
				DocumentLine previousLine = data.Document.GetLine (line);
				if (currentLine.Length != 0 && previousLine.Length == 0) {
					data.Caret.Offset = previousLine.Offset;
					return;
				}
				currentLine = previousLine;
			}
			
			data.Caret.Offset = currentLine.Offset;
		}
		
		public static void NewLineBelow (TextEditorData data)
		{
			DocumentLine currentLine = data.Document.GetLine (data.Caret.Line);
			data.Caret.Offset = currentLine.Offset + currentLine.Length;
			MiscActions.InsertNewLine (data);
		}
		
		public static void NewLineAbove (TextEditorData data)
		{
			if (data.Caret.Line == DocumentLocation.MinLine ) {
				data.Caret.Offset = 0;
				MiscActions.InsertNewLine (data);
				data.Caret.Offset = 0;
				return;
			}
			
			DocumentLine currentLine = data.Document.GetLine (data.Caret.Line - 1);
			data.Caret.Offset = currentLine.Offset + currentLine.Length;
			MiscActions.InsertNewLine (data);
		}
		
		public static void Join (TextEditorData data)
		{
			int startLine, endLine, startOffset, length;
			
			if (data.IsSomethingSelected) {
				startLine = data.Document.OffsetToLineNumber (data.SelectionRange.Offset);
				endLine = data.Document.OffsetToLineNumber (data.SelectionRange.EndOffset - 1);
			} else {
				startLine = endLine = data.Caret.Line;
			}
			
			//single-line joins
			if (endLine == startLine)
				endLine++;
			
			if (endLine > data.Document.LineCount)
				return;
			
			DocumentLine seg = data.Document.GetLine (startLine);
			startOffset = seg.Offset;
			StringBuilder sb = new StringBuilder (data.Document.GetTextAt (seg).TrimEnd ());
			//lastSpaceOffset = startOffset + sb.Length;
			
			for (int i = startLine + 1; i <= endLine; i++) {
				seg = data.Document.GetLine (i);
				//lastSpaceOffset = startOffset + sb.Length;
				sb.Append (" ");
				sb.Append (data.Document.GetTextAt (seg).Trim ());
			}
			length = (seg.Offset - startOffset) + seg.Length;
			// TODO: handle conversion issues ? 
			data.Replace (startOffset, length, sb.ToString ());
		}
		
		public static void ToggleCase (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				if (!data.CanEditSelection)
					return;
				
				StringBuilder sb = new StringBuilder (data.SelectedText);
				for (int i = 0; i < sb.Length; i++) {
					char ch = sb [i];
					if (Char.IsLower (ch))
						sb [i] = Char.ToUpper (ch);
					else if (Char.IsUpper (ch))
						sb [i] = Char.ToLower (ch);
				}
				data.Replace (data.SelectionRange.Offset, data.SelectionRange.Length, sb.ToString ());
			} else if (data.CanEdit (data.Caret.Line)) {
				char ch = data.Document.GetCharAt (data.Caret.Offset);
				if (Char.IsLower (ch))
					ch = Char.ToUpper (ch);
				else if (Char.IsUpper (ch))
					ch = Char.ToLower (ch);
				var caretOffset = data.Caret.Offset;
				int length = data.Replace (caretOffset, 1, new string (ch, 1));
				DocumentLine seg = data.Document.GetLine (data.Caret.Line);
				if (data.Caret.Column < seg.Length)
					data.Caret.Offset = caretOffset + length;
			}
		}
		
		public static void Right (TextEditorData data)
		{
			DocumentLine segment = data.Document.GetLine (data.Caret.Line);
			if (segment.EndOffsetIncludingDelimiter-1 > data.Caret.Offset) {
				CaretMoveActions.Right (data);
				RetreatFromLineEnd (data);
			}
		}
		
		public static void Left (TextEditorData data)
		{
			if (DocumentLocation.MinColumn < data.Caret.Column) {
				CaretMoveActions.Left (data);
			}
		}
		
		public static void Down (TextEditorData data)
		{
			int desiredColumn = System.Math.Max (data.Caret.Column, data.Caret.DesiredColumn);
			
			CaretMoveActions.Down (data);
			RetreatFromLineEnd (data);
			
			data.Caret.DesiredColumn = desiredColumn;
		}
		
		public static void Up (TextEditorData data)
		{
			int desiredColumn = System.Math.Max (data.Caret.Column, data.Caret.DesiredColumn);
			
			CaretMoveActions.Up (data);
			RetreatFromLineEnd (data);
			
			data.Caret.DesiredColumn = desiredColumn;
		}
		
		public static void WordEnd (TextEditorData data)
		{
			data.Caret.Offset = data.FindCurrentWordEnd (data.Caret.Offset);
		}
		
		public static void WordStart (TextEditorData data)
		{
			data.Caret.Offset = data.FindCurrentWordStart (data.Caret.Offset);
		}

		public static void InnerWord (TextEditorData data)
		{
			var start = data.FindCurrentWordStart (data.Caret.Offset);
			var end = data.FindCurrentWordEnd (data.Caret.Offset);
			data.SelectionRange = new TextSegment(start, end - start);
		}

		private static readonly Dictionary<char, char> EndToBeginCharMap = new Dictionary<char, char>
		{
			{')', '('},
			{'}', '{'},
			{']', '['},
			{'>', '<'},
		};
		private static readonly Dictionary<char, char> BeginToEndCharMap = new Dictionary<char, char>
		{
			{'(', ')'},
			{'{', '}'},
			{'[', ']'},
			{'<', '>'},
		};

		public static Action<TextEditorData> OuterSymbol (char command) 
		{
			return data =>
			{
				SymbolBlock result;
				if (!TryFindSymbolBlock (data, command, out result)) return;

				data.SelectionRange = result.GetOuterTextSegment ();
			};
		}

		public static Action<TextEditorData> InnerSymbol (char command) 
		{
			return data =>
			{
				SymbolBlock result;
				if (!TryFindSymbolBlock (data, command, out result)) return;

				data.SelectionRange = result.GetInnerTextSegment ();
			};
		}

		struct SymbolBlock
		{
			public int StartOffset, EndOffset;
			public DocumentLine StartLine, EndLine;
			bool IsSameLine { get { return StartLine == EndLine; } }

			public TextSegment GetInnerTextSegment()
			{
				var length = IsSameLine ? EndOffset - StartOffset : EndLine.PreviousLine.EndOffset - StartOffset;
				return new TextSegment (StartOffset + 1, length - 1);
			}

			public TextSegment GetOuterTextSegment ()
			{
				return new TextSegment (StartOffset, (EndOffset - StartOffset) + 1);
			}
		}

		static bool TryFindSymbolBlock (TextEditorData data, char command, out SymbolBlock result)
		{
			char end, begin;
			if (!BeginToEndCharMap.TryGetValue (command, out end)) end = command;
			if (!EndToBeginCharMap.TryGetValue (command, out begin)) begin = command;

			var offset = data.Caret.Offset;

			var startTokenOffset = ParseForChar(data, offset, 0, end, begin, false);
			var endTokenOffset = ParseForChar(data, offset, data.Length, begin, end, true);

			// Use the editor's FindMatchingBrace built-in functionality. It's better at handling erroneous braces
			// inside quotes. We still need to do the above paragraph because we needed to find the braces.
			var matchingStartBrace = endTokenOffset.HasValue ? data.Document.GetMatchingBracketOffset(
				endTokenOffset.GetValueOrDefault ()) : -1;
			if (matchingStartBrace >= 0 && (!startTokenOffset.HasValue 
			                                || matchingStartBrace != startTokenOffset.GetValueOrDefault ()))
				startTokenOffset = matchingStartBrace;

			var matchingEndBrace = startTokenOffset.HasValue && data.GetCharAt (offset) != end ? 
				data.Document.GetMatchingBracketOffset(startTokenOffset.GetValueOrDefault ()) : -1;
			if (matchingEndBrace >= 0 && (!endTokenOffset.HasValue 
			                              || matchingEndBrace != endTokenOffset.GetValueOrDefault ()))
				endTokenOffset = matchingEndBrace;

			if (!startTokenOffset.HasValue || !endTokenOffset.HasValue) throw new ViModeAbortException();

			result = new SymbolBlock 
			{ 
				StartOffset = startTokenOffset.GetValueOrDefault (),
				EndOffset = endTokenOffset.GetValueOrDefault (),
				StartLine = data.GetLineByOffset (startTokenOffset.GetValueOrDefault()),
				EndLine = data.GetLineByOffset (endTokenOffset.GetValueOrDefault()),
			};
			return true;
		}

		static int? ParseForChar(TextEditorData data, int fromOffset, int toOffset, char oppositeToken, char findToken, bool forward)
		{
			int increment = forward ? 1 : -1;
			var symbolCount = 0;
			for (int i = fromOffset; forward && i < toOffset || !forward && i >= toOffset; i += increment)
			{
				var c = data.GetCharAt(i);
				if (c == oppositeToken) 
					symbolCount++;
				else if (c == findToken)
				{
					if (symbolCount == 0) return i;
					symbolCount--;
				}
			}
			return null;
		}

		public static Action<TextEditorData> InnerQuote (char c)
		{
			return data => 
			{
				int beginOffset, length;
				if (TryFindInnerQuote (data, c, out beginOffset, out length))
					data.SelectionRange = new TextSegment (beginOffset, length);
			};
		}

		static bool TryFindInnerQuote (TextEditorData data, char c, out int begin, out int length)
		{
			begin = 0;
			length = 0;
			var currentOffset = data.Caret.Offset;
			var lineText = data.Document.GetLineText (data.Caret.Line);
			var line = data.Document.GetLine (data.Caret.Line);
			var lineOffset = currentOffset - line.Offset;

			var beginOffset = ParseForQuote (lineText, lineOffset - 1, c, false);
			if (!beginOffset.HasValue && lineText[lineOffset] == c)
				beginOffset = lineOffset;
			if (!beginOffset.HasValue) return false;
			var startEndSearchAt = beginOffset.GetValueOrDefault () == lineOffset ? lineOffset + 1 : lineOffset;
			var endOffset = ParseForQuote (lineText, startEndSearchAt, c, true);
			if (!endOffset.HasValue) return false;

			begin = beginOffset.GetValueOrDefault () + line.Offset + 1;
			length = endOffset.GetValueOrDefault () - beginOffset.GetValueOrDefault () - 1;
			return true;
		}

		public static Action<TextEditorData> OuterQuote (char c)
		{
			return data => 
			{
				int beginOffset, length;
				if (TryFindInnerQuote (data, c, out beginOffset, out length))
				{
					beginOffset--;
					length += 2;
					data.SelectionRange = new TextSegment (beginOffset, length);
				}
			};
		}

		static int? ParseForQuote (string text, int start, char charToFind, bool forward) 
		{
			int increment = forward ? 1 : -1;
			for (int i = start; forward && i < text.Length || !forward && i >= 0; i += increment)
			{
				if (text[i] == charToFind &&
					(i < 1 || text[i-1] != '\\') &&
					(i < 2 || text[i-2] != '\\'))
					return i;
			}
			return null;
		}

		public static void LineEnd (TextEditorData data)
		{
			int desiredColumn = System.Math.Max (data.Caret.Column, data.Caret.DesiredColumn);
			
			CaretMoveActions.LineEnd (data);
			RetreatFromLineEnd (data);
			
			data.Caret.DesiredColumn = desiredColumn;
		}
		
		internal static bool IsEol (char c)
		{
			return (c == '\r' || c == '\n');
		}
		
		internal static void RetreatFromLineEnd (TextEditorData data)
		{
			if (data.Caret.Mode == CaretMode.Block && !data.IsSomethingSelected && !data.Caret.PreserveSelection) {
				while (DocumentLocation.MinColumn < data.Caret.Column && (data.Caret.Offset >= data.Document.TextLength
				                                 || IsEol (data.Document.GetCharAt (data.Caret.Offset)))) {
					Left (data);
				}
			}
		}
		
		public static Action<TextEditorData> VisualSelectionFromMoveAction (Action<TextEditorData> moveAction)
		{
			return delegate (TextEditorData data) {
				//get info about the old selection state
				DocumentLocation oldCaret = data.Caret.Location, oldAnchor = oldCaret, oldLead = oldCaret;
				if (data.MainSelection != null) {
					oldLead = data.MainSelection.Lead;
					oldAnchor = data.MainSelection.Anchor;
				}
				
				//do the action, preserving selection
				SelectionActions.StartSelection (data);
				moveAction (data);
				SelectionActions.EndSelection (data);
				
				DocumentLocation newCaret = data.Caret.Location, newAnchor = newCaret, newLead = newCaret;
				if (data.MainSelection != null) {
					newLead = data.MainSelection.Lead;
					newAnchor = data.MainSelection.Anchor;
				}
				
				//Console.WriteLine ("oc{0}:{1} oa{2}:{3} ol{4}:{5}", oldCaret.Line, oldCaret.Column, oldAnchor.Line, oldAnchor.Column, oldLead.Line, oldLead.Column);
				//Console.WriteLine ("nc{0}:{1} na{2}:{3} nl{4}:{5}", newCaret.Line, newCaret.Line, newAnchor.Line, newAnchor.Column, newLead.Line, newLead.Column);
				
				//pivot the anchor around the anchor character
				if (oldAnchor < oldLead && newAnchor >= newLead) {
					data.SetSelection (new DocumentLocation (newAnchor.Line, newAnchor.Column + 1), newLead);
				} else if (oldAnchor > oldLead && newAnchor <= newLead) {
					data.SetSelection (new DocumentLocation (newAnchor.Line, newAnchor.Column - 1), newLead);
				}
				
				//pivot the lead about the anchor character
				if (newAnchor == newLead) {
					if (oldAnchor < oldLead)
						SelectionActions.FromMoveAction (Left) (data);
					else
						SelectionActions.FromMoveAction (Right) (data);
				}
				//pivot around the anchor line
				else {
					if (oldAnchor < oldLead && newAnchor > newLead && (
							(newLead.Line == newAnchor.Line && oldLead.Line == oldAnchor.Line + 1) ||
						    (newLead.Line == newAnchor.Line - 1 && oldLead.Line == oldAnchor.Line)))
						SelectionActions.FromMoveAction (Left) (data);
					else if (oldAnchor > oldLead && newAnchor < newLead && (
							(newLead.Line == newAnchor.Line && oldLead.Line == oldAnchor.Line - 1) ||
							(newLead.Line == newAnchor.Line + 1 && oldLead.Line == oldAnchor.Line)))
						SelectionActions.FromMoveAction (Right) (data);
				}
			};
		}
	}
}
