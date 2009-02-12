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

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class ToolboxWidget : Gtk.DrawingArea
	{
		List<Category> categories = new List<Category> ();
		
		bool showCategories = true;
		bool listMode       = false;
		int mouseX, mouseY;
		Pango.FontDescription desc;
		
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
		
		internal void SetCustomFont (Pango.FontDescription desc)
		{
			this.desc = desc;
			if (layout != null)
				layout.FontDescription = desc;
			if (headerLayout != null)
				headerLayout.FontDescription = desc;
		}
		
		const int spacing = 4;
		const int categoryHeaderSize = 20;
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
		
		public Gdk.Pixbuf GetIcon (string name, Gtk.IconSize size)
		{
			Gtk.IconSet iconset = Gtk.IconFactory.LookupDefault (name);
			if (iconset != null) {
				return iconset.RenderIcon (Gtk.Widget.DefaultStyle, Gtk.TextDirection.None, Gtk.StateType.Normal, size, null, null);
			}
			return null;
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
				this.iconSize.Width  = Math.Max (this.iconSize.Width,  item.Icon.Width + spacing);
				this.iconSize.Height  = Math.Max (this.iconSize.Height,  item.Icon.Height + spacing);
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
			headerLayout.Attributes.Insert (new Pango.AttrWeight (Pango.Weight.Bold));
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
		}
		
		static Cairo.Color Convert (Gdk.Color color)
		{
			return new Cairo.Color (color.Red / (double)ushort.MaxValue, color.Green / (double)ushort.MaxValue,  color.Blue / (double)ushort.MaxValue);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			Gdk.Drawable  draw = e.Window;
			Gdk.Rectangle area = e.Area;
			if (this.categories.Count == 0) {
				Pango.Layout messageLayout = new Pango.Layout (this.PangoContext);
				messageLayout.Alignment = Pango.Alignment.Center;
				messageLayout.Width = (int)(Allocation.Width * 2 / 3 * Pango.Scale.PangoScale);
				messageLayout.SetText (MonoDevelop.Core.GettextCatalog.GetString ("There are no tools available for the current document."));
				draw.DrawLayout (Style.TextGC (StateType.Normal), Allocation.Width * 1 / 6 , 12, messageLayout);
				messageLayout.Dispose ();
				return true;
			}
			
			Cairo.Context cr = Gdk.CairoHelper.Create (e.Window);
			draw.DrawRectangle (Style.BaseGC (StateType.Normal), true, area);
			int xpos = spacing - (this.hAdjustement != null ? (int)this.hAdjustement.Value : 0);
			int vadjustment = (this.vAdjustement != null ? (int)this.vAdjustement.Value : 0);
			int ypos = spacing - vadjustment;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				const int foldSegmentHeight = 8;
				
				if (category == SelectedItem) {
					draw.DrawRectangle (Style.BaseGC (StateType.Selected), 
					                   true, 
					                   new Gdk.Rectangle (xpos,
					                                      ypos,
					                                      itemDimension.Width, 
					                                      itemDimension.Height));
				} else {
					cr.NewPath ();
					cr.MoveTo (xpos, ypos);
					cr.RelLineTo (itemDimension.Width, 0);
					cr.RelLineTo (0, itemDimension.Height);
					cr.RelLineTo (-itemDimension.Width, 0);
					cr.RelLineTo (0, -itemDimension.Height);
					cr.ClosePath ();
					Cairo.Gradient pat = new Cairo.LinearGradient (xpos, ypos, xpos, ypos + itemDimension.Height);
					pat.AddColorStop (0, Convert (Style.Mid (StateType.Normal)));
					pat.AddColorStop (1, Convert (Style.Base (StateType.Normal)));
					cr.Pattern = pat;
					cr.FillPreserve ();
					
				}
				
				DrawFoldSegment (draw, xpos + 2, ypos + (itemDimension.Height - foldSegmentHeight) / 2, foldSegmentHeight, foldSegmentHeight, category.IsExpanded, category == mouseOverItem && mouseX < xpos + 2 + 12);
				headerLayout.SetText (category.Text);
				int width, height;
				layout.GetPixelSize (out width, out height);
				draw.DrawLayout (Style.TextGC (category != this.SelectedItem ? StateType.Normal : StateType.Selected), xpos + 2 + 12, ypos + (itemDimension.Height - height) / 2, headerLayout);
				if (category == mouseOverItem) {
					draw.DrawRectangle (Style.DarkGC (StateType.Prelight), 
					                   false, 
					                   new Gdk.Rectangle (xpos, 
					                                      ypos, 
					                                      itemDimension.Width - 1, 
					                                      itemDimension.Height - 1));
				}
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
				if (item == SelectedItem) {
					draw.DrawRectangle (Style.BaseGC (StateType.Selected), 
					                   true, 
					                   new Gdk.Rectangle (xpos, 
					                                      ypos, 
					                                      itemDimension.Width, 
					                                      itemDimension.Height));
				}
				if (listMode || !curCategory.CanIconizeItems)  {
					draw.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), 
					                item.Icon, 0, 0, 
					                xpos + spacing, 
					                ypos + 1 + (itemDimension.Height - item.Icon.Height) / 2, 
					                item.Icon.Width, item.Icon.Height, Gdk.RgbDither.None, 0, 0);
					layout.SetText (item.Text);
					int width, height;
					layout.GetPixelSize (out width, out height);
					draw.DrawLayout (Style.TextGC (item != this.SelectedItem ? StateType.Normal : StateType.Selected), xpos + IconSize.Width, ypos + (itemDimension.Height - height) / 2, layout);
				} else {
					draw.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), 
					                item.Icon, 0, 0, 
					                xpos + (itemDimension.Width  - item.Icon.Width) / 2, 
					                ypos + (itemDimension.Height - item.Icon.Height) / 2, 
					                item.Icon.Width, item.Icon.Height, Gdk.RgbDither.None, 0, 0);
				}
					
				if (item == mouseOverItem) {
					draw.DrawRectangle (Style.DarkGC (StateType.Prelight), 
					                   false, 
					                   new Gdk.Rectangle (xpos, 
					                                      ypos , 
					                                      itemDimension.Width, 
					                                      itemDimension.Height));
				}
			});
			((IDisposable)cr).Dispose ();
			return true;
		}
		
		// From Mono.TextEditor.FoldMarkerMargin
		void DrawFoldSegment (Gdk.Drawable win, int x, int y, int w, int h, bool isOpen, bool selected)
		{
			Gdk.Rectangle drawArea = new Gdk.Rectangle (x, y, w, h);
			
			win.DrawRectangle (selected ? Style.ForegroundGC (StateType.Normal) : Style.BaseGC (StateType.Normal), true, drawArea);
			win.DrawRectangle (selected ? Style.ForegroundGC (StateType.Normal) : Style.DarkGC (StateType.Normal), false, drawArea);
			
			win.DrawLine (selected ? Style.BaseGC (StateType.Normal) : Style.ForegroundGC (StateType.Normal), 
			              drawArea.Left  + drawArea.Width * 3 / 10,
			              drawArea.Top + drawArea.Height / 2,
			              drawArea.Right - drawArea.Width * 3 / 10,
			              drawArea.Top + drawArea.Height / 2);
			
			if (!isOpen)
				win.DrawLine (selected ? Style.BaseGC (StateType.Normal) : Style.ForegroundGC (StateType.Normal), 
				              drawArea.Left + drawArea.Width / 2,
				              drawArea.Top + drawArea.Height * 3 / 10,
				              drawArea.Left  + drawArea.Width / 2,
				              drawArea.Bottom - drawArea.Height * 3 / 10);
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
					((Category)this.SelectedItem).IsExpanded = false;
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
						selectedCategory.IsExpanded = true;
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
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)		
		{
			if (evnt.Mode == CrossingMode.Normal) {
				HideTooltipWindow ();
				ClearMouseOverItem ();
			}
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			HideTooltipWindow ();
			ClearMouseOverItem ();
			return base.OnScrollEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			this.GrabFocus ();
			HideTooltipWindow ();
			if (this.mouseOverItem is Category) {
				if (e.Button == 1) {
					Category mouseOverCateogry = (Category)this.mouseOverItem;
					mouseOverCateogry.IsExpanded = !mouseOverCateogry.IsExpanded;
				}
				this.SelectedItem = mouseOverItem;
				this.QueueResize ();
			} else {
				this.SelectedItem = mouseOverItem;
				this.QueueDraw ();
			}
			if (e.Type == EventType.TwoButtonPress && this.SelectedItem != null) 
				this.OnActivateSelectedItem (EventArgs.Empty);
			return base.OnButtonPressEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			int xpos = spacing;
			int ypos = spacing;
			HideTooltipWindow ();
			mouseOverItem = null;
			this.mouseX = (int)e.X + (int)(this.hAdjustement != null ? this.hAdjustement.Value : 0);
			this.mouseY = (int)e.Y + (int)(this.vAdjustement != null ? this.vAdjustement.Value : 0);
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (xpos <= mouseX && mouseX <= xpos + itemDimension.Width + spacing  &&
				    ypos <= mouseY && mouseY <= ypos + itemDimension.Height + spacing) {
					mouseOverItem = category;
					ShowTooltip (mouseOverItem, TipTimer, (int)e.X + 2, (int)e.Y + 16);
				}
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
				if (xpos <= mouseX && mouseX <= xpos + itemDimension.Width + spacing  &&
				    ypos <= mouseY && mouseY <= ypos + itemDimension.Height + spacing) {
					mouseOverItem = item;
					ShowTooltip (mouseOverItem, TipTimer, (int)e.X + 2, (int)e.Y + 16);
				}
			});
			
			this.QueueDraw ();
			
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
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
			}, delegate (Category curCategory, Item innerItem, Gdk.Size itemDimension) {
				if (innerItem == item) 
					result = curCategory;
			});
			return result;
		}
		
		Category GetNextCategory (Category category)
		{
			Category result = category;
			Category last = null;
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category curCategory, Gdk.Size itemDimension) {
				if (last == category) 
					result = curCategory;
				last = curCategory;
			}, delegate (Category curCategory, Item innerItem, Gdk.Size itemDimension) {
			});
			return result;
		}
		
		Item GetItemRight (Item item)
		{
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
			}, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (xpos > rect.X && ypos == rect.Y && result == item)
					result = curItem;
			});
			return result;
		}
		
		Item GetItemLeft (Item item)
		{
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
			}, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (xpos < rect.X && ypos == rect.Y)
					result = curItem;
			});
			return result;
		}
		
		Item GetItemBelow (Item item)
		{
			Category itemCategory = GetCategory (item);
						
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
			}, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (ypos > rect.Y && xpos == rect.X && result == item && curCategory == itemCategory)
					result = curItem;
			});
			return result;
		}
		
		Item GetItemAbove (Item item)
		{
			Category itemCategory = GetCategory (item);
			Item result = item;
			Gdk.Rectangle rect = GetItemExtends (item);
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
			}, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (ypos < rect.Y && xpos == rect.X && curCategory == itemCategory)
					result = curItem;
			});
			return result;
		}
		
		Gdk.Rectangle GetItemExtends (Item item)
		{
			Gdk.Rectangle result = new Gdk.Rectangle (0, 0, 0, 0);
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (item == category)
					result = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
			}, delegate (Category curCategory, Item curItem, Gdk.Size itemDimension) {
				if (item == curItem)
					result = new Gdk.Rectangle (xpos, ypos, itemDimension.Width, itemDimension.Height);
			});
			return result;
		}
		
		Item GetPrevItem (Item currentItem)
		{
			Item result = currentItem;
			Item lastItem = null;
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (currentItem == category && lastItem != null)
					result = lastItem;
				lastItem = category;
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
				if (currentItem == item && lastItem != null) 
					result = lastItem;
				lastItem = item;
			});
			
			return result;
		}
		
		Item GetNextItem (Item currentItem)
		{
			Item result = currentItem;
			Item lastItem = null;
			int xpos = spacing, ypos = spacing;
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				if (lastItem == currentItem) {
					result = category;
				}
				lastItem = category;
			}, delegate (Category curCategory, Item item, Gdk.Size itemDimension) {
				if (lastItem == currentItem) {
					result = item;
				}
				lastItem = item;
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
			if (this.vAdjustement.Value + this.Allocation.Height - spacing < rect.Bottom)
				this.vAdjustement.Value = rect.Bottom - this.Allocation.Height + spacing;
		}
		
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			this.hAdjustement = hAdjustement;
			this.hAdjustement.ValueChanged += delegate {
				this.QueueDraw ();
			};
			this.vAdjustement = vAdjustement;
			this.vAdjustement.ValueChanged += delegate {
				this.QueueDraw ();
			};
		}
		#endregion
		
		#region Item & Category iteration
		delegate void CategoryAction (Category category, Gdk.Size categoryDimension);
		delegate void ItemAction (Category curCategory, Item item, Gdk.Size itemDimension);
		void IterateItems (Category category, ref int xpos, ref int ypos, ItemAction action)
		{
			if (listMode || !category.CanIconizeItems) {
				foreach (Item item in category.Items) {
					if (!item.IsVisible)
						continue;
					
					layout.SetText (item.Text);
					int x, y;
					layout.GetPixelSize (out x, out y);
					y = Math.Max (IconSize.Height, y) + spacing;
					
					xpos = spacing;
					if (action != null)
						action (category, item, new Gdk.Size (Allocation.Width - spacing * 2, y));
					
					ypos += y;
				}
				return;
			}
			foreach (Item item in category.Items) {
				if (!item.IsVisible)
					continue;
				if (xpos + IconSize.Width + spacing >= this.Allocation.Width) {
					xpos = spacing;
					ypos += IconSize.Height;
				}
				if (action != null)
					action (category, item, IconSize);
				xpos += IconSize.Width;
			}
			ypos += IconSize.Height + spacing;
		}
		
		void Iterate (ref int xpos, ref int ypos, CategoryAction catAction, ItemAction action)
		{
			foreach (Category category in this.categories) {
				if (!category.IsVisible)
					continue;
				xpos = spacing;
				if (this.showCategories) {
					
					layout.SetText (category.Text);
					int x, y;
					layout.GetPixelSize (out x, out y);
					y = Math.Max (categoryHeaderSize, y);
					
					
					if (catAction != null)
						catAction (category, new Size (this.Allocation.Width - spacing * 2, y));
					ypos += y;
				} 
				if (category.IsExpanded || !this.showCategories) {
					IterateItems (category, ref xpos, ref  ypos, action);
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
			int xpos = spacing;
			int ypos = spacing;
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
				tooltipWindow.Dispose ();
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
			tooltipWindow.Move (Math.Max (spacing, ox + tipX), oy + tipY);
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
	
	public class Category : Item
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
		static Gdk.Pixbuf defaultIcon;
		Gdk.Pixbuf icon;
		string     text;
		string     tooltip;
		object     tag;
		bool       isVisible = true;
		
		public string Tooltip {
			get {
				return tooltip;
			}
			set {
				tooltip = value;
			}
		}
		
		public Gdk.Pixbuf Icon {
			get {
				return icon ?? DefaultIcon;
			}
			set {
				icon = value;
			}
		}
		
		static Gdk.Pixbuf DefaultIcon {
			get {
				if (defaultIcon == null) {
					Gtk.Label lab = new Gtk.Label ();
					lab.EnsureStyle ();
					defaultIcon = lab.RenderIcon (Stock.MissingImage, IconSize.Menu, string.Empty);
					lab.Destroy ();
				}
				return defaultIcon;
			}
		}
		
		public string Text {
			get {
				return text;
			}
			set {
				text = value;
			}
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
				return tag;
			}
			set {
				tag = value;
			}
		}
		
		public Item (string text) : this (null, text, null)
		{
		}
		
		public Item (Gdk.Pixbuf icon, string text) : this (icon, text, null)
		{
		}
		
		public Item (Gdk.Pixbuf icon, string text, string tooltip) : this (icon, text, tooltip, null)
		{
		}
		
		public Item (Gdk.Pixbuf icon, string text, string tooltip, object tag)
		{
			this.icon    = icon;
			this.text    = text;
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
