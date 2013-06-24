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
	partial class MessageBubbleTextMarker : TextLineMarker, IDisposable, IActionTextLineMarker
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
			this.Flags = TextLineMarkerFlags.DrawsSelection;
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
			EnsureLayoutCreated (editor);
			double x = editor.TextViewMargin.XOffset;
			int errorCounterWidth = GetErrorCountBounds (metrics.Layout).Item1;

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
			g.RoundedRectangle (sx, y + 1, width, editor.LineHeight - 2, editor.LineHeight / 2 - 1);
			g.Color = TagColor.Color;
			g.Fill ();

			if (errorCounterWidth > 0 && errorCountLayout != null) {
				g.RoundedRectangle (sx + width - errorCounterWidth - editor.LineHeight / 2, y + 2, errorCounterWidth, editor.LineHeight - 4, editor.LineHeight / 2 - 3);
				g.Color = CounterColor.Color;
				g.Fill ();

				g.Save ();
				int ew, eh;
				errorCountLayout.GetPixelSize (out ew, out eh);
				g.Translate (sx + width - errorCounterWidth - editor.LineHeight / 2 + (errorCounterWidth - ew) / 2, y + 1);
				g.Color = CounterColor.SecondColor;
				g.ShowLayout (errorCountLayout);
				g.Restore ();
			}

			if (errorCounterWidth <= 0 || errorCountLayout == null || !hideText) {
				g.Save ();
				g.Translate (sx + editor.LineHeight / 2, y + (editor.LineHeight - layouts [0].Height) / 2 + layouts [0].Height % 2);
				g.Color = TagColor.SecondColor;
				g.ShowLayout (drawLayout);
				g.Restore ();
			}

			if (customLayout)
				drawLayout.Dispose ();

		}
	}
}
