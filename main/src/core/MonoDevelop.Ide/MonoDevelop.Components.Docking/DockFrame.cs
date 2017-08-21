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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;
using Xwt.Motion;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components.Docking
{
	internal class DockFrame
	{
		public event EventHandler<EventArgs> LayoutChanged;

		internal const double ItemDockCenterArea = 0.4;
		internal const int GroupDockSeparatorSize = 40;
		internal bool ShadedSeparators = true;

		int handleSize = 1;
		int handlePadding = 0;
		int defaultItemWidth = 300;
		int defaultItemHeight = 250;
		uint autoShowDelay = 400;
		uint autoHideDelay = 500;

		SortedDictionary<string, DockLayout> layouts = new SortedDictionary<string, DockLayout> ();
		string currentLayout;
		int compactGuiLevel = 3;

		DockVisualStyle defaultStyle;

		public IDockFrameControl Control { get; private set; }

		public DockFrame ()
		{
			Control = new DockFrameControl (this);
			//Control = DesktopService.CreateNativeImplementation<IDockFrameControl> (this);

			Control.DockBarTop = new DockBar (this, PositionType.Top);
			Control.DockBarBottom = new DockBar (this, PositionType.Bottom);
			Control.DockBarLeft = new DockBar (this, PositionType.Left);
			Control.DockBarRight = new DockBar (this, PositionType.Right);
			Control.Container = new DockContainer (this);

			Control.Initialize (this);

			CompactGuiLevel = 2;
			DefaultVisualStyle = new DockVisualStyle ();
		}

		public DockVisualStyle DefaultVisualStyle {
			get {
				return defaultStyle;
			}
			set {
				defaultStyle = DockVisualStyle.CreateDefaultStyle ();
				defaultStyle.CopyValuesFrom (value);
			}
		}

		/// <summary>
		/// Compactness level of the gui, from 1 (not compact) to 5 (very compact).
		/// </summary>
		public int CompactGuiLevel {
			get { return compactGuiLevel; }
			set {
				compactGuiLevel = value;
				/*				switch (compactGuiLevel) {
									case 1: handleSize = 6; break;
									case 2:
									case 3: handleSize = IsWindows ? 4 : 6; break;
									case 4:
									case 5: handleSize = 3; break;
								}
				*/
				handlePadding = 0;

				Control.DockBarTop.OnCompactLevelChanged ();
				Control.DockBarBottom.OnCompactLevelChanged ();
				Control.DockBarLeft.OnCompactLevelChanged ();
				Control.DockBarRight.OnCompactLevelChanged ();

				Container.RelayoutUI ();
			}
		}

		// Registered region styles. We are using a list instead of a dictionary because
		// the registering order is important
		List<Tuple<string, DockVisualStyle>> regionStyles = new List<Tuple<string, DockVisualStyle>> ();

		// Styles specific to items
		Dictionary<string, DockVisualStyle> stylesById = new Dictionary<string, DockVisualStyle> ();

		/// <summary>
		/// Sets the style for a region of the dock frame
		/// </summary>
		/// <param name='regionPosition'>
		/// A region is a collection with the format: "ItemId1/Position1;ItemId2/Position2..."
		/// ItemId is the id of a dock item. Position is one of the values of the DockPosition enumeration
		/// </param>
		/// <param name='style'>
		/// Style.
		/// </param>
		public void SetRegionStyle (string regionPosition, DockVisualStyle style)
		{
			// Remove any old region style and add it
			regionStyles.RemoveAll (s => s.Item1 == regionPosition);
			if (style != null)
				regionStyles.Add (new Tuple<string, DockVisualStyle> (regionPosition, style));
		}

		public void SetDockItemStyle (string itemId, DockVisualStyle style)
		{
			if (style != null)
				stylesById [itemId] = style;
			else
				stylesById.Remove (itemId);
		}

		internal void UpdateRegionStyle (DockObject obj)
		{
			obj.VisualStyle = GetRegionStyleForObject (obj);
		}

		/// <summary>
		/// Gets the style for a dock object, which will inherit values from all region/style definitions
		/// </summary>
		internal DockVisualStyle GetRegionStyleForObject (DockObject obj)
		{
			DockVisualStyle mergedStyle = null;
			if (obj is DockGroupItem) {
				DockVisualStyle s;
				if (stylesById.TryGetValue (((DockGroupItem)obj).Id, out s)) {
					mergedStyle = DefaultVisualStyle.Clone ();
					mergedStyle.CopyValuesFrom (s);
				}
			}
			foreach (var e in regionStyles) {
				if (InRegion (e.Item1, obj)) {
					if (mergedStyle == null)
						mergedStyle = DefaultVisualStyle.Clone ();
					mergedStyle.CopyValuesFrom (e.Item2);
				}
			}
			return mergedStyle ?? DefaultVisualStyle;
		}

		internal DockVisualStyle GetRegionStyleForItem (DockItem item)
		{
			DockVisualStyle s;
			if (stylesById.TryGetValue (item.Id, out s)) {
				var ds = DefaultVisualStyle.Clone ();
				ds.CopyValuesFrom (s);
				return ds;
			}
			return DefaultVisualStyle;
		}

		/// <summary>
		/// Gets the style assigned to a specific position of the layout
		/// </summary>
		/// <returns>
		/// The region style for position.
		/// </returns>
		/// <param name='parentGroup'>
		/// Group which contains the position
		/// </param>
		/// <param name='childIndex'>
		/// Index of the position inside the group
		/// </param>
		/// <param name='insertingPosition'>
		/// If true, the position will be inserted (meaning that the objects in childIndex will be shifted 1 position)
		/// </param>
		internal DockVisualStyle GetRegionStyleForPosition (DockGroup parentGroup, int childIndex, bool insertingPosition)
		{
			DockVisualStyle mergedStyle = null;
			foreach (var e in regionStyles) {
				if (InRegion (e.Item1, parentGroup, childIndex, insertingPosition)) {
					if (mergedStyle == null)
						mergedStyle = DefaultVisualStyle.Clone ();
					mergedStyle.CopyValuesFrom (e.Item2);
				}
			}
			return mergedStyle ?? DefaultVisualStyle;
		}

		internal bool InRegion (string location, DockObject obj)
		{
			if (obj.ParentGroup == null)
				return false;
			return InRegion (location, obj.ParentGroup, obj.ParentGroup.GetObjectIndex (obj), false);
		}

		internal bool InRegion (string location, DockGroup objToFindParent, int objToFindIndex, bool insertingPosition)
		{
			// Checks if the object is in the specified region.
			// A region is a collection with the format: "ItemId1/Position1;ItemId2/Position2..."
			string [] positions = location.Split (';');
			foreach (string pos in positions) {
				// We individually check each entry in the region specification
				int i = pos.IndexOf ('/');
				if (i == -1) continue;
				string id = pos.Substring (0, i).Trim ();
				DockGroup g = Container.Layout.FindGroupContaining (id);
				if (g != null) {
					DockPosition dpos;
					try {
						dpos = (DockPosition)Enum.Parse (typeof (DockPosition), pos.Substring (i + 1).Trim (), true);
					} catch {
						continue;
					}

					var refItem = g.FindDockGroupItem (id);
					if (InRegion (g, dpos, refItem, objToFindParent, objToFindIndex, insertingPosition))
						return true;
				}
			}
			return false;
		}

		bool InRegion (DockGroup grp, DockPosition pos, DockObject refObject, DockGroup objToFindParent, int objToFindIndex, bool insertingPosition)
		{
			if (grp == null)
				return false;

			if (grp.Type == DockGroupType.Tabbed) {
				if (pos != DockPosition.Center && pos != DockPosition.CenterBefore)
					return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
			}
			if (grp.Type == DockGroupType.Horizontal) {
				if (pos != DockPosition.Left && pos != DockPosition.Right)
					return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
			}
			if (grp.Type == DockGroupType.Vertical) {
				if (pos != DockPosition.Top && pos != DockPosition.Bottom)
					return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
			}

			bool foundAtLeftSide = true;
			bool findingLeft = pos == DockPosition.Left || pos == DockPosition.Top || pos == DockPosition.CenterBefore;

			if (objToFindParent == grp) {
				// Check positions beyond the current range of items
				if (objToFindIndex < 0 && findingLeft)
					return true;
				if (objToFindIndex >= grp.Objects.Count && !findingLeft)
					return true;
			}

			for (int n = 0; n < grp.Objects.Count; n++) {
				var ob = grp.Objects [n];

				bool foundRefObject = ob == refObject;
				bool foundTargetObject = objToFindParent == grp && objToFindIndex == n;

				if (foundRefObject) {
					// Found the reference object, but if insertingPosition=true it is in the position that the new item will have,
					// so this position still has to be considered to be at the left side
					if (foundTargetObject && insertingPosition)
						return foundAtLeftSide == findingLeft;
					foundAtLeftSide = false;
				} else if (foundTargetObject)
					return foundAtLeftSide == findingLeft;
				else if (ob is DockGroup) {
					DockGroup gob = (DockGroup)ob;
					if (gob == objToFindParent || ObjectHasAncestor (objToFindParent, gob))
						return foundAtLeftSide == findingLeft;
				}
			}
			return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
		}

		bool ObjectHasAncestor (DockObject obj, DockGroup ancestorToFind)
		{
			return obj != null && (obj.ParentGroup == ancestorToFind || ObjectHasAncestor (obj.ParentGroup, ancestorToFind));
		}

		internal DockContainer Container {
			get { return Control.Container; }
		}

		public int HandleSize {
			get {
				return handleSize;
			}
			set {
				handleSize = value;
			}
		}

		public int HandlePadding {
			get {
				return handlePadding;
			}
			set {
				handlePadding = value;
			}
		}

		public int DefaultItemWidth {
			get {
				return defaultItemWidth;
			}
			set {
				defaultItemWidth = value;
			}
		}

		public int DefaultItemHeight {
			get {
				return defaultItemHeight;
			}
			set {
				defaultItemHeight = value;
			}
		}

		internal int TotalHandleSize {
			get { return handleSize + handlePadding * 2; }
		}

		internal int TotalSensitiveHandleSize {
			get { return 6; }
		}

		public DockItem AddItem (string id)
		{
			foreach (DockItem dit in Container.Items) {
				if (dit.Id == id) {
					if (dit.IsPositionMarker) {
						dit.IsPositionMarker = false;
						return dit;
					}
					throw new InvalidOperationException ("An item with id '" + id + "' already exists.");
				}
			}

			DockItem it = new DockItem (this, id);
			Container.Items.Add (it);
			return it;
		}

		public void RemoveItem (DockItem it)
		{
			if (Container.Layout != null)
				Container.Layout.RemoveItemRec (it);
			foreach (DockGroup grp in layouts.Values)
				grp.RemoveItemRec (it);
			Container.Items.Remove (it);
		}

		public DockItem GetItem (string id)
		{
			foreach (DockItem it in Container.Items) {
				if (it.Id == id) {
					if (!it.IsPositionMarker)
						return it;
					else
						return null;
				}
			}
			return null;
		}

		public IEnumerable<DockItem> GetItems ()
		{
			return Container.Items;
		}

		bool LoadLayout (string layoutName)
		{
			DockLayout dl;
			if (!layouts.TryGetValue (layoutName, out dl))
				return false;

			Control.SwitchLayout (dl);

			return true;
		}

		public void CreateLayout (string name)
		{
			CreateLayout (name, false);
		}

		public void DeleteLayout (string name)
		{
			layouts.Remove (name);
		}

		public void CreateLayout (string name, bool copyCurrent)
		{
			DockLayout dl;
			if (Container.Layout == null || !copyCurrent) {
				dl = GetDefaultLayout ();
			} else {
				Container.StoreAllocation ();
				dl = (DockLayout)Container.Layout.Clone ();
			}
			dl.Name = name;
			layouts [name] = dl;
		}

		public string CurrentLayout {
			get {
				return currentLayout;
			}
			set {
				if (currentLayout == value)
					return;
				if (LoadLayout (value)) {
					currentLayout = value;
				}
			}
		}

		public bool HasLayout (string id)
		{
			return layouts.ContainsKey (id);
		}

		public string [] Layouts {
			get {
				if (layouts.Count == 0)
					return new string [0];
				string [] arr = new string [layouts.Count];
				layouts.Keys.CopyTo (arr, 0);
				return arr;
			}
		}

		public uint AutoShowDelay {
			get {
				return autoShowDelay;
			}
			set {
				autoShowDelay = value;
			}
		}

		public uint AutoHideDelay {
			get {
				return autoHideDelay;
			}
			set {
				autoHideDelay = value;
			}
		}

		public void SaveLayouts (string file)
		{
			using (XmlTextWriter w = new XmlTextWriter (file, System.Text.Encoding.UTF8)) {
				w.Formatting = Formatting.Indented;
				SaveLayouts (w);
			}
		}

		public void SaveLayouts (XmlWriter writer)
		{
			if (Container.Layout != null)
				Container.Layout.StoreAllocation ();
			writer.WriteStartElement ("layouts");
			foreach (DockLayout la in layouts.Values)
				la.Write (writer);
			writer.WriteEndElement ();
		}

		public void LoadLayouts (string file)
		{
			using (XmlReader r = new XmlTextReader (new System.IO.StreamReader (file))) {
				LoadLayouts (r);
			}
		}

		public void LoadLayouts (XmlReader reader)
		{
			layouts.Clear ();
			Container.Clear ();
			currentLayout = null;

			reader.MoveToContent ();
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}
			reader.ReadStartElement ("layouts");
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					DockLayout layout = DockLayout.Read (this, reader);
					layout.AllocationChanged += LayoutAllocationChanged;
					layouts.Add (layout.Name, layout);
				} else
					reader.Skip ();
				reader.MoveToContent ();
			}
			reader.ReadEndElement ();
			Container.RelayoutUI ();
		}

		void LayoutAllocationChanged (object sender, EventArgs e)
		{
			LayoutChanged?.Invoke (this, EventArgs.Empty);
		}

		internal void UpdateTitle (DockItem item)
		{
			DockGroupItem gitem = Container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;

			gitem.ParentGroup.UpdateTitle (item);

			Control.DockBarTop.UpdateTitle (item);
			Control.DockBarBottom.UpdateTitle (item);
			Control.DockBarLeft.UpdateTitle (item);
			Control.DockBarRight.UpdateTitle (item);
		}

		internal void UpdateStyles ()
		{
			Container.ReloadStyles ();
		}

		internal void UpdateStyle (DockItem item)
		{
			DockGroupItem gitem = Container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;

			gitem.ParentGroup.UpdateStyle (item);

			Control.DockBarTop.UpdateStyle (item);
			Control.DockBarBottom.UpdateStyle (item);
			Control.DockBarLeft.UpdateStyle (item);
			Control.DockBarRight.UpdateStyle (item);
		}

		internal void Present (DockItem item, bool giveFocus)
		{
			DockGroupItem gitem = Container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;

			gitem.ParentGroup.Present (item, giveFocus);
		}

		public DockBar ExtractDockBar (PositionType pos)
		{
			DockBar db = new DockBar (this, pos);
			switch (pos) {
			case PositionType.Left: db.OriginalBar = Control.DockBarLeft; Control.DockBarLeft = db; break;
			case PositionType.Top: db.OriginalBar = Control.DockBarTop; Control.DockBarTop = db; break;
			case PositionType.Right: db.OriginalBar = Control.DockBarRight; Control.DockBarRight = db; break;
			case PositionType.Bottom: db.OriginalBar = Control.DockBarBottom; Control.DockBarBottom = db; break;
			}
			return db;
		}

		public DockBar GetDockBar (PositionType pos)
		{
			switch (pos) {
			case Gtk.PositionType.Top: return Control.DockBarTop;
			case Gtk.PositionType.Bottom: return Control.DockBarBottom;
			case Gtk.PositionType.Left: return Control.DockBarLeft;
			case Gtk.PositionType.Right: return Control.DockBarRight;
			}
			return null;
		}

		internal bool GetVisible (DockItem item)
		{
			DockGroupItem gitem = Container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return false;
			return gitem.VisibleFlag;
		}

		internal bool GetVisible (DockItem item, string layoutName)
		{
			DockLayout dl;
			if (!layouts.TryGetValue (layoutName, out dl))
				return false;

			DockGroupItem gitem = dl.FindDockGroupItem (item.Id);
			if (gitem == null)
				return false;
			return gitem.VisibleFlag;
		}

		internal void SetVisible (DockItem item, bool visible)
		{
			if (Container.Layout == null)
				return;
			DockGroupItem gitem = Container.FindDockGroupItem (item.Id);

			if (gitem == null) {
				if (visible) {
					// The item is not present in the layout. Add it now.
					if (!string.IsNullOrEmpty (item.DefaultLocation))
						gitem = AddDefaultItem (Container.Layout, item);

					if (gitem == null) {
						// No default position
						gitem = new DockGroupItem (this, item);
						Container.Layout.AddObject (gitem);
					}
				} else
					return; // Already invisible
			}
			gitem.SetVisible (visible);
			Container.RelayoutUI ();
		}

		internal DockItemStatus GetStatus (DockItem item)
		{
			DockGroupItem gitem = Container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return DockItemStatus.Dockable;
			return gitem.Status;
		}

		internal void SetStatus (DockItem item, DockItemStatus status)
		{
			DockGroupItem gitem = Container.FindDockGroupItem (item.Id);
			if (gitem == null) {
				item.DefaultStatus = status;
				return;
			}
			gitem.StoreAllocation ();
			gitem.Status = status;
			Container.RelayoutUI();
		}

		internal void SetDockLocation (DockItem item, string placement)
		{
			bool vis = item.Visible;
			DockItemStatus stat = item.Status;
			item.ResetMode ();
			Container.Layout.RemoveItemRec (item);
			AddItemAtLocation (Container.Layout, item, placement, vis, stat);
		}

		DockLayout GetDefaultLayout ()
		{
			DockLayout group = new DockLayout (this);

			// Add items which don't have relative defaut positions

			List<DockItem> todock = new List<DockItem> ();
			foreach (DockItem item in Container.Items) {
				if (string.IsNullOrEmpty (item.DefaultLocation)) {
					DockGroupItem dgt = new DockGroupItem (this, item);
					dgt.SetVisible (item.DefaultVisible);
					group.AddObject (dgt);
				} else
					todock.Add (item);
			}

			// Add items with relative positions.
			int lastCount = 0;
			while (lastCount != todock.Count) {
				lastCount = todock.Count;
				for (int n = 0; n < todock.Count; n++) {
					DockItem it = todock [n];
					if (AddDefaultItem (group, it) != null) {
						todock.RemoveAt (n);
						n--;
					}
				}
			}

			// Items which could not be docked because of an invalid default location
			foreach (DockItem item in todock) {
				DockGroupItem dgt = new DockGroupItem (this, item);
				dgt.SetVisible (false);
				group.AddObject (dgt);
			}
			//			group.Dump ();
			return group;
		}

		DockGroupItem AddDefaultItem (DockGroup grp, DockItem it)
		{
			return AddItemAtLocation (grp, it, it.DefaultLocation, it.DefaultVisible, it.DefaultStatus);
		}

		DockGroupItem AddItemAtLocation (DockGroup grp, DockItem it, string location, bool visible, DockItemStatus status)
		{
			string [] positions = location.Split (';');
			foreach (string pos in positions) {
				int i = pos.IndexOf ('/');
				if (i == -1) continue;
				string id = pos.Substring (0, i).Trim ();
				DockGroup g = grp.FindGroupContaining (id);
				if (g != null) {
					DockPosition dpos;
					try {
						dpos = (DockPosition)Enum.Parse (typeof (DockPosition), pos.Substring (i + 1).Trim (), true);
					} catch {
						continue;
					}
					DockGroupItem dgt = g.AddObject (it, dpos, id);
					dgt.SetVisible (visible);
					dgt.Status = status;
					return dgt;
				}
			}
			return null;
		}

		internal void MinimizeAllAutohidden ()
		{
			foreach (var it in GetItems ()) {
				if (it.Visible && it.Status == DockItemStatus.AutoHide)
					it.Minimize ();
			}
		}

		public void ShowPlaceholder (DockItem draggedItem)
		{
			Container.ShowPlaceholder (draggedItem);
		}

		public void DockInPlaceholder (DockItem item)
		{
			Container.DockInPlaceholder (item);
		}

		public void HidePlaceholder ()
		{
			Container.HidePlaceholder ();
		}

		public void UpdatePlaceholder (DockItem item, Gdk.Size size, bool allowDocking)
		{
			Container.UpdatePlaceholder (item, size, allowDocking);
		}

		public DockBarItem BarDock (Gtk.PositionType pos, DockItem item, int size)
		{
			return GetDockBar (pos).AddItem (item, size);
		}

		public bool ChildFocus (Gtk.DirectionType direction)
		{
			return Control.ChildFocus (direction);
		}

		public Gdk.Rectangle Allocation {
			get {
				return Control.Allocation;
			}
			set {
				Control.Allocation = value;
			}
		}
	}

	internal interface IDockFrameControl : IAnimatable
	{
		void Initialize (DockFrame frame);

		DockFrame Frame { get; }
		bool DockbarsVisible { get; }
		bool UseWindowsForTopLevelFrames { get; }

		bool OverlayWidgetVisible { get; }

		DockBar DockBarTop { get; set; }
		DockBar DockBarBottom { get; set; }
		DockBar DockBarLeft { get; set; }
		DockBar DockBarRight { get; set; }
		DockContainer Container { get; set; }

		void SwitchLayout (DockLayout dl);

		void AddOverlayWidget (Widget widget, bool animate = false);
		void RemoveOverlayWidget (bool animate = false);

		void AddTopLevel (DockFrameTopLevel w, int x, int y, int width, int height);
		void RemoveTopLevel (DockFrameTopLevel w);

		Gdk.Rectangle GetCoordinates (Gtk.Widget w);

		AutoHideBox AutoShow (DockItem item, DockBar bar, int size);
		void UpdateSize (DockBar bar, AutoHideBox aframe);
		void AutoHide (DockItem item, AutoHideBox widget, bool animate);

		bool ChildFocus (Gtk.DirectionType direction);

		Gdk.Rectangle Allocation { get; set; }
	}

	internal class DockFrameControl: HBox, IDockFrameControl
	{
		public DockFrame Frame { get; private set; }
		DockContainer container;

		List<DockFrameTopLevel> topLevels = new List<DockFrameTopLevel> ();

		DockBar dockBarTop, dockBarBottom, dockBarLeft, dockBarRight;
		VBox mainBox;
		Gtk.Widget overlayWidget;

		public DockFrameControl (DockFrame frame)
		{
			GtkWorkarounds.FixContainerLeak (this);

			Accessible.Name = "DockFrame";
		}

		public void Initialize (DockFrame frame)
		{
			Frame = frame;

			if (dockBarTop == null || dockBarBottom == null ||
			    dockBarLeft == null || dockBarRight == null ||
			    container == null) {
				throw new Exception ("Missing a dockbar");
			}

			HBox hbox = new HBox ();
			hbox.Accessible.SetShouldIgnore (true);

			hbox.PackStart ((Widget)dockBarLeft.Control, false, false, 0);

			var containerWidget = container.Control as Widget;
			if (containerWidget == null) {
				throw new ToolkitMismatchException ();
			}

			hbox.PackStart (containerWidget, true, true, 0);
			hbox.PackStart ((Widget)dockBarRight.Control, false, false, 0);
			mainBox = new VBox ();
			mainBox.Accessible.SetShouldIgnore (true);

			mainBox.PackStart ((Widget)dockBarTop.Control, false, false, 0);
			mainBox.PackStart (hbox, true, true, 0);
			mainBox.PackStart ((Widget)dockBarBottom.Control, false, false, 0);
			Add (mainBox);
			mainBox.ShowAll ();
			mainBox.NoShowAll = true;

			UpdateDockbarsVisibility ();
		}

		public DockBar DockBarLeft {
			get {
				return dockBarLeft;
			}

			set {
				dockBarLeft = value;
			}
		}

		public DockBar DockBarRight {
			get {
				return dockBarRight;
			}

			set {
				dockBarRight = value;
			}
		}

		public DockBar DockBarTop {
			get {
				return dockBarTop;
			}

			set {
				dockBarTop = value;
			}
		}

		public DockBar DockBarBottom {
			get {
				return dockBarBottom;
			}

			set {
				dockBarBottom = value;
			}
		}

		public bool DockbarsVisible {
			get {
				return !OverlayWidgetVisible;
			}
		}

		public bool UseWindowsForTopLevelFrames {
			get { return Platform.IsMac; }
		}

		public void SwitchLayout (DockLayout dl)
		{
			var focus = GetActiveWidget ();

			container.LoadLayout (dl);

			// Keep the currently focused widget when switching layouts
			if (focus != null && focus.IsRealized && focus.Visible)
				GtkUtil.SetFocus (focus);
		}

		public bool OverlayWidgetVisible { get; private set; }

		protected override void OnDestroyed ()
		{
			this.AbortAnimation ("ShowOverlayWidget");
			this.AbortAnimation ("HideOverlayWidget");
			base.OnDestroyed ();
		}

		public void AddOverlayWidget (Widget widget, bool animate = false)
		{
			RemoveOverlayWidget (false);

			this.overlayWidget = widget;
			widget.Parent = this;

			// Emit the add signal so that the A11y system will pick up that a widget has been added to the box
			// but the box won't handle it because widget.Parent has already been set.
			GtkWorkarounds.EmitAddSignal(this, widget);

			OverlayWidgetVisible = true;
			Frame.MinimizeAllAutohidden ();
			if (animate) {
				currentOverlayPosition = Math.Max (0, Allocation.Y + Allocation.Height);
				this.Animate (
					"ShowOverlayWidget",
					ShowOverlayWidgetAnimation,
					finished: (a, b) => {
						mainBox.Hide ();
					},
					easing: Easing.CubicOut);
			} else {
				currentOverlayPosition = Math.Max (0, Allocation.Y);
				mainBox.Hide ();
				QueueResize ();
			}

			UpdateDockbarsVisibility ();
		}

		public void RemoveOverlayWidget (bool animate = false)
		{
			this.AbortAnimation ("ShowOverlayWidget");
			this.AbortAnimation ("HideOverlayWidget");
			OverlayWidgetVisible = false;

			mainBox.Show ();

			if (overlayWidget != null) {
				if (animate) {
					currentOverlayPosition = Allocation.Y;
					this.Animate (
						"HideOverlayWidget",
						HideOverlayWidgetAnimation,
						finished: (a,b) => {
							if (overlayWidget != null) {
								overlayWidget.Unparent ();

								// After we've unparented the widget, we call remove so the A11y system can clean up as well.
								GtkWorkarounds.EmitRemoveSignal(this, overlayWidget);
								overlayWidget = null;
							}
						},
						easing: Easing.SinOut);
				} else {
					overlayWidget.Unparent ();
					// After we've unparented the widget, we call remove so the A11y system can clean up as well.
					GLib.Signal.Emit (this, "remove", overlayWidget);

					overlayWidget = null;
					QueueResize ();
				}
			}

			UpdateDockbarsVisibility ();
		}

		int currentOverlayPosition;

		void UpdateDockbarsVisibility ()
		{
			dockBarTop.UpdateVisibility ();
			dockBarBottom.UpdateVisibility ();
			dockBarLeft.UpdateVisibility ();
			dockBarRight.UpdateVisibility ();
		}

		void ShowOverlayWidgetAnimation (double value)
		{
			currentOverlayPosition = Allocation.Y + (int)((double)Allocation.Height * (1f - value));
			overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, Allocation.Width, Allocation.Height));
		}

		void HideOverlayWidgetAnimation (double value)
		{
			currentOverlayPosition = Allocation.Y + (int)((double)Allocation.Height * value);
			overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, Allocation.Width, Allocation.Height));
		}

		void IAnimatable.BatchBegin ()
		{
		}

		void IAnimatable.BatchCommit ()
		{
		}

		public DockContainer Container {
			get { return container; }
			set { container = value; }
		}

		Gtk.Widget GetActiveWidget ()
		{
			Gtk.Widget widget = this;
			while (widget is Gtk.Container) {
				Gtk.Widget child = ((Gtk.Container)widget).FocusChild;
				if (child != null)
					widget = child;
				else
					break;
			}
			return widget;
		}
		
		public void AddTopLevel (DockFrameTopLevel w, int x, int y, int width, int height)
		{
			w.X = x;
			w.Y = y;

			DockFrameTopLevelControl toplevelControl = w.Control as DockFrameTopLevelControl;

			if (UseWindowsForTopLevelFrames) {
				var win = new IdeWindow (Gtk.WindowType.Toplevel);
				win.SkipTaskbarHint = true;
				win.Decorated = false;
				win.TypeHint = Gdk.WindowTypeHint.Toolbar;
				toplevelControl.ContainerWindow = win;
				w.Size = new Size (width, height);
				win.Add (toplevelControl);
				toplevelControl.Show ();
				var p = this.GetScreenCoordinates (new Gdk.Point (x, y));
				win.Opacity = 0.0;
				win.Move (p.X, p.Y);
				win.Resize (width, height);
				win.Show ();
				Ide.DesktopService.AddChildWindow ((Gtk.Window)Toplevel, win);
				win.AcceptFocus = true;
				win.Opacity = 1.0;

				/* When we use real windows for frames, it's possible for pads to be over other
				 * windows. For some reason simply presenting or raising those dialogs doesn't
				 * seem to work, so we hide/show them in order to force them above the pad. */
				var toplevels = Gtk.Window.ListToplevels ().Where (t => t.IsRealized && t.Visible && t.TypeHint == WindowTypeHint.Dialog); // && t.TransientFor != null);
				foreach (var t in toplevels) {
					t.Hide ();
					t.Show ();
				}

				MonoDevelop.Ide.IdeApp.CommandService.RegisterTopWindow (win);
			} else {
				toplevelControl.Parent = this;
				w.Size = new Size (width, height);
				Requisition r = toplevelControl.SizeRequest ();
				toplevelControl.Allocation = new Gdk.Rectangle (Allocation.X + x, Allocation.Y + y, r.Width, r.Height);
				topLevels.Add (w);
			}
		}
		
		public void RemoveTopLevel (DockFrameTopLevel w)
		{
			DockFrameTopLevelControl toplevelControl = w.Control as DockFrameTopLevelControl;

			toplevelControl.Unparent ();
			topLevels.Remove (w);
			QueueResize ();
		}
		
		public Gdk.Rectangle GetCoordinates (Gtk.Widget w)
		{
			int px, py;
			if (!w.TranslateCoordinates (this, 0, 0, out px, out py))
				return new Gdk.Rectangle (0,0,0,0);

			Gdk.Rectangle rect = w.Allocation;
			rect.X = px - Allocation.X;
			rect.Y = py - Allocation.Y;
			return rect;
		}
		
		public AutoHideBox AutoShow (DockItem item, DockBar bar, int size)
		{
			AutoHideBox aframe = new AutoHideBox (Frame, item, bar.Position, size);
			Gdk.Size sTop = GetBarFrameSize (dockBarTop);
			Gdk.Size sBot = GetBarFrameSize (dockBarBottom);
			Gdk.Size sLeft = GetBarFrameSize (dockBarLeft);
			Gdk.Size sRgt = GetBarFrameSize (dockBarRight);

			int x,y,w,h;
			if (bar == dockBarLeft || bar == dockBarRight) {
				h = Allocation.Height - sTop.Height - sBot.Height;
				w = size;
				y = sTop.Height;
				if (bar == dockBarLeft)
					x = sLeft.Width;
				else
					x = Allocation.Width - size - sRgt.Width;
			} else {
				w = Allocation.Width - sLeft.Width - sRgt.Width;
				h = size;
				x = sLeft.Width;
				if (bar == dockBarTop)
					y = sTop.Height;
				else
					y = Allocation.Height - size - sBot.Height;
			}

			AddTopLevel (aframe, x, y, w, h);
			aframe.AnimateShow ();

			return aframe;
		}
		
		public void UpdateSize (DockBar bar, AutoHideBox aframe)
		{
			Gdk.Size sTop = GetBarFrameSize (dockBarTop);
			Gdk.Size sBot = GetBarFrameSize (dockBarBottom);
			Gdk.Size sLeft = GetBarFrameSize (dockBarLeft);
			Gdk.Size sRgt = GetBarFrameSize (dockBarRight);

			var widget = (Widget)aframe.Control;
			if (bar == dockBarLeft || bar == dockBarRight) {
				widget.HeightRequest = Allocation.Height - sTop.Height - sBot.Height;
				if (bar == dockBarRight)
					aframe.X = Allocation.Width - widget.Allocation.Width - sRgt.Width;
			} else {
				widget.WidthRequest = Allocation.Width - sLeft.Width - sRgt.Width;
				if (bar == dockBarBottom)
					aframe.Y = Allocation.Height - widget.Allocation.Height - sBot.Height;
			}
		}
		
		Gdk.Size GetBarFrameSize (DockBar bar)
		{
			if (bar.OriginalBar != null)
				bar = bar.OriginalBar;
			if (!((Widget)bar.Control).Visible)
				return new Gdk.Size (0,0);
			Gtk.Requisition req = ((Widget)bar.Control).SizeRequest ();
			return new Gdk.Size (req.Width, req.Height);
		}
		
		public void AutoHide (DockItem item, AutoHideBox widget, bool animate)
		{
			var ahbWidget = widget.Control as Widget;
			if (animate) {
				ahbWidget.Hidden += delegate {
					if (!widget.Disposed)
						AutoHide (item, widget, false);
				};
				widget.AnimateHide ();
			}
			else {
				var realWidget = (Widget)item.Widget.Control;
				// The widget may already be removed from the parent
				// so 'parent' can be null
				Gtk.Container parent = (Gtk.Container) realWidget.Parent;
				if (parent != null) {
					//removing the widget from its parent causes it to unrealize without unmapping
					//so make sure it's unmapped
					if (realWidget.IsMapped) {
						realWidget.Unmap ();
					}
					item.Widget.RemoveFromParent();
				}
				var w = item.TitleTab.Control as Widget;
				if (w == null) {
					throw new ToolkitMismatchException ();
				}

				parent = (Gtk.Container) w.Parent;
				if (parent != null) {
					//removing the widget from its parent causes it to unrealize without unmapping
					//so make sure it's unmapped
					if (w.IsMapped) {
						w.Unmap ();
					}
					parent.Remove (w);
				}

				var control = widget.Control as AutoHideBoxControl;
				if (control.ContainerWindow != null) {
					control.ContainerWindow.Destroy ();
				} else
					RemoveTopLevel (widget);

				widget.Disposed = true;
				ahbWidget.Destroy ();
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (overlayWidget != null)
				overlayWidget.SizeRequest ();
			base.OnSizeRequested (ref requisition);
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			foreach (DockFrameTopLevel tl in topLevels) {
				var widget = tl.Control as Widget;
				Requisition r = widget.SizeRequest ();
				widget.SizeAllocate (new Gdk.Rectangle (allocation.X + tl.X, allocation.Y + tl.Y, r.Width, r.Height));
			}
			if (overlayWidget != null)
				overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, allocation.Width, allocation.Height));
		}
		
		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			List<DockFrameTopLevel> clone = new List<DockFrameTopLevel> (topLevels);
			foreach (DockFrameTopLevel child in clone) {
				callback ((Widget)child.Control);
			}
			if (overlayWidget != null)
				callback (overlayWidget);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			Frame.MinimizeAllAutohidden ();
			return base.OnButtonPressEvent (evnt);
		}

		static internal bool IsWindows {
			get { return System.IO.Path.DirectorySeparatorChar == '\\'; }
		}

		internal static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color (color.Red / (double) ushort.MaxValue, color.Green / (double) ushort.MaxValue, color.Blue / (double) ushort.MaxValue);
		}

		protected override bool OnFocused (DirectionType direction)
		{
			// If there's an overlay widget, that's all we can focus
			if (overlayWidget != null && overlayWidget.Visible) {
				overlayWidget.ChildFocus (direction);
				return true;
			}

			return base.OnFocused (direction);
		}
	}

	public class DockStyle
	{
		public const string Default = "Default";
		public const string Browser = "Browser";
	}
	
	
	internal delegate void DockDelegate (DockItem item);
	
}
