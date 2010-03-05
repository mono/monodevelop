using System;
using System.Linq;
using Gtk;
using Gdk;
using System.Collections.Generic;

namespace Mono.TextEditor.Theatrics
{
	/// <summary>
	/// A scrolled window with the ability to put widgets beside the scrollbars.
	/// </summary>
	public class SmartScrolledWindow : Bin
	{
		Adjustment vAdjustment;
		Gtk.VScrollbar vScrollBar;
		
		Adjustment hAdjustment;
		Gtk.HScrollbar hScrollBar;
		
		List<SmartScrolledWindowContainerChild> children = new List<SmartScrolledWindowContainerChild> ();
		public enum ChildPosition {
			Top,
			Bottom,
			Left,
			Right
		}
		
		public override ContainerChild this [Widget w] {
			get {
				foreach (ContainerChild info in children.ToArray ()) {
					if (info.Child == w)
						return info;
				}
				return null;
			}
		}
		
		public class SmartScrolledWindowContainerChild : Container.ContainerChild
		{
			public ChildPosition ChildPosition {
				get;
				set;
			}
			
			public SmartScrolledWindowContainerChild (Container parent, Widget child) : base (parent, child)
			{
			}
		}
		
		public SmartScrolledWindow ()
		{
			vAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			vAdjustment.Changed += HandleAdjustmentChanged;
			
			vScrollBar = new VScrollbar (vAdjustment);
			vScrollBar.Parent = this;
			vScrollBar.Show ();
			
			hAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			hAdjustment.Changed += HandleAdjustmentChanged;
			
			hScrollBar = new HScrollbar (hAdjustment);
			hScrollBar.Parent = this;
			hScrollBar.Show ();
		}
		
		void HandleAdjustmentChanged (object sender, EventArgs e)
		{
			Adjustment adjustment = (Adjustment)sender;
			Scrollbar scrollbar = adjustment == vAdjustment ? (Scrollbar)vScrollBar : hScrollBar;
			bool newVisible = adjustment.Upper - adjustment.Lower > adjustment.PageSize;
			if (scrollbar.Visible != newVisible) {
				scrollbar.Visible = newVisible;
				QueueResize ();
			}
		}
		
		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			base.ForAll (include_internals, callback);
			
			if (include_internals) {
				callback (vScrollBar);
				callback (hScrollBar);
				children.ForEach (child => callback (child.Child));
			}
		}
		
		public void AddChild (Gtk.Widget child, ChildPosition position)
		{
			child.Parent = this;
			child.Show ();
			children.Add (new SmartScrolledWindowContainerChild (this, child) { ChildPosition = position });
		}
		
		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			if (widget == Child)
				widget.SetScrollAdjustments (hAdjustment, vAdjustment);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			widget.Unparent ();
			foreach (var info in children.ToArray ()) {
				if (info.Child == widget) {
					children.Remove (info);
					break;
				}
			}
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			int vwidth = vScrollBar.Visible ? vScrollBar.Requisition.Width : 0;
			int hheight = hScrollBar.Visible ? hScrollBar.Requisition.Height : 0; 
			if (Child != null)
				Child.SizeAllocate (new Rectangle (allocation.X, allocation.Y, allocation.Width - vwidth, allocation.Height - hheight));
			
			if (vScrollBar.Visible) {
				int right = allocation.Right - vwidth;
				int vChildTopHeight = 0;
				foreach (var child in children.Where (child => child.ChildPosition == ChildPosition.Top)) {
					child.Child.SizeAllocate (new Rectangle (right, allocation.Y + vChildTopHeight, allocation.Width - vwidth, child.Child.Requisition.Height));
					vChildTopHeight += child.Child.Requisition.Height;
				}
				vScrollBar.SizeAllocate (new Rectangle (right, allocation.Y + vChildTopHeight, vwidth, allocation.Height - hheight - vChildTopHeight));
				vScrollBar.Value = System.Math.Max (System.Math.Min (vAdjustment.Upper - vAdjustment.PageSize, vScrollBar.Value), vAdjustment.Lower);
			}
			
			if (hScrollBar.Visible) {
				hScrollBar.SizeAllocate (new Rectangle (allocation.X, allocation.Bottom - hheight, allocation.Width - vwidth, hheight));
				hScrollBar.Value = System.Math.Max (System.Math.Min (hAdjustment.Upper - hAdjustment.PageSize, hScrollBar.Value), hAdjustment.Lower);
			}
		}
		
		static double GetWheelDelta (Scrollbar scrollbar, ScrollDirection direction)
		{
			double delta = System.Math.Pow (scrollbar.Adjustment.PageSize, 2.0 / 3.0);
			if (direction == ScrollDirection.Up || direction == ScrollDirection.Left)
				delta = -delta;
			if (scrollbar.Inverted)
				delta = -delta;
			return delta;
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			Scrollbar scrollWidget = (evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Down) ? (Scrollbar)vScrollBar : hScrollBar;
			
			if (scrollWidget.Visible) {
				double newValue = scrollWidget.Adjustment.Value + GetWheelDelta (scrollWidget, evnt.Direction);
				newValue = System.Math.Max (System.Math.Min (scrollWidget.Adjustment.Upper  - scrollWidget.Adjustment.PageSize, newValue), scrollWidget.Adjustment.Lower);
				scrollWidget.Adjustment.Value = newValue;
			}
			return base.OnScrollEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			if (Child != null)
				Child.SizeRequest ();
			vScrollBar.SizeRequest ();
			hScrollBar.SizeRequest ();
			children.ForEach (child => child.Child.SizeRequest ());
		}
	}
}

