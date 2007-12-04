//
// CompactToolboxView.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class ToolboxObject
	{
		Gdk.Pixbuf icon;
		string     text;
		object     tag;
		
		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
			set {
				icon = value;
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
		
		public object Tag {
			get {
				return tag;
			}
			set {
				tag = value;
			}
		}
		
		public ToolboxObject (Gdk.Pixbuf icon, string text, object tag)
		{
			this.icon = icon;
			this.text = text;
			this.tag  = tag;
		}
		
		public bool FilterItem (string filter)
		{
			return !String.IsNullOrEmpty (filter) && Text.ToUpper ().IndexOf (filter.ToUpper ()) < 0;			
		}
	}
	
	public class ToolboxCategory
	{
		string name;
		List<ToolboxObject> items = new List<ToolboxObject> (); 
			
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public List<ToolboxObject> Items {
			get {
				return items;
			}
		}
		
		public ToolboxCategory (string name)
		{
			this.name = name;
		}
		
		public bool ContainsIcons (string filter)
		{
			foreach (ToolboxObject item in Items)
				if (!item.FilterItem (filter))
					return true;
			return false;
		}
	}
	
	public class CompactWidget : Gtk.DrawingArea
	{
		ToolboxObject         selectedItem = null;
		List<ToolboxCategory> categories   = new List<ToolboxCategory> ();
		string filter = "";
		bool showCategories = true;
		bool realSizeRequest;
		const int spacing            = 4;
		const int categoryHeaderSize = 22;
		
		public bool ShowCategories {
			get {
				return showCategories;
			}
			set {
				if (this.showCategories != value) {
					this.showCategories = value;
					this.QueueDraw ();
				}
			}
		}
		public string Filter {
			get {
				return filter;
			}
			set {
				if (filter != value) {
					filter = value;
					this.QueueDraw ();
				}
			}
		}
		public IEnumerable<ToolboxObject> AllItems {
			get {
				foreach (ToolboxCategory category in this.categories) {
					foreach (ToolboxObject item in category.Items) {
						yield return item;
					}
				}
			}
		}
		
		public int ItemCount {
			get {
				int result = 0;
				foreach (ToolboxCategory category in this.categories) 
					result += category.Items.Count;
				return result;
			}
		}
		
		public List<ToolboxCategory> Categories {
			get {
				return categories;
			}
		}
		
		public ToolboxObject SelectedItem {
			get {
				return selectedItem;
			}
			set {
				if (this.selectedItem != value) {
					this.selectedItem = value;
					this.QueueDraw ();
					OnSelectedItemChanged (EventArgs.Empty);
				}
			}
		}
		
		protected virtual void OnSelectedItemChanged (EventArgs e)
		{
			if (SelectedItemChanged != null) 
				SelectedItemChanged (this, e);
		}
		
		public event EventHandler SelectedItemChanged;
		
		public CompactWidget ()
		{
			this.Events =  EventMask.ExposureMask | 
				           EventMask.EnterNotifyMask |
				           EventMask.LeaveNotifyMask |
				           EventMask.ButtonPressMask | 
				           EventMask.ButtonReleaseMask | 
					       EventMask.PointerMotionMask
			;
		}
		
		public ToolboxCategory GetCategory (string name)
		{
			foreach (ToolboxCategory category in this.categories) 
				if (category.Name == name)
					return category;
			ToolboxCategory result = new ToolboxCategory (name);
			this.categories.Add (result);
			return result;
		}
		
		System.Drawing.Size IconSize {
			get {
				int width  = 0;
				int height = 0;
				foreach (ToolboxObject item in this.AllItems) { 
					width  = Math.Max (width, item.Icon.Width);
					height = Math.Max (height, item.Icon.Height);
				}
				return new System.Drawing.Size (width, height);
			}
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing e)
		{
			// Don't clear the selection when clicking on a button
			if (e.Detail != NotifyType.Ancestor || e.Mode != Gdk.CrossingMode.Grab)
				ClearMouseOverItem ();
			return true;
			//return OnLeaveNotifyEvent (e);
		}
		
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
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			SelectedItem = mouseOverItem;
			return base.OnButtonPressEvent (e);
		}
		
		ToolboxObject mouseOverItem;
		
		delegate void CategoryAction (ToolboxCategory category);
		delegate void Action (ToolboxObject item);
		void IterateIcons (IEnumerable<ToolboxObject> collection, ref int xpos, ref int ypos, Action action)
		{
			foreach (ToolboxObject item in collection) {
				if (item.FilterItem (filter))
					continue;
				if (xpos + IconSize.Width + spacing >= this.GdkWindow.VisibleRegion.Clipbox.Width) {
					xpos = spacing;
					ypos += IconSize.Height + spacing;
				}
				if (action != null)
					action (item);
				xpos += IconSize.Width + spacing;
			}
			ypos += IconSize.Height + spacing;
		}
		void Iterate (ref int xpos, ref int ypos,  CategoryAction catAction, Action action)
		{
			if (this.showCategories) {
				foreach (ToolboxCategory category in this.Categories) {
					if (!category.ContainsIcons (filter))
						continue;
					xpos = spacing;
					if (catAction != null)
						catAction (category);
					ypos += categoryHeaderSize;
					IterateIcons (category.Items, ref xpos, ref  ypos, action);
				}
			} else {
				IterateIcons (this.AllItems, ref xpos, ref  ypos, action);
			}
		}
		
		void ClearMouseOverItem ()
		{
			if (mouseOverItem != null) {
				mouseOverItem = null;
				HideTooltipWindow ();
				this.QueueDraw ();
			}
		}
		
		CustomTooltipWindow tooltipWindow = null;
		public void HideTooltipWindow ()
		{
			if (tooltipWindow != null) {
				tooltipWindow.Destroy ();
				tooltipWindow = null;
			}
		}
		public void ShowTooltip (string text, int x, int y, int iconWidth)
		{
			HideTooltipWindow (); 
			tooltipWindow = new CustomTooltipWindow ();
			tooltipWindow.Tooltip = text;
			int ox, oy;
			this.GdkWindow.GetOrigin (out ox, out oy);
			int w = tooltipWindow.Child.SizeRequest().Width;
			tooltipWindow.Move (ox + x - (w - iconWidth)/2, oy + y);
			tooltipWindow.ShowAll ();
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			int xpos = spacing;
			int ypos = spacing;
			bool found = false;
			Iterate (ref xpos, ref ypos, null, delegate (ToolboxObject item) {
				if (xpos <= e.X && e.X <= xpos + IconSize.Width + spacing  &&
				    ypos <= e.Y && e.Y <= ypos + IconSize.Height + spacing) {
					found = true;
					if (mouseOverItem != item) {
						mouseOverItem = item;
						ShowTooltip (item.Text, xpos - 2, ypos + IconSize.Height + 3, IconSize.Width + 4);
						this.QueueDraw ();
					}
				}
			});
			if (!found) {
				ClearMouseOverItem ();
			}
			return base.OnMotionNotifyEvent (e);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			Gdk.Window win = e.Window;
			Gdk.Rectangle area = e.Area;
			win.DrawRectangle (Style.BaseGC (StateType.Normal), true, area);
			
			int xpos = spacing;
			int ypos = spacing;
			Iterate (ref xpos, ref ypos, 
			delegate (ToolboxCategory category) {
				Pango.Layout layout = new Pango.Layout (this.PangoContext);
				layout.Width = Pango.Units.FromPixels (320);
				layout.Wrap = Pango.WrapMode.Word;
				layout.Alignment = Pango.Alignment.Left;
				layout.FontDescription = Pango.FontDescription.FromString ("Ahafoni CLM Bold 10");
				layout.SetMarkup (category.Name);
				xpos = spacing;
				win.DrawLayout (Style.ForegroundGC (StateType.Normal), xpos, ypos, layout);
			}, 
			delegate (ToolboxObject item) {
				if (item == selectedItem) {
					win.DrawRectangle (Style.BaseGC (StateType.Selected), 
					                   true, 
					                   new Gdk.Rectangle (xpos - 1, 
					                                      ypos - 1, 
					                                      IconSize.Width  + 2, 
					                                      IconSize.Height + 2));
				}
				win.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), 
				                item.Icon, 0, 0, 
				                xpos + 1 + (IconSize.Width  - item.Icon.Width) / 2, 
				                ypos + 1 + (IconSize.Height - item.Icon.Height) / 2, 
				                item.Icon.Width, item.Icon.Height, Gdk.RgbDither.None, 0, 0);
				
				if (item == mouseOverItem) {
					win.DrawRectangle (Style.DarkGC (StateType.Prelight), 
					                   false, 
					                   new Gdk.Rectangle (xpos - 2, 
					                                      ypos - 2 , 
					                                      IconSize.Width + 3, 
					                                      IconSize.Height + 3));
				}
			});
			return true;		
		}
	}
	
	public class CompactToolboxView : Gtk.ScrolledWindow
	{
		CompactWidget compactWidget;
		
		public object CurrentlySelected {
			get {
				return compactWidget.SelectedItem != null ? compactWidget.SelectedItem.Tag : null;
			}
		}
		
		public bool ShowCategories {
			get {
				return compactWidget.ShowCategories;
			}
			set {
				compactWidget.ShowCategories = value;
			}
		}
		public string Filter {
			get {
				return compactWidget.Filter;
			}
			set {
				compactWidget.Filter = value;
			}
		}

		public CompactToolboxView()
		{
			compactWidget = new CompactWidget ();
					
			compactWidget.SelectedItemChanged += delegate {
				OnSelectionChanged (EventArgs.Empty);
			};
			
			this.AddWithViewport (compactWidget);
			this.HscrollbarPolicy = PolicyType.Never;
			this.VscrollbarPolicy = PolicyType.Automatic;
		}
		
		public void SetNodes (ICollection nodes)
		{
			compactWidget.Categories.Clear ();
			foreach (BaseToolboxNode node in nodes) {
				ToolboxObject newItem = new ToolboxObject (node.ViewIcon, node.Label, node);
				if (node is ItemToolboxNode) {
					ToolboxCategory category = compactWidget.GetCategory (((ItemToolboxNode)node).Category);
					category.Items.Add (newItem);
				}
			}
			QueueDraw ();
		}
		
		protected virtual void OnSelectionChanged (EventArgs e)
		{
			if (this.SelectionChanged != null)
				this.SelectionChanged (this, e);
		}
		
		public event EventHandler SelectionChanged;
	}
	
	public class CustomTooltipWindow : Gtk.Window
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
		public CustomTooltipWindow () : base (Gtk.WindowType.Popup)
		{
			Name = "gtk-tooltips";
			label.Xalign = 0;
			label.Xpad = 3;
			label.Ypad = 3;
			Add (label);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			Gtk.Requisition req = SizeRequest ();
			Gtk.Style.PaintFlatBox (this.Style, 
			                        this.GdkWindow, 
			                        Gtk.StateType.Normal, 
			                        Gtk.ShadowType.Out, 
			                        Gdk.Rectangle.Zero, 
			                        this, "tooltip", 0, 0, req.Width, req.Height);
			return true;
		}
	}	
}
