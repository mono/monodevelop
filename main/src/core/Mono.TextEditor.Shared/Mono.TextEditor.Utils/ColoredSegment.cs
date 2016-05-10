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

namespace Mono.TextEditor.Utils
{
	class ColoredSegment
	{
		public string Style { get; set; }
		public string Text { get; set; }

		public ColoredSegment (Chunk chunk, TextDocument doc)
		{
			this.Style = chunk.Style;
			this.Text = doc.GetTextAt (chunk);
		}

		public static List<List<ColoredSegment>> GetChunks (TextEditorData data, ISegment selectedSegment)
		{
			int startLineNumber = data.OffsetToLineNumber (selectedSegment.Offset);
			int endLineNumber = data.OffsetToLineNumber (selectedSegment.EndOffset);
			var copiedColoredChunks = new List<List<ColoredSegment>> ();
			foreach (var line in data.Document.GetLinesBetween (startLineNumber, endLineNumber)) {
				var offset = System.Math.Max (selectedSegment.Offset, line.Offset);
				var length = System.Math.Min (selectedSegment.EndOffset, line.EndOffset) - offset;
				copiedColoredChunks.Add (
					data.GetChunks (
					line, 
					offset,
					length
				)
					.Select (chunk => new ColoredSegment (chunk, data.Document))
					.ToList ()
				);
			}
			return copiedColoredChunks;
		}
	}
	
}
