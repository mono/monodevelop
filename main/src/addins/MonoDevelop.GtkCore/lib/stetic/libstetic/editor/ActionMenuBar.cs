
using System;
using System.Xml;
using System.Collections;
using Stetic.Wrapper;
using Mono.Unix;

namespace Stetic.Editor
{
	class ActionMenuBar: Gtk.MenuBar, IMenuItemContainer
	{
		ActionMenu openSubmenu;
		ActionTree actionTree;
		int dropPosition = -1;
		int dropIndex;
		ArrayList menuItems = new ArrayList ();
		bool showPlaceholder;
		Gtk.Widget addLabel;
		Gtk.Widget spacerItem;
		
		public ActionMenuBar ()
		{
			DND.DestSet (this, true);
		}
		
		public void FillMenu (ActionTree actionTree)
		{
			addLabel = null;

			if (this.actionTree != null) {
				this.actionTree.ChildNodeAdded -= OnChildAdded;
				this.actionTree.ChildNodeRemoved -= OnChildRemoved;
			}
			
			this.actionTree = actionTree;
			if (actionTree == null) {
				AddSpacerItem ();
				return;
			}
				
			actionTree.ChildNodeAdded += OnChildAdded;
			actionTree.ChildNodeRemoved += OnChildRemoved;
			
			HideSpacerItem ();
			menuItems.Clear ();
			Widget wrapper = Widget.Lookup (this);
			
			foreach (Gtk.Widget w in Children) {
				Remove (w);
				w.Destroy ();
				w.Dispose ();
			}

			foreach (ActionTreeNode node in actionTree.Children) {
				ActionMenuItem aitem = new ActionMenuItem (wrapper, this, node);
				AddItem (aitem, -1);
				menuItems.Add (aitem);
			}

			if (showPlaceholder) {
				AddCreateItemLabel ();
			} else if (actionTree.Children.Count == 0) {
				// Give some height to the toolbar
				AddSpacerItem ();
			}
		}
		
		public object SaveStatus ()
		{
			ArrayList status = new ArrayList ();
			
			for (int n=0; n<menuItems.Count; n++) {
				ActionMenuItem item = (ActionMenuItem) menuItems [n];
				if (item.IsSubmenuVisible) {
					status.Add (n);
					OpenSubmenu.SaveStatus (status);
					break;
				}
			}
			return status;
		}
		
		public void RestoreStatus (object data)
		{
			ArrayList status = (ArrayList) data;
			if (status.Count == 0)
				return;

			int pos = (int) status [0];
			if (pos >= menuItems.Count)
				return;
				
			ActionMenuItem item = (ActionMenuItem) menuItems [pos];
			if (status.Count == 1)	{
				// The last position in the status is the selected item
				item.Select ();
				if (item.Node.Action != null && item.Node.Action.Name.Length == 0) {
					// Then only case when there can have an action when an empty name
					// is when the user clicked on the "add action" link. In this case,
					// start editing the item again
					item.EditingDone += OnEditingDone;
					item.StartEditing ();
				}
			} else {
				item.ShowSubmenu ();
				if (OpenSubmenu != null)
					OpenSubmenu.RestoreStatus (status, 1);
			}
		}
		
		void AddCreateItemLabel ()
		{
			HideSpacerItem ();
			Gtk.Label emptyLabel = new Gtk.Label ();
			emptyLabel.Xalign = 0;
			emptyLabel.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("Click to create menu") + "</span></i>";
			Gtk.MenuItem mit = new Gtk.MenuItem ();
			mit.Child = emptyLabel;
			mit.ButtonPressEvent += OnNewItemPress;
			Insert (mit, -1);
			mit.ShowAll ();
			addLabel = mit;
		}
		
		void AddSpacerItem ()
		{
			if (spacerItem == null) {
				Gtk.Label emptyLabel = new Gtk.Label ();
				emptyLabel.Xalign = 0;
				emptyLabel.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("Empty menu bar") + "</span></i>";
				Gtk.MenuItem mit = new Gtk.MenuItem ();
				mit.Child = emptyLabel;
				Insert (mit, -1);
				spacerItem = mit;
				ShowAll ();
			}
		}
		
		void HideSpacerItem ()
		{
			if (spacerItem != null) {
				Remove (spacerItem);
				spacerItem = null;
			}
		}
		
