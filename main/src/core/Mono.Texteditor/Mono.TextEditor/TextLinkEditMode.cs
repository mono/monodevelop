// 
// TextLinkEditMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using MonoDevelop.TextEditor.PopupWindow;

namespace Mono.TextEditor	
{
	public class TextLink : IListDataProvider
	{
		public ISegment PrimaryLink {
			get {
				if (links.Count == 0)
					return null;
				return links[0];
			}
		}
		
		List<Segment> links = new List<Segment> ();
		public IList<Segment> Links {
			get {
				return links;
			}
		}
		
		List<string> proposedStrings = new List<string> ();
		public IList<string> ProposedStrings {
			get {
				return ProposedStrings;
			}
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
		
		public string[] Values {
			get;
			set;
		}
		
		public Func<Func<string, string>, string> GetStringFunc {
			get;
			set;
		}
		
		public TextLink (string name)
		{
			IsEditable = true;
			this.Name  = name;
		}
		
		public void AddLink (Segment segment)
		{
			links.Add (segment);
		}
		
		public void AddString (string proposedString)
		{
			this.proposedStrings.Add (proposedString);
		}
	
		#region IListDataProvider implementation
		public string GetText (int n)
		{
			return Values[n];
		}
		
		public string GetMarkup (int n)
		{
			return Values[n];
		}
		
		public bool HasMarkup (int n)
		{
			return true;
		}
		
		public string GetCompletionText (int n)
		{
			return Values[n];
		}
		
		public Gdk.Pixbuf GetIcon (int n)
		{
			return null;
		}
		
		public int ItemCount {
			get {
				return Values != null ? Values.Length : 0;
			}
		}
		#endregion
	}
	
	public class TextLinkEditMode : SimpleEditMode
	{
		TextEditor editor;
		List<TextLink> links;
		int baseOffset;
		int endOffset;
		bool resetCaret = true;
		
		public EditMode OldMode {
			get;
			set;
		}
		
		public List<TextLink> Links  {
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
				return links.Any (l => l.IsEditable);
			}
		}
	
		public TextEditor Editor {
			get {
				return editor;
			}
		}
		
		TextLinkTooltipProvider tooltipProvider;
		public TextLinkEditMode (TextEditor editor, int baseOffset, List<TextLink> links)
		{
			this.editor = editor;
			this.links  = links;
			this.baseOffset = baseOffset;
			this.endOffset = editor.Caret.Offset;
			tooltipProvider = new TextLinkTooltipProvider (this);
			this.editor.TooltipProviders.Insert (0, tooltipProvider);
			this.editor.Caret.PositionChanged += HandlePositionChanged;
		}
		TextLink closedLink = null;
		void HandlePositionChanged(object sender, DocumentLocationEventArgs e)
		{
			int caretOffset = editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => l.Links.Any (s => s.Offset <= caretOffset && caretOffset <= s.EndOffset));
			
			if (link != null && link.ItemCount > 0) {
				if (window != null && window.DataProvider != link)
					DestroyWindow ();
				if (closedLink == link)
					return;
				closedLink = null;
				if (window == null) {
					window = new ListWindow ();
					window.DataProvider = link;
					DocumentLocation loc = editor.Document.OffsetToLocation (BaseOffset + link.PrimaryLink.Offset);
					Gdk.Point p = editor.TextViewMargin.LocationToDisplayCoordinates (loc);
					int ox = 0, oy = 0;
					editor.GdkWindow.GetOrigin (out ox, out oy);
			
					window.Move (ox + p.X - window.TextOffset , oy + p.Y + editor.LineHeight);
					window.ShowAll ();
					
				} 
			} else {
				DestroyWindow ();
				closedLink = null;
			}
		}
		
