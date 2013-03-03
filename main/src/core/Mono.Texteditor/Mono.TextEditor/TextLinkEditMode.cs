// 
// TextLinkEditMode.cs
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
using System.Collections.Generic;
using Mono.TextEditor.PopupWindow;

namespace Mono.TextEditor
{
	public class TextLink : IListDataProvider<string>
	{
		public TextSegment PrimaryLink {
			get {
				if (links.Count == 0)
					return TextSegment.Invalid;
				return links [0];
			}
		}

		List<TextSegment> links = new List<TextSegment> ();

		public List<TextSegment> Links {
			get {
				return links;
			}
			set {
				links = value;
			}
		}

		public bool IsIdentifier {
			get;
			set;
		}

		public bool IsEditable {
			get;
			set;
		}

		public string Name {
			get;
			set;
		}

		public string CurrentText {
			get;
			set;
		}

		public string Tooltip {
			get;
			set;
		}

		public IListDataProvider<string> Values {
			get;
			set;
		}

		public Func<Func<string, string>, IListDataProvider<string>> GetStringFunc {
			get;
			set;
		}

		public TextLink (string name)
		{
			IsEditable = true;
			this.Name = name;
			this.IsIdentifier = false;
		}

		public override string ToString ()
		{
			return string.Format ("[TextLink: Name={0}, Links={1}, IsEditable={2}, Tooltip={3}, CurrentText={4}, Values=({5})]", 
								Name, 
								Links.Count, 
								IsEditable, 
								Tooltip, 
								CurrentText, 
								Values.Count);
		}

		public void AddLink (TextSegment segment)
		{
			links.Add (segment);
		}

		#region IListDataProvider implementation
		public string GetText (int n)
		{
			return Values != null ? Values.GetText (n) : "";
		}

		public string this [int n] {
			get {
				return Values != null ? Values [n] : "";
			}
		}

		public Gdk.Pixbuf GetIcon (int n)
		{
			return Values != null ? Values.GetIcon (n) : null;
		}

		public int Count {
			get {
				return Values != null ? Values.Count : 0;
			}
		}
		#endregion
	}

	public enum TextLinkMode {
		EditIdentifier,
		General
	}

	public class TextLinkEditMode : HelpWindowEditMode
	{
		List<TextLink> links;
		int baseOffset;
		int endOffset;
		int undoDepth = -1;
		bool resetCaret = true;

		public EditMode OldMode {
			get;
			set;
		}

		public List<TextLink> Links {
			get {
				return links;
			}
		}

		public int BaseOffset {
			get {
				return baseOffset;
			}
		}

		public bool ShouldStartTextLinkMode {
			get {
				return !(Editor.CurrentMode is TextLinkEditMode) && links.Any (l => l.
					IsEditable);
			}
		}

		public bool SetCaretPosition {
			get;
			set;
		}

		public bool SelectPrimaryLink {
			get;
			set;
		}

		public TextLinkMode TextLinkMode {
			get;
			set;
		}

		TextLinkTooltipProvider tooltipProvider;

		public TextLinkEditMode (TextEditor editor,int baseOffset,List<TextLink> links)
		{
			this.editor = editor;
			this.links = links;
			this.baseOffset = baseOffset;
			this.endOffset = editor.Caret.Offset;
			tooltipProvider = new TextLinkTooltipProvider (this);
			this.Editor.GetTextEditorData ().tooltipProviders.Insert (0, tooltipProvider);
			this.SetCaretPosition = true;
			this.SelectPrimaryLink = true;
		}

		void HandleEditorDocumentBeginUndo (object sender, EventArgs e)
		{
			ExitTextLinkMode ();
		}

		public event EventHandler Exited;

