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
using MonoDevelop.Components;
using MonoDevelop.Ide.Fonts;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class ListWidget : Gtk.DrawingArea
	{
		int listWidth = 300;
		Pango.Layout layout, categoryLayout;
		ListWindow win;
		int selection = 0;

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
				this.ScrollToSelectedItem ();
				if (inCategoryMode)
					SelectFirstItemInCategory ();
			}
		}
		public int CategoryCount {
			get { return this.categories.Count; }
		}

		Cairo.Color backgroundColor;
		Cairo.Color selectedItemColor, selectionBorderColor;

		Gdk.Color textColor;
		Gdk.Color highlightColor;
		

		const int marginIconSpacing = 4;
		const int iconTextSpacing = 6;

		const int itemSeparatorHeight = 2;

		public ListWidget (ListWindow win)
		{
			this.win = win;
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;
			DefaultCompletionString = "";
			categoryLayout = new Pango.Layout (this.PangoContext);
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			FontDescription des = FontService.GetFontDescription ("Editor");

			var style = SyntaxModeService.GetColorStyle (Style, PropertyService.Get ("ColorScheme", "Default"));

			layout.FontDescription = des;
			var completion = style.GetChunkStyle ("completion");
			textColor = completion.Color;

			highlightColor = style.GetChunkStyle ("completion.highlight").Color;
			backgroundColor = completion.CairoBackgroundColor;
			selectedItemColor = style.GetChunkStyle ("completion.selection").CairoColor;
			selectionBorderColor = style.GetChunkStyle ("completion.selection.border").CairoColor;
		}

		internal Adjustment vadj;

		protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
		{
			this.vadj = vadj;
			base.OnSetScrollAdjustments (hadj, vadj);
			if (this.vadj != null) {
				this.vadj.ValueChanged += (sender, e) => QueueDraw ();
				SetAdjustments ();
			}
		}

		public void ResetState ()
		{
			categories.Clear ();
			filteredItems.Clear ();
			oldCompletionString = completionString = null;
			selection = 0;
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
					ScrollToSelectedItem ();
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
			ScrollToSelectedItem ();
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
				SelectedItem = 0;
			} else {
				SelectedItem = newSelection;
			}
		}
		
		public void ScrollToSelectedItem ()
		{
			var area = GetRowArea (SelectedItem);
			if (area.Y < vadj.Value) {
				vadj.Value = area.Y;
				return;
			}
			if (vadj.Value + Allocation.Height < area.Bottom) {
				vadj.Value = Math.Max (0, area.Bottom - vadj.PageSize);
			}
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

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			using (var context = Gdk.CairoHelper.Create (args.Window)) {
				context.LineWidth = 1;
				Gdk.Window window = args.Window;
				var alloc = Allocation;
				int width = alloc.Width;
				int height = alloc.Height;
				
				int lineWidth = width;
				int xpos = iconTextSpacing;
				int yPos = (int)-vadj.Value;
				
				if (PreviewCompletionString) {
					layout.SetText (
						string.IsNullOrEmpty (CompletionString) ? MonoDevelop.Core.GettextCatalog.GetString ("Select template") : CompletionString
					);
					int wi, he;
					layout.GetPixelSize (out wi, out he);
					context.Rectangle (0, yPos, lineWidth, he + iconTextSpacing);
					context.Fill ();

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
					return false;
				}


				var textGCInsensitive = this.Style.WhiteGC;
				var textGCNormal = new Gdk.GC (window);
				textGCNormal.RgbFgColor = textColor;
				var fgGCNormal = this.Style.ForegroundGC (StateType.Normal);
				var matcher = CompletionMatcher.CreateCompletionMatcher (CompletionString);
				Iterate (true, ref yPos, delegate (Category category, int ypos) {
					if (ypos >= height)
						return;
					if (ypos < -rowHeight)
						return;

					//	window.DrawRectangle (this.Style.BackgroundGC (StateType.Insensitive), true, 0, yPos, width, rowHeight);
					int x = 2;
					if (category.CompletionCategory != null && !string.IsNullOrEmpty (category.CompletionCategory.Icon)) {
						var icon = ImageService.GetPixbuf (category.CompletionCategory.Icon, IconSize.Menu);
						window.DrawPixbuf (fgGCNormal, icon, 0, 0, 0, ypos, icon.Width, icon.Height, Gdk.RgbDither.None, 0, 0);
						x = icon.Width + 4;
					}
					context.Rectangle (0, ypos, Allocation.Width, rowHeight);
					context.Color = backgroundColor;
					context.Fill ();


//					layout.SetMarkup ("<span weight='bold' foreground='#AAAAAA'>" + (category.CompletionCategory != null ? category.CompletionCategory.DisplayText : "Uncategorized") + "</span>");
//					window.DrawLayout (textGCInsensitive, x - 1, ypos + 1 + (rowHeight - py) / 2, layout);
//					layout.SetMarkup ("<span weight='bold'>" + (category.CompletionCategory != null ? category.CompletionCategory.DisplayText : "Uncategorized") + "</span>");
					categoryLayout.SetMarkup ((category.CompletionCategory != null ? category.CompletionCategory.DisplayText : "Uncategorized"));
					int px, py;
					categoryLayout.GetPixelSize (out px, out py);
					window.DrawLayout (textGCNormal, x, ypos + (rowHeight - py) / 2, categoryLayout);
				}, delegate (Category curCategory, int item, int itemidx, int ypos) {
					if (ypos >= height)
						return false;
					if (ypos < -rowHeight)
						return true;
					const int categoryModeItemIndenting = 0;
					if (InCategoryMode && curCategory != null && curCategory.CompletionCategory != null) {
						xpos = iconTextSpacing + categoryModeItemIndenting;
					} else {
						xpos = iconTextSpacing;
					}
					string markup = win.DataProvider.HasMarkup (item) ? (win.DataProvider.GetMarkup (item) ?? "&lt;null&gt;") : GLib.Markup.EscapeText (win.DataProvider.GetText (item) ?? "<null>");
					string description = win.DataProvider.GetDescription (item);
					
					if (string.IsNullOrEmpty (description)) {
						layout.SetMarkup (markup);
					} else {
						if (item == SelectedItem) {
							layout.SetMarkup (markup + " " + description);
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
					
					if (!string.IsNullOrEmpty (text)) {
						int[] matchIndices = matcher.GetMatch (text);
						if (matchIndices != null) {
							Pango.AttrList attrList = layout.Attributes ?? new Pango.AttrList ();
							for (int newSelection = 0; newSelection < matchIndices.Length; newSelection++) {
								int idx = matchIndices [newSelection];
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
						context.Rectangle (0, ypos, Allocation.Width, rowHeight);
						context.Color = this.selectedItemColor;
						context.Fill ();

						context.Color = this.selectionBorderColor;
						context.MoveTo (0, ypos + 0.5);
						context.LineTo (Allocation.Width, ypos + 0.5);

						context.MoveTo (0, ypos + rowHeight - 1 + 0.5);
						context.LineTo (Allocation.Width, ypos + rowHeight - 1 + 0.5);
						if (!SelectionEnabled)
							context.SetDash (new double[] {4, 1}, 0);
						context.Stroke ();

						if (icon != null) {
							window.DrawPixbuf (fgGCNormal, icon, 0, 0, xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
							xpos += iconTextSpacing;
						}
						window.DrawLayout (textGCNormal, xpos + iconWidth + 2, typos, layout);
					} else {
						context.Rectangle (0, ypos, Allocation.Width, rowHeight);
						context.Color = this.backgroundColor;
						context.Fill ();
						if (icon != null) {
							window.DrawPixbuf (fgGCNormal, icon, 0, 0, xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
							xpos += iconTextSpacing;
						}
						window.DrawLayout (textGCNormal, xpos + iconWidth + 2, typos, layout);
					}

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
				return false;
			}
		}
		
		public int TextOffset {
			get {
				int iconWidth, iconHeight;
				if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out iconWidth, out iconHeight))
					iconHeight = iconWidth = 16;
				var xSpacing = marginIconSpacing + iconTextSpacing;
				return iconWidth + xSpacing + 2 + 1;
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
			SetAdjustments ();
			ScrollToSelectedItem ();
			
			OnWordsFiltered (EventArgs.Empty);
			oldCompletionString = CompletionString;
		}
		
		void SelectFirstItemInCategory ()
		{
			if (string.IsNullOrEmpty (CompletionString) && inCategoryMode)
				selection = categories.First ().Items.First ();
		}

		void SetAdjustments ()
		{
			if (vadj == null)
				return;
			vadj.SetBounds (0, filteredItems.Count * rowHeight, rowHeight, Allocation.Height, Allocation.Height);
		}
		
		protected virtual void OnWordsFiltered (EventArgs e)
		{
			SetAdjustments ();
			this.vadj.Value = 0;
			EventHandler handler = this.WordsFiltered;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler WordsFiltered;
		
		int GetRowByPosition (int ypos)
		{
			return GetItem (true, ((int)vadj.Value + ypos) / rowHeight - (PreviewCompletionString ? 1 : 0));
		}
		
		public Gdk.Rectangle GetRowArea (int row)
		{
			int outpos = 0;
			int yPos = 0;
			Iterate (false, ref yPos, delegate (Category category, int ypos) {
			}, delegate (Category curCategory, int item, int itemIndex, int ypos) {
				if (item == row) {
					outpos = ypos;
					return false;
				}
				return true;
			});
			
			return new Gdk.Rectangle (0, outpos, Allocation.Width, rowHeight);
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			SetAdjustments ();
			ScrollToSelectedItem ();
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Height += requisition.Height % rowHeight;
		}

		const int maxVisibleRows = 7;
		void CalcVisibleRows ()
		{
			int lvWidth, lvHeight;
			this.GetSizeRequest (out lvWidth, out lvHeight);
			
			int rowWidth;
			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight += 4;

			int viewableCats = InCategoryMode ? categories.Count: 0;
			if (InCategoryMode && categories.Any (cat => cat.CompletionCategory == null))
				viewableCats--;
			int newHeight = rowHeight * maxVisibleRows;
			if (PreviewCompletionString) {
				newHeight += rowHeight;
			}
			if (lvWidth != listWidth || lvHeight != newHeight) 
				this.SetSizeRequest (listWidth, newHeight);
			ScrollToSelectedItem ();
		}
		
		const int spacing = 2;
		
		delegate void CategoryAction (Category category, int yPos);
		delegate bool ItemAction (Category curCategory, int item, int itemIndex, int yPos);
		
		void Iterate (bool startAtPage, ref int ypos, CategoryAction catAction, ItemAction action)
		{
			int curItem = 0;
			if (InCategoryMode) {
				foreach (Category category in this.categories) {
					var nextYPos = ypos + rowHeight;
//					if (!startAtPage || nextYPos >= vadj.Value) {
					if (catAction != null)  
						catAction (category, ypos);
//					}
					ypos = nextYPos;
					curItem++;

					bool result = IterateItems (category, startAtPage,ref ypos, ref curItem, action);
					if (!result)
						break;
				}
			} else {
				int startItem = 0;
				if (startAtPage)
					startItem = curItem = Math.Max (0, (int)(ypos / rowHeight));
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
				var nextYpos = ypos + rowHeight;
//				if (!startAtPage || nextYpos >= vadj.Value) {
					if (action != null) {
						bool result = action (category, item, curItem, ypos);
						if (!result)
							return false;
					}
//				}
				ypos = nextYpos;
				curItem++;
			}
			return true;
		}
	}
}
