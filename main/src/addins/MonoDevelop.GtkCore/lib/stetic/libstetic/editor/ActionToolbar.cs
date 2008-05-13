
using System;
using System.Xml;
using System.Collections;
using Stetic.Wrapper;
using Mono.Unix;

namespace Stetic.Editor
{
	class ActionToolbar: Gtk.Toolbar, IMenuItemContainer
	{
		ActionTree actionTree;
		int dropPosition = -1;
		int dropIndex;
		ArrayList toolItems = new ArrayList ();
		bool showPlaceholder = true;
		Gtk.Widget addLabel;
		Gtk.Widget spacerItem;
		
		public ActionToolbar ()
		{
			DND.DestSet (this, true);
			this.ShowArrow = false;
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
			toolItems.Clear ();
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (this);
			
			foreach (Gtk.Widget w in Children) {
				Remove (w);
				w.Destroy ();
			}

			foreach (ActionTreeNode node in actionTree.Children) {
				ActionToolItem aitem = new ActionToolItem (wrapper, this, node);
				AddItem (aitem, -1);
				toolItems.Add (aitem);
			}
			
			if (actionTree.Children.Count == 0) {
				// If there are no buttons in the toolbar, give it some height so it is selectable.
				AddSpacerItem ();
			}

			if (showPlaceholder) {
				AddCreateItemLabel ();
			}
		}
		
		void AddCreateItemLabel ()
		{
			HideSpacerItem ();
			Gtk.EventBox ebox = new Gtk.EventBox ();
			ebox.VisibleWindow = false;
			Gtk.Label emptyLabel = new Gtk.Label ();
			emptyLabel.Xalign = 0;
			if (this.Orientation == Gtk.Orientation.Vertical)
				emptyLabel.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("New\nbutton") + "</span></i>";
			else
				emptyLabel.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("New button") + "</span></i>";
			ebox.BorderWidth = 3;
			ebox.Add (emptyLabel);
			Gtk.ToolItem mit = new Gtk.ToolItem ();
			mit.Child = ebox;
			ebox.ButtonPressEvent += OnNewItemPress;
			Insert (mit, -1);
			mit.ShowAll ();
			addLabel = mit;
		}
		
		void AddSpacerItem ()
		{
			if (spacerItem == null) {
				Gtk.ToolItem tb = new Gtk.ToolItem ();
				Gtk.Label emptyLabel = new Gtk.Label ();
				emptyLabel.Xalign = 0;
				emptyLabel.Xpad = 3;
				emptyLabel.Ypad = 3;
				if (this.Orientation == Gtk.Orientation.Vertical)
					emptyLabel.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("Empty\ntoolbar") + "</span></i>";
				else
					emptyLabel.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("Empty toolbar") + "</span></i>";
				tb.Child = emptyLabel;
				Insert (tb, -1);
				ShowAll ();
				spacerItem = tb;
			}
		}
		
		void HideSpacerItem ()
		{
			if (spacerItem != null) {
				Remove (spacerItem);
				spacerItem = null;
			}
		}
		
