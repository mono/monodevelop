//
// DiffTracker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor.Utils;

namespace Mono.TextEditor
{
	class DiffTracker
	{
		class LineChangeInfo
		{
			public Mono.TextEditor.TextDocument.LineState state;

			public LineChangeInfo (Mono.TextEditor.TextDocument.LineState state)
			{
				this.state = state;
			}
		}

		CompressingTreeList<LineChangeInfo> lineStates;
		TextDocument trackDocument;
//		TextDocument baseDocument;

		public Mono.TextEditor.TextDocument.LineState GetLineState (DocumentLine line)
		{
			if (line != null && lineStates != null) {
				try {
					var info = lineStates [line.LineNumber];
					if (info != null) {
						return info.state;
					}
				} catch (Exception) {

				}
			}
			return Mono.TextEditor.TextDocument.LineState.Unchanged;
		}

		public void SetTrackDocument (TextDocument document)
		{
			trackDocument = document;
		}

		void TrackDocument_TextChanging (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			if (lineStates == null)
				return;
			var startLine = trackDocument.GetLineByOffset (e.Offset);
			var endRemoveLine = trackDocument.GetLineByOffset (e.Offset + e.RemovalLength);
			try {
				var lineNumber = startLine.LineNumber;
				lineStates.RemoveRange (lineNumber, endRemoveLine.LineNumber - lineNumber);
			} catch (Exception ex) {
				Console.WriteLine ("error while DiffTracker.TrackDocument_TextChanging:" + ex);
			}
		}

		void TrackDocument_TextChanged (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			var startLine = trackDocument.GetLineByOffset (e.Offset);
			var endLine = trackDocument.GetLineByOffset (e.Offset + e.InsertionLength);
			var lineNumber = startLine.LineNumber;
			var insertedLines = endLine.LineNumber - lineNumber;
			try {
				lineStates [lineNumber] = new LineChangeInfo (Mono.TextEditor.TextDocument.LineState.Dirty);
				if (trackDocument != null)
					trackDocument.CommitLineUpdate (lineNumber);
				while (insertedLines-- > 0) {
					lineStates.Insert (lineNumber, new LineChangeInfo (Mono.TextEditor.TextDocument.LineState.Dirty));
				}
			} catch (Exception ex) {
				Console.WriteLine ("error while DiffTracker.TrackDocument_TextChanged:" + ex);
			}

		}

		public void SetBaseDocument (TextDocument document)
		{
			if (lineStates != null) {
				foreach (var node in lineStates.tree) {
					if (node.value.state == Mono.TextEditor.TextDocument.LineState.Dirty)
						node.value.state = Mono.TextEditor.TextDocument.LineState.Changed;
				}
			} else {
				lineStates = new CompressingTreeList<LineChangeInfo>((x, y) => x.Equals(y));
				lineStates.InsertRange(0, document.LineCount + 1, new LineChangeInfo (Mono.TextEditor.TextDocument.LineState.Unchanged));
				trackDocument.TextChanging += TrackDocument_TextChanging;
				trackDocument.TextChanged += TrackDocument_TextChanged;
			}
		}

		public void Reset ()
		{
			lineStates = new CompressingTreeList<LineChangeInfo>((x, y) => x.Equals(y));
			lineStates.InsertRange(0, trackDocument.LineCount + 1, new LineChangeInfo (Mono.TextEditor.TextDocument.LineState.Unchanged));
		}
	}
}

