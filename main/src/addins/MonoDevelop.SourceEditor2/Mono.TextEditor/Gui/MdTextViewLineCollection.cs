//
// MdTextViewLineCollection.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	partial class MdTextViewLineCollection : List<ITextViewLine>, ITextViewLineCollection
	{
		readonly MonoTextEditor textEditor;
		readonly ITextSourceVersion version;
		ITextSnapshot textSnapshot;

		public MdTextViewLineCollection (MonoTextEditor textEditor) : base (64)
		{
			this.textEditor = textEditor;
			this.version = this.textEditor.Document.Version;
			textEditor.TextViewModel.VisualBuffer.ChangedLowPriority += OnVisualBufferChanged;
		}

		internal ITextViewLine Add (int logicalLineNumber, DocumentLine line, TextViewMargin.LayoutWrapper layoutWrapper)
		{
			if (line == null)
				return null;

			if (Count == 0)
				this.textSnapshot = textEditor.TextSnapshot;
			else
				System.Diagnostics.Debug.Assert (this.textSnapshot == textEditor.TextSnapshot);

			if (line.EndOffsetIncludingDelimiter > textSnapshot.Length) // prevent to create invalid text view lines.
				return null;

			var newLine = new MdTextViewLine (this, textEditor, line, logicalLineNumber, layoutWrapper);
			for (int i = 0; i < Count; i++) {
				if (((MdTextViewLine)this [i]).LineNumber == logicalLineNumber) {
					this [i] = newLine;
					return newLine;
				}
			}
			int index = 0;
			for (; index < Count; index++) {
				if (this [index].Start.Position > newLine.Start.Position)
					break;
			}
			Insert (index, newLine);
			return newLine;
		}

		internal void RemoveLinesBefore (int lineNumber)
		{
			for (int i = this.Count - 1; i >= 0; i--) {
				if (((MdTextViewLine)this [i]).LineNumber < lineNumber) {
					this.RemoveAt (i);
				}
			}
		}

		internal void RemoveLinesAfter (int lineNumber)
		{
			for (int i = this.Count - 1; i >= 0; i--) {
				if (((MdTextViewLine)this [i]).LineNumber > lineNumber) {
					this.RemoveAt (i);
				}
			}
		}

		public ITextViewLine FirstVisibleLine => this.FirstOrDefault ();

		public ITextViewLine LastVisibleLine => this.LastOrDefault ();

		public SnapshotSpan FormattedSpan {
			get {
				if (this.Count == 0)
					return new SnapshotSpan (textEditor.TextSnapshot, 0, 0);
				var start = this [0].Start;
				var end = this [Count - 1].EndIncludingLineBreak;
				if (start.Snapshot.Version.VersionNumber == end.Snapshot.Version.VersionNumber) {
					return new SnapshotSpan (start, end);
				} else if (start.Snapshot.Version.VersionNumber > end.Snapshot.Version.VersionNumber) {
					return new SnapshotSpan (start, end.TranslateTo (start.Snapshot, PointTrackingMode.Positive));
				} else {
					return new SnapshotSpan (start.TranslateTo (end.Snapshot, PointTrackingMode.Negative), end);
				}
			}
		}

		private readonly HashSet<int> modifiedLinesCache = new HashSet<int> ();
		private readonly List<MdTextViewLine> reusedLinesCache = new List<MdTextViewLine> ();

		internal void OnVisualBufferChanged (object sender, TextContentChangedEventArgs e)
		{
			if (textSnapshot != null) {
				foreach (ITextChange textChange in e.Changes) {
					Span textChangeCurrentSpan;
					if (e.Before == textSnapshot) {
						textChangeCurrentSpan = textChange.OldSpan;
					} else if (e.After == textSnapshot) {
						textChangeCurrentSpan = textChange.NewSpan;
					} else {
						ITrackingSpan textChangeOldSpan = e.Before.CreateTrackingSpan (textChange.OldSpan, SpanTrackingMode.EdgeInclusive);
						textChangeCurrentSpan = textChangeOldSpan.GetSpan (textSnapshot);
					}

					var startLine = textSnapshot.GetLineFromPosition (textChangeCurrentSpan.Start);
					var endLine = startLine;
					if (startLine.EndIncludingLineBreak.Position < textChangeCurrentSpan.End) {
						endLine = textSnapshot.GetLineFromPosition (textChangeCurrentSpan.End);
					}

					for (int i = startLine.LineNumber; i <= endLine.LineNumber; i++) {
						modifiedLinesCache.Add (i);
					}
				}
			}

			// Recreate MdTextViewLine for the current snapshot for all lines except those
			// modified as those will get recreated during render
			foreach (MdTextViewLine line in this) {
				int lineNumber = line.LineNumber;
				if (!modifiedLinesCache.Contains(lineNumber - 1)) {
					reusedLinesCache.Add (line);
				}
			}

			this.Clear ();

			var newSnapshot = e.After;
			foreach(MdTextViewLine line in reusedLinesCache) {
				var snapshotLine = textSnapshot.GetLineFromLineNumber (line.LineNumber - 1);
				var lineStart = textSnapshot.CreateTrackingPoint (snapshotLine.Start, PointTrackingMode.Negative);
				var newLineStart = lineStart.GetPosition (newSnapshot);
				var newSnapshotLine = newSnapshot.GetLineFromPosition (newLineStart);
				int lineNumber = newSnapshotLine.LineNumber + 1;
				var documentLine = textEditor.Document.GetLine (lineNumber);

				var newLine = new MdTextViewLine (this, textEditor, documentLine, lineNumber, textEditor.TextViewMargin.GetLayout (documentLine));
				Add (newLine);
			}

			modifiedLinesCache.Clear ();
			reusedLinesCache.Clear ();

			textSnapshot = newSnapshot;

			// we need this to synchronize MultiSelectionBroker to the new snapshot
			this.textEditor.TextArea.RaiseLayoutChanged ();
		}

		public bool IsValid => version.CompareAge (textEditor.Document.Version) == 0;

		public bool ContainsBufferPosition (SnapshotPoint bufferPosition)
		{
			foreach (var line in this) {
				if (line.Start <= bufferPosition && line.End < bufferPosition)
					return true;
			}
			return false;
		}

		public TextBounds GetCharacterBounds (SnapshotPoint bufferPosition)
		{
			foreach (var line in this) {
				if (line.Start <= bufferPosition && line.End < bufferPosition) {
					// TODO: correct both 0 parameters
					return new TextBounds (0, line.Top, 0, line.Height, line.TextTop, line.TextHeight);
				}
			}

			return new TextBounds ();
		}

		public int GetIndexOfTextLine (ITextViewLine textLine)
		{
			return IndexOf (textLine);
		}

		public Collection<TextBounds> GetNormalizedTextBounds (SnapshotSpan bufferSpan)
		{
			var bounds = new Collection<TextBounds> ();
			foreach (var line in this)
				foreach (var bound in line.GetNormalizedTextBounds (bufferSpan))
					bounds.Add (bound);
			return bounds;
		}

		public SnapshotSpan GetTextElementSpan (SnapshotPoint bufferPosition)
		{
			var line = GetTextViewLineContainingBufferPosition (bufferPosition);
			if (line == null)
				throw new ArgumentOutOfRangeException (nameof (bufferPosition));

			return line.GetTextElementSpan (bufferPosition);
		}

		public ITextViewLine GetTextViewLineContainingBufferPosition (SnapshotPoint bufferPosition)
		{
			if (textSnapshot == null)
			{
				textSnapshot = bufferPosition.Snapshot;
			}

			if (bufferPosition.Snapshot != textSnapshot) {
				throw new ArgumentOutOfRangeException ($"Requested snapshot version {bufferPosition.Snapshot.Version.VersionNumber} but the TextViewLineCollection currently contains snapshot version {textSnapshot.Version.VersionNumber}");
			}

			if (this.Count == 0 || bufferPosition.Position < this[0].Start.Position) {
				return CreateAndAddLineFromPosition (bufferPosition);
			}

			//If the position starts on or after the start of the last line, then use the
			//line's ContainsBufferPosition logic to handle the special case at the end of
			//the buffer.
			ITextViewLine lastLine = this [this.Count - 1];
			if (bufferPosition.Position >= lastLine.Start.Position) {
				if (lastLine.ContainsBufferPosition (bufferPosition)) {
					return lastLine;
				}

				return CreateAndAddLineFromPosition (bufferPosition);
			}

			int low = 0;
			int high = this.Count;
			while (low < high) {
				int middle = (low + high) / 2;
				var middleLine = this [middle];

				if (bufferPosition.Position < middleLine.Start.Position)
					high = middle;
				else
					low = middle + 1;
			}

			var result = this[low - 1];
			if (!result.ContainsBufferPosition (bufferPosition)) {
				result = CreateAndAddLineFromPosition (bufferPosition);
			}

			return result;
		}

		private ITextViewLine CreateAndAddLineFromPosition (SnapshotPoint position)
		{
			int lineNumber1Based = textSnapshot.GetLineNumberFromPosition (position) + 1;
			var line = textEditor.Document.GetLine (lineNumber1Based);
			var wrapper = textEditor.TextViewMargin.GetLayout (line);

			return Add (lineNumber1Based, line, wrapper);
		}

		public ITextViewLine GetTextViewLineContainingYCoordinate (double y)
		{
			foreach (var l in this) {
				if (l.Top <= y && l.Top + l.Height >= y)
					return l;
			}
			return null;
		}

		public Collection<ITextViewLine> GetTextViewLinesIntersectingSpan (SnapshotSpan bufferSpan)
		{
			var result = new Collection<ITextViewLine> ();
			foreach (var line in this) {
				if (line.IntersectsBufferSpan (bufferSpan))
					result.Add (line);
			}
			return result;
		}

		public bool IntersectsBufferSpan (SnapshotSpan bufferSpan)
		{
			foreach (var line in this)
				if (line.IntersectsBufferSpan (bufferSpan))
					return true;
			return false;
		}
	}
}
