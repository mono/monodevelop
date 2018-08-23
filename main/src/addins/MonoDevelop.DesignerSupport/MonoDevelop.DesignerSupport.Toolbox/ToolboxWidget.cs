//
// Toolbox.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gtk;
using Pango;
using Gdk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class ToolboxWidget : Gtk.DrawingArea
	{
		List<ToolboxWidgetCategory> categories = new List<ToolboxWidgetCategory> ();

		bool showCategories = true;
		bool listMode = false;
		int mouseX, mouseY;
		Pango.FontDescription desc;
		Xwt.Drawing.Image discloseDown;
		Xwt.Drawing.Image discloseUp;
		Gdk.Cursor handCursor;

		const int animationDurationMs = 600;

		public bool IsListMode {
			get {
				return listMode;
			}
			set {
				listMode = value;
				this.QueueResize ();
				this.ScrollToSelectedItem ();
			}
		}

		public bool CanIconizeToolboxCategories {
			get {
				foreach (ToolboxWidgetCategory category in categories) {
					if (category.CanIconizeItems)
						return true;
				}
				return false;
			}
		}

		public bool ShowCategories {
			get {
				return showCategories;
			}
			set {
				showCategories = value;
				this.QueueResize ();
				this.ScrollToSelectedItem ();
			}
		}

		public string CustomMessage { get; set; }

		internal void SetCustomFont (Pango.FontDescription desc)
		{
			this.desc = desc;
			if (layout != null)
				layout.FontDescription = desc;
			if (headerLayout != null)
				headerLayout.FontDescription = desc;
		}

		Pango.Layout layout;
		Pango.Layout headerLayout;
		Size iconSize = new Size (24, 24);

		public IEnumerable<ToolboxWidgetCategory> Categories {
			get { return categories; }
		}

		public IEnumerable<ToolboxWidgetItem> AllItems {
			get {
				foreach (ToolboxWidgetCategory category in this.categories) {
					foreach (ToolboxWidgetItem item in category.Items) {
						yield return item;
					}
				}
			}
		}

		public void ClearCategories ()
		{
			categories.Clear ();
			iconSize = new Gdk.Size (24, 24);
		}

		public void AddCategory (ToolboxWidgetCategory category)
		{
			categories.Add (category);
			foreach (ToolboxWidgetItem item in category.Items) {
				if (item.Icon == null)
					continue;

				this.iconSize.Width = Math.Max (this.iconSize.Width, (int)item.Icon.Width);
				this.iconSize.Height = Math.Max (this.iconSize.Height, (int)item.Icon.Height);
			}
		}

		public ToolboxWidget ()
		{
			this.Events = EventMask.ExposureMask |
						   EventMask.EnterNotifyMask |
						   EventMask.LeaveNotifyMask |
						   EventMask.ButtonPressMask |
						   EventMask.ButtonReleaseMask |
						   EventMask.KeyPressMask |
						   EventMask.PointerMotionMask;
			this.CanFocus = true;
			discloseDown = ImageService.GetIcon ("md-disclose-arrow-down", Gtk.IconSize.Menu);
			discloseUp = ImageService.GetIcon ("md-disclose-arrow-up", Gtk.IconSize.Menu);
			handCursor = new Cursor (CursorType.Hand1);

			var actionHandler = new ActionDelegate (this);
			actionHandler.PerformShowMenu += PerformShowMenu;
		}

		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			if (this.layout != null) {
				this.layout.Dispose ();
				this.layout = null;
			}
			if (this.headerLayout != null) {
				this.headerLayout.Dispose ();
				this.headerLayout = null;
			}

			base.OnStyleSet (previous_style);

			layout = new Pango.Layout (this.PangoContext);
			headerLayout = new Pango.Layout (this.PangoContext);

			if (desc != null) {
				layout.FontDescription = desc;
				headerLayout.FontDescription = desc;
			}

			layout.Ellipsize = EllipsizeMode.End;

			headerLayout.Attributes = new AttrList ();
			//			headerLayout.Attributes.Insert (new Pango.AttrWeight (Pango.Weight.Bold));
		}


		protected override void OnDestroyed ()
		{
			HideTooltipWindow ();
			if (this.layout != null) {
				this.layout.Dispose ();
				this.layout = null;
			}
			if (this.headerLayout != null) {
				this.headerLayout.Dispose ();
				this.headerLayout = null;
			}
			base.OnDestroyed ();
			handCursor.Dispose ();
		}

		static Cairo.Color Convert (Gdk.Color color)
		{
			return new Cairo.Color (color.Red / (double)ushort.MaxValue, color.Green / (double)ushort.MaxValue, color.Blue / (double)ushort.MaxValue);
		}

		const int CategoryLeftPadding = 6;
		const int CategoryRightPadding = 4;
		const int CategoryTopBottomPadding = 6;
		const int ItemTopBottomPadding = 3;
		const int ItemLeftPadding = 4;
		const int ItemIconTextItemSpacing = 4;
		const int IconModePadding = 2;

		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			Cairo.Context cr = Gdk.CairoHelper.Create (e.Window);

			Gdk.Rectangle area = e.Area;

			if (this.categories.Count == 0 || !string.IsNullOrEmpty (CustomMessage)) {
				Pango.Layout messageLayout = new Pango.Layout (this.PangoContext);
				messageLayout.Alignment = Pango.Alignment.Center;
				messageLayout.Width = (int)(Allocation.Width * 2 / 3 * Pango.Scale.PangoScale);
				if (!string.IsNullOrEmpty (CustomMessage))
					messageLayout.SetText (CustomMessage);
				else
					messageLayout.SetText (MonoDevelop.Core.GettextCatalog.GetString ("There are no tools available for the current document."));
				cr.MoveTo (Allocation.Width * 1 / 6, 12);
				cr.SetSourceColor (Style.Text (StateType.Normal).ToCairoColor ());
				Pango.CairoHelper.ShowLayout (cr, messageLayout);
				messageLayout.Dispose ();
				((IDisposable)cr).Dispose ();
				return true;
			}

			var backColor = Style.Base (StateType.Normal).ToCairoColor ();
			cr.SetSourceColor (backColor);
			cr.Rectangle (area.X, area.Y, area.Width, area.Height);
			cr.Fill ();

			int xpos = (this.hAdjustement != null ? (int)this.hAdjustement.Value : 0);
			int vadjustment = (this.vAdjustement != null ? (int)this.vAdjustement.Value : 0);
			int ypos = -vadjustment;
			ToolboxWidgetCategory lastCategory = null;
			int lastCategoryYpos = 0;

			cr.LineWidth = 1;

			Iterate (ref xpos, ref ypos, (category, itemDimension) => {
				ProcessExpandAnimation (cr, lastCategory, lastCategoryYpos, backColor, area, ref ypos);

				if (!area.IntersectsWith (new Gdk.Rectangle (new Gdk.Point (xpos, ypos), itemDimension)))
					return true;
				cr.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
				cr.SetSourceColor (Ide.Gui.Styles.PadCategoryBackgroundColor.ToCairoColor ());
				cr.Fill ();

				if (lastCategory == null || lastCategory.IsExpanded || lastCategory.AnimatingExpand) {
					cr.MoveTo (xpos, ypos + 0.5);
					cr.LineTo (itemDimension.Width, ypos + 0.5);
				}
				cr.MoveTo (0, ypos + itemDimension.Height - 0.5);
				cr.LineTo (xpos + Allocation.Width, ypos + itemDimension.Height - 0.5);
				cr.SetSourceColor (MonoDevelop.Ide.Gui.Styles.PadCategoryBorderColor.ToCairoColor ());
				cr.Stroke ();

				headerLayout.SetMarkup (category.Text);
				int width, height;
				cr.SetSourceColor (MonoDevelop.Ide.Gui.Styles.PadCategoryLabelColor.ToCairoColor ());
				headerLayout.GetPixelSize (out width, out height);
				cr.MoveTo (xpos + CategoryLeftPadding, ypos + (double)(Math.Round ((double)(itemDimension.Height - height) / 2)));
				Pango.CairoHelper.ShowLayout (cr, headerLayout);

				var img = category.IsExpanded ? discloseUp : discloseDown;
				cr.DrawImage (this, img, Allocation.Width - img.Width - CategoryRightPadding, ypos + Math.Round ((itemDimension.Height - img.Height) / 2));

				lastCategory = category;
				lastCategoryYpos = ypos + itemDimension.Height;

				return true;
			}, (curCategory, item, itemDimension) => {
				if (!area.IntersectsWith (new Gdk.Rectangle (new Gdk.Point (xpos, ypos), itemDimension)))
					return true;

				var icon = item.Icon;
				if (!icon.HasFixedSize) {
					var maxIconSize = Math.Min (itemDimension.Width, itemDimension.Height);
					var fittingIconSize = maxIconSize > 32 ? Xwt.IconSize.Large : maxIconSize > 16 ? Xwt.IconSize.Medium : Xwt.IconSize.Small;
					icon = item.Icon.WithSize (fittingIconSize);
				}
				if (item == SelectedItem) {
					icon = icon.WithStyles ("sel");
					cr.SetSourceColor (Style.Base (StateType.Selected).ToCairoColor ());
					cr.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					cr.Fill ();
				}
				if (listMode || !curCategory.CanIconizeItems) {
					cr.DrawImage (this, icon, xpos + ItemLeftPadding, ypos + Math.Round ((itemDimension.Height - icon.Height) / 2));
					layout.SetMarkup (item.Text);
					layout.Width = (int)((itemDimension.Width - ItemIconTextItemSpacing - iconSize.Width - ItemLeftPadding * 2) * Pango.Scale.PangoScale);
					layout.GetPixelSize (out var width, out var height);
					cr.SetSourceColor (Style.Text (item != SelectedItem ? StateType.Normal : StateType.Selected).ToCairoColor ());
					cr.MoveTo (xpos + ItemLeftPadding + iconSize.Width + ItemIconTextItemSpacing, ypos + Math.Round ((double)(itemDimension.Height - height) / 2));
					Pango.CairoHelper.ShowLayout (cr, layout);
				} else {
					cr.DrawImage (this, icon, xpos + Math.Round ((itemDimension.Width - icon.Width) / 2), ypos + Math.Round ((itemDimension.Height - icon.Height) / 2));
				}

				if (item == mouseOverItem) {
					cr.SetSourceColor (Style.Dark (StateType.Prelight).ToCairoColor ());
					cr.Rectangle (xpos + 0.5, ypos + 0.5, itemDimension.Width - 1, itemDimension.Height - 1);
					cr.Stroke ();
				}

				return true;
			});

			ProcessExpandAnimation (cr, lastCategory, lastCategoryYpos, backColor, area, ref ypos);

			if (lastCategory != null && lastCategory.AnimatingExpand) {
				// Closing line when animating the last group of the toolbox
				cr.MoveTo (area.X, ypos + 0.5);
				cr.RelLineTo (area.Width, 0);
				cr.SetSourceColor (MonoDevelop.Ide.Gui.Styles.PadCategoryBorderColor.ToCairoColor ());
				cr.Stroke ();
			}

			((IDisposable)cr).Dispose ();
			return true;
		}

		void ProcessExpandAnimation (Cairo.Context cr, ToolboxWidgetCategory lastCategory, int lastCategoryYpos, Cairo.Color backColor, Gdk.Rectangle area, ref int ypos)
		{
			if (lastCategory != null && lastCategory.AnimatingExpand) {
				int newypos;
				if (lastCategory.IsExpanded) {
					newypos = lastCategoryYpos + (int)(lastCategory.AnimationPosition * (ypos - lastCategoryYpos));
				} else {
					newypos = ypos - (int)(lastCategory.AnimationPosition * (ypos - lastCategoryYpos));
				}

				// Clear the area where the category will be drawn since it will be
				// drawn over the items being hidden/shown
				cr.SetSourceColor (backColor);
				cr.Rectangle (area.X, newypos, area.Width, ypos - lastCategoryYpos);
				cr.Fill ();
				ypos = newypos;
			}
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			ToolboxWidgetItem nextItem;

			// Handle keyboard toolip popup
			if ((evnt.Key == Gdk.Key.F1 && (evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)) {
				if (this.SelectedItem != null) {
					int vadjustment = (this.vAdjustement != null ? (int)this.vAdjustement.Value : 0);
					Gdk.Rectangle rect = GetItemExtends (SelectedItem);
					ShowTooltip (SelectedItem, 0, rect.X, rect.Bottom - vadjustment);
				}
				return true;
			}

			switch (evnt.Key) {
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				if (this.SelectedItem != null)
					this.OnActivateSelectedItem (EventArgs.Empty);
				return true;
			case Gdk.Key.KP_Up:
			case Gdk.Key.Up:
				if (this.listMode || this.SelectedItem is ToolboxWidgetCategory) {
					this.SelectedItem = GetPrevItem (this.SelectedItem);
				} else {
					nextItem = GetItemAbove (this.SelectedItem);
					this.SelectedItem = nextItem != this.SelectedItem ? nextItem : GetCategory (this.SelectedItem);
				}
				this.QueueDraw ();
				return true;
			case Gdk.Key.KP_Down:
			case Gdk.Key.Down:
				if (this.listMode || this.SelectedItem is ToolboxWidgetCategory) {
					this.SelectedItem = GetNextItem (this.SelectedItem);
				} else {
					nextItem = GetItemBelow (this.SelectedItem);
					if (nextItem == this.SelectedItem) {
						ToolboxWidgetCategory category = GetCategory (this.SelectedItem);
						nextItem = GetNextCategory (category);
						if (nextItem == category)
							nextItem = this.SelectedItem;
					}
					this.SelectedItem = nextItem;
				}
				this.QueueDraw ();
				return true;

			case Gdk.Key.KP_Left:
			case Gdk.Key.Left:
				if (this.SelectedItem is ToolboxWidgetCategory) {
					SetCategoryExpanded ((ToolboxWidgetCategory)this.SelectedItem, false);
				} else {
					if (this.listMode) {
						this.SelectedItem = GetCategory (this.SelectedItem);
					} else {
						this.SelectedItem = GetItemLeft (this.SelectedItem);
					}
				}
				this.QueueDraw ();
				return true;

			case Gdk.Key.KP_Right:
			case Gdk.Key.Right:
				if (this.SelectedItem is ToolboxWidgetCategory selectedCategory) {
					if (selectedCategory.IsExpanded) {
						if (selectedCategory.ItemCount > 0)
							this.SelectedItem = selectedCategory.Items [0];
					} else {
						SetCategoryExpanded (selectedCategory, true);
					}
				} else {
					if (this.listMode) {
						// nothing
					} else {
						this.SelectedItem = GetItemRight (this.SelectedItem);
					}
				}
				this.QueueDraw ();
				return true;

			}
			return false;
		}

		protected override void OnUnrealized ()
		{
			HideTooltipWindow ();
			base.OnUnrealized ();
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			if (evnt.Mode == CrossingMode.Normal) {
				HideTooltipWindow ();
				ClearMouseOverItem ();
			}
			GdkWindow.Cursor = null;
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			HideTooltipWindow ();
			ClearMouseOverItem ();
			return base.OnScrollEvent (evnt);
		}

		public Action<Gdk.EventButton> DoPopupMenu { get; set; }

		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			this.GrabFocus ();
			HideTooltipWindow ();
			if (this.mouseOverItem is ToolboxWidgetCategory) {
				if (!e.TriggersContextMenu () && e.Button == 1 && e.Type == EventType.ButtonPress) {
					ToolboxWidgetCategory mouseOverCateogry = (ToolboxWidgetCategory)this.mouseOverItem;
					SetCategoryExpanded (mouseOverCateogry, !mouseOverCateogry.IsExpanded);
					return true;
				}
				this.SelectedItem = mouseOverItem;
				this.QueueResize ();
			} else {
				this.SelectedItem = mouseOverItem;
				this.QueueDraw ();
			}
			if (e.TriggersContextMenu ()) {
				if (DoPopupMenu != null) {
					DoPopupMenu (e);
					return true;
				}
			} else if (e.Type == EventType.TwoButtonPress && this.SelectedItem != null) {
				this.OnActivateSelectedItem (EventArgs.Empty);
				return true;
			}
			return base.OnButtonPressEvent (e);
		}

		void PerformShowMenu (object sender, EventArgs args)
		{
			DoPopupMenu?.Invoke (null);
		}

		void SetCategoryExpanded (ToolboxWidgetCategory cat, bool expanded)
		{
			if (cat.IsExpanded == expanded)
				return;
			cat.IsExpanded = expanded;
			StartExpandAnimation (cat);
		}

		Xwt.Motion.Tweener tweener;

		void StartExpandAnimation (ToolboxWidgetCategory cat)
		{
			if (tweener != null) {
				tweener.Stop ();
			}

			cat.AnimatingExpand = true;
			cat.AnimationPosition = 0.0f;

			tweener = new Xwt.Motion.Tweener (animationDurationMs, 10) { Easing = Xwt.Motion.Easing.SinOut };
			tweener.ValueUpdated += (sender, e) => {
				cat.AnimationPosition = tweener.Value;
				QueueDraw ();
			};
			tweener.Finished += (sender, e) => {
				cat.AnimatingExpand = false;
				QueueDraw ();
			};
			tweener.Start ();
			QueueDraw ();
		}

		protected override bool OnPopupMenu ()
		{
			if (DoPopupMenu != null) {
				DoPopupMenu (null);
				return true;
			}
			return base.OnPopupMenu ();
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			int xpos = 0;
			int ypos = 0;
			HideTooltipWindow ();
			var oldItem = mouseOverItem;
			mouseOverItem = null;
			Gdk.Rectangle newItemExtents = Gdk.Rectangle.Zero;
			this.mouseX = (int)e.X + (int)(this.hAdjustement != null ? this.hAdjustement.Value : 0);
			this.mouseY = (int)e.Y + (int)(this.vAdjustement != null ? this.vAdjustement.Value : 0);
			Iterate (ref xpos, ref ypos, (category, itemDimension) => {
				if (xpos <= mouseX && mouseX <= xpos + itemDimension.Width &&
					ypos <= mouseY && mouseY <= ypos + itemDimension.Height) {
					mouseOverItem = category;
					GdkWindow.Cursor = handCursor;
					if (!e.State.HasFlag (ModifierType.Button1Mask))
						ShowTooltip (mouseOverItem, TipTimer, (int)e.X + 2, (int)e.Y + 16);
					newItemExtents = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					return false;
				}
				return true;
			}, (curCategory, item, itemDimension) => {
				if (xpos <= mouseX && mouseX <= xpos + itemDimension.Width &&
					ypos <= mouseY && mouseY <= ypos + itemDimension.Height) {
					mouseOverItem = item;
					GdkWindow.Cursor = null;
					if (!e.State.HasFlag (ModifierType.Button1Mask))
						ShowTooltip (mouseOverItem, TipTimer, (int)e.X + 2, (int)e.Y + 16);
					newItemExtents = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					return false;
				}
				return true;
			});

			if (mouseOverItem == null)
				GdkWindow.Cursor = null;

			if (oldItem != mouseOverItem) {
				this.QueueDraw ();
				var oldItemExtents = GetItemExtends (oldItem);
				QueueDrawArea (oldItemExtents.X, oldItemExtents.Y, oldItemExtents.Width, oldItemExtents.Height);
				QueueDrawArea (newItemExtents.X, newItemExtents.Y, newItemExtents.Width, newItemExtents.Height);
			}

			return base.OnMotionNotifyEvent (e);
		}

		#region Item selection logic
		ToolboxWidgetItem selectedItem = null;
		ToolboxWidgetItem mouseOverItem = null;

		public ToolboxWidgetItem SelectedItem {
			get {
				return selectedItem;
			}
			set {
				if (selectedItem != value) {
					selectedItem = value;
					ScrollToSelectedItem ();
					OnSelectedItemChanged (EventArgs.Empty);
				}
			}
		}

		public event EventHandler SelectedItemChanged;
		protected virtual void OnSelectedItemChanged (EventArgs args)
		{
			HideTooltipWindow ();
			if (SelectedItemChanged != null)
				SelectedItemChanged (this, args);
		}

		public event EventHandler ActivateSelectedItem;
		protected virtual void OnActivateSelectedItem (EventArgs args)
		{
			if (ActivateSelectedItem != null)
				ActivateSelectedItem (this, args);
		}

		void ClearMouseOverItem ()
		{
			if (mouseOverItem != null) {
				mouseOverItem = null;
			}
			HideTooltipWindow ();
			this.QueueDraw ();
		}

		ToolboxWidgetCategory GetCategory (ToolboxWidgetItem item)
		{
			ToolboxWidgetCategory result = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, (curCategory, innerItem, itemDimension) => {
				if (innerItem == item) {
					result = curCategory;
					return false;
				}
				return true;
			});
			return result;
		}

		ToolboxWidgetCategory GetNextCategory (ToolboxWidgetCategory category)
		{
			ToolboxWidgetCategory result = category;
			ToolboxWidgetCategory last = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, (curCategory, itemDimension) => {
				if (last == category) {
					result = curCategory;
					return false;
				}
				last = curCategory;
				return true;
			}, null);
			return result;
		}

		ToolboxWidgetItem GetItemRight (ToolboxWidgetItem item)
		{
			ToolboxWidgetItem result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, (curCategory, curItem, itemDimension) => {
				if (xpos > rect.X && ypos == rect.Y && result == item) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}

		ToolboxWidgetItem GetItemLeft (ToolboxWidgetItem item)
		{
			ToolboxWidgetItem result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, (curCategory, curItem, itemDimension) => {
				if (xpos < rect.X && ypos == rect.Y) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}

		ToolboxWidgetItem GetItemBelow (ToolboxWidgetItem item)
		{
			ToolboxWidgetCategory itemCategory = GetCategory (item);

			ToolboxWidgetItem result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, (curCategory, curItem, itemDimension) => {
				if (ypos > rect.Y && xpos == rect.X && result == item && curCategory == itemCategory) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}

		ToolboxWidgetItem GetItemAbove (ToolboxWidgetItem item)
		{
			ToolboxWidgetCategory itemCategory = GetCategory (item);
			ToolboxWidgetItem result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, (curCategory, curItem, itemDimension) => {
				if (ypos < rect.Y && xpos == rect.X && curCategory == itemCategory) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}

		Gdk.Rectangle GetItemExtends (ToolboxWidgetItem item)
		{
			var result = new Gdk.Rectangle (0, 0, 0, 0);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, (category, itemDimension) => {
				if (item == category) {
					result = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					return false;
				}
				return true;
			}, (curCategory, curItem, itemDimension) => {
				if (item == curItem) {
					result = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					return false;
				}
				return true;
			});
			return result;
		}

		ToolboxWidgetItem GetPrevItem (ToolboxWidgetItem currentItem)
		{
			ToolboxWidgetItem result = currentItem;
			ToolboxWidgetItem lastItem = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, (category, itemDimension) => {
				if (currentItem == category && lastItem != null) {
					result = lastItem;
					return false;
				}
				lastItem = category;
				return true;
			}, (curCategory, item, itemDimension) => {
				if (currentItem == item && lastItem != null) {
					result = lastItem;
					return false;
				}
				lastItem = item;
				return true;
			});

			return result;
		}

		ToolboxWidgetItem GetNextItem (ToolboxWidgetItem currentItem)
		{
			ToolboxWidgetItem result = currentItem;
			ToolboxWidgetItem lastItem = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, (category, itemDimension) => {
				if (lastItem == currentItem) {
					result = category;
					return false;
				}
				lastItem = category;
				return true;
			}, (curCategory, item, itemDimension) => {
				if (lastItem == currentItem) {
					result = item;
					return false;
				}
				lastItem = item;
				return true;
			});
			return result;
		}
		#endregion

		#region Scrolling
		Adjustment hAdjustement = null;
		Adjustment vAdjustement = null;

		public void ScrollToSelectedItem ()
		{
			if (this.SelectedItem == null || this.vAdjustement == null)
				return;
			Gdk.Rectangle rect = GetItemExtends (this.SelectedItem);
			if (this.vAdjustement.Value > rect.Top)
				this.vAdjustement.Value = rect.Top;
			if (this.vAdjustement.Value + this.Allocation.Height < rect.Bottom)
				this.vAdjustement.Value = rect.Bottom - this.Allocation.Height;
		}

		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			this.hAdjustement = hAdjustement;
			if (this.hAdjustement != null) {
				this.hAdjustement.ValueChanged += (sender, e) => this.QueueDraw ();
			}
			this.vAdjustement = vAdjustement;
			if (this.vAdjustement != null) {
				this.vAdjustement.ValueChanged += (sender, e) => this.QueueDraw ();
			}
		}
		#endregion

		#region Item & Category iteration
		bool IterateItems (ToolboxWidgetCategory category, ref int xpos, ref int ypos, Func<ToolboxWidgetCategory, ToolboxWidgetItem, Size, bool> action)
		{
			if (listMode || !category.CanIconizeItems) {
				foreach (ToolboxWidgetItem item in category.Items) {
					if (!item.IsVisible)
						continue;

					int x, y = item.ItemHeight;

					if (y == 0) {
						layout.SetMarkup (item.Text);
						layout.GetPixelSize (out x, out y);
						y = Math.Max (iconSize.Height, y);
						y += ItemTopBottomPadding * 2;
						item.ItemHeight = y;
					}

					xpos = 0;
					if (action != null && !action (category, item, new Gdk.Size (Allocation.Width, y)))
						return false;

					ypos += y;
				}
				return true;
			}
			foreach (ToolboxWidgetItem item in category.Items) {
				if (!item.IsVisible)
					continue;
				if (xpos + iconSize.Width >= this.Allocation.Width) {
					xpos = 0;
					ypos += iconSize.Height;
				}
				if (action != null && !action (category, item, iconSize))
					return false;
				xpos += iconSize.Width;
			}
			ypos += iconSize.Height;
			return true;
		}

		void Iterate (ref int xpos, ref int ypos, Func<ToolboxWidgetCategory, Size, bool> catAction, Func<ToolboxWidgetCategory, ToolboxWidgetItem, Size, bool> action)
		{
			foreach (ToolboxWidgetCategory category in this.categories) {
				if (!category.IsVisible)
					continue;
				xpos = 0;
				if (this.showCategories) {
					int x, y = category.ItemHeight;

					if (y == 0) {
						layout.SetMarkup (category.Text);
						layout.GetPixelSize (out x, out y);
						y += CategoryTopBottomPadding * 2;
						category.ItemHeight = y;
					}

					if (catAction != null && !catAction (category, new Size (this.Allocation.Width, y)))
						return;
					ypos += y;
				}
				if (category.IsExpanded || category.AnimatingExpand || !this.showCategories) {
					if (!IterateItems (category, ref xpos, ref ypos, action))
						return;
				}
			}
		}
		#endregion

		#region Control size management
		bool realSizeRequest;
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (!realSizeRequest) {
				// Request a minimal width, to size recalculation infinite loops with
				// small widths, due to the vscrollbar being shown and hidden.
				requisition.Width = 50;
				requisition.Height = 0;
				return;
			}
			int xpos = 0;
			int ypos = 0;
			Iterate (ref xpos, ref ypos, null, null);
			requisition.Width = 50;
			requisition.Height = ypos;
			if (this.vAdjustement != null) {
				this.vAdjustement.SetBounds (0,
											 ypos,
											 20,
											 Allocation.Height,
											 Allocation.Height);
				if (ypos < Allocation.Height)
					this.vAdjustement.Value = 0;
				if (vAdjustement.Value + vAdjustement.PageSize > vAdjustement.Upper)
					vAdjustement.Value = vAdjustement.Upper - vAdjustement.PageSize;
				if (vAdjustement.Value < 0)
					vAdjustement.Value = 0;
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (!realSizeRequest) {
				realSizeRequest = true;
				QueueResize ();
			} else
				realSizeRequest = false;
		}

		#endregion

		#region Tooltips
		const int TipTimer = 800;
		Gtk.Window tooltipWindow = null;
		ToolboxWidgetItem tipItem;
		int tipX, tipY;
		uint tipTimeoutId;

		public void HideTooltipWindow ()
		{
			if (tipTimeoutId != 0) {
				GLib.Source.Remove (tipTimeoutId);
				tipTimeoutId = 0;
			}
			if (tooltipWindow != null) {
				tooltipWindow.Destroy ();
				tooltipWindow = null;
			}
		}

		bool ShowTooltip ()
		{
			HideTooltipWindow ();

			if (tipItem.Node is ICustomTooltipToolboxNode custom) {
				tooltipWindow = custom.CreateTooltipWindow (this);
			} else {
				tooltipWindow = new CustomTooltipWindow  {
					Tooltip = tipItem.Tooltip,
					ParentWindow = this.GdkWindow
				};
			}

			//translate mouse position into screen coords
			GdkWindow.GetOrigin (out var ox, out var oy);
			int x = tipX + ox + Allocation.X;
			int y = tipY + oy + Allocation.Y;

			//get window size. might not work for all tooltips.
			var req = tooltipWindow.SizeRequest ();
			int w = req.Width;
			int h = req.Height;

			var geometry = Screen.GetUsableMonitorGeometry (Screen.GetMonitorAtPoint (ox + x, oy + y));

			//if hits right edge, shift in
			if (x + w > geometry.Right)
				x = geometry.Right - w;

			//if hits bottom, flip above mouse
			if (y + h > geometry.Bottom)
				y = y - h - 20;

			int destX = Math.Max (0, x);
			int destY = Math.Max (0, y);

			tooltipWindow.Move (destX, destY);
			tooltipWindow.ShowAll ();

			return false;
		}

		public void ShowTooltip (ToolboxWidgetItem item, uint timer, int x, int y)
		{
			HideTooltipWindow ();

			if (!string.IsNullOrEmpty (item.Tooltip) || (item.Node is ICustomTooltipToolboxNode custom && custom.HasTooltip)) {
				tipItem = item;
				tipX = x;
				tipY = y;
				tipTimeoutId = GLib.Timeout.Add (timer, ShowTooltip);
			}
		}

		class CustomTooltipWindow : MonoDevelop.Components.TooltipWindow
		{
			string tooltip;
			public string Tooltip {
				get {
					return tooltip;
				}
				set {
					tooltip = value;
					label.Text = tooltip;
				}
			}

			Label label = new Label ();
			public CustomTooltipWindow ()
			{
				label.Xalign = 0;
				label.Xpad = 3;
				label.Ypad = 3;
				Add (label);
			}
		}
		#endregion
	}

	class ToolboxWidgetCategory : ToolboxWidgetItem
	{
		public bool IsExpanded { get; set; }

		public bool AnimatingExpand { get; set; }

		List<ToolboxWidgetItem> items = new List<ToolboxWidgetItem> ();
		internal double AnimationPosition;

		public int ItemCount => items.Count;

		public ReadOnlyCollection<ToolboxWidgetItem> Items {
			get {
				return items.AsReadOnly ();
			}
		}
		public bool CanIconizeItems { get; set; } = true;
		public bool IsDropTarget { get; set; } = false;
		public bool IsSorted { get; set; } = true;
		public int Priority { get; set; }

		public ToolboxWidgetCategory (string text) : base (text)
		{
		}

		public void Clear ()
		{
			this.items.Clear ();
		}

		public void Add (ToolboxWidgetItem item)
		{
			this.items.Add (item);
			if (IsSorted)
				items.Sort ();
		}

		public void Remove (ToolboxWidgetItem item)
		{
			this.items.Remove (item);
			if (IsSorted)
				items.Sort ();
		}

		public override string ToString ()
		{
			return String.Format ("[Category: Text={0}]", Text);
		}
	}

	class ToolboxWidgetItem : IComparable<ToolboxWidgetItem>
	{
		static Xwt.Drawing.Image defaultIcon;
		readonly Xwt.Drawing.Image icon;
		readonly string text;
		readonly string tooltip;
		readonly object tag;

		public string Tooltip {
			get {
				if (Node != null)
					return string.IsNullOrEmpty (Node.Description) ? Node.Name : Node.Description;
				return tooltip;
			}
		}

		public Xwt.Drawing.Image Icon {
			get {
				return Node?.Icon ?? icon ?? DefaultIcon;
			}
		}

		static Xwt.Drawing.Image DefaultIcon {
			get {
				if (defaultIcon == null)
					defaultIcon = ImageService.GetIcon (Stock.MissingImage, IconSize.Menu);
				return defaultIcon;
			}
		}

		public string Text {
			get {
				if (Node != null) {
					var t = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (Node.Name);
					if (!string.IsNullOrEmpty (Node.Source)) {
						var c = MonoDevelop.Ide.Gui.Styles.SecondaryTextColorHexString;
						t = string.Format ("{2} <span size=\"smaller\" color=\"{1}\">{0}</span>", Node.Source, c, t);
					}
					return t;
				}
				return text;
			}
		}

		public int ItemHeight {
			get;
			set;
		}

		public bool IsVisible { get; set; } = true;

		public object Tag => Node ?? tag;

		public ItemToolboxNode Node { get; private set; }

		public ToolboxWidgetItem (ItemToolboxNode node)
		{
			Node = node;
		}

		public ToolboxWidgetItem (string text) : this (null, text, null)
		{
		}

		public ToolboxWidgetItem (Xwt.Drawing.Image icon, string text) : this (icon, text, null)
		{
		}

		public ToolboxWidgetItem (Xwt.Drawing.Image icon, string text, string tooltip) : this (icon, text, tooltip, null)
		{
		}

		public ToolboxWidgetItem (Xwt.Drawing.Image icon, string text, string tooltip, object tag)
		{
			this.icon = icon;
			this.text = Ide.TypeSystem.Ambience.EscapeText (text);
			this.tooltip = tooltip;
			this.tag = tag;
		}

		public int CompareTo (ToolboxWidgetItem other)
		{
			if (other == null)
				return -1;
			return Text.CompareTo (other.Text);
		}
	}

	[Obsolete("This class should never have been public")]
	public class Item : IComparable<Item>
	{
		ToolboxWidgetItem inner;

		public string Tooltip => inner.Tooltip;
		public Xwt.Drawing.Image Icon => inner.Icon;

		public string Text => inner.Text;

		public int ItemHeight {
			get => inner.ItemHeight;
			set => inner.ItemHeight = value;
		}

		public bool IsVisible {
			get => inner.IsVisible;
			set => inner.IsVisible = value;
		}

		public object Tag => inner.Tag;

		public Item (ItemToolboxNode node) => inner = new ToolboxWidgetItem (node);
		public Item (string text) : this (null, text, null) {}
		public Item (Xwt.Drawing.Image icon, string text) : this (icon, text, null) {}
		public Item (Xwt.Drawing.Image icon, string text, string tooltip) : this (icon, text, tooltip, null) {}
		public Item (Xwt.Drawing.Image icon, string text, string tooltip, object tag) => inner = new ToolboxWidgetItem (icon, text, tooltip, tag);

		public virtual int CompareTo (Item other) => other == null ? -1 : other.inner.CompareTo (other.inner);
	}
}
