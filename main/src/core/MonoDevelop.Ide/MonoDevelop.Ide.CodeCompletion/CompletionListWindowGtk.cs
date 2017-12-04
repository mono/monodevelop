// CompletionListWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.Linq;
using MonoDevelop.Ide.Editor.Extension;
using System.ComponentModel;
using System.Threading;
using Xwt.Drawing;

namespace MonoDevelop.Ide.CodeCompletion
{
	class CompletionListWindowGtk : PopoverWindow, ICompletionView
	{
		const int declarationWindowMargin = 3;
		const int WindowWidth = 400;

		ICompletionViewEventSink eventSink;

		Gtk.Widget parsingMessage;
		int previousWidth = -1, previousHeight = -1;
		Entry previewEntry;

		ListWidget list;
		Widget footer;
		VBox vbox;

		ScrolledWindow scrollbar;

		public bool InCategoryMode {
			get => list.InCategoryMode;
			set => list.InCategoryMode = value;
		}

		public int X { get; private set; }
		public int Y { get; private set; }

		public CompletionListWindowGtk (WindowType type = WindowType.Popup) : base (type)
		{
			vbox = new VBox ();
			list = new ListWidget (this);
			list.ScrollEvent += OnScrolled;

			scrollbar = new MonoDevelop.Components.CompactScrolledWindow ();
			scrollbar.Name = "CompletionScrolledWindow"; // use a different gtkrc style for GtkScrollBar
			scrollbar.Child = list;
			list.ButtonPressEvent += OnButtonPressed;
			vbox.PackEnd (scrollbar, true, true, 0);
			var colorBox = new EventBox ();
			colorBox.Add (vbox);
			ContentBox.Add (colorBox);
			this.TypeHint = Gdk.WindowTypeHint.Menu;
			Theme.CornerRadius = 0;
			Theme.Padding = 0;

			UpdateStyle ();
			Gui.Styles.Changed += HandleThemeChanged;
			IdeApp.Preferences.ColorScheme.Changed += HandleThemeChanged;

			if (IdeApp.Workbench != null)
				this.TransientFor = IdeApp.Workbench.RootWindow;
			TypeHint = Gdk.WindowTypeHint.Combo;
			SizeAllocated += new SizeAllocatedHandler (ListSizeChanged);
			Events = Gdk.EventMask.PropertyChangeMask;
			WindowTransparencyDecorator.Attach (this);
			list.SelectionChanged += OnSelectionChanged;
		}

		void HandleThemeChanged (object sender, EventArgs e)
		{
			UpdateStyle ();
		}

		void UpdateStyle ()
		{
			Theme.SetBackgroundColor (Gui.Styles.CodeCompletion.BackgroundColor.ToCairoColor ());
			Theme.ShadowColor = Gui.Styles.PopoverWindow.ShadowColor.ToCairoColor ();
			ContentBox.Child.ModifyBg (StateType.Normal, Gui.Styles.CodeCompletion.BackgroundColor.ToGdkColor ());
			list.ModifyBg (StateType.Normal, Gui.Styles.CodeCompletion.BackgroundColor.ToGdkColor ());
		}

		public void Initialize (IListDataProvider provider, ICompletionViewEventSink eventSink)
		{
			list.DataProvider = provider;
			this.eventSink = eventSink;
		}

		public int SelectedItemIndex {
			get { return list.SelectedItemIndex; }
			set { list.SelectedItemIndex = value; }
		}

		public bool SelectionEnabled {
			get { return list.SelectionEnabled; }
			set { list.SelectionEnabled = value; }
		}

		public void ShowFilteredItems (CompletionListFilterResult filterResult)
		{
			list.ShowFilteredItems (filterResult);
		}

		public void ShowFooter (Widget w)
		{
			HideFooter ();
			vbox.PackStart (w, false, false, 0);
			footer = w;
		}

		public void HideFooter ()
		{
			if (footer != null) {
				vbox.Remove (footer);
				footer = null;
			}
		}

		/// <summary>
		/// This method is used to set the completion window to it's inital state.
		/// This is required for re-using the window object.
		/// </summary>
		protected internal virtual void ResetState ()
		{
			list.ResetState ();
		}

		protected int curXPos, curYPos;

