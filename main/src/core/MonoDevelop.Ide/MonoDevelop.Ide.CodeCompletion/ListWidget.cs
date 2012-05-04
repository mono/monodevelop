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
using ICSharpCode.NRefactory.Completion;

namespace MonoDevelop.Ide.CodeCompletion
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
		
		int rowHeight;
		bool buttonPressed;
		public event EventHandler SelectionChanged;

		protected virtual void OnSelectionChanged (EventArgs e)
		{
			var handler = this.SelectionChanged;
			if (handler != null)
				handler (this, e);
		}

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
		
		public bool CloseOnSquareBrackets {
			get;
			set;
		}
		
			
		static bool inCategoryMode;
		public bool InCategoryMode {
			get { return inCategoryMode; }
			set {
				inCategoryMode = value;
				this.CalcVisibleRows ();
				this.UpdatePage ();
				if (inCategoryMode)
					SelectFirstItemInCategory ();
			}
		}
		public int CategoryCount {
			get { return this.categories.Count; }
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
		
		public void ResetState ()
		{
			categories.Clear ();
			filteredItems.Clear ();
			oldCompletionString = completionString = null;
			selection = 0;
			page = 0;
			AutoSelect = false;
		}
		
		public int SelectionFilterIndex {
			get {
				var idx = SelectedItem;
				if (idx < 0)
					return -1;
				return filteredItems.IndexOf (idx);
			}
			set {
				if (value < 0) {
					SelectedItem = -1;
					return;
				}
				if (value < filteredItems.Count)
					SelectedItem = filteredItems [value];
			}
		}
		
		public int SelectedItem {
			get { 
				if (selection < 0 || filteredItems.Count == 0)
					return -1;
				return selection;
			}
			set {
				if (value != selection) {
					selection = value;
					UpdatePage ();
					OnSelectionChanged (EventArgs.Empty);
					this.QueueDraw ();
				}
			}
		}
		
		int GetIndex (bool countCategories, int item)
		{
			int result = -1;
			int yPos = 0;
			int curItem = 0;
			Iterate (false, ref yPos, delegate (Category category, int ypos) {
				if (countCategories)
					curItem++;
			}, delegate (Category curCategory, int item2, int itemIndex, int ypos) {
				if (item == item2) {
					result = curItem;
					return false;
				}
				curItem++;
				return true;
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
						result = category.Items [0];
					curItem++;
				}
			}, delegate (Category curCategory, int item, int itemIndex, int ypos) {
				if (curItem == index) {
					result = item;
					return false;
				}
				curItem++;
				return true;
			});
			
			return result;
		}
		
		public void MoveToCategory (int relative)
		{
			int current = CurrentCategory ();
			int next = System.Math.Min (categories.Count - 1, System.Math.Max (0, current + relative));
			if (next < 0 || next >= categories.Count)
				return;
			Category newCategory = categories[next];
			SelectionFilterIndex = newCategory.Items[0];
			if (next == 0)
				Page = 0;
			UpdatePage ();
		}
		
		int CurrentCategory ()
		{
			for (int i = 0; i < categories.Count; i++) {
				if (categories[i].Items.Contains (SelectionFilterIndex)) 
					return i;
			}
			return -1;
		}
		
		public void MoveCursor (int relative)
		{
			int newIndex = GetIndex (false, SelectedItem) + relative;
			int newSelection = GetItem (false, newIndex);
			if (newSelection < 0) 
				return;

			if (SelectedItem == newSelection && relative < 0) {
				Page = 0;
			} else {
				SelectedItem = newSelection;
			}
		}
		
		public void UpdatePage ()
		{
			int index = GetIndex (true, SelectedItem);
			if (index < page || index >= page + VisibleRows)
				page = index - (VisibleRows / 2);
			int itemCount = filteredItems.Count;
			if (InCategoryMode) {
				itemCount += categories.Count;
				if (categories.Any (cat => cat.CompletionCategory == null))
					itemCount--;
			}
			Page = System.Math.Max (0, System.Math.Min (page, itemCount - VisibleRows - 1));
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
				return AutoSelect && (AutoCompleteEmptyMatch || !string.IsNullOrEmpty (CompletionString));
			}
		}
		
		public int Page {
			get { return page; }
			set {
				if (page != value) {
					page = value;
					this.QueueDraw ();
				}
				OnSelectionChanged (EventArgs.Empty);
			}
		}
		
		protected override bool OnButtonPressEvent (EventButton e)
		{
			SelectedItem = GetRowByPosition ((int)e.Y);
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
			SelectedItem = GetRowByPosition ((int)e.Y);
			return true;
		}
		
		string NoMatchesMsg {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("No matches"); }
		}
		
		string NoSuggestionsMsg {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("No suggestions"); }
		}

		Gdk.Color HighlightColor {
			get {
				return (Gdk.Color)Mono.TextEditor.HslColor.GenerateHighlightColors (Style.Base (State), Style.Text (State), 3)[2];
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Gdk.Window window = args.Window;
			var alloc = Allocation;
			int width = alloc.Width;
			int height = alloc.Height;
			
			int lineWidth = width - margin * 2;
			int xpos = margin + padding;
			int yPos = margin;
			
			if (PreviewCompletionString) {
				layout.SetText (
					string.IsNullOrEmpty (CompletionString) ? MonoDevelop.Core.GettextCatalog.GetString ("Select template") : CompletionString
				);
				int wi, he;
				layout.GetPixelSize (out wi, out he);
				window.DrawRectangle (this.Style.BaseGC (StateType.Insensitive), true, margin, yPos, lineWidth, he + padding);
				window.DrawLayout (
					string.IsNullOrEmpty (CompletionString) ? this.Style.TextGC (StateType.Insensitive) : this.Style.TextGC (StateType.Normal),
					xpos,
					yPos,
					layout
				);
				yPos += rowHeight;
			}
			
			//when there are no matches, display a message to indicate that the completion list is still handling input
			if (filteredItems.Count == 0) {
				Gdk.GC gc = new Gdk.GC (window);
				gc.RgbFgColor = new Gdk.Color (0xff, 0xbc, 0xc1);
				window.DrawRectangle (gc, true, 0, yPos, width, height - yPos);
				layout.SetText (win.DataProvider.ItemCount == 0 ? NoSuggestionsMsg : NoMatchesMsg);
				int lWidth, lHeight;
				layout.GetPixelSize (out lWidth, out lHeight);
				gc.RgbFgColor = new Gdk.Color (0, 0, 0);
				window.DrawLayout (gc, (width - lWidth) / 2, yPos + (height - lHeight - yPos) / 2, layout);
				gc.Dispose ();
				return true;
			}
			
			var textGCInsensitive = this.Style.TextGC (StateType.Insensitive);
			var textGCNormal = this.Style.TextGC (StateType.Normal);
			var fgGCNormal = this.Style.ForegroundGC (StateType.Normal);
			var matcher = CompletionMatcher.CreateCompletionMatcher (CompletionString);
			var highlightColor = HighlightColor;
			Iterate (true, ref yPos, delegate (Category category, int ypos) {
				if (ypos >= height - margin)
					return;
				
				//	window.DrawRectangle (this.Style.BackgroundGC (StateType.Insensitive), true, 0, yPos, width, rowHeight);
				int x = 2;
				if (!string.IsNullOrEmpty (category.CompletionCategory.Icon)) {
					var icon = ImageService.GetPixbuf (category.CompletionCategory.Icon, IconSize.Menu);
					window.DrawPixbuf (fgGCNormal, icon, 0, 0, margin, ypos, icon.Width, icon.Height, Gdk.RgbDither.None, 0, 0);
					x = icon.Width + 4;
				}
				
				layout.SetMarkup ("<span weight='bold'>" + category.CompletionCategory.DisplayText + "</span>");
				window.DrawLayout (textGCInsensitive, x, ypos, layout);
				layout.SetMarkup ("");
			}, delegate (Category curCategory, int item, int itemidx, int ypos) {
				if (ypos >= height - margin)
					return false;
				if (InCategoryMode && curCategory != null && curCategory.CompletionCategory != null) {
					xpos = margin + padding + 8;
				} else {
					xpos = margin + padding;
				}
				string markup      = win.DataProvider.HasMarkup (item) ? (win.DataProvider.GetMarkup (item) ?? "&lt;null&gt;") : GLib.Markup.EscapeText (win.DataProvider.GetText (item) ?? "<null>");
				string description = win.DataProvider.GetDescription (item);
				
				if (string.IsNullOrEmpty (description)) {
					layout.SetMarkup (markup);
				} else {
					if (item == SelectedItem) {
						layout.SetMarkup (markup + " " + description );
					} else {
						layout.SetMarkup (markup + " <span foreground=\"darkgray\">" + description + "</span>");
					}
				}
				int mw, mh;
				layout.GetPixelSize (out mw, out mh);
				if (mw > listWidth) {
					WidthRequest = listWidth = mw;
					win.WidthRequest = win.Allocation.Width + mw - width;
					win.QueueResize ();
				}
			
				string text = win.DataProvider.GetText (item);
				
				if ((!SelectionEnabled || item != SelectedItem) && !string.IsNullOrEmpty (text)) {
					int[] matchIndices = matcher.GetMatch (text);
					if (matchIndices != null) {
						Pango.AttrList attrList = layout.Attributes ?? new Pango.AttrList ();
						for (int newSelection = 0; newSelection < matchIndices.Length; newSelection++) {
							int idx = matchIndices[newSelection];
							Pango.AttrForeground fg = new Pango.AttrForeground (highlightColor.Red, highlightColor.Green, highlightColor.Blue);
							fg.StartIndex = (uint)idx;
							fg.EndIndex = (uint)(idx + 1);
							attrList.Insert (fg);
						}
						layout.Attributes = attrList;
					}
				}
				
				Gdk.Pixbuf icon = win.DataProvider.GetIcon (item);
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
				if (item == SelectedItem) {
					if (SelectionEnabled) {
						window.DrawRectangle (this.Style.BaseGC (StateType.Selected), true, margin, ypos, lineWidth, he + padding);
						window.DrawLayout (this.Style.TextGC (StateType.Selected), xpos + iconWidth + 2, typos, layout);
					} else {
						window.DrawRectangle (this.Style.DarkGC (StateType.Prelight), false, margin, ypos, lineWidth - 1, he + padding - 1);
						window.DrawLayout (textGCNormal, xpos + iconWidth + 2, typos, layout);
					}
				} else
					window.DrawLayout (textGCNormal, xpos + iconWidth + 2, typos, layout);
				if (icon != null)
					window.DrawPixbuf (fgGCNormal, icon, 0, 0, xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
				
				layout.SetMarkup ("");
				if (layout.Attributes != null) {
					layout.Attributes.Dispose ();
					layout.Attributes = null;
				}
				return true;
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
		
		string oldCompletionString = null;
		public void FilterWords ()
		{
			categories.Clear ();
			var matcher = CompletionMatcher.CreateCompletionMatcher (CompletionString);
				
			if (oldCompletionString == null || !CompletionString.StartsWith (oldCompletionString)) {
				filteredItems.Clear ();
				for (int newSelection = 0; newSelection < win.DataProvider.ItemCount; newSelection++) {
					if (string.IsNullOrEmpty (CompletionString) || matcher.IsMatch (win.DataProvider.GetText (newSelection))) {
						var completionCategory = win.DataProvider.GetCompletionCategory (newSelection);
						GetCategory (completionCategory).Items.Add (newSelection);
						filteredItems.Add (newSelection);
					}
				}
			} else {
				var oldItems = filteredItems;
				filteredItems = new List<int> ();
				foreach (int newSelection in oldItems) {
					if (string.IsNullOrEmpty (CompletionString) || matcher.IsMatch (win.DataProvider.GetText (newSelection))) {
						var completionCategory = win.DataProvider.GetCompletionCategory (newSelection);
						GetCategory (completionCategory).Items.Add (newSelection);
						filteredItems.Add (newSelection);
					}
				}
			}

			filteredItems.Sort (delegate (int left, int right) {
				var lt = win.DataProvider.GetText (left);
				var rt = win.DataProvider.GetText (right);
/*				int r1;
				int r2;
				matcher.CalcMatchRank (lt, out r1);
				matcher.CalcMatchRank (rt, out r2);
				if (r1 == r2) {
					if (lt.Length != rt.Length)
						return lt.Length.CompareTo (rt.Length);*/
					return lt.CompareTo (rt);
/*				}
				return r1.CompareTo (r2);*/
			});
			categories.Sort (delegate (Category left, Category right) {
				return left.CompletionCategory != null ? left.CompletionCategory.CompareTo (right.CompletionCategory) : -1;
			});
			
			SelectFirstItemInCategory ();
			CalcVisibleRows ();
			UpdatePage ();
			
			OnWordsFiltered (EventArgs.Empty);
			oldCompletionString = CompletionString;
		}
		
		void SelectFirstItemInCategory ()
		{
			if (string.IsNullOrEmpty (CompletionString) && inCategoryMode)
				selection = categories.First ().Items.First ();
		}
		
		protected virtual void OnWordsFiltered (EventArgs e)
		{
			EventHandler handler = this.WordsFiltered;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler WordsFiltered;
		
		int GetRowByPosition (int ypos)
		{
			return GetItem (true, page + (ypos - margin) / rowHeight - (PreviewCompletionString ? 1 : 0));
		}
		
		public Gdk.Rectangle GetRowArea (int row)
		{
			if (this.GdkWindow == null)
				return Gdk.Rectangle.Zero;
			row -= page;
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			return new Gdk.Rectangle (margin, margin + rowHeight * row, winWidth, rowHeight);
		}
		
		public int VisibleRows {
			get {
				int rowWidth;
				layout.GetPixelSize (out rowWidth, out rowHeight);
				rowHeight += padding;
				return Allocation.Height / rowHeight;
			}
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			UpdatePage ();
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Height += requisition.Height % rowHeight;
		}

		void CalcVisibleRows ()
		{
			int winHeight = 200;
			int lvWidth, lvHeight;
			this.GetSizeRequest (out lvWidth, out lvHeight);
			
			int rowWidth;
			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight += padding;
			int requestedVisibleRows = (winHeight + padding - margin * 2) / rowHeight;
			
			int viewableCats = InCategoryMode ? categories.Count: 0;
			if (InCategoryMode && categories.Any (cat => cat.CompletionCategory == null))
				viewableCats--;
			int newHeight = (rowHeight * Math.Max (1, Math.Min (requestedVisibleRows, Math.Max (1, filteredItems.Count + viewableCats)))) + margin * 2;
			if (PreviewCompletionString) {
				newHeight += rowHeight;
			}
			
			if (lvWidth != listWidth || lvHeight != newHeight) 
				this.SetSizeRequest (listWidth, newHeight);
		}
		
		const int spacing = 2;
		
		delegate void CategoryAction (Category category, int yPos);
		delegate bool ItemAction (Category curCategory, int item, int itemIndex, int yPos);
		
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
					
					bool result = IterateItems (category, startAtPage,ref ypos, ref curItem, action);
					if (!result)
						break;
				}
			} else {
				int startItem = 0;
				if (startAtPage)
					startItem = curItem = page;
				if (action != null) {
					for (int item = startItem; item < filteredItems.Count; item++) {
						bool result = action (null, filteredItems[item], curItem, ypos);
						if (!result)
							break;
						ypos += rowHeight;
						curItem++;
					}
				} else {
					int itemCount = (filteredItems.Count - startItem);
					ypos += rowHeight * itemCount;
					curItem += itemCount;
				}
			}
		}
		
		bool IterateItems (Category category, bool startAtPage, ref int ypos, ref int curItem, ItemAction action)
		{
			foreach (int item in category.Items) {
				if (!startAtPage || curItem >= page) {
					if (action != null) {
						bool result = action (category, item, curItem, ypos);
						if (!result)
							return false;
					}
					ypos += rowHeight;
				}
				curItem++;
			}
			return true;
		}
	}
}