		void AddItem (ActionMenuItem aitem, int pos)
		{
			Gtk.Table t = new Gtk.Table (1, 3, false);
			aitem.Attach (t, 0, 0);
			aitem.KeyPressEvent += OnItemKeyPress;
			t.ShowAll ();
			
			CustomMenuBarItem it = new CustomMenuBarItem ();
			it.ActionMenuItem = aitem;
			aitem.Bind (it);
			it.Child = t;
			it.ShowAll ();
			Insert (it, pos);
		}
		
		public bool ShowInsertPlaceholder {
			get { return showPlaceholder; }
			set {
				showPlaceholder = value;
				if (value && addLabel == null) {
					AddCreateItemLabel ();
				} else if (!value && addLabel != null) {
					Remove (addLabel);
					addLabel = null;
					if (menuItems.Count == 0)
						AddSpacerItem ();
				}
			}
		}
		
		public void Unselect ()
		{
			// Unselects any selected item and hides any open submenu menu
			Widget wrapper = Widget.Lookup (this);
			if (OpenSubmenu != null)
				OpenSubmenu.ResetSelection ();
			IDesignArea area = wrapper.GetDesignArea ();
			if (area != null) {
				foreach (Gtk.Widget w in Children) {
					CustomMenuBarItem it = w as CustomMenuBarItem;
					if (it != null)
						area.ResetSelection (it.ActionMenuItem);
				}
			}
			OpenSubmenu = null;
		}
		
		void OnChildAdded (object ob, ActionTreeNodeArgs args)
		{
			Refresh ();
		}
		
		void OnChildRemoved (object ob, ActionTreeNodeArgs args)
		{
			OpenSubmenu = null;
			
			Widget wrapper = Widget.Lookup (this);
			IDesignArea area = wrapper.GetDesignArea ();
			IObjectSelection asel = area.GetSelection ();
			ActionMenuItem curSel = asel != null ? asel.DataObject as ActionMenuItem : null;
			int pos = menuItems.IndexOf (curSel);
			
			foreach (Gtk.Widget w in Children) {
				if (w is CustomMenuBarItem && ((CustomMenuBarItem)w).ActionMenuItem.Node == args.Node) {
					Remove (w);
					menuItems.Remove (((CustomMenuBarItem)w).ActionMenuItem);
					if (menuItems.Count == 0 && !showPlaceholder)
						AddSpacerItem ();
					break;
				}
			}
			if (pos != -1 && pos < menuItems.Count)
				((ActionMenuItem)menuItems[pos]).Select ();
			else if (menuItems.Count > 0)
				((ActionMenuItem)menuItems[menuItems.Count-1]).Select ();
		}
		
