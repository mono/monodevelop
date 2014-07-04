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
using MonoDevelop.Components.Docking.Internal;
using System.Linq;

namespace MonoDevelop.Components.Docking
{
	class DockFrame: Control, IDockFrameController
	{
		int defaultItemWidth = 300;
		int defaultItemHeight = 250;
		uint autoShowDelay = 400;
		uint autoHideDelay = 500;
		
		SortedDictionary<string,DockLayout> layouts = new SortedDictionary<string,DockLayout> ();
		string currentLayout;

		DockLayout layout;
		List<DockItem> items = new List<DockItem> ();
		IDockFrameBackend backend;

		DockVisualStyle defaultStyle;

		// Registered region styles. We are using a list instead of a dictionary because
		// the registering order is important
		List<Tuple<string,DockVisualStyle>> regionStyles = new List<Tuple<string, DockVisualStyle>> ();

		// Styles specific to items
		Dictionary<string,DockVisualStyle> stylesById = new Dictionary<string, DockVisualStyle> ();

		public DockFrame ()
		{
			DefaultVisualStyle = new DockVisualStyle ();
		}

		protected override object CreateNativeWidget ()
		{
			backend = new GtkDockFrame ();
			backend.Initialize (this);
			return (Gtk.Widget)backend;
		}

		public void AddOverlayWidget (Control widget, bool animate = false)
		{
			MinimizeAllAutohidden ();
			backend.AddOverlayWidget (widget, animate);
		}

		public void RemoveOverlayWidget (bool animate = false)
		{
			backend.RemoveOverlayWidget (animate);
		}

		internal IDockFrameBackend Backend {
			get { return backend; }
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
				regionStyles.Add (new Tuple<string,DockVisualStyle> (regionPosition, style));
		}

