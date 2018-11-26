//
// MdTextViewLineCollection.MdTextViewLine.cs
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
using Microsoft.VisualStudio.Language.Intellisense.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Implementation;
using MonoDevelop.Core.Text;
using System.Threading;
using MonoDevelop.Ide;

namespace Mono.TextEditor
{
	partial class MdTextViewLineCollection
	{
		internal sealed class MdTextViewLine : ITextViewLine
		{
			internal readonly DocumentLine line;
			internal readonly TextViewMargin.LayoutWrapper layoutWrapper;
			readonly MdTextViewLineCollection collection; 
			MonoTextEditor textEditor;

			/// <summary>
			/// 1-based
			/// </summary>
			public int LineNumber { get; private set; }

			public MdTextViewLine(MdTextViewLineCollection collection, MonoTextEditor textEditor, DocumentLine line, int lineNumber, TextViewMargin.LayoutWrapper layoutWrapper)
			{
				this.collection = collection;
				this.layoutWrapper = layoutWrapper;
				this.textEditor = textEditor;
				this.line = line;
				this.LineNumber = lineNumber;
				Snapshot = textEditor.VisualSnapshot;
				this.LineBreakLength = line.DelimiterLength;
			}

			object indentityTag = new object ();
			public object IdentityTag => indentityTag;

			public ITextSnapshot Snapshot { get; }

			public bool IsFirstTextViewLineForSnapshotLine => collection[0] == this;

			public bool IsLastTextViewLineForSnapshotLine => collection[collection.Count - 1] == this;

			public double Baseline => throw new System.NotImplementedException();

			public SnapshotSpan Extent => new SnapshotSpan (Snapshot, line.Offset, line.Length);

			public IMappingSpan ExtentAsMappingSpan => new MappingSpan (Extent, SpanTrackingMode.EdgeInclusive, null);

			public SnapshotSpan ExtentIncludingLineBreak => new SnapshotSpan (Snapshot, line.Offset, line.LengthIncludingDelimiter);

			public IMappingSpan ExtentIncludingLineBreakAsMappingSpan => new MappingSpan (ExtentIncludingLineBreak, SpanTrackingMode.EdgeInclusive, null);

			public SnapshotPoint Start => new SnapshotPoint (Snapshot, line.Offset);

			public int Length => Length - LineBreakLength;

			public int LengthIncludingLineBreak => line.LengthIncludingDelimiter;

			public SnapshotPoint End => EndIncludingLineBreak - LineBreakLength;

			public SnapshotPoint EndIncludingLineBreak => new SnapshotPoint (Snapshot, line.EndOffsetIncludingDelimiter);

			public int LineBreakLength { get; }

			public double Left => textEditor.VAdjustment.Value;

			public double Top => textEditor.LocationToPoint (line.LineNumber, 0).Y;

			public double Height => layoutWrapper.Height;

			public double TextTop => Top;

			public double TextBottom => Top + TextHeight;

			public double TextHeight => textEditor.LineHeight;

			public double TextLeft => Left;

			public double TextRight => layoutWrapper.Width;

			public double TextWidth => layoutWrapper.Width;

			public double Width => textEditor.TextViewMargin.RectInParent.Width;

			public double Bottom => TextBottom;

			public double Right => layoutWrapper.Width;

			public double EndOfLineWidth => 0;

			public double VirtualSpaceWidth => 0;

			public bool IsValid => true;

			public LineTransform LineTransform => DefaultLineTransform;

			public LineTransform DefaultLineTransform => new LineTransform ();

			public VisibilityState VisibilityState => VisibilityState.FullyVisible;

			public double DeltaY => 0;

			public TextViewLineChange Change => TextViewLineChange.None;

			SnapshotPoint FixBufferPosition(SnapshotPoint bufferPosition)
			{
				if (bufferPosition.Snapshot != Snapshot)
					throw new ArgumentException("The specified SnapshotPoint is on a different ITextSnapshot than this SnapshotPoint.");

				return bufferPosition;
			}

			public bool ContainsBufferPosition(SnapshotPoint bufferPosition)
			{
				bufferPosition = this.FixBufferPosition(bufferPosition);

				return ((bufferPosition >= line.Offset) &&
						((bufferPosition < line.EndOffsetIncludingDelimiter) ||
						 ((bufferPosition == line.EndOffsetIncludingDelimiter) &&
						 (LineBreakLength == 0) && (line.EndOffsetIncludingDelimiter == Snapshot.Length))));
			}

			public TextBounds? GetAdornmentBounds(object identityTag)
			{
				throw new System.NotImplementedException();
			}

