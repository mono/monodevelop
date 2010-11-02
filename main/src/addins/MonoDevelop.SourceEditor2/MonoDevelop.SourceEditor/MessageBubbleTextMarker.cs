// 
// MessageBubbleTextMarker.cs
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
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Fonts;

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
		internal const int border = 4;
		internal Gdk.Pixbuf errorPixbuf;
		internal Gdk.Pixbuf warningPixbuf;
//		bool fitCalculated = false;
		bool fitsInSameLine = true;
		public bool FitsInSameLine {
			get { return this.fitsInSameLine; }
		}

		TextEditor editor;

		public override bool IsVisible {
			get { return !task.Completed; }
			set { task.Completed = !value; }
		}

		bool collapseExtendedErrors;
		public bool CollapseExtendedErrors {
			get { return collapseExtendedErrors; }
			set {
				if (collapseExtendedErrors == value)
					return;
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

		public bool UseVirtualLines { get; set; }

		List<ErrorText> errors = new List<ErrorText> ();
		internal IList<ErrorText> Errors {
			get { return errors; }
		}

		Task task;
		LineSegment lineSegment;
		int editorAllocHeight = -1, lastLineLength = -1;
		internal double lastHeight = 0;

		public double GetLineHeight (TextEditor editor)
		{
			if (!IsVisible || DebuggingService.IsDebugging)
				return editor.LineHeight;
			
			if (editorAllocHeight == editor.Allocation.Width && lastLineLength == lineSegment.EditableLength)
				return lastHeight;
			
			CalculateLineFit (editor, lineSegment);
			double height;
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

		public void SetPrimaryError (string text)
		{
			var match = mcsErrorFormat.Match (text);
			if (match.Success)
				text = match.Groups[1].Value;
			int idx = -1;
			for (int i = 0; i < errors.Count; i++) {
				if (errors[i].ErrorMessage == text) {
					idx = i;
					break;
				}
			}
			if (idx <= 0)
				return;
			var tmp = errors[idx];
			errors.RemoveAt (idx);
			errors.Insert (0, tmp);
			var tmplayout = layouts[idx];
			layouts.RemoveAt (idx);
			layouts.Insert (0, tmplayout);
		}

		static Dictionary<string, KeyValuePair<int, int>> textWidthDictionary = new Dictionary<string, KeyValuePair<int, int>> ();
		static Dictionary<LineSegment, double> lineWidthDictionary = new Dictionary<LineSegment, double> ();

		void CalculateLineFit (TextEditor editor, LineSegment lineSegment)
		{
			double textWidth;
			if (!lineWidthDictionary.TryGetValue (lineSegment, out textWidth)) {
				var textLayout = editor.TextViewMargin.GetLayout (lineSegment);
				textWidth = textLayout.PangoWidth / Pango.Scale.PangoScale;
				if (textWidthDictionary.Count > 10000)
					textWidthDictionary.Clear ();
				
				lineWidthDictionary[lineSegment] = textWidth;
			}
			EnsureLayoutCreated (editor);
			fitsInSameLine = editor.TextViewMargin.XOffset + textWidth + LayoutWidth + errorPixbuf.Width + border + editor.LineHeight / 2 < editor.Allocation.Width;
		}

		string initialText;
		public MessageBubbleTextMarker (TextEditor editor, Task task, LineSegment lineSegment, bool isError, string errorMessage)
		{
			this.editor = editor;
			this.task = task;
			this.IsVisible = true;
			this.lineSegment = lineSegment;
			this.initialText = editor.Document.GetTextAt (lineSegment);
			this.Flags = TextMarkerFlags.DrawsSelection;
			AddError (isError, errorMessage);
			editor.EditorOptionsChanged += HandleEditorEditorOptionsChanged;
			errorPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
			warningPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);
		}

		void HandleEditorEditorOptionsChanged (object sender, EventArgs e)
		{
			DisposeLayout ();
			textWidthDictionary.Clear ();
			lineWidthDictionary.Clear ();
			EnsureLayoutCreated (editor);
			CalculateLineFit (editor, lineSegment);
		}

		static System.Text.RegularExpressions.Regex mcsErrorFormat = new System.Text.RegularExpressions.Regex ("(.+)\\(CS\\d+\\)\\Z");
		public void AddError (bool isError, string errorMessage)
		{
			var match = mcsErrorFormat.Match (errorMessage);
			if (match.Success)
				errorMessage = match.Groups[1].Value;
			errors.Add (new ErrorText (isError, errorMessage));
			CollapseExtendedErrors = errors.Count > 1;
			DisposeLayout ();
		}

		public static bool RemoveLine (LineSegment line)
		{
			if (!lineWidthDictionary.ContainsKey (line))
				return false;
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
			if (errorCountLayout != null) {
				errorCountLayout.Dispose ();
				errorCountLayout = null;
			}
		}

		public void Dispose ()
		{
			editor.EditorOptionsChanged -= HandleEditorEditorOptionsChanged;
			DisposeLayout ();
			if (!CollapseExtendedErrors)
				editor.Document.UnRegisterVirtualTextMarker (this);
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

		internal Cairo.Color gc, gcLight, gcSelected;
		internal Pango.Layout errorCountLayout;
		List<LayoutDescriptor> layouts;
		internal IList<LayoutDescriptor> Layouts {
			get { return layouts; }
		}
		Pango.FontDescription fontDescription;
		internal Cairo.Color[,,,,] colorMatrix;	
		static	Cairo.Color[,,,,] warningMatrix, errorMatrix;
		static Cairo.Color errorGc, warningGc;
	
		static Cairo.Color[,,,,] CreateColorMatrix (TextEditor editor, bool isError)
		{
			string typeString = isError ? "error" : "warning";
			Cairo.Color[,,,,] colorMatrix = new Cairo.Color[2, 2, 3, 2, 2];
			
			colorMatrix[0, 0, 0, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".light.color1").Color);
			colorMatrix[0, 1, 0, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".light.color2").Color);
			
			colorMatrix[0, 0, 1, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".dark.color1").Color);
			colorMatrix[0, 1, 1, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".dark.color2").Color);
			
			colorMatrix[0, 0, 2, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".line.top").Color);
			colorMatrix[0, 1, 2, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".line.bottom").Color);
			
			colorMatrix[1, 0, 0, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".light.color1").Color);
			colorMatrix[1, 1, 0, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".light.color2").Color);
			
			colorMatrix[1, 0, 1, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".dark.color1").Color);
			colorMatrix[1, 1, 1, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".dark.color2").Color);
			
			colorMatrix[1, 0, 2, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".line.top").Color);
			colorMatrix[1, 1, 2, 0, 0] = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.GetChunkStyle ("bubble.inactive." + typeString + ".line.bottom").Color);
			
			double factor = 1.03;
			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < 2; j++) {
					for (int k = 0; k < 3; k++) {
						HslColor color = colorMatrix[i, j, k, 0, 0];
						color.L *= factor;
						colorMatrix[i, j, k, 1, 0] = color;
					}
				}
			}
			var selectionColor = Style.ToCairoColor (editor.ColorStyle.Selection.BackgroundColor);
			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < 2; j++) {
					for (int k = 0; k < 3; k++) {
						for (int l = 0; l < 2; l++) {
							var color = colorMatrix[i, j, k, l, 0];
							colorMatrix[i, j, k, l, 1] = new Cairo.Color ((color.R + selectionColor.R * 1.5) / 2.5, (color.G + selectionColor.G * 1.5) / 2.5, (color.B + selectionColor.B * 1.5) / 2.5);
						}
					}
				}
			}
			return colorMatrix;
		}
		
		void EnsureLayoutCreated (TextEditor editor)
		{
			if (colorMatrix == null && editor.ColorStyle != null) {
				bool isError = errors.Any (e => e.IsError);
				if (errorMatrix == null) {
					errorGc =  (HslColor)(editor.ColorStyle.GetChunkStyle ("bubble.error.text").Color);
					warningGc =  (HslColor)(editor.ColorStyle.GetChunkStyle ("bubble.warning.text").Color);
					errorMatrix = CreateColorMatrix (editor, true);
					warningMatrix = CreateColorMatrix (editor, false);
				}
				colorMatrix = isError ? errorMatrix : warningMatrix;
				
				gc = isError ? errorGc : warningGc;
				gcSelected = (HslColor)editor.ColorStyle.Selection.Color;
				gcLight = new Cairo.Color (1, 1, 1);
					
			}
			
			if (layouts != null)
				return;
			
			layouts = new List<LayoutDescriptor> ();
			fontDescription = FontService.GetFontDescription ("MessageBubbles");
			
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
		const int DARK = 1;
		const int LINE = 2;

		const int TOP = 0;
		const int BOTTOM = 1;

		bool ShowIconsInBubble = false;
		internal int LayoutWidth {
			get {
				if (layouts == null)
					return 0;
				if (!CollapseExtendedErrors && errors.Count > 1)
					return layouts.Max (l => l.Width);
				return layouts[0].Width;
			}
		}
		
		public bool DrawBackground (TextEditor editor, Cairo.Context g, TextViewMargin.LayoutWrapper layout2, int selectionStart, int selectionEnd, int startOffset, int endOffset, double y, double startXPos, double endXPos, ref bool drawBg)
		{
			if (!IsVisible || DebuggingService.IsDebugging)
				return true;
			EnsureLayoutCreated (editor);
			double x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			int errorCounterWidth = 0;
			bool isCaretInLine = startOffset <= editor.Caret.Offset && editor.Caret.Offset <= endOffset;
			int ew = 0, eh = 0;
			if (errors.Count > 1 && errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out ew, out eh);
				errorCounterWidth = ew + 10;
			}
			
			double x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? errorPixbuf.Width : 0) - errorCounterWidth, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset + lineSegment.EditableLength) : false;
			
			int active = editor.Document.GetTextAt (lineSegment) == initialText ? 0 : 1;
			int highlighted = active == 0 && isCaretInLine ? 1 : 0;
			int selected = 0;
			
			double topSize = Math.Floor (editor.LineHeight / 2);
			double bottomSize = editor.LineHeight / 2 + editor.LineHeight % 2;
		
			if (!fitsInSameLine) {
				if (isEolSelected) {
					x -= (int)editor.HAdjustment.Value;
					editor.TextViewMargin.DrawRectangleWithRuler (g, x, new Cairo.Rectangle (x, y + editor.LineHeight, editor.TextViewMargin.TextStartPosition, editor.LineHeight), editor.ColorStyle.Default.CairoBackgroundColor, true);
					editor.TextViewMargin.DrawRectangleWithRuler (g, x + editor.TextViewMargin.TextStartPosition, new Cairo.Rectangle (x + editor.TextViewMargin.TextStartPosition, y + editor.LineHeight, editor.Allocation.Width + (int)editor.HAdjustment.Value, editor.LineHeight), editor.ColorStyle.Selection.CairoBackgroundColor, true);
					x += (int)editor.HAdjustment.Value;
				} else {
					editor.TextViewMargin.DrawRectangleWithRuler (g, x, new Cairo.Rectangle (x, y + editor.LineHeight, x2, editor.LineHeight), editor.ColorStyle.Default.CairoBackgroundColor, true);
				}
			}
			DrawRectangle (g, x, y, right, topSize);
			g.Color = colorMatrix[active, TOP, LIGHT, highlighted, selected];
			g.Fill ();
			DrawRectangle (g, x, y + topSize, right, bottomSize);
			g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted, selected];
			g.Fill ();
			
			g.MoveTo (new Cairo.PointD (x, y + 0.5));
			g.LineTo (new Cairo.PointD (x + right, y + 0.5));
			g.Color = colorMatrix[active, TOP, LINE, highlighted, selected];
			g.Stroke ();
			
			g.MoveTo (new Cairo.PointD (x, y + editor.LineHeight - 0.5));
			g.LineTo (new Cairo.PointD ((fitsInSameLine ? x + right : x2 + 1), y + editor.LineHeight - 0.5));
			g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, selected];
			g.Stroke ();
			if (editor.Options.ShowRuler) {
				double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
				g.MoveTo (new Cairo.PointD (divider + 0.5, y));
				g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
				g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, selected];
				g.Stroke ();
			}
			
			// draw background
			if (layout2.StartSet || selectionStart == endOffset) {
				double startX;
				double endX;
				
				if (selectionStart != endOffset) {
					var start = layout2.Layout.IndexToPos ((int)layout2.SelectionStartIndex);
					startX = (int)(start.X / Pango.Scale.PangoScale);
					var end = layout2.Layout.IndexToPos ((int)layout2.SelectionEndIndex);
					endX = (int)(end.X / Pango.Scale.PangoScale);
				} else {
					startX = x2;
					endX = startX;
				}
				
				if (editor.MainSelection.SelectionMode == SelectionMode.Block && startX == endX)
					endX = startX + 2;
				startX += startXPos;
				endX += startXPos;
				startX = Math.Max (editor.TextViewMargin.XOffset, startX);
				// clip region to textviewmargin start
				if (isEolSelected)
					endX = editor.Allocation.Width + (int)editor.HAdjustment.Value;
				if (startX < endX) {
					DrawRectangle (g, startX, y, endX - startX, topSize);
					g.Color = colorMatrix[active, TOP, LIGHT, highlighted, 1];
					g.Fill ();
					DrawRectangle (g, startX, y + topSize, endX - startX, bottomSize);
					g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted, 1];
					g.Fill ();
					
					g.MoveTo (new Cairo.PointD (startX, y + 0.5));
					g.LineTo (new Cairo.PointD (endX, y + 0.5));
					g.Color = colorMatrix[active, TOP, LINE, highlighted, 1];
					g.Stroke ();
					
					if (startX < x2) {
						g.MoveTo (new Cairo.PointD (startX, y + editor.LineHeight - 0.5));
						g.LineTo (new Cairo.PointD (System.Math.Min (endX, x2 + 1), y + editor.LineHeight - 0.5));
						g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, 1];
						g.Stroke ();
						if (x2 + 1 < endX) {
							g.MoveTo (new Cairo.PointD (x2 + 1, y + editor.LineHeight - 0.5));
							g.LineTo (new Cairo.PointD (endX, y + editor.LineHeight - 0.5));
							g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted, 1];
							g.Stroke ();
						}
					}
					
					if (editor.Options.ShowRuler) {
						double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
						g.MoveTo (new Cairo.PointD (divider + 0.5, y));
						g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
						g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, 1];
						g.Stroke ();
					}
				}
			}
			
			if (!fitsInSameLine)
				y += editor.LineHeight;
			double y2 = y + 0.5;
			double y2Bottom = y2 + editor.LineHeight - 1;
			selected = isEolSelected && (CollapseExtendedErrors || errors.Count == 1) ? 1 : 0;
			
			// draw message text background
			if (CollapseExtendedErrors || errors.Count == 1) {
				if (!fitsInSameLine) {
					// draw box below line 
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2 - 1));
					g.ClosePath ();
					g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted, selected];
					g.Fill ();
					
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, selected];
					g.Stroke ();
				} else {
					// draw 'arrow marker' in the same line
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
					double mid = y2 + topSize;
					g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, mid));
					
					g.LineTo (new Cairo.PointD (right, mid));
					g.LineTo (new Cairo.PointD (right, y2));
					g.ClosePath ();
					g.Color = colorMatrix[active, TOP, DARK, highlighted, selected];
					g.Fill ();
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, mid));
					
					g.LineTo (new Cairo.PointD (right, mid));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.ClosePath ();
					
					g.Color = colorMatrix[active, BOTTOM, DARK, highlighted, selected];
					g.Fill ();
					
					// draw border
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
					g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
					
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2));
					g.ClosePath ();
					
					g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, selected];
					g.Stroke ();
				}
			} else {
				if (!fitsInSameLine) {
					// draw box below line
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2 - 1));
					g.ClosePath ();
				} else {
					// draw filled arrow box
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
					g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2));
					g.ClosePath ();
				}
				g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted, selected];
				g.Fill ();
				
				// draw light bottom line
				g.MoveTo (new Cairo.PointD (right, y2Bottom));
				g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
				g.Stroke ();
				
				// stroke left line
				if (fitsInSameLine) {
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
					g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
				} else {
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom + 1));
				}
				
				g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, selected];
				g.Stroke ();
				
				// stroke top line
				if (fitsInSameLine) {
					g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, selected];
					g.MoveTo (new Cairo.PointD (right, y2));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2));
					g.Stroke ();
				}
			}
			
			if (editor.Options.ShowRuler) {
				double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
				if (divider >= x2) {
					g.MoveTo (new Cairo.PointD (divider + 0.5, y2));
					g.LineTo (new Cairo.PointD (divider + 0.5, y2Bottom));
					g.Color = colorMatrix[active, BOTTOM, DARK, highlighted, selected];
					g.Stroke ();
				}
			}
			
			if (errors.Count > 1 && errorCountLayout != null) {
				double rX = x2 + (ShowIconsInBubble ? errorPixbuf.Width : 0) + border + LayoutWidth;
				double rY = y + editor.LineHeight / 6;
				double rW = errorCounterWidth - 2;
				double rH = editor.LineHeight * 3 / 4;
				BookmarkMarker.DrawRoundRectangle (g, rX, rY, 8, rW, rH);
				
				g.Color = oldIsOver ? new Cairo.Color (0.3, 0.3, 0.3) : new Cairo.Color (0.5, 0.5, 0.5);
				g.Fill ();
				if (CollapseExtendedErrors) {
					g.Color = gcLight;
					g.Save ();
					g.Translate (x2 + (ShowIconsInBubble ? errorPixbuf.Width : 0) + border + LayoutWidth + 4, y + (editor.LineHeight - eh) / 2 + eh % 2);
					g.ShowLayout (errorCountLayout);
					g.Restore ();
				} else {
					g.MoveTo (rX + rW / 2 - rW / 4, rY + rH - rH / 4);
					g.LineTo (rX + rW / 2 + rW / 4, rY + rH - rH / 4);
					g.LineTo (rX + rW / 2, rY + rH / 4);
					g.ClosePath ();
					
					g.Color = new Cairo.Color (1, 1, 1);
					g.Fill ();
				}
			}
			
			for (int i = 0; i < layouts.Count; i++) {
				LayoutDescriptor layout = layouts[i];
				x2 = right - layout.Width - border - errorPixbuf.Width;
				if (i == 0)
					x2 -= errorCounterWidth;
				x2 = System.Math.Max (x2, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
				if (i > 0) {
					editor.TextViewMargin.DrawRectangleWithRuler (g, x, new Cairo.Rectangle (x, y, right, editor.LineHeight), isEolSelected ? editor.ColorStyle.Selection.CairoBackgroundColor : editor.ColorStyle.Default.CairoBackgroundColor, true);
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
					g.LineTo (new Cairo.PointD (right, y + editor.LineHeight));
					g.LineTo (new Cairo.PointD (right, y));
					g.ClosePath ();
					
					if (CollapseExtendedErrors) {
						Cairo.Gradient pat = new Cairo.LinearGradient (x2, y, x2, y + editor.LineHeight);
						pat.AddColorStop (0, colorMatrix[active, TOP, LIGHT, highlighted, selected]);
						pat.AddColorStop (1, colorMatrix[active, BOTTOM, LIGHT, highlighted, selected]);
						g.Pattern = pat;
					} else {
						g.Color = colorMatrix[active, TOP, LIGHT, highlighted, selected];
					}
					g.Fill ();
					if (editor.Options.ShowRuler) {
						double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
						if (divider >= x2) {
							g.MoveTo (new Cairo.PointD (divider + 0.5, y));
							g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
							g.Color = colorMatrix[active, BOTTOM, DARK, highlighted, selected];
							g.Stroke ();
						}
					}
				}
				int lw, lh;
				layout.Layout.GetPixelSize (out lw, out lh);
				g.Color = (HslColor)(selected == 0 ? gc : gcSelected);
				g.Save ();
				g.Translate (x2 + errorPixbuf.Width + border, y + (editor.LineHeight - layout.Height) / 2 + layout.Height % 2);
				g.ShowLayout (layout.Layout);
				g.Restore ();
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

		static void DrawRectangle (Cairo.Context g, double x, double y, double width, double height)
		{
			double right = x + width;
			double bottom = y + height;
			g.MoveTo (new Cairo.PointD (x, y));
			g.LineTo (new Cairo.PointD (right, y));
			g.LineTo (new Cairo.PointD (right, bottom));
			g.LineTo (new Cairo.PointD (x, bottom));
			g.LineTo (new Cairo.PointD (x, y));
			g.ClosePath ();
		}
		#region IIconBarMarker implementation

		public void DrawIcon (Mono.TextEditor.TextEditor editor, Cairo.Context cr, LineSegment line, int lineNumber, double x, double y, double width, double height)
		{
			if (DebuggingService.IsDebugging)
				return;
			editor.GdkWindow.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), 
				errors.Any (e => e.IsError) ? errorPixbuf : warningPixbuf, 
				0, 0, 
				(int)(x + (width - errorPixbuf.Width) / 2), 
				(int)(y + (height - errorPixbuf.Height) / 2), 
				errorPixbuf.Width, errorPixbuf.Height, 
				Gdk.RgbDither.None, 0, 0);
		}

		public void MousePress (MarginMouseEventArgs args)
		{
		}

		public void MouseRelease (MarginMouseEventArgs args)
		{
		}
		
		public void MouseHover (MarginMouseEventArgs args)
		{
			var sb = new System.Text.StringBuilder ();
			foreach (var error in errors) {
				if (sb.Length > 0)
					sb.AppendLine ();
				sb.Append (error.ErrorMessage);
			}
			args.Editor.TooltipText = sb.ToString ();
		}
		
		#endregion

		public Gdk.Rectangle ErrorTextBounds {
			get {
				int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
				
				double y = editor.LineToY (lineNumber) - (int)editor.VAdjustment.Value;
				double height = editor.LineHeight * errors.Count;
				if (!fitsInSameLine)
					y += editor.LineHeight;
				int errorCounterWidth = 0;
				
				int ew = 0, eh = 0;
				if (errors.Count > 1 && errorCountLayout != null) {
					errorCountLayout.GetPixelSize (out ew, out eh);
					errorCounterWidth = ew + 10;
				}
				
				double labelWidth = LayoutWidth + border + (ShowIconsInBubble ? errorPixbuf.Width : 0) + errorCounterWidth;
				if (fitsInSameLine)
					labelWidth += editor.LineHeight / 2;
				return new Gdk.Rectangle ((int)(editor.Allocation.Width - labelWidth), (int)y, (int)labelWidth, (int)height);
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
			double y = editor.LineToY (lineNumber) - editor.VAdjustment.Value;
			if (fitsInSameLine) {
				if (args.Y < y + 2 || args.Y > y + editor.LineHeight - 2)
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
			double y = editor.LineToY (lineNumber) - editor.VAdjustment.Value;
			double height = editor.LineHeight * errors.Count;
			if (!fitsInSameLine)
				y += editor.LineHeight;
//			Console.WriteLine (lineNumber +  ": height={0}, y={1}, args={2}", height, y, args.Y);
			if (y > args.Y || args.Y > y + height)
				return -1;
			int error = (int)((args.Y - y) / editor.LineHeight);
//			Console.WriteLine ("error:" + error);
			if (error >= layouts.Count)
				return -1;
			int errorCounterWidth = 0;
			
			int ew = 0, eh = 0;
			if (error == 0 && errors.Count > 1 && errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out ew, out eh);
				errorCounterWidth = ew + 10;
			}
			
			double labelWidth = LayoutWidth + border + (ShowIconsInBubble ? errorPixbuf.Width : 0) + errorCounterWidth + editor.LineHeight / 2;
			
			if (editor.Allocation.Width - editor.TextViewMargin.XOffset - args.X < labelWidth)
				return error;
			
			return -1;
		}

		bool oldIsOver = false;

		Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);
		public void MouseHover (TextEditor editor, MarginMouseEventArgs args, TextMarkerHoverResult result)
		{
			bool isOver = MouseIsOverMarker (editor, args);
			if (isOver != oldIsOver)
				editor.Document.CommitLineUpdate (this.LineSegment);
			oldIsOver = isOver;
			
			int errorNumber = MouseIsOverError (editor, args);
			if (errorNumber >= 0) {
				result.Cursor = arrowCursor;
				if (!isOver)
					// don't show tooltip when hovering over error counter layout.
					result.TooltipMarkup = GLib.Markup.EscapeText (errors[errorNumber].ErrorMessage);
			}
			
		}

		#endregion

		#region IExtendingTextMarker implementation
		public void Draw (TextEditor editor, Cairo.Context g, int lineNr, Cairo.Rectangle lineArea)
		{
			EnsureLayoutCreated (editor);
			int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
			int errorNumber = lineNr - lineNumber;
			double x = editor.TextViewMargin.XOffset;
			double y = lineArea.Y;
			double right = editor.Allocation.Width;
			int errorCounterWidth = 0;
			
			int ew = 0, eh = 0;
			if (errors.Count > 1 && errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out ew, out eh);
				errorCounterWidth = ew + 10;
			}
			
			double x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? errorPixbuf.Width : 0) - errorCounterWidth, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
