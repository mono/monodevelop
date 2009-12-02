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
		ChunkStyle defaultStyle;
		public override ChunkStyle Default {
			get {
				return defaultStyle;
			}
		}
		
		public override ChunkStyle Caret {
			get {
				return Default;
			}
		}
		
		ChunkStyle lineNumberStyle;
		public override ChunkStyle LineNumber {
			get {
				return lineNumberStyle;
			}
		}
		
		public override Color LineNumberFgHighlighted {
			get {
				return new Gdk.Color (122, 118, 103);
			}
		}
		Color iconBarBg;
		public override Color IconBarBg {
			get {
				return iconBarBg;
			}
		}

		Color iconBarSeperator;
		public override Color IconBarSeperator {
			get {
				return iconBarSeperator;
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
		
		ChunkStyle selectionStyle;
		public override ChunkStyle Selection {
			get {
				return selectionStyle;
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
		
		public DefaultStyle (Gtk.Style widgetStyle)
		{
			this.PopulateDefaults ();
			UpdateFromGtkStyle (widgetStyle ?? Gtk.Widget.DefaultStyle);
		}
		
		public override void UpdateFromGtkStyle (Gtk.Style style)
		{
			this.selectionStyle = new ChunkStyle (style.Text (StateType.Selected), style.Base (StateType.Selected));
			this.defaultStyle = new ChunkStyle (style.Text (StateType.Normal), style.Base (StateType.Normal));
			this.lineNumberStyle = new ChunkStyle (new Gdk.Color (172, 168, 153), style.Base (StateType.Normal));
			this.iconBarBg = style.Background (StateType.Normal);
			this.iconBarSeperator = style.Background (StateType.Active);
		}

	}
}