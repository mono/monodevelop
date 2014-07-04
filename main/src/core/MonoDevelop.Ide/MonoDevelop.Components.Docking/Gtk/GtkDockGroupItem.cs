//
// DockGroupItem.cs
//
// Author:
//   Lluis Sanchez Gual
//

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
using System.Xml;
using Gtk;

namespace MonoDevelop.Components.Docking
{
	internal class GtkDockGroupItem: GtkDockObject
	{
		DockItemBackend item;

		public DockItemBackend Item {
			get {
				return item;
			}
			set {
				item = value;
			}
		}
		
		public string Id {
			get { return item.Id; }
		}
		
		public GtkDockGroupItem (GtkDockFrame frame, IDockGroupItem item): base (frame, item)
		{
			this.item = (DockItemBackend) item.Item.Backend;
		}

		internal override void GetDefaultSize (out int width, out int height)
		{
			width = item.DefaultWidth;
			height = item.DefaultHeight;
		}
		
		internal override void GetMinSize (out int width, out int height)
		{
			Requisition req = SizeRequest ();
			width = req.Width;
			height = req.Height;
		}
		
		internal override Requisition SizeRequest ()
		{
			var req = item.Widget.SizeRequest ();

			if (ParentGroup.Type != DockGroupType.Tabbed || ParentGroup.Objects.Count == 1) {
				var tr = item.TitleTab.SizeRequest ();
				req.Height += tr.Height;
				return req;
			} else
				return req;
		}

		public override void SizeAllocate (Gdk.Rectangle newAlloc)
		{
			if ((ParentGroup.Type != DockGroupType.Tabbed || ParentGroup.Objects.Count == 1) && (item.Behavior & DockItemBehavior.NoGrip) == 0) {
				var tr = newAlloc;
				tr.Height = item.TitleTab.SizeRequest ().Height;
				item.TitleTab.SizeAllocate (tr);
				var wr = newAlloc;
				wr.Y += tr.Height;
				wr.Height -= tr.Height;
				item.Widget.SizeAllocate (wr);
			}
			else
				item.Widget.SizeAllocate (newAlloc);

			base.SizeAllocate (newAlloc);
		}
		
		public override bool Expand {
			get { return item.Frontend.Expand; }
		}
		
		internal override void QueueResize ()
		{
			item.Widget.QueueResize ();
		}
		
		internal override bool GetDockTarget (DockItemBackend item, int px, int py, out DockDelegate dockDelegate, out Gdk.Rectangle rect)
		{
			return GetDockTarget (item, px, py, Allocation, out dockDelegate, out rect);
		}
		
		public bool GetDockTarget (DockItemBackend item, int px, int py, Gdk.Rectangle rect, out DockDelegate dockDelegate, out Gdk.Rectangle outrect)
		{
			outrect = Gdk.Rectangle.Zero;
			dockDelegate = null;
			
			if (item != this.item && rect.Contains (px, py)) {

				// Check if the item is allowed to be docked here
				var s = Frontend.GetRegionStyle ();

				int xdockMargin = (int) ((double)rect.Width * (1.0 - GtkDockFrame.ItemDockCenterArea)) / 2;
				int ydockMargin = (int) ((double)rect.Height * (1.0 - GtkDockFrame.ItemDockCenterArea)) / 2;
				DockPosition pos;
				
/*				if (ParentGroup.Type == DockGroupType.Tabbed) {
					rect = new Gdk.Rectangle (rect.X + xdockMargin, rect.Y + ydockMargin, rect.Width - xdockMargin*2, rect.Height - ydockMargin*2);
					pos = DockPosition.CenterAfter;
				}*/				
				if (px <= rect.X + xdockMargin && ParentGroup.Type != DockGroupType.Horizontal) {
					if (s.SingleColumnMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.X, rect.Y, xdockMargin, rect.Height);
					pos = DockPosition.Left;
				}
				else if (px >= rect.Right - xdockMargin && ParentGroup.Type != DockGroupType.Horizontal) {
					if (s.SingleColumnMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.Right - xdockMargin, rect.Y, xdockMargin, rect.Height);
					pos = DockPosition.Right;
				}
				else if (py <= rect.Y + ydockMargin && ParentGroup.Type != DockGroupType.Vertical) {
					if (s.SingleRowMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.X, rect.Y, rect.Width, ydockMargin);
					pos = DockPosition.Top;
				}
				else if (py >= rect.Bottom - ydockMargin && ParentGroup.Type != DockGroupType.Vertical) {
					if (s.SingleRowMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.X, rect.Bottom - ydockMargin, rect.Width, ydockMargin);
					pos = DockPosition.Bottom;
				}
				else {
					outrect = new Gdk.Rectangle (rect.X + xdockMargin, rect.Y + ydockMargin, rect.Width - xdockMargin*2, rect.Height - ydockMargin*2);
					pos = DockPosition.Center;
				}
				
				dockDelegate = delegate (DockItemBackend dit) {
					Frame.Frontend.DockItemRelative (dit.Frontend, (IDockGroupItem)Frontend, pos, Id);
					dit.Frontend.Present (true);
				};
				return true;
			}
			return false;
		}
		
		internal override void Dump (int ind)
		{
			Console.WriteLine (new string (' ', ind) + item.Id + " size:" + Size + " alloc:" + Allocation);
		}

		public void SetFloatRect (Xwt.Rectangle rect)
		{
			((IDockGroupItem)Frontend).FloatRect = rect;
		}
		
		public override string ToString ()
		{
			return "[DockItemBackend " + Item.Id + "]";
		}
	}
}
