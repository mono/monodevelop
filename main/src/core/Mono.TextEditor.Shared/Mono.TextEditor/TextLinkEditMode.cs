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
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	class TextLink : IListDataProvider<string>
	{
		public ISegment PrimaryLink {
			get {
				if (links.Count == 0)
					return TextSegment.Invalid;
				return links [0];
			}
		}

		List<ISegment> links = new List<ISegment> ();

		public List<ISegment> Links {
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
			return string.Format ("[TextLink: Name={0}, Links={1}, IsEditable={2}, CurrentText={3}, Values=({4})]", 
			                      Name, 
			                      Links.Count, 
			                      IsEditable, 
			                      CurrentText, 
			                      Values.Count);
		}

		public void AddLink (ISegment segment)
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

		public Xwt.Drawing.Image GetIcon (int n)
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

	enum TextLinkMode
	{
		EditIdentifier,
		General
	}

	class TextLinkEditMode : HelpWindowEditMode
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

		public TextLinkEditMode (MonoTextEditor editor, int baseOffset, List<TextLink> links)
		{
			this.editor = editor;
			this.links = links;
			this.baseOffset = baseOffset;
			this.endOffset = editor.Caret.Offset;
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

		TextLink closedLink = null, currentSelectedLink = null;

		void HandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => !l.PrimaryLink.IsInvalid () && l.PrimaryLink.Offset <= caretOffset && caretOffset <= l.PrimaryLink.EndOffset);

			if (link != currentSelectedLink) {
				foreach (var l in textLinkMarkers) {
					Editor.Document.CommitLineUpdate (l.LineSegment);
				}
				currentSelectedLink = link;
			}


			if (link != null && link.Count > 0 && link.IsEditable) {
				if (closedLink == link)
					return;
				closedLink = null;
				/* Disabled because code completion was enabled in link mode.
				if (window == null) {
					window = new ListWindow<string> ();
					window.DoubleClicked += delegate {
						CompleteWindow ();
					};
					window.DataProvider = link;
					
					DocumentLocation loc = Editor.Document.OffsetToLocation (BaseOffset + link.PrimaryLink.Offset);
					Editor.ShowListWindow (window, loc);
					
				} */
			} else {
				closedLink = null;
			}
		}

		List<TextLinkMarker> textLinkMarkers = new List<TextLinkMarker> ();

		public void StartMode ()
		{
			foreach (TextLink link in links) {
				if (!link.PrimaryLink.IsInvalid ())
					link.CurrentText = Editor.Document.GetTextAt (link.PrimaryLink.Offset + baseOffset, link.PrimaryLink.Length);
				foreach (TextSegment segment in link.Links) {
					Editor.Document.EnsureOffsetIsUnfolded (baseOffset + segment.Offset);
					DocumentLine line = Editor.Document.GetLineByOffset (baseOffset + segment.Offset);
					TextLinkMarker marker = (TextLinkMarker)Editor.Document.GetMarkers (line).OfType<TextLinkMarker> ().FirstOrDefault ();
					if (marker != null)
						continue;
					marker = new TextLinkMarker (this);
					marker.BaseOffset = baseOffset;
					Editor.Document.AddMarker (line, marker);
					textLinkMarkers.Add (marker);
				}
			}
			
			editor.Document.BeforeUndoOperation += HandleEditorDocumentBeginUndo;
			Editor.Document.TextBuffer.Changed += UpdateLinksOnTextReplace;
			this.Editor.Caret.PositionChanged += HandlePositionChanged;
			this.UpdateTextLinks ();
			this.HandlePositionChanged (null, null);
			TextLink firstLink = links.First (l => l.IsEditable);
			if (SelectPrimaryLink)
				Setlink (firstLink);
			Editor.Document.CommitUpdateAll ();
			this.undoDepth = Editor.Document.GetCurrentUndoDepth ();
			ShowHelpWindow ();
			currentSelectedLink = null;
		}

		public bool HasChangedText {
			get {
				return undoDepth != Editor.Document.GetCurrentUndoDepth ();
			}
		}

		void Setlink (TextLink link)
		{
			if (link.PrimaryLink.IsInvalid ())
				return;
			Editor.Caret.Offset = baseOffset + link.PrimaryLink.Offset;
			Editor.ScrollToCaret ();
			Editor.Caret.Offset = baseOffset + link.PrimaryLink.EndOffset;
			Editor.MainSelection = new MonoDevelop.Ide.Editor.Selection (Editor.Document.OffsetToLocation (baseOffset + link.PrimaryLink.Offset),
			                                      Editor.Document.OffsetToLocation (baseOffset + link.PrimaryLink.EndOffset));
			Editor.Document.CommitUpdateAll ();
		}

		public void ExitTextLinkMode ()
		{
			editor.Document.BeforeUndoOperation -= HandleEditorDocumentBeginUndo;
			DestroyHelpWindow ();
			isExited = true;
			textLinkMarkers.ForEach (m => Editor.Document.RemoveMarker (m));
			textLinkMarkers.Clear ();
			if (SetCaretPosition && resetCaret)
				Editor.Caret.Offset = endOffset;
			
			Editor.Document.TextBuffer.Changed -= UpdateLinksOnTextReplace;
			this.Editor.Caret.PositionChanged -= HandlePositionChanged;
			if (undoDepth >= 0)
				Editor.Document.StackUndoToDepth (undoDepth);
			Editor.CurrentMode = OldMode;
			Editor.Document.CommitUpdateAll ();
			OnExited (EventArgs.Empty);
		}

		bool isExited = false;
		bool wasReplaced = false;

		static readonly object _linkEditTag = new object();

		void UpdateLinksOnTextReplace (object sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
		{
			wasReplaced = true;

			if (e.EditTag != _linkEditTag) {
				foreach (var change in e.Changes) {
					int offset = change.OldPosition - baseOffset;
					if (!links.Any (link => link.Links.Any (segment => segment.Contains (offset)
						|| segment.EndOffset == offset))) {
						SetCaretPosition = false;
						ExitTextLinkMode ();
						return;
					}
				}
			}

			foreach (TextLink link in links) {
				var newLinks = new List<ISegment>();
				foreach (var s in link.Links) {
					var newStart = Microsoft.VisualStudio.Text.Tracking.TrackPositionForwardInTime(Microsoft.VisualStudio.Text.PointTrackingMode.Negative,
																								   s.Offset + baseOffset,
																								   e.BeforeVersion, e.AfterVersion) - baseOffset;
					var newEnd = Microsoft.VisualStudio.Text.Tracking.TrackPositionForwardInTime(Microsoft.VisualStudio.Text.PointTrackingMode.Positive,
																								 s.Offset + s.Length + baseOffset,
																								 e.BeforeVersion, e.AfterVersion) - baseOffset;
			
					newLinks.Add(new TextSegment(newStart, newEnd - newStart));
				}

				link.Links = newLinks;
				endOffset = Microsoft.VisualStudio.Text.Tracking.TrackPositionForwardInTime(Microsoft.VisualStudio.Text.PointTrackingMode.Negative,
																							endOffset,
																							e.BeforeVersion, e.AfterVersion);
			}

			// If this is the final edit of a compund edit (e.g. no one has modified the buffer in an event handler before this one).
			if (Editor.Document.TextBuffer.CurrentSnapshot == e.After) {
				// Edits due to replacing link text trigger a commit. Otherwise, update the link text
				if (e.EditTag == _linkEditTag) {
					// TODO this really should be triggered on the post change if there were any link-inspired updates.
					foreach (TextLink link in links) {
						for (int i = 0; (i < link.Links.Count); ++i) {
							var s = link.Links[i];
							int offset = s.Offset + baseOffset;
							Editor.Document.CommitLineUpdate(Editor.Document.OffsetToLineNumber(offset));
						}
					}
				} else {
					UpdateTextLinks();
				}
			}
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

		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => l.Links.Any (s => s.Offset <= caretOffset && caretOffset <= s.EndOffset));
			
			switch (key) {
			case Gdk.Key.BackSpace:
				if (link != null && caretOffset == link.PrimaryLink.Offset && !IsSomethingSelectedInLink (editor, link.PrimaryLink))
					return;
				goto default;
			case Gdk.Key.space:
				if (link == null || !link.IsIdentifier)
					goto default;
				return;
			case Gdk.Key.Delete:
				if (link != null && caretOffset == link.PrimaryLink.EndOffset && !IsSomethingSelectedInLink (editor, link.PrimaryLink))
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
				ExitTextLinkMode ();
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
		}

		bool IsSomethingSelectedInLink (MonoTextEditor editor, ISegment primaryLink)
		{
			if (editor.IsSomethingSelected) {
				int start = primaryLink.Offset + baseOffset;
				int end = start + primaryLink.Length;
				if ((start <= editor.SelectionRange.Offset) && (editor.SelectionRange.EndOffset <= end))
					return true;
			}

			return false;
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
			using (var edit = Editor.Document.TextBuffer.CreateEdit(Microsoft.VisualStudio.Text.EditOptions.None, null, _linkEditTag)) {
				foreach (TextLink l in links) {
					UpdateTextLink(l, edit);
				}
				edit.Apply();
			}
		}

		void UpdateTextLink (TextLink link, Microsoft.VisualStudio.Text.ITextEdit edit)
		{
			if (link.GetStringFunc != null) {
				link.Values = link.GetStringFunc (GetStringCallback);
			}
			if (!link.IsEditable && link.Values != null && link.Values.Count > 0) {
				link.CurrentText = (string)link.Values [link.Values.Count - 1];
			} else {
				if (!link.PrimaryLink.IsInvalid ()) {
					int offset = link.PrimaryLink.Offset + baseOffset;
					if (offset >= 0 && link.PrimaryLink.Length >= 0)
						link.CurrentText = Editor.Document.GetTextAt(offset, link.PrimaryLink.Length);
				}
			}
			UpdateLinkText (link, edit);
		}

		void UpdateLinkText (TextLink link, Microsoft.VisualStudio.Text.ITextEdit edit)
		{
			for (int i = 0; (i < link.Links.Count); ++i)
			{
				var s = link.Links[i];
				int offset = s.Offset + baseOffset;
				if (offset < 0 || s.Length < 0 || offset + s.Length > Editor.Document.Length)
				{
					// This should never happen since it implies a corrupted link/bad update following a text change.
					continue;
				}

				var span = new Microsoft.VisualStudio.Text.Span(offset, s.Length);
				if (edit.Snapshot.GetText(span) != link.CurrentText)
				{
					edit.Replace(span, link.CurrentText);
				}
			}
		}
	}

	class TextLinkMarker : MarginMarker
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

		public override bool DrawBackground (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics)
		{
			int caretOffset = editor.Caret.Offset - BaseOffset;

			foreach (var link in mode.Links) {
				if (!link.IsEditable) 
					continue; 
				bool isPrimaryHighlighted = link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset;
				foreach (TextSegment segment in link.Links) {
					if ((BaseOffset + segment.Offset <= metrics.TextStartOffset && metrics.TextStartOffset < BaseOffset + segment.EndOffset) || (metrics.TextStartOffset <= BaseOffset + segment.Offset && BaseOffset + segment.Offset < metrics.TextEndOffset)) {
						int strOffset = BaseOffset + segment.Offset - metrics.TextStartOffset;
						int strEndOffset = BaseOffset + segment.EndOffset - metrics.TextStartOffset;

						int x_pos = metrics.Layout.IndexToPos (strOffset).X;
						int x_pos2 = metrics.Layout.IndexToPos (strEndOffset).X;
					
						x_pos = (int)(x_pos / Pango.Scale.PangoScale);
						x_pos2 = (int)(x_pos2 / Pango.Scale.PangoScale);
						Cairo.Color fillGc, rectangleGc;
						if (segment.Equals (link.PrimaryLink)) {


							fillGc = SyntaxHighlightingService.GetColor (editor.EditorTheme, isPrimaryHighlighted ? EditorThemeColors.PrimaryTemplateHighlighted2 : EditorThemeColors.PrimaryTemplate2);
							rectangleGc = SyntaxHighlightingService.GetColor (editor.EditorTheme, isPrimaryHighlighted ? EditorThemeColors.PrimaryTemplateHighlighted2 : EditorThemeColors.PrimaryTemplate2);
						} else {
							fillGc = SyntaxHighlightingService.GetColor (editor.EditorTheme, isPrimaryHighlighted ? EditorThemeColors.SecondaryTemplateHighlighted2 : EditorThemeColors.SecondaryTemplate2);
							rectangleGc = SyntaxHighlightingService.GetColor (editor.EditorTheme, isPrimaryHighlighted ? EditorThemeColors.SecondaryTemplateHighlighted : EditorThemeColors.SecondaryTemplate);
						}
						
						// Draw segment
						double x1 = metrics.TextRenderStartPosition + x_pos - 1;
						double x2 = metrics.TextRenderStartPosition + x_pos2 - 1 + 0.5;

						cr.Rectangle (x1 + 0.5, metrics.LineYRenderStartPosition + 0.5, x2 - x1, editor.LineHeight - 1);
						
						cr.SetSourceColor (fillGc);
						cr.FillPreserve ();
						
						cr.SetSourceColor (rectangleGc);
						cr.Stroke ();
					}
				}
			}
			return true;
		}
		#region IGutterMarker implementation

		public override bool CanDrawForeground (Margin margin)
		{
			return margin is GutterMargin;
		}

		public override bool CanDrawBackground (Margin margin)
		{
			return margin is GutterMargin;
		}

		public override bool DrawBackground (MonoTextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			var width = metrics.Width;

			cr.Rectangle (metrics.X, metrics.Y, metrics.Width, metrics.Height);
			var lineNumberGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineNumbers);
			cr.SetSourceColor (editor.Caret.Line == metrics.LineNumber ? SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineHighlight) : lineNumberGC);
			cr.Fill ();

			return true;
		}

		public override void DrawForeground (MonoTextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			var width = metrics.Width;
			var lineNumberBgGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineNumbersBackground);



			if (metrics.LineNumber <= editor.Document.LineCount) {
				// Due to a mac? gtk bug I need to re-create the layout here
				// otherwise I get pango exceptions.
				using (var layout = PangoUtil.CreateLayout (editor)) {
					layout.FontDescription = editor.Options.Font;
					layout.Width = (int)width;
					layout.Alignment = Pango.Alignment.Right;
					layout.SetText (metrics.LineNumber.ToString ());
					cr.Save ();
					cr.Translate (metrics.X + (int)width + (editor.Options.ShowFoldMargin ? 0 : -2), metrics.Y);
					cr.SetSourceColor (lineNumberBgGC);
					cr.ShowLayout (layout);
					cr.Restore ();
				}
			}
		}
		#endregion
	}
}
