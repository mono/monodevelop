//
// ColoredSegment.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using Mono.TextEditor.Highlighting;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Mono.TextEditor.Utils
{
	class ClipboardColoredText
	{
		public ScopeStack ScopeStack { get; set; }
		public string Text { get; set; }

		public ClipboardColoredText (ColoredSegment chunk, TextDocument doc)
		{
			this.ScopeStack = chunk.ScopeStack;
			this.Text = doc.GetTextAt (chunk);
		}

		public static async Task<List<List<ClipboardColoredText>>> GetChunks (TextEditorData data, ISegment selectedSegment)
		{
			int startLineNumber = data.OffsetToLineNumber (selectedSegment.Offset);
			int endLineNumber = data.OffsetToLineNumber (selectedSegment.EndOffset);
			var copiedColoredChunks = new List<List<ClipboardColoredText>> ();
			foreach (var line in data.Document.GetLinesBetween (startLineNumber, endLineNumber)) {
				var offset = System.Math.Max (selectedSegment.Offset, line.Offset);
				var length = System.Math.Min (selectedSegment.EndOffset, line.EndOffset) - offset;
				var chunks = await data.GetChunks (
					line,
					offset,
					length
				);
				copiedColoredChunks.Add (
					chunks
					.Select (chunk => new ClipboardColoredText (chunk, data.Document))
					.ToList ()
				);
			}
			return copiedColoredChunks;
		}
	}
	
}