		protected virtual void OnExited (EventArgs e)
		{
			EventHandler handler = this.Exited;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Cancel;

		protected virtual void OnCancel (EventArgs e)
		{
			EventHandler handler = this.Cancel;
			if (handler != null)
				handler (this, e);
		}

		TextLink closedLink = null;

		void HandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => !l.PrimaryLink.IsInvalid && l.PrimaryLink.Offset <= caretOffset && caretOffset <= l.PrimaryLink.EndOffset);
			if (link != null && link.Count > 0 && link.IsEditable) {
				if (window != null && window.DataProvider != link) {
					DestroyWindow ();
				}
				if (closedLink == link)
					return;
				closedLink = null;
				if (window == null) {
					window = new ListWindow<string> ();
					window.DoubleClicked += delegate {
						CompleteWindow ();
					};
					window.DataProvider = link;
					
					DocumentLocation loc = Editor.Document.OffsetToLocation (BaseOffset + link.PrimaryLink.Offset);
					Editor.ShowListWindow (window, loc);
					
				} 
			} else {
				DestroyWindow ();
				closedLink = null;
			}
		}

		List<TextLinkMarker> textLinkMarkers = new List<TextLinkMarker> ();

		public void StartMode ()
		{
			foreach (TextLink link in links) {
				if (!link.PrimaryLink.IsInvalid)
					link.CurrentText = Editor.Document.GetTextAt (link.PrimaryLink.Offset + baseOffset, link.PrimaryLink.Length);
				foreach (TextSegment segment in link.Links) {
					Editor.Document.EnsureOffsetIsUnfolded (baseOffset + segment.Offset);
					DocumentLine line = Editor.Document.GetLineByOffset (baseOffset + segment.Offset);
					if (line.GetMarker (typeof(TextLinkMarker)) != null)
						continue;
					TextLinkMarker marker = (TextLinkMarker)line.GetMarker (typeof(TextLinkMarker));
					if (marker == null) {
						marker = new TextLinkMarker (this);
						marker.BaseOffset = baseOffset;
						Editor.Document.AddMarker (line, marker);
						textLinkMarkers.Add (marker);
					}
				}
			}
			
			editor.Document.BeforeUndoOperation += HandleEditorDocumentBeginUndo;
			Editor.Document.TextReplaced += UpdateLinksOnTextReplace;
			this.Editor.Caret.PositionChanged += HandlePositionChanged;
			this.UpdateTextLinks ();
			this.HandlePositionChanged (null, null);
			TextLink firstLink = links.First (l => l.IsEditable);
			if (SelectPrimaryLink)
				Setlink (firstLink);
			Editor.Document.CommitUpdateAll ();
			editor.Document.OptimizeTypedUndo ();
			this.undoDepth = Editor.Document.GetCurrentUndoDepth ();
			ShowHelpWindow ();
		}
		
		public bool HasChangedText {
			get {
				return undoDepth != Editor.Document.GetCurrentUndoDepth ();
			}
		}

		void Setlink (TextLink link)
		{
			if (link.PrimaryLink.IsInvalid)
				return;
			Editor.Caret.Offset = baseOffset + link.PrimaryLink.Offset;
			Editor.ScrollToCaret ();
			Editor.Caret.Offset = baseOffset + link.PrimaryLink.EndOffset;
			Editor.MainSelection = new Selection (Editor.Document.OffsetToLocation (baseOffset + link.PrimaryLink.Offset),
											Editor.Document.OffsetToLocation (baseOffset + link.PrimaryLink.EndOffset));
			Editor.Document.CommitUpdateAll ();
		}

		public void ExitTextLinkMode ()
		{
			editor.Document.BeforeUndoOperation -= HandleEditorDocumentBeginUndo;
			DestroyHelpWindow ();
			isExited = true;
			DestroyWindow ();
			textLinkMarkers.ForEach (m => Editor.Document.RemoveMarker (m));
			textLinkMarkers.Clear ();
			if (SetCaretPosition && resetCaret)
				Editor.Caret.Offset = endOffset;
			
			Editor.Document.TextReplaced -= UpdateLinksOnTextReplace;
			this.Editor.Caret.PositionChanged -= HandlePositionChanged;
			this.Editor.RemoveTooltipProvider (tooltipProvider);
			if (undoDepth >= 0)
				Editor.Document.StackUndoToDepth (undoDepth);
			Editor.CurrentMode = OldMode;
			Editor.Document.CommitUpdateAll ();
			OnExited (EventArgs.Empty);
		}

