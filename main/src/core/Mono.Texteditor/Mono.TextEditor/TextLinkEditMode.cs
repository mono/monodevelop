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

namespace Mono.TextEditor	
{
	public class TextLink
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
		
		public bool ShouldStartTextLinkMode {
			get {
				return links.Any (l => l.IsEditable);
			}
		}
		
		public TextLinkEditMode (TextEditor editor, int baseOffset, List<TextLink> links)
		{
			this.editor = editor;
			this.links  = links;
			this.baseOffset = baseOffset;
			this.endOffset = editor.Caret.Offset;
		}
		
		public void StartMode ()
		{
			foreach (TextLink link in links) {
				link.CurrentText = editor.Document.GetTextAt (link.PrimaryLink.Offset + baseOffset, 
				                                              link.PrimaryLink.Length);
				foreach (ISegment segment in link.Links) {
					LineSegment line = editor.Document.GetLineByOffset (baseOffset + segment.Offset);
					TextLinkMarker marker = (TextLinkMarker)line.GetMarker (typeof (TextLinkMarker));
					if (marker == null) {
						marker = new TextLinkMarker ();
						marker.BaseOffset = baseOffset;
						line.AddMarker (marker);
					}
					marker.AddLink (link);
				}
			}
			TextLink firstLink = links.First (l => l.IsEditable);
			Setlink (firstLink);
			editor.Document.TextReplaced += UpdateLinksOnTextReplace;
		}
		
		void Setlink (TextLink link)
		{
			editor.Caret.Offset    = baseOffset + link.PrimaryLink.EndOffset;
			editor.SelectionAnchor = baseOffset + link.PrimaryLink.Offset;
			editor.SelectionRange = new Segment (editor.SelectionAnchor, link.PrimaryLink.Length);
			editor.Document.CommitUpdateAll ();
		}
		
		void ExitTextLinkMode ()
		{
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
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
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
				TextLink nextLink = links.Find (l => l.IsEditable && l.PrimaryLink.Offset > (link != null ? link.PrimaryLink.EndOffset : caretOffset));
				if (nextLink == null)
					nextLink = links[0];
				Setlink (nextLink);
				return;
			case Gdk.Key.ISO_Left_Tab:
				TextLink prevLink = links.FindLast (l => l.IsEditable && l.PrimaryLink.Offset < (link != null ? link.PrimaryLink.Offset : caretOffset));
				if (prevLink == null)
					prevLink = links[links.Count - 1];
				Setlink (prevLink);
				return;
			case Gdk.Key.Escape:
			case Gdk.Key.Return:
				ExitTextLinkMode ();
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
	
	public class TextLinkMarker : TextMarker, IBackgroundMarker
	{
		HashSet<TextLink> links = new HashSet<TextLink> ();
		
		public int BaseOffset {
			get;
			set;
		}
		
		public void AddLink (TextLink link)
		{
			links.Add (link);
		}
		
		public bool DrawBackground (TextEditor editor, Gdk.Drawable win, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos, ref bool drawBg)
		{
			int caretOffset = editor.Caret.Offset - BaseOffset;
			
			foreach (TextLink link in links) {
				if (!link.IsEditable)
					continue;
				
				foreach (ISegment segment in link.Links) {
					if (BaseOffset + segment.Offset <= startOffset && startOffset <= BaseOffset + segment.EndOffset) {
						int strOffset = startOffset - (BaseOffset + segment.Offset);
						int width = editor.GetWidth (link.CurrentText.Substring (strOffset));
						using (Gdk.GC gc = new Gdk.GC (win)) {
							drawBg = false;
							
							if (segment == link.PrimaryLink) {
								gc.RgbFgColor = editor.ColorStyle.PrimaryTemplate.BackgroundColor;
							} else {
								gc.RgbFgColor = editor.ColorStyle.Default.BackgroundColor;
								if (segment != link.PrimaryLink && link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset) 
									gc.RgbFgColor = editor.ColorStyle.SecondaryTemplate.BackgroundColor;
							}
							if (selected)
								gc.RgbFgColor = editor.ColorStyle.Selection.BackgroundColor;
							win.DrawRectangle (gc, true, startXPos, y, width, editor.LineHeight);
							
							gc.RgbFgColor = editor.ColorStyle.Default.BackgroundColor;
							if (selected)
								gc.RgbFgColor = editor.ColorStyle.Selection.BackgroundColor;
							win.DrawRectangle (gc, true, startXPos + width, y, endXPos - (startXPos + width), editor.LineHeight);
							
							if (segment != link.PrimaryLink) {
								if (link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset) {
									gc.RgbFgColor = editor.ColorStyle.SecondaryTemplate.Color;
									win.DrawRectangle (gc, false, startXPos, y, System.Math.Max (1, width), editor.LineHeight - 1);
								}
							} else if (segment.Offset <= caretOffset && caretOffset <= segment.EndOffset) {
								gc.RgbFgColor = editor.ColorStyle.PrimaryTemplate.Color;
								if (strOffset != 0) {
									int x1 = startXPos - 1;
									int x2 = x1 + System.Math.Max (1, width) - 1;
									int y2 = y + editor.LineHeight - 1;
									
									win.DrawLine (gc, x1, y, x2, y);
									win.DrawLine (gc, x1, y2, x2, y2);
									win.DrawLine (gc, x2, y, x2, y2);
								} else {
									win.DrawRectangle (gc, false, startXPos - 1, y, System.Math.Max (1, width), editor.LineHeight - 1);
								}
							}
						}
					}
				}
			}
			return true;
		}
	}
}