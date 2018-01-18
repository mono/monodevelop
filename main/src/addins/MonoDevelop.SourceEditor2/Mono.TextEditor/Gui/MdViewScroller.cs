//
// MdViewScroller.cs
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Mono.TextEditor
{
	class MdViewScroller : IViewScroller
	{
		readonly MonoTextEditor textEditor;

		internal MdViewScroller(MonoTextEditor editor)
		{
			textEditor = editor;
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
			if (!textEditor.IsClosed) {
				if ((options & ~(EnsureSpanVisibleOptions.ShowStart | EnsureSpanVisibleOptions.MinimumScroll | EnsureSpanVisibleOptions.AlwaysCenter)) != 0x00)
					throw new ArgumentOutOfRangeException ("options");

				//It is possible that this call is a result of an action that was defered until the view was loaded (& if so, it is possible that the
				//snapshot changed inbetween).
				span = span.TranslateTo (textEditor.TextSnapshot);

				// TODO: handle the various options for scrolling
				textEditor.ScrollTo (span.Start.Position);
			}
		}

		public void ScrollViewportHorizontallyByPixels (double distanceToScroll)
		{
			textEditor.HAdjustment.Value += distanceToScroll;
		}

		public void ScrollViewportVerticallyByLine (ScrollDirection direction)
		{
			ScrollViewportVerticallyByLines (direction, 1);
		}

		public void ScrollViewportVerticallyByLines (ScrollDirection direction, int count)
		{
			switch (direction) {
			case ScrollDirection.Up:
				textEditor.VAdjustment.Value -= textEditor.LineHeight * count;
				break;
			case ScrollDirection.Down:
				textEditor.VAdjustment.Value += textEditor.LineHeight * count;
				break;
			}
		}

		public bool ScrollViewportVerticallyByPage (ScrollDirection direction)
		{
			switch (direction) {
			case ScrollDirection.Up:
				if (textEditor.VAdjustment.Value == 0)
					return false;
				textEditor.VAdjustment.Value -= textEditor.VAdjustment.PageSize;
				return true;
			case ScrollDirection.Down:
				if (textEditor.VAdjustment.Value + textEditor.VAdjustment.PageSize > textEditor.VAdjustment.Upper)
					return false;
				textEditor.VAdjustment.Value += textEditor.VAdjustment.PageSize;
				return true;
			}
			return false;
		}

		public void ScrollViewportVerticallyByPixels (double distanceToScroll)
		{
			textEditor.VAdjustment.Value += distanceToScroll;
		}
	}
}
