// DebugTextMarker.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
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
using System.Linq;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.SourceEditor.Wrappers;
using MonoDevelop.Components;

using Xwt.Drawing;
using MonoDevelop.Debugger;

namespace MonoDevelop.SourceEditor
{
	abstract class DebugTextMarker : MarginMarker
	{
		protected DebugTextMarker (TextEditor editor)
		{
			Editor = editor;
		}

		protected abstract Cairo.Color BackgroundColor {
			get;
		}
		
		protected abstract Cairo.Color BorderColor {
			get;
		}
		
		protected Cairo.Color GetBorderColor (AmbientColor color) 
		{
			if (color.HasBorderColor)
				return color.BorderColor;
			return color.Color;
		}

		protected TextEditor Editor {
			get; private set;
		}

		public override bool CanDrawBackground (Margin margin)
		{
			return margin is TextViewMargin;
		}

		public override bool CanDrawForeground (Margin margin)
		{
			return margin is IconMargin;
		}

		public override bool DrawBackground (TextEditor editor, Cairo.Context cr, LineMetrics metrics)
		{
			// check, if a message bubble is active in that line.
			if (LineSegment != null && LineSegment.Markers.Any (m => m != this && (m is IExtendingTextLineMarker)))
				return false;

			var sidePadding = 4;
			var rounding = editor.LineHeight / 2 - 1;
			var y = metrics.LineYRenderStartPosition;
			var d = metrics.TextRenderEndPosition - metrics.TextRenderStartPosition;
			if (d > 0) {
				cr.LineWidth = 1;
				cr.RoundedRectangle (metrics.TextRenderStartPosition, Math.Floor (y) + 0.5, d + sidePadding, metrics.LineHeight - 1, rounding);
				cr.SetSourceColor (BackgroundColor); 
				cr.FillPreserve ();
				cr.SetSourceColor (BorderColor); 
				cr.Stroke ();
			}

			return base.DrawBackground (editor, cr, metrics);
		}

		public override void DrawForeground (TextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			double size = metrics.Margin.Width;
			double borderLineWidth = cr.LineWidth;

			double x = Math.Floor (metrics.Margin.XOffset - borderLineWidth / 2);
			double y = Math.Floor (metrics.Y + (metrics.Height - size) / 2);

			DrawMarginIcon (cr, x, y, size);
		}

		protected virtual void SetForegroundColor (ChunkStyle style)
		{
		}

		public override ChunkStyle GetStyle (ChunkStyle baseStyle)
		{
			if (baseStyle == null)
				return null;

			var style = new ChunkStyle (baseStyle);
			//			style.Background = BackgroundColor;
			SetForegroundColor (style);

			return style;
		}

		protected void DrawImage (Cairo.Context cr, Image image, double x, double y, double size)
		{
			var deltaX = size / 2 - image.Width / 2 + 0.5f;
			var deltaY = size / 2 - image.Height / 2 + 0.5f;

			cr.DrawImage (Editor, image, Math.Round (x + deltaX), Math.Round (y + deltaY));
		}

		protected virtual void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
		}
	}

	class BreakpointTextMarker : DebugTextMarker
	{
		static readonly Image breakpoint = Image.FromResource (typeof(BreakpointPad), "gutter-breakpoint-light-15.png");
		static readonly Image tracepoint = Image.FromResource (typeof(BreakpointPad), "gutter-tracepoint-light-15.png");

		public BreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
		{
			IsTracepoint = tracepoint;
		}

		public bool IsTracepoint {
			get; private set;
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.BreakpointMarker.Color; }
		}
		
		protected override Cairo.Color BorderColor {
			get { return GetBorderColor (Editor.ColorStyle.BreakpointMarker); }
		}

		protected override void SetForegroundColor (ChunkStyle style)
		{
			style.Foreground = Editor.ColorStyle.BreakpointText.Foreground;
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			DrawImage (cr, IsTracepoint ? tracepoint : breakpoint, x, y, size);
		}
	}

	class DisabledBreakpointTextMarker : DebugTextMarker
	{
		static readonly Image breakpoint = Image.FromResource (typeof(BreakpointPad), "gutter-breakpoint-disabled-light-15.png");
		static readonly Image tracepoint = Image.FromResource (typeof(BreakpointPad), "gutter-tracepoint-disabled-light-15.png");

		public DisabledBreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
		{
			IsTracepoint = tracepoint;
		}

		public bool IsTracepoint {
			get; private set;
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.BreakpointMarkerDisabled.Color; }
		}
		
		protected override Cairo.Color BorderColor {
			get { return GetBorderColor (Editor.ColorStyle.BreakpointMarkerDisabled); }
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			DrawImage (cr, IsTracepoint ? tracepoint : breakpoint, x, y, size);
		}
	}

	class InvalidBreakpointTextMarker : DebugTextMarker
	{
		static readonly Image breakpoint = Image.FromResource (typeof(BreakpointPad), "gutter-breakpoint-invalid-light-15.png");
		static readonly Image tracepoint = Image.FromResource (typeof(BreakpointPad), "gutter-tracepoint-invalid-light-15.png");

		public InvalidBreakpointTextMarker (TextEditor editor, bool tracepoint) : base (editor)
		{
			IsTracepoint = tracepoint;
		}

		public bool IsTracepoint {
			get; private set;
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.BreakpointMarkerInvalid.Color; }
		}
		
		protected override Cairo.Color BorderColor {
			get { return GetBorderColor (Editor.ColorStyle.BreakpointMarkerInvalid); }
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			DrawImage (cr, IsTracepoint ? tracepoint : breakpoint, x, y, size);
		}
	}

	class CurrentDebugLineTextMarker : DebugTextMarker, MonoDevelop.Ide.Editor.ICurrentDebugLineTextMarker
	{
		static readonly Image currentLine = Image.FromResource (typeof(BreakpointPad), "gutter-execution-light-15.png");

		public CurrentDebugLineTextMarker (TextEditor editor) : base (editor)
		{
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.DebuggerCurrentLineMarker.Color; }
		}

		protected override Cairo.Color BorderColor {
			get { return GetBorderColor (Editor.ColorStyle.DebuggerCurrentLineMarker); }
		}

		protected override void SetForegroundColor (ChunkStyle style)
		{
			style.Foreground = Editor.ColorStyle.DebuggerCurrentLine.Foreground;
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			DrawImage (cr, currentLine, x, y, size);
		}

		MonoDevelop.Ide.Editor.IDocumentLine MonoDevelop.Ide.Editor.ITextLineMarker.Line {
			get {
				return new DocumentLineWrapper (base.LineSegment);
			}
		}
	}

	class DebugStackLineTextMarker : DebugTextMarker
	{
		static readonly Image stackLine = Image.FromResource (typeof(BreakpointPad), "gutter-stack-light-15.png");

		public DebugStackLineTextMarker (TextEditor editor) : base (editor)
		{
		}

		protected override Cairo.Color BackgroundColor {
			get { return Editor.ColorStyle.DebuggerStackLineMarker.Color; }
		}
		
		protected override Cairo.Color BorderColor {
			get { return GetBorderColor (Editor.ColorStyle.DebuggerStackLineMarker); }
		}

		protected override void SetForegroundColor (ChunkStyle style)
		{
			style.Foreground = Editor.ColorStyle.DebuggerStackLine.Foreground;
		}

		protected override void DrawMarginIcon (Cairo.Context cr, double x, double y, double size)
		{
			DrawImage (cr, stackLine, x, y, size);
		}
	}

}
