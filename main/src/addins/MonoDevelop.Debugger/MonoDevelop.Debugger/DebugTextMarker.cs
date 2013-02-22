// DebugTextMarker.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//
//

using System;

using Gdk;

using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using System.Linq;

namespace MonoDevelop.Debugger
{
	public abstract class DebugTextMarker : StyleTextLineMarker, IIconBarMarker
	{
		protected Mono.TextEditor.TextEditor editor;
		
		public override StyleFlag IncludedStyles {
			get {
				// check, if a message bubble is active in that line.
				if (LineSegment != null && LineSegment.Markers.Any (m => m != this && (m is IExtendingTextLineMarker)))
					return StyleFlag.None;
				return base.IncludedStyles;
			}
			set {
				base.IncludedStyles = value;
			}
		}
		
		
		public DebugTextMarker (Mono.TextEditor.TextEditor editor)
		{
			this.editor = editor;
		}


		public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
			if (!(this is CurrentDebugLineTextMarker) && LineSegment.Markers.Any (m => m is CurrentDebugLineTextMarker))
				return;
			base.Draw (editor, cr, layout, selected, startOffset, endOffset, y, startXPos, endXPos);
		}

		public void DrawIcon (Mono.TextEditor.TextEditor editor, Cairo.Context cr, DocumentLine line, int lineNumber, double x, double y, double width, double height)
		{
			double size;
			if (width > height) {
				size = height;
			} else {
				size = width;
			}
			double borderLineWidth = cr.LineWidth;
			x = Math.Floor (x + (width - borderLineWidth - size) / 2);
			y = Math.Floor (y + (height - size) / 2);

			DrawIcon (cr, x, y, size);
		}
		
		protected virtual void DrawIcon (Cairo.Context cr, double x, double y, double size)
		{
		}
		
		protected void DrawCircle (Cairo.Context cr, double x, double y, double size)
		{
			x += 0.5; y += 0.5;
			cr.NewPath ();
			cr.Arc (x + size/2, y + size / 2, (size-4)/2, 0, 2 * Math.PI);
			cr.ClosePath ();
		}
		
		protected void DrawDiamond (Cairo.Context cr, double x, double y, double size)
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
		
		protected void DrawArrow (Cairo.Context cr, double x, double y, double size)
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
		
		protected void FillGradient (Cairo.Context cr, Cairo.Color color1, Cairo.Color color2, double x, double y, double size)
		{
			using (var pat = new Cairo.LinearGradient (x + size / 4, y, x + size / 2, y + size - 4)) {
				pat.AddColorStop (0, color1);
				pat.AddColorStop (1, color2);
				cr.Pattern = pat;
				cr.FillPreserve ();
			}
		}
		
		protected void DrawBorder (Cairo.Context cr, Cairo.Color color, double x, double y, double size)
		{
			using (var pat = new Cairo.LinearGradient (x, y + size, x + size, y)) {
				pat.AddColorStop (0, color);
				cr.Pattern = pat;
				cr.Stroke ();
			}
		}

		public void MousePress (MarginMouseEventArgs args)
		{
		}
		
		public void MouseRelease (MarginMouseEventArgs args)
		{
		}
		
		public void MouseHover (MarginMouseEventArgs args)
		{
		}
	}
	
	public class BreakpointTextMarker : DebugTextMarker
	{
		public override Cairo.Color BackgroundColor {
			get { return editor.ColorStyle.BreakpointText.Background; }
			set {  }
		}
		public override Cairo.Color Color {
			get { return editor.ColorStyle.BreakpointText.Foreground; }
			set {  }
		}
		
		public bool IsTracepoint { get; set; }

		public BreakpointTextMarker (Mono.TextEditor.TextEditor editor, bool isTracePoint) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor | StyleFlag.Color;
			IsTracepoint = isTracePoint;
		}

		protected override void DrawIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color color1 = editor.ColorStyle.BreakpointMarker.GetColor ("color");
			Cairo.Color color2 = editor.ColorStyle.BreakpointMarker.GetColor ("secondcolor");
			if (IsTracepoint)
				DrawDiamond (cr, x, y, size);
			else
				DrawCircle (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, color2, x, y, size);
		}
	}
	
	public class DisabledBreakpointTextMarker: DebugTextMarker
	{
		public override Cairo.Color BackgroundColor {
			get { return editor.ColorStyle.BreakpointMarkerDisabled.GetColor ("color"); }
			set {  }
		}
	
		public DisabledBreakpointTextMarker (Mono.TextEditor.TextEditor editor, bool isTracePoint) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor;
			IsTracepoint = isTracePoint;
		}
		
		public bool IsTracepoint { get; set; }
		
		protected override void DrawIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color border = editor.ColorStyle.BreakpointText.Background;
			if (IsTracepoint)
				DrawDiamond (cr, x, y, size);
			else
				DrawCircle (cr, x, y, size);
			//FillGradient (cr, new Cairo.Color (1,1,1), new Cairo.Color (1,0.8,0.8), x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
	
	public class CurrentDebugLineTextMarker: DebugTextMarker
	{
		public override Cairo.Color BackgroundColor {
			get { return editor.ColorStyle.DebuggerCurrentLine.Background; }
			set {  }
		}
		
		public override Cairo.Color Color {
			get { return editor.ColorStyle.DebuggerCurrentLine.Foreground;  }
			set {  }
		}
		
		public CurrentDebugLineTextMarker (Mono.TextEditor.TextEditor editor) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor | StyleFlag.Color;
		}
		
		protected override void DrawIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color color1 = editor.ColorStyle.DebuggerCurrentLineMarker.GetColor ("color");
			Cairo.Color color2 = editor.ColorStyle.DebuggerCurrentLineMarker.GetColor ("secondcolor");
			Cairo.Color border = editor.ColorStyle.DebuggerCurrentLineMarker.GetColor ("bordercolor");

			DrawArrow (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
	
	public class DebugStackLineTextMarker: DebugTextMarker
	{
		public override Cairo.Color BackgroundColor {
			get { return editor.ColorStyle.DebuggerStackLine.Background; }
			set {  }
		}
		
		public override Cairo.Color Color {
			get { return editor.ColorStyle.DebuggerStackLine.Foreground;  }
			set {  }
		}
		
		public DebugStackLineTextMarker (Mono.TextEditor.TextEditor editor) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor | StyleFlag.Color;
		}
		
		protected override void DrawIcon (Cairo.Context cr, double x, double y, double size)
		{
			Cairo.Color color1 = editor.ColorStyle.DebuggerStackLineMarker.GetColor ("color");
			Cairo.Color color2 = editor.ColorStyle.DebuggerStackLineMarker.GetColor ("secondcolor");
			Cairo.Color border = editor.ColorStyle.DebuggerStackLineMarker.GetColor ("bordercolor");

			DrawArrow (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
	
	public class InvalidBreakpointTextMarker: DebugTextMarker
	{
		public override Cairo.Color BackgroundColor {
			get { return editor.ColorStyle.BreakpointTextInvalid.Background; }
			set {  }
		}
		
		public InvalidBreakpointTextMarker (Mono.TextEditor.TextEditor editor) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor;
		}
		
		protected override void DrawIcon (Cairo.Context cr, double x, double y, double size)
		{	Cairo.Color color1 = editor.ColorStyle.InvalidBreakpointMarker.GetColor ("color");
			Cairo.Color color2 = color1;
			Cairo.Color border = editor.ColorStyle.InvalidBreakpointMarker.GetColor ("secondcolor");
			DrawCircle (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
}
