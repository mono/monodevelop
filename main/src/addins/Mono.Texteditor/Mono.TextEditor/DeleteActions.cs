// 
// DeleteActions.cs
// 
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Text;

using Gtk;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	
	
	public static class DeleteActions
	{
		public static void PreviousWord (TextEditorData data)
		{
			int oldLine = data.Caret.Line;
			int offset = data.FindPrevWordOffset (data.Caret.Offset);
			if (data.Caret.Offset != offset && data.CanEdit (oldLine) && data.CanEdit (data.Caret.Line)) {
				data.Document.Remove (offset, data.Caret.Offset - offset);
				data.Caret.Offset = offset;
				if (oldLine != data.Caret.Line)
					data.Document.CommitLineToEndUpdate (data.Caret.Line);
			}
		}
		
		public static void NextWord (TextEditorData data)
		{
			int oldLine = data.Caret.Line;
			int offset = data.FindNextWordOffset (data.Caret.Offset);
			if (data.Caret.Offset != offset && data.CanEdit (oldLine) && data.CanEdit (data.Caret.Line))  {
				data.Document.Remove (data.Caret.Offset, offset - data.Caret.Offset);
				data.Document.CommitLineToEndUpdate (data.Caret.Line);
			}
		}
		
		public static void CaretLine (TextEditorData data)
		{
			if (data.Document.LineCount <= 1 || !data.CanEdit (data.Caret.Line))
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			data.Document.Remove (line.Offset, line.Length);
			data.Document.CommitLineToEndUpdate (data.Caret.Line);
		}
		
		public static void CaretLineToEnd (TextEditorData data)
		{
			if (!data.CanEdit (data.Caret.Line))
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (data.Caret.Column == line.EditableLength) {
				// Nothing after the cursor, delete the end-of-line sequence
				data.Document.Remove (line.Offset + data.Caret.Column, line.Length - data.Caret.Column);
			} else {
				// Delete from cursor position to the end of the line
				data.Document.Remove (line.Offset + data.Caret.Column, line.EditableLength - data.Caret.Column);
			}
			data.Document.CommitLineUpdate (data.Caret.Line);
		}
		
		public static void Backspace (TextEditorData data)
		{
			Backspace (data, RemoveCharBeforeCaret);
		}
		
		public static void Backspace (TextEditorData data, Action<TextEditorData> removeCharBeforeCaret)
		{
			if (!data.CanEditSelection)
				return;
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
				return;
			}
			if (data.Caret.Offset == 0)
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (data.Caret.Offset == line.Offset) {
				LineSegment lineAbove = data.Document.GetLine (data.Caret.Line - 1);
				data.Caret.Location = new DocumentLocation (data.Caret.Line - 1, lineAbove.EditableLength);
				data.Document.Remove (lineAbove.EndOffset - lineAbove.DelimiterLength, lineAbove.DelimiterLength);
			} else {
				removeCharBeforeCaret (data);
			}
		}
		
		public static void RemoveCharBeforeCaret (TextEditorData data)
		{
			data.Document.Remove (data.Caret.Offset - 1, 1);
			data.Caret.Column--;
		}
		
		public static void Delete (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
				return;
			}
			if (data.Caret.Offset >= data.Document.Length)
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (data.Caret.Column == line.EditableLength) {
				if (data.Caret.Line < data.Document.LineCount) { 
					data.Document.Remove (line.EndOffset - line.DelimiterLength, line.DelimiterLength);
					if (line.EndOffset == data.Document.Length)
						line.DelimiterLength = 0;
				}
			} else {
				data.Document.Remove (data.Caret.Offset, 1); 
				data.Document.CommitLineUpdate (data.Caret.Line);
			}
		}
	}
}
