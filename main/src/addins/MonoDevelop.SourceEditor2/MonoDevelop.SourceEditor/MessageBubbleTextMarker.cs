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
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.SourceEditor.Wrappers;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	class MessageBubbleTextMarker : MarginMarker, IDisposable, IActionTextLineMarker, MonoDevelop.Ide.Editor.IMessageBubbleLineMarker
	{
		readonly MessageBubbleCache cache;
		
		internal const int border = 4;
		
		MonoTextEditor editor {
			get { return cache.editor;}
		}

		public override bool IsVisible {
			get { return !task.Completed; }
			set { task.Completed = !value; editor.QueueDraw (); }
		}

		public bool UseVirtualLines { get; set; }

		List<ErrorText> errors = new List<ErrorText> ();
		internal IList<ErrorText> Errors {
			get { return errors; }
		}

		TaskListEntry task;
		internal TaskListEntry Task {
			get {
				return this.task;
			}
		}

		TaskListEntry primaryTask;

//		int editorAllocHeight = -1;
//		int lastLineLength = -1;
		internal double lastHeight = 0;

		public double GetLineHeight (MonoTextEditor editor)
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

		public void SetPrimaryError (TaskListEntry task)
		{
			this.primaryTask = task;
			var text = task.Description;
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
				if (LineSegment != null && editor.Document.GetTextSegmentMarkersAt (LineSegment).Any (m => m is DebugTextMarker)) 
					return TextLineMarkerFlags.None;

				return TextLineMarkerFlags.DrawsSelection;
			}
		}

		bool isError;

		public MessageBubbleTextMarker (MessageBubbleCache cache)
		{
			if (cache == null)
				throw new ArgumentNullException ("cache");
			this.cache = cache;
			this.IsVisible = true;
		}

		internal MessageBubbleTextMarker (MessageBubbleCache cache, TaskListEntry task, bool isError, string errorMessage)
		{
			if (cache == null)
				throw new ArgumentNullException ("cache");
			this.cache = cache;
			this.task = task;
			this.isError = isError;
			AddError (task, isError, errorMessage);
//			cache.Changed += (sender, e) => CalculateLineFit (editor, lineSegment);
		}
		
		static System.Text.RegularExpressions.Regex mcsErrorFormat = new System.Text.RegularExpressions.Regex ("(.+)\\(CS\\d+\\)\\Z");
		public void AddError (TaskListEntry task, bool isError, string errorMessage)
		{
			if (this.task == null) {
				this.task = task;
			}
			var match = mcsErrorFormat.Match (errorMessage);
			string trimmedMessage = errorMessage;
			if (match.Success)
				trimmedMessage = match.Groups [1].Value;
			errors.Add (new ErrorText (task, isError, trimmedMessage, errorMessage));
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
		
		internal HslColor MarkerColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorMarker : EditorThemeColors.MessageBubbleWarningMarker);
			}
		}

		internal HslColor TagColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorTag : EditorThemeColors.MessageBubbleWarningTag);
			}
		}

		internal HslColor TagColor2 {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorTag2 : EditorThemeColors.MessageBubbleWarningTag2);
			}
		}

		internal HslColor TooltipColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorTooltip : EditorThemeColors.MessageBubbleWarningTooltip);
			}
		}

		internal HslColor LineColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorLine : EditorThemeColors.MessageBubbleWarningLine);
			}
		}

		internal HslColor LineColor2 {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorLine2 : EditorThemeColors.MessageBubbleWarningLine2);
			}
		}

		internal HslColor BorderLineColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorBorderLine : EditorThemeColors.MessageBubbleWarningBorderLine);
			}
		}


		internal HslColor CounterColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorCounter : EditorThemeColors.MessageBubbleWarningCounter);
			}
		}

		internal HslColor CounterColor2 {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorCounter2 : EditorThemeColors.MessageBubbleWarningCounter2);
			}
		}

		internal HslColor IconMarginColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorIconMargin : EditorThemeColors.MessageBubbleWarningIconMargin);
			}
		}

		internal HslColor IconMarginBorderColor {
			get {
				return Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, isError ? EditorThemeColors.MessageBubbleErrorIconMarginBorder : EditorThemeColors.MessageBubbleWarningIconMarginBorder);
			}
		}


		Cairo.Color BlendSelection (Cairo.Color color, bool selected)
		{
			if (!selected)
				return color;

			var selectionColor = (Cairo.Color)Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Selection);
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
			var selectionColor = (Cairo.Color)Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Background);
			const double bubbleAlpha = 0.7;
			return new Cairo.Color (
				(color.R * bubbleAlpha + selectionColor.R * (1 - bubbleAlpha)), 
				(color.G * bubbleAlpha + selectionColor.G * (1 - bubbleAlpha)), 
				(color.B * bubbleAlpha + selectionColor.B * (1 - bubbleAlpha))
				);
		}

		Cairo.Color GetLineColor (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (LineColor, highlighted), selected);
		}

		Cairo.Color GetMarkerColor (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (MarkerColor, highlighted), selected);
		}

		Cairo.Color GetLineColorBottom (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (LineColor2, highlighted), selected);
		}

		Cairo.Color GetLineColorBorder (bool highlighted, bool selected) 
		{
			return BlendSelection (Highlight (BorderLineColor, highlighted), selected);
		}

		internal IList<MessageBubbleCache.LayoutDescriptor> Layouts {
			get { return layouts; }
		}
		
		internal void EnsureLayoutCreated (MonoTextEditor editor)
		{
			if (layouts != null)
				return;
			
			layouts = new List<MessageBubbleCache.LayoutDescriptor> ();
			foreach (ErrorText errorText in errors) {
				layouts.Add (cache.CreateLayoutDescriptor (errorText));
			}
			
			if (errorCountLayout == null && errors.Count > 1) {
				errorCountLayout = new Pango.Layout (editor.PangoContext);
				errorCountLayout.FontDescription = cache.errorCountFontDescription;
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
		int curError = 0;
		public bool MousePressed (MonoTextEditor editor, MarginMouseEventArgs args)
		{
			if (bubbleDrawX < args.X && args.X < bubbleDrawX + bubbleWidth) {
				errors [curError].Task.SelectInPad ();
				curError = (curError + 1) % errors.Count;
				return true;
			}
			return false;
		}

		bool IActionTextLineMarker.MouseReleased (MonoTextEditor editor, MarginMouseEventArgs args)
		{
			return false;
		}

		public void MouseHover (MonoTextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
		{
			if (!IsVisible)
				return;
			if (base.LineSegment == null)
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
		
		public override void Draw (MonoTextEditor editor, Cairo.Context g, LineMetrics metrics)
		{

		}

		public override void DrawAfterEol (MonoTextEditor textEditor, Cairo.Context g, EndOfLineMetrics metrics)
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
			var y = metrics.LineYRenderStartPosition;
			bool customLayout = true; //sx + width > editor.Allocation.Width;
			bool hideText = false;
			bubbleIsReduced = customLayout;
			var showErrorCount = errorCounterWidth > 0 && errorCountLayout != null;
			double roundingRadius = editor.LineHeight / 2 - 1;

			if (customLayout) {
				width = editor.Allocation.Width - sx;
				string text = layouts[0].Layout.Text;
				drawLayout = new Pango.Layout (editor.PangoContext);
				drawLayout.FontDescription = cache.fontDescription;
				var paintWidth = (width - errorCounterWidth - editor.LineHeight + 4);
				var minWidth = Math.Max (25, errorCounterWidth) * editor.Options.Zoom;
				if (paintWidth < minWidth) {
					hideText = true;
					showErrorCount = false;
//					drawLayout.SetMarkup ("<span weight='heavy'>···</span>");
					width = minWidth;
					//roundingRadius = 10 * editor.Options.Zoom;
					sx = Math.Min (sx, editor.Allocation.Width - width);
				} else {
					drawLayout.Ellipsize = Pango.EllipsizeMode.End;
					drawLayout.Width = (int)(paintWidth * Pango.Scale.PangoScale);
					drawLayout.SetText (text);
					int w2, h2;
					drawLayout.GetPixelSize (out w2, out h2);
					width = w2 + errorCounterWidth + editor.LineHeight - 2;
				}
			}

			bubbleDrawX = sx - editor.TextViewMargin.XOffset;
			bubbleDrawY = y + 2;
			bubbleWidth = width;
			var bubbleHeight = editor.LineHeight;

			g.RoundedRectangle (sx, y, width, bubbleHeight, roundingRadius);
			g.SetSourceColor (TagColor);
			g.Fill ();

			// Draw error count icon
			if (showErrorCount) {
				var errorCounterHeight = bubbleHeight - 2;
				var errorCounterX = sx + width - errorCounterWidth - 1;
				var errorCounterY = Math.Round (y + (bubbleHeight - errorCounterHeight) / 2);

				g.RoundedRectangle (
					errorCounterX, 
					errorCounterY, 
					errorCounterWidth, 
					errorCounterHeight, 
					editor.LineHeight / 2 - 2
				);

				// FIXME: VV: Remove gradient features
				using (var lg = new Cairo.LinearGradient (errorCounterX, errorCounterY, errorCounterX, errorCounterY + errorCounterHeight)) {
					lg.AddColorStop (0, CounterColor);
					lg.AddColorStop (1, CounterColor.AddLight (-0.1));
					g.SetSource (lg);
					g.Fill ();
				}

				g.Save ();

				int ew;
				errorCountLayout.GetPixelSize (out ew, out eh);

				var tx = Math.Round (errorCounterX + (2 + errorCounterWidth - ew) / 2);
				var ty = Math.Round (errorCounterY + (-1 + errorCounterHeight - eh) / 2);

				g.Translate (tx, ty);
				g.SetSourceColor (CounterColor2);
				g.ShowLayout (errorCountLayout);
				g.Restore ();
			}

			if (hideText) {
				// Draw dots
				double radius = 2 * editor.Options.Zoom;
				double spacing = 1 * editor.Options.Zoom;

				sx += 1 * editor.Options.Zoom + Math.Ceiling((bubbleWidth - 3 * (radius * 2) - 2 * spacing) / 2);

				for (int i = 0; i < 3; i++) {
					g.Arc (sx, y + bubbleHeight / 2, radius, 0, Math.PI * 2);
					g.SetSourceColor (TagColor2);
					g.Fill ();
					sx += radius * 2 + spacing;
				}
			} else {
				// Draw label text
				var tx = Math.Round (sx + editor.LineHeight / 2);
				var ty = Math.Round (y + (editor.LineHeight - layouts [0].Height) / 2) - 1;

				g.Save ();
				g.Translate (tx, ty);

				g.SetSourceColor (TagColor2);
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

		void DrawIconMarginBackground (MonoTextEditor ed, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			cr.Rectangle (metrics.X, metrics.Y, metrics.Width, metrics.Height);
			cr.SetSourceColor (IconMarginColor);
			cr.Fill ();
			cr.MoveTo (metrics.Right - 0.5, metrics.Y);
			cr.LineTo (metrics.Right - 0.5, metrics.Bottom);
			cr.SetSourceColor (IconMarginBorderColor);
			cr.Stroke ();
			if (cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this) {
				cr.Rectangle (metrics.X, metrics.Y, metrics.Width, metrics.Height);
				var color = (Cairo.Color)IconMarginColor;
				cr.SetSourceRGBA (color.R, color.G, color.B, 0.5);
				cr.Fill ();
			}
		}

		public override void DrawForeground (MonoTextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			var tx = Math.Round (metrics.X + (metrics.Width - MessageBubbleCache.errorPixbuf.Width) / 2) - 1;
			var ty = Math.Floor (metrics.Y + (metrics.Height - MessageBubbleCache.errorPixbuf.Height) / 2);

			cr.Save ();
			cr.Translate (tx, ty);
			cr.DrawImage (editor, errors.Any (e => e.IsError) ? MessageBubbleCache.errorPixbuf : MessageBubbleCache.warningPixbuf, 0, 0);
			cr.Restore ();
		}

		public override bool DrawBackground (MonoTextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			if (metrics.Margin is FoldMarkerMargin || metrics.Margin is GutterMargin || metrics.Margin is ActionMargin)
				return DrawMarginBackground (editor, metrics.Margin, cr, metrics.Area, LineSegment, metrics.LineNumber, metrics.X, metrics.Y, metrics.Height);
			if (metrics.Margin is IconMargin) {
				DrawIconMarginBackground (editor, cr, metrics);
				return true;
			}
			return false;
		}

		bool DrawMarginBackground (MonoTextEditor e, Margin margin, Cairo.Context cr, Cairo.Rectangle area, DocumentLine documentLine, long line, double x, double y, double lineHeight)
		{
			if (cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this)
				return false;
			cr.Rectangle (x, y, margin.Width, lineHeight);
			cr.SetSourceColor (LineColor);
			cr.Fill ();
			return true;
		}


		#endregion

		#region text background

		public override bool DrawBackground (MonoTextEditor editor, Cairo.Context g, LineMetrics metrics)
		{
			if (!IsVisible)
				return false;
			bool markerShouldDrawnAsHidden = cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this;
			if (editor.Document.GetTextSegmentMarkersAt (metrics.LineSegment).Any (m => m is DebugTextMarker))
				return false;

			EnsureLayoutCreated (editor);
			double x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			bool isCaretInLine = metrics.TextStartOffset <= editor.Caret.Offset && editor.Caret.Offset <= metrics.TextEndOffset;
			int errorCounterWidth = GetErrorCountBounds (metrics).Item1;

			var min = right - LayoutWidth - border - (ShowIconsInBubble ? MessageBubbleCache.errorPixbuf.Width : 0) - errorCounterWidth;
			var max = Math.Round (editor.TextViewMargin.XOffset + editor.LineHeight / 2);
			double x2 = Math.Max (min, max);

			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != MonoDevelop.Ide.Editor.SelectionMode.Block ? editor.SelectionRange.Contains (LineSegment.Offset + LineSegment.Length) : false;

			int active = 0;
			bool highlighted = active == 0 && isCaretInLine;
			var y = metrics.LineYRenderStartPosition;
			// draw background
			if (!markerShouldDrawnAsHidden) {
				DrawRectangle (g, x, y, right, editor.LineHeight);
				g.SetSourceColor (LineColor);
				g.Fill ();

				if (metrics.Layout.StartSet || metrics.SelectionStart == metrics.TextEndOffset) {
					double startX;
					double endX;

					if (metrics.SelectionStart != metrics.TextEndOffset) {
						var start = metrics.Layout.IndexToPos ((int)metrics.Layout.SelectionStartIndex);
						startX = (int)(start.X / Pango.Scale.PangoScale);
						var end = metrics.Layout.IndexToPos ((int)metrics.Layout.SelectionEndIndex);
						endX = (int)(end.X / Pango.Scale.PangoScale);
					} else {
						startX = x2;
						endX = startX;
					}

					if (editor.MainSelection.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block && startX == endX)
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

		void DrawErrorMarkers (MonoTextEditor editor, Cairo.Context g, LineMetrics metrics, double y)
		{
			uint curIndex = 0, byteIndex = 0;

			var o = metrics.LineSegment.Offset;

			foreach (var task in errors.Select (t => t.Task)) {
				try {
					var column = (uint)(Math.Min (Math.Max (0, task.Column - 1), metrics.Layout.Text.Length));
					var line = editor.GetLine (task.Line);
					// skip possible white space locations 
					while (column < line.Length && char.IsWhiteSpace (editor.GetCharAt (line.Offset + (int)column))) {
						column++;
					}
					if (column >= metrics.Layout.Text.Length)
						continue;
					int index = (int)metrics.Layout.TranslateToUTF8Index (column, ref curIndex, ref byteIndex);
					var pos = metrics.Layout.IndexToPos (index);
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
				} catch (Exception e) {
					LoggingService.LogError ("Error while drawing task marker " + task, e);
				}				
			}
		}

		#endregion

		MonoDevelop.Ide.Editor.IDocumentLine MonoDevelop.Ide.Editor.ITextLineMarker.Line {
			get {
				return base.LineSegment;
			}
		}

		void MonoDevelop.Ide.Editor.IMessageBubbleLineMarker.AddTask (TaskListEntry task)
		{
			AddError (task, task.Severity == TaskSeverity.Error, task.Description);
		}

		TaskListEntry MonoDevelop.Ide.Editor.IMessageBubbleLineMarker.PrimaryTask {
			get {
				return primaryTask;
			}
			set {
				SetPrimaryError (task);
			}
		}

		int MonoDevelop.Ide.Editor.IMessageBubbleLineMarker.TaskCount {
			get {
				return errors.Count;
			}
		}

		IEnumerable<TaskListEntry> MonoDevelop.Ide.Editor.IMessageBubbleLineMarker.Tasks {
			get {
				return errors.Select (e => e.Task);
			}
		}

	}
}