//			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset  + lineSegment.EditableLength) : false;
			int active = editor.Document.GetTextAt (lineSegment) == initialText ? 0 : 1;
			bool isCaretInLine = lineSegment.Offset <= editor.Caret.Offset && editor.Caret.Offset <= lineSegment.EndOffset;
			int highlighted = active == 0 && isCaretInLine ? 1 : 0;
			int selected = 0;
			LayoutDescriptor layout = layouts[errorNumber];
			x2 = right - LayoutWidth - border - (ShowIconsInBubble ? errorPixbuf.Width : 0);
			
			x2 -= errorCounterWidth;
			x2 = System.Math.Max (x2, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			
			g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
			g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
			g.LineTo (new Cairo.PointD (right, y + editor.LineHeight));
			g.LineTo (new Cairo.PointD (right, y));
			g.ClosePath ();
			g.Color = colorMatrix[active, BOTTOM, LIGHT, highlighted, selected];
			g.Fill ();
			
			g.Color = colorMatrix[active, BOTTOM, LINE, highlighted, selected];
			g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
			g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
			if (errorNumber == errors.Count - 1)
				g.LineTo (new Cairo.PointD (lineArea.X + lineArea.Width, y + editor.LineHeight));
			g.Stroke ();
			
			if (editor.Options.ShowRuler) {
				double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
				if (divider >= x2) {
					g.MoveTo (new Cairo.PointD (divider + 0.5, y));
					g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
					g.Color = colorMatrix[active, BOTTOM, DARK, highlighted, selected];
					g.Stroke ();
				}
			}
			g.Save ();
			g.Translate (x2 + (ShowIconsInBubble ? errorPixbuf.Width : 0) + border, y + (editor.LineHeight - layout.Height) / 2 + layout.Height % 2);
			g.Color = selected == 0 ? gc : gcSelected;
			g.ShowLayout (layout.Layout);
			g.Restore ();
//			if (ShowIconsInBubble)
//				win.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), errors[errorNumber].IsError ? errorPixbuf : warningPixbuf, 0, 0, x2, y + (editor.LineHeight - errorPixbuf.Height) / 2, errorPixbuf.Width, errorPixbuf.Height, Gdk.RgbDither.None, 0, 0);
		}
		
		#endregion
		
	}
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
