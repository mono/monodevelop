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

	public class MessageBubbleTextMarker : TextLineMarker, IBackgroundMarker, IIconBarMarker, IExtendingTextLineMarker, IDisposable, IActionTextLineMarker
	{
		MessageBubbleCache cache;
		
		internal const int border = 4;
		
//		bool fitCalculated = false;
		bool fitsInSameLine = true;
		public bool FitsInSameLine {
			get { return this.fitsInSameLine; }
		}

		TextEditor editor {
			get { return cache.editor;}
		}

		public override bool IsVisible {
			get { return !task.Completed; }
			set { task.Completed = !value; }
		}

		bool collapseExtendedErrors = true;
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
					var fitting = IsCurrentErrorTextFitting ();
					for (int i = fitting ? 1 : 0; i < errors.Count; i++)
						editor.Document.RegisterVirtualTextMarker (lineNumber + i + (fitting ? 0 : 1), this);
				}
				editor.Document.CommitMultipleLineUpdate (lineNumber, lineNumber + errors.Count + 1);
			}
		}

		public bool UseVirtualLines { get; set; }

		List<ErrorText> errors = new List<ErrorText> ();
		internal IList<ErrorText> Errors {
			get { return errors; }
		}

		Task task;
		DocumentLine lineSegment;
//		int editorAllocHeight = -1;
//		int lastLineLength = -1;
		internal double lastHeight = 0;

		public double GetLineHeight (TextEditor editor)
		{
			return editor.LineHeight;
			/*
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
			
			return height;*/
		}

		public void SetPrimaryError (string text)
		{
			EnsureLayoutCreated (editor);
			
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

//		void CalculateLineFit (TextEditor editor, LineSegment lineSegment)
//		{
//			double textWidth;
//			if (!cache.lineWidthDictionary.TryGetValue (lineSegment, out textWidth)) {
//				var textLayout = editor.TextViewMargin.GetLayout (lineSegment);
//				textWidth = textLayout.PangoWidth / Pango.Scale.PangoScale;
//				cache.lineWidthDictionary[lineSegment] = textWidth;
//			}
//			EnsureLayoutCreated (editor);
//			fitsInSameLine = editor.TextViewMargin.XOffset + textWidth + LayoutWidth + cache.errorPixbuf.Width + border + editor.LineHeight / 2 < editor.Allocation.Width;
//		}

		string initialText;
		internal MessageBubbleTextMarker (MessageBubbleCache cache, Task task, DocumentLine lineSegment, bool isError, string errorMessage)
		{
			this.cache = cache;
			this.task = task;
			this.IsVisible = true;
			this.lineSegment = lineSegment;
			this.initialText = editor.Document.GetTextAt (lineSegment);
			this.Flags = TextLineMarkerFlags.DrawsSelection;
			AddError (isError, errorMessage);
//			cache.Changed += (sender, e) => CalculateLineFit (editor, lineSegment);
		}
		
		static System.Text.RegularExpressions.Regex mcsErrorFormat = new System.Text.RegularExpressions.Regex ("(.+)\\(CS\\d+\\)\\Z");
		public void AddError (bool isError, string errorMessage)
		{
			var match = mcsErrorFormat.Match (errorMessage);
			if (match.Success)
				errorMessage = match.Groups [1].Value;
			errors.Add (new ErrorText (isError, errorMessage));
			CollapseExtendedErrors = true;
			DisposeLayout ();
		}
		
		public void DisposeLayout ()
		{
			layouts = null;
			if (errorCountLayout != null) {
				errorCountLayout.Dispose ();
				errorCountLayout = null;
			}
		}
		
		public void Dispose ()
		{
			DisposeLayout ();
			if (!CollapseExtendedErrors)
				editor.Document.UnRegisterVirtualTextMarker (this);
		}
		
		internal Pango.Layout errorCountLayout;
		List<MessageBubbleCache.LayoutDescriptor> layouts;
		
		internal Cairo.Color[,,,,] colorMatrix {
			get {
				bool isError = errors.Any (e => e.IsError);
				return isError ? cache.errorMatrix : cache.warningMatrix;
			}
		}
		
		internal Cairo.Color gc {
			get {
				bool isError = errors.Any (e => e.IsError);
				return isError ? cache.errorGc : cache.warningGc;
			}
		}
		
		internal IList<MessageBubbleCache.LayoutDescriptor> Layouts {
			get { return layouts; }
		}
		
		internal void EnsureLayoutCreated (TextEditor editor)
		{
			if (layouts != null)
				return;
			
			layouts = new List<MessageBubbleCache.LayoutDescriptor> ();
			foreach (ErrorText errorText in errors) {
				layouts.Add (cache.CreateLayoutDescriptor (errorText));
			}
			
			if (errorCountLayout == null && errors.Count > 1) {
				errorCountLayout = new Pango.Layout (editor.PangoContext);
				errorCountLayout.FontDescription = cache.fontDescription;
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
				return layouts [0].Width;
			}
		}
		
		Tuple<int, int> GetErrorCountBounds (TextViewMargin.LayoutWrapper wrapper = null)
		{
			EnsureLayoutCreated (editor);
			var layout = wrapper ?? editor.TextViewMargin.GetLayout (lineSegment);
			try {
				var lineTextPx = editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition + layout.PangoWidth / Pango.Scale.PangoScale;
				if (errors.Count > 1 && errorCountLayout != null || editor.Allocation.Width < lineTextPx + layouts [0].Width) {
					int ew = 0, eh = 0;
					if (errorCountLayout != null) {
						errorCountLayout.GetPixelSize (out ew, out eh);
					} else {
						ew = 10;
					}
					return Tuple.Create (ew + 10, eh);
				}
				return Tuple.Create (0, 0);
			} finally {
				if (wrapper == null && layout.IsUncached)
					layout.Dispose ();
			}
		}

		void DrawMessageExtendIcon (Mono.TextEditor.TextEditor editor, Cairo.Context g, double y, int errorCounterWidth, int eh)
		{
			EnsureLayoutCreated (editor);
			double rW = errorCounterWidth - 2;
			double rH = editor.LineHeight * 3 / 4;
			
			double rX = editor.Allocation.Width - rW - 2;
			double rY = y + (editor.LineHeight - rH) / 2;
			BookmarkMarker.DrawRoundRectangle (g, rX, rY, 8, rW, rH);
			
			g.Color = oldIsOver ? new Cairo.Color (0.3, 0.3, 0.3) : new Cairo.Color (0.5, 0.5, 0.5);
			g.Fill ();
			if (CollapseExtendedErrors) {
				if (errorCountLayout != null) {
					g.Color = cache.gcLight;
					g.Save ();
					g.Translate (rX + rW / 4, rY + (rH - eh) / 2);
					g.ShowLayout (errorCountLayout);
					g.Restore ();
				} else {
					g.MoveTo (rX + rW / 2 - rW / 4, rY + rH / 4);
					g.LineTo (rX + rW / 2 + rW / 4, rY + rH / 4);
					g.LineTo (rX + rW / 2, rY + rH - rH / 4);
					g.ClosePath ();
				
					g.Color = new Cairo.Color (1, 1, 1);
					g.Fill ();
				}
			} else {
				g.MoveTo (rX + rW / 2 - rW / 4, rY + rH - rH / 4);
				g.LineTo (rX + rW / 2 + rW / 4, rY + rH - rH / 4);
				g.LineTo (rX + rW / 2, rY + rH / 4);
				g.ClosePath ();
				
				g.Color = new Cairo.Color (1, 1, 1);
				g.Fill ();
			}
		}
		
		public bool DrawBackground (TextEditor editor, Cairo.Context g, TextViewMargin.LayoutWrapper layout2, int selectionStart, int selectionEnd, int startOffset, int endOffset, double y, double startXPos, double endXPos, ref bool drawBg)
		{
			if (!IsVisible)
				return true;
			EnsureLayoutCreated (editor);
			double x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			bool isCaretInLine = startOffset <= editor.Caret.Offset && editor.Caret.Offset <= endOffset;
			var lineTextPx = editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition + layout2.PangoWidth / Pango.Scale.PangoScale;
			int errorCounterWidth = GetErrorCountBounds (layout2).Item1;
//			int eh = GetErrorCountBounds ().Item2;
			double x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) - errorCounterWidth, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			
			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset + lineSegment.Length) : false;
			
			int active = editor.Document.GetTextAt (lineSegment) == initialText ? 0 : 1;
			int highlighted = active == 0 && isCaretInLine ? 1 : 0;
			int selected = 0;
			
			double topSize = Math.Floor (editor.LineHeight / 2);
			double bottomSize = editor.LineHeight / 2 + editor.LineHeight % 2;
		
			if (!fitsInSameLine) {
				if (isEolSelected) {
					x -= (int)editor.HAdjustment.Value;
					editor.TextViewMargin.DrawRectangleWithRuler (g, x, new Cairo.Rectangle (x, y + editor.LineHeight, editor.TextViewMargin.TextStartPosition, editor.LineHeight), editor.ColorStyle.PlainText.Background, true);
					editor.TextViewMargin.DrawRectangleWithRuler (g, x + editor.TextViewMargin.TextStartPosition, new Cairo.Rectangle (x + editor.TextViewMargin.TextStartPosition, y + editor.LineHeight, editor.Allocation.Width + (int)editor.HAdjustment.Value, editor.LineHeight), editor.ColorStyle.SelectedText.Background, true);
					x += (int)editor.HAdjustment.Value;
				} else {
					editor.TextViewMargin.DrawRectangleWithRuler (g, x, new Cairo.Rectangle (x, y + editor.LineHeight, x2, editor.LineHeight), editor.ColorStyle.PlainText.Background, true);
				}
			}
			DrawRectangle (g, x, y, right, topSize);
			g.Color = colorMatrix [active, TOP, LIGHT, highlighted, selected];
			g.Fill ();
			DrawRectangle (g, x, y + topSize, right, bottomSize);
			g.Color = colorMatrix [active, BOTTOM, LIGHT, highlighted, selected];
			g.Fill ();
			
			g.MoveTo (new Cairo.PointD (x, y + 0.5));
			g.LineTo (new Cairo.PointD (x + right, y + 0.5));
			g.Color = colorMatrix [active, TOP, LINE, highlighted, selected];
			g.Stroke ();
			
			g.MoveTo (new Cairo.PointD (x, y + editor.LineHeight - 0.5));
			g.LineTo (new Cairo.PointD ((fitsInSameLine ? x + right : x2 + 1), y + editor.LineHeight - 0.5));
			g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, selected];
			g.Stroke ();
			if (editor.Options.ShowRuler) {
				double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
				g.MoveTo (new Cairo.PointD (divider + 0.5, y));
				g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
				g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, selected];
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
					g.Color = colorMatrix [active, TOP, LIGHT, highlighted, 1];
					g.Fill ();
					DrawRectangle (g, startX, y + topSize, endX - startX, bottomSize);
					g.Color = colorMatrix [active, BOTTOM, LIGHT, highlighted, 1];
					g.Fill ();
					
					g.MoveTo (new Cairo.PointD (startX, y + 0.5));
					g.LineTo (new Cairo.PointD (endX, y + 0.5));
					g.Color = colorMatrix [active, TOP, LINE, highlighted, 1];
					g.Stroke ();
					
					if (startX < x2) {
						g.MoveTo (new Cairo.PointD (startX, y + editor.LineHeight - 0.5));
						g.LineTo (new Cairo.PointD (System.Math.Min (endX, x2 + 1), y + editor.LineHeight - 0.5));
						g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, 1];
						g.Stroke ();
						if (x2 + 1 < endX) {
							g.MoveTo (new Cairo.PointD (x2 + 1, y + editor.LineHeight - 0.5));
							g.LineTo (new Cairo.PointD (endX, y + editor.LineHeight - 0.5));
							g.Color = colorMatrix [active, BOTTOM, LIGHT, highlighted, 1];
							g.Stroke ();
						}
					}
					
					if (editor.Options.ShowRuler) {
						double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
						g.MoveTo (new Cairo.PointD (divider + 0.5, y));
						g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
						g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, 1];
						g.Stroke ();
					}
				}
			}
			
			if (!fitsInSameLine)
				y += editor.LineHeight;
			double y2 = y + 0.5;
			double y2Bottom = y2 + editor.LineHeight - 1;
			selected = isEolSelected && (CollapseExtendedErrors) ? 1 : 0;
			if (x2 < lineTextPx) 
				x2 = lineTextPx;

