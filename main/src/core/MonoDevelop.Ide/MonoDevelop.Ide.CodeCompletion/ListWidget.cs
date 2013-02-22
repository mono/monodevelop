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

using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using Pango;
using ICSharpCode.NRefactory.Completion;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Components;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class ListWidget : Gtk.DrawingArea
	{
		int listWidth = 300;
		Pango.Layout layout, categoryLayout, noMatchLayout;
		ListWindow win;
		int selection = 0;

		internal int rowHeight;
		bool buttonPressed;
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs e)
		{
			var handler = SelectionChanged;
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
		
		public bool CloseOnSquareBrackets {
			get;
			set;
		}
		
			
		static bool inCategoryMode;
		public bool InCategoryMode {
			get { return inCategoryMode; }
			set {
				inCategoryMode = value;
				CalcVisibleRows ();
				if (inCategoryMode)
					SelectFirstItemInCategory ();
			}
		}
		public int CategoryCount {
			get { return this.categories.Count; }
		}

		ICompletionWidget completionWidget;
		public ICompletionWidget CompletionWidget {
			get {
				return completionWidget;
			}
			set {
				completionWidget = value;
				SetFont ();
			}
		}

		Cairo.Color backgroundColor;
		Cairo.Color selectionBorderColor, selectionBorderInactiveColor;
		ChunkStyle selectedItemColor, selectedItemInactiveColor;
		Cairo.Color textColor;
		Cairo.Color highlightColor;
		FontDescription itemFont;

		const int marginIconSpacing = 4;
		const int iconTextSpacing = 6;

		const int itemSeparatorHeight = 2;

		void SetFont ()
		{
			// TODO: Add font property to ICompletionWidget;
			if (itemFont != null)
				itemFont.Dispose ();
			itemFont = FontService.GetFontDescription ("Editor").Copy ();
			var provider = CompletionWidget as ITextEditorDataProvider;
			if (provider != null) {
				var newSize = (itemFont.Size * provider.GetTextEditorData ().Options.Zoom);
				if (newSize > 0) {
					itemFont.Size = (int)newSize;
					layout.FontDescription = itemFont;
				}
			}
		}

		public ListWidget (ListWindow win)
		{
			this.win = win;
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;
			DefaultCompletionString = "";
			categoryLayout = new Pango.Layout (this.PangoContext);
			noMatchLayout = new Pango.Layout (this.PangoContext);
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			var style = SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme);
			SetFont ();
			textColor = style.CompletionText.Foreground;

			highlightColor = style.CompletionHighlight.GetColor ("color");
			backgroundColor = style.CompletionText.Background;
			selectedItemColor = style.CompletionSelectedText;
			selectedItemInactiveColor = style.CompletionSelectedInactiveText;
			selectionBorderColor = style.CompletionBorder.GetColor ("color");
			selectionBorderInactiveColor = style.CompletionInactiveBorder.GetColor ("color");
		}

		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (layout != null) {
				layout.Dispose ();
				categoryLayout.Dispose ();
				noMatchLayout.Dispose ();
				layout = categoryLayout = noMatchLayout = null;
			}
			if (itemFont != null) {
				itemFont.Dispose ();
				itemFont = null;
			}
		}
		internal Adjustment vadj;

		protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
		{
			if (this.vadj != null)
				this.vadj.ValueChanged -= HandleValueChanged;
			this.vadj = vadj;
			base.OnSetScrollAdjustments (hadj, vadj);
			if (this.vadj != null) {
				this.vadj.ValueChanged += HandleValueChanged;
				SetAdjustments ();
			}
		}

		void HandleValueChanged (object sender, EventArgs e)
		{
			QueueDraw ();
			OnListScrolled (EventArgs.Empty);
		}

		public event EventHandler ListScrolled;

		protected virtual void OnListScrolled (EventArgs e)
		{
			EventHandler handler = this.ListScrolled;
			if (handler != null)
				handler (this, e);
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
					QueueDraw ();
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
			int next = Math.Min (categories.Count - 1, Math.Max (0, current + relative));
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
			double newValue;
			if (vadj.PageSize == 1.0) {
				newValue = Math.Min (vadj.Upper - vadj.PageSize, area.Y);
			} else if (area.Y < vadj.Value) {
				newValue = Math.Min (vadj.Upper - vadj.PageSize, area.Y);
			} else if (vadj.Value + vadj.PageSize < area.Bottom) {
				newValue = Math.Min (vadj.Upper - vadj.PageSize, area.Bottom - vadj.PageSize + 1);
			} else {
				return;
			}
			if (vadj.Upper <= vadj.PageSize) {
				vadj.Value = 0;
			} else {
				vadj.Value = Math.Min (vadj.Upper, Math.Max (vadj.Lower, newValue));
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
		
		public bool AutoCompleteEmptyMatchOnCurlyBrace {
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
			get { return MonoDevelop.Core.GettextCatalog.GetString ("No Completions Found"); }
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
				context.Rectangle (args.Area.X, args.Area.Y, args.Area.Width, args.Area.Height);
				context.Color = this.backgroundColor;
				context.Fill ();

				int xpos = iconTextSpacing;
				int yPos = (int)-vadj.Value;
				
				//when there are no matches, display a message to indicate that the completion list is still handling input
				if (filteredItems.Count == 0) {
					Gdk.GC gc = new Gdk.GC (window);
					gc.RgbFgColor = backgroundColor.ToGdkColor ();
					window.DrawRectangle (gc, true, 0, yPos, width, height - yPos);
					noMatchLayout.SetText (win.DataProvider.ItemCount == 0 ? NoSuggestionsMsg : NoMatchesMsg);
					int lWidth, lHeight;
					noMatchLayout.GetPixelSize (out lWidth, out lHeight);
					gc.RgbFgColor = (Mono.TextEditor.HslColor)textColor;
					window.DrawLayout (gc, (width - lWidth) / 2, yPos + (height - lHeight - yPos) / 2 - lHeight, noMatchLayout);
					gc.Dispose ();

					return false;
				}


				var textGCNormal = new Gdk.GC (window);
				textGCNormal.RgbFgColor = (Mono.TextEditor.HslColor)textColor;
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
				
					string text = win.DataProvider.GetText (item);
					
					if (!string.IsNullOrEmpty (text)) {
						int[] matchIndices = matcher.GetMatch (text);
						if (matchIndices != null) {
							Pango.AttrList attrList = layout.Attributes ?? new Pango.AttrList ();
							for (int newSelection = 0; newSelection < matchIndices.Length; newSelection++) {
								int idx = matchIndices [newSelection];
								var fg = new AttrForeground ((ushort)(highlightColor.R * ushort.MaxValue), (ushort)(highlightColor.G * ushort.MaxValue), (ushort)(highlightColor.B  * ushort.MaxValue));
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
						context.Rectangle (0, ypos, Allocation.Width, rowHeight / 2);
						context.Color = SelectionEnabled ? selectedItemColor.Foreground : selectedItemInactiveColor.Background;
						context.Fill ();
						context.Rectangle (0, ypos + rowHeight / 2, Allocation.Width, rowHeight / 2);
						context.Color = SelectionEnabled ? selectedItemColor.Background : selectedItemInactiveColor.Background;
						context.Fill ();

						context.Rectangle (0.5, ypos + 0.5, Allocation.Width - 1, rowHeight - 1);
						if (!SelectionEnabled)
							context.SetDash (new double[] {4, 4}, 0);
						context.Color = SelectionEnabled ? selectionBorderColor : selectionBorderInactiveColor;
						context.Stroke ();
					} 

					if (icon != null) {
						window.DrawPixbuf (fgGCNormal, icon, 0, 0, xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
						xpos += iconTextSpacing;
					}
					window.DrawLayout (textGCNormal, xpos + iconWidth + 2, typos, layout);

					if (wi + xpos + iconWidth + 2 > listWidth) {
						WidthRequest = listWidth = wi + xpos + iconWidth + 2 + iconTextSpacing;
						win.ResetSizes ();
					} else {
						//workaround for the vscrollbar display - the calculated width needs to be the width ofthe render region.
						if (Allocation.Width < listWidth) {
							if (listWidth - Allocation.Width < 30) {
								WidthRequest = listWidth + listWidth - Allocation.Width;
								win.ResetSizes ();
							}
						}
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
				return iconWidth + xSpacing + 5;
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
				var result = lt.CompareTo (rt);
				if (result == 0)
					return right.CompareTo (left);
				return result;
			});
			categories.Sort (delegate (Category left, Category right) {
				return left.CompletionCategory != null ? left.CompletionCategory.CompareTo (right.CompletionCategory) : -1;
			});
			
			SelectFirstItemInCategory ();
			CalcVisibleRows ();
			SetAdjustments ();

			OnWordsFiltered (EventArgs.Empty);
			oldCompletionString = CompletionString;
		}
		
		void SelectFirstItemInCategory ()
		{
			if (string.IsNullOrEmpty (CompletionString) && inCategoryMode)
				selection = categories.First ().Items.First ();
		}

		void SetAdjustments (bool scrollToSelectedItem = true)
		{
			if (vadj == null)
				return;
			int viewableCats = InCategoryMode ? categories.Count: 0;
			if (InCategoryMode && categories.Any (cat => cat.CompletionCategory == null))
				viewableCats--;

			var upper = Math.Max (Allocation.Height, (filteredItems.Count + viewableCats) * rowHeight);
			if (upper != vadj.Upper || Allocation.Height != vadj.PageSize) {
				vadj.SetBounds (0, upper, rowHeight, Allocation.Height, Allocation.Height);
				if (vadj.Upper <= Allocation.Height)
					vadj.Value = 0;
			}
			if (scrollToSelectedItem)
				ScrollToSelectedItem ();
		}
		
		protected virtual void OnWordsFiltered (EventArgs e)
		{
			SetAdjustments ();
			EventHandler handler = this.WordsFiltered;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler WordsFiltered;
		
		int GetRowByPosition (int ypos)
		{
			return GetItem (true, ((int)vadj.Value + ypos) / rowHeight);
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
			SetAdjustments (false);
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			if (rowHeight > 0)
				requisition.Height += requisition.Height % rowHeight;
		}

		const int maxVisibleRows = 7;
		void CalcVisibleRows ()
		{

			int rowWidth;
			layout.SetText ("F_B");
			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight = Math.Max (1, rowHeight * 3 / 2);

			int newHeight = rowHeight * maxVisibleRows;
			if (Allocation.Height != listWidth || Allocation.Width != newHeight)
				this.SetSizeRequest (listWidth, newHeight);
			SetAdjustments ();
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