		public void ResetSizes ()
		{
			var allocWidth = Allocation.Width;
			if (IsRealized && !Visible) {
				allocWidth = list.WidthRequest = WindowWidth;
			}

			int width = Math.Max (allocWidth, list.WidthRequest + Theme.CornerRadius * 2);
			int height = Math.Max (Allocation.Height, list.HeightRequest + 2 + (footer != null ? footer.Allocation.Height : 0) + Theme.CornerRadius * 2);
			GetSizeRequest (out var currentWidth, out var currentHeight);
			if (currentWidth != width || currentHeight != height) {
				SetSizeRequest (width, height);
				if (IsRealized) {
					Resize (width, height);
				}
			}
		}

		void ReleaseObjects ()
		{
			list.ResetState ();
		}

		protected override void OnDestroyed ()
		{
			list.SelectionChanged -= OnSelectionChanged;
			list.ScrollEvent -= OnScrolled;
			list.ButtonPressEvent -= OnButtonPressed;

			ReleaseObjects ();

			Gui.Styles.Changed -= HandleThemeChanged;
			IdeApp.Preferences.ColorScheme.Changed -= HandleThemeChanged;

			base.OnDestroyed ();
		}

		enum WindowPositonY
		{
			None,
			Top,
			Bottom
		}
		WindowPositonY yPosition;

		int currentTriggerX;
		int currentTriggerY;
		int currentTriggerHeight;

		public void Reposition (int triggerX, int triggerY, int triggerHeight, bool force)
		{
			currentTriggerX = triggerX;
			currentTriggerY = triggerY;
			currentTriggerHeight = triggerHeight;
			Reposition (force);
		}

		public void Reposition (bool force)
		{
			X = currentTriggerX - TextOffset;
			Y = currentTriggerY;

			int w, h;
			GetSize (out w, out h);

			if (!force && previousHeight != h && previousWidth != w)
				return;

			// Note: we add back the TextOffset here in case X and X+TextOffset are on different monitors.
			Xwt.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen.Number, Screen.GetMonitorAtPoint (X + TextOffset, Y));

			previousHeight = h;
			previousWidth = w;

			if (X + w > geometry.Right)
				X = (int)geometry.Right - w;
			else if (X < geometry.Left)
				X = (int)geometry.Left;

			if (Y + h > geometry.Bottom || yPosition == WindowPositonY.Top) {
				// Put the completion-list window *above* the cursor
				Y = Y - currentTriggerHeight - h;
				yPosition = WindowPositonY.Top;
			} else {
				yPosition = WindowPositonY.Bottom;
			}

			curXPos = X;
			curYPos = Y;
			Move (X, Y);
		}

