using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;

namespace Mono.TextEditor
{
	sealed class MdTextViewLine : ITextViewLine
	{
		TextArea textArea;
		private readonly DocumentLine item;
		SnapshotSpan lineSpan;
		int lineBreakLength;

		public MdTextViewLine (TextArea textArea, DocumentLine item)
		{
			this.textArea = textArea;
			this.item = item;
			this.lineSpan = new SnapshotSpan (textArea.VisualSnapshot, item.Offset, item.LengthIncludingDelimiter);
			this.lineBreakLength = item.DelimiterLength;
		}

		public object IdentityTag => throw new System.NotImplementedException ();

		public ITextSnapshot Snapshot => lineSpan.Snapshot;

		public bool IsFirstTextViewLineForSnapshotLine => throw new System.NotImplementedException ();

		public bool IsLastTextViewLineForSnapshotLine => throw new System.NotImplementedException ();

		public double Baseline => throw new System.NotImplementedException ();

		public SnapshotSpan Extent => throw new System.NotImplementedException ();

		public IMappingSpan ExtentAsMappingSpan => throw new System.NotImplementedException ();

		public SnapshotSpan ExtentIncludingLineBreak => throw new System.NotImplementedException ();

		public IMappingSpan ExtentIncludingLineBreakAsMappingSpan => throw new System.NotImplementedException ();

		public SnapshotPoint Start => lineSpan.Start;

		public int Length => Length - LineBreakLength;

		public int LengthIncludingLineBreak => lineSpan.Length;

		public SnapshotPoint End => EndIncludingLineBreak - LineBreakLength;

		public SnapshotPoint EndIncludingLineBreak => lineSpan.End;

		public int LineBreakLength => lineBreakLength;

		public double Left => throw new System.NotImplementedException ();

		public double Top {
			get {
				return textArea.LocationToPoint (item.LineNumber, 0).Y;
			}
		}

		public double Height => TextHeight;

		public double TextTop => Top;

		public double TextBottom => Top + TextHeight;

		public double TextHeight => textArea.LineHeight;

		public double TextLeft => throw new System.NotImplementedException ();

		public double TextRight => throw new System.NotImplementedException ();

		public double TextWidth => throw new System.NotImplementedException ();

		public double Width => textArea.TextViewMargin.RectInParent.Width;

		public double Bottom => TextBottom;

		public double Right => throw new System.NotImplementedException ();

		public double EndOfLineWidth => throw new System.NotImplementedException ();

		public double VirtualSpaceWidth => throw new System.NotImplementedException ();

		public bool IsValid => throw new System.NotImplementedException ();

		public LineTransform LineTransform => throw new System.NotImplementedException ();

		public LineTransform DefaultLineTransform => throw new System.NotImplementedException ();

		public VisibilityState VisibilityState => VisibilityState.FullyVisible;

		public double DeltaY => throw new System.NotImplementedException ();

		public TextViewLineChange Change => throw new System.NotImplementedException ();

		SnapshotPoint FixBufferPosition (SnapshotPoint bufferPosition)
		{
			if (bufferPosition.Snapshot != this.lineSpan.Snapshot)
				throw new ArgumentException ("The specified SnapshotPoint is on a different ITextSnapshot than this SnapshotPoint.");

			return bufferPosition;
		}

		public bool ContainsBufferPosition (SnapshotPoint bufferPosition)
		{
			bufferPosition = this.FixBufferPosition (bufferPosition);

			return ((bufferPosition >= lineSpan.Start) &&
					((bufferPosition < lineSpan.End) ||
					 ((bufferPosition == lineSpan.End) &&
					 (lineBreakLength == 0) && (lineSpan.End == lineSpan.Snapshot.Length))));
		}

		public TextBounds? GetAdornmentBounds (object identityTag)
		{
			throw new System.NotImplementedException ();
		}

		public ReadOnlyCollection<object> GetAdornmentTags (object providerTag)
		{
			throw new System.NotImplementedException ();
		}

		public SnapshotPoint? GetBufferPositionFromXCoordinate (double xCoordinate, bool textOnly)
		{
			var y = textArea.LocationToPoint (textArea.OffsetToLocation (lineSpan.Start)).Y;
			var loc = textArea.PointToLocation (xCoordinate, y);
			var pos = textArea.LocationToOffset (loc);
			return new SnapshotPoint (Snapshot, pos);
		}

		public SnapshotPoint? GetBufferPositionFromXCoordinate (double xCoordinate)
		{
			return GetBufferPositionFromXCoordinate (xCoordinate, true);
		}

		public TextBounds GetCharacterBounds (SnapshotPoint bufferPosition)
		{
			var point = textArea.LocationToPoint (textArea.OffsetToLocation (bufferPosition.Position));
			return new TextBounds (point.X, point.Y, textArea.TextViewMargin.CharWidth, textArea.LineHeight, TextTop, textArea.LineHeight);
		}

		public TextBounds GetCharacterBounds (VirtualSnapshotPoint bufferPosition)
		{
			throw new System.NotImplementedException ();
		}

		public TextBounds GetExtendedCharacterBounds (SnapshotPoint bufferPosition)
		{
			return new TextBounds (textArea.LocationToPoint (textArea.OffsetToLocation (bufferPosition.Position)).X, Top, Width, TextHeight, TextTop, TextHeight);
		}

		public TextBounds GetExtendedCharacterBounds (VirtualSnapshotPoint bufferPosition)
		{
			throw new System.NotImplementedException ();
		}

		public VirtualSnapshotPoint GetInsertionBufferPositionFromXCoordinate (double xCoordinate)
		{
			throw new System.NotImplementedException ();
		}

		public Collection<TextBounds> GetNormalizedTextBounds (SnapshotSpan bufferSpan)
		{
			if (bufferSpan.OverlapsWith (lineSpan)) {
				double leading = 0;
				if (lineSpan.Contains (bufferSpan.Start))
					leading = textArea.LocationToPoint (textArea.OffsetToLocation (bufferSpan.Start)).X;
				var endLoc = textArea.OffsetToLocation (lineSpan.Contains (bufferSpan.End) ? bufferSpan.End : lineSpan.End);
				double endPos = textArea.LocationToPoint (endLoc).X;
				return new Collection<TextBounds> (new List<TextBounds> () { new TextBounds (leading, Top, endPos - leading, TextHeight, TextTop, TextHeight) });
			} else
				return new Collection<TextBounds> ();
		}

		public SnapshotSpan GetTextElementSpan (SnapshotPoint bufferPosition)
		{
			throw new System.NotImplementedException ();
		}

		public VirtualSnapshotPoint GetVirtualBufferPositionFromXCoordinate (double xCoordinate)
		{
			throw new System.NotImplementedException ();
		}

		public bool IntersectsBufferSpan (SnapshotSpan bufferSpan)
		{
			throw new System.NotImplementedException ();
		}
	}
}