// draw message text background
			if (CollapseExtendedErrors) {
				if (!fitsInSameLine) {
// draw box below line 
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2 - 1));
					g.ClosePath ();
					g.Color = colorMatrix [active, BOTTOM, LIGHT, highlighted, selected];
					g.Fill ();
					
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2 - 1));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, selected];
					g.Stroke ();
				} else {
// draw 'arrow marker' in the same line
					if (errors.Count > 1) {
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
						double mid = y2 + topSize;
						g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, mid));
						
						g.LineTo (new Cairo.PointD (right, mid));
						g.LineTo (new Cairo.PointD (right, y2));
						g.ClosePath ();
						g.Color = colorMatrix [active, TOP, DARK, highlighted, selected];
						g.Fill ();
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, mid));
						
						g.LineTo (new Cairo.PointD (right, mid));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.ClosePath ();
						
						g.Color = colorMatrix [active, BOTTOM, DARK, highlighted, selected];
						g.Fill ();
					}
					
// draw border
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
					g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
					
					g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2Bottom));
					g.LineTo (new Cairo.PointD (right, y2));
					g.ClosePath ();
					
					g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, selected];
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
					if (!(errors.Count == 1 && !CollapseExtendedErrors)) {
						g.MoveTo (new Cairo.PointD (x2 + 0.5, y2));
						g.LineTo (new Cairo.PointD (x2 - editor.LineHeight / 2 + 0.5, y2 + editor.LineHeight / 2));
						g.LineTo (new Cairo.PointD (x2 + 0.5, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2Bottom));
						g.LineTo (new Cairo.PointD (right, y2));
						g.ClosePath ();
					}
				}
				g.Color = colorMatrix [active, BOTTOM, LIGHT, highlighted, selected];
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
				
				g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, selected];
				g.Stroke ();
				
