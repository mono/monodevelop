// EditActions.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;
using System.Linq;
using System.Threading;

namespace MonoDevelop.SourceEditor
{
	class TabAction
	{
		ExtensibleTextEditor editor;
		
		public TabAction (ExtensibleTextEditor editor)
		{
			this.editor = editor;
		}
		
		public void Action (TextEditorData data)
		{
			if (!editor.DoInsertTemplate ())
				MiscActions.InsertTab (data);
		}
	}
	
	static class EditActions
	{
		
		public static void AdvancedBackspace (TextEditorData data)
		{
			RemoveCharBeforCaret (data);
			//DeleteActions.Backspace (data, RemoveCharBeforCaret);
		}
		
		const string open    = "'\"([{<";
		const string closing = "'\")]}>";
		
		static int GetNextNonWsCharOffset (TextEditorData data, int offset)
		{
			int result = offset;
			if (result >= data.Document.Length)
				return -1;
			while (Char.IsWhiteSpace (data.Document.GetCharAt (result))) {
				result++;
				if (result >= data.Document.Length)
					return -1;
			}
			return result;
		}
		
		static async void RemoveCharBeforCaret (TextEditorData data)
		{
			if (!data.IsSomethingSelected && MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket) {
				if (data.Caret.Offset > 0) {
					var stack = await data.Document.SyntaxMode.GetScopeStackAsync (data.Caret.Offset, CancellationToken.None);
					if (stack.Any (s => s.Contains ("string"))) {
						DeleteActions.Backspace (data);
						return;
					}
					
					char ch = data.Document.GetCharAt (data.Caret.Offset - 1);
					int idx = open.IndexOf (ch);
					
					if (idx >= 0) {
						int nextCharOffset = GetNextNonWsCharOffset (data, data.Caret.Offset);
						if (nextCharOffset >= 0 && closing[idx] == data.Document.GetCharAt (nextCharOffset)) {
							data.Remove (data.Caret.Offset, nextCharOffset - data.Caret.Offset + 1);
						}
					}
				}
			}
			DeleteActions.Backspace (data);
		}
	}
}