			public ReadOnlyCollection<object> GetAdornmentTags(object providerTag)
			{
				throw new System.NotImplementedException();
			}

			public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate, bool textOnly)
			{
				var y = textEditor.LocationToPoint(textEditor.OffsetToLocation(line.Offset)).Y;
				var loc = textEditor.PointToLocation(xCoordinate, y);
				return Snapshot.GetSnapshotPoint (loc.Line, loc.Column);
			}

			public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate)
			{
				return GetBufferPositionFromXCoordinate(xCoordinate, true);
			}

			public TextBounds GetCharacterBounds(SnapshotPoint bufferPosition)
			{
				var point = textEditor.LocationToPoint(textEditor.OffsetToLocation(bufferPosition.Position));
				return new TextBounds(point.X, point.Y, textEditor.TextViewMargin.CharWidth, textEditor.LineHeight, TextTop, textEditor.LineHeight);
			}

			public TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition)
			{
				return GetCharacterBounds (bufferPosition.Position);
			}

			public TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition)
			{
				return new TextBounds(textEditor.LocationToPoint(textEditor.OffsetToLocation(bufferPosition.Position)).X, Top, Width, TextHeight, TextTop, TextHeight);
			}

			public TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition)
			{
				// if the point is in virtual space, then it can't be next to any space negotiating adornments, 
				// so just return its character bounds. If the point is not in virtual space, then use the regular
				// GetExtendedCharacterBounds method for a non-virtual SnapshotPoint
				if (bufferPosition.IsInVirtualSpace)
					return this.GetCharacterBounds (bufferPosition);
				else
					return this.GetExtendedCharacterBounds (bufferPosition.Position);
			}

			public VirtualSnapshotPoint GetInsertionBufferPositionFromXCoordinate(double xCoordinate)
			{
				throw new System.NotImplementedException();
			}

			public Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan)
			{
				if (bufferSpan.OverlapsWith(new Span (line.Offset, line.Length)))
				{
					double leading = 0;
					if (line.Contains(bufferSpan.Start))
						leading = textEditor.LocationToPoint(textEditor.OffsetToLocation(bufferSpan.Start)).X;
					var endLoc = textEditor.OffsetToLocation(line.Contains(bufferSpan.End) ? bufferSpan.End : line.EndOffsetIncludingDelimiter);
					double endPos = textEditor.LocationToPoint(endLoc).X;
					return new Collection<TextBounds>(new List<TextBounds>() { new TextBounds(leading, Top, endPos - leading, TextHeight, TextTop, TextHeight) });
				}
				else
					return new Collection<TextBounds>();
			}

			public SnapshotSpan GetTextElementSpan (SnapshotPoint bufferPosition)
			{
				bufferPosition = this.FixBufferPosition (bufferPosition);
				if (!this.ContainsBufferPosition (bufferPosition))
					throw new ArgumentOutOfRangeException (nameof (bufferPosition));

				if (bufferPosition >= ExtentIncludingLineBreak.End - LineBreakLength) {
					return new SnapshotSpan (ExtentIncludingLineBreak.End - LineBreakLength, LineBreakLength);
				}
				var line = textEditor.GetLineByOffset (bufferPosition.Position);
				var lineOffset = line.Offset;

				var highlightedLine = this.textEditor.Document.SyntaxMode.GetHighlightedLineAsync (line, default (CancellationToken)).WaitAndGetResult ();
				if (highlightedLine != null) {
					foreach (var seg in highlightedLine.Segments) {
						if (seg.Contains (bufferPosition - lineOffset)) {
							return new SnapshotSpan (bufferPosition.Snapshot, lineOffset + seg.Offset, seg.Length);
						}
					}
				}

				var c = textEditor.GetCharAt (bufferPosition.Position);
				if (CaretMoveActions.IsLowSurrogateMarkerSet (c))
					return new SnapshotSpan (bufferPosition.Snapshot, bufferPosition.Position, 2);
				if (CaretMoveActions.IsHighSurrogateMarkerSet (c))
					return new SnapshotSpan (bufferPosition.Snapshot, bufferPosition.Position - 1, 2);
				return new SnapshotSpan (bufferPosition, 1);
			}

			public VirtualSnapshotPoint GetVirtualBufferPositionFromXCoordinate(double xCoordinate)
			{
				throw new System.NotImplementedException();
			}

			public bool IntersectsBufferSpan(SnapshotSpan bufferSpan)
			{
				return new Span (line.Offset, line.LengthIncludingDelimiter).IntersectsWith (bufferSpan);
			}
		}
	}
}