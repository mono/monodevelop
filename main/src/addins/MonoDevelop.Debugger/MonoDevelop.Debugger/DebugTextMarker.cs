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

namespace MonoDevelop.Debugger
{
	public abstract class DebugTextMarker : StyleTextMarker, IIconBarMarker
	{
		protected Mono.TextEditor.TextEditor editor;
		
		public DebugTextMarker (Mono.TextEditor.TextEditor editor)
		{
			this.editor = editor;
		}
		
		public void DrawIcon (Mono.TextEditor.TextEditor editor, Gdk.Drawable win, LineSegment line, int lineNumber, int x, int y, int width, int height)
		{
			int size;
			if (width > height) {
				x += (width - height) / 2;
				size = height;
			} else {
				y += (height - width) / 2;
				size = width;
			}
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (win)) {
				DrawIcon (cr, x, y, size);
			}
		}
		
		protected virtual void DrawIcon (Cairo.Context cr, int x, int y, int size)
		{
		}
		
		protected void DrawCircle (Cairo.Context cr, double x, double y, int size)
		{
			x += 0.5; y += 0.5;
			cr.NewPath ();
			cr.Arc (x + size/2, y + size / 2, (size-4)/2, 0, 2 * Math.PI);
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
		
		protected void FillGradient (Cairo.Context cr, Cairo.Color color1, Cairo.Color color2, int x, int y, int size)
		{
			Cairo.Gradient pat = new Cairo.LinearGradient (x + size / 4, y, x + size / 2, y + size - 4);
			pat.AddColorStop (0, color1);
			pat.AddColorStop (1, color2);
			cr.Pattern = pat;
			cr.FillPreserve ();
		}
		
		protected void DrawBorder (Cairo.Context cr, Cairo.Color color, int x, int y, int size)
		{
			Cairo.Gradient pat = new Cairo.LinearGradient (x, y + size, x + size, y);
			pat.AddColorStop (0, color);
			cr.Pattern = pat;
			cr.LineWidth = 1;
			cr.Stroke ();
		}

		public void MousePress (MarginMouseEventArgs args)
		{
		}
		
		public void MouseRelease (MarginMouseEventArgs args)
		{
		}
	}
	
	public class BreakpointTextMarker : DebugTextMarker
	{
		public override Color BackgroundColor {
			get { return editor.ColorStyle.BreakpointBg; }
			set {  }
		}
		public override Color Color {
			get { return editor.ColorStyle.BreakpointFg; }
			set {  }
		}


		public BreakpointTextMarker (Mono.TextEditor.TextEditor editor) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor | StyleFlag.Color;
		}
		
		protected override void DrawIcon (Cairo.Context cr, int x, int y, int size)
		{
			Cairo.Color color1 = Style.ToCairoColor (editor.ColorStyle.BreakpointMarkerColor1);
			Cairo.Color color2 = Style.ToCairoColor (editor.ColorStyle.BreakpointMarkerColor2);
			DrawCircle (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, color2, x, y, size);
		}
	}
	
	public class DisabledBreakpointTextMarker: DebugTextMarker
	{
		public override Color BackgroundColor {
			get { return editor.ColorStyle.DisabledBreakpointBg; }
			set {  }
		}
	
		public DisabledBreakpointTextMarker (Mono.TextEditor.TextEditor editor) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor;
		}
		
		protected override void DrawIcon (Cairo.Context cr, int x, int y, int size)
		{
			Cairo.Color border = Style.ToCairoColor (editor.ColorStyle.InvalidBreakpointMarkerBorder);
			DrawCircle (cr, x, y, size);
			//FillGradient (cr, new Cairo.Color (1,1,1), new Cairo.Color (1,0.8,0.8), x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
	
	public class CurrentDebugLineTextMarker: DebugTextMarker
	{
		public override Color BackgroundColor {
			get { return editor.ColorStyle.CurrentDebugLineBg; }
			set {  }
		}
		
		public override Color Color {
			get { return editor.ColorStyle.CurrentDebugLineFg;  }
			set {  }
		}
		
		public CurrentDebugLineTextMarker (Mono.TextEditor.TextEditor editor) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor | StyleFlag.Color;
		}
		
		protected override void DrawIcon (Cairo.Context cr, int x, int y, int size)
		{
			Cairo.Color color1 = Style.ToCairoColor (editor.ColorStyle.CurrentDebugLineMarkerColor1);
			Cairo.Color color2 = Style.ToCairoColor (editor.ColorStyle.CurrentDebugLineMarkerColor2);
			Cairo.Color border = Style.ToCairoColor (editor.ColorStyle.CurrentDebugLineMarkerBorder);
		
			DrawArrow (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
	
	public class InvalidBreakpointTextMarker: DebugTextMarker
	{
		public override Color BackgroundColor {
			get { return editor.ColorStyle.InvalidBreakpointBg; }
			set {  }
		}
		
		public InvalidBreakpointTextMarker (Mono.TextEditor.TextEditor editor) : base (editor)
		{
			IncludedStyles |= StyleFlag.BackgroundColor;
		}
		
		protected override void DrawIcon (Cairo.Context cr, int x, int y, int size)
		{
			Cairo.Color color1 = Style.ToCairoColor (editor.ColorStyle.InvalidBreakpointMarkerColor1);
			Cairo.Color color2 = color1;
			Cairo.Color border = Style.ToCairoColor (editor.ColorStyle.InvalidBreakpointMarkerBorder);
			DrawCircle (cr, x, y, size);
			FillGradient (cr, color1, color2, x, y, size);
			DrawBorder (cr, border, x, y, size);
		}
	}
}
