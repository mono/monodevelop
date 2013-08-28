// DebugTextMarker.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
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
//
//

using System;
using System.Linq;

using Mono.TextEditor;
using Mono.TextEditor.Highlighting;

namespace MonoDevelop.Debugger
{
	public abstract class DebugTextMarker : MarginMarker
	{
		protected DebugTextMarker (TextEditor editor)
		{
			Editor = editor;
		}

		protected abstract Cairo.Color BackgroundColor {
			get;
		}

		protected TextEditor Editor {
			get; private set;
		}

		public override bool CanDrawBackground (Margin margin)
		{
			return false;
		}

		public override bool CanDrawForeground (Margin margin)
		{
			return margin is IconMargin;
		}

		public override bool DrawBackground (TextEditor editor, Cairo.Context cr, double y, LineMetrics metrics)
		{
			// check, if a message bubble is active in that line.
			if (LineSegment != null && LineSegment.Markers.Any (m => m != this && (m is IExtendingTextLineMarker)))
				return false;

			return base.DrawBackground (editor, cr, y, metrics);
		}

		public override void DrawForeground (TextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			double size = metrics.Margin.Width;
			double borderLineWidth = cr.LineWidth;

			double x = Math.Floor (metrics.Margin.XOffset - borderLineWidth / 2);
			double y = Math.Floor (metrics.Y + (metrics.Height - size) / 2);

			DrawMarginIcon (cr, x, y, size);
		}

		protected virtual void SetForegroundColor (ChunkStyle style)
		{
		}

		public override ChunkStyle GetStyle (ChunkStyle baseStyle)
		{
			if (baseStyle == null)
				return null;

			var style = new ChunkStyle (baseStyle);
			style.Background = BackgroundColor;
			SetForegroundColor (style);

			return style;
		}

		protected virtual void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
		}

		protected static void DrawCircle (Cairo.Context cr, double x, double y, double size)
		{
			x += 0.5; y += 0.5;
			cr.NewPath ();
			cr.Arc (x + size/2, y + size / 2, (size-4)/2, 0, 2 * Math.PI);
			cr.ClosePath ();
		}

		protected static void DrawDiamond (Cairo.Context cr, double x, double y, double size)
		{
			x += 0.5; y += 0.5;
			size -= 2;
			cr.NewPath ();
			cr.MoveTo (x + size/2, y);
			cr.LineTo (x + size, y + size/2);
			cr.LineTo (x + size/2, y + size);
			cr.LineTo (x, y + size/2);
			cr.LineTo (x + size/2, y);
			cr.ClosePath ();
		}

		protected static void DrawArrow (Cairo.Context cr, double x, double y, double size)
		{
			y += 2.5;
			x += 2.5;
			size -= 4;
			double awidth = 0.5;
			double aheight = 0.4;
			double pich = (size - (size * aheight)) / 2;
			cr.NewPath ();
			cr.MoveTo (x + size * awidth, y);
			cr.LineTo (x + size, y + size / 2);
			cr.LineTo (x + size * awidth, y + size);
			cr.RelLineTo (0, -pich);
			cr.RelLineTo (-size * awidth, 0);
			cr.RelLineTo (0, -size * aheight);
			cr.RelLineTo (size * awidth, 0);
			cr.RelLineTo (0, -pich);
			cr.ClosePath ();
		}

		protected static void FillGradient (Cairo.Context cr, Cairo.Color color1, Cairo.Color color2, double x, double y, double size)
		{
			using (var pat = new Cairo.LinearGradient (x + size / 4, y, x + size / 2, y + size - 4)) {
				pat.AddColorStop (0, color1);
				pat.AddColorStop (1, color2);
				cr.SetSource (pat);
				cr.FillPreserve ();
			}
		}

		protected static void DrawBorder (Cairo.Context cr, Cairo.Color color, double x, double y, double size)
		{
			using (var pat = new Cairo.LinearGradient (x, y + size, x + size, y)) {
				pat.AddColorStop (0, color);
				cr.SetSource (pat);
				cr.Stroke ();
			}
		}
	}

	public class BreakpointTextMarker : DebugTextMarker
	{
		public BreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
		{
			IsTracepoint = tracepoint;
		}

		public bool IsTracepoint {
			get; private set;
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.BreakpointText.Background; }
		}

		protected override void SetForegroundColor (ChunkStyle style)
		{
			style.Foreground = Editor.ColorStyle.BreakpointText.Foreground;
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color color1 = Editor.ColorStyle.BreakpointMarker.Color;
			Cairo.Color color2 = Editor.ColorStyle.BreakpointMarker.SecondColor;
			if (IsTracepoint)
				DrawDiamond (cr, x, y, size);
			else
				DrawCircle (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, color2, x, y, size);
		}
	}

	public class DisabledBreakpointTextMarker : DebugTextMarker
	{
		public DisabledBreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
		{
			IsTracepoint = tracepoint;
		}

		public bool IsTracepoint {
			get; private set;
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.BreakpointMarkerDisabled.Color; }
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color border = Editor.ColorStyle.BreakpointText.Background;
			if (IsTracepoint)
				DrawDiamond (cr, x, y, size);
			else
				DrawCircle (cr, x, y, size);
			//FillGradient (cr, new Cairo.Color (1,1,1), new Cairo.Color (1,0.8,0.8), x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}

	public class InvalidBreakpointTextMarker : DebugTextMarker
	{
		public InvalidBreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
		{
			IsTracepoint = tracepoint;
		}

		public bool IsTracepoint {
			get; private set;
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.BreakpointTextInvalid.Background; }
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color color1 = Editor.ColorStyle.InvalidBreakpointMarker.Color;
			Cairo.Color color2 = color1;
			Cairo.Color border = Editor.ColorStyle.InvalidBreakpointMarker.SecondColor;

			if (IsTracepoint)
				DrawDiamond (cr, x, y, size);
			else
				DrawCircle (cr, x, y, size);

			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}

	public class CurrentDebugLineTextMarker : DebugTextMarker
	{
		public CurrentDebugLineTextMarker (TextEditor editor) : base (editor)
		{
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.DebuggerCurrentLine.Background; }
		}

		protected override void SetForegroundColor (ChunkStyle style)
		{
			style.Foreground = Editor.ColorStyle.DebuggerCurrentLine.Foreground;
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color color1 = Editor.ColorStyle.DebuggerCurrentLineMarker.Color;
			Cairo.Color color2 = Editor.ColorStyle.DebuggerCurrentLineMarker.SecondColor;
			Cairo.Color border = Editor.ColorStyle.DebuggerCurrentLineMarker.BorderColor;

			DrawArrow (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}

	public class DebugStackLineTextMarker : DebugTextMarker
	{
		public DebugStackLineTextMarker (TextEditor editor) : base (editor)
		{
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.DebuggerStackLine.Background; }
		}

		protected override void SetForegroundColor (ChunkStyle style)
		{
			style.Foreground = Editor.ColorStyle.DebuggerStackLine.Foreground;
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color color1 = Editor.ColorStyle.DebuggerStackLineMarker.Color;
			Cairo.Color color2 = Editor.ColorStyle.DebuggerStackLineMarker.SecondColor;
			Cairo.Color border = Editor.ColorStyle.DebuggerStackLineMarker.BorderColor;

			DrawArrow (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
}
