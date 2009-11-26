// 
// ErrorTextMarker.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.SourceEditor
{
	public class ErrorTextMarker : TextMarker, IBackgroundMarker, IIconBarMarker, IExtendingTextMarker, IDisposable
	{
		const int border = 4;
		Gdk.Pixbuf iconPixbuf;
//		bool fitCalculated = false;
		bool fitsInSameLine = true;
		public bool IsError { get; set; }
		public bool IsExpanded { 
			get {
				return !task.Completed;
			}
			set {
				task.Completed = !value;
			}
		}
		public string ErrorMessage { get; set; }
		Task task;
		LineSegment lineSegment;
		
		public int GetLineHeight (TextEditor editor) 
		{
			if (!IsExpanded || DebuggingService.IsDebugging) 
				return editor.LineHeight;
			CalculateLineFit (editor, editor.TextViewMargin.GetLayout (lineSegment).Layout);
			return fitsInSameLine ? editor.LineHeight : editor.LineHeight * 2;
		}
		
		void CalculateLineFit (TextEditor editor, Pango.Layout textLayout)
		{
			int textWidth, textHeight;
			textLayout.GetPixelSize (out textWidth, out textHeight);
			
			EnsureLayoutCreated (editor);
			fitsInSameLine = editor.TextViewMargin.XOffset + textWidth + layoutWidth + iconPixbuf.Width + border + editor.LineHeight / 2  < editor.Allocation.Width;
		}
		
		public ErrorTextMarker (Task task, LineSegment lineSegment, bool isError, string errorMessage)
		{
			this.task = task;
			this.IsExpanded = true;
			this.lineSegment = lineSegment;
			this.IsError = isError;
			this.ErrorMessage = errorMessage;
			iconPixbuf = MonoDevelop.Core.Gui.ImageService.GetPixbuf (isError ? MonoDevelop.Core.Gui.Stock.Error : MonoDevelop.Core.Gui.Stock.Warning, Gtk.IconSize.Menu);
		}
		
		public void DisposeLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			if (fontDescription != null) {
				fontDescription.Dispose ();
				fontDescription = null;
			}
			if (gc != null) {
				gc.Dispose ();
				gc = null;
			}
		}
		
		public void Dispose ()
		{
			DisposeLayout ();
		}
		
		Gdk.GC gc;
		Pango.Layout layout;
		Pango.FontDescription fontDescription;
		int layoutWidth, layoutHeight;
		
		Cairo.Color lightBg;
		Cairo.Color darkBg;
		
		Cairo.Color lightBg2;
		Cairo.Color darkBg2;
		
		Cairo.Color topLine;
		Cairo.Color bottomLine;
		
		void EnsureLayoutCreated (TextEditor editor)
		{
			if (editor.ColorStyle != null && gc == null) {
				gc = new Gdk.GC (editor.GdkWindow);
				gc.RgbFgColor = editor.ColorStyle.GetChunkStyle (IsError ? "error.text" : "warning.text").Color;
				
				lightBg = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (IsError ? "error.light.color1" : "warning.light.color1").Color);
				darkBg  = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (IsError ? "error.light.color2" : "warning.light.color2").Color);
			
				lightBg2 = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (IsError ? "error.dark.color1" : "warning.dark.color1").Color);
				darkBg2  = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (IsError ? "error.dark.color2" : "warning.dark.color2").Color);
			
				topLine = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (IsError ? "error.line.top" : "warning.line.top").Color);
				bottomLine = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (IsError ? "error.line.bottom" : "warning.line.bottom").Color);
			}
			
			if (layout != null)
				return;
			layout = new Pango.Layout (editor.PangoContext);
			fontDescription = Pango.FontDescription.FromString (editor.Options.FontName);
			fontDescription.Family = "Sans";
			fontDescription.Size = (int)(fontDescription.Size * 0.8f * editor.Options.Zoom);
			layout.FontDescription = fontDescription;
			layout.SetText (ErrorMessage);
			
			layout.GetPixelSize (out layoutWidth, out layoutHeight);
			
		}
		
		public bool DrawBackground (TextEditor editor, Gdk.Drawable win, Pango.Layout layout2, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos, ref bool drawBg)
		{
			if (!IsExpanded || DebuggingService.IsDebugging) 
				return true;
			
			EnsureLayoutCreated (editor);
			CalculateLineFit (editor, layout2);
			int x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			
			int x2 = System.Math.Max (right - layoutWidth - border - iconPixbuf.Width, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			
			using (var g = Gdk.CairoHelper.Create (win)) {
				if (!fitsInSameLine) {
					bool isEolSelected = editor.IsSomethingSelected ? editor.SelectionRange.Contains (lineSegment.EndOffset) : false;
					if (isEolSelected)
						x2 = editor.Allocation.Width;
					DrawRectangle (g, x, y + editor.LineHeight, x2, editor.LineHeight);
					g.Color = Mono.TextEditor.Highlighting.Style.ToCairoColor (isEolSelected ? editor.ColorStyle.Selection.BackgroundColor : editor.ColorStyle.Default.BackgroundColor);
					g.Fill ();
				}
				DrawRectangle (g, x, y, right, editor.LineHeight);
				Cairo.Gradient pat = new Cairo.LinearGradient (x, y, x, y + editor.LineHeight);
				pat.AddColorStop (0, lightBg);
				pat.AddColorStop (1, darkBg);
				g.Pattern = pat;
				g.Fill ();
				
				g.MoveTo (new Cairo.PointD (x, y + 0.5));
				g.LineTo (new Cairo.PointD (x + right, y + 0.5));
				g.Color = topLine;
				g.LineWidth = 1;
				g.Stroke ();
				
				g.MoveTo (new Cairo.PointD (x, y + editor.LineHeight - 0.5));
				g.LineTo (new Cairo.PointD ((fitsInSameLine ? x + right : x2), y + editor.LineHeight - 0.5));
				g.Color = bottomLine;
				g.LineWidth = 1;
				g.Stroke ();
			}
			
			if (!fitsInSameLine) 
				y += editor.LineHeight;
			double y2       = fitsInSameLine ? y + 0.5 : y - 0.5;
			double y2Bottom = fitsInSameLine ? y2 + editor.LineHeight  - 1 : y2 + editor.LineHeight;
			using (var g = Gdk.CairoHelper.Create (win)) {
				
				g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
				if (fitsInSameLine) {
					g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
				}
				
				g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
				g.LineTo (new Cairo.PointD (right, y2Bottom));
				g.LineTo (new Cairo.PointD (right, y2));
				if (fitsInSameLine)
					g.ClosePath ();
				
				Cairo.Gradient pat = new Cairo.LinearGradient (x, y, x, y2Bottom);
				pat.AddColorStop (0, fitsInSameLine ? lightBg2 : darkBg);
				pat.AddColorStop (1, fitsInSameLine ? darkBg2 : lightBg);
				g.Pattern = pat;
				g.FillPreserve ();
				g.Color = bottomLine;
				g.LineWidth = 1;
				g.Stroke ();
			}
			win.DrawLayout (gc, x2 + iconPixbuf.Width + border, y + (editor.LineHeight - layoutHeight) / 2, layout);
			
			win.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), 
			                iconPixbuf, 
			                0, 0, 
			                x2, y + (editor.LineHeight - iconPixbuf.Height) / 2, 
			                iconPixbuf.Width, iconPixbuf.Height, 
			                Gdk.RgbDither.None, 0, 0);
			
			return true;
		}
		
		/*
		static double min (params double[] arr)
		{
			int minp = 0;
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] < arr[minp])
					minp = i;
			return arr[minp];
		}*/
		
		static void DrawRectangle (Cairo.Context g, int x, int y, int width, int height)
		{
			int right = x + width;
			int bottom = y + height;
			g.MoveTo (new Cairo.PointD (x, y));
			g.LineTo (new Cairo.PointD (right, y));
			g.LineTo (new Cairo.PointD (right, bottom));
			g.LineTo (new Cairo.PointD (x, bottom));
			g.LineTo (new Cairo.PointD (x, y));
			g.ClosePath ();
		}
		#region IIconBarMarker implementation
		
		public void DrawIcon (Mono.TextEditor.TextEditor editor, Gdk.Drawable win, LineSegment line, int lineNumber, int x, int y, int width, int height)
		{
			if (DebuggingService.IsDebugging) 
				return;
			win.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), 
			                iconPixbuf, 
			                0, 0, 
			                x + (width - iconPixbuf.Width) / 2, y + (height - iconPixbuf.Height) / 2, 
			                iconPixbuf.Width, iconPixbuf.Height, 
			                Gdk.RgbDither.None, 0, 0);
		}
		
		public void MousePress (MarginMouseEventArgs args)
		{
			if (DebuggingService.IsDebugging) 
				return;
			IsExpanded = !IsExpanded;
			args.Editor.Repaint ();
			MonoDevelop.Ide.Gui.Pads.ErrorListPad pad = MonoDevelop.Ide.Gui.IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ().Content as MonoDevelop.Ide.Gui.Pads.ErrorListPad;
			pad.Control.QueueDraw ();
		}
		
		public void MouseRelease (MarginMouseEventArgs args)
		{
		}
		
		#endregion
		/*
		static void  DrawRoundedRectangle (Cairo.Context gr, double x, double y, double width, double height, double radius)
		{
			gr.Save ();
			
			if ((radius > height / 2) || (radius > width / 2))
				radius = min (height / 2, width / 2);
			
			gr.MoveTo (x, y + radius);
			gr.Arc (x + radius, y + radius, radius, System.Math.PI, -System.Math.PI / 2);
			gr.LineTo (x + width - radius, y);
			gr.Arc (x + width - radius, y + radius, radius, -System.Math.PI / 2, 0);
			gr.LineTo (x + width, y + height - radius);
			gr.Arc (x + width - radius, y + height - radius, radius, 0, System.Math.PI / 2);
			gr.LineTo (x + radius, y + height);
			gr.Arc (x + radius, y + height - radius, radius, System.Math.PI / 2, System.Math.PI);
			gr.ClosePath ();
			gr.Restore ();
		}*/
	}
}