		void AddItem (ActionToolItem aitem, int pos)
		{
			aitem.KeyPressEvent += OnItemKeyPress;
			
			CustomToolbarItem it = new CustomToolbarItem ();
			it.ActionToolItem = aitem;
			it.Child = aitem;
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
					addLabel.Destroy ();
					addLabel = null;
					if (actionTree.Children.Count == 0)
						AddSpacerItem ();
				}
			}
		}

		public Stetic.Editor.ActionMenu OpenSubmenu {
			get { return null; }
			set { }
		}

		public bool IsTopMenu {
			get { return true; }
		}

		public Gtk.Widget Widget {
			get { return this; }
		}
		
		public void Unselect ()
		{
			// Unselects any selected item
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (this);
			IDesignArea area = wrapper.GetDesignArea ();
			if (area != null) {
				foreach (Gtk.Widget w in Children) {
					CustomToolbarItem it = w as CustomToolbarItem;
					if (it != null)
						area.ResetSelection (it.ActionToolItem);
				}
			}
		}
		
		void OnChildAdded (object ob, ActionTreeNodeArgs args)
		{
			Refresh ();
		}
		
		void OnChildRemoved (object ob, ActionTreeNodeArgs args)
		{
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (this);
			IDesignArea area = wrapper.GetDesignArea ();
			IObjectSelection asel = area.GetSelection ();
			ActionToolItem curSel = asel != null ? asel.DataObject as ActionToolItem : null;
			int pos = toolItems.IndexOf (curSel);
			
			foreach (Gtk.Widget w in Children) {
				if (w is CustomToolbarItem && ((CustomToolbarItem)w).ActionToolItem.Node == args.Node) {
					Remove (w);
					toolItems.Remove (((CustomToolbarItem)w).ActionToolItem);
					w.Destroy ();
					if (!showPlaceholder && toolItems.Count == 0)
						AddSpacerItem ();
					break;
				}
			}
			
			if (pos != -1 && pos < toolItems.Count)
				((ActionToolItem)toolItems[pos]).Select ();
			else if (toolItems.Count > 0)
				((ActionToolItem)toolItems[toolItems.Count-1]).Select ();
		}
		
		void Refresh ()
		{
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (this);
			IDesignArea area = wrapper.GetDesignArea ();
			if (area == null)
				return;

			ActionTreeNode selNode = null;
			
			foreach (Gtk.Widget w in Children) {
				CustomToolbarItem it = w as CustomToolbarItem;
				if (it != null && area.IsSelected (it.ActionToolItem)) {
					selNode = it.ActionToolItem.Node;
					area.ResetSelection (it.ActionToolItem);
				}
				Remove (w);
				w.Destroy ();
			}
			
			FillMenu (actionTree);
			
			if (selNode != null) {
				ActionToolItem mi = FindMenuItem (selNode);
				if (mi != null)
					mi.Select ();
			}
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnNewItemPress (object ob, Gtk.ButtonPressEventArgs args)
		{
			InsertAction (toolItems.Count);
			args.RetVal = true;
		}
		
		void InsertAction (int pos)
		{
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (this);
			using (wrapper.UndoManager.AtomicChange) {
				Wrapper.Action ac = (Wrapper.Action) ObjectWrapper.Create (wrapper.Project, new Gtk.Action ("", "", null, null));
				ActionTreeNode node = new ActionTreeNode (Gtk.UIManagerItemType.Toolitem, "", ac);
				actionTree.Children.Insert (pos, node);

				ActionToolItem aitem = FindMenuItem (node);
				aitem.EditingDone += OnEditingDone;
				aitem.Select ();
				aitem.StartEditing (false);
				//ShowInsertPlaceholder = false;

				if (wrapper.LocalActionGroups.Count == 0)
					wrapper.LocalActionGroups.Add (new ActionGroup ("Default"));
				wrapper.LocalActionGroups[0].Actions.Add (ac);
			}
		}
		
		void OnEditingDone (object ob, EventArgs args)
		{
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (this);
			if (wrapper == null)
				return;
			
			IDesignArea area = wrapper.GetDesignArea ();
			if (area == null)	// The toolbar may be disposed before ending editing
				return;
				
			ActionToolItem item = (ActionToolItem) ob;
			item.EditingDone -= OnEditingDone;
			
			if (item.Node.Action.GtkAction.Label.Length == 0 && item.Node.Action.GtkAction.StockId == null) {
				area.ResetSelection (item);
				using (wrapper.UndoManager.AtomicChange) {
					actionTree.Children.Remove (item.Node);
					wrapper.LocalActionGroups [0].Actions.Remove (item.Node.Action);
				}
			}
		}
		
		public void Select (ActionTreeNode node)
		{
			ActionToolItem item = FindMenuItem (node);
			if (item != null)
				item.Select ();
		}

		protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
		{
			ActionPaletteItem dragItem = DND.DragWidget as ActionPaletteItem;
			if (dragItem == null)
				return false;
				
			x += Allocation.X;
			y += Allocation.Y;
			
			if (actionTree.Children.Count > 0) {
				ActionToolItem item = LocateWidget (x, y);
				if (item != null) {
					// Look for the index where to insert the new item
					dropIndex = actionTree.Children.IndexOf (item.Node);
					int spos = (Orientation == Gtk.Orientation.Horizontal) ? x : y;
					int mpos = GetButtonPos (item) + GetButtonSize (item) / 2;
					if (spos > mpos)
						dropIndex++;
					
					// Calculate the drop position, used to show the drop bar
					if (dropIndex == 0)
						dropPosition = GetButtonPos (item);
					else if (dropIndex == toolItems.Count)
						dropPosition = GetButtonEndPos (item);
					else {
						item = (ActionToolItem) toolItems [dropIndex];
						ActionToolItem prevItem = (ActionToolItem) toolItems [dropIndex - 1];
						dropPosition = GetButtonEndPos (prevItem) + (GetButtonPos (item) - GetButtonEndPos (prevItem))/2;
					}
				}
			} else
				dropIndex = 0;

			QueueDraw ();
			return base.OnDragMotion (context, x, y, time);
		}
		
		int GetButtonPos (Gtk.Widget w)
		{
			return (Orientation == Gtk.Orientation.Horizontal) ? w.Allocation.X : w.Allocation.Y;
		}
		
		int GetButtonEndPos (Gtk.Widget w)
		{
			return (Orientation == Gtk.Orientation.Horizontal) ? w.Allocation.Right : w.Allocation.Bottom;
		}
		
		int GetButtonSize (Gtk.Widget w)
		{
			return (Orientation == Gtk.Orientation.Horizontal) ? w.Allocation.Width : w.Allocation.Height;
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
				dropped.Node.Type != Gtk.UIManagerItemType.Toolitem &&
				dropped.Node.Type != Gtk.UIManagerItemType.Separator)
				return false;
			
			ActionTreeNode newNode = dropped.Node;
			if (dropped.Node.Type == Gtk.UIManagerItemType.Menuitem) {
				newNode = newNode.Clone ();
				newNode.Type = Gtk.UIManagerItemType.Toolitem;
			}

			Widget wrapper = Stetic.Wrapper.Widget.Lookup (this);
			using (wrapper.UndoManager.AtomicChange) {
				if (dropIndex < actionTree.Children.Count) {
					// Do nothing if trying to drop the node over the same node
					ActionTreeNode dropNode = actionTree.Children [dropIndex];
					if (dropNode == newNode)
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
			}
			// Select the dropped node
			ActionToolItem mi = (ActionToolItem) toolItems [dropIndex];
			mi.Select ();
			
			return base.OnDragDrop (context, x,	y, time);
		}		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			bool r = base.OnExposeEvent (ev);
			if (dropPosition != -1) {
				if (this.Orientation == Gtk.Orientation.Horizontal)
					GdkWindow.DrawRectangle (this.Style.BlackGC, true, dropPosition, Allocation.Y, 3, Allocation.Height);
				else
					GdkWindow.DrawRectangle (this.Style.BlackGC, true, Allocation.X, dropPosition, Allocation.Width, 3);
			}
			return r;
		}
		
		void OnItemKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			int pos = toolItems.IndexOf (s);
			args.RetVal = false;
			
			switch (args.Event.Key) {
				case Gdk.Key.Left:
					args.RetVal = true;
					if (pos > 0)
						((ActionToolItem)toolItems[pos - 1]).Select ();
					break;
				case Gdk.Key.Right:
					args.RetVal = true;
					if (pos < toolItems.Count - 1)
						((ActionToolItem)toolItems[pos + 1]).Select ();
					else if (pos == toolItems.Count - 1)
						InsertAction (toolItems.Count);
					break;
			}
		}
		
		void InsertActionAt (ActionToolItem item, bool after, bool separator)
		{
			int pos = toolItems.IndexOf (item);
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
		
		void Paste (ActionToolItem item)
		{
		}
		
		public void ShowContextMenu (ActionItem aitem)
		{
			ActionToolItem menuItem = aitem as ActionToolItem;
			
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
			item = new Gtk.MenuItem (Catalog.GetString ("Insert Separator Before"));
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				InsertActionAt (menuItem, false, true);
			};
			item = new Gtk.MenuItem (Catalog.GetString ("Insert Separator After"));
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				InsertActionAt (menuItem, true, true);
			};
			
			m.Add (new Gtk.SeparatorMenuItem ());
			
			item = new Gtk.ImageMenuItem (Gtk.Stock.Cut, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				menuItem.Cut ();
			};
			item = new Gtk.ImageMenuItem (Gtk.Stock.Copy, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				menuItem.Copy ();
			};
			item = new Gtk.ImageMenuItem (Gtk.Stock.Paste, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				Paste (menuItem);
			};
			item = new Gtk.ImageMenuItem (Gtk.Stock.Delete, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				menuItem.Delete ();
			};
			m.ShowAll ();
			m.Popup ();
		}
		
		public object SaveStatus ()
		{
			for (int n=0; n<toolItems.Count; n++)
				if (((ActionToolItem)toolItems[n]).IsSelected)
					return n;
			return null;
		}
		
		public void RestoreStatus (object stat)
		{
			if (stat == null)
				return;
			int n = (int) stat;
			if (n < toolItems.Count)
				((ActionToolItem)toolItems[n]).Select ();
		}
		
		ActionToolItem LocateWidget (int x, int y)
		{
			foreach (ActionToolItem mi in toolItems) {
				if (mi.Allocation.Contains (x, y))
					return mi;
			}
			return null;
		}
		
		ActionToolItem FindMenuItem (ActionTreeNode node)
		{
			foreach (ActionToolItem mi in toolItems) {
				if (mi.Node == node)
					return mi;
			}
			return null;
		}
	}
	
	class CustomToolbarItem: Gtk.ToolItem
	{
		public override void Dispose ()
		{
			ActionToolItem.Dispose ();
			base.Dispose ();
		}
		
		public ActionToolItem ActionToolItem;
	}
}
