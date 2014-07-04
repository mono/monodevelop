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

namespace MonoDevelop.Components.Docking
{
	internal class DockGroupItem: DockObject, IDockGroupItem
	{
		DockItem item;
		bool visibleFlag;
		DockItemStatus status;
		Xwt.Rectangle floatRect;
		DockPositionType barDocPosition;
		int autoHideSize = -1;

		public DockItem Item {
			get {
				return item;
			}
			set {
				item = value;
			}
		}

		IDockItemBackend IDockGroupItem.ItemBackend {
			get {
				return item.Backend;
			}
		}

		public string Id {
			get { return item.Id; }
		}

		public DockGroupItem (DockFrame frame, DockItem item): base (frame)
		{
			this.item = item;
			visibleFlag = item.Visible;
		}

		internal override void GetDefaultSize (out int width, out int height)
		{
			width = item.DefaultWidth;
			height = item.DefaultHeight;
		}

		public override bool Expand {
			get { return item.Expand; }
		}

		internal override void Write (XmlWriter writer)
		{
			base.Write (writer);
			writer.WriteAttributeString ("id", item.Id);
			writer.WriteAttributeString ("visible", visibleFlag.ToString ());
			writer.WriteAttributeString ("status", status.ToString ());

			if (status == DockItemStatus.AutoHide)
				writer.WriteAttributeString ("autoHidePosition", barDocPosition.ToString ());

			if (autoHideSize != -1)
				writer.WriteAttributeString ("autoHideSize", autoHideSize.ToString ());

			if (!floatRect.Equals (Xwt.Rectangle.Zero)) {
				writer.WriteAttributeString ("floatX", floatRect.X.ToString ());
				writer.WriteAttributeString ("floatY", floatRect.Y.ToString ());
				writer.WriteAttributeString ("floatWidth", floatRect.Width.ToString ());
				writer.WriteAttributeString ("floatHeight", floatRect.Height.ToString ());
			}
		}

		internal override void Read (XmlReader reader)
		{
			base.Read (reader);
			visibleFlag = bool.Parse (reader.GetAttribute ("visible")) && !item.IsPositionMarker;
			status = (DockItemStatus) Enum.Parse (typeof (DockItemStatus), reader.GetAttribute ("status"));
			int fx=0, fy=0, fw=0, fh=0;
			string s = reader.GetAttribute ("floatX");
			if (s != null)
				fx = int.Parse (s);
			s = reader.GetAttribute ("floatY");
			if (s != null)
				fy = int.Parse (s);
			s = reader.GetAttribute ("floatWidth");
			if (s != null)
				fw = int.Parse (s);
			s = reader.GetAttribute ("floatHeight");
			if (s != null)
				fh = int.Parse (s);
			s = reader.GetAttribute ("autoHidePosition");
			if (s != null)
				barDocPosition = (DockPositionType) Enum.Parse (typeof (DockPositionType), s);
			s = reader.GetAttribute ("autoHideSize");
			if (s != null)
				autoHideSize = int.Parse (s);
			floatRect = new Xwt.Rectangle (fx, fy, fw, fh);
		}

		public override void CopyFrom (DockObject ob)
		{
			base.CopyFrom (ob);
			DockGroupItem it = (DockGroupItem)ob;
			item = it.item;
			visibleFlag = it.visibleFlag;
			floatRect = it.floatRect;
		}

		internal override bool Visible {
			get { return visibleFlag && status == DockItemStatus.Dockable; }
		}

		internal bool VisibleFlag {
			get { return visibleFlag; }
		}

		public DockItemStatus Status {
			get {
				return status;
			}
			set {
				if (status == value)
					return;

				status = value;
				UpdateItemStatus (false);
			}
		}

		void UpdateItemStatus (bool showing)
		{
			if (status == DockItemStatus.Floating) {
				if (!showing && floatRect.Equals (Xwt.Rectangle.Zero))
					floatRect = item.GetAllocation ();
				item.SetFloatMode (floatRect);
			}
			else if (status == DockItemStatus.AutoHide) {
				if (!showing)
					SetBarDocPosition ();
				item.SetAutoHideMode (barDocPosition, GetAutoHideSize (barDocPosition));
			}

			item.UpdateVisibleStatus ();

			if (ParentGroup != null)
				ParentGroup.UpdateVisible (this);
		}