		//smaller lists get size reallocated after FillList, so we have to reposition them
		//if the window size has changed since we last positioned it
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			Reposition (true);
			eventSink.OnAllocationChanged ();
		}

		void ListSizeChanged (object obj, SizeAllocatedArgs args)
		{
			eventSink.OnAllocationChanged ();
		}

		protected override void OnHidden ()
		{
			ReleaseObjects ();
			base.OnHidden ();
		}

		void ICompletionView.ResetState ()
		{
			previousWidth = previousHeight = -1;
			yPosition = WindowPositonY.None;
			list.ResetState ();
		}

		Xwt.Rectangle ICompletionView.Allocation {
			get {
				var r = Allocation;
				return new Xwt.Rectangle (r.Left, r.Top, r.Width, r.Height);
			}
		}

		public void MoveCursor (int relative)
		{
			list.MoveCursor (relative);
		}

		public void PageDown ()
		{
			scrollbar.Vadjustment.Value = Math.Max (0, Math.Min (scrollbar.Vadjustment.Upper - scrollbar.Vadjustment.PageSize, scrollbar.Vadjustment.Value + scrollbar.Vadjustment.PageSize));
			MoveCursor (8);
		}

		public void PageUp ()
		{
			scrollbar.Vadjustment.Value = Math.Max (0, scrollbar.Vadjustment.Value - scrollbar.Vadjustment.PageSize);
			MoveCursor (-8);
		}

		public void HideLoadingMessage ()
		{
			HideFooter ();
		}

		public void ShowLoadingMessage ()
		{
			if (parsingMessage == null) {
				var box = new Gtk.VBox ();
				box.PackStart (new Gtk.HSeparator (), false, false, 0);
				var hbox = new Gtk.HBox ();
				hbox.BorderWidth = 3;
				hbox.PackStart (new Components.ImageView ("md-parser", Gtk.IconSize.Menu), false, false, 0);
				var lab = new Gtk.Label (GettextCatalog.GetString ("Updating..."));
				lab.Xalign = 0;
				hbox.PackStart (lab, true, true, 3);
				hbox.ShowAll ();
				parsingMessage = hbox;
			}
			ShowFooter (parsingMessage);
		}

		void ICompletionView.Show ()
		{
			ShowAll ();
			DesktopService.RemoveWindowShadow (this);
		}

		public void ShowPreviewCompletionEntry ()
		{
			previewEntry = new Entry ();
			previewEntry.Changed += delegate (object sender, EventArgs e) {
				eventSink.OnPreviewCompletionEntryChanged (previewEntry.Text);
			};
			previewEntry.KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
				args.RetVal = !eventSink.OnPreProcessPreviewCompletionEntryKey (KeyDescriptor.FromGtk (args.Event.Key, (char)args.Event.KeyValue, args.Event.State));
			};
			vbox.PackStart (previewEntry, false, true, 0);

			previewEntry.Activated += (sender, e) => eventSink.OnPreviewCompletionEntryActivated ();
			previewEntry.Show ();
			FocusOutEvent += (o, args) => eventSink.OnPreviewCompletionEntryLostFocus ();
			GLib.Timeout.Add (10, delegate {
				previewEntry.GrabFocus ();
				return false;
			});
		}

		public bool RepositionDeclarationViewWindow (TooltipInformationWindow declarationviewwindow, int item)
		{
			if (base.GdkWindow == null)
				return false;

			Gdk.Rectangle rect = list.GetRowArea (item);
			if (rect.IsEmpty || rect.Bottom < (int)list.VAdjustment.Value || rect.Y > list.Allocation.Height + (int)list.VAdjustment.Value)
				return false;

			int ox;
			int oy;
			base.GdkWindow.GetOrigin (out ox, out oy);
			declarationviewwindow.MaximumYTopBound = oy;
			int y = rect.Y + Theme.Padding - (int)list.VAdjustment.Value;
			declarationviewwindow.ShowPopup (this, new Gdk.Rectangle (0, Math.Min (Allocation.Height, Math.Max (0, y)), Allocation.Width, rect.Height), PopupPosition.Left);
			return true;
		}

		public void RepositionWindow (Xwt.Rectangle? r)
		{
			Gdk.Rectangle? rect = r != null ? (Gdk.Rectangle?)new Gdk.Rectangle ((int)r.Value.Left, (int)r.Value.Top, (int)r.Value.Width, (int)r.Value.Height) : null;
			RepositionWindow (rect);
		}

		void OnSelectionChanged (object o, EventArgs args)
		{
			eventSink.OnSelectedItemChanged ();
		}

		void OnScrolled (object o, ScrollEventArgs args)
		{
			if (!scrollbar.Visible)
				return;

			var adj = scrollbar.Vadjustment;
			var alloc = Allocation;

			//This widget is a special case because it's always aligned to items as it scrolls.
			//Although this means we can't use the pixel deltas for true smooth scrolling, we 
			//can still make use of the effective scrolling velocity by basing the calculation 
			//on pixels and rounding to the nearest item.

			double dx, dy;
			args.Event.GetPageScrollPixelDeltas (0, alloc.Height, out dx, out dy);
			if (dy == 0)
				return;

			var itemDelta = dy / (alloc.Height / adj.PageSize);
			double discreteItemDelta = System.Math.Round (itemDelta);
			if (discreteItemDelta == 0.0 && dy != 0.0)
				discreteItemDelta = dy > 0 ? 1.0 : -1.0;

			adj.AddValueClamped (discreteItemDelta);
			args.RetVal = true;

			eventSink.OnListScrolled ();
		}

		void OnButtonPressed (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1 && args.Event.Type == Gdk.EventType.TwoButtonPress)
				eventSink.OnDoubleClick ();
		}

		public int TextOffset {
			get { return list.TextOffset + (int)Theme.CornerRadius; }
		}
	}
}
