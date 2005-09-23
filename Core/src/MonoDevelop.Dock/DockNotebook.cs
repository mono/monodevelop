/*
 * Copyright (C) 2004 Todd Berman <tberman@off.net>
 * Copyright (C) 2004 Jeroen Zwartepoorte <jeroen@xs4all.nl>
 * Copyright (C) 2005 John Luke <john.luke@gmail.com>
 *
 * based on work by:
 * Copyright (C) 2002 Gustavo Giráldez <gustavo.giraldez@gmx.net>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Xml;
using Gtk;

namespace Gdl
{
	public class DockNotebook : DockItem
	{
		private struct DockInfo
		{
			public DockPlacement position;
			public object data;
			
			public DockInfo (DockPlacement position, object data)
			{
				this.position = position;
				this.data = data;
			}
		}
		
		private DockInfo dockInfo;

		protected DockNotebook (IntPtr raw) : base (raw) { }

		static DockNotebook ()
		{
			Rc.ParseString ("style \"gdl-dock-notebook-default\" {\n" +
			                    "xthickness = 2\n" +
			                    "ythickness = 2\n" +
			                    "}\n" +
			                    "widget_class \"*.GtkNotebook.Gdl_DockItem\" " +
			                    "style : gtk \"gdl-dock-notebook-default\"\n");
		}
		
		public DockNotebook ()
		{
			Child = new Notebook ();
			Child.Parent = this;
			((Notebook)Child).TabPos = PositionType.Bottom;
			// FIXME: enable these if we do a DockTabLabel
			//((Notebook)Child).SwitchPage += new SwitchPageHandler (SwitchPageCb);
			//((Notebook)Child).ButtonPressEvent += new ButtonPressEvent (ButtonPressCb);
			//((Notebook)Child).ButtonReleaseEvent += new ButtonReleaseEvent (ButtonReleaseCb);
			((Notebook)Child).Scrollable = true;
			Child.Show ();
			DockObjectFlags &= ~(DockObjectFlags.Automatic);
		}
		
		protected void SwitchPageHandler (object o, SwitchPageArgs e)
		{
			// FIXME: port this if we do a DockTabLabel
		}

		protected override void OnDestroyed ()
		{
			// this first
			base.OnDestroyed ();

			// after that we can remove the GtkNotebook
			if (Child != null) {
				Child.Unparent ();
				Child = null;
			}
		}
		
		protected override void OnAdded (Widget widget)
		{
			if (widget == null || !(widget is DockItem))
				return;

			Dock ((DockObject)widget, DockPlacement.Center, null);
		}
		
		protected override void ForAll (bool includeInternals, Callback cb)
		{
			if (includeInternals) {
				base.ForAll (includeInternals, cb);
			} else {
				if (Child != null) {
					((Notebook)Child).Foreach (cb);
				}
			}
		}

		private void DockChild (Widget w)
		{
			if (w is DockObject)
				Dock ((DockObject)w, dockInfo.position, dockInfo.data);
		}

		public override void OnDocked (DockObject requestor, DockPlacement position, object data)
		{
			/* we only add support for DockPlacement.Center docking
			   strategy here... for the rest use our parent class' method */
			if (position == DockPlacement.Center) {
				/* we can only dock simple (not compound) items */
				if (requestor.IsCompound) {
					requestor.Freeze ();
					dockInfo = new DockInfo (position, data);
					requestor.Foreach (new Callback (DockChild));
					requestor.Thaw ();
				} else {
					DockItem requestorItem = requestor as DockItem;
					Widget label = requestorItem.TabLabel;
					if (label == null) {
						label = new Label (requestorItem.LongName);
						requestorItem.TabLabel = label;
					}
					
					int tabPosition = -1;
					if (data is Int32)
						tabPosition = Convert.ToInt32 (data);
					((Notebook)Child).InsertPage (requestor, label, tabPosition);
					requestor.DockObjectFlags |= DockObjectFlags.Attached;
				}
			} else {
				base.OnDocked (requestor, position, data);
			}
		}
		
		public override void SetOrientation (Orientation orientation)
		{
			if (Child != null && Child is Notebook) {
				if (orientation == Orientation.Horizontal)
					((Notebook)Child).TabPos = PositionType.Left;
				else
					((Notebook)Child).TabPos = PositionType.Bottom;
			}
			base.SetOrientation (orientation);
		}
		
		public override bool OnChildPlacement (DockObject child, ref DockPlacement position)
		{
			DockPlacement pos = DockPlacement.None;
			if (Child != null) {
				foreach (Widget widget in ((Notebook)Child).Children) {
					if (widget == child) {
						pos = DockPlacement.Center;
						break;
					}
				}
			}
			if (pos != DockPlacement.None) {
				position = pos;
				return true;
			}
			return false;
		}
		
		public override void OnPresent (DockObject child)
		{
			Notebook nb = Child as Notebook;
			
			int i = nb.PageNum (child);
			if (i >= 0)
				nb.CurrentPage = i;

			base.OnPresent (child);
		}
		
		public override bool OnReorder (DockObject requestor, DockPlacement position, object other_data)
		{
			bool handled = false;
			int current_position, new_pos = -1;
			
			if (Child != null && position == DockPlacement.Center) {
				current_position = ((Notebook)Child).PageNum (requestor);
				if (current_position >= 0) {
					handled = true;
					if (other_data is Int32)
						new_pos = Convert.ToInt32 (other_data);
					((Notebook)Child).ReorderChild (requestor, new_pos);
				}
			}
			return handled;
		}
		
		public override bool IsCompound {
			get { return true; }
		}
		
		[After]
		[Export]
		public int Page {
			get { return ((Notebook)Child).CurrentPage; }
			set { ((Notebook)Child).CurrentPage = value; }
		}
		
		public override bool HasGrip {
			get { return false; }
		}
	}
}
