//
// LinkMarker.cs
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
using MonoDevelop.Ide.Editor;
using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	class LinkMarker : UnderlineTextSegmentMarker, ILinkTextMarker, IActionTextLineMarker
	{
		static readonly Gdk.Cursor textLinkCursor = new Gdk.Cursor (Gdk.CursorType.Hand1);

		Action<LinkRequest> activateLink;

		public bool OnlyShowLinkOnHover {
			get;
			set;
		}


		public LinkMarker (int offset, int length, Action<LinkRequest> activateLink) : base (null, new TextSegment (offset, length), TextSegmentMarkerEffect.Underline)
		{
			this.Color = SyntaxHighlightingService.GetColor (DefaultSourceEditorOptions.Instance.GetEditorTheme (), EditorThemeColors.Link);
			this.activateLink = activateLink;
		}

		public event EventHandler<TextMarkerMouseEventArgs> MousePressed;
		public event EventHandler<TextMarkerMouseEventArgs> MouseHover;

		object ITextSegmentMarker.Tag {
			get;
			set;
		}

		bool IActionTextLineMarker.MousePressed (MonoTextEditor editor, MarginMouseEventArgs args)
		{
			MousePressed?.Invoke (this, new TextEventArgsWrapper (args));
			return false;
		}

		bool IActionTextLineMarker.MouseReleased (MonoTextEditor editor, MarginMouseEventArgs args)
		{
			if ((Platform.IsMac && (args.ModifierState & Gdk.ModifierType.Mod2Mask) == Gdk.ModifierType.Mod2Mask) ||
			    (!Platform.IsMac && (args.ModifierState & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask))
				activateLink?.Invoke (LinkRequest.RequestNewView);
			else
				activateLink?.Invoke (LinkRequest.SameView);
			
			return false;
		}


		void IActionTextLineMarker.MouseHover (MonoTextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
		{
			MouseHover?.Invoke (this, new TextEventArgsWrapper (args));
			result.Cursor = textLinkCursor;
			if (OnlyShowLinkOnHover) {
				editor.GetTextEditorData ().Document.CommitLineUpdate (args.LineSegment);
				editor.TextViewMargin.HoveredLineChanged += new UpdateOldLine (editor, args.LineSegment).TextViewMargin_HoveredLineChanged;
			}
		}

		class UpdateOldLine
		{
			MonoTextEditor editor;
			DocumentLine lineSegment;

			public UpdateOldLine (MonoTextEditor editor, DocumentLine lineSegment)
			{
				this.editor = editor;
				this.lineSegment = lineSegment;
			}

			public void TextViewMargin_HoveredLineChanged (object sender, Mono.TextEditor.LineEventArgs e)
			{
				editor.GetTextEditorData ().Document.CommitLineUpdate (lineSegment);
				editor.TextViewMargin.HoveredLineChanged -= TextViewMargin_HoveredLineChanged;
			}
		}

		public override void Draw (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics, int startOffset, int endOffset)
		{
			if (OnlyShowLinkOnHover) {
					if (editor.TextViewMargin.MarginCursor != textLinkCursor)
					return;
				if (editor.TextViewMargin.HoveredLine == null)
					return;
				var hoverOffset = editor.LocationToOffset (editor.TextViewMargin.HoveredLocation);
				if (!Segment.Contains (hoverOffset)) 
					return; 
			}

			this.Color = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Link);

			if (!OnlyShowLinkOnHover) {
				if (editor.TextViewMargin.MarginCursor == textLinkCursor && editor.TextViewMargin.HoveredLine != null) {
					var hoverOffset = editor.LocationToOffset (editor.TextViewMargin.HoveredLocation);
					// if (Segment.Contains (hoverOffset))
					//	this.Color = editorEditorThemle.ActiveLinkColor.Color;
				}
			}

			base.Draw (editor, cr, metrics, startOffset, endOffset);
		}
	}
}

