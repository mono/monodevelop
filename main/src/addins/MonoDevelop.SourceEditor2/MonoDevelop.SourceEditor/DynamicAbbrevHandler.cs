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

using System;
using System.Linq;
using MonoDevelop.Refactoring;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;

namespace MonoDevelop.SourceEditor
{
	public class DynamicAbbrevHandler : CommandHandler
	{
		enum AbbrevState {
			SearchBackward,
			SearchForward,
			SearchOtherBuffers,
			CycleThroughFoundWords
		}
		
		static SourceEditorView lastView = null;
		static string           lastAbbrev = null;
		static int              lastTriggerOffset = 0;
		static int              lastInsertPos = 0;
		static List<string>     foundWords = new List<string> ();
		static int              lastStartOffset = 0;
		static AbbrevState      curState;
		
		protected override void Run (object data)
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			SourceEditorView view = IdeApp.Workbench.ActiveDocument.GetContent<SourceEditorView> ();
			if (view == null)
				return;

			string abbrevWord;
			int offset;
			int startOffset;

			if (lastView == view && view.TextEditor.Caret.Offset == lastTriggerOffset) {
				abbrevWord = lastAbbrev;
				offset = lastStartOffset;
			} else {
				abbrevWord = view.TextEditor.GetWordBeforeCaret ();
				lastAbbrev = abbrevWord;
				offset = view.TextEditor.Caret.Offset - abbrevWord.Length - 1;
				lastInsertPos = lastTriggerOffset = offset + 1;
				foundWords.Clear ();
				curState = AbbrevState.SearchBackward;
			}

			lastView = view;
			switch (curState) {
			case AbbrevState.SearchBackward:
				while (offset > 0) {
					if (IsMatchAt (view, offset, abbrevWord)) {
						int endOffset = SearchEndPos (offset, view);
						string curWord = view.TextEditor.Document.GetTextBetween (offset, endOffset);
						if (foundWords.Contains (curWord)) {
							offset--;
							continue;
						}
						foundWords.Add (curWord);
						ReplaceWord (view, curWord);
						lastStartOffset = offset - 1;
						return;
					}
					offset--;
				}
				offset = view.TextEditor.Caret.Offset;
				curState = AbbrevState.SearchForward;
				goto case AbbrevState.SearchForward;
			case AbbrevState.SearchForward:
				while (offset < view.TextEditor.Document.Length) {
					if (IsMatchAt (view, offset, abbrevWord)) {
						int endOffset = SearchEndPos (offset, view);
						string curWord = view.TextEditor.Document.GetTextBetween (offset, endOffset);
						if (foundWords.Contains (curWord)) {
							offset++;
							continue;
						}
						foundWords.Add (curWord);
						ReplaceWord (view, curWord);
						lastStartOffset = offset + 1;
						return;
					}
					offset++;
				}
				curState = AbbrevState.SearchOtherBuffers;
				goto case AbbrevState.SearchOtherBuffers;
			case AbbrevState.SearchOtherBuffers:
				foreach (Document curDoc in IdeApp.Workbench.Documents.Where (d => d != doc)) {
					SourceEditorView otherView = curDoc.GetContent<SourceEditorView> ();
					if (otherView == null)
						continue;
					for (int i = 0; i < otherView.Document.Length; i++) {
						if (IsMatchAt (otherView, i, abbrevWord)) {
							int endOffset = SearchEndPos (i, otherView);
							string curWord = otherView.TextEditor.Document.GetTextBetween (i, endOffset);
							if (foundWords.Contains (curWord)) 
								continue;
							foundWords.Add (curWord);
						}
					}
				}
				curState = AbbrevState.SearchForward;
				goto case AbbrevState.CycleThroughFoundWords;
			case AbbrevState.CycleThroughFoundWords:
				int index = foundWords.IndexOf (view.TextEditor.Document.GetTextAt (lastInsertPos, view.TextEditor.Caret.Offset - lastInsertPos));
				if (index < 0)
					break;
				startOffset = offset;
				offset = startOffset + foundWords[index].Length;
				index = (index + foundWords.Count + 1) % foundWords.Count;
				ReplaceWord (view, foundWords[index]);
				break;
			}
			
		}
		
		static void ReplaceWord (MonoDevelop.SourceEditor.SourceEditorView view, string curWord)
		{
			view.TextEditor.Replace (lastInsertPos, view.TextEditor.Caret.Offset - lastInsertPos, curWord);
			view.TextEditor.Document.CommitLineUpdate (view.TextEditor.Caret.Line);
			lastTriggerOffset = view.TextEditor.Caret.Offset = lastInsertPos + curWord.Length;
		}
		
		static int SearchEndPos (int offset, MonoDevelop.SourceEditor.SourceEditorView view)
		{
			while (offset < view.TextEditor.Document.Length && view.TextEditor.Document.GetCharAt (offset).IsIdentifierPart ()) {
				offset++;
			}
			return offset;
		}
		
		static bool IsMatchAt (MonoDevelop.SourceEditor.SourceEditorView view, int offset, string abbrevWord)
		{
			if (offset + abbrevWord.Length >= view.TextEditor.Document.Length)
				return false;
			if (offset > 0 && view.TextEditor.Document.GetCharAt (offset - 1).IsIdentifierPart ())
				return false;
			if (offset + abbrevWord.Length < view.TextEditor.Document.Length && !view.TextEditor.Document.GetCharAt (offset + abbrevWord.Length).IsIdentifierPart ())
				return false;
			return view.TextEditor.Document.GetTextAt (offset, abbrevWord.Length) == abbrevWord;
		}
	}
}
 