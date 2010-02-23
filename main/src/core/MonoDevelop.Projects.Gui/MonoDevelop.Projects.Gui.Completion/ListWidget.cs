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
using MonoDevelop.Core.Gui;

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
		
		class Category {
			public CompletionCategory CompletionCategory {
				get;
				set;
			}
			
			System.Collections.Generic.List<int> items = new System.Collections.Generic.List<int> ();
			public List<int> Items {
				get { return items; }
				set { items = value; }
			}
		}
		
		List<Category> categories = new List<Category> ();
		
		public string CompletionString {
			get { return completionString; }
			set {
				if (completionString != value) {
					completionString = value;
					FilterWords ();
					QueueDraw ();
				}
			}
		}
		
		public string DefaultCompletionString {
			get;
			set;
		}
		
		public bool PreviewCompletionString {
			get;
			set;
		}
		
		static bool inCategoryMode;
		public bool InCategoryMode {
			get { return inCategoryMode; }
			set { inCategoryMode = value; this.CalcVisibleRows (); this.UpdatePage (); }
		}
		
		public ListWidget (ListWindow win)
		{
			this.win = win;
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;
			DefaultCompletionString = "";
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			FontDescription des = this.Style.FontDescription.Copy ();
			layout.FontDescription = des;
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
			CalcVisibleRows ();
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
		
		int GetIndex (bool countCategories, int itemNumber)
		{
			int result = -1;
			int yPos = 0;
			int curItem = 0;
			Iterate (false, ref yPos, delegate (Category category, int ypos) {
				if (countCategories)
					curItem++;
			}, delegate (Category curCategory, int item, int ypos) {
				if (item == itemNumber)
					result = curItem;
				curItem++;
			});
			return result;
		}
		
		int GetItem (bool countCategories, int index)
		{
			int result = -1;
			int curItem = 0;
			int yPos = 0;
			Iterate (false, ref yPos, delegate (Category category, int ypos) {
				if (countCategories) {
					if (curItem == index)
						result = category.Items[0];
					curItem++;
				}
			}, delegate (Category curCategory, int item, int ypos) {
				if (curItem == index)
					result = item;
				curItem++;
			});
			
			return result;
		}
		public void MoveToCategory (int relative)
		{
			int current = CurrentCategory ();
			int next = System.Math.Min (categories.Count - 1, System.Math.Max (0, current + relative));
				
			Category newCategory = categories[next];
			Selection = newCategory.Items[0];
			if (next == 0)
				Page = 0;
			UpdatePage ();
		}
		
		int CurrentCategory ()
		{
			for (int i = 0; i < categories.Count; i++) {
				if (categories[i].Items.Contains (Selection)) 
					return i;
			}
			return -1;
		}
		
		public void MoveCursor (int relative)
		{
			int newIndex = GetIndex (false, Selection) + relative;
			if (Math.Abs (relative) == 1) {
				if (newIndex < 0)
					newIndex = filteredItems.Count - 1;
				if (newIndex >= filteredItems.Count)
					newIndex = 0;
			}
			int newSelection = GetItem (false, System.Math.Min (filteredItems.Count - 1, System.Math.Max (0, newIndex)));
			if (newSelection < 0) 
				return;
			if (Selection == newSelection && relative < 0) {
				Page = 0;
			} else {
				Selection = newSelection;
			}
			this.UpdatePage ();
		}
		
		public void UpdatePage ()
		{
			int index = GetIndex (true, Selection);
			
			if (index < page || index >= page + VisibleRows)
				page = index - (VisibleRows / 2);
			int itemCount = filteredItems.Count;
			if (InCategoryMode)
				itemCount += categories.Count;
			Page = System.Math.Max (0, System.Math.Min (page, itemCount - VisibleRows));
		}
		
		bool autoSelect;
		public bool AutoSelect {
			get { return autoSelect; }
			set {
				autoSelect = value;
				QueueDraw ();
			}
		}
		
		public bool AutoCompleteEmptyMatch {
			get;
			set;
		}
		
		public bool SelectionEnabled {
			get {
				return AutoSelect && (AutoCompleteEmptyMatch || !string.IsNullOrEmpty (CompletionString) || !string.IsNullOrEmpty (DefaultCompletionString));
			}
		}
		
		public int Page {
			get { return page; }
			set {
				page = value;
				this.QueueDraw ();
				if (SelectionChanged != null)
					SelectionChanged (this, EventArgs.Empty);
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
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			this.GdkWindow.Background = this.Style.Base (StateType.Normal);
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
			Gdk.Window window = args.Window;
			int winWidth = window.ClipRegion.Clipbox.Width;
			int winHeight = window.ClipRegion.Clipbox.Height;
			
			int lineWidth = winWidth - margin * 2;
			int xpos = margin + padding;
			int yPos = margin;
			
			if (PreviewCompletionString) {
				layout.SetText (string.IsNullOrEmpty (CompletionString) ? MonoDevelop.Core.GettextCatalog.GetString ("Select template") : CompletionString);
				int wi, he;
				layout.GetPixelSize (out wi, out he);
				window.DrawRectangle (this.Style.BaseGC (StateType.Insensitive), true, margin, yPos, lineWidth, he + padding);
				window.DrawLayout (string.IsNullOrEmpty (CompletionString) ? this.Style.TextGC (StateType.Insensitive) : this.Style.TextGC (StateType.Normal), xpos, yPos, layout);
				yPos += rowHeight;
			}
			
			if (filteredItems.Count == 0) {
				Gdk.GC gc = new Gdk.GC (window);
				gc.RgbFgColor = new Gdk.Color (0xff, 0xbc, 0xc1);
				window.DrawRectangle (gc, true, 0, yPos, Allocation.Width, Allocation.Height - yPos);
				gc.Dispose ();
				layout.SetText (MonoDevelop.Core.GettextCatalog.GetString ("No suggestions"));
				int width, height;
				layout.GetPixelSize (out width, out height);
				window.DrawLayout (this.Style.TextGC (StateType.Normal), (Allocation.Width - width) / 2, yPos + (Allocation.Height - height - yPos) / 2, layout);
				return true;
			}
			Iterate (true, ref yPos, delegate (Category category, int ypos) {
				if (ypos >= winHeight - margin)
					return;
				
			//	window.DrawRectangle (this.Style.BackgroundGC (StateType.Insensitive), true, 0, yPos, Allocation.Width, rowHeight);
				
				Gdk.Pixbuf icon = ImageService.GetPixbuf (category.CompletionCategory.Icon, IconSize.Menu);
				window.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), icon, 0, 0, margin, ypos, icon.Width, icon.Height, Gdk.RgbDither.None, 0, 0);
				
				layout.SetMarkup ("<span weight='bold'>" + category.CompletionCategory.DisplayText + "</span>");
				window.DrawLayout (this.Style.TextGC (StateType.Insensitive), icon.Width + 4, ypos, layout);
				layout.SetMarkup ("");
			}, delegate (Category curCategory, int item, int ypos) {
				if (ypos >= winHeight - margin)
					return;
				int itemIndex = filteredItems[item];
				if (InCategoryMode && curCategory != null && curCategory.CompletionCategory != null) {
					xpos = margin + padding + 8;
				} else {
					xpos = margin + padding;
				}
				bool hasMarkup = win.DataProvider.HasMarkup (itemIndex);
				if (hasMarkup) {
					layout.SetMarkup (win.DataProvider.GetMarkup (itemIndex) ?? "&lt;null&gt;");
				} else {
					layout.SetText (win.DataProvider.GetText (itemIndex) ?? "<null>");
				}
				string text = win.DataProvider.GetText (itemIndex);
				
				if ((!SelectionEnabled || item != selection) && !string.IsNullOrEmpty (text)) {
					int[] matchIndices = Match (CompletionString, text);
					if (matchIndices != null) {
						Pango.AttrList attrList = layout.Attributes ?? new Pango.AttrList ();
						for (int newSelection = 0; newSelection < matchIndices.Length; newSelection++) {
							int idx = matchIndices[newSelection];
							Pango.AttrForeground fg = new Pango.AttrForeground (0, 0, ushort.MaxValue);
							fg.StartIndex = (uint)idx;
							fg.EndIndex = (uint)(idx + 1);
							attrList.Insert (fg);
						}
						layout.Attributes = attrList;
					}
				}
				
				Gdk.Pixbuf icon = win.DataProvider.GetIcon (itemIndex);
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
				if (item == selection) {
					if (SelectionEnabled) {
						window.DrawRectangle (this.Style.BaseGC (StateType.Selected), true, margin, ypos, lineWidth, he + padding);
						window.DrawLayout (this.Style.TextGC (StateType.Selected), xpos + iconWidth + 2, typos, layout);
					} else {
						window.DrawRectangle (this.Style.DarkGC (StateType.Prelight), false, margin, ypos, lineWidth - 1, he + padding - 1);
						window.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + iconWidth + 2, typos, layout);
					}
				} else
					window.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + iconWidth + 2, typos, layout);
				if (icon != null)
					window.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), icon, 0, 0, xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
				
				if (hasMarkup)
					layout.SetMarkup (string.Empty);
				if (layout.Attributes != null) {
					layout.Attributes.Dispose ();
					layout.Attributes = null;
				}
			});
			/*
			int n = 0;
			while (ypos < winHeight - margin && (page + n) < filteredItems.Count) {
				
				bool hasMarkup = win.DataProvider.HasMarkup (filteredItems[page + n]);
				if (hasMarkup) {
					layout.SetMarkup (win.DataProvider.GetMarkup (filteredItems[page + n]) ?? "&lt;null&gt;");
				} else {
					layout.SetText (win.DataProvider.GetText (filteredItems[page + n]) ?? "<null>");
				}
				string text = win.DataProvider.GetText (filteredItems[page + n]);
				if ((!SelectionEnabled || page + n != selection) && !string.IsNullOrEmpty (text)) {
					int[] matchIndices = Match (CompletionString, text);
					if (matchIndices != null) {
						Pango.AttrList attrList = layout.Attributes ?? new Pango.AttrList ();
						for (int newSelection = 0; newSelection < matchIndices.Length; newSelection++) {
							int idx = matchIndices[newSelection];
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
					if (SelectionEnabled) {
						window.DrawRectangle (this.Style.BaseGC (StateType.Selected), true, margin, ypos, lineWidth, he + padding);
						window.DrawLayout (this.Style.TextGC (StateType.Selected), xpos + iconWidth + 2, typos, layout);
					} else {
						window.DrawRectangle (this.Style.DarkGC (StateType.Prelight), false, margin, ypos, lineWidth - 1, he + padding - 1);
						window.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + iconWidth + 2, typos, layout);
					}
				} else
					window.DrawLayout (this.Style.TextGC (StateType.Normal), xpos + iconWidth + 2, typos, layout);
				if (icon != null)
					window.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), icon, 0, 0, xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
				ypos += rowHeight;
				n++;
				if (hasMarkup)
					layout.SetMarkup (string.Empty);
				if (layout.Attributes != null) {
					layout.Attributes.Dispose ();
					layout.Attributes = null;
				}
			}
			*/
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
			
			for (int newSelection = 0; newSelection < indices.Length; newSelection++) {
				int idx = indices[newSelection];
				if (idx >= text.Length)
					break;
				int positionRating = short.MaxValue - idx + 1;
				int weight = filterText[newSelection] == text[idx] ? 10 : 5;
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
			int itemIndex = 0;
			
			for (int newSelection = 0; newSelection < text.Length && itemIndex < filterText.Length; newSelection++) {
				char ch1 = char.ToUpper (text[newSelection]);
				char ch2 = char.ToUpper (filterText[itemIndex]);
				
				if (ch1 == ch2) {
					itemIndex++;
					matchIndices.Add (newSelection);
					wasMatch = true;
					continue;
				}
				
				if (char.IsPunctuation (ch2) || char.IsWhiteSpace (ch2)) {
					wasMatch = false;
					break;
				}
				
				if (wasMatch) {
					wasMatch = false;
					bool match = false;
					for (; newSelection < text.Length; newSelection++) {
						if (ch2 == text[newSelection]) {
							newSelection--;
							match = true;
							break;
						}
					}
					if (match)
						continue;
				}
				break;
			}
			
			return itemIndex == filterText.Length ? matchIndices.ToArray () : null;
		}
		
				
		public static bool Matches (string filterText, string text)
		{
			return Match (filterText, text) != null;
		}
		
		Category GetCategory (CompletionCategory completionCategory)
		{
			foreach (Category cat in categories) {
				if (cat.CompletionCategory == completionCategory)
					return cat;
			}
			Category result = new Category ();
			result.CompletionCategory = completionCategory;
			if (completionCategory == null) {
				categories.Add (result);
			} else {
				categories.Insert (0, result);
			}
			return result;
		}
		
		public void FilterWords ()
		{
			filteredItems.Clear ();
			categories.Clear ();
			for (int newSelection = 0; newSelection < win.DataProvider.ItemCount; newSelection++) {
				if (Matches (CompletionString, win.DataProvider.GetText (newSelection))) {
					CompletionCategory completionCategory = win.DataProvider.GetCompletionCategory (newSelection);
					GetCategory (completionCategory).Items.Add (filteredItems.Count);
					filteredItems.Add (newSelection);
				}
			}
			categories.Sort (delegate (Category left, Category right) {
				return right.CompletionCategory != null ? right.CompletionCategory.CompareTo (left.CompletionCategory) : -1;
			});
			CalcVisibleRows ();
			UpdatePage ();
		}
		
		int GetRowByPosition (int ypos)
		{
			if (visibleRows == -1)
				CalcVisibleRows ();
			
			return GetItem (true, page + (ypos - margin) / rowHeight - (PreviewCompletionString ? 1 : 0));
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
			int winHeight = 200;
			int lvWidth, lvHeight;
			int rowWidth;
			this.GetSizeRequest (out lvWidth, out lvHeight);
			
			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight += padding;
			visibleRows = (winHeight + padding - margin * 2) / rowHeight;
			
			int viewableCats = InCategoryMode ? categories.Count : 0;
			int newHeight = (rowHeight * Math.Max (1, Math.Min (visibleRows, filteredItems.Count + viewableCats))) + margin * 2;
			if (PreviewCompletionString) {
				visibleRows--;
				newHeight += rowHeight;
			}
			if (lvWidth != listWidth || lvHeight != newHeight) 
				this.SetSizeRequest (listWidth, newHeight);
		}
		
		const int spacing = 2;
		
		delegate void CategoryAction (Category category, int yPos);
		delegate void ItemAction (Category curCategory, int item, int yPos);
		
		void Iterate (bool startAtPage, ref int ypos, CategoryAction catAction, ItemAction action)
		{
			int curItem = 0;
			if (InCategoryMode) {
				foreach (Category category in this.categories) {
					if (category.CompletionCategory != null) {
						if (!startAtPage || curItem >= page) {
							if (catAction != null)  
								catAction (category, ypos);
							ypos += rowHeight;
						}
						curItem++;
					}
					
					IterateItems (category, startAtPage,ref ypos, ref curItem, action);
				}
			} else {
				for (int item = 0; item < filteredItems.Count; item++) {
					if (!startAtPage || curItem >= page) {
						if (action != null) 
							action (null, item, ypos);
						ypos += rowHeight;
					}
					curItem++;
				}
			}
		}
		
		void IterateItems (Category category, bool startAtPage, ref int ypos, ref int curItem, ItemAction action)
		{
			foreach (int item in category.Items) {
				if (!startAtPage || curItem >= page) {
					if (action != null) 
						action (category, item, ypos);
					ypos += rowHeight;
				}
				curItem++;
			}
		}
	}
}
