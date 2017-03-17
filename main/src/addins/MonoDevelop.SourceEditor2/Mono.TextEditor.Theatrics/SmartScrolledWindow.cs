using System;
using System.Linq;
using Gtk;
using Gdk;
using System.Collections.Generic;
using MonoDevelop.Components;

namespace Mono.TextEditor.Theatrics
{
	/// <summary>
	/// A scrolled window with the ability to put widgets beside the scrollbars.
	/// </summary>
	class SmartScrolledWindow : Bin
	{
		Adjustment vAdjustment;
		Gtk.Widget vScrollBar;
		
		Adjustment hAdjustment;
		Gtk.HScrollbar hScrollBar;
		
		List<SmartScrolledWindowContainerChild> children = new List<SmartScrolledWindowContainerChild> ();
		public enum ChildPosition {
			Top,
			Bottom,
			Left,
			Right
		}
		
		public Adjustment Vadjustment {
			get { return this.vAdjustment; }
		}

		public Adjustment Hadjustment {
			get { return this.hAdjustment; }
		}

		public bool BorderVisible { get; set; }
		
		public override ContainerChild this [Widget w] {
			get {
				foreach (ContainerChild info in children.ToArray ()) {
					if (info.Child == w)
						return info;
				}
				return null;
			}
		}
		
		internal class SmartScrolledWindowContainerChild : Container.ContainerChild
		{
			public ChildPosition ChildPosition {
				get;
				set;
			}
			
			public SmartScrolledWindowContainerChild (Container parent, Widget child) : base (parent, child)
			{
			}
		}
		
		protected override void OnMapped ()
		{
			base.OnMapped ();
			QueueDraw ();
		}
		
		public SmartScrolledWindow (Gtk.Widget vScrollBar = null)
		{
			GtkWorkarounds.FixContainerLeak (this);
			
			vAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			vAdjustment.Changed += HandleAdjustmentChanged;
			
			this.vScrollBar = vScrollBar ?? new VScrollbar (vAdjustment);
			this.vScrollBar.Parent = this;
			this.vScrollBar.Show ();
			
			hAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			hAdjustment.Changed += HandleAdjustmentChanged;
			
			hScrollBar = new HScrollbar (hAdjustment);
			hScrollBar.Parent = this;
			hScrollBar.Show ();
		}
		
		public void ReplaceVScrollBar (Gtk.Widget widget)
		{
			if (vScrollBar != null) {
				vScrollBar.Unparent ();
				vScrollBar.Destroy ();
			}
			this.vScrollBar = widget;
			this.vScrollBar.Parent = this;
			this.vScrollBar.Show ();
		}
		
		protected override void OnDestroyed ()
		{
			if (Child != null)
				Child.Destroy ();
			if (vAdjustment != null) {
				vAdjustment.Changed -= HandleAdjustmentChanged;
				vAdjustment.Destroy ();
				vAdjustment = null;
			}
			if (hAdjustment != null) {
				hAdjustment.Changed -= HandleAdjustmentChanged;
				hAdjustment.Destroy ();
				hAdjustment = null;
			}
			if (vScrollBar != null) {
				vScrollBar.Destroy ();
				vScrollBar = null;
			}
			if (hScrollBar != null) {
				hScrollBar.Destroy ();
				hScrollBar = null;
			}
			foreach (var c in children) {
				c.Child.Destroy ();
			}
			base.OnDestroyed ();
		}
		
		void HandleAdjustmentChanged (object sender, EventArgs e)
		{
			var adjustment = (Adjustment)sender;
			var scrollbar = adjustment == vAdjustment ? vScrollBar : hScrollBar;
			if (!(scrollbar is Scrollbar))
				return;
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
			if (!include_internals) 
				return;
			if (Child != null)
				callback (Child);
			callback (vScrollBar);
			callback (hScrollBar);
			children.ForEach (child => callback (child.Child));
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
			foreach (var info in children.ToArray ()) {
				if (info.Child == widget) {
					info.Child.Unparent ();
					children.Remove (info);
					return;
				}
			}
			if (widget == vScrollBar) {
				vScrollBar = null;
				return;
			}
			if (widget == hScrollBar) {
				vScrollBar = null;
				return;
			}
			base.OnRemoved (widget);
		}
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			int margin = BorderVisible ? 1 : 0;
			int vwidth = vScrollBar.Visible ? vScrollBar.Requisition.Width : 0;
			int hheight = hScrollBar.Visible ? hScrollBar.Requisition.Height : 0; 
			var childRectangle = new Rectangle (allocation.X + margin, allocation.Y + margin, allocation.Width - vwidth - margin*2, allocation.Height - hheight - margin*2);

			if (Child != null) 
				Child.SizeAllocate (childRectangle);
			if (vScrollBar.Visible) {
				int vChildTopHeight = -1;
				foreach (var child in children.Where (child => child.ChildPosition == ChildPosition.Top)) {
					child.Child.SizeAllocate (new Rectangle (childRectangle.RightInside (), childRectangle.Y + vChildTopHeight, allocation.Width - vwidth, child.Child.Requisition.Height));
					vChildTopHeight += child.Child.Requisition.Height;
				}
				int v = vScrollBar is Scrollbar && hScrollBar.Visible ? hScrollBar.Requisition.Height : 0;
				vScrollBar.SizeAllocate (new Rectangle (childRectangle.X + childRectangle.Width + margin, childRectangle.Y + vChildTopHeight, vwidth, Allocation.Height - v - vChildTopHeight - margin));
				vAdjustment.Value = System.Math.Max (System.Math.Min (vAdjustment.Upper - vAdjustment.PageSize, vAdjustment.Value), vAdjustment.Lower);
			}
			
			if (hScrollBar.Visible) {
				int v = vScrollBar.Visible ? vScrollBar.Requisition.Width : 0;
				hScrollBar.SizeAllocate (new Rectangle (allocation.X, childRectangle.Y + childRectangle.Height + margin, allocation.Width - v, hheight));
				hScrollBar.Value = System.Math.Max (System.Math.Min (hAdjustment.Upper - hAdjustment.PageSize, hScrollBar.Value), hAdjustment.Lower);
			}
		}
		
		static double Clamp (double min, double val, double max)
		{
			return System.Math.Max (min, System.Math.Min (val, max));
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			var alloc = Allocation;
			double dx, dy;
			evnt.GetPageScrollPixelDeltas (alloc.Width, alloc.Height, out dx, out dy);
			
			if (dx != 0.0 && hScrollBar.Visible)
				hAdjustment.AddValueClamped (dx);
			
			if (dy != 0.0 && vScrollBar.Visible)
				vAdjustment.AddValueClamped (dy);
			
			return (dx != 0.0 || dy != 0.0) || base.OnScrollEvent (evnt);
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
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (BorderVisible) {
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					cr.LineWidth = 1;
					
					var alloc = Allocation;
					int right = alloc.RightInside ();
					int bottom = alloc.BottomInside ();
					
					cr.SharpLineX (alloc.X, alloc.Y, alloc.X, bottom);
					cr.SharpLineX (right, alloc.Y, right, bottom);
					
					cr.SharpLineY (alloc.X, alloc.Y, right, alloc.Y);
					cr.SharpLineY (alloc.X, bottom, right, bottom);
					
					cr.SetSourceColor ((HslColor)Style.Dark (State));
					cr.Stroke ();
				}
			}
			return base.OnExposeEvent (evnt);
		}
		
	}
}

