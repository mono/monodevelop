//
// InsertionPoint.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor
{
	public enum NewLineInsertion
	{
		None,
		Eol,
		BlankLine
	}

	public class InsertionPoint 
	{
		public TextLocation Location {
			get;
			set;
		}

		public NewLineInsertion LineBefore { get; set; }
		public NewLineInsertion LineAfter { get; set; }

		public InsertionPoint (TextLocation location, NewLineInsertion lineBefore, NewLineInsertion lineAfter)
		{
			this.Location = location;
			this.LineBefore = lineBefore;
			this.LineAfter = lineAfter;
		}

		public override string ToString ()
		{
			return string.Format ("[InsertionPoint: Location={0}, LineBefore={1}, LineAfter={2}]", Location, LineBefore, LineAfter);
		}

		public void InsertNewLine (ITextEditor editor, NewLineInsertion insertion, ref int offset)
		{
			string str = null;
			switch (insertion) {
			case NewLineInsertion.Eol:
				str = editor.EolMarker;
				break;
			case NewLineInsertion.BlankLine:
				str = editor.EolMarker + editor.EolMarker;
				break;
			default:
				return;
		}

			offset += editor.Insert (offset, str);
		}

		public int Insert (ITextEditor editor, string text)
		{
			int offset = editor.LocationToOffset (Location);
			using (var undo = editor.OpenUndoGroup ()) {

				// TODO: Run formatter !!!
				// text = editor.FormatString (Location, text);

				var line = editor.GetLineByOffset (offset);
				int insertionOffset = line.Offset + Location.Column - 1;
				offset = insertionOffset;
				InsertNewLine (editor, LineBefore, ref offset);
				int result = offset - insertionOffset;

				offset += editor.Insert (offset, text);
				InsertNewLine (editor, LineAfter, ref offset);
				return result;
			}
		}
	}
}

