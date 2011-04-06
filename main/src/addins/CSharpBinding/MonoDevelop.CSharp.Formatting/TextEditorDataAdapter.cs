// 
// TextEditorDataAdapter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Formatting
{
	public class TextEditorDataAdapter : ITextEditorAdapter
	{
		TextEditorData data;
		
		public TextEditorDataAdapter (TextEditorData data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			this.data = data;
		}

		#region ITextEditorAdapter implementation
		public int LocationToOffset (int line, int col)
		{
			return data.LocationToOffset (line, col);
		}

		public char GetCharAt (int offset)
		{
			return data.GetCharAt (offset);
		}

		public string GetTextAt (int offset, int length)
		{
			return data.GetTextAt (offset, length);
		}

		public int GetEditableLength (int lineNumber)
		{
			var line = data.GetLine (lineNumber);
			if (line == null)
				return -1;
			return line.EditableLength;
		}

		public string GetIndentation (int lineNumber)
		{
			return data.GetLineIndent (lineNumber);
		}

		public int GetLineOffset (int lineNumber)
		{
			var line = data.GetLine (lineNumber);
			if (line == null)
				return -1;
			return line.Offset;
		}

		public int GetLineLength (int lineNumber)
		{
			var line = data.GetLine (lineNumber);
			if (line == null)
				return -1;
			return line.Length;
		}

		public int GetLineEndOffset (int lineNumber)
		{
			var line = data.GetLine (lineNumber);
			if (line == null)
				return -1;
			return line.EndOffset;
		}

		public void Replace (int offset, int count, string text)
		{
			data.Replace (offset, count, text);
		}

		public bool TabsToSpaces {
			get {
				return data.Options.TabsToSpaces;
			}
		}

		public int TabSize {
			get {
				return data.Options.TabSize;
			}
		}

		public string EolMarker {
			get {
				return data.EolMarker;
			}
		}

		public string Text {
			get {
				return data.Text;
			}
		}

		public int Length {
			get {
				return data.Length;
			}
		}

		public int LineCount {
			get {
				return data.LineCount;
			}
		}
		#endregion
	}
}