// stroke top line
				if (fitsInSameLine) {
					g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, selected];
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
					g.Color = colorMatrix [active, BOTTOM, DARK, highlighted, selected];
					g.Stroke ();
				}
			}

			
			for (int i = 0; i < layouts.Count; i++) {
				if (!IsCurrentErrorTextFitting (layout2) && !CollapseExtendedErrors)
					break;
				
				var layout = layouts [i];
				x2 = right - layout.Width - border - (ShowIconsInBubble ? cache.errorPixbuf.Width : 0);
				if (i == 0) {
					x2 -= errorCounterWidth;
					if (x2 < lineTextPx) {
						//			if (CollapseExtendedErrors) {
						x2 = lineTextPx;
						//			}
					}
				}
//				x2 = System.Math.Max (x2, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
				
				if (i > 0) {
					editor.TextViewMargin.DrawRectangleWithRuler (g, x, new Cairo.Rectangle (x, y, right, editor.LineHeight), isEolSelected ? editor.ColorStyle.SelectedText.Background : editor.ColorStyle.PlainText.Background, true);
					g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
					g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
					g.LineTo (new Cairo.PointD (right, y + editor.LineHeight));
					g.LineTo (new Cairo.PointD (right, y));
					g.ClosePath ();
					
					if (CollapseExtendedErrors) {
						using (var pat = new Cairo.LinearGradient (x2, y, x2, y + editor.LineHeight)) {
							pat.AddColorStop (0, colorMatrix [active, TOP, LIGHT, highlighted, selected]);
							pat.AddColorStop (1, colorMatrix [active, BOTTOM, LIGHT, highlighted, selected]);
							g.Pattern = pat;
						}
					} else {
						g.Color = colorMatrix [active, TOP, LIGHT, highlighted, selected];
					}
					g.Fill ();
					if (editor.Options.ShowRuler) {
						double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
						if (divider >= x2) {
							g.MoveTo (new Cairo.PointD (divider + 0.5, y));
							g.LineTo (new Cairo.PointD (divider + 0.5, y + editor.LineHeight));
							g.Color = colorMatrix [active, BOTTOM, DARK, highlighted, selected];
							g.Stroke ();
						}
					}
				}
				g.Color = (HslColor)(selected == 0 ? gc : cache.gcSelected);
				g.Save ();
				g.Translate (x2 + (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) + border, y + (editor.LineHeight - layout.Height) / 2 + layout.Height % 2);
				g.ShowLayout (layout.Layout);
				g.Restore ();
				y += editor.LineHeight;
				if (!UseVirtualLines)
					break;
			}
			return true;
		}
		
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

		public void DrawIcon (Mono.TextEditor.TextEditor editor, Cairo.Context cr, DocumentLine line, int lineNumber, double x, double y, double width, double height)
		{
			editor.GdkWindow.DrawPixbuf (cache.editor.Style.BaseGC (Gtk.StateType.Normal), 
				errors.Any (e => e.IsError) ? cache.errorPixbuf : cache.warningPixbuf, 
				0, 0, 
				(int)(x + (width - cache.errorPixbuf.Width) / 2), 
				(int)(y + (height - cache.errorPixbuf.Height) / 2), 
				cache.errorPixbuf.Width, cache.errorPixbuf.Height, 
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
				
				double y = editor.Allocation.Y + editor.LineToY (lineNumber) - (int)editor.VAdjustment.Value;
				double height = editor.LineHeight * errors.Count;
				if (!fitsInSameLine)
					y += editor.LineHeight;
				int errorCounterWidth = GetErrorCountBounds ().Item1;
				
				double labelWidth = LayoutWidth + border + (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) + errorCounterWidth;
				if (fitsInSameLine)
					labelWidth += editor.LineHeight / 2;
				
				var layout = editor.TextViewMargin.GetLayout (lineSegment);
				var lineTextPx = editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition + layout.PangoWidth / Pango.Scale.PangoScale;
				labelWidth = Math.Min (editor.Allocation.Width - lineTextPx - editor.TextViewMargin.TextStartPosition, labelWidth);
				
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
			int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
			double y = editor.LineToY (lineNumber) - editor.VAdjustment.Value;
			if (fitsInSameLine) {
				if (args.Y < y + 2 || args.Y > y + editor.LineHeight - 2)
					return false;
			} else {
				if (args.Y < y + editor.LineHeight + 2 || args.Y > y + editor.LineHeight * 2 - 2)
					return false;
			}
			
			int errorCounterWidth = GetErrorCountBounds ().Item1;
			if (errorCounterWidth > 0)
				return editor.Allocation.Width - editor.TextViewMargin.XOffset - 2 - errorCounterWidth <= args.X;
			return false;
		}
		
		bool IsCurrentErrorTextFitting (TextViewMargin.LayoutWrapper wrapper = null)
		{
			int errorCounterWidth = GetErrorCountBounds (wrapper).Item1;
			double labelWidth = LayoutWidth + border + (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) + errorCounterWidth + editor.LineHeight / 2;
			
			var layout = wrapper ?? editor.TextViewMargin.GetLayout (lineSegment);
			
			var lineTextPx = editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition + layout.PangoWidth / Pango.Scale.PangoScale;
			
			if (wrapper == null && layout.IsUncached)
				layout.Dispose ();
			
			return labelWidth < editor.Allocation.Width - lineTextPx - editor.TextViewMargin.TextStartPosition;
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
			int errorCounterWidth = GetErrorCountBounds ().Item1;
			
			double labelWidth = LayoutWidth + border + (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) + errorCounterWidth + editor.LineHeight / 2;
			
			var layout = editor.TextViewMargin.GetLayout (lineSegment);
			
			var lineTextPx = editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition + layout.PangoWidth / Pango.Scale.PangoScale;
			
			labelWidth = Math.Min (editor.Allocation.Width - lineTextPx - editor.TextViewMargin.TextStartPosition, labelWidth);
			
			if (editor.Allocation.Width - editor.TextViewMargin.XOffset - labelWidth < args.X)
				return error;
			
			return -1;
		}

		bool oldIsOver = false;

		public void MouseHover (TextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
		{
			if (this.LineSegment == null)
				return;
			bool isOver = MouseIsOverMarker (editor, args);
			if (isOver != oldIsOver)
				editor.Document.CommitLineUpdate (this.LineSegment);
			oldIsOver = isOver;
			
			int errorNumber = MouseIsOverError (editor, args);
			if (errorNumber >= 0) {
				result.Cursor = cache.arrowCursor;
				if (!isOver)
					// don't show tooltip when hovering over error counter layout.
					result.TooltipMarkup = GLib.Markup.EscapeText (errors[errorNumber].ErrorMessage);
			}
			
		}
		#endregion
		
		public override void Draw (TextEditor editor, Cairo.Context g, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
			var bounds = GetErrorCountBounds ();
			int errorCounterWidth = bounds.Item1;
			int eh = bounds.Item2;
			
			if (errorCounterWidth > 0)
				DrawMessageExtendIcon (editor, g, y, errorCounterWidth, eh);

		}

		#region IExtendingTextMarker implementation
		public void Draw (TextEditor editor, Cairo.Context g, int lineNr, Cairo.Rectangle lineArea)
		{
			EnsureLayoutCreated (editor);
			int lineNumber = editor.Document.OffsetToLineNumber (lineSegment.Offset);
			int errorNumber = lineNr - lineNumber;
			if (!IsCurrentErrorTextFitting ())
				errorNumber--;
			double x = editor.TextViewMargin.XOffset;
			double y = lineArea.Y;
			double right = editor.Allocation.Width;
			int errorCounterWidth = GetErrorCountBounds ().Item1;
//			int eh = GetErrorCountBounds ().Item2;
			
			double x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) - errorCounterWidth, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			if (errors.Count == 1)
				x2 = editor.TextViewMargin.XOffset;
//			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset  + lineSegment.EditableLength) : false;
			int active = editor.Document.GetTextAt (lineSegment) == initialText ? 0 : 1;
			bool isCaretInLine = lineSegment.Offset <= editor.Caret.Offset && editor.Caret.Offset <= lineSegment.EndOffsetIncludingDelimiter;
			int highlighted = active == 0 && isCaretInLine ? 1 : 0;
			int selected = 0;
			var layout = layouts [errorNumber];
			x2 = right - LayoutWidth - border - (ShowIconsInBubble ? cache.errorPixbuf.Width : 0);
			
			x2 -= errorCounterWidth;
			x2 = System.Math.Max (x2, fitsInSameLine ? editor.TextViewMargin.XOffset + editor.LineHeight / 2 : editor.TextViewMargin.XOffset);
			
			g.MoveTo (new Cairo.PointD (x2 + 0.5, y));
			g.LineTo (new Cairo.PointD (x2 + 0.5, y + editor.LineHeight));
			g.LineTo (new Cairo.PointD (right, y + editor.LineHeight));
			g.LineTo (new Cairo.PointD (right, y));
			g.ClosePath ();
			g.Color = colorMatrix [active, BOTTOM, LIGHT, highlighted, selected];
			g.Fill ();
			
			g.Color = colorMatrix [active, BOTTOM, LINE, highlighted, selected];
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
					g.Color = colorMatrix [active, BOTTOM, DARK, highlighted, selected];
					g.Stroke ();
				}
			}
			g.Save ();
			g.Translate (x2 + (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) + border, y + (editor.LineHeight - layout.Height) / 2 + layout.Height % 2);
			g.Color = selected == 0 ? gc : cache.gcSelected;
			g.ShowLayout (layout.Layout);
			g.Restore ();
			
//			if (ShowIconsInBubble)
//				win.DrawPixbuf (editor.Style.BaseGC (Gtk.StateType.Normal), errors[errorNumber].IsError ? errorPixbuf : warningPixbuf, 0, 0, x2, y + (editor.LineHeight - errorPixbuf.Height) / 2, errorPixbuf.Width, errorPixbuf.Height, Gdk.RgbDither.None, 0, 0);
		}
		
		#endregion
		
	}
}
