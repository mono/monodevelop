// 
// DynamicAbbrevHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Ide;

namespace MonoDevelop.Ide.Editor
{
	class DynamicAbbrevHandler : CommandHandler
	{
		enum AbbrevState {
			SearchBackward,
			SearchForward,
			SearchOtherBuffers,
			CycleThroughFoundWords
		}
		
		static TextEditor       lastView = null;
		static string           lastAbbrev = null;
		static int              lastTriggerOffset = 0;
		static int              lastInsertPos = 0;
		static List<string>     foundWords = new List<string> ();
		static int              lastStartOffset = 0;
		static AbbrevState      curState;
		
		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			var editor = doc.Editor;
			if (editor == null)
				return;
			
			string abbrevWord;
			int offset;
			int startOffset;
			
			if (lastView == editor && editor.CaretOffset == lastTriggerOffset) {
				abbrevWord = lastAbbrev;
				offset = lastStartOffset;
			} else {
				abbrevWord = GetWordBeforeCaret (editor);
				lastAbbrev = abbrevWord;
				offset = editor.CaretOffset - abbrevWord.Length - 1;
				lastInsertPos = lastTriggerOffset = offset + 1;
				foundWords.Clear ();
				foundWords.Add (abbrevWord);
				curState = AbbrevState.SearchBackward;
			}
			
			lastView = editor;
			switch (curState) {
			case AbbrevState.SearchBackward:
				while (offset > 0) {
					if (IsMatchAt (editor, offset, abbrevWord)) {
						int endOffset = SearchEndPos (offset, editor);
						string curWord = editor.GetTextBetween (offset, endOffset);
						if (foundWords.Contains (curWord)) {
							offset--;
							continue;
						}
						foundWords.Add (curWord);
						ReplaceWord (editor, curWord);
						lastStartOffset = offset - 1;
						return;
					}
					offset--;
				}
				offset = editor.CaretOffset;
				curState = AbbrevState.SearchForward;
				goto case AbbrevState.SearchForward;
			case AbbrevState.SearchForward:
				while (offset < editor.Length) {
					if (IsMatchAt (editor, offset, abbrevWord)) {
						int endOffset = SearchEndPos (offset, editor);
						string curWord = editor.GetTextBetween (offset, endOffset);
						if (foundWords.Contains (curWord)) {
							offset++;
							continue;
						}
						foundWords.Add (curWord);
						ReplaceWord (editor, curWord);
						lastStartOffset = offset + 1;
						return;
					}
					offset++;
				}
				curState = AbbrevState.SearchOtherBuffers;
				goto case AbbrevState.SearchOtherBuffers;
			case AbbrevState.SearchOtherBuffers:
				foreach (Document curDoc in IdeApp.Workbench.Documents) {
					var otherView = curDoc.GetContent<TextEditor> ();
					if (curDoc == doc || otherView == null)
						continue;
					for (int i = 0; i < otherView.Length; i++) {
						if (IsMatchAt (otherView, i, abbrevWord)) {
							int endOffset = SearchEndPos (i, otherView);
							string curWord = otherView.GetTextBetween (i, endOffset);
							if (foundWords.Contains (curWord))
								continue;
							foundWords.Add (curWord);
						}
					}
				}
				curState = AbbrevState.CycleThroughFoundWords;
				goto case AbbrevState.CycleThroughFoundWords;
			case AbbrevState.CycleThroughFoundWords:
				int index = foundWords.IndexOf (editor.GetTextAt (lastInsertPos, editor.CaretOffset - lastInsertPos));
				if (index < 0)
					break;
				startOffset = offset;
				offset = startOffset + foundWords[index].Length;
				index = (index + foundWords.Count + 1) % foundWords.Count;
				ReplaceWord (editor, foundWords[index]);
				break;
			}
		}
		
		public static bool IsIdentifierPart (char ch)
		{
			return char.IsLetterOrDigit (ch) || ch == '_';
		}
		
		static string GetWordBeforeCaret (TextEditor editor)
		{
			int startOffset = editor.CaretOffset;
			int offset = startOffset - 1;
			while (offset > 0) {
				char ch = editor.GetCharAt (offset);
				if (!IsIdentifierPart (ch)) {
					offset++;
					break;
				}
				offset--;
			}
			if (offset >= startOffset)
				return "";
			return editor.GetTextBetween (offset, startOffset);
		}
		
		static void ReplaceWord (TextEditor editor, string curWord)
		{
			editor.ReplaceText (lastInsertPos, editor.CaretOffset - lastInsertPos, curWord);
			lastTriggerOffset = editor.CaretOffset;
		}
		
		static int SearchEndPos (int offset, TextEditor editor)
		{
			while (offset < editor.Length && IsIdentifierPart (editor.GetCharAt (offset))) {
				offset++;
			}
			return offset;
		}
		
		static bool IsMatchAt (TextEditor editor, int offset, string abbrevWord)
		{
			if (offset + abbrevWord.Length >= editor.Length)
				return false;
			if (offset > 0 && IsIdentifierPart (editor.GetCharAt (offset - 1)))
				return false;
			if (offset + abbrevWord.Length < editor.Length && !IsIdentifierPart (editor.GetCharAt (offset + abbrevWord.Length)))
				return false;
			return editor.GetTextAt (offset, abbrevWord.Length) == abbrevWord;
		}
	}
}