		public void StartMode ()
		{
			foreach (TextLink link in links) {
				link.CurrentText = editor.Document.GetTextAt (link.PrimaryLink.Offset + baseOffset, 
				                                              link.PrimaryLink.Length);
				foreach (ISegment segment in link.Links) {
					LineSegment line = editor.Document.GetLineByOffset (baseOffset + segment.Offset);
					if (line.GetMarker (typeof (TextLinkMarker)) != null)
						continue;
					TextLinkMarker marker = (TextLinkMarker)line.GetMarker (typeof (TextLinkMarker));
					if (marker == null) {
						marker = new TextLinkMarker (this);
						marker.BaseOffset = baseOffset;
						line.AddMarker (marker);
					}
				}
			}
			TextLink firstLink = links.First (l => l.IsEditable);
			Setlink (firstLink);
			editor.Document.TextReplaced += UpdateLinksOnTextReplace;
		}
		
		void Setlink (TextLink link)
		{
			editor.Caret.Offset    = baseOffset + link.PrimaryLink.Offset;
			editor.ScrollToCaret ();
			editor.Caret.Offset    = baseOffset + link.PrimaryLink.EndOffset;
			editor.SelectionAnchor = baseOffset + link.PrimaryLink.Offset;
			editor.SelectionRange = new Segment (editor.SelectionAnchor, link.PrimaryLink.Length);
			editor.Document.CommitUpdateAll ();
			
		}
		
		void ExitTextLinkMode ()
		{
			DestroyWindow ();
			foreach (TextLink link in links) {
				foreach (ISegment segment in link.Links) {
					LineSegment line = editor.Document.GetLineByOffset (baseOffset + segment.Offset);
					line.RemoveMarker (typeof (TextLinkMarker));
				}
			}
			if (resetCaret)
				editor.Caret.Offset = endOffset;
			editor.CurrentMode = OldMode;
			editor.Document.CommitUpdateAll ();
			editor.Document.TextReplaced -= UpdateLinksOnTextReplace;
			this.editor.Caret.PositionChanged -= HandlePositionChanged;
			this.editor.TooltipProviders.Remove (tooltipProvider);
		}
		
		bool wasReplaced = false;
		void UpdateLinksOnTextReplace (object sender, ReplaceEventArgs e)
		{
			wasReplaced = true;
			int offset = e.Offset - baseOffset;
			int delta = -e.Count + (!string.IsNullOrEmpty (e.Value) ? e.Value.Length : 0);
			foreach (TextLink link in links) {
				foreach (Segment s in link.Links) {
					if (offset < s.Offset) {
						s.Offset += delta;
					} else if (offset <= s.EndOffset) {
						s.Length += delta;
					}
				}
			}
			if (e.Offset < endOffset) 
				endOffset += delta;
		}
		
		void GotoNextLink (TextLink link)
		{
			int caretOffset = editor.Caret.Offset - baseOffset;
			TextLink nextLink = links.Find (l => l.IsEditable && l.PrimaryLink.Offset > (link != null ? link.PrimaryLink.EndOffset : caretOffset));
			if (nextLink == null)
				nextLink = links.Find (l => l.IsEditable);
			Setlink (nextLink);
		}
		
