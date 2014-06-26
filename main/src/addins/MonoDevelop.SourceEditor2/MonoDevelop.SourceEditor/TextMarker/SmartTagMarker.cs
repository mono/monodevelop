//
// SmartTagMarker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	class SmartTagMarker : TextSegmentMarker, IActionTextLineMarker, ISmartTagMarker
	{
		const double tagMarkerWidth = 8;
		const double tagMarkerHeight = 2;
		MonoDevelop.Ide.Editor.DocumentLocation loc;
		Mono.TextEditor.TextEditor editor;

		public SmartTagMarker (int offset, MonoDevelop.Ide.Editor.DocumentLocation realLocation) : base (offset, 0)
		{
			this.loc = realLocation;
		}

		public override void Draw (Mono.TextEditor.TextEditor editor, Cairo.Context cr, LineMetrics metrics, int startOffset, int endOffset)
		{
			this.editor = editor;
			var line = editor.GetLine (loc.Line);
			var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition;

			cr.Rectangle (Math.Floor (x) + 0.5, Math.Floor (metrics.LineYRenderStartPosition) + 0.5 + (line == editor.GetLineByOffset (startOffset) ? editor.LineHeight - tagMarkerHeight - 1 : 0), tagMarkerWidth * cr.LineWidth, tagMarkerHeight * cr.LineWidth);

			if (HslColor.Brightness (editor.ColorStyle.PlainText.Background) < 0.5) {
				cr.SetSourceRGBA (0.8, 0.8, 1, 0.9);
			} else {
				cr.SetSourceRGBA (0.2, 0.2, 1, 0.9);
			}
			cr.Stroke ();
		}

		#region IActionTextLineMarker implementation
		class TextEventArgsWrapper : TextMarkerMouseEventArgs
		{
			readonly MarginMouseEventArgs args;

			public override double X {
				get {
					return args.X;
				}
			}

			public override double Y {
				get {
					return args.Y;
				}
			}

			public override object OverwriteCursor {
				get;
				set;
			}

			public override string TooltipMarkup {
				get;
				set;
			}

			public TextEventArgsWrapper (MarginMouseEventArgs args)
			{
				if (args == null)
					throw new ArgumentNullException ("args");
				this.args = args;
			}

			public override bool TriggersContextMenu ()
			{
				return args.TriggersContextMenu ();
			}
		}

		bool IActionTextLineMarker.MousePressed (Mono.TextEditor.TextEditor editor, MarginMouseEventArgs args)
		{
			var handler = MousePressed;
			if (handler != null)
				handler (this, new TextEventArgsWrapper (args));
			return false;
		}

		void IActionTextLineMarker.MouseHover (Mono.TextEditor.TextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
		{
			if (args.Button != 0)
				return;
			var handler = MouseHover;
			if (handler != null)
				handler (this, new TextEventArgsWrapper (args));
		}

		#endregion

		bool ISmartTagMarker.IsInsideSmartTag (double x, double y)
		{
			var lin2 = editor.GetLine (loc.Line);
			var x2 = editor.ColumnToX (lin2, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.TextStartPosition;
			var y2 = editor.LineToY (loc.Line + 1) - editor.VAdjustment.Value;
			return x - x2 >= 0 * editor.Options.Zoom && 
				x - x2 < tagMarkerWidth * editor.Options.Zoom && 
				y - y2 < (editor.LineHeight / 2) * editor.Options.Zoom;
		}

		bool ISmartTagMarker.IsInsideWindow (Gtk.MotionNotifyEventArgs args)
		{
			return args.Event.Window == editor.TextArea.GdkWindow;
		}

		public event EventHandler<TextMarkerMouseEventArgs> MousePressed;
		public event EventHandler<TextMarkerMouseEventArgs> MouseHover;

		object ITextSegmentMarker.Tag {
			get;
			set;
		}
	}
}

