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
		List<Category> categories = new List<Category> ();
		
		bool showCategories = true;
		bool listMode       = false;
		int mouseX, mouseY;
		Pango.FontDescription desc;
		Xwt.Drawing.Image discloseDown;
		Xwt.Drawing.Image discloseUp;
		Gdk.Cursor handCursor;
		
		const uint animationTimeSpan = 10;
		const int animationStepSize = 35;

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
				foreach (Category category in categories) {
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
		
		Gdk.Size iconSize = new Gdk.Size (24, 24);
		Gdk.Size IconSize {
			get {
				return iconSize;
			}
		}
		
		public IEnumerable<Category> Categories {
			get { return categories; }
		}
		
		public IEnumerable<Item> AllItems {
			get {
				foreach (Category category in this.categories) {
					foreach (Item item in category.Items) {
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
		
		public void AddCategory (Category category)
		{
			categories.Add (category);
			foreach (Item item in category.Items) {
				if (item.Icon == null)
					continue;

				this.iconSize.Width  = Math.Max (this.iconSize.Width,  (int)item.Icon.Width);
				this.iconSize.Height  = Math.Max (this.iconSize.Height,  (int)item.Icon.Height);
			}
		}
		
		public ToolboxWidget ()
		{
			this.Events =  EventMask.ExposureMask | 
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
			return new Cairo.Color (color.Red / (double)ushort.MaxValue, color.Green / (double)ushort.MaxValue,  color.Blue / (double)ushort.MaxValue);
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
			Category lastCategory = null;
			int lastCategoryYpos = 0;

			cr.LineWidth = 1;

			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				const int foldSegmentHeight = 8;

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
				layout.GetPixelSize (out width, out height);
				cr.MoveTo (xpos + CategoryLeftPadding, ypos + (double)(Math.Round ((double)(itemDimension.Height - height) / 2)));
				Pango.CairoHelper.ShowLayout (cr, headerLayout);

				var img = category.IsExpanded ? discloseUp : discloseDown;
				cr.DrawImage (this, img, Allocation.Width - img.Width - CategoryRightPadding, ypos + Math.Round ((itemDimension.Height - img.Height) / 2));

				lastCategory = category;
				lastCategoryYpos = ypos + itemDimension.Height;

				return true;
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
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
				if (listMode || !curCategory.CanIconizeItems)  {
					cr.DrawImage (this, icon, xpos + ItemLeftPadding, ypos + Math.Round ((itemDimension.Height - icon.Height) / 2));
					layout.SetMarkup (item.Text);
					int width, height;
					layout.GetPixelSize (out width, out height);
					cr.SetSourceColor (Style.Text (item != this.SelectedItem ? StateType.Normal : StateType.Selected).ToCairoColor ());
					cr.MoveTo (xpos + ItemLeftPadding + IconSize.Width + ItemIconTextItemSpacing, ypos + (double)(Math.Round ((double)(itemDimension.Height - height) / 2)));
					Pango.CairoHelper.ShowLayout (cr, layout);
				} else {
					cr.DrawImage (this, icon, xpos + Math.Round ((itemDimension.Width  - icon.Width) / 2), ypos + Math.Round ((itemDimension.Height - icon.Height) / 2));
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

		void ProcessExpandAnimation (Cairo.Context cr, Category lastCategory, int lastCategoryYpos, Cairo.Color backColor, Gdk.Rectangle area, ref int ypos)
		{
			if (lastCategory != null && lastCategory.AnimatingExpand) {
				int newypos = lastCategory.IsExpanded ? lastCategoryYpos + lastCategory.AnimationHeight : ypos + lastCategory.AnimationHeight;
				if (newypos < lastCategoryYpos) {
					newypos = lastCategoryYpos;
					StopExpandAnimation (lastCategory);
				}
				if (newypos > ypos) {
					newypos = ypos;
					StopExpandAnimation (lastCategory);
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
			Item nextItem;
			
			// Handle keyboard toolip popup
			if ((evnt.Key == Gdk.Key.F1 && (evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)) {
				if (this.SelectedItem != null) {
					int vadjustment = (this.vAdjustement != null ? (int)this.vAdjustement.Value : 0);
					Gdk.Rectangle rect = GetItemExtends (SelectedItem);
					ShowTooltip (SelectedItem, 0,rect.X, rect.Bottom - vadjustment );
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
				if (this.listMode || this.SelectedItem is Category) {
					this.SelectedItem = GetPrevItem (this.SelectedItem);
				} else {
					nextItem = GetItemAbove (this.SelectedItem);
					this.SelectedItem = nextItem != this.SelectedItem ? nextItem : GetCategory (this.SelectedItem);
				}
				this.QueueDraw ();
				return true;
			case Gdk.Key.KP_Down:
			case Gdk.Key.Down:
				if (this.listMode || this.SelectedItem is Category) {
					this.SelectedItem = GetNextItem (this.SelectedItem);
				} else {
					nextItem = GetItemBelow (this.SelectedItem);
					if (nextItem == this.SelectedItem) {
						Category category = GetCategory (this.SelectedItem);
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
				if (this.SelectedItem is Category) {
					SetCategoryExpanded ((Category)this.SelectedItem, false);
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
				if (this.SelectedItem is Category) {
					Category selectedCategory = ((Category)this.SelectedItem);
					if (selectedCategory.IsExpanded) {
						if (selectedCategory.ItemCount > 0)
							this.SelectedItem = selectedCategory.Items[0];
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
			if (this.mouseOverItem is Category) {
				if (!e.TriggersContextMenu () && e.Button == 1 && e.Type == EventType.ButtonPress) {
					Category mouseOverCateogry = (Category)this.mouseOverItem;
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

		void SetCategoryExpanded (Category cat, bool expanded)
		{
			if (cat.IsExpanded == expanded)
				return;
			cat.IsExpanded = expanded;
			if (cat.IsExpanded)
				StartExpandAnimation (cat);
			else
				StartCollapseAnimation (cat);
		}

		void StartExpandAnimation (Category cat)
		{
			if (cat.AnimatingExpand)
				GLib.Source.Remove (cat.AnimationHandle);

			cat.AnimationHeight = 0;
			cat.AnimatingExpand = true;
			cat.AnimationHandle = GLib.Timeout.Add (animationTimeSpan, delegate {
				cat.AnimationHeight += animationStepSize;
				QueueResize ();
				return true;
			});
		}

		void StartCollapseAnimation (Category cat)
		{
			if (cat.AnimatingExpand)
				GLib.Source.Remove (cat.AnimationHandle);

			cat.AnimationHeight = 0;
			cat.AnimatingExpand = true;
			cat.AnimationHandle = GLib.Timeout.Add (animationTimeSpan, delegate {
				cat.AnimationHeight -= animationStepSize;
				QueueResize ();
				return true;
			});
		}

		void StopExpandAnimation (Category cat)
		{
			if (cat.AnimatingExpand) {
				cat.AnimatingExpand = false;
				GLib.Source.Remove (cat.AnimationHandle);
			}
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
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (xpos <= mouseX && mouseX <= xpos + itemDimension.Width  &&
				    ypos <= mouseY && mouseY <= ypos + itemDimension.Height) {
					mouseOverItem = category;
					GdkWindow.Cursor = handCursor;
					if (!e.State.HasFlag (ModifierType.Button1Mask))
						ShowTooltip (mouseOverItem, TipTimer, (int)e.X + 2, (int)e.Y + 16);
					newItemExtents = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					return false;
				}
				return true;
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
				if (xpos <= mouseX && mouseX <= xpos + itemDimension.Width  &&
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
		Item selectedItem  = null;
		Item mouseOverItem = null;
		
		public Item SelectedItem {
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
		
		Category GetCategory (Item item)
		{
			Category result = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, delegate (Category curCategory, Item innerItem, Gdk.Size itemDimension) {
				if (innerItem == item) {
					result = curCategory;
					return false;
				}
				return true;
			});
			return result;
		}
		
		Category GetNextCategory (Category category)
		{
			Category result = category;
			Category last = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, delegate (Category curCategory, Gdk.Size itemDimension) {
				if (last == category) {
					result = curCategory;
					return false;
				}
				last = curCategory;
				return true;
			}, null);
			return result;
		}
		
		Item GetItemRight (Item item)
		{
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (xpos > rect.X && ypos == rect.Y && result == item) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}
		
		Item GetItemLeft (Item item)
		{
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (xpos < rect.X && ypos == rect.Y) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}
		
		Item GetItemBelow (Item item)
		{
			Category itemCategory = GetCategory (item);
						
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (ypos > rect.Y && xpos == rect.X && result == item && curCategory == itemCategory) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}
		
		Item GetItemAbove (Item item)
		{
			Category itemCategory = GetCategory (item);
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, null, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (ypos < rect.Y && xpos == rect.X && curCategory == itemCategory) {
					result = curItem;
					return false;
				}
				return true;
			});
			return result;
		}
		
		Gdk.Rectangle GetItemExtends (Item item)
		{
			Gdk.Rectangle result = new Gdk.Rectangle (0, 0, 0, 0);
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (item == category) {
					result = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					return false;
				}
				return true;
			}, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (item == curItem) {
					result = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
					return false;
				}
				return true;
			});
			return result;
		}
		
		Item GetPrevItem (Item currentItem)
		{
			Item result = currentItem;
			Item lastItem = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (currentItem == category && lastItem != null) {
					result = lastItem;
					return false;
				}
				lastItem = category;
				return true;
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
				if (currentItem == item && lastItem != null) {
					result = lastItem;
					return false;
				}
				lastItem = item;
				return true;
			});
			
			return result;
		}
		
		Item GetNextItem (Item currentItem)
		{
			Item result = currentItem;
			Item lastItem = null;
			int xpos = 0, ypos = 0;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (lastItem == currentItem) {
					result = category;
					return false;
				}
				lastItem = category;
				return true;
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
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
				this.hAdjustement.ValueChanged += delegate {
					this.QueueDraw ();
				};
			}
			this.vAdjustement = vAdjustement;
			if (this.vAdjustement != null) {
				this.vAdjustement.ValueChanged += delegate {
					this.QueueDraw ();
				};
			}
		}
		#endregion
		
		#region Item & Category iteration
		delegate bool CategoryAction (Category category, Gdk.Size categoryDimension);
		delegate bool ItemAction (Category curCategory, Item item, Gdk.Size itemDimension);
		bool IterateItems (Category category, ref int xpos, ref int ypos, ItemAction action)
		{
			if (listMode || !category.CanIconizeItems) {
				foreach (Item item in category.Items) {
					if (!item.IsVisible)
						continue;

					int x, y = item.ItemHeight;

					if (y == 0) {
						layout.SetMarkup (item.Text);
						layout.GetPixelSize (out x, out y);
						y = Math.Max (IconSize.Height, y);
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
			foreach (Item item in category.Items) {
				if (!item.IsVisible)
					continue;
				if (xpos + IconSize.Width >= this.Allocation.Width) {
					xpos = 0;
					ypos += IconSize.Height;
				}
				if (action != null && !action (category, item, IconSize))
					return false;
				xpos += IconSize.Width;
			}
			ypos += IconSize.Height;
			return true;
		}
		
		void Iterate (ref int xpos, ref int ypos, CategoryAction catAction, ItemAction action)
		{
			foreach (Category category in this.categories) {
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
					if (!IterateItems (category, ref xpos, ref  ypos, action))
						return;
				}
			}
		}
		#endregion
		
		#region Control size management
		bool realSizeRequest;
		protected override void OnSizeRequested (ref Requisition req)
		{
			if (!realSizeRequest) {
				// Request a minimal width, to size recalculation infinite loops with
				// small widths, due to the vscrollbar being shown and hidden.
				req.Width = 50;
				req.Height = 0;
				return;
			}
			int xpos = 0;
			int ypos = 0;
			Iterate (ref xpos, ref ypos, null, null);
			req.Width  = 50; 
			req.Height = ypos;
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
			}
			else
				realSizeRequest = false;
		}
		
		#endregion
		
		#region Tooltips
		const int TipTimer = 800;
		CustomTooltipWindow tooltipWindow = null;
		Item tipItem;
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
			tooltipWindow = new CustomTooltipWindow ();
			tooltipWindow.Tooltip = tipItem.Tooltip;
			tooltipWindow.ParentWindow = this.GdkWindow;
			int ox, oy;
			this.GdkWindow.GetOrigin (out ox, out oy);
			tooltipWindow.Move (Math.Max (0, ox + tipX), oy + tipY);
			tooltipWindow.ShowAll ();
			return false;
		}
		
		public void ShowTooltip (Item item, uint timer, int x, int y)
		{
			HideTooltipWindow ();
			if (!String.IsNullOrEmpty (item.Tooltip)) {
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
					label.Markup = tooltip;
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
	
	class Category : Item
	{
		bool isExpanded;
		
		public bool IsExpanded {
			get {
				return isExpanded;
			}
			set {
				isExpanded = value;
			}
		}

		public bool AnimatingExpand { get; set; }
		public uint AnimationHandle;
		public int AnimationHeight;

		List<Item> items = new List<Item> ();
		
		public int ItemCount {
			get {
				return items.Count;
			}
		}
		
		public ReadOnlyCollection<Item> Items {
			get {
				return items.AsReadOnly ();
			}
		}
		
		bool canIconizeItems = true;
		public bool CanIconizeItems {
			get {
				return canIconizeItems;
			}
			set {
				canIconizeItems = value;
			}
		}
		
		bool isDropTarget    = false;
		public bool IsDropTarget {
			get {
				return isDropTarget;
			}
			set {
				isDropTarget = value;
			}
		}
		
		bool isSorted    = true;
		public bool IsSorted {
			get {
				return isSorted;
			}
			set {
				isSorted = value;
			}
		}
		
		public int Priority {
			get; set;
		}
		
		public Category (string text) : base (text)
		{
		}
		
		public void Clear ()
		{
			this.items.Clear ();
		}
		
		public void Add (Item item)
		{
			this.items.Add (item);
			if (isSorted)
				items.Sort ();
		}
		
		public void Remove (Item item)
		{
			this.items.Remove (item);
			if (isSorted)
				items.Sort ();
		}
		
		public override string ToString ()
		{
			return String.Format ("[Category: Text={0}]", Text);
		}
	}
	
	public class Item : IComparable<Item>
	{
		static Xwt.Drawing.Image defaultIcon;
		Xwt.Drawing.Image icon;
		string     text;
		string     tooltip;
		object     tag;
		bool       isVisible = true;
		
		public string Tooltip {
			get {
				if (node != null)
					return string.IsNullOrEmpty (node.Description) ? node.Name : node.Description;
				return tooltip;
			}
		}
		
		public Xwt.Drawing.Image Icon {
			get {
				return node?.Icon ?? icon ?? DefaultIcon;
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
				if (node != null) {
					var t = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (node.Name);
					if (!string.IsNullOrEmpty (node.Source)) {
						var c = MonoDevelop.Ide.Gui.Styles.SecondaryTextColorHexString;
						t = string.Format ("{2} <span size=\"smaller\" color=\"{1}\">{0}</span>", node.Source, c, t);
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
		
		public bool IsVisible {
			get {
				return this.isVisible;
			}
			set {
				this.isVisible = value;
			}
		}
		
		public object Tag {
			get {
				return node ?? tag;
			}
		}

		ItemToolboxNode node;
		public Item (ItemToolboxNode node)
		{
			this.node = node;
		}
		

		public Item (string text) : this (null, text, null)
		{
		}
		
		public Item (Xwt.Drawing.Image icon, string text) : this (icon, text, null)
		{
		}
		
		public Item (Xwt.Drawing.Image icon, string text, string tooltip) : this (icon, text, tooltip, null)
		{
		}
		
		public Item (Xwt.Drawing.Image icon, string text, string tooltip, object tag)
		{
			this.icon    = icon;
			this.text    = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (text);
			this.tooltip = tooltip;
			this.tag     = tag;
		}
		
		public virtual int CompareTo (Item other)
		{
			if (other == null) 
				return -1;
			return Text.CompareTo (other.Text);
		}
		
	}

}