		void Refresh ()
		{
			Widget wrapper = Widget.Lookup (this);
			IDesignArea area = wrapper.GetDesignArea ();
			if (area == null)
				return;

			ActionTreeNode selNode = null;
			
			foreach (Gtk.Widget w in Children) {
				CustomMenuBarItem it = w as CustomMenuBarItem;
				if (it != null && area.IsSelected (it.ActionMenuItem)) {
					selNode = it.ActionMenuItem.Node;
					area.ResetSelection (it.ActionMenuItem);
				}
				Remove (w);
			}
			
			FillMenu (actionTree);
			
			if (selNode != null) {
				ActionMenuItem mi = FindMenuItem (selNode);
				if (mi != null)
					mi.Select ();
			}
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnNewItemPress (object ob, Gtk.ButtonPressEventArgs args)
		{
			InsertAction (menuItems.Count);
			args.RetVal = true;
		}
		
		void InsertAction (int pos)
		{
			Widget wrapper = Widget.Lookup (this);
			using (wrapper.UndoManager.AtomicChange) {
				Wrapper.Action ac = (Wrapper.Action) ObjectWrapper.Create (wrapper.Project, new Gtk.Action ("", "", null, null));
				ActionTreeNode node = new ActionTreeNode (Gtk.UIManagerItemType.Menu, "", ac);
				actionTree.Children.Insert (pos, node);

				ActionMenuItem aitem = FindMenuItem (node);
				aitem.EditingDone += OnEditingDone;
				aitem.Select ();
				aitem.StartEditing ();
				
				if (wrapper.LocalActionGroups.Count == 0)
					wrapper.LocalActionGroups.Add (new ActionGroup ("Default"));
				wrapper.LocalActionGroups[0].Actions.Add (ac);
			}
		}
		
		void OnEditingDone (object ob, EventArgs args)
		{
			ActionMenuItem item = (ActionMenuItem) ob;
			item.EditingDone -= OnEditingDone;
			Widget wrapper = Widget.Lookup (this);
			
			if (item.Node.Action.GtkAction.Label.Length == 0 && item.Node.Action.GtkAction.StockId == null) {
				IDesignArea area = wrapper.GetDesignArea ();
				area.ResetSelection (item);
				using (wrapper.UndoManager.AtomicChange) {
					actionTree.Children.Remove (item.Node);
					wrapper.LocalActionGroups [0].Actions.Remove (item.Node.Action);
				}
			}
		}
		
		public void Select (ActionTreeNode node)
		{
			ActionMenuItem item = FindMenuItem (node);
			if (item != null)
				item.Select ();
		}
		
		public void DropMenu (ActionTreeNode node)
		{
			ActionMenuItem item = FindMenuItem (node);
			if (item != null) {
				if (item.HasSubmenu) {
					item.ShowSubmenu ();
					if (openSubmenu != null)
						openSubmenu.Select (null);
				}
				else
					item.Select ();
			}
		}
		
		public ActionMenu OpenSubmenu {
			get { return openSubmenu; }
			set {
				if (openSubmenu != null) {
					openSubmenu.OpenSubmenu = null;
					Widget wrapper = Widget.Lookup (this);
					IDesignArea area = wrapper.GetDesignArea ();
					if (area != null)
						area.RemoveWidget (openSubmenu);
					openSubmenu.Dispose ();
				}
				openSubmenu = value;
			}
		}

		bool IMenuItemContainer.IsTopMenu { 
			get { return true; } 
		}
		
		Gtk.Widget IMenuItemContainer.Widget { 
			get { return this; }
		}
		
		protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
		{
			ActionPaletteItem dragItem = DND.DragWidget as ActionPaletteItem;
			if (dragItem == null)
				return false;
			
			if (actionTree.Children.Count > 0) {
				ActionMenuItem item = LocateWidget (x, y);
				if (item != null) {
					Widget wrapper = Widget.Lookup (this);
				
					// Show the submenu to allow droping to it, but avoid
					// droping a submenu inside itself
					if (item.HasSubmenu && item.Node != dragItem.Node)
						item.ShowSubmenu (wrapper.GetDesignArea(), item);
					
					// Look for the index where to insert the new item
					dropIndex = actionTree.Children.IndexOf (item.Node);
					int mpos = item.Allocation.X + item.Allocation.Width / 2;
					if (x > mpos)
						dropIndex++;
					
					// Calculate the drop position, used to show the drop bar
					if (dropIndex == 0)
						dropPosition = item.Allocation.X;
					else if (dropIndex == menuItems.Count)
						dropPosition = item.Allocation.Right;
					else {
						item = (ActionMenuItem) menuItems [dropIndex];
						ActionMenuItem prevItem = (ActionMenuItem) menuItems [dropIndex - 1];
						dropPosition = prevItem.Allocation.Right + (item.Allocation.X - prevItem.Allocation.Right)/2;
					}
				}
			} else
				dropIndex = 0;

			QueueDraw ();
			return base.OnDragMotion (context, x, y, time);
		}
		
		protected override void OnDragLeave (Gdk.DragContext context, uint time)
		{
			dropPosition = -1;
			QueueDraw ();
			base.OnDragLeave (context, time);
		}
		
		protected override bool OnDragDrop (Gdk.DragContext context, int x,	int y, uint time)
		{
			ActionPaletteItem dropped = DND.Drop (context, null, time) as ActionPaletteItem;
			if (dropped == null)
				return false;

			if (dropped.Node.Type != Gtk.UIManagerItemType.Menuitem && 
				dropped.Node.Type != Gtk.UIManagerItemType.Menu &&
				dropped.Node.Type != Gtk.UIManagerItemType.Toolitem &&
				dropped.Node.Type != Gtk.UIManagerItemType.Separator)
				return false;
				
			ActionTreeNode newNode = dropped.Node;
			if (dropped.Node.Type == Gtk.UIManagerItemType.Toolitem) {
				newNode = newNode.Clone ();
				newNode.Type = Gtk.UIManagerItemType.Menuitem;
			}

			Widget wrapper = Widget.Lookup (this);
			using (wrapper.UndoManager.AtomicChange) {
				if (dropIndex < actionTree.Children.Count) {
					// Do nothing if trying to drop the node over the same node
					ActionTreeNode dropNode = actionTree.Children [dropIndex];
					if (dropNode == dropped.Node)
						return false;
						
					if (newNode.ParentNode != null)
						newNode.ParentNode.Children.Remove (newNode);
					
					// The drop position may have changed after removing the dropped node,
					// so get it again.
					dropIndex = actionTree.Children.IndexOf (dropNode);
					actionTree.Children.Insert (dropIndex, newNode);
				} else {
					if (newNode.ParentNode != null)
						newNode.ParentNode.Children.Remove (newNode);
					actionTree.Children.Add (newNode);
					dropIndex = actionTree.Children.Count - 1;
				}
				
				// Select the dropped node
				ActionMenuItem mi = (ActionMenuItem) menuItems [dropIndex];
				mi.Select ();
			}
			
			return base.OnDragDrop (context, x,	y, time);
		}		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			bool r = base.OnExposeEvent (ev);
			int w, h;
			this.GdkWindow.GetSize (out w, out h);
			if (dropPosition != -1)
				GdkWindow.DrawRectangle (this.Style.BlackGC, true, dropPosition, 0, 3, h);
			return r;
		}
		
