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
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Tasks;
using System.Collections.Generic;

namespace MonoDevelop.SourceEditor
{
	public class ErrorText
	{
		public bool IsError { get; set; }
		public string ErrorMessage { get; set; }
		
		public ErrorText (bool isError, string errorMessage)
		{
			this.IsError = isError;
			this.ErrorMessage = errorMessage;
		}
		
		public override string ToString ()
		{
			return string.Format ("[ErrorText: IsError={0}, ErrorMessage={1}]", IsError, ErrorMessage);
		}
	}
	
	public class ErrorTextMarker : TextMarker, IBackgroundMarker, IIconBarMarker, IExtendingTextMarker, IDisposable
	{
		const int border = 4;
		Gdk.Pixbuf errorPixbuf;
		Gdk.Pixbuf warningPixbuf;
//		bool fitCalculated = false;
		bool fitsInSameLine = true;
		public bool IsExpanded { 
			get {
				return !task.Completed;
			}
			set {
				task.Completed = !value;
			}
		}
		
		List<ErrorText> errors = new List<ErrorText> ();
		
		Task task;
		LineSegment lineSegment;
		
		public int GetLineHeight (TextEditor editor) 
		{
			if (!IsExpanded || DebuggingService.IsDebugging) 
				return editor.LineHeight; 
			CalculateLineFit (editor, editor.TextViewMargin.GetLayout (lineSegment).Layout);
			int height = editor.LineHeight * errors.Count;
			if (!fitsInSameLine)
				height += editor.LineHeight;
			return height;
		}
		
		void CalculateLineFit (TextEditor editor, Pango.Layout textLayout)
		{
			int textWidth, textHeight;
			textLayout.GetPixelSize (out textWidth, out textHeight);
			
			EnsureLayoutCreated (editor);
			fitsInSameLine = editor.TextViewMargin.XOffset + textWidth + layouts[0].Width + errorPixbuf.Width + border + editor.LineHeight / 2  < editor.Allocation.Width;
		}
		
		public ErrorTextMarker (Task task, LineSegment lineSegment, bool isError, string errorMessage)
		{
			this.task = task;
			this.IsExpanded = true;
			this.lineSegment = lineSegment;
			errors.Add (new ErrorText (isError, errorMessage));
			errorPixbuf = MonoDevelop.Core.Gui.ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.Error, Gtk.IconSize.Menu);
			warningPixbuf = MonoDevelop.Core.Gui.ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.Warning, Gtk.IconSize.Menu);
		}
		
		public void AddError (bool isError, string errorMessage)
		{
			errors.Add (new ErrorText (isError, errorMessage));
			DisposeLayout ();
		}
		
		public void DisposeLayout ()
		{
			if (layouts != null) {
				layouts.ForEach (l => l.Layout.Dispose ());
				layouts = null;
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
		
		class LayoutDescriptor 
		{
			public Pango.Layout Layout { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }
			
			public LayoutDescriptor (Pango.Layout layout, int width, int height)
			{
				this.Layout = layout;
				this.Width = width;
				this.Height = height;
			}
		}
		
		Gdk.GC gc;
		List<LayoutDescriptor> layouts;
		Pango.FontDescription fontDescription;
		
		Cairo.Color errorLightBg;
		Cairo.Color errorDarkBg;
		Cairo.Color errorLightBg2;
		Cairo.Color errorDarkBg2;

		Cairo.Color warningLightBg;
		Cairo.Color warningDarkBg;
		Cairo.Color warningLightBg2;
		Cairo.Color warningDarkBg2;
		
		Cairo.Color topLine;
		Cairo.Color bottomLine;
		
		void EnsureLayoutCreated (TextEditor editor)
		{
			if (editor.ColorStyle != null && gc == null) {
				bool isError = errors.Any (e => e.IsError);
				gc = new Gdk.GC (editor.GdkWindow);
				gc.RgbFgColor = editor.ColorStyle.GetChunkStyle (isError ? "error.text" : "warning.text").Color;
				
				errorLightBg = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("error.light.color1").Color);
				errorDarkBg  = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("error.light.color2").Color);
			
				errorLightBg2 = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("error.dark.color1").Color);
				errorDarkBg2  = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("error.dark.color2").Color);
			
				warningLightBg = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("warning.light.color1").Color);
				warningDarkBg  = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("warning.light.color2").Color);
			
				warningLightBg2 = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("warning.dark.color1").Color);
				warningDarkBg2  = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("warning.dark.color2").Color);
			
				topLine = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (isError ? "error.line.top" : "warning.line.top").Color);
				bottomLine = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle (isError ? "error.line.bottom" : "warning.line.bottom").Color);
			}
			
			if (layouts != null)
				return;
			layouts = new List<LayoutDescriptor> ();
			fontDescription = Pango.FontDescription.FromString (editor.Options.FontName);
			fontDescription.Family = "Sans";
			fontDescription.Size = (int)(fontDescription.Size * 0.8f * editor.Options.Zoom);
			
			foreach (ErrorText errorText in errors) {
				Console.WriteLine (errorText);
				Pango.Layout layout = new Pango.Layout (editor.PangoContext);
				layout.FontDescription = fontDescription;
				layout.SetText (errorText.ErrorMessage);
				
				int layoutWidth, layoutHeight;
				layout.GetPixelSize (out layoutWidth, out layoutHeight);
				layouts.Add (new LayoutDescriptor (layout, layoutWidth, layoutHeight));
			}
		}
		
		public bool DrawBackground (TextEditor editor, Gdk.Drawable win, Pango.Layout layout2, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos, ref bool drawBg)
		{
			if (!IsExpanded || DebuggingService.IsDebugging) 
				return true;
			
			EnsureLayoutCreated (editor);
			CalculateLineFit (editor, layout2);
			int x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			
			int x2 = System.Math.Max (right - layouts[0].Width - border - errorPixbuf.Width, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			bool isEolSelected = editor.IsSomethingSelected ? editor.SelectionRange.Contains (lineSegment.EndOffset) : false;
			bool isError = errors.Any (e => e.IsError);
			
			using (var g = Gdk.CairoHelper.Create (win)) {
				if (!fitsInSameLine) {
					if (isEolSelected)
						x2 = editor.Allocation.Width;
					DrawRectangle (g, x, y + editor.LineHeight, x2, editor.LineHeight);
					g.Color = Mono.TextEditor.Highlighting.Style.ToCairoColor (isEolSelected ? editor.ColorStyle.Selection.BackgroundColor : editor.ColorStyle.Default.BackgroundColor);
					g.Fill ();
				}
				DrawRectangle (g, x, y, right, editor.LineHeight);
				Cairo.Gradient pat = new Cairo.LinearGradient (x, y, x, y + editor.LineHeight);
				
				pat.AddColorStop (0, isError ? errorLightBg : warningLightBg);
				pat.AddColorStop (1, isError ? errorDarkBg : warningDarkBg);
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
				if (isError) {
					pat.AddColorStop (0, fitsInSameLine ? errorLightBg2 : errorDarkBg);
					pat.AddColorStop (1, fitsInSameLine ? errorDarkBg2 : errorLightBg);
				} else {
					pat.AddColorStop (0, fitsInSameLine ? warningLightBg2 : warningDarkBg);
					pat.AddColorStop (1, fitsInSameLine ? warningDarkBg2 : warningLightBg);
				}
				g.Pattern = pat;
				g.FillPreserve ();
				g.Color = bottomLine;
				g.LineWidth = 1;
				g.Stroke ();
			}
			for (int i = 0; i < layouts.Count; i++) {
				LayoutDescriptor layout = layouts[i];
				x2 = System.Math.Max (right - layout.Width - border - errorPixbuf.Width, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
				if (i > 0) {
					editor.TextViewMargin.DrawRectangleWithRuler (win, x, new Gdk.Rectangle (x, y, right, editor.LineHeight), isEolSelected ? editor.ColorStyle.Selection.BackgroundColor : editor.ColorStyle.Default.BackgroundColor, true);
					if (!isEolSelected) {
						using (var g = Gdk.CairoHelper.Create (win)) {
							g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
							g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
							g.LineTo (new Cairo.PointD (right, y + editor.LineHeight));
							g.LineTo (new Cairo.PointD (right, y));
							g.ClosePath ();
							
							Cairo.Gradient pat = new Cairo.LinearGradient (x, y, x, y + editor.LineHeight);
							pat.AddColorStop (0, errors[i].IsError ? errorLightBg : warningLightBg);
							pat.AddColorStop (1, errors[i].IsError ? errorDarkBg : warningDarkBg);
							g.Pattern = pat;
							g.Fill ();
						}
					}
				}
				win.DrawLayout (gc, x2 + errorPixbuf.Width + border, y + (editor.LineHeight - layout.Height) / 2, layout.Layout);
				win.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), 
				                errors[i].IsError ? errorPixbuf : warningPixbuf, 
				                0, 0, 
				                x2, y + (editor.LineHeight - errorPixbuf.Height) / 2, 
				                errorPixbuf.Width, errorPixbuf.Height, 
				                Gdk.RgbDither.None, 0, 0);
				y += editor.LineHeight;
			}
			
			
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
			                errors.Any (e => e.IsError) ? errorPixbuf : warningPixbuf, 
			                0, 0, 
			                x + (width - errorPixbuf.Width) / 2, y + (height - errorPixbuf.Height) / 2, 
			                errorPixbuf.Width, errorPixbuf.Height, 
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
