// 
// ListWidget.cs
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

using Gtk;
using Gdk;
using Pango;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Gui.Completion
{
	public class ListWidget : Gtk.DrawingArea
	{
		int margin = 0;
		int padding = 4;
		int listWidth = 300;
		Pango.Layout layout;
		ListWindow win;
		int selection = 0;
		int page = 0;
		int visibleRows = -1;
		int rowHeight;
		bool buttonPressed;
		public event EventHandler SelectionChanged;
		string completionString;
		
		public string CompletionString {
			get { return completionString; }
			set {
				completionString = value;
				FilterWords ();
				QueueDraw ();
			}
		}
		
		public bool PreviewCompletionString {
			get;
			set;
		}
		
		public ListWidget (ListWindow win)
		{
			this.win = win;
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;
		}
		
		public void Reset ()
		{
			if (win.DataProvider == null) {
				selection = -1;
				return;
			}
			selection = win.DataProvider.ItemCount == 0 ? -1 : 0;
			page = 0;
			AutoSelect = false;
			if (IsRealized) {
				UpdateStyle ();
				QueueDraw ();
			}
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}
		
		public int SelectionIndex {
			get {
				if (Selection < 0 || filteredItems.Count <= Selection)
					return -1;
				return filteredItems[Selection];
			}
		}
		
		public int Selection {
			get { return selection; }
			set {
				value = Math.Min (filteredItems.Count - 1, Math.Max (0, value));
				if (value != selection) {
					selection = value;
					UpdatePage ();
					if (SelectionChanged != null)
						SelectionChanged (this, EventArgs.Empty);
					this.QueueDraw ();
				}
			}
		}
		
		public void UpdatePage ()
		{
			if (!IsRealized) {
				page = 0;
				return;
			}
			
			if (selection < page || selection >= page + VisibleRows)
				page = selection - (VisibleRows / 2);
			page = System.Math.Max (0, System.Math.Min (page, filteredItems.Count - VisibleRows));
		}
		
		bool autoSelect;
		public bool AutoSelect {
			get { return autoSelect; }
			set {
				autoSelect = value;
				QueueDraw ();
			}
		}
		
		public int Page {
			get { return page; }
			set {
				page = value;
				this.QueueDraw ();
			}
		}
		
		protected override bool OnButtonPressEvent (EventButton e)
		{
			Selection = GetRowByPosition ((int)e.Y);
			buttonPressed = true;
			return base.OnButtonPressEvent (e);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton e)
		{
			buttonPressed = false;
			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion e)
		{
			if (!buttonPressed)
				return base.OnMotionNotifyEvent (e);
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			Selection = GetRowByPosition ((int)e.Y);
			return true;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			base.OnExposeEvent (args);
			DrawList ();
			return true;
		}
		
		public int TextOffset {
			get {
				int iconWidth, iconHeight;
				if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out iconWidth, out iconHeight))
					iconHeight = iconWidth = 24;
				return iconWidth + margin + padding + 2;
			}
		}
		
		internal List<int> filteredItems = new List<int> ();
		public static int MatchRating (string filterText, string text)
		{
			int[] indices = Match (filterText, text);
			if (indices == null)
				return -1;
			int result = 0;
			int lastIndex = -10;
			
			for (int i = 0; i < indices.Length; i++) {
				int idx = indices[i];
				if (idx >= text.Length)
					break;
				int positionRating = short.MaxValue - idx + 1;
				int weight = filterText[i] == text[idx] ? 10 : 5;
				result += positionRating * weight;
				if (idx - lastIndex == 1)
					result += positionRating * weight;
				lastIndex = idx;
			}
			return result;
		}
		static int[] Match (string filterText, string text)
		{
			if (string.IsNullOrEmpty (filterText))
				return new int[0];
			if (string.IsNullOrEmpty (text))
				return null;
			List<int> matchIndices = new List<int> ();
			bool wasMatch = false;
			int j = 0;
			for (int i = 0; i < text.Length && j < filterText.Length; i++) {
				char ch1 = char.ToUpper (text[i]);
				char ch2 = char.ToUpper (filterText[j]);
				if (ch1 == ch2) {
					j++;
					matchIndices.Add (i);
					wasMatch = true;
					continue;
				}
				if (wasMatch) {
					wasMatch = false;
					bool match = false;
					for (; i < text.Length; i++) {
						if (ch2 == text[i]) {
							i--;
							match = true;
							break;
						}
					}
					if (match)
						continue;
				}
				break;
			}
			return j == filterText.Length ? matchIndices.ToArray () : null;
		}
		
		public static bool Matches (string filterText, string text)
		{
			return Match (filterText, text) != null;
		}
		
		public void FilterWords ()
		{
			filteredItems.Clear ();
			for (int i = 0; i < win.DataProvider.ItemCount; i++) {
				if (Matches (CompletionString, win.DataProvider.GetText (i)))
					filteredItems.Add (i);
			}
			CalcVisibleRows ();
			UpdatePage ();
		}
		
		void DrawList ()
		{
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			int ypos = margin;
			int lineWidth = winWidth - margin * 2;
			int xpos = margin + padding;

			if (PreviewCompletionString) {
				layout.SetText (string.IsNullOrEmpty (CompletionString) ? MonoDevelop.Core.GettextCatalog.GetString ("Select template") : CompletionString);
				int wi, he;
				layout.GetPixelSize (out wi, out he);
				this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Insensitive), true, margin, ypos, lineWidth, he + padding);
				this.GdkWindow.DrawLayout (string.IsNullOrEmpty (CompletionString) ? this.Style.TextGC (StateType.Insensitive) : this.Style.TextGC (StateType.Normal), xpos, ypos, layout);
				ypos += rowHeight;
			}

			if (filteredItems.Count == 0) {
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbFgColor = new Gdk.Color (0xff, 0xbc, 0xc1);
				this.GdkWindow.DrawRectangle (gc, true, 0, ypos, Allocation.Width, Allocation.Height - ypos);
				gc.Dispose ();
				layout.SetText (MonoDevelop.Core.GettextCatalog.GetString ("No suggestions"));
				int width, height;
				layout.GetPixelSize (out width, out height);
				this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), (Allocation.Width - width) / 2, ypos + (Allocation.Height - height - ypos) / 2, layout);
				return;
			}
			
			int n = 0;
			while (ypos < winHeight - margin && (page + n) < filteredItems.Count) {
				bool hasMarkup = win.DataProvider.HasMarkup (filteredItems[page + n]);
				if (hasMarkup) {
					layout.SetMarkup (win.DataProvider.GetMarkup (filteredItems[page + n]) ?? "&lt;null&gt;");
				} else {
					layout.SetText (win.DataProvider.GetText (filteredItems[page + n]) ?? "<null>");
				}
				string text = win.DataProvider.GetText (filteredItems[page + n]);
				if ((!AutoSelect || page + n != selection) && !string.IsNullOrEmpty (text) && !string.IsNullOrEmpty (CompletionString)) {
					int[] matchIndices = Match (CompletionString, text);
					if (matchIndices != null) {
						Pango.AttrList attrList = layout.Attributes ?? new Pango.AttrList ();
						for (int i = 0; i < matchIndices.Length; i++) {
							int idx = matchIndices[i];
							Pango.AttrForeground fg = new Pango.AttrForeground (0, 0, ushort.MaxValue);
							fg.StartIndex = (uint)idx;
							fg.EndIndex = (uint)(idx + 1);
							attrList.Insert (fg);
						}
						layout.Attributes = attrList;
					}
				}
				
				Gdk.Pixbuf icon = win.DataProvider.GetIcon (filteredItems[page + n]);
				int iconHeight, iconWidth;
				if (icon != null) {
					iconWidth = icon.Width;
					iconHeight = icon.Height;
				} else if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out iconWidth, out iconHeight)) {
					iconHeight = iconWidth = 24;
				}
				
				int wi, he, typos, iypos;
				layout.GetPixelSize (out wi, out he);
				typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
				iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
				if (page + n == selection) {
					if (AutoSelect) {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected), true, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Selected), xpos + iconWidth + 2, typos, layout);
					} else {
						this.GdkWindow.DrawRectangle (this.Style.DarkGC (StateType.Prelight), false, margin, ypos, lineWidth - 1, he + padding - 1);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + iconWidth + 2, typos, layout);
					} 
				} else
					this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + iconWidth + 2, typos, layout);
				if (icon != null)
					this.GdkWindow.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), icon, 0, 0, xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
				ypos += rowHeight;
				n++;
				if (hasMarkup)
					layout.SetMarkup (string.Empty);
				if (layout.Attributes != null) {
					layout.Attributes.Dispose ();
					layout.Attributes = null;
				}
			}
		}
		
		int GetRowByPosition (int ypos)
		{
			if (visibleRows == -1)
				CalcVisibleRows ();
			return page + (ypos - margin) / rowHeight - (PreviewCompletionString ? 1 : 0);
		}
		
		public Gdk.Rectangle GetRowArea (int row)
		{
			row -= page;
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			return new Gdk.Rectangle (margin, margin + rowHeight * row, winWidth, rowHeight);
		}
		
		public int VisibleRows {
			get {
				if (visibleRows == -1)
					CalcVisibleRows ();
				return visibleRows;
			}
		}
		
		void CalcVisibleRows ()
		{
			if (layout == null)
				return;
			int winHeight = 200;
			int lvWidth, lvHeight;
			int rowWidth;
			this.GetSizeRequest (out lvWidth, out lvHeight);
			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight += padding;
			visibleRows = (winHeight + padding - margin * 2) / rowHeight;
			int newHeight = (rowHeight * Math.Max (1, Math.Min (visibleRows, filteredItems.Count))) + margin * 2;
			if (PreviewCompletionString) {
				visibleRows--;
				newHeight += rowHeight;
			}
			if (lvWidth != listWidth || lvHeight != newHeight)
				this.SetSizeRequest (listWidth, newHeight);
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateStyle ();
			UpdatePage ();
		}
		
		void UpdateStyle ()
		{
			this.GdkWindow.Background = this.Style.Base (StateType.Normal);
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			FontDescription des = this.Style.FontDescription.Copy ();
			layout.FontDescription = des;
			CalcVisibleRows ();
		}
	}
}
