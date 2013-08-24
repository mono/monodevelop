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
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.SourceEditor
{
	class MessageBubbleTextMarker : MarginMarker, IDisposable, IActionTextLineMarker
	{
		readonly MessageBubbleCache cache;
		
		internal const int border = 4;
		
		TextEditor editor {
			get { return cache.editor;}
		}

		public override bool IsVisible {
			get { return !task.Completed; }
			set { task.Completed = !value; }
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

		public override TextLineMarkerFlags Flags {
			get {
				if (lineSegment != null && lineSegment.Markers.Any (m => m is DebugTextMarker)) 
					return TextLineMarkerFlags.None;

				return TextLineMarkerFlags.DrawsSelection;
			}
		}

		string initialText;
		bool isError;
		internal MessageBubbleTextMarker (MessageBubbleCache cache, Task task, DocumentLine lineSegment, bool isError, string errorMessage)
		{
			if (cache == null)
				throw new ArgumentNullException ("cache");
			this.cache = cache;
			this.task = task;
			this.IsVisible = true;
			this.lineSegment = lineSegment;
			this.initialText = editor.Document.GetTextAt (lineSegment);
			this.isError = isError;
			AddError (task, isError, errorMessage);
//			cache.Changed += (sender, e) => CalculateLineFit (editor, lineSegment);
		}
		
		static System.Text.RegularExpressions.Regex mcsErrorFormat = new System.Text.RegularExpressions.Regex ("(.+)\\(CS\\d+\\)\\Z");
		public void AddError (Task task, bool isError, string errorMessage)
		{
			var match = mcsErrorFormat.Match (errorMessage);
			if (match.Success)
				errorMessage = match.Groups [1].Value;
			errors.Add (new ErrorText (task, isError, errorMessage));
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
			cache.DestroyPopoverWindow ();
		}
		
		internal Pango.Layout errorCountLayout;
		List<MessageBubbleCache.LayoutDescriptor> layouts;
		
		internal AmbientColor MarkerColor {
			get {
				return isError ? editor.ColorStyle.MessageBubbleErrorMarker : editor.ColorStyle.MessageBubbleWarningMarker;
			}
		}

		internal AmbientColor TagColor {
			get {
				return isError ? editor.ColorStyle.MessageBubbleErrorTag : editor.ColorStyle.MessageBubbleWarningTag;
			}
		}

		internal AmbientColor TooltipColor {
			get {
				return isError ? editor.ColorStyle.MessageBubbleErrorTooltip : editor.ColorStyle.MessageBubbleWarningTooltip;
			}
		}

		internal AmbientColor LineColor {
			get {
				return isError ? editor.ColorStyle.MessageBubbleErrorLine : editor.ColorStyle.MessageBubbleWarningLine;
			}
		}

		internal AmbientColor CounterColor {
			get {
				return isError ? editor.ColorStyle.MessageBubbleErrorCounter : editor.ColorStyle.MessageBubbleWarningCounter;
			}
		}

		internal AmbientColor IconMarginColor {
			get {
				return isError ? editor.ColorStyle.MessageBubbleErrorIconMargin : editor.ColorStyle.MessageBubbleWarningIconMargin;
			}
		}

		Cairo.Color BlendSelection (Cairo.Color color, bool selected)
		{
			if (!selected)
				return color;
			var selectionColor = editor.ColorStyle.SelectedText.Background;
			const double bubbleAlpha = 0.1;
			return new Cairo.Color (
				(color.R * bubbleAlpha + selectionColor.R * (1 - bubbleAlpha)), 
				(color.G * bubbleAlpha + selectionColor.G * (1 - bubbleAlpha)), 
				(color.B * bubbleAlpha + selectionColor.B * (1 - bubbleAlpha))
				);
		}

		Cairo.Color Highlight (Cairo.Color color, bool highlighted)
		{
			if (!highlighted)
				return color;
			var selectionColor = editor.ColorStyle.PlainText.Background;
			const double bubbleAlpha = 0.7;
			return new Cairo.Color (
				(color.R * bubbleAlpha + selectionColor.R * (1 - bubbleAlpha)), 
				(color.G * bubbleAlpha + selectionColor.G * (1 - bubbleAlpha)), 
				(color.B * bubbleAlpha + selectionColor.B * (1 - bubbleAlpha))
				);
		}

		Cairo.Color GetLineColor (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (LineColor.Color, highlighted), selected);
		}

		Cairo.Color GetMarkerColor (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (MarkerColor.Color, highlighted), selected);
		}

		Cairo.Color GetLineColorBottom (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (LineColor.SecondColor, highlighted), selected);
		}

		Cairo.Color GetLineColorBorder (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (LineColor.BorderColor, highlighted), selected);
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
				errorCountLayout.FontDescription = FontService.GetFontDescription ("MessageBubbles");
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
				return layouts [0].Width;
			}
		}
		
		Tuple<int, int> GetErrorCountBounds (LineMetrics metrics)
		{
			EnsureLayoutCreated (editor);
			var lineTextPx = editor.TextViewMargin.XOffset + metrics.TextRenderEndPosition;
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

		#region IActionTextMarker implementation
		public bool MousePressed (TextEditor editor, MarginMouseEventArgs args)
		{
			return false;
		}

		public void MouseHover (TextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
		{
			if (!IsVisible)
				return;
			if (LineSegment == null)
				return;
			if (bubbleDrawX < args.X && args.X < bubbleDrawX + bubbleWidth) {
				editor.HideTooltip ();
				result.Cursor = null;
				cache.StartHover (this, bubbleDrawX, bubbleDrawY, bubbleWidth, bubbleIsReduced);
			}
		}
		#endregion

		double bubbleDrawX, bubbleDrawY;
		double bubbleWidth;
		bool bubbleIsReduced;
		
		public override void Draw (TextEditor editor, Cairo.Context g, double y, LineMetrics metrics)
		{

		}

		public override void DrawAfterEol (TextEditor textEditor, Cairo.Context g, double y, EndOfLineMetrics metrics)
		{
			if (!IsVisible)
				return;
			EnsureLayoutCreated (editor);
			int errorCounterWidth = 0, eh = 0;
			if (errorCountLayout != null) {
				errorCountLayout.GetPixelSize (out errorCounterWidth, out eh);
				errorCounterWidth = Math.Max (15, Math.Max (errorCounterWidth + 3, (int)(editor.LineHeight * 3 / 4)));
			}

			var sx = metrics.TextRenderEndPosition;
			var width = LayoutWidth + errorCounterWidth + editor.LineHeight;
			var drawLayout = layouts[0].Layout;
			int ex = 0 , ey = 0;
			bool customLayout = sx + width > editor.Allocation.Width;
			bool hideText = false;
			bubbleIsReduced = customLayout;
			if (customLayout) {
				width = editor.Allocation.Width - sx;
				string text = layouts[0].Layout.Text;
				drawLayout = new Pango.Layout (editor.PangoContext);
				drawLayout.FontDescription = cache.fontDescription;
				for (int j = text.Length - 4; j > 0; j--) {
					drawLayout.SetText (text.Substring (0, j) + "...");
					drawLayout.GetPixelSize (out ex, out ey);
					if (ex + (errorCountLayout != null ? errorCounterWidth : 0) + editor.LineHeight < width)
						break;
				}
				if (ex + (errorCountLayout != null ? errorCounterWidth : 0) + editor.LineHeight > width) {
					hideText = true;
					drawLayout.SetMarkup ("<span weight='heavy'>···</span>");
					width = Math.Max (17, errorCounterWidth) + editor.LineHeight;
					sx = Math.Min (sx, editor.Allocation.Width - width);
				}
			}
			bubbleDrawX = sx - editor.TextViewMargin.XOffset;
			bubbleDrawY = y;
			bubbleWidth = width;

			var bubbleHeight = editor.LineHeight - 1;
			g.RoundedRectangle (sx, y + 1, width, bubbleHeight, editor.LineHeight / 2 - 1);
			g.SetSourceColor (TagColor.Color);
			g.Fill ();

			// Draw error count icon
			if (errorCounterWidth > 0 && errorCountLayout != null) {
				var errorCounterHeight = bubbleHeight - 2;
				var errorCounterX = sx + width - errorCounterWidth - 3;
				var errorCounterY = y + 1 + (bubbleHeight - errorCounterHeight) / 2;

				g.RoundedRectangle (
					errorCounterX - 1, 
					errorCounterY - 1, 
					errorCounterWidth + 2, 
					errorCounterHeight + 2, 
					editor.LineHeight / 2 - 3
				);

				g.SetSourceColor (new Cairo.Color (0, 0, 0, 0.081));
				g.Fill ();

				g.RoundedRectangle (
					errorCounterX, 
					errorCounterY, 
					errorCounterWidth, 
					errorCounterHeight, 
					editor.LineHeight / 2 - 3
					);
				using (var lg = new Cairo.LinearGradient (errorCounterX, errorCounterY, errorCounterX, errorCounterY + errorCounterHeight)) {
					lg.AddColorStop (0, CounterColor.Color);
					lg.AddColorStop (1, CounterColor.Color.AddLight (-0.1));
					g.Pattern = lg;
					g.Fill ();
				}

				g.Save ();

				int ew;
				errorCountLayout.GetPixelSize (out ew, out eh);

				g.Translate (
					errorCounterX + (errorCounterWidth - ew) / 2,
					errorCounterY + (errorCounterHeight - eh) / 2
				);
				g.SetSourceColor (CounterColor.SecondColor);
				g.ShowLayout (errorCountLayout);
				g.Restore ();
			}

			// Draw label text
			if (errorCounterWidth <= 0 || errorCountLayout == null || !hideText) {
				g.Save ();
				g.Translate (sx + editor.LineHeight / 2, y + (editor.LineHeight - layouts [0].Height) / 2 + 1);

				// draw shadow
				g.SetSourceColor (MessageBubbleCache.ShadowColor);
				g.ShowLayout (drawLayout);
				g.Translate (0, -1);

				g.SetSourceColor (TagColor.SecondColor);
				g.ShowLayout (drawLayout);
				g.Restore ();
			}

			if (customLayout)
				drawLayout.Dispose ();

		}

		#region MarginMarker

		public override bool CanDrawBackground (Margin margin)
		{
			if (!IsVisible)
				return false;
			return margin is FoldMarkerMargin || margin is GutterMargin || margin is IconMargin || margin is ActionMargin;
		}

		public override bool CanDrawForeground (Margin margin)
		{
			if (!IsVisible)
				return false;
			return margin is IconMargin;
		}

		void DrawIconMarginBackground (TextEditor ed, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			cr.Rectangle (metrics.X, metrics.Y, metrics.Width, metrics.Height);
			cr.SetSourceColor (IconMarginColor.Color);
			cr.Fill ();
			cr.MoveTo (metrics.Right - 0.5, metrics.Y);
			cr.LineTo (metrics.Right - 0.5, metrics.Bottom);
			cr.SetSourceColor (IconMarginColor.BorderColor);
			cr.Stroke ();
			if (cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this) {
				cr.Rectangle (metrics.X, metrics.Y, metrics.Width, metrics.Height);
				cr.SetSourceRGBA (ed.ColorStyle.IndicatorMargin.Color.R, ed.ColorStyle.IndicatorMargin.Color.G, ed.ColorStyle.IndicatorMargin.Color.B, 0.5);
				cr.Fill ();
			}
		}

		public override void DrawForeground (TextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			cr.Save ();
			cr.Translate (
				metrics.X + 0.5 + (metrics.Width - 2 - cache.errorPixbuf.Width) / 2,
				metrics.Y + 0.5 + (metrics.Height - cache.errorPixbuf.Height) / 2
				);
			Gdk.CairoHelper.SetSourcePixbuf (
				cr,
				errors.Any (e => e.IsError) ? cache.errorPixbuf : cache.warningPixbuf, 0, 0);
			cr.Paint ();
			cr.Restore ();

		}

		public override bool DrawBackground (TextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			if (metrics.Margin is FoldMarkerMargin || metrics.Margin is GutterMargin || metrics.Margin is ActionMargin)
				return DrawMarginBackground (editor, metrics.Margin, cr, metrics.Area, lineSegment, metrics.LineNumber, metrics.X, metrics.Y, metrics.Height);
			if (metrics.Margin is IconMargin) {
				DrawIconMarginBackground (editor, cr, metrics);
				return true;
			}
			return false;
		}

		bool DrawMarginBackground (TextEditor e, Margin margin, Cairo.Context cr, Cairo.Rectangle area, DocumentLine documentLine, long line, double x, double y, double lineHeight)
		{
			if (cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this)
				return false;
			cr.Rectangle (x, y, margin.Width, lineHeight);
			cr.SetSourceColor (LineColor.Color);
			cr.Fill ();
			return true;
		}


		#endregion

		#region text background

		public override bool DrawBackground (TextEditor editor, Cairo.Context g, double y, LineMetrics metrics)
		{
			if (!IsVisible)
				return false;
			bool markerShouldDrawnAsHidden = cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this;
			if (metrics.LineSegment.Markers.Any (m => m is DebugTextMarker))
				return false;

			EnsureLayoutCreated (editor);
			double x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			bool isCaretInLine = metrics.TextStartOffset <= editor.Caret.Offset && editor.Caret.Offset <= metrics.TextEndOffset;
			int errorCounterWidth = GetErrorCountBounds (metrics).Item1;

			double x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) - errorCounterWidth, editor.TextViewMargin.XOffset + editor.LineHeight / 2);

			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != Mono.TextEditor.SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset + lineSegment.Length) : false;

			int active = editor.Document.GetTextAt (lineSegment) == initialText ? 0 : 1;
			bool highlighted = active == 0 && isCaretInLine;

			// draw background
			if (!markerShouldDrawnAsHidden) {
				DrawRectangle (g, x, y, right, editor.LineHeight);
				g.SetSourceColor (LineColor.Color);
				g.Fill ();

				if (metrics.Layout.StartSet || metrics.SelectionStart == metrics.TextEndOffset) {
					double startX;
					double endX;

					if (metrics.SelectionStart != metrics.TextEndOffset) {
						var start = metrics.Layout.Layout.IndexToPos ((int)metrics.Layout.SelectionStartIndex);
						startX = (int)(start.X / Pango.Scale.PangoScale);
						var end = metrics.Layout.Layout.IndexToPos ((int)metrics.Layout.SelectionEndIndex);
						endX = (int)(end.X / Pango.Scale.PangoScale);
					} else {
						startX = x2;
						endX = startX;
					}

					if (editor.MainSelection.SelectionMode == Mono.TextEditor.SelectionMode.Block && startX == endX)
						endX = startX + 2;
					startX += metrics.TextRenderStartPosition;
					endX += metrics.TextRenderStartPosition;
					startX = Math.Max (editor.TextViewMargin.XOffset, startX);
					// clip region to textviewmargin start
					if (isEolSelected)
						endX = editor.Allocation.Width + (int)editor.HAdjustment.Value;
					if (startX < endX) {
						DrawRectangle (g, startX, y, endX - startX, editor.LineHeight);
						g.SetSourceColor (GetLineColor (highlighted, true));
						g.Fill ();
					}
				}
				DrawErrorMarkers (editor, g, metrics, y);
			}

			double y2 = y + 0.5;
			double y2Bottom = y2 + editor.LineHeight - 1;
			var selected = isEolSelected;
			var lineTextPx = editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition + metrics.Layout.Width;
			if (x2 < lineTextPx) 
				x2 = lineTextPx;

			if (editor.Options.ShowRuler) {
				double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
				if (divider >= x2) {
					g.MoveTo (new Cairo.PointD (divider + 0.5, y2));
					g.LineTo (new Cairo.PointD (divider + 0.5, y2Bottom));
					g.SetSourceColor (GetLineColorBorder (highlighted, selected));
					g.Stroke ();
				}
			}

			return true;
		}

		void DrawErrorMarkers (TextEditor editor, Cairo.Context g, LineMetrics metrics, double y)
		{
			uint curIndex = 0, byteIndex = 0;

			var o = metrics.LineSegment.Offset;

			foreach (var task in errors.Select (t => t.Task)) {
				var column = (uint)(Math.Min (Math.Max (0, task.Column - 1), metrics.Layout.LineChars.Length));
				int index = (int)metrics.Layout.TranslateToUTF8Index (column, ref curIndex, ref byteIndex);
				var pos = metrics.Layout.Layout.IndexToPos (index);
				var co = o + task.Column - 1;
				g.SetSourceColor (GetMarkerColor (false, metrics.SelectionStart <= co && co < metrics.SelectionEnd));
				g.MoveTo (
					metrics.TextRenderStartPosition + editor.TextViewMargin.TextStartPosition + pos.X / Pango.Scale.PangoScale,
					y + editor.LineHeight - 3
					);
				g.RelLineTo (3, 3);
				g.RelLineTo (-6, 0);
				g.ClosePath ();

				g.Fill ();
			}
		}

		#endregion
	}
}