		void CompleteWindow ()
		{
			if (window == null)
				return;
			TextLink lnk = (TextLink)window.DataProvider;
			int line = editor.Caret.Line;
			editor.Replace (baseOffset + lnk.PrimaryLink.Offset, lnk.PrimaryLink.Length, window.CompleteWord);
			lnk.CurrentText = window.CompleteWord;
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (window != null) {
				ListWindow.KeyAction action = window.ProcessKey (key, modifier);
				if ((action & ListWindow.KeyAction.Complete) == ListWindow.KeyAction.Complete)
					CompleteWindow ();
				if ((action & ListWindow.KeyAction.CloseWindow) == ListWindow.KeyAction.CloseWindow) {
					closedLink = (TextLink)window.DataProvider;
					DestroyWindow ();
				}
				if ((action & ListWindow.KeyAction.Complete) == ListWindow.KeyAction.Complete)
					GotoNextLink (closedLink);

				if ((action & ListWindow.KeyAction.Ignore) == ListWindow.KeyAction.Ignore)
					return;
			}
			int caretOffset = editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => l.Links.Any (s => s.Offset <= caretOffset && caretOffset <= s.EndOffset));
			switch (key) {
			case Gdk.Key.BackSpace:
				if (link != null && caretOffset == link.PrimaryLink.Offset)
					return;
				goto default;
			case Gdk.Key.Delete:
				if (link != null && caretOffset == link.PrimaryLink.EndOffset)
					return;
				goto default;
			case Gdk.Key.Tab:
				GotoNextLink (link);
				return;
			case Gdk.Key.ISO_Left_Tab:
				TextLink prevLink = links.FindLast (l => l.IsEditable && l.PrimaryLink.Offset < (link != null ? link.PrimaryLink.Offset : caretOffset));
				if (prevLink == null)
					prevLink = links.FindLast (l => l.IsEditable);
				Setlink (prevLink);
				return;
			case Gdk.Key.Escape:
			case Gdk.Key.Return:
				if (window != null) {
					CompleteWindow ();
				} else {
					ExitTextLinkMode ();
				}
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
			foreach (TextLink l in links) {
				if (l.GetStringFunc != null && !l.IsEditable) {
					l.CurrentText = l.GetStringFunc (GetStringCallback);
				} else {
					l.CurrentText = editor.Document.GetTextAt (l.PrimaryLink.Offset + baseOffset, 
					                                           l.PrimaryLink.Length);
				}
				UpdateLinkText (l);
			}
			editor.Document.CommitUpdateAll ();
		}
		ListWindow window;
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
		