		void SetBarDocPosition ()
		{
			var alloc = Item.GetAllocation ();

			// Determine the best position for docking the item

			if (alloc.IsEmpty) {
				int uniqueTrue = -1;
				int uniqueFalse = -1;
				for (int n=0; n<4; n++) {
					bool inMargin = IsNextToMargin ((DockPositionType) n, false);
					if (inMargin) {
						if (uniqueTrue == -1)
							uniqueTrue = n;
						else
							uniqueTrue = -2;
					} else {
						if (uniqueFalse == -1)
							uniqueFalse = n;
						else
							uniqueFalse = -2;
					}
				}

				if (uniqueTrue >= 0) {
					barDocPosition = (DockPositionType) uniqueTrue;
					autoHideSize = 200;
					return;
				} else if (uniqueFalse >= 0) {
					barDocPosition = (DockPositionType) uniqueFalse;
					switch (barDocPosition) {
					case DockPositionType.Left: barDocPosition = DockPositionType.Right; break;
					case DockPositionType.Right: barDocPosition = DockPositionType.Left; break;
					case DockPositionType.Top: barDocPosition = DockPositionType.Bottom; break;
					case DockPositionType.Bottom: barDocPosition = DockPositionType.Top; break;
					}
					autoHideSize = 200;
					return;
				}

				// If the item is in a group, use the dock location of other items
				DockObject current = this;
				do {
					if (EstimateBarDocPosition (current.ParentGroup, current, out barDocPosition, out autoHideSize))
						return;
					current = current.ParentGroup;
				} while (current.ParentGroup != null);

				// Can't find a good location. Just guess.
				barDocPosition = DockPositionType.Bottom;
				autoHideSize = 200;
				return;
			}
			barDocPosition = CalcBarDocPosition ();
		}

		bool EstimateBarDocPosition (DockGroup grp, DockObject ignoreChild, out DockPositionType pos, out int size)
		{
			foreach (DockObject ob in grp.Objects) {
				if (ob == ignoreChild)
					continue;
				if (ob is DockGroup) {
					if (EstimateBarDocPosition ((DockGroup)ob, null, out pos, out size))
						return true;
				} else if (ob is DockGroupItem) {
					var it = (DockGroupItem) ob;
					if (it.status == DockItemStatus.AutoHide) {
						pos = it.barDocPosition;
						size = it.autoHideSize;
						return true;
					}
					if (!it.Item.GetAllocation().IsEmpty) {
						pos = it.CalcBarDocPosition ();
						size = it.GetAutoHideSize (pos);
						return true;
					}
				}
			}
			pos = DockPositionType.Bottom;
			size = 0;
			return false;
		}

		DockPositionType CalcBarDocPosition ()
		{
			Xwt.Rectangle alloc = Item.GetAllocation ();
			var frameAlloc = Frame.GetAllocation ();
			if (alloc.Width < alloc.Height) {
				int mid = (int) (alloc.Left + alloc.Width / 2);
				if (mid > frameAlloc.Left + frameAlloc.Width / 2)
					return DockPositionType.Right;
				else
					return DockPositionType.Left;
			} else {
				int mid = (int) (alloc.Top + alloc.Height / 2);
				if (mid > frameAlloc.Top + frameAlloc.Height / 2)
					return DockPositionType.Bottom;
				else
					return DockPositionType.Top;
			}
		}

		int GetAutoHideSize (DockPositionType pos)
		{
			if (autoHideSize != -1)
				return autoHideSize;

			if (pos == DockPositionType.Left || pos == DockPositionType.Right)
				return (int) Item.GetAllocation().Width;
			else
				return (int) Item.GetAllocation().Height;
		}

		internal void SetVisible (bool value)
		{
			if (visibleFlag != value) {
				visibleFlag = value;
				if (visibleFlag) {
					UpdateItemStatus (true);
					item.ShowWidget ();
				}
				else
					item.HideWidget ();
				if (ParentGroup != null)
					ParentGroup.UpdateVisible (this);
			}
		}

		public Xwt.Rectangle FloatRect {
			get {
				return floatRect;
			}
			set {
				floatRect = value;
			}
		}

		internal override void StoreAllocation ()
		{
			base.StoreAllocation ();
			if (Status == DockItemStatus.Floating)
				floatRect = item.FloatingPosition;
			else if (Status == DockItemStatus.AutoHide)
				autoHideSize = item.AutoHideSize;
		}

		internal override void RestoreAllocation ()
		{
			base.RestoreAllocation ();
			item.UpdateVisibleStatus ();
			if (VisibleFlag) {
				if (Status == DockItemStatus.Floating)
					item.SetFloatMode (floatRect);
				else if (Status == DockItemStatus.AutoHide)
					item.SetAutoHideMode (barDocPosition, GetAutoHideSize (barDocPosition));
			} else
				item.HideWidget ();
		}

		public override string ToString ()
		{
			return "[DockItem " + Item.Id + "]";
		}
	}
}