		public void SetDockItemStyle (string itemId, DockVisualStyle style)
		{
			if (style != null)
				stylesById [itemId] = style;
			else
				stylesById.Remove (itemId);
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

		DockVisualStyle IDockFrameController.GetRegionStyleForPosition (IDockGroup parentGroup, int childIndex, bool insertingPosition)
		{
			return GetRegionStyleForPosition ((DockGroup)parentGroup, childIndex, insertingPosition);
		}

		bool InRegion (string location, DockObject obj)
		{
			if (obj.ParentGroup == null)
				return false;
			return InRegion (location, obj.ParentGroup, obj.ParentGroup.GetObjectIndex (obj), false);
		}

		bool InRegion (string location, DockGroup objToFindParent, int objToFindIndex, bool insertingPosition)
		{
			// Checks if the object is in the specified region.
			// A region is a collection with the format: "ItemId1/Position1;ItemId2/Position2..."
			string[] positions = location.Split (';');
			foreach (string pos in positions) {
				// We individually check each entry in the region specification
				int i = pos.IndexOf ('/');
				if (i == -1) continue;
				string id = pos.Substring (0,i).Trim ();
				DockGroup g = layout.FindGroupContaining (id);
				if (g != null) {
					DockPosition dpos;
					try {
						dpos = (DockPosition) Enum.Parse (typeof(DockPosition), pos.Substring(i+1).Trim(), true);
					}
					catch {
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
				if (pos != DockPosition.Center &&  pos != DockPosition.CenterBefore)
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

			for (int n=0; n<grp.Objects.Count; n++) {
				var ob = grp.Objects[n];

				bool foundRefObject = ob == refObject;
				bool foundTargetObject = objToFindParent == grp && objToFindIndex == n;

				if (foundRefObject) {
					// Found the reference object, but if insertingPosition=true it is in the position that the new item will have,
					// so this position still has to be considered to be at the left side
					if (foundTargetObject && insertingPosition)
						return foundAtLeftSide == findingLeft;
					foundAtLeftSide = false;
				}
				else if (foundTargetObject)
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
		
		DockGroupItem FindDockGroupItem (string id)
		{
			if (layout == null)
				return null;
			else
				return layout.FindDockGroupItem (id);
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
		
		public DockItem AddItem (string id)
		{
			foreach (DockItem dit in items) {
				if (dit.Id == id) {
					if (dit.IsPositionMarker) {
						dit.IsPositionMarker = false;
						return dit;
					}
					throw new InvalidOperationException ("An item with id '" + id + "' already exists.");
				}
			}
			
			DockItem it = new DockItem (this, id);
			it.Init (backend.CreateItemBackend (it));
			items.Add (it);
			return it;
		}
		
		public void RemoveItem (DockItem it)
		{
			if (layout != null)
				layout.RemoveItemRec (it);
			foreach (DockGroup grp in layouts.Values)
				grp.RemoveItemRec (it);
			items.Remove (it);
			DoPendingRelayout ();
		}
		
		public DockItem GetItem (string id)
		{
			foreach (DockItem it in items) {
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
			return items;
		}
		
		bool LoadLayout (string layoutName)
		{
			DockLayout dl;
			if (!layouts.TryGetValue (layoutName, out dl))
				return false;

			// Make sure items not present in this layout are hidden
			foreach (var it in items) {
				if ((it.Behavior & DockItemBehavior.Sticky) != 0)
					it.Visible = it.StickyVisible;
				if (dl.FindDockGroupItem (it.Id) == null)
					it.HideWidget ();
			}

			dl.RestoreAllocation ();
			dl.UpdateStyle ();

			layout = dl;

			backend.LoadLayout (dl);

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
			if (layout == null || !copyCurrent) {
				dl = GetDefaultLayout ();
			} else {
				layout.StoreAllocation ();
				dl = (DockLayout) layout.Clone ();
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
		
		public string[] Layouts {
			get {
				if (layouts.Count == 0)
					return new string [0];
				string[] arr = new string [layouts.Count];
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
			if (layout != null)
				layout.StoreAllocation ();
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
					layouts.Add (layout.Name, layout);
				}
				else
					reader.Skip ();
				reader.MoveToContent ();
			}
			reader.ReadEndElement ();
		}

		internal bool GetVisible (DockItem item)
		{
			DockGroupItem gitem = FindDockGroupItem (item.Id);
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
			if (layout == null)
				return;
			DockGroupItem gitem = FindDockGroupItem (item.Id);
			
			if (gitem == null) {
				if (visible) {
					// The item is not present in the layout. Add it now.
					if (!string.IsNullOrEmpty (item.DefaultLocation))
						gitem = AddDefaultItem (layout, item);
						
					if (gitem == null) {
						// No default position
						gitem = new DockGroupItem (this, item);
						layout.AddObject (gitem);
					}
				} else
					return; // Already invisible
			}
			gitem.SetVisible (visible);
			DoPendingRelayout ();
		}
		
		internal DockItemStatus GetStatus (DockItem item)
		{
			DockGroupItem gitem = FindDockGroupItem (item.Id);
			if (gitem == null)
				return DockItemStatus.Dockable;
			return gitem.Status;
		}
		
		internal void SetStatus (DockItem item, DockItemStatus status)
		{
			DockGroupItem gitem = FindDockGroupItem (item.Id);
			if (gitem == null) {
				item.DefaultStatus = status;
				return;
			}
			gitem.StoreAllocation ();
			gitem.Status = status;
			DoPendingRelayout ();
		}

		internal Xwt.Rectangle GetAllocation ()
		{
			return backend.GetAllocation ();
		}

		internal void SetDockLocation (DockItem item, string placement)
		{
			bool vis = item.Visible;
			DockItemStatus stat = item.Status;
			item.ResetMode ();
			layout.RemoveItemRec (item);
			AddItemAtLocation (layout, item, placement, vis, stat);
			DoPendingRelayout ();
		}
		
		DockLayout GetDefaultLayout ()
		{
			DockLayout group = new DockLayout (this);
			
			// Add items which don't have relative defaut positions
			
			List<DockItem> todock = new List<DockItem> ();
			foreach (DockItem item in items) {
				if (string.IsNullOrEmpty (item.DefaultLocation)) {
					DockGroupItem dgt = new DockGroupItem (this, item);
					dgt.SetVisible (item.DefaultVisible);
					group.AddObject (dgt);
				}
				else
					todock.Add (item);
			}
			
			// Add items with relative positions.
			int lastCount = 0;
			while (lastCount != todock.Count) {
				lastCount = todock.Count;
				for (int n=0; n<todock.Count; n++) {
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
			string[] positions = location.Split (';');
			foreach (string pos in positions) {
				int i = pos.IndexOf ('/');
				if (i == -1) continue;
				string id = pos.Substring (0,i).Trim ();
				DockGroup g = grp.FindGroupContaining (id);
				if (g != null) {
					DockPosition dpos;
					try {
						dpos = (DockPosition) Enum.Parse (typeof(DockPosition), pos.Substring(i+1).Trim(), true);
					}
					catch {
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

		void MinimizeAllAutohidden ()
		{
			foreach (var it in GetItems ()) {
				if (it.Visible && it.Status == DockItemStatus.AutoHide)
					it.Minimize ();
			}
		}

		void IDockFrameController.DockItem (DockItem item, IDockGroup group, IDockObject insertBeforeObject)
		{
			((DockGroup)group).DockTarget (item, insertBeforeObject);
			DoPendingRelayout ();
		}

		void IDockFrameController.DockItemRelative (DockItem item, IDockGroupItem targetPosition, DockPosition pos, string relItemId)
		{
			((DockGroupItem)targetPosition).ParentGroup.AddObject (item, pos, relItemId);
			DoPendingRelayout ();
		}

		List<DockObject> objectsToRealyout = new List<DockObject> ();

		internal void MarkForRelayout (DockObject ob)
		{
			if (ob.ParentLayout != layout)
				return;

			if (objectsToRealyout.Any (o => o == ob || IsAncestor (ob, o)))
				return;
			objectsToRealyout.RemoveAll (o => IsAncestor (o, ob));
			objectsToRealyout.Add (ob);
		}

		internal void DoPendingRelayout ()
		{
			foreach (var ob in objectsToRealyout.ToArray ())
				backend.Refresh (ob);
			objectsToRealyout.Clear ();
		}

		bool IsAncestor (DockObject ob, DockObject potentialAncestor)
		{
			ob = ob.ParentGroup;
			while (ob != null) {
				if (ob == potentialAncestor)
					return true;
				ob = ob.ParentGroup;
			}
			return false;
		}

		IEnumerable<IDockItemBackend> IDockFrameController.GetItemBackends ()
		{
			foreach (var it in items)
				yield return it.Backend;
		}
	}

	public class DockStyle
	{
		public const string Default = "Default";
		public const string Browser = "Browser";
	}
	
	//	internal delegate void DockDelegate (DockItem item);
	
}
