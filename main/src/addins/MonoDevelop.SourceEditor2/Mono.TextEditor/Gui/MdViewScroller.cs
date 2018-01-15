using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Mono.TextEditor
{
	class MdViewScroller : IViewScroller
	{
		private TextArea _textEditor;

		internal MdViewScroller(TextArea editor)
		{
			_textEditor = editor;
		}

		public void EnsureSpanVisible (SnapshotSpan span)
		{
			this.EnsureSpanVisible (new VirtualSnapshotSpan (span), EnsureSpanVisibleOptions.None);
		}

		public void EnsureSpanVisible (SnapshotSpan span, EnsureSpanVisibleOptions options)
		{
			this.EnsureSpanVisible (new VirtualSnapshotSpan (span), options);
		}

		public void EnsureSpanVisible (VirtualSnapshotSpan span, EnsureSpanVisibleOptions options)
		{
			// If the textview is closed, this should be a no-op
			if (!_textEditor.IsClosed) {
				if ((options & ~(EnsureSpanVisibleOptions.ShowStart | EnsureSpanVisibleOptions.MinimumScroll | EnsureSpanVisibleOptions.AlwaysCenter)) != 0x00)
					throw new ArgumentOutOfRangeException ("options");

				//It is possible that this call is a result of an action that was defered until the view was loaded (& if so, it is possible that the
				//snapshot changed inbetween).
				span = span.TranslateTo (_textEditor.TextSnapshot);

				// TODO: handle the various options for scrolling
				_textEditor.ScrollTo (span.Start.Position);
			}
		}

		public void ScrollViewportHorizontallyByPixels (double distanceToScroll)
		{
			throw new NotImplementedException ();
		}

		public void ScrollViewportVerticallyByLine (ScrollDirection direction)
		{
			throw new NotImplementedException ();
		}

		public void ScrollViewportVerticallyByLines (ScrollDirection direction, int count)
		{
			throw new NotImplementedException ();
		}

		public bool ScrollViewportVerticallyByPage (ScrollDirection direction)
		{
			throw new NotImplementedException ();
		}

		public void ScrollViewportVerticallyByPixels (double distanceToScroll)
		{
			throw new NotImplementedException ();
		}
	}
}
