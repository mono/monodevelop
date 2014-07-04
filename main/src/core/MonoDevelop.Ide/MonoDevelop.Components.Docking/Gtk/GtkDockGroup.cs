//
// MonoDevelop.Components.Docking.cs
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
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components.Docking
{
	class GtkDockGroup: GtkDockObject
	{
		DockGroupType type;
		List<GtkDockObject> dockObjects = new List<GtkDockObject> ();
		AllocStatus allocStatus = AllocStatus.NotSet;
		TabStrip boundTabStrip;
		GtkDockGroupItem tabFocus;
		int currentTabPage;
		IDockGroup group;
		
		enum AllocStatus { NotSet, Invalid, RestorePending, NewSizeRequest, Valid };
		
		internal GtkDockGroup (GtkDockFrame frame, IDockGroup grp): base (frame, grp)
		{
			type = grp.Type;
		}
		
		public DockGroupType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}

		public IDockGroup Group {
			get { return group; }
		}
		
		public List<GtkDockObject> Objects {
			get { return dockObjects; }
		}

		public void Sync (IDockGroup group)
		{
			this.group = group;
			MarkForRelayout ();
			Type = group.Type;
			dockObjects.Clear ();
			foreach (var ob in group.VisibleObjects) {
				if (ob is IDockGroup) {
					var ng = new GtkDockGroup (Frame, (IDockGroup) ob);
					ng.Sync ((IDockGroup)ob);
					dockObjects.Add (ng);
					ng.ParentGroup = this;
				} else {
					var oi = (IDockGroupItem)ob;
					var it = new GtkDockGroupItem (Frame, oi);
					dockObjects.Add (it);
					it.ParentGroup = this;
					oi.Item.ResetMode ();
				}
			}
		}
		
		void MarkForRelayout ()
		{
			if (allocStatus == AllocStatus.Valid)
				allocStatus = AllocStatus.Invalid;
		}
		
		internal GtkDockGroup FindGroupContaining (string id)
		{
			GtkDockGroupItem it = FindDockGroupItem (id);
			if (it != null)
				return it.ParentGroup;
			else
				return null;
		}
		
		internal GtkDockGroupItem FindDockGroupItem (string id)
		{
			foreach (GtkDockObject ob in dockObjects) {
				GtkDockGroupItem it = ob as GtkDockGroupItem;
				if (it != null && it.Id == id)
					return it;
				GtkDockGroup g = ob as GtkDockGroup;
				if (g != null) {
					it = g.FindDockGroupItem (id);
					if (it != null)
						return it;
				}
			}
			return null;
		}
		
		public int GetObjectIndex (GtkDockObject obj)
		{
			for (int n=0; n<dockObjects.Count; n++) {
				if (dockObjects [n] == obj)
					return n;
			}
			return -1;
		}
		
		internal override void RestoreAllocation ()
		{
			base.RestoreAllocation ();
			allocStatus = Size >= 0 ? AllocStatus.RestorePending : AllocStatus.NotSet;
			
			// Make a copy because RestoreAllocation can fire events such as VisibleChanged,
			// and subscribers may do changes in the list.
			List<GtkDockObject> copy = new List<GtkDockObject> (dockObjects);
			foreach (GtkDockObject ob in copy)
				ob.RestoreAllocation ();
		}
		
		internal override void StoreAllocation ()
		{
			base.StoreAllocation ();
			foreach (GtkDockObject ob in dockObjects)
				ob.StoreAllocation ();
			if (Type == DockGroupType.Tabbed && boundTabStrip != null)
				currentTabPage = boundTabStrip.CurrentTab;
		}
		
		public override bool Expand {
			get {
				foreach (GtkDockObject ob in dockObjects)
					if (ob.Expand)
						return true;
				return false;
			}
		}
		
		public override void SizeAllocate (Gdk.Rectangle newAlloc)
		{
			Gdk.Rectangle oldAlloc = Allocation;
			base.SizeAllocate (newAlloc);
			
			if (type == DockGroupType.Tabbed) {
				if (boundTabStrip != null) {
					int tabsHeight = boundTabStrip.SizeRequest ().Height;
					boundTabStrip.SizeAllocate (new Gdk.Rectangle (newAlloc.X, newAlloc.Y, newAlloc.Width, tabsHeight));
				}
				if (allocStatus == AllocStatus.Valid && newAlloc == oldAlloc) {
					// Even if allocation has not changed, SizeAllocation has to be called on all items to avoid redrawing issues.
					foreach (GtkDockObject ob in Objects)
						ob.SizeAllocate (ob.Allocation);
					return;
				}
				if (Objects.Count > 1 && boundTabStrip != null) {
					int tabsHeight = boundTabStrip.SizeRequest ().Height;
					newAlloc.Height -= tabsHeight;
					newAlloc.Y += tabsHeight;
					boundTabStrip.QueueDraw ();
				} else if (Objects.Count != 0) {
					((GtkDockGroupItem)Objects [0]).Item.Widget.Show ();
				}
				allocStatus = AllocStatus.Valid;
				foreach (GtkDockObject ob in Objects) {
					ob.Size = ob.PrefSize = -1;
					ob.SizeAllocate (newAlloc);
				}
				return;
			}
			
			bool horiz = type == DockGroupType.Horizontal;
			int pos = horiz ? Allocation.Left : Allocation.Top;
			
			if (allocStatus == AllocStatus.Valid && newAlloc == oldAlloc) {
				// The layout of this group (as a whole) has not changed, but the layout
				// of child items may have changed. Assign the new sizes.
				
				if (CheckMinSizes ())
					allocStatus = AllocStatus.NewSizeRequest;
				else {
					foreach (GtkDockObject ob in Objects) {
						Gdk.Rectangle rect;
						int ins = ob.AllocSize;
						if (horiz)
							rect = new Gdk.Rectangle (pos, Allocation.Y, ins, Allocation.Height);
						else
							rect = new Gdk.Rectangle (Allocation.X, pos, Allocation.Width, ins);
						ob.SizeAllocate (rect);
						pos += ins + Frame.TotalHandleSize;
					}
					return;
				}
			}
			
			// This is the space available for the child items (excluding size
			// required for the resize handles)
			int realSize = GetRealSize (Objects);
			
			if (allocStatus == AllocStatus.NotSet/* || allocStatus == AllocStatus.RestorePending*/) {
				// It is the first size allocation. Calculate all sizes.
				CalcNewSizes ();
			}
			else if (allocStatus != AllocStatus.NewSizeRequest) {

				// Don't proportionally resize the pads. Instead, resize only those pads with the Expand flag.
				// This logic is implemented in CalcNewSizes, so no need to reimplement it
				CalcNewSizes ();

				// Disabled the proportional resize of pads for the above reason

/*				// Available space has changed, so the size of the items must be changed.
				// First of all, get the change fraction
				double change;
				if (horiz)
					change = (double) newAlloc.Width / (double) oldAlloc.Width;
				else
					change = (double) newAlloc.Height / (double) oldAlloc.Height;

				// Get the old total size of the visible objects. Used to calculate the
				// proportion of size of each item.
				double tsize = 0;
				double rsize = 0;
				foreach (GtkDockObject ob in VisibleObjects) {
					tsize += ob.PrefSize;
					rsize += ob.Size;
				}
				
				foreach (GtkDockObject ob in dockObjects) {
					if (ob.Visible) {
						// Proportionally spread the new available space among all visible objects
						ob.Size = ob.PrefSize = (ob.PrefSize / tsize) * (double) realSize;
					} else {
						// For non-visible objects, change the size by the same grow fraction. In this
						// way, when the item is shown again, it size will have the correct proportions.
						ob.Size = ob.Size * change;
						ob.PrefSize = ob.PrefSize * change;
					}
					ob.DefaultSize = ob.DefaultSize * change;
				}
				CheckMinSizes ();*/
			}

			allocStatus = AllocStatus.Valid;

			// Sizes for all items have been set. 
			// Sizes are real numbers to ensure that the values are not degradated when resizing
			// pixel by pixel. Now those have to be converted to integers, that is, actual allocated sizes.
			
			int ts = 0;
			for (int n=0; n<Objects.Count; n++) {
				GtkDockObject ob = Objects [n];

				int ins = (int) Math.Truncate (ob.Size);
				
				if (n == Objects.Count - 1)
					ins = realSize - ts;
				
				ts += ins;
				
				if (ins < 0)
					ins = 0;
				
				ob.AllocSize = ins;
				
				if (horiz)
					ob.SizeAllocate (new Gdk.Rectangle (pos, Allocation.Y, ins, Allocation.Height));
				else
					ob.SizeAllocate (new Gdk.Rectangle (Allocation.X, pos, Allocation.Width, ins));
				
				pos += ins + Frame.TotalHandleSize;
			}
		}
		
		int GetRealSize (List<GtkDockObject> objects)
		{
			// Returns the space available for the child items (excluding size
			// required for the resize handles)
			
			int realSize;
			if (type == DockGroupType.Horizontal)
				realSize = Allocation.Width;
			else
				realSize = Allocation.Height;
			
			// Ignore space required for the handles
			if (objects.Count > 1)
				realSize -= (Frame.TotalHandleSize * (objects.Count - 1));
			
			return realSize;
		}
		
		internal void CalcNewSizes ()
		{
			// Calculates the size assigned by default to each child item.
			// Size is proportionally assigned to each item, taking into account
			// the available space, and the default size of each item.
			
			// If there are items with the Expand flag set, those will proportionally
			// take the space left after allocating the other (not exandable) items.
			
			// This is the space available for the child items (excluding size
			// required for the resize handles)
			double realSize = (double) GetRealSize (Objects);
			
			bool hasExpandItems = false;
			double noexpandSize = 0;
			double minExpandSize = 0;
			double defaultExpandSize = 0;
			
			for (int n=0; n<Objects.Count; n++) {
				GtkDockObject ob = Objects [n];
				if (ob.Expand) {
					minExpandSize += ob.MinSize;
					defaultExpandSize += ob.DefaultSize;
					hasExpandItems = true;
				}
				else {
					ob.Size = ob.DefaultSize;
					noexpandSize += ob.DefaultSize;
				}
			}

			double expandSize = realSize - noexpandSize;
			foreach (GtkDockObject ob in Objects) {
				if (!hasExpandItems)
					ob.Size = (ob.DefaultSize / noexpandSize) * realSize;
				else if (ob.Expand)
					ob.Size = (ob.DefaultSize / defaultExpandSize) * expandSize;
				ob.PrefSize = ob.Size;
			}

			CheckMinSizes ();
		}
		
		bool CheckMinSizes ()
		{
			// Checks if any of the items has a size smaller than permitted.
			// In this case it tries to regain size by reducing other items.
			
			// First of all calculate the size to be regained, and the size available
			// from other items
			
			bool sizesChanged = false;
			
			double avSize = 0;
			double regSize = 0;
			foreach (GtkDockObject ob in Objects) {
				if (ob.Size < ob.MinSize) {
					regSize += ob.MinSize - ob.Size;
					ob.Size = ob.MinSize;
					sizesChanged = true;
				} else {
					avSize += ob.Size - ob.MinSize;
				}
			}
			
			if (!sizesChanged)
				return false;
			
			// Now spread the required size among the resizable items 
			
			if (regSize > avSize)
				regSize = avSize;
			
			double ratio = (avSize - regSize) / avSize;
			foreach (GtkDockObject ob in Objects) {
				if (ob.Size <= ob.MinSize)
					continue;
				double avs = ob.Size - ob.MinSize;
				ob.Size = ob.MinSize + avs * ratio;
			}
			return sizesChanged;
		}
		
		internal override Gtk.Requisition SizeRequest ()
		{
			bool getMaxW = true, getMaxH = true;
			if (type == DockGroupType.Horizontal)
				getMaxW = false;
			else if (type == DockGroupType.Vertical)
				getMaxH = false;
			
			Requisition ret = new Requisition ();
			ret.Height = Objects.Count * Frame.TotalHandleSize;
			foreach (GtkDockObject ob in Objects) {
				Requisition req = ob.SizeRequest ();
				if (getMaxH) {
					if (req.Height > ret.Height)
						ret.Height = req.Height;
				} else
					ret.Height += req.Height;
				
				if (getMaxW) {
					if (req.Width > ret.Width)
						ret.Width = req.Width;
				} else
					ret.Width += req.Width;
			}
			if (type == DockGroupType.Tabbed && Objects.Count > 1 && boundTabStrip != null) {
				Gtk.Requisition tabs = boundTabStrip.SizeRequest ();
				ret.Height += tabs.Height;
				if (ret.Width < tabs.Width)
					ret.Width = tabs.Width;
			}
			return ret;
		}

		internal void UpdateNotebook (TabStrip ts)
		{
			Gtk.Widget oldpage = null;
			int oldtab = -1;
			
			if (tabFocus != null) {
				oldpage = tabFocus.Item.Widget;
				tabFocus = null;
			} else if (boundTabStrip != null) {
				oldpage = boundTabStrip.CurrentPage;
				oldtab = boundTabStrip.CurrentTab;
			}
			
			ts.Clear ();
			
			// Add missing pages
			foreach (GtkDockObject ob in Objects) {
				GtkDockGroupItem it = ob as GtkDockGroupItem;
				ts.AddTab (it.Item.TitleTab);
			}

			boundTabStrip = ts;
			
			if (oldpage != null) {
				boundTabStrip.CurrentPage = oldpage;
			}
			else if (currentTabPage != -1 && currentTabPage < boundTabStrip.TabCount) {
				boundTabStrip.CurrentTab = currentTabPage;
			}

			// Discard the currentTabPage value. Current page is now tracked by the tab strip
			currentTabPage = -1;

			if (boundTabStrip.CurrentTab == -1) {
				if (oldtab != -1) {
					if (oldtab < boundTabStrip.TabCount)
						boundTabStrip.CurrentTab = oldtab;
					else
						boundTabStrip.CurrentTab = boundTabStrip.TabCount - 1;
				} else
					boundTabStrip.CurrentTab = 0;
			}
			boundTabStrip.BottomPadding = 0;
		}
		
		internal void Present (DockItemBackend it, bool giveFocus)
		{
			if (type == DockGroupType.Tabbed) {
				for (int n=0; n<Objects.Count; n++) {
					GtkDockGroupItem dit = Objects[n] as GtkDockGroupItem;
					if (dit.Item == it) {
						currentTabPage = n;
						if (boundTabStrip != null)
							boundTabStrip.CurrentPage = it.Widget;
						break;
					}
				}
			}
			if (giveFocus && it.Frontend.Visible)
				it.SetFocus ();
		}

		internal bool IsSelectedPage (DockItemBackend it)
		{
			if (type != DockGroupType.Tabbed || boundTabStrip == null || boundTabStrip.CurrentTab == -1 || Objects == null || boundTabStrip.CurrentTab >= Objects.Count)
				return false;
			GtkDockGroupItem dit = Objects[boundTabStrip.CurrentTab] as GtkDockGroupItem;
			return dit.Item == it;
		}
		
		internal void UpdateTitle (DockItemBackend it)
		{
			if (type == DockGroupType.Tabbed && boundTabStrip != null)
				boundTabStrip.SetTabLabel (it.Widget, it.Icon, it.Label);
		}
				
		internal void UpdateStyle (DockItemBackend it)
		{
			if (type == DockGroupType.Tabbed && boundTabStrip != null)
				boundTabStrip.UpdateStyle (it);
		}
		
		internal void FocusItem (GtkDockGroupItem it)
		{
			tabFocus = it;
		}
		
		internal void ResetNotebook ()
		{
			boundTabStrip = null;
		}

		public void AddRemoveWidgets ()
		{
			foreach (GtkDockObject ob in Objects) {
				GtkDockGroupItem it = ob as GtkDockGroupItem;
				if (it != null) {
					// Add the dock item to the container and show it if visible
					if (it.Item.Widget.Parent != Frame.Container) {
						if (it.Item.Widget.Parent != null) {
							((Gtk.Container)it.Item.Widget.Parent).Remove (it.Item.Widget);
						}
						Frame.Container.Add (it.Item.Widget);
					}
					if (!it.Item.Widget.Visible && type != DockGroupType.Tabbed)
						it.Item.Widget.Show ();

					// Do the same for the title tab
					if ((type != DockGroupType.Tabbed || Objects.Count == 1) && (it.Item.Behavior & DockItemBehavior.NoGrip) == 0) {
						var tab = it.Item.TitleTab;
						if (tab.Parent != Frame.Container) {
							if (tab.Parent != null) {
								((Gtk.Container)tab.Parent).Remove (tab);
							}
							Frame.Container.Add (tab);
							tab.Active = true;
						}
						tab.ShowAll ();
					}
				}
				else
					((GtkDockGroup)ob).AddRemoveWidgets ();
			}
		}

		internal override void GetDefaultSize (out int width, out int height)
		{
			if (type == DockGroupType.Tabbed) {
				width = -1;
				height = -1;
				foreach (GtkDockObject ob in Objects) {
					int dh, dw;
					ob.GetDefaultSize (out dw, out dh);
					if (dw > width)
						width = dw;
					if (dh > height)
						height = dh;
				}
			}
			else if (type == DockGroupType.Vertical) {
				height = Objects.Count > 0 ? (Objects.Count - 1) * Frame.TotalHandleSize : 0;
				width = -1;
				foreach (GtkDockObject ob in Objects) {
					int dh, dw;
					ob.GetDefaultSize (out dw, out dh);
					if (dw > width)
						width = dw;
					height += dh;
				}
			}
			else {
				width = Objects.Count > 0 ? (Objects.Count - 1) * Frame.TotalHandleSize : 0;
				height = -1;
				foreach (GtkDockObject ob in Objects) {
					int dh, dw;
					ob.GetDefaultSize (out dw, out dh);
					if (dh > height)
						height = dh;
					width += dw;
				}
			}
		}
		
		internal override void GetMinSize (out int width, out int height)
		{
			if (type == DockGroupType.Tabbed) {
				width = -1;
				height = -1;
				foreach (GtkDockObject ob in Objects) {
					int dh, dw;
					ob.GetMinSize (out dw, out dh);
					if (dw > width)
						width = dw;
					if (dh > height)
						height = dh;
				}
			}
			else if (type == DockGroupType.Vertical) {
				height = Objects.Count > 1 ? (Objects.Count - 1) * Frame.TotalHandleSize : 0;
				width = -1;
				foreach (GtkDockObject ob in Objects) {
					int dh, dw;
					ob.GetMinSize (out dw, out dh);
					if (dw > width)
						width = dw;
					height += dh;
				}
			}
			else {
				width = Objects.Count > 0 ? (Objects.Count - 1) * Frame.TotalHandleSize : 0;
				height = -1;
				foreach (GtkDockObject ob in Objects) {
					int dh, dw;
					ob.GetMinSize (out dw, out dh);
					if (dh > height)
						height = dh;
					width += dw;
				}
			}
		}
		
		public void Draw (Gdk.Rectangle exposedArea, GtkDockGroup currentHandleGrp, int currentHandleIndex)
		{
			if (type != DockGroupType.Tabbed) {
				DrawSeparators (exposedArea, currentHandleGrp, currentHandleIndex, DrawSeparatorOperation.Draw, false, null);
				foreach (GtkDockObject it in Objects) {
					GtkDockGroup grp = it as GtkDockGroup;
					if (grp != null)
						grp.Draw (exposedArea, currentHandleGrp, currentHandleIndex);
				}
			}
		}
		
		public void DrawSeparators (Gdk.Rectangle exposedArea, GtkDockGroup currentHandleGrp, int currentHandleIndex, DrawSeparatorOperation oper, List<Gdk.Rectangle> areasList)
		{
			DrawSeparators (exposedArea, currentHandleGrp, currentHandleIndex, oper, true, areasList);
		}
		
		void DrawSeparators (Gdk.Rectangle exposedArea, GtkDockGroup currentHandleGrp, int currentHandleIndex, DrawSeparatorOperation oper, bool drawChildrenSep, List<Gdk.Rectangle> areasList)
		{
			if (type == DockGroupType.Tabbed || Objects.Count == 0)
				return;
			
			GtkDockObject last = Objects [Objects.Count - 1];
			
			bool horiz = type == DockGroupType.Horizontal;
			int x = Allocation.X;
			int y = Allocation.Y;
			int hw = horiz ? Frame.HandleSize : Allocation.Width;
			int hh = horiz ? Allocation.Height : Frame.HandleSize;

			Gdk.GC hgc = null;

			if (areasList == null && oper == DrawSeparatorOperation.Draw) {
				hgc = new Gdk.GC (Frame.Container.GdkWindow);
				hgc.RgbFgColor = Styles.DockFrameBackground;
			}

			for (int n=0; n<Objects.Count; n++) {
				GtkDockObject ob = Objects [n];
				GtkDockGroup grp = ob as GtkDockGroup;
				if (grp != null && drawChildrenSep)
					grp.DrawSeparators (exposedArea, currentHandleGrp, currentHandleIndex, oper, areasList);
				if (ob != last) {
					if (horiz)
						x += ob.Allocation.Width + Frame.HandlePadding;
					else
						y += ob.Allocation.Height + Frame.HandlePadding;

					switch (oper) {
					case DrawSeparatorOperation.CollectAreas:
						if (Frame.ShadedSeparators)
							areasList.Add (new Gdk.Rectangle (x, y, hw, hh));
						break;
					case DrawSeparatorOperation.Invalidate:
						Frame.Container.QueueDrawArea (x, y, hw, hh);
						break;
					case DrawSeparatorOperation.Draw:
						Frame.Container.GdkWindow.DrawRectangle (hgc, true, x, y, hw, hh);
						break;
					case DrawSeparatorOperation.Allocate:
						Frame.Container.AllocateSplitter (this, n, new Gdk.Rectangle (x, y, hw, hh));
						break;
					}
					
					if (horiz)
						x += Frame.HandleSize + Frame.HandlePadding;
					else
						y += Frame.HandleSize + Frame.HandlePadding;
				}
			}
			if (hgc != null)
				hgc.Dispose ();
		}
		
		public void ResizeItem (int index, int newSize)
		{
			GtkDockObject o1 = Objects [index];
			GtkDockObject o2 = Objects [index+1];
			
			int dsize;
			
			dsize = newSize - o1.AllocSize;
			if (dsize < 0 && o1.AllocSize + dsize < o1.MinSize)
				dsize = o1.MinSize - o1.AllocSize;
			else if (dsize > 0 && o2.AllocSize - dsize < o2.MinSize)
				dsize = o2.AllocSize - o2.MinSize;
			
			// Assign the new sizes, applying the current ratio
			double sizeDif = (double)dsize;
			
			o1.AllocSize += dsize;
			o2.AllocSize -= dsize;
			
			o1.DefaultSize += (o1.DefaultSize * sizeDif) / o1.Size;
			o1.Size = o1.AllocSize;
			o1.PrefSize = o1.Size;
			
			o2.DefaultSize -= (o2.DefaultSize * sizeDif) / o2.Size;
			o2.Size = o2.AllocSize;
			o2.PrefSize = o2.Size;
			
			o1.QueueResize ();
			o2.QueueResize ();
		}
		
		internal override void QueueResize ()
		{
			foreach (GtkDockObject obj in Objects)
				obj.QueueResize ();
		}
		
		internal double GetObjectsSize ()
		{
			double total = 0;
			foreach (GtkDockObject obj in Objects)
				total += obj.Size;
			return total;
		}
		
		void DockTarget (DockItemBackend item, int n)
		{
			Frame.Frontend.DockItem (item.Frontend, (IDockGroup)Frontend, n != -1 && n < dockObjects.Count ? dockObjects [n].Frontend : null);
			CalcNewSizes ();
		}
		
		internal override bool GetDockTarget (DockItemBackend item, int px, int py, out DockDelegate dockDelegate, out Gdk.Rectangle rect)
		{
			dockDelegate = null;
			rect = Gdk.Rectangle.Zero;

			if (!Allocation.Contains (px, py) || Objects.Count == 0)
				return false;

			if (type == DockGroupType.Tabbed) {
				// Tabs can only contain DockGroupItems
				var sel = boundTabStrip != null ? Objects[boundTabStrip.CurrentTab] : Objects[Objects.Count - 1];
				return ((GtkDockGroupItem)sel).GetDockTarget (item, px, py, Allocation, out dockDelegate, out rect);
			}
			else if (type == DockGroupType.Horizontal) {
				if (px >= Allocation.Right - GtkDockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Objects[Objects.Count - 1].Frontend.GetRegionStyle ();
					if (s.SingleColumnMode.Value)
						return false;
					// Dock to the right of the group
					dockDelegate = delegate (DockItemBackend it) {
						DockTarget (it, dockObjects.Count);
					};
					rect = new Gdk.Rectangle (Allocation.Right - GtkDockFrame.GroupDockSeparatorSize, Allocation.Y, GtkDockFrame.GroupDockSeparatorSize, Allocation.Height);
					return true;
				}
				else if (px <= Allocation.Left + GtkDockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Objects[0].Frontend.GetRegionStyle ();
					if (s.SingleColumnMode.Value)
						return false;
					// Dock to the left of the group
					dockDelegate = delegate (DockItemBackend it) {
						DockTarget (it, 0);
					};
					rect = new Gdk.Rectangle (Allocation.Left, Allocation.Y, GtkDockFrame.GroupDockSeparatorSize, Allocation.Height);
					return true;
				}
				// Dock in a separator
				for (int n=0; n<Objects.Count; n++) {
					GtkDockObject ob = Objects [n];
					if (n < Objects.Count - 1 &&
					    px >= ob.Allocation.Right - GtkDockFrame.GroupDockSeparatorSize/2 &&
					    px <= ob.Allocation.Right + GtkDockFrame.GroupDockSeparatorSize/2)
					{
						// Check if the item is allowed to be docked here
						int dn = dockObjects.IndexOf (ob) + 1;
						var s = Frame.Frontend.GetRegionStyleForPosition (group, dn, true);
						if (s.SingleColumnMode.Value)
							return false;
						dockDelegate = delegate (DockItemBackend it) {
							DockTarget (it, dn);
						};
						rect = new Gdk.Rectangle (ob.Allocation.Right - GtkDockFrame.GroupDockSeparatorSize/2, Allocation.Y, GtkDockFrame.GroupDockSeparatorSize, Allocation.Height);
						return true;
					}
					else if (ob.GetDockTarget (item, px, py, out dockDelegate, out rect))
						return true;
				}
			}
			else if (type == DockGroupType.Vertical) {
				if (py >= Allocation.Bottom - GtkDockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Objects[Objects.Count - 1].Frontend.GetRegionStyle ();
					if (s.SingleRowMode.Value)
						return false;
					// Dock to the bottom of the group
					dockDelegate = delegate (DockItemBackend it) {
						DockTarget (it, dockObjects.Count);
					};
					rect = new Gdk.Rectangle (Allocation.X, Allocation.Bottom - GtkDockFrame.GroupDockSeparatorSize, Allocation.Width, GtkDockFrame.GroupDockSeparatorSize);
					return true;
				}
				else if (py <= Allocation.Top + GtkDockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Objects[0].Frontend.GetRegionStyle ();
					if (s.SingleRowMode.Value)
						return false;
					// Dock to the top of the group
					dockDelegate = delegate (DockItemBackend it) {
						DockTarget (it, 0);
					};
					rect = new Gdk.Rectangle (Allocation.X, Allocation.Top, Allocation.Width, GtkDockFrame.GroupDockSeparatorSize);
					return true;
				}
				// Dock in a separator
				for (int n=0; n<Objects.Count; n++) {
					GtkDockObject ob = Objects [n];
					if (n < Objects.Count - 1 &&
					    py >= ob.Allocation.Bottom - GtkDockFrame.GroupDockSeparatorSize/2 &&
					    py <= ob.Allocation.Bottom + GtkDockFrame.GroupDockSeparatorSize/2)
					{
						// Check if the item is allowed to be docked here
						int dn = dockObjects.IndexOf (ob) + 1;
						var s = Frame.Frontend.GetRegionStyleForPosition (group, dn, true);
						if (s.SingleRowMode.Value)
							return false;
						dockDelegate = delegate (DockItemBackend it) {
							DockTarget (it, dn);
						};
						rect = new Gdk.Rectangle (Allocation.X, ob.Allocation.Bottom - GtkDockFrame.GroupDockSeparatorSize/2, Allocation.Width, GtkDockFrame.GroupDockSeparatorSize);
						return true;
					}
					else if (ob.GetDockTarget (item, px, py, out dockDelegate, out rect))
						return true;
				}
			}
			dockDelegate = null;
			rect = Gdk.Rectangle.Zero;
			return false;
		}
		
		internal void Dump ()
		{
			Dump (0);
		}
		
		internal override void Dump (int ind)
		{
			Console.WriteLine (new string (' ', ind) + "Group (" + type + ") size:" + Size + " DefaultSize:" + DefaultSize + " alloc:" + Allocation);
			foreach (GtkDockObject ob in dockObjects) {
				ob.Dump (ind + 2);
			}
		}

		public bool IsChildNextToMargin (Gtk.PositionType margin, GtkDockObject obj, bool visibleOnly)
		{
			if (type == DockGroupType.Tabbed)
				return true;
			else if (type == DockGroupType.Horizontal) {
				if (margin == PositionType.Top || margin == PositionType.Bottom)
					return true;
				int i = visibleOnly ? Objects.IndexOf (obj) : Objects.IndexOf (obj);
				if (margin == PositionType.Left && i == 0)
					return true;
				if (margin == PositionType.Right && i == (visibleOnly ? Objects.Count - 1 : Objects.Count - 1))
					return true;
			}
			else if (type == DockGroupType.Vertical) {
				if (margin == PositionType.Left || margin == PositionType.Right)
					return true;
				int i = visibleOnly ? Objects.IndexOf (obj) : Objects.IndexOf (obj);
				if (margin == PositionType.Top && i == 0)
					return true;
				if (margin == PositionType.Bottom && i == (visibleOnly ? Objects.Count - 1 : Objects.Count - 1))
					return true;
			}
			return false;
		}
		
		internal TabStrip TabStrip {
			get { return boundTabStrip; }
		}
		
		public override string ToString ()
		{
			return "[GtkDockGroup " + type + "]";
		}
	}
}
