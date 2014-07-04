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
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components.Docking
{
	class DockGroup: DockObject, IDockGroup
	{
		DockGroupType type;
		List<DockObject> dockObjects = new List<DockObject> ();
		List<IDockObject> visibleObjects;
		int currentTabPage;

		public DockGroup (DockFrame frame, DockGroupType type): base (frame)
		{
			this.type = type;
		}

		internal DockGroup (DockFrame frame): base (frame)
		{
		}

		public DockGroupType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}

		public List<DockObject> Objects {
			get { return dockObjects; }
		}

		public void AddObject (DockObject obj)
		{
			obj.ParentGroup = this;
			dockObjects.Add (obj);
			ResetVisibleGroups ();
		}

		public DockGroupItem AddObject (DockItem obj, DockPosition pos, string relItemId)
		{
			int npos = -1;
			if (relItemId != null) {
				for (int n=0; n<dockObjects.Count; n++) {
					DockGroupItem it = dockObjects [n] as DockGroupItem;
					if (it != null && it.Id == relItemId)
						npos = n;
				}
			}

			if (npos == -1) {
				if (pos == DockPosition.Left || pos == DockPosition.Top)
					npos = 0;
				else
					npos = dockObjects.Count - 1;
			}

			DockGroupItem gitem = null;

			if (pos == DockPosition.Left || pos == DockPosition.Right) {
				if (type != DockGroupType.Horizontal)
					gitem = Split (DockGroupType.Horizontal, pos == DockPosition.Left, obj, npos);
				else
					gitem = InsertObject (obj, npos, pos);
			}
			else if (pos == DockPosition.Top || pos == DockPosition.Bottom) {
				if (type != DockGroupType.Vertical)
					gitem = Split (DockGroupType.Vertical, pos == DockPosition.Top, obj, npos);
				else
					gitem = InsertObject (obj, npos, pos);
			}
			else if (pos == DockPosition.CenterBefore || pos == DockPosition.Center) {
				if (type != DockGroupType.Tabbed)
					gitem = Split (DockGroupType.Tabbed, pos == DockPosition.CenterBefore, obj, npos);
				else {
					if (pos == DockPosition.Center)
						npos++;
					gitem = new DockGroupItem (Frame, obj);
					dockObjects.Insert (npos, gitem);
					gitem.ParentGroup = this;
				}
			}
			ResetVisibleGroups ();
			return gitem;
		}

		DockGroupItem InsertObject (DockItem obj, int npos, DockPosition pos)
		{
			if (pos == DockPosition.Bottom || pos == DockPosition.Right)
				npos++;

			DockGroupItem gitem = new DockGroupItem (Frame, obj);
			dockObjects.Insert (npos, gitem);
			gitem.ParentGroup = this;
			return gitem;
		}

		DockGroupItem Split (DockGroupType newType, bool addFirst, DockItem obj, int npos)
		{
			DockGroupItem item = new DockGroupItem (Frame, obj);

			if (npos == -1 || type == DockGroupType.Tabbed) {
				if (ParentGroup != null && ParentGroup.Type == newType) {
					// No need to split. Just add the new item as a sibling of this one.
					int i = ParentGroup.Objects.IndexOf (this);
					if (addFirst)
						ParentGroup.Objects.Insert (i, item);
					else
						ParentGroup.Objects.Insert (i+1, item);
					item.ParentGroup = ParentGroup;
					item.ResetDefaultSize ();
				}
				else {
					DockGroup grp = Copy ();
					dockObjects.Clear ();
					if (addFirst) {
						dockObjects.Add (item);
						dockObjects.Add (grp);
					} else {
						dockObjects.Add (grp);
						dockObjects.Add (item);
					}
					item.ParentGroup = this;
					item.ResetDefaultSize ();
					grp.ParentGroup = this;
					grp.ResetDefaultSize ();
					Type = newType;
				}
			}
			else {
				DockGroup grp = new DockGroup (Frame, newType);
				DockObject replaced = dockObjects[npos];
				if (addFirst) {
					grp.AddObject (item);
					grp.AddObject (replaced);
				} else {
					grp.AddObject (replaced);
					grp.AddObject (item);
				}
				grp.CopySizeFrom (replaced);
				dockObjects [npos] = grp;
				grp.ParentGroup = this;
			}
			return item;
		}

		public void ReplaceItem (DockObject ob1, DockObject ob2)
		{
			int i = dockObjects.IndexOf (ob1);
			dockObjects [i] = ob2;
			ob2.ParentGroup = this;
			ob2.ResetDefaultSize ();
			ob2.Size = ob1.Size;
			ob2.DefaultSize = ob1.DefaultSize;
		}

		internal DockGroup FindGroupContaining (string id)
		{
			DockGroupItem it = FindDockGroupItem (id);
			if (it != null)
				return it.ParentGroup;
			else
				return null;
		}

		internal DockGroupItem FindDockGroupItem (string id)
		{
			foreach (DockObject ob in dockObjects) {
				DockGroupItem it = ob as DockGroupItem;
				if (it != null && it.Id == id)
					return it;
				DockGroup g = ob as DockGroup;
				if (g != null) {
					it = g.FindDockGroupItem (id);
					if (it != null)
						return it;
				}
			}
			return null;
		}

		DockGroup Copy ()
		{
			DockGroup grp = new DockGroup (Frame, type);
			grp.dockObjects = new List<DockObject> (dockObjects);
			foreach (DockObject obj in grp.dockObjects)
				obj.ParentGroup = grp;

			grp.CopySizeFrom (this);
			return grp;
		}

		public int GetObjectIndex (DockObject obj)
		{
			for (int n=0; n<dockObjects.Count; n++) {
				if (dockObjects [n] == obj)
					return n;
			}
			return -1;
		}

		public bool RemoveItemRec (DockItem item)
		{
			foreach (DockObject ob in dockObjects) {
				if (ob is DockGroup) {
					if (((DockGroup)ob).RemoveItemRec (item))
						return true;
				} else {
					DockGroupItem dit = ob as DockGroupItem;
					if (dit != null && dit.Item == item) {
						Remove (ob);
						return true;
					}
				}
			}
			return false;
		}

		void MarkForRelayout ()
		{
			// TODO: batch updates
			UpdateStyle ();
			Frame.MarkForRelayout (this);
		}

		public void Remove (DockObject obj)
		{
			dockObjects.Remove (obj);
			Reduce ();
			obj.ParentGroup = null;
			visibleObjects = null;

			if (VisibleObjects.Count > 0) {
				MarkForRelayout ();
			} else
				ParentGroup.UpdateVisible (this);
		}

		public void Reduce ()
		{
			if (ParentGroup != null && dockObjects.Count == 1) {
				DockObject obj = dockObjects [0];
				int n = ParentGroup.GetObjectIndex (this);
				ParentGroup.dockObjects [n] = obj;
				obj.ParentGroup = ParentGroup;
				obj.CopySizeFrom (this);
				dockObjects.Clear ();
				ResetVisibleGroups ();
				ParentGroup.ResetVisibleGroups ();
			}
		}

		IEnumerable<IDockObject> IDockGroup.VisibleObjects {
			get { return VisibleObjects; }
		}

		internal List<IDockObject> VisibleObjects {
			get {
				if (visibleObjects == null) {
					visibleObjects = new List<IDockObject> ();
					foreach (DockObject obj in dockObjects)
						if (obj.Visible)
							visibleObjects.Add (obj);
				}
				return visibleObjects;
			}
		}

		void ResetVisibleGroups ()
		{
			visibleObjects = null;
			MarkForRelayout ();
		}

		internal void UpdateVisible (DockObject child)
		{
			visibleObjects = null;
			bool visChanged;

			visChanged = child.Visible ? VisibleObjects.Count == 1 : VisibleObjects.Count == 0;

			if (visChanged && ParentGroup != null)
				ParentGroup.UpdateVisible (this);

			MarkForRelayout ();
		}

		public override bool Expand {
			get {
				foreach (DockObject ob in dockObjects)
					if (ob.Expand)
						return true;
				return false;
			}
		}

		internal override void GetDefaultSize (out int width, out int height)
		{
			width = height = 0;
		}


		public override void CopyFrom (DockObject other)
		{
			base.CopyFrom (other);
			DockGroup grp = (DockGroup) other;
			dockObjects = new List<DockObject> ();
			foreach (DockObject ob in grp.dockObjects) {
				DockObject cob = ob.Clone ();
				cob.ParentGroup = this;
				dockObjects.Add (cob);
			}
			type = grp.type;
			ResetVisibleGroups ();
		}

		internal override bool Visible {
			get {
				foreach (DockObject ob in dockObjects)
					if (ob.Visible)
						return true;
				return false;
			}
		}

		internal override void Write (XmlWriter writer)
		{
			base.Write (writer);
			writer.WriteAttributeString ("type", type.ToString ());
			if (type == DockGroupType.Tabbed && currentTabPage != -1)
				writer.WriteAttributeString ("currentTabPage", currentTabPage.ToString ());

			foreach (DockObject ob in dockObjects) {
				if (ob is DockGroupItem)
					writer.WriteStartElement ("item");
				else
					writer.WriteStartElement ("group");
				ob.Write (writer);
				writer.WriteEndElement ();
			}
		}

		internal override void Read (XmlReader reader)
		{
			base.Read (reader);
			type = (DockGroupType) Enum.Parse (typeof(DockGroupType), reader.GetAttribute ("type"));
			if (type == DockGroupType.Tabbed) {
				string s = reader.GetAttribute ("currentTabPage");
				if (s != null)
					currentTabPage = int.Parse (s);
			}

			reader.MoveToElement ();
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}

			reader.ReadStartElement ();
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "item") {
						string id = reader.GetAttribute ("id");
						DockItem it = Frame.GetItem (id);
						if (it == null) {
							it = Frame.AddItem (id);
							it.IsPositionMarker = true;
						}
						DockGroupItem gitem = new DockGroupItem (Frame, it);
						gitem.Read (reader);
						AddObject (gitem);

						reader.MoveToElement ();
						reader.Skip ();
					}
					else if (reader.LocalName == "group") {
						DockGroup grp = new DockGroup (Frame);
						grp.Read (reader);
						AddObject (grp);
					}
				}
				else
					reader.Skip ();
				reader.MoveToContent ();
			}
			reader.ReadEndElement ();
		}

		public bool IsChildNextToMargin (DockPositionType margin, DockObject obj, bool visibleOnly)
		{
			if (type == DockGroupType.Tabbed)
				return true;
			else if (type == DockGroupType.Horizontal) {
				if (margin == DockPositionType.Top || margin == DockPositionType.Bottom)
					return true;
				int i = visibleOnly ? VisibleObjects.IndexOf (obj) : Objects.IndexOf (obj);
				if (margin == DockPositionType.Left && i == 0)
					return true;
				if (margin == DockPositionType.Right && i == (visibleOnly ? VisibleObjects.Count - 1 : Objects.Count - 1))
					return true;
			}
			else if (type == DockGroupType.Vertical) {
				if (margin == DockPositionType.Left || margin == DockPositionType.Right)
					return true;
				int i = visibleOnly ? VisibleObjects.IndexOf (obj) : Objects.IndexOf (obj);
				if (margin == DockPositionType.Top && i == 0)
					return true;
				if (margin == DockPositionType.Bottom && i == (visibleOnly ? VisibleObjects.Count - 1 : Objects.Count - 1))
					return true;
			}
			return false;
		}

		public void Present (DockItem item, bool giveFocus)
		{
			throw new NotImplementedException ();
		}

		public void UpdateStyle ()
		{
			VisualStyle = Frame.GetRegionStyleForObject (this);

			foreach (var ob in Objects) {
				var it = ob as DockGroupItem;
				if (it != null) {
					if (it.Visible) {
						it.VisualStyle = Frame.GetRegionStyleForObject (it);
						it.Item.SetRegionStyle (it.VisualStyle);
					}
				}
				else
					((DockGroup)ob).UpdateStyle ();
			}
		}

		public DockGroupItem DockTarget (DockItem item, IDockObject insertBeforeObject)
		{
			int n;
			if (insertBeforeObject != null)
				n = dockObjects.IndexOf ((DockObject)insertBeforeObject);
			else
				n = dockObjects.Count;

			var gitem = new DockGroupItem (Frame, item);
			dockObjects.Insert (n, gitem);
			gitem.ParentGroup = this;
			gitem.SetVisible (true);
			return gitem;
		}

		internal override void RestoreAllocation ()
		{
			base.RestoreAllocation ();

			// Make a copy because RestoreAllocation can fire events such as VisibleChanged,
			// and subscribers may do changes in the list.
			foreach (var ob in dockObjects.ToArray ())
				ob.RestoreAllocation ();
		}

		internal override void StoreAllocation ()
		{
			base.StoreAllocation ();
			foreach (var ob in dockObjects)
				ob.StoreAllocation ();
		}

		public override string ToString ()
		{
			return "[DockGroup " + type + "]";
		}
	}
}
