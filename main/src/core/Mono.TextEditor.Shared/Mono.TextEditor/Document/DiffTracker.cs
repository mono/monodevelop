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
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	class DiffTracker
	{
		class LineChangeInfo
		{
			internal readonly static LineChangeInfo Unchanged = new LineChangeInfo (TextDocument.LineState.Unchanged);
			internal readonly static LineChangeInfo Dirty = new LineChangeInfo (TextDocument.LineState.Dirty);
			internal readonly static LineChangeInfo Changed = new LineChangeInfo (TextDocument.LineState.Changed);

			public Mono.TextEditor.TextDocument.LineState state;

			LineChangeInfo (Mono.TextEditor.TextDocument.LineState state)
			{
				this.state = state;
			}
		}

		CompressingTreeList<LineChangeInfo> lineStates;
		TextDocument trackDocument;
		IReadonlyTextDocument trackDocumentSnapshot;

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
			trackDocumentSnapshot = document.CreateDocumentSnapshot ();
		}

		void TrackDocument_TextChanged (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			if (lineStates == null || e.TextChanges.Count == 0)
				return;
			for (int i = e.TextChanges.Count - 1; i >= 0; i--) {
				var change = e.TextChanges [i];
				if (change.RemovalLength == 0)
					continue;

				var startLine = trackDocumentSnapshot.GetLineByOffset (change.Offset);
				var endRemoveLine = trackDocumentSnapshot.GetLineByOffset (change.Offset + change.RemovalLength);
				if (startLine == null || endRemoveLine == null)
					continue;
				try {
					var lineNumber = startLine.LineNumber;
					lineStates.RemoveRange (lineNumber, endRemoveLine.LineNumber - lineNumber);
				} catch (Exception ex) {
					LoggingService.LogError ("error while DiffTracker.TrackDocument_TextChanged changing update", ex);
				}
			}

			for (int i = 0; i < e.TextChanges.Count; ++i) {
				var change = e.TextChanges[i];
				var startLine = trackDocument.GetLineByOffset (change.NewOffset);
				var endLine = trackDocument.GetLineByOffset (change.NewOffset + change.InsertionLength);
				var lineNumber = startLine.LineNumber;
				var insertedLines = endLine.LineNumber - lineNumber;
				if (insertedLines == 0) {
					var oldState = lineNumber < lineStates.Count ? lineStates [lineNumber] : null;
					if (oldState != null && oldState.state == TextDocument.LineState.Dirty) 
						continue;
					lineStates[lineNumber] = LineChangeInfo.Dirty;
					if (trackDocument != null)
						trackDocument.CommitMultipleLineUpdate (lineNumber, lineNumber + insertedLines);
					continue;
				}
				try {
					lineStates.InsertRange (lineNumber, insertedLines , LineChangeInfo.Dirty);
					if (trackDocument != null)
						trackDocument.CommitMultipleLineUpdate (lineNumber, lineNumber + insertedLines);
				} catch (Exception ex) {
					LoggingService.LogError ("error while DiffTracker.TrackDocument_TextChanged changed update", ex);
				}
			}
			trackDocumentSnapshot = trackDocument.CreateDocumentSnapshot ();
		}

		public void SetBaseDocument (IReadonlyTextDocument document)
		{
			var curLineStates = lineStates;
			if (curLineStates != null) {
				try {
					foreach (var node in curLineStates.tree) {
						if (node.value.state == TextDocument.LineState.Dirty)
							node.value = LineChangeInfo.Changed;
					}
				} catch (Exception e) {
					LoggingService.LogError ("error while DiffTracker.SetBaseDocument", e);
					Reset ();
				}
			} else {
				lineStates = new CompressingTreeList<LineChangeInfo>((x, y) => x.Equals(y));
				lineStates.InsertRange (0, document.LineCount + 1, LineChangeInfo.Unchanged);
				trackDocument.TextChanged += TrackDocument_TextChanged;
			}
		}

		public void Reset ()
		{
			lineStates = new CompressingTreeList<LineChangeInfo>((x, y) => x.Equals(y));
			lineStates.InsertRange(0, trackDocument.LineCount + 1, LineChangeInfo.Unchanged);
		}
	}
}

