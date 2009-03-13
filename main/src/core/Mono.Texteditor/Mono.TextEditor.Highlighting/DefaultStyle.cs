// DefaultStyle.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using Gdk;
using Gtk;

namespace Mono.TextEditor.Highlighting
{
	public class DefaultStyle : Style
	{
		public override ChunkStyle Default {
			get {
				return new ChunkStyle (widget.Style.Text (StateType.Normal), widget.Style.Base (StateType.Normal));
			}
		}
		
		public override ChunkStyle Caret {
			get {
				return Default;
			}
		}
		
		public override ChunkStyle LineNumber {
			get {
				return new ChunkStyle (new Gdk.Color (172, 168, 153), widget.Style.Base (StateType.Normal));
			}
		}
		
		public override Color LineNumberFgHighlighted {
			get {
				return new Gdk.Color (122, 118, 103);
			}
		}

		public override Color IconBarBg {
			get {
				return widget.Style.Background (StateType.Normal);
			}
		}

		public override Color IconBarSeperator {
			get {
				return widget.Style.Background (StateType.Active);
			}
		}

		public override ChunkStyle FoldLine {
			get {
				return LineNumber;
			}
		}

		public override Color FoldLineHighlighted {
			get {
				return new Gdk.Color (122, 118, 103);
			}
		}
		
		public override ChunkStyle Selection {
			get {
				return new ChunkStyle (widget.Style.Text (StateType.Selected), widget.Style.Base (StateType.Selected));
			}
		}

		public override Color LineMarker {
			get {
				return new Gdk.Color (172, 168, 153);
			}
		}

		public override Color Ruler {
			get {
				return new Gdk.Color (172, 168, 153);
			}
		}

//		public override Color WhitespaceMarker {
//			get {
//				return whitespaceMarker;
//			}
//		}
//
//		public override Color InvalidLineMarker {
//			get {
//				return invalidLineMarker;
//			}
//		}
//		public override Color FoldToggleMarker {
//			get {
//				return foldToggleMarker;
//			}
//		}
		Widget widget;
		public DefaultStyle (Widget widget)
		{
			this.PopulateDefaults ();
			this.widget = widget;
		}
	}
}