		void OnItemKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			int pos = menuItems.IndexOf (s);
			ActionMenuItem item = (ActionMenuItem) s;
			
			switch (args.Event.Key) {
				case Gdk.Key.Left:
					if (pos > 0)
						((ActionMenuItem)menuItems[pos - 1]).Select ();
					break;
				case Gdk.Key.Right:
					if (pos < menuItems.Count - 1)
						((ActionMenuItem)menuItems[pos + 1]).Select ();
					else if (pos == menuItems.Count - 1)
						InsertAction (menuItems.Count);
					break;
				case Gdk.Key.Down:
					if (item.HasSubmenu) {
						item.ShowSubmenu ();
						if (openSubmenu != null)
							openSubmenu.Select (null);
					}
					break;
				case Gdk.Key.Up:
					OpenSubmenu = null;
					break;
			}
			args.RetVal = true;
		}
		
		void InsertActionAt (ActionMenuItem item, bool after, bool separator)
		{
			int pos = menuItems.IndexOf (item);
			if (pos == -1)
				return;
			
			if (after)
				pos++;

			if (separator) {
				ActionTreeNode newNode = new ActionTreeNode (Gtk.UIManagerItemType.Separator, null, null);
				actionTree.Children.Insert (pos, newNode);
			} else
				InsertAction (pos);
		}
		
		void Paste (ActionMenuItem item)
		{
		}
		
		public void ShowContextMenu (ActionItem aitem)
		{
			ActionMenuItem menuItem = (ActionMenuItem) aitem;
			
			Gtk.Menu m = new Gtk.Menu ();
			Gtk.MenuItem item = new Gtk.MenuItem (Catalog.GetString ("Insert Before"));
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				InsertActionAt (menuItem, false, false);
			};
			item = new Gtk.MenuItem (Catalog.GetString ("Insert After"));
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				InsertActionAt (menuItem, true, false);
			};
			
			m.Add (new Gtk.SeparatorMenuItem ());
			
			item = new Gtk.ImageMenuItem (Gtk.Stock.Cut, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				menuItem.Cut ();
			};
			item.Visible = false;	// No copy & paste for now
			item = new Gtk.ImageMenuItem (Gtk.Stock.Copy, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				menuItem.Copy ();
			};
			item.Visible = false;	// No copy & paste for now
			item = new Gtk.ImageMenuItem (Gtk.Stock.Paste, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				Paste (menuItem);
			};
			item.Visible = false;	// No copy & paste for now
			item = new Gtk.ImageMenuItem (Gtk.Stock.Delete, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				menuItem.Delete ();
			};
			m.ShowAll ();
			m.Popup ();
		}
		
		ActionMenuItem LocateWidget (int x, int y)
		{
			foreach (ActionMenuItem mi in menuItems) {
				if (mi.Allocation.Contains (x, y))
					return mi;
			}
			return null;
		}
		
		ActionMenuItem FindMenuItem (ActionTreeNode node)
		{
			foreach (ActionMenuItem mi in menuItems) {
				if (mi.Node == node)
					return mi;
			}
			return null;
		}
	}
}