		public void UpdateLinkText (TextLink link)
		{
			for (int i = link.Links.Count - 1; i >= 0; i--) {
				Segment s = link.Links[i];
				editor.Replace (s.Offset + baseOffset, s.Length, link.CurrentText);
				s.Length = link.CurrentText.Length;
			}
		}
		
	}
	
	public class TextLinkTooltipProvider : ITooltipProvider
	{
		TextLinkEditMode mode;
		
		public TextLinkTooltipProvider (TextLinkEditMode mode)
		{
			this.mode = mode;
		}

		#region ITooltipProvider implementation 
		public object GetItem (TextEditor editor, int offset)
		{
			int o = offset - mode.BaseOffset;
			return mode.Links.First (l => l.PrimaryLink.Offset <= o && o <= l.PrimaryLink.EndOffset);
		}
		
		public Gtk.Window CreateTooltipWindow (TextEditor editor, Gdk.ModifierType modifierState, object item)
		{
			TextLink link = item as TextLink;
			if (link == null)
				return null;
			
			TooltipWindow window = new TooltipWindow ();
			window.Markup = link.Tooltip;
			return window;
		}
		
		public void GetRequiredPosition (TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			TooltipWindow win = (TooltipWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		
		public bool IsInteractive (TextEditor editor, Gtk.Window tipWindow)
		{
			return false;
		}
		#endregion 
	}
	
	public class TextLinkMarker : TextMarker, IBackgroundMarker
	{
		TextLinkEditMode mode;
		
		public int BaseOffset {
			get;
			set;
		}
		
		public TextLinkMarker (TextLinkEditMode mode)
		{
			this.mode = mode;
		}
		
		void InternalDrawBackground (TextEditor editor, Gdk.Drawable win, bool selected, int startOffset, int endOffset, int y, ref int startXPos, int endXPos, ref bool drawBg)
		{
			Gdk.Rectangle clipRectangle = new Gdk.Rectangle (mode.Editor.TextViewMargin.XOffset, 0, 
			                                                 editor.Allocation.Width - mode.Editor.TextViewMargin.XOffset, editor.Allocation.Height);
			
			// draw default background
			using (Gdk.GC fillGc = new Gdk.GC (win)) {
				fillGc.ClipRectangle = clipRectangle;
				fillGc.RgbFgColor = selected ? editor.ColorStyle.Selection.BackgroundColor : editor.ColorStyle.Default.BackgroundColor;
				win.DrawRectangle (fillGc, true, startXPos, y, endXPos, editor.LineHeight);
			}
			
			if (startOffset >= endOffset)
				return;
			
			int caretOffset = editor.Caret.Offset - BaseOffset;
			foreach (TextLink link in mode.Links) {
				if (!link.IsEditable)
					continue;
				bool isPrimaryHighlighted = link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset;
				
				foreach (ISegment segment in link.Links) {
					if (BaseOffset + segment.Offset <= startOffset && startOffset < BaseOffset + segment.EndOffset) {
						int strOffset    = startOffset - (BaseOffset + segment.Offset);
						int strEndOffset = System.Math.Min (segment.Length, endOffset - startOffset);
						string txt = strEndOffset - strOffset <= link.CurrentText.Length - strOffset ? link.CurrentText.Substring (strOffset, strEndOffset - strOffset) : "";
						int width = editor.GetWidth (txt);
						using (Gdk.GC rectangleGc = new Gdk.GC (win)) {
							rectangleGc.ClipRectangle = clipRectangle;
							using (Gdk.GC fillGc = new Gdk.GC (win)) {
								fillGc.ClipRectangle = clipRectangle;
								drawBg = false;
								
								if (segment == link.PrimaryLink) {
									fillGc.RgbFgColor      = isPrimaryHighlighted ? editor.ColorStyle.PrimaryTemplateHighlighted.BackgroundColor : editor.ColorStyle.PrimaryTemplate.BackgroundColor;
									rectangleGc.RgbFgColor = isPrimaryHighlighted ? editor.ColorStyle.PrimaryTemplateHighlighted.Color           : editor.ColorStyle.PrimaryTemplate.Color;
								} else {
									fillGc.RgbFgColor      = isPrimaryHighlighted ? editor.ColorStyle.SecondaryTemplateHighlighted.BackgroundColor : editor.ColorStyle.SecondaryTemplate.BackgroundColor;
									rectangleGc.RgbFgColor = isPrimaryHighlighted ? editor.ColorStyle.SecondaryTemplateHighlighted.Color           : editor.ColorStyle.SecondaryTemplate.Color;
								}
								// Draw segment
								if (!selected)
									win.DrawRectangle (fillGc, true, startXPos, y, width, editor.LineHeight);
								
								if (strOffset != 0) {
									int x1 = startXPos - 1;
									int x2 = x1 + System.Math.Max (1, width) - 1;
									int y2 = y + editor.LineHeight - 1;
									
									win.DrawLine (rectangleGc, x1, y, x2, y);
									win.DrawLine (rectangleGc, x1, y2, x2, y2);
									win.DrawLine (rectangleGc, x2, y, x2, y2);
								} else {
									win.DrawRectangle (rectangleGc, false, startXPos, y, System.Math.Max (1, width) - 1, editor.LineHeight - 1);
								}
							}
						}
					}
				}
			}
			startXPos += editor.GetWidth (editor.Document.GetTextBetween (startOffset, endOffset));
		}
		
		bool Overlaps (ISegment segment, int start, int end)
		{
			return segment.Offset <= start && start < segment.EndOffset || 
				    segment.Offset <= end && end < segment.EndOffset ||
					start <= segment.Offset && segment.Offset < end ||
					start < segment.EndOffset && segment.EndOffset < end;
		}
		
		public bool DrawBackground (TextEditor editor, Gdk.Drawable win, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos, ref bool drawBg)
		{
			int curOffset = startOffset;
			
			foreach (TextLink link in mode.Links) {
				if (!link.IsEditable)
					continue;
				ISegment segment = link.Links.Where (s => Overlaps (s, curOffset - BaseOffset, endOffset - BaseOffset)).FirstOrDefault ();
				if (segment == null) {
					break;
				}
				InternalDrawBackground (editor, win, selected, curOffset, segment.Offset + BaseOffset, y, ref startXPos, endXPos, ref drawBg);
				curOffset = segment.EndOffset + BaseOffset;
				InternalDrawBackground (editor, win, selected, segment.Offset + BaseOffset, curOffset, y, ref startXPos, endXPos, ref drawBg);
			}
			InternalDrawBackground (editor, win, selected, curOffset, endOffset, y, ref startXPos, endXPos, ref drawBg);
			return true;
		}
	}
}