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
using MonoDevelop.Ide;
using System.Text.RegularExpressions;

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
	
	public class MessageBubbleTextMarker : TextMarker, IBackgroundMarker, IIconBarMarker, IExtendingTextMarker, IDisposable, IActionTextMarker
	{
		const int border = 4;
		internal Gdk.Pixbuf errorPixbuf;
		internal Gdk.Pixbuf warningPixbuf;
//		bool fitCalculated = false;
		bool fitsInSameLine = true;
		
		TextEditor editor;
		
		public bool IsExpanded {
			get {
				return !task.Completed;
			}
			set {
				task.Completed = !value;
			}
		}
		
		bool collapseExtendedErrors;
		public bool CollapseExtendedErrors {
			get { return collapseExtendedErrors; }
			set {
				collapseExtendedErrors = value;
				int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
				if (collapseExtendedErrors) {
					editor.Document.UnRegisterVirtualTextMarker (this);
				} else {
					for (int i = 1; i < errors.Count; i++) 
						editor.Document.RegisterVirtualTextMarker (lineNumber + i, this);
				}
				editor.Document.CommitMultipleLineUpdate (lineNumber, lineNumber + errors.Count);
			}
		}
		
		public bool UseVirtualLines {
			get;
			set;
		}
		
		List<ErrorText> errors = new List<ErrorText> ();
		internal IList<ErrorText> Errors {
			get {
				return errors;
			}
		}
		
		Task task;
		LineSegment lineSegment;
		int editorAllocHeight = -1, lastLineLength = -1;
		int lastHeight = 0;
		
		public int GetLineHeight (TextEditor editor)
		{
			if (!IsExpanded || DebuggingService.IsDebugging)
				return editor.LineHeight;
			
			if (editorAllocHeight == editor.Allocation.Width && lastLineLength == lineSegment.EditableLength)
				return lastHeight;
			
			CalculateLineFit (editor, lineSegment);
			int height;
			if (CollapseExtendedErrors) {
				height = editor.LineHeight;
			} else {
				// TODO: Insert virtual lines, if required
				height = UseVirtualLines ? editor.LineHeight * errors.Count : editor.LineHeight;
			}
			
			if (!fitsInSameLine)
				height += editor.LineHeight;
			
			editorAllocHeight = editor.Allocation.Height;
			lastLineLength = lineSegment.EditableLength;
			lastHeight = height;
			
			return height;
		}
		
		static Dictionary<string, KeyValuePair<int, int>> textWidthDictionary = new Dictionary<string, KeyValuePair<int, int>>();
		static Dictionary<LineSegment, KeyValuePair<int, int>> lineWidthDictionary = new Dictionary<LineSegment, KeyValuePair<int, int>>();
		
		void CalculateLineFit(TextEditor editor, LineSegment lineSegment)
		{
			KeyValuePair<int, int> textSize;
			if (!lineWidthDictionary.TryGetValue(lineSegment, out textSize)) {
				Pango.Layout textLayout = editor.TextViewMargin.GetLayout(lineSegment).Layout;
				int textWidth, textHeight;
				textLayout.GetPixelSize (out textWidth, out textHeight);
				textSize = new KeyValuePair<int, int> (textWidth, textHeight);
				if (textWidthDictionary.Count > 10000) {
					textWidthDictionary.Clear ();
				}
				lineWidthDictionary[lineSegment] = textSize;
			} 
			EnsureLayoutCreated (editor);
			fitsInSameLine = editor.TextViewMargin.XOffset + textSize.Key + LayoutWidth + errorPixbuf.Width + border + editor.LineHeight / 2  < editor.Allocation.Width;
		}
		
		string initialText;
		public MessageBubbleTextMarker (TextEditor editor, Task task, LineSegment lineSegment, bool isError, string errorMessage)
		{
			this.editor = editor;
			this.task = task;
			this.IsExpanded = true;
			this.lineSegment = lineSegment;
			this.initialText = editor.Document.GetTextAt (lineSegment);
			AddError (isError, errorMessage);
			
			errorPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
			warningPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);
		}
		
		static Regex mcsErrorFormat = new Regex ("(.+)\\(CS\\d+\\)\\Z");
		public void AddError (bool isError, string errorMessage)
		{
			Match match = mcsErrorFormat.Match (errorMessage);
			if (match.Success)
				errorMessage = match.Groups[1].Value;
			errors.Add (new ErrorText (isError, errorMessage));
			CollapseExtendedErrors = errors.Count > 1;
			DisposeLayout ();
		}

		public static bool RemoveLine (LineSegment line, out int oldHeight)
		{
			KeyValuePair<int, int> result;
			if (!lineWidthDictionary.TryGetValue (line, out result)) {
				oldHeight = -1;
				return false;
			}
			oldHeight = result.Value;
			lineWidthDictionary.Remove (line);
			return true;
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
			if (gcLight != null) {
				gcLight.Dispose ();
				gcLight = null;
			}
			if (errorCountLayout != null) {
				errorCountLayout.Dispose ();
				errorCountLayout = null;
			}
		}
		
		public void Dispose ()
		{
			DisposeLayout ();
		}
		
		internal class LayoutDescriptor 
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
		
		internal Gdk.GC gc, gcLight;
		Pango.Layout errorCountLayout;
		List<LayoutDescriptor> layouts;
		internal IList<LayoutDescriptor> Layouts {
			get {
				return layouts;
			}
		}
		Pango.FontDescription fontDescription;
		internal Cairo.Color[,,,] colorMatrix = new Cairo.Color [2,2,3,2];
		void EnsureLayoutCreated (TextEditor editor)
		{
			if (editor.ColorStyle != null && gc == null) {
				bool isError = errors.Any (e => e.IsError);
				
				string typeString = isError ? "error" : "warning";
				
				gc = new Gdk.GC (editor.GdkWindow);
				gc.RgbFgColor = editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".text").Color;
				gcLight = new Gdk.GC (editor.GdkWindow);
				gcLight.RgbFgColor = new Gdk.Color (255, 255, 255);
				
				colorMatrix[0, 0, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".light.color1").Color);
				colorMatrix[0, 1, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".light.color2").Color);
				
				colorMatrix[0, 0, 1, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".dark.color1").Color);
				colorMatrix[0, 1, 1, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".dark.color2").Color);
				
				colorMatrix[0, 0, 2, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".line.top").Color);
				colorMatrix[0, 1, 2, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".line.bottom").Color);
				
				colorMatrix[1, 0, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".light.color1").Color);
				colorMatrix[1, 1, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".light.color2").Color);
				
				colorMatrix[1, 0, 1, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".dark.color1").Color);
				colorMatrix[1, 1, 1, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".dark.color2").Color);
				
				colorMatrix[1, 0, 2, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".line.top").Color);
				colorMatrix[1, 1, 2, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".line.bottom").Color);
				
				double factor = 1.03;
				for (int i = 0; i < 2; i++) {
					for (int j = 0; j < 2; j++) {
						for (int k = 0; k < 3; k++) {
							HslColor color = colorMatrix [i, j, k, 0];
							color.L *= factor;
							colorMatrix [i, j, k, 1] = color;
						}
					}
				}
			}
			
			if (layouts != null)
				return;
			layouts = new List<LayoutDescriptor> ();
			fontDescription = Pango.FontDescription.FromString (editor.Options.FontName);
			fontDescription.Family = "Sans";
			fontDescription.Size = (int)(fontDescription.Size * 0.8f * editor.Options.Zoom);
			foreach (ErrorText errorText in errors) {
				Pango.Layout layout = new Pango.Layout (editor.PangoContext);
				layout.FontDescription = fontDescription;
				layout.SetText (errorText.ErrorMessage);
				
				KeyValuePair<int, int> textSize;
				if (!textWidthDictionary.TryGetValue (errorText.ErrorMessage, out textSize)) {
					int w, h;
					layout.GetPixelSize (out w, out h);
					textSize = new KeyValuePair<int, int> (w, h);
					textWidthDictionary[errorText.ErrorMessage] = textSize;
				}
				layouts.Add (new LayoutDescriptor (layout, textSize.Key, textSize.Value));
			}
			
			if (errorCountLayout == null && errors.Count > 1) {
				errorCountLayout = new Pango.Layout (editor.PangoContext);
				errorCountLayout.FontDescription = fontDescription;
				errorCountLayout.SetText (errors.Count.ToString ());
			}
		}
		const int LIGHT = 0;
		const int DARK  = 1;
		const int LINE  = 2;
		
		const int TOP = 0;
		const int BOTTOM = 1;
		
		bool ShowIconsInBubble = false;
		int LayoutWidth {
			get {
				if (!CollapseExtendedErrors && errors.Count > 1)
					return layouts.Max (l => l.Width);
				return layouts[0].Width;
			}
		}
		public bool DrawBackground (TextEditor editor, Gdk.Drawable win, Pango.Layout layout2, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos, ref bool drawBg)
		{
			if (!IsExpanded || DebuggingService.IsDebugging) 
				return true;
			EnsureLayoutCreated (editor);
//			CalculateLineFit (editor, layout2);
			int x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			int errorCounterWidth = 0;
			bool isCaretInLine = startOffset <= editor.Caret.Offset && editor.Caret.Offset <= endOffset;
			int ew = 0, eh = 0;
			if (errors.Count > 1 && errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out ew, out eh);
				errorCounterWidth = ew + 10;
			}
			
			int x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? errorPixbuf.Width : 0) - errorCounterWidth, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset  + lineSegment.EditableLength) : false;
			int active = editor.HasFocus ? 0 : 1;
			if (editor.Document.GetTextAt (lineSegment) != initialText)
				active = 1;
			int highlighted = active == 0 && isCaretInLine ? 1 : 0;
			
			
			using (var g = Gdk.CairoHelper.Create (win)) {
				if (!fitsInSameLine) {
					if (isEolSelected)
						x2 = editor.Allocation.Width;
					editor.TextViewMargin.DrawRectangleWithRuler (win, x, new Gdk.Rectangle (x, y + editor.LineHeight, x2, editor.LineHeight), isEolSelected ? editor.ColorStyle.Selection.BackgroundColor : editor.ColorStyle.Default.BackgroundColor, true);
				}
				DrawRectangle (g, x, y, right, editor.LineHeight / 2);
				g.Color = colorMatrix[active, TOP, LIGHT, highlighted];
				g.Fill ();
				DrawRectangle (g, x, y + editor.LineHeight / 2, right, editor.LineHeight / 2);
				g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted];
				g.Fill ();
				
				g.MoveTo (new Cairo.PointD (x, y + 0.5));
				g.LineTo (new Cairo.PointD (x + right, y + 0.5));
				g.Color = colorMatrix[active, TOP, LINE, highlighted];
				g.LineWidth = 1;
				g.Stroke ();
				
				g.MoveTo (new Cairo.PointD (x, y + editor.LineHeight - 0.5));
				g.LineTo (new Cairo.PointD ((fitsInSameLine ? x + right : x2), y + editor.LineHeight - 0.5));
				g.Color = colorMatrix[active, BOTTOM, LINE, highlighted];
				g.LineWidth = 1;
				g.Stroke ();
				if (editor.Options.ShowRuler) {
					int divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
					g.MoveTo (new Cairo.PointD (divider + 0.5, y));
					g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
					g.Color = colorMatrix[active, BOTTOM, LINE, highlighted];
					g.LineWidth = 1;
					g.Stroke ();
				}
			}
			
			if (!fitsInSameLine) 
				y += editor.LineHeight;
			double y2       = fitsInSameLine ? y + 0.5 : y - 0.5;
			double y2Bottom = fitsInSameLine ? y2 + editor.LineHeight  - 1 : y2 + editor.LineHeight;
			// draw message text background
			using (var g = Gdk.CairoHelper.Create (win)) {
				if (CollapseExtendedErrors || errors.Count == 1) {
					if (!fitsInSameLine) { // draw box
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
						g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2 - 1));
						g.ClosePath ();
						g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted];
						g.Fill ();
						
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
						g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.Color = colorMatrix[active, BOTTOM, LINE, highlighted];
						g.LineWidth = 1;
						g.Stroke ();
					} else { // draw 'arrow marker'
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
						double mid = y2 + editor.LineHeight / 2;
						g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, mid));
						
						g.LineTo (new Cairo.PointD (right, mid));
						g.LineTo (new Cairo.PointD (right, y2));
						g.ClosePath ();
						g.Color = colorMatrix[active, TOP, DARK, highlighted];
						g.Fill ();
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, mid));
						
						g.LineTo (new Cairo.PointD (right, mid));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.ClosePath ();
						
						g.Color = colorMatrix[active, BOTTOM, DARK, highlighted];
						g.Fill ();
						
						// draw border
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
						g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
						
						g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2));
						g.ClosePath ();
	
						g.Color = colorMatrix[active, BOTTOM, LINE, highlighted];
						g.LineWidth = 1;
						g.Stroke ();
					}
				} else {
					if (!fitsInSameLine) { // draw box
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
						g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2 - 1));
						g.ClosePath ();
					} else { // draw filled arrow box
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
						g.LineTo (new Cairo.PointD (x2  - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
						g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2));
						g.ClosePath ();
					}
					g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted];
					g.Fill ();
					
					// draw light bottom line
					g.MoveTo (new Cairo.PointD (right, y2Bottom));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.Stroke ();

					// stroke without the arrow
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
					if (fitsInSameLine)
						g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
					
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					
					g.Color = colorMatrix[active, BOTTOM, LINE, highlighted];
					g.LineWidth = 1;
					g.Stroke ();
					
					// stroke top line
					if (fitsInSameLine) {
						g.Color = colorMatrix[active, BOTTOM, LINE, highlighted];
						g.MoveTo (new Cairo.PointD (right, y2));
						g.LineTo (new Cairo.PointD (x2 + 0.5, y2));
						g.Stroke ();
					}
				}
				
				if (editor.Options.ShowRuler) {
					int divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
					if (divider >= x2) {
						g.MoveTo (new Cairo.PointD (divider + 0.5, y2));
						g.LineTo (new Cairo.PointD (divider + 0.5, y2Bottom));
						g.Color = colorMatrix[active, BOTTOM, DARK, highlighted];
						g.LineWidth = 1;
						g.Stroke ();
					}
				}
				
				if (errors.Count > 1 && errorCountLayout != null) {
					int rX = x2 + (ShowIconsInBubble ? errorPixbuf.Width : 0) + border + LayoutWidth;
					int rY = y + editor.LineHeight / 6;
					int rW = errorCounterWidth - 2;
					int rH = editor.LineHeight * 3 / 4;
					BookmarkMarker.DrawRoundRectangle (g, rX, rY, 8, rW, rH);
					
					g.Color = oldIsOver ? new Cairo.Color (0.3, 0.3, 0.3) : new Cairo.Color (0.5, 0.5, 0.5);
					g.Fill ();
					if (CollapseExtendedErrors) {
						win.DrawLayout (gcLight, x2 + (ShowIconsInBubble ? errorPixbuf.Width : 0) + border + LayoutWidth + 4, y + (editor.LineHeight - eh) / 2, errorCountLayout);
					} else {
						g.MoveTo (rX + rW / 2 - rW / 4, rY + rH - rH / 4);
						g.LineTo (rX + rW / 2 + rW / 4, rY + rH - rH / 4);
						g.LineTo (rX + rW / 2 , rY + rH / 4);
						g.ClosePath ();
						
						g.Color = new Cairo.Color (1, 1, 1);
						g.Fill ();
					}
				}
			}
			
			for (int i = 0; i < layouts.Count; i++) {
				LayoutDescriptor layout = layouts[i];
				x2 = right - layout.Width - border - errorPixbuf.Width;
				if (i == 0)
					x2 -= errorCounterWidth;
				x2 = System.Math.Max (x2, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
				if (i > 0) {
					editor.TextViewMargin.DrawRectangleWithRuler (win, x, new Gdk.Rectangle (x, y, right, editor.LineHeight), isEolSelected ? editor.ColorStyle.Selection.BackgroundColor : editor.ColorStyle.Default.BackgroundColor, true);
					if (!isEolSelected) {
						using (var g = Gdk.CairoHelper.Create (win)) {
							g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
							g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
							g.LineTo (new Cairo.PointD (right, y + editor.LineHeight));
							g.LineTo (new Cairo.PointD (right, y));
							g.ClosePath ();
							
							if (CollapseExtendedErrors) {
								Cairo.Gradient pat = new Cairo.LinearGradient (x2, y, x2, y + editor.LineHeight);
								pat.AddColorStop (0, colorMatrix[active, TOP, LIGHT, highlighted]);
								pat.AddColorStop (1, colorMatrix[active, BOTTOM, LIGHT, highlighted]);
								g.Pattern = pat;
							} else {
								g.Color = colorMatrix[active, TOP, LIGHT, highlighted];
							}
							g.Fill ();
							if (editor.Options.ShowRuler) {
								int divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
								if (divider >= x2) {
									g.MoveTo (new Cairo.PointD (divider + 0.5, y));
									g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
									g.Color = colorMatrix[active, BOTTOM, DARK, highlighted];
									g.LineWidth = 1;
									g.Stroke ();
								}
							}
						}
					}
				}
				if (!isEolSelected) {
					win.DrawLayout (gc, x2 + errorPixbuf.Width + border, y + (editor.LineHeight - layout.Height) / 2, layout.Layout);
					if (ShowIconsInBubble) {
						win.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), 
						                errors[i].IsError ? errorPixbuf : warningPixbuf, 
						                0, 0, 
						                x2, y + (editor.LineHeight - errorPixbuf.Height) / 2, 
						                errorPixbuf.Width, errorPixbuf.Height, 
						                Gdk.RgbDither.None, 0, 0);
					}
				}
				y += editor.LineHeight;
				if (!UseVirtualLines)
					break;
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
			
		}
		
		public void MouseRelease (MarginMouseEventArgs args)
		{
		}
		
		#endregion
	
		public Gdk.Rectangle ErrorTextBounds {
			get {
//				int x = editor.Allocation.Width;
				int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
				
				int y = editor.LineToVisualY (lineNumber) - (int)editor.VAdjustment.Value;
				int height = editor.LineHeight * errors.Count;
				if (!fitsInSameLine)
					y += editor.LineHeight;
				int errorCounterWidth = 0;
				
				int ew = 0, eh = 0;
				if (errors.Count > 1 && errorCountLayout != null) {
					errorCountLayout.GetPixelSize (out ew, out eh);
					errorCounterWidth = ew + 10;
				}
				
				int labelWidth = LayoutWidth + border + (ShowIconsInBubble ? errorPixbuf.Width : 0) + errorCounterWidth + editor.LineHeight / 2;
				
				
				return new Gdk.Rectangle (editor.Allocation.Width - labelWidth, y, labelWidth, height);
			}
		}
		

		#region IActionTextMarker implementation
		public bool MousePressed (TextEditor editor, MarginMouseEventArgs args)
		{
			if (MouseIsOverMarker (editor, args)) {
				CollapseExtendedErrors = !CollapseExtendedErrors;
				editor.QueueDraw ();
				return true;
			}
			MouseIsOverMarker (editor, args);
			return false;
		}
		
		bool MouseIsOverMarker (TextEditor editor, MarginMouseEventArgs args)
		{
			int ew = 0, eh = 0;
			int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
			int y = editor.LineToVisualY (lineNumber) - (int)editor.VAdjustment.Value;
			if (fitsInSameLine) {
				if (args.Y < y + 2 ||  args.Y > y + editor.LineHeight - 2)
					return false;
			} else {
				if (args.Y < y + editor.LineHeight + 2 || args.Y > y + editor.LineHeight * 2 - 2)
					return false;
			}
			
			if (errors.Count > 1 && errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out ew, out eh);
				int errorCounterWidth = ew + 10;
				if (editor.Allocation.Width - args.X - editor.TextViewMargin.XOffset <= errorCounterWidth) 
					return true;
			}
			return false;
		}
		
		int MouseIsOverError (TextEditor editor, MarginMouseEventArgs args)
		{
			if (layouts == null)
				return -1;
			int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
			int y = editor.LineToVisualY (lineNumber) - (int)editor.VAdjustment.Value;
			int height = editor.LineHeight * errors.Count;
			if (!fitsInSameLine) {
				y += editor.LineHeight;
			}
//			Console.WriteLine (lineNumber +  ": height={0}, y={1}, args={2}", height, y, args.Y);
			if (y > args.Y || args.Y > y + height)
				return -1;
			int error = (args.Y - y) / editor.LineHeight;
//			Console.WriteLine ("error:" + error);
			if (error >= layouts.Count)
				return -1;
			int errorCounterWidth = 0;
			
			int ew = 0, eh = 0;
			if (error == 0 && errors.Count > 1 && errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out ew, out eh);
				errorCounterWidth = ew + 10;
			}
			
			int labelWidth = LayoutWidth + border + (ShowIconsInBubble ? errorPixbuf.Width : 0) + errorCounterWidth + editor.LineHeight / 2;
			
			if (editor.Allocation.Width - editor.TextViewMargin.XOffset - args.X < labelWidth)
				return error;
		
			return -1;
		}
		
		bool oldIsOver = false;
		
		Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);
		public void MouseHover (TextEditor editor, MarginMouseEventArgs args, TextMarkerHoverResult result)
		{
			bool isOver = MouseIsOverMarker (editor, args);
			if (isOver != oldIsOver) 
				editor.Document.CommitLineUpdate (this.LineSegment);
			oldIsOver = isOver;
			
			int errorNumber = MouseIsOverError (editor, args);
			if (errorNumber >= 0) {
				result.Cursor = arrowCursor;
				if (!isOver) // don't show tooltip when hovering over error counter layout.
					result.TooltipMarkup = GLib.Markup.EscapeText (errors[errorNumber].ErrorMessage);
			}
			
		}

		#endregion
		
		#region IExtendingTextMarker implementation
		public void Draw (TextEditor editor, Gdk.Drawable win, int lineNr, Gdk.Rectangle lineArea)
		{
			int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
			int errorNumber = lineNr - lineNumber;
			int x = editor.TextViewMargin.XOffset;
			int y = lineArea.Y;
			int right = editor.Allocation.Width;
			int errorCounterWidth = 0;
			
			int ew = 0, eh = 0;
			if (errors.Count > 1 && errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out ew, out eh);
				errorCounterWidth = ew + 10;
			}
			
			int x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? errorPixbuf.Width : 0) - errorCounterWidth, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset  + lineSegment.EditableLength) : false;
			
			int active = editor.HasFocus ? 0 : 1;
			if (editor.Document.GetTextAt (lineSegment) != initialText)
				active = 1;
			bool isCaretInLine = lineSegment.Offset <= editor.Caret.Offset && editor.Caret.Offset <= lineSegment.EndOffset;
			int highlighted = active == 0 && isCaretInLine ? 1 : 0;
			
			LayoutDescriptor layout = layouts[errorNumber];
			x2 = right - LayoutWidth - border - (ShowIconsInBubble ? errorPixbuf.Width : 0);
			
			x2 -= errorCounterWidth;
			x2 = System.Math.Max (x2, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			
			if (!isEolSelected) {
				using (var g = Gdk.CairoHelper.Create (win)) {
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y - 1.5));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
					g.LineTo (new Cairo.PointD (right, y + editor.LineHeight));
					g.LineTo (new Cairo.PointD (right, y - 1.5));
					g.ClosePath ();
					g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted];
					g.Fill ();
					
					g.Color = colorMatrix[active, BOTTOM, LINE, highlighted];
					g.LineWidth = 1;
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
					if (errorNumber == errors.Count - 1)
						g.LineTo (new Cairo.PointD (lineArea.Right, y + editor.LineHeight));
					g.Stroke ();
					
					if (editor.Options.ShowRuler) {
						int divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
						if (divider >= x2) {
							g.MoveTo (new Cairo.PointD (divider + 0.5, y));
							g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
							g.Color = colorMatrix[active, BOTTOM, DARK, highlighted];
							g.LineWidth = 1;
							g.Stroke ();
						}
					}
				}
			}
			
			if (!isEolSelected) {
				win.DrawLayout (gc, x2 + (ShowIconsInBubble ? errorPixbuf.Width : 0) + border, y + (editor.LineHeight - layout.Height) / 2, layout.Layout);
				if (ShowIconsInBubble) {
					win.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), 
					                errors[errorNumber].IsError ? errorPixbuf : warningPixbuf, 
					                0, 0, 
					                x2, y + (editor.LineHeight - errorPixbuf.Height) / 2, 
					                errorPixbuf.Width, errorPixbuf.Height, 
					                Gdk.RgbDither.None, 0, 0);
				}
			}
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