		public bool IsInUpdate {
			get;
			set;
		}

		bool isExited = false;
		bool wasReplaced = false;

		void UpdateLinksOnTextReplace (object sender, DocumentChangeEventArgs e)
		{
			wasReplaced = true;
			int offset = e.Offset - baseOffset;
			int delta = e.ChangeDelta;
			if (!IsInUpdate && !links.Where (link => link.Links.Where (segment => segment.Contains (offset)
					|| segment.EndOffset == offset).
				Any ()).Any ()) {
				SetCaretPosition = false;
				ExitTextLinkMode ();
				return;
			}
			AdjustLinkOffsets (offset, delta);
			UpdateTextLinks ();
		}

		void AdjustLinkOffsets (int offset, int delta)
		{
			foreach (TextLink link in links) {
				var newLinks = new List<TextSegment> ();
				foreach (var s in link.Links) {
					if (offset < s.Offset) {
						newLinks.Add (new TextSegment (s.Offset + delta, s.Length));
					} else if (offset < s.EndOffset) {
						newLinks.Add (new TextSegment (s.Offset, s.Length + delta));
					} else if (offset == s.EndOffset && delta > 0) {
						newLinks.Add (new TextSegment (s.Offset, s.Length + delta));
					} else {
						newLinks.Add (s);
					}
				}
				link.Links = newLinks;
			}
			if (baseOffset + offset < endOffset)
				endOffset += delta;
		}

		void GotoNextLink (TextLink link)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink nextLink = links.Find (l => l.IsEditable && l.PrimaryLink.Offset > (link != null ? link.PrimaryLink.EndOffset : caretOffset));
			if (nextLink == null)
				nextLink = links.Find (l => l.IsEditable);
			closedLink = null;
			Setlink (nextLink);
		}

		void GotoPreviousLink (TextLink link)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			var prevLink = links.FindLast (l => l.IsEditable && l.PrimaryLink.Offset < (link != null ? link.PrimaryLink.Offset : caretOffset));
			if (prevLink == null)
				prevLink = links.FindLast (l => l.IsEditable);
			Setlink (prevLink);
		}

		void CompleteWindow ()
		{
			if (window == null)
				return;
			TextLink lnk = (TextLink)window.DataProvider;
			//int line = Editor.Caret.Line;
			lnk.CurrentText = (string)window.CurrentItem;
			UpdateLinkText (lnk);
			UpdateTextLinks ();
			Editor.Document.CommitUpdateAll ();
		}

		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			var wnd = window;
			if (wnd != null) {
				ListWindowKeyAction action = wnd.ProcessKey (key, modifier);
				if ((action & ListWindowKeyAction.Complete) == ListWindowKeyAction.Complete)
					CompleteWindow ();
				if ((action & ListWindowKeyAction.CloseWindow) == ListWindowKeyAction.CloseWindow) {
					closedLink = (TextLink)wnd.DataProvider;
					DestroyWindow ();
				}
				if ((action & ListWindowKeyAction.Complete) == ListWindowKeyAction.Complete)
					GotoNextLink (closedLink);

				if ((action & ListWindowKeyAction.Ignore) == ListWindowKeyAction.Ignore)
					return;
			}
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => l.Links.Any (s => s.Offset <= caretOffset && caretOffset <= s.EndOffset));
			
			switch (key) {
			case Gdk.Key.BackSpace:
				if (link != null && caretOffset == link.PrimaryLink.Offset)
					return;
				goto default;
			case Gdk.Key.space:
				if (link == null || !link.IsIdentifier)
					goto default;
				return;
			case Gdk.Key.Delete:
				if (link != null && caretOffset == link.PrimaryLink.EndOffset)
					return;
				goto default;
			case Gdk.Key.Tab:
				if ((modifier & Gdk.ModifierType.ControlMask) != 0)
				if (link != null && !link.IsIdentifier)
					goto default;
				if ((modifier & Gdk.ModifierType.ShiftMask) == 0)
					GotoNextLink (link);
				else
					GotoPreviousLink (link);
				return;
			case Gdk.Key.Escape:
			case Gdk.Key.Return:
			case Gdk.Key.KP_Enter:
				if ((modifier & Gdk.ModifierType.ControlMask) != 0)
				if (link != null && !link.IsIdentifier)
					goto default;
				if (wnd != null) {
					CompleteWindow ();
				} else {
					ExitTextLinkMode ();
				}
				if (key == Gdk.Key.Escape)
					OnCancel (EventArgs.Empty);
				return;
			default:
				wasReplaced = false;
				base.HandleKeypress (key, unicodeKey, modifier);
				if (wasReplaced && link == null) {
					resetCaret = false;
					ExitTextLinkMode ();
				}
				break;
			}
