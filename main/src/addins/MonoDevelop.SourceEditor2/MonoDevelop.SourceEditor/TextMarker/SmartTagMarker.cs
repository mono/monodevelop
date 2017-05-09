//
// SmartTagMarker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	class SmartTagMarker : TextSegmentMarker, IActionTextLineMarker, ISmartTagMarker
	{
		const double tagMarkerWidth = 8;
		const double tagMarkerHeight = 2;
		MonoDevelop.Ide.Editor.DocumentLocation loc;
		Mono.TextEditor.MonoTextEditor editor;

		public SmartTagMarker (int offset, MonoDevelop.Ide.Editor.DocumentLocation realLocation) : base (offset, 0)
		{
			this.loc = realLocation;
		}

		public override void Draw (Mono.TextEditor.MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics, int startOffset, int endOffset)
		{
			this.editor = editor;
			var line = editor.GetLine (loc.Line);
			if (line == null)
				return;
			var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition;

			cr.Rectangle (Math.Floor (x), Math.Floor (metrics.LineYRenderStartPosition) + (line == editor.GetLineByOffset (startOffset) ? editor.LineHeight - tagMarkerHeight : 0), tagMarkerWidth, tagMarkerHeight);
			cr.SetSourceColor ((HslColor.Brightness (SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Background)) < 0.5 ? Ide.Gui.Styles.Editor.SmartTagMarkerColorDark : Ide.Gui.Styles.Editor.SmartTagMarkerColorLight).ToCairoColor ());
			cr.Fill ();
		}

		#region IActionTextLineMarker implementation

		bool IActionTextLineMarker.MousePressed (Mono.TextEditor.MonoTextEditor editor, MarginMouseEventArgs args)
		{
			var handler = MousePressed;
			if (handler != null)
				handler (this, new TextEventArgsWrapper (args));
			return false;
		}

		bool IActionTextLineMarker.MouseReleased (MonoTextEditor editor, MarginMouseEventArgs args)
		{
			return false;
		}


		void IActionTextLineMarker.MouseHover (Mono.TextEditor.MonoTextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
		{
			if (args.Button != 0)
				return;
			var line = editor.GetLine (loc.Line);
			if (line == null)
				return;
			var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.TextStartPosition;
			//var y = editor.LineToY (line.LineNumber + 1) - editor.VAdjustment.Value;
			const double xAdditionalSpace = tagMarkerWidth;
			if (args.X - x >= -xAdditionalSpace * editor.Options.Zoom && 
				args.X - x < (tagMarkerWidth + xAdditionalSpace) * editor.Options.Zoom /*&& 
				    args.Y - y < (editor.LineHeight / 2) * editor.Options.Zoom*/) {
				result.Cursor = null;
				ShowPopup?.Invoke (null, null);
			} else {
				CancelPopup?.Invoke (null, null);
			}
		}


		#endregion

		bool ISmartTagMarker.IsInsideSmartTag (double x, double y)
		{
			if (editor == null)
				return false;
			var lin2 = editor.GetLine (loc.Line);
			var x2 = editor.ColumnToX (lin2, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.TextStartPosition;
			var y2 = editor.LineToY (loc.Line + 1) - editor.VAdjustment.Value;
			return x - x2 >= 0 * editor.Options.Zoom && 
				x - x2 < tagMarkerWidth * editor.Options.Zoom && 
				y - y2 < (editor.LineHeight / 2) * editor.Options.Zoom;
		}

		public event EventHandler<TextMarkerMouseEventArgs> MousePressed;
		#pragma warning disable 0067
		public event EventHandler<TextMarkerMouseEventArgs> MouseHover;
		#pragma warning restore 0067
		public event EventHandler ShowPopup;
		public event EventHandler CancelPopup;

		object ITextSegmentMarker.Tag {
			get;
			set;
		}
	}

	class TextEventArgsWrapper : TextMarkerMouseEventArgs
	{
		readonly MarginMouseEventArgs args;

		public override double X {
			get {
				return args.X;
			}
		}

		public override double Y {
			get {
				return args.Y;
			}
		}

		public override object OverwriteCursor {
			get;
			set;
		}

		public override string TooltipMarkup {
			get;
			set;
		}

		public TextEventArgsWrapper (MarginMouseEventArgs args)
		{
			if (args == null)
				throw new ArgumentNullException ("args");
			this.args = args;
		}

		public override bool TriggersContextMenu ()
		{
			return args.TriggersContextMenu ();
		}
	}

}

