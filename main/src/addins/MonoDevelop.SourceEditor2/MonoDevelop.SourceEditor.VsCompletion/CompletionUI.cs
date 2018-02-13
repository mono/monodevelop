//
// CompletionUI.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
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
using MonoDevelop.Components;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Editor;
using System.ComponentModel.Design;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Core;
using Xwt.Drawing;
using MonoDevelop.Ide.Gui;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor.VsCompletion
{
	class CompletionUI : Gtk.DrawingArea, ICompletionUI
	{
		const int minSize = 400;
		const int maxListWidth = 800;
		internal const int rows = 13;
		const int marginIconSpacing = 4;
		const int iconTextSpacing = 6;
		const int itemSeparatorHeight = 2;

		int listWidth = minSize;
		int rowHeight;

		Pango.Layout layout, categoryLayout, noMatchLayout;
		IMdTextView textView;
		FontDescription itemFont, noMatchFont;
		Adjustment vadj;
		XwtPopupWindowTheme Theme;

		int selection = 0;
		ISpaceReservationAgent agent;

		bool buttonPressed;
		CompletionPresentation presentationData;
		List<CompletionItemWithHighlight> filteredItems = new List<CompletionItemWithHighlight> (0);
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs e)
		{
			var handler = SelectionChanged;
			if (handler != null)
				handler (this, e);
		}

		void SetFont ()
		{
			// TODO: Add font property to ICompletionWidget;

			if (itemFont != null)
				itemFont.Dispose ();

			if (noMatchFont != null)
				noMatchFont.Dispose ();

			itemFont = FontService.MonospaceFont.Copy ();
			noMatchFont = FontService.SansFont.CopyModified (Styles.FontScale11);

			var newItemFontSize = itemFont.Size;
			var newNoMatchFontSize = noMatchFont.Size;

			if (newItemFontSize > 0) {
				itemFont.Size = (int)newItemFontSize;
				layout.FontDescription = itemFont;
			}

			if (newNoMatchFontSize > 0) {
				noMatchFont.Size = (int)newNoMatchFontSize;
				noMatchLayout.FontDescription = noMatchFont;
			}
		}

		ScrolledWindow scrollbar;
		EventBox box;
		public CompletionUI (IMdTextView textView)
		{
			var vbox = new VBox ();
			this.textView = textView;
			scrollbar = new MonoDevelop.Components.CompactScrolledWindow ();
			scrollbar.Name = "CompletionScrolledWindow"; // use a different gtkrc style for GtkScrollBar
			scrollbar.Child = this;
			vbox.PackEnd (scrollbar, true, true, 0);
			box = new EventBox ();
			box.Add (vbox);

			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;
			categoryLayout = new Pango.Layout (this.PangoContext);
			noMatchLayout = new Pango.Layout (this.PangoContext);
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;

			SetFont ();
			this.Show ();
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
			presentationData = default (CompletionPresentation);
			filteredItems.Clear ();
			selection = 0;
			listWidth = minSize;
		}

		public CompletionItem SelectedItem {
			get {
				return filteredItems [SelectedItemIndex].CompletionItem;
			}
		}

		public int SelectedItemIndex {
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
			Iterate (false, ref yPos, delegate (int item2, int itemIndex, int ypos) {
				if (item == item2) {
					result = curItem;
					return false;
				}
				curItem++;
				return true;
			});
			return result;
		}

		public void MoveCursor (int relative)
		{
			int newIndex = GetIndex (false, SelectedItemIndex) + relative;
			newIndex = Math.Min (filteredItems.Count - 1, Math.Max (0, newIndex));

			int newSelection = newIndex;
			if (newSelection < 0)
				return;

			if (SelectedItemIndex == newSelection && relative < 0) {
				SelectedItemIndex = 0;
			} else {
				SelectedItemIndex = newSelection;
			}
		}

		public void ScrollToSelectedItem ()
		{
			ScrollToItem (SelectedItemIndex);
		}

		public void ScrollToItem (int item)
		{
			var area = GetRowArea (item);
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

		bool selectionEnabled;
		public bool SelectionEnabled {
			get {
				return selectionEnabled;
			}
			set {
				selectionEnabled = value;
				QueueDraw ();
			}
		}

		protected override bool OnButtonPressEvent (EventButton e)
		{
			SelectedItemIndex = GetRowByPosition ((int)e.Y);
			buttonPressed = true;
			if (e.Button == 1 && e.Type == Gdk.EventType.TwoButtonPress) {
				CompletionItemCommitted?.Invoke (this, new CompletionItemCommittedEventArgs (SelectedItem));
				return true;
			} else {
				return base.OnButtonPressEvent (e);
			}
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
			SelectedItemIndex = GetRowByPosition ((int)e.Y);
			return true;
		}

		string NoMatchesMsg {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("No completions found"); }
		}

		string NoSuggestionsMsg {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("No suggestions"); }
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			using (var context = Gdk.CairoHelper.Create (args.Window)) {
				var scalef = GtkWorkarounds.GetScaleFactor (this);
				context.LineWidth = 1;
				var alloc = Allocation;
				int width = alloc.Width;
				int height = alloc.Height;
				context.Rectangle (args.Area.X, args.Area.Y, args.Area.Width, args.Area.Height);
				var backgroundColor = Styles.CodeCompletion.BackgroundColor.ToCairoColor ();
				var textColor = Styles.CodeCompletion.TextColor.ToCairoColor ();
				var categoryColor = Styles.CodeCompletion.CategoryColor.ToCairoColor ();
				context.SetSourceColor (backgroundColor);
				context.Fill ();
				int xpos = iconTextSpacing;
				int yPos = (int)-vadj.Value;
				//when there are no matches, display a message to indicate that the completion list is still handling input
				if (filteredItems.Count == 0) {
					context.Rectangle (0, yPos, width, height - yPos);
					context.SetSourceColor (backgroundColor);
					context.Stroke ();
					//TODO: David, line below is simplified noMatchLayout.SetText (DataProvider.ItemCount == 0 ? NoSuggestionsMsg : NoMatchesMsg);
					noMatchLayout.SetText (NoSuggestionsMsg);
					int lWidth, lHeight;
					noMatchLayout.GetPixelSize (out lWidth, out lHeight);
					context.SetSourceColor (textColor);
					context.MoveTo ((width - lWidth) / 2, yPos + (height - lHeight - yPos) / 2 - lHeight / 2);
					Pango.CairoHelper.ShowLayout (context, noMatchLayout);
					return false;
				}

				Iterate (true, ref yPos, delegate (int index, int itemidx, int ypos) {
					if (ypos >= height)
						return false;
					if (ypos < -rowHeight)
						return true;
					xpos = iconTextSpacing;
					var selected = index == SelectedItemIndex;
					bool drawIconAsSelected = SelectionEnabled && selected;
					var item = filteredItems [index];
					string markup = GLib.Markup.EscapeText (item.CompletionItem.DisplayText);
					string description = "";//TODO: David DataProvider.GetDescription (item, drawIconAsSelected);

					if (string.IsNullOrEmpty (description)) {
						layout.SetMarkup (markup);
					} else {
						layout.SetMarkup (markup + " " + description);
					}

					string text = item.CompletionItem.DisplayText;

					if (!string.IsNullOrEmpty (text) && item.HighlightedSpans.Any ()) {
						Pango.AttrList attrList = layout.Attributes ?? new Pango.AttrList ();
						foreach (var span in item.HighlightedSpans) {
							var bold = new AttrWeight (Weight.Bold);

							bold.StartIndex = (uint)span.Start;
							bold.EndIndex = (uint)span.End;
							attrList.Insert (bold);

							if (!selected) {
								var highlightColor = (selected) ? Styles.CodeCompletion.SelectionHighlightColor : Styles.CodeCompletion.HighlightColor;
								var fg = new AttrForeground ((ushort)(highlightColor.Red * ushort.MaxValue), (ushort)(highlightColor.Green * ushort.MaxValue), (ushort)(highlightColor.Blue * ushort.MaxValue));
								fg.StartIndex = (uint)span.Start;
								fg.EndIndex = (uint)span.End;
								attrList.Insert (fg);
							}
						}
						layout.Attributes = attrList;
					}


					Xwt.Drawing.Image icon = ImageService.GetImage (item.CompletionItem.Icon);
					int iconHeight, iconWidth;
					if (icon != null) {
						if (drawIconAsSelected)
							icon = icon.WithStyles ("sel");
						iconWidth = (int)icon.Width;
						iconHeight = (int)icon.Height;
					} else if (!Gtk.Icon.SizeLookup (IconSize.Menu, out iconWidth, out iconHeight)) {
						iconHeight = iconWidth = 24;
					}

					int wi, he, typos, iypos;
					layout.GetPixelSize (out wi, out he);


					typos = he < rowHeight ? ypos + (int)Math.Ceiling ((rowHeight - he) / 2.0) : ypos;
					if (scalef <= 1.0)
						typos -= 1; // 1px up on non HiDPI
					iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
					if (selected) {
						var barStyle = SelectionEnabled ? Styles.CodeCompletion.SelectionBackgroundColor : Styles.CodeCompletion.SelectionBackgroundInactiveColor;
						context.SetSourceColor (barStyle.ToCairoColor ());

						if (SelectionEnabled) {
							context.Rectangle (0, ypos, Allocation.Width, rowHeight);
							context.Fill ();
						} else {
							context.LineWidth++;
							context.Rectangle (0.5, ypos + 0.5, Allocation.Width - 1, rowHeight - 1);
							context.Stroke ();
							context.LineWidth--;
						}
					}

					if (icon != null) {
						context.DrawImage (this, icon, xpos, iypos);
						xpos += iconTextSpacing;
					}
					context.SetSourceColor ((drawIconAsSelected ? Styles.CodeCompletion.SelectionTextColor : Styles.CodeCompletion.TextColor).ToCairoColor ());
					var textXPos = xpos + iconWidth + 2;
					context.MoveTo (textXPos, typos);
					layout.Width = (int)((Allocation.Width - textXPos) * Pango.Scale.PangoScale);
					layout.Ellipsize = EllipsizeMode.End;
					Pango.CairoHelper.ShowLayout (context, layout);
					int textW, textH;
					layout.GetPixelSize (out textW, out textH);
					layout.Width = -1;
					layout.Ellipsize = EllipsizeMode.None;

					layout.SetMarkup ("");
					if (layout.Attributes != null) {
						layout.Attributes.Dispose ();
						layout.Attributes = null;
					}

					string rightText = "";//TODO: David DataProvider.GetRightSideDescription (index, drawIconAsSelected);
					if (!string.IsNullOrEmpty (rightText)) {
						layout.SetMarkup (rightText);

						int w, h;
						layout.GetPixelSize (out w, out h);
						const int leftpadding = 8;
						const int rightpadding = 3;
						w += rightpadding;
						w = Math.Min (w, Allocation.Width - textXPos - textW - leftpadding);
						wi += w;
						typos = h < rowHeight ? ypos + (rowHeight - h) / 2 : ypos;
						if (scalef <= 1.0)
							typos -= 1; // 1px up on non HiDPI
						context.MoveTo (Allocation.Width - w, typos);
						layout.Width = (int)(w * Pango.Scale.PangoScale);
						layout.Ellipsize = EllipsizeMode.End;

						Pango.CairoHelper.ShowLayout (context, layout);
						layout.Width = -1;
						layout.Ellipsize = EllipsizeMode.None;

					}

					if (Math.Min (maxListWidth, wi + xpos + iconWidth + 2) > listWidth) {
						box.WidthRequest = listWidth = Math.Min (maxListWidth, wi + xpos + iconWidth + 2 + iconTextSpacing);
						textView.QueueSpaceReservationStackRefresh ();
					} else {
						//workaround for the vscrollbar display - the calculated width needs to be the width ofthe render region.
						if (Allocation.Width < listWidth) {
							if (listWidth - Allocation.Width < 30) {
								box.WidthRequest = listWidth + listWidth - Allocation.Width;
								textView.QueueSpaceReservationStackRefresh ();
							}
						}
					}

					return true;
				});

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

		void SetAdjustments (bool scrollToSelectedItem = true)
		{
			if (vadj == null)
				return;

			var upper = Math.Max (Allocation.Height, (filteredItems.Count) * rowHeight);
			if (upper != vadj.Upper || Allocation.Height != vadj.PageSize) {
				vadj.SetBounds (0, upper, rowHeight, Allocation.Height, Allocation.Height);
				if (vadj.Upper <= Allocation.Height)
					vadj.Value = 0;
			}
			if (scrollToSelectedItem)
				ScrollToSelectedItem ();
		}

		int GetRowByPosition (int ypos)
		{
			return ((int)vadj.Value + ypos) / rowHeight;
		}

		public Gdk.Rectangle GetRowArea (int row)
		{
			int outpos = 0;
			int yPos = 0;
			Iterate (false, ref yPos, delegate (int item, int itemIndex, int ypos) {
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
			requisition.Width = listWidth;
			if (rowHeight > 0)
				requisition.Height += requisition.Height % rowHeight;
		}

		void CalcVisibleRows ()
		{
			var icon = ImageService.GetIcon (Ide.TypeSystem.Stock.Namespace, IconSize.Menu);
			rowHeight = Math.Max (1, (int)icon.Height + 2);

			int newHeight = rowHeight * rows;
			if (Allocation.Width != listWidth || Allocation.Height != newHeight)
				box.SetSizeRequest (listWidth, newHeight);
			SetAdjustments ();
		}

		const int spacing = 2;
		EditorTheme EditorTheme => SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme);

		delegate bool ItemAction (int item, int itemIndex, int yPos);

		void Iterate (bool startAtPage, ref int ypos, ItemAction action)
		{
			int curItem = 0;
			int startItem = 0;
			if (startAtPage)
				startItem = curItem = Math.Max (0, (int)(ypos / rowHeight));
			if (action != null) {
				for (int item = startItem; item < filteredItems.Count; item++) {
					bool result = action (item, curItem, ypos);
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


		public event EventHandler<CompletionFilterChangedEventArgs> FiltersChanged;
		public event EventHandler<CompletionItemCommittedEventArgs> CompletionItemCommitted;
		public event EventHandler<CompletionClosedEventArgs> CompletionClosed;

		public void Open (CompletionPresentation presentation)
		{
			Update (presentation);
			box.ShowAll ();
			var manager = textView.GetSpaceReservationManager ("completion");
			agent = manager.CreatePopupAgent (presentation.ApplicableSpan, Microsoft.VisualStudio.Text.Adornments.PopupStyles.None, Xwt.Toolkit.CurrentEngine.WrapWidget (box, Xwt.NativeWidgetSizing.DefaultPreferredSize));
			//HACK...
			Theme = ((Microsoft.VisualStudio.Text.Editor.Implementation.PopupAgent.PopUpContainer)((Microsoft.VisualStudio.Text.Editor.Implementation.PopupAgent)agent)._popup)._popup.Theme;
			Theme.CornerRadius = 0;
			Theme.Padding = 0;
			UpdateStyle ();
			Ide.Gui.Styles.Changed += HandleThemeChanged;
			IdeApp.Preferences.ColorScheme.Changed += HandleThemeChanged;

			manager.AddAgent (agent);
			textView.QueueSpaceReservationStackRefresh ();
		}

		public void Update (CompletionPresentation presentation)
		{
			presentationData = presentation;
			filteredItems = presentationData.Items.ToList ();
			selection = presentationData.SelectedItemIndex;
			CalcVisibleRows ();
			SetAdjustments ();
			QueueDraw ();
		}

		public void SetSelection (int selectedIndex)
		{
			throw new NotImplementedException ();//This will be removed in future and selection will be updated via Update method
		}

		public new void Hide ()
		{
			agent.Hide ();
		}

		public override void Dispose ()
		{
			var manager = textView.GetSpaceReservationManager ("completion");
			manager.RemoveAgent (agent);
			base.Dispose ();
		}

		void HandleThemeChanged (object sender, EventArgs e)
		{
			UpdateStyle ();
		}

		void UpdateStyle ()
		{
			Theme.SetBackgroundColor (Styles.CodeCompletion.BackgroundColor);
			Theme.ShadowColor = Styles.PopoverWindow.ShadowColor;
			box.ModifyBg (StateType.Normal, Styles.CodeCompletion.BackgroundColor.ToGdkColor ());
			this.ModifyBg (StateType.Normal, Styles.CodeCompletion.BackgroundColor.ToGdkColor ());
		}
	}
}