/*			if (link != null)
				UpdateTextLink (link);
			UpdateTextLinks ();
			Editor.Document.CommitUpdateAll ();*/
		}

		ListWindow<string> window;

		void DestroyWindow ()
		{
			if (window != null) {
				window.Destroy ();
				window = null;
			}
		}

		public string GetStringCallback (string linkName)
		{
			foreach (TextLink link in links) {
				if (link.Name == linkName)
					return link.CurrentText;
			}
			return null;
		}

		public void UpdateTextLinks ()
		{
			if (isExited)
				return;
			foreach (TextLink l in links) {
				UpdateTextLink (l);
			}
		}

		void UpdateTextLink (TextLink link)
		{
			if (link.GetStringFunc != null) {
				link.Values = link.GetStringFunc (GetStringCallback);
			}
			if (!link.IsEditable && link.Values.Count > 0) {
				link.CurrentText = (string)link.Values [link.Values.Count - 1];
			} else {
				if (!link.PrimaryLink.IsInvalid) {
					int offset = link.PrimaryLink.Offset + baseOffset;
					if (offset >= 0 && link.PrimaryLink.Length >= 0)
						link.CurrentText = Editor.Document.GetTextAt (offset, link.PrimaryLink.Length);
				}
			}
			UpdateLinkText (link);
		}

		public void UpdateLinkText (TextLink link)
		{
			Editor.Document.TextReplaced -= UpdateLinksOnTextReplace;
			for (int i = link.Links.Count - 1; i >= 0; i--) {
				var s = link.Links [i];
				int offset = s.Offset + baseOffset;
				if (offset < 0 || s.Length < 0 || offset + s.Length > Editor.Document.TextLength)
					continue;
				if (Editor.Document.GetTextAt (offset, s.Length) != link.CurrentText) {
					Editor.Replace (offset, s.Length, link.CurrentText);
					int delta = link.CurrentText.Length - s.Length;
					AdjustLinkOffsets (s.Offset, delta);
					Editor.Document.CommitLineUpdate (Editor.Document.OffsetToLineNumber (offset));
				}
			}
			Editor.Document.TextReplaced += UpdateLinksOnTextReplace;
		}
	}

	public class TextLinkTooltipProvider : TooltipProvider
	{
		TextLinkEditMode mode;

		public TextLinkTooltipProvider (TextLinkEditMode mode)
		{
			this.mode = mode;
		}

		#region ITooltipProvider implementation 
		public override TooltipItem GetItem (TextEditor Editor, int offset)
		{
			int o = offset - mode.BaseOffset;
			for (int i = 0; i < mode.Links.Count; i++) {
				TextLink l = mode.Links [i];
				if (!l.PrimaryLink.IsInvalid && l.PrimaryLink.Offset <= o && o <= l.PrimaryLink.EndOffset)
					return new TooltipItem (l, l.PrimaryLink.Offset, l.PrimaryLink.Length);
			}
			return null;
			//return mode.Links.First (l => l.PrimaryLink != null && l.PrimaryLink.Offset <= o && o <= l.PrimaryLink.EndOffset);
		}

		protected override Gtk.Window CreateTooltipWindow (TextEditor Editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			TextLink link = item.Item as TextLink;
			if (link == null || string.IsNullOrEmpty (link.Tooltip))
				return null;
			
			TooltipWindow window = new TooltipWindow ();
			window.Markup = link.Tooltip;
			return window;
		}

		protected override void GetRequiredPosition (TextEditor Editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			TooltipWindow win = (TooltipWindow)tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		#endregion 
	}

	public class TextLinkMarker : TextLineMarker, IBackgroundMarker, IGutterMarker
	{
		TextLinkEditMode mode;

		public int BaseOffset {
			get;
			set;
		}

		public TextLinkMarker (TextLinkEditMode mode)
		{
			this.mode = mode;
			IsVisible = true;
		}

		/*
		void InternalDrawBackground (TextEditor Editor, Gdk.Drawable win, Pango.Layout layout, bool selected, int startOffset, int endOffset, int y, ref int startXPos, int endXPos, ref bool drawBg)
		{
			Gdk.Rectangle clipRectangle = new Gdk.Rectangle (mode.Editor.TextViewMargin.XOffset, 0, 
													Editor.Allocation.Width - mode.Editor.TextViewMargin.XOffset, Editor.Allocation.Height);
			
			// draw default background
			using (Gdk.GC fillGc = new Gdk.GC (win)) {
				fillGc.ClipRectangle = clipRectangle;
				fillGc.RgbFgColor = selected ? Editor.ColorStyle.Selection.BackgroundColor : Editor.ColorStyle.Default.BackgroundColor;
				win.DrawRectangle (fillGc, true, startXPos, y, endXPos, Editor.LineHeight);
			}
			
			if (startOffset >= endOffset)
				return;
			
			int caretOffset = Editor.Caret.Offset - BaseOffset;
			foreach (TextLink link in mode.Links) {
				if (!link.IsEditable)
					continue;
				bool isPrimaryHighlighted = link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset;
				
				foreach (TextSegment segment in link.Links) {
					
					if ((BaseOffset + segment.Offset <= startOffset && startOffset < BaseOffset + segment.EndOffset) ||
						(startOffset <= BaseOffset + segment.Offset && BaseOffset + segment.Offset < endOffset)) {
						int strOffset    = BaseOffset + segment.Offset - startOffset;
						int strEndOffset = BaseOffset + segment.EndOffset - startOffset;
						
						int lineNr, x_pos, x_pos2;
						layout.IndexToLineX (strOffset, false, out lineNr, out x_pos);
						layout.IndexToLineX (strEndOffset, false, out lineNr, out x_pos2);
						x_pos  = (int)(x_pos / Pango.Scale.PangoScale);
						x_pos2 = (int)(x_pos2 / Pango.Scale.PangoScale);
						using (Gdk.GC rectangleGc = new Gdk.GC (win)) {
							rectangleGc.ClipRectangle = clipRectangle;
							using (Gdk.GC fillGc = new Gdk.GC (win)) {
								fillGc.ClipRectangle = clipRectangle;
								drawBg = false;
								
								if (segment == link.PrimaryLink) {
									fillGc.RgbFgColor      = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.BackgroundColor : Editor.ColorStyle.PrimaryTemplate.BackgroundColor;
									rectangleGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.Color           : Editor.ColorStyle.PrimaryTemplate.Color;
								} else {
									fillGc.RgbFgColor      = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.BackgroundColor : Editor.ColorStyle.SecondaryTemplate.BackgroundColor;
									rectangleGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.Color           : Editor.ColorStyle.SecondaryTemplate.Color;
								}
								// Draw segment
								if (!selected)
									win.DrawRectangle (fillGc, true, startXPos, y, x_pos2 - x_pos, Editor.LineHeight);
								
								int x1 = startXPos + x_pos - 1;
								int x2 = startXPos + x_pos2 - 1;
								int y2 = y + Editor.LineHeight - 1;
								
								win.DrawLine (rectangleGc, x1, y, x2, y);
								win.DrawLine (rectangleGc, x1, y2, x2, y2);
								win.DrawLine (rectangleGc, x1, y, x1, y2);
								win.DrawLine (rectangleGc, x2, y, x2, y2);
							}
						}
					}
				}
			}
			startXPos += Editor.GetWidth (Editor.Document.GetTextBetween (startOffset, endOffset));
		}
		*/
	/*	bool Overlaps (TextSegment segment, int start, int end)
		{
			return segment.Offset <= start && start < segment.EndOffset || 
					segment.Offset <= end && end < segment.EndOffset ||
					start <= segment.Offset && segment.Offset < end ||
					start < segment.EndOffset && segment.EndOffset < end;
		}*/
		
		public bool DrawBackground (TextEditor Editor, Cairo.Context cr, TextViewMargin.LayoutWrapper layout, int selectionStart, int selectionEnd, int startOffset, int endOffset, double y, double startXPos, double endXPos, ref bool drawBg)
		{
			int caretOffset = Editor.Caret.Offset - BaseOffset;

			foreach (TextLink link in mode.Links) {
				if (!link.IsEditable) 
					continue; 
				bool isPrimaryHighlighted = link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset;

				foreach (TextSegment segment in link.Links) {

					if ((BaseOffset + segment.Offset <= startOffset && startOffset < BaseOffset + segment.EndOffset) || (startOffset <= BaseOffset + segment.Offset && BaseOffset + segment.Offset < endOffset)) {
						int strOffset = BaseOffset + segment.Offset - startOffset;
						int strEndOffset = BaseOffset + segment.EndOffset - startOffset;

						int x_pos = layout.Layout.IndexToPos (strOffset).X;
						int x_pos2 = layout.Layout.IndexToPos (strEndOffset).X;
					
						x_pos = (int)(x_pos / Pango.Scale.PangoScale);
						x_pos2 = (int)(x_pos2 / Pango.Scale.PangoScale);
						drawBg = false;
						Cairo.Color fillGc, rectangleGc;
						if (segment == link.PrimaryLink) {
							fillGc = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.GetColor ("secondcolor") : Editor.ColorStyle.PrimaryTemplate.GetColor ("secondcolor");
							rectangleGc = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.GetColor ("secondcolor") : Editor.ColorStyle.PrimaryTemplate.GetColor ("secondcolor");
						} else {
							fillGc = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.GetColor ("secondcolor") : Editor.ColorStyle.SecondaryTemplate.GetColor ("secondcolor");
							rectangleGc = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.GetColor ("color") : Editor.ColorStyle.SecondaryTemplate.GetColor ("color");
						}
						
						// Draw segment
						double x1 = startXPos + x_pos - 1;
						double x2 = startXPos + x_pos2 - 1 + 0.5;

						cr.Rectangle (x1 + 0.5, y + 0.5, x2 - x1, Editor.LineHeight - 1);
						
						cr.Color = fillGc;
						cr.FillPreserve ();
						
						cr.Color = rectangleGc;
						cr.Stroke ();
					}
				}
			}
			return true;
		}

		#region IGutterMarker implementation
		public void DrawLineNumber (TextEditor editor, double width, Cairo.Context cr, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
		{
			var lineNumberBgGC = editor.ColorStyle.LineNumbers.Background;
			var lineNumberGC = editor.ColorStyle.LineNumbers.Foreground;

			cr.Rectangle (x, y, width, lineHeight);
			cr.Color = editor.Caret.Line == line ? editor.ColorStyle.LineMarker.GetColor ("color") : lineNumberGC;
			cr.Fill ();
			
			if (line <= editor.Document.LineCount) {
				// Due to a mac? gtk bug I need to re-create the layout here
				// otherwise I get pango exceptions.
				using (var layout = PangoUtil.CreateLayout (editor)) {
					layout.FontDescription = editor.Options.Font;
					layout.Width = (int)width;
					layout.Alignment = Pango.Alignment.Right;
					layout.SetText (line.ToString ());
					cr.Save ();
					cr.Translate (x + (int)width + (editor.Options.ShowFoldMargin ? 0 : -2), y);
					cr.Color = lineNumberBgGC;
					cr.ShowLayout (layout);
					cr.Restore ();
				}
			}
		}
		#endregion
	}
}
