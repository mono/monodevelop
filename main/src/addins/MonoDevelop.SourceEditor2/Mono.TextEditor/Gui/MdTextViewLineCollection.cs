using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Mono.TextEditor
{
	class MdTextViewLineCollection : List<ITextViewLine>, ITextViewLineCollection
	{
		private readonly TextArea textView;

		public MdTextViewLineCollection (TextArea textView) : base (64)
		{
			this.textView = textView;
			foreach (var item in textView.TextViewMargin.CachedLine.OrderBy (l => l.LineNumber)) {
				var textViewLine = new MdTextViewLine (textView, item);
				Add (textViewLine);
			}
		}

		public ITextViewLine FirstVisibleLine => this.FirstOrDefault ();

		public ITextViewLine LastVisibleLine => this.LastOrDefault ();

		public SnapshotSpan FormattedSpan => new SnapshotSpan (this [0].Start, this.Last ().EndIncludingLineBreak);

		public bool IsValid => throw new NotImplementedException ();

		public bool ContainsBufferPosition (SnapshotPoint bufferPosition)
		{
			throw new NotImplementedException ();
		}

		public TextBounds GetCharacterBounds (SnapshotPoint bufferPosition)
		{
			throw new NotImplementedException ();
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
			return new SnapshotSpan (bufferPosition, 1);
		}

		public ITextViewLine GetTextViewLineContainingBufferPosition (SnapshotPoint bufferPosition)
		{
			return this.FirstOrDefault (l => l.ContainsBufferPosition (bufferPosition));
		}

		public ITextViewLine GetTextViewLineContainingYCoordinate (double y)
		{
			return this.FirstOrDefault (l => l.Top <= y && l.Top + l.Height >= y);
		}

		public Collection<ITextViewLine> GetTextViewLinesIntersectingSpan (SnapshotSpan bufferSpan)
		{
			throw new NotImplementedException ();
		}

		public bool IntersectsBufferSpan (SnapshotSpan bufferSpan)
		{
			throw new NotImplementedException ();
		}
	}
}
