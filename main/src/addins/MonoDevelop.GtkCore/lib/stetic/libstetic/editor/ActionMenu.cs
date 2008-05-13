
using System;
using System.Collections;
using Stetic.Wrapper;
using Mono.Unix;

namespace Stetic.Editor
{
	public class ActionMenu: Gtk.EventBox, IMenuItemContainer
	{
		ActionTreeNode parentNode;
		ActionTreeNodeCollection nodes;
		ArrayList menuItems = new ArrayList ();
		Gtk.Table table;
		ActionMenu openSubmenu;
		Widget wrapper;
		int dropPosition = -1;
		int dropIndex;
		Gtk.EventBox emptyLabel;
		IMenuItemContainer parentMenu;
		
		public ActionMenu (IntPtr p): base (p)
		{}
		
		internal ActionMenu (Widget wrapper, IMenuItemContainer parentMenu, ActionTreeNode node)
		{
			DND.DestSet (this, true);
			parentNode = node;
			this.parentMenu = parentMenu;
			this.wrapper = wrapper;
			this.nodes = node.Children;
			table = new Gtk.Table (0, 0, false);
			table.ColumnSpacing = 5;
			table.RowSpacing = 5;
			table.BorderWidth = 5;
			this.AppPaintable = true;
			
			Add (table);
			
			Fill ();
			
			parentNode.ChildNodeAdded += OnChildAdded;
			parentNode.ChildNodeRemoved += OnChildRemoved;
		}
		
		public override void Dispose ()
		{
			foreach (Gtk.Widget w in table.Children) {
				table.Remove (w);
				w.Destroy ();
			}
			
			parentNode.ChildNodeAdded -= OnChildAdded;
			parentNode.ChildNodeRemoved -= OnChildRemoved;
			parentNode = null;
			base.Dispose ();
		}
		
		public void Select (ActionTreeNode node)
		{
			if (node != null) {
				ActionMenuItem item = FindMenuItem (node);
				if (item != null)
					item.Select ();
			} else {
				if (menuItems.Count > 0)
					((ActionMenuItem)menuItems [0]).Select ();
				else
					InsertAction (0);
			}
		}
		
		public ActionTreeNode ParentNode {
			get { return parentNode; }
		}
		
		bool IMenuItemContainer.IsTopMenu { 
			get { return false; } 
		}
		
		Gtk.Widget IMenuItemContainer.Widget { 
			get { return this; }
		}
		
		public void TrackWidgetPosition (Gtk.Widget refWidget, bool topMenu)
		{
			IDesignArea area = wrapper.GetDesignArea ();
			Gdk.Rectangle rect = area.GetCoordinates (refWidget);
			if (topMenu)
				area.MoveWidget (this, rect.X, rect.Bottom);
			else
				area.MoveWidget (this, rect.Right, rect.Top);

			GLib.Timeout.Add (50, new GLib.TimeoutHandler (RepositionSubmenu));
		}
		
		public bool RepositionSubmenu ()
		{
			if (openSubmenu == null)
				return false;

			ActionMenuItem item = FindMenuItem (openSubmenu.parentNode);
			if (item != null)
				openSubmenu.TrackWidgetPosition (item, false);
			return false;
		}
		
		void Fill ()
		{
			menuItems.Clear ();

			uint n = 0;
			ActionMenuItem editItem = null;
			
			if (nodes.Count > 0) {
				foreach (ActionTreeNode node in nodes) {
					ActionMenuItem item = new ActionMenuItem (wrapper, this, node);
					item.KeyPressEvent += OnItemKeyPress;
					item.Attach (table, n++, 0);
					menuItems.Add (item);
					// If adding an action with an empty name, select and start editing it
//					if (node.Action != null && node.Action.Name.Length == 0)
//						editItem = item;
				}
			}
			
			emptyLabel = new Gtk.EventBox ();
			emptyLabel.VisibleWindow = false;
			Gtk.Label label = new Gtk.Label ();
			label.Xalign = 0;
			label.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("Click to create action") + "</span></i>";
			emptyLabel.Add (label);
			emptyLabel.ButtonPressEvent += OnAddClicked;
			table.Attach (emptyLabel, 1, 2, n, n + 1);
			
			ShowAll ();
			
			if (editItem != null) {
				// If there is an item with an empty action, it means that it was an item that was
				// being edited. Restart the editing now.
				GLib.Timeout.Add (200, delegate {
					editItem.Select ();
					editItem.EditingDone += OnEditingDone;
					editItem.StartEditing ();
					return false;
				});
			}
		}
		
		void Refresh ()
		{
			IDesignArea area = wrapper.GetDesignArea ();
			ActionTreeNode selNode = null;
			
			foreach (Gtk.Widget w in table.Children) {
				ActionMenuItem ami = w as ActionMenuItem;
				if (area.IsSelected (w) && ami != null) {
					selNode = ami.Node;
					area.ResetSelection (w);
				}
				table.Remove (w);
			}
			
			Fill ();
			
			ActionMenuItem mi = FindMenuItem (selNode);
			if (mi != null)
				mi.Select ();

			GLib.Timeout.Add (50, new GLib.TimeoutHandler (RepositionSubmenu));
		}
		
		public ActionMenu OpenSubmenu {
			get { return openSubmenu; }
			set {
				if (openSubmenu != null) {
					openSubmenu.OpenSubmenu = null;
					IDesignArea area = wrapper.GetDesignArea ();
					area.RemoveWidget (openSubmenu);
					openSubmenu.Dispose ();
				}
				openSubmenu = value;
			}
		}
		
		internal void ResetSelection ()
		{
			if (OpenSubmenu != null)
				OpenSubmenu.ResetSelection ();
			IDesignArea area = wrapper.GetDesignArea ();
			if (area != null) {
				foreach (Gtk.Widget w in table.Children) {
					ActionMenuItem ami = w as ActionMenuItem;
					if (ami != null)
						area.ResetSelection (w);
				}
			}
		}
		
		ActionTreeNode InsertAction (int pos)
		{
			using (wrapper.UndoManager.AtomicChange) {
				Wrapper.Action ac = (Wrapper.Action) ObjectWrapper.Create (wrapper.Project, new Gtk.Action ("", "", null, null));
				ActionTreeNode newNode = new ActionTreeNode (Gtk.UIManagerItemType.Menuitem, null, ac);
				nodes.Insert (pos, newNode);
				ActionMenuItem item = FindMenuItem (newNode);
				item.EditingDone += OnEditingDone;
				item.Select ();
				item.StartEditing ();
				emptyLabel.Hide ();
				
				if (wrapper.LocalActionGroups.Count == 0)
					wrapper.LocalActionGroups.Add (new ActionGroup ("Default"));
				wrapper.LocalActionGroups [0].Actions.Add (ac);
				return newNode;
			}
		}
		
		void DeleteAction (ActionMenuItem item)
		{
			int pos = menuItems.IndexOf (item);
			item.Delete ();
			if (pos >= menuItems.Count)
				SelectLastItem ();
			else
				((ActionMenuItem)menuItems [pos]).Select ();
		}
		
		void OnEditingDone (object ob, MenuItemEditEventArgs args)
		{
			ActionMenuItem item = (ActionMenuItem) ob;
			item.EditingDone -= OnEditingDone;
			if (item.Node.Action.GtkAction.Label.Length == 0 && item.Node.Action.GtkAction.StockId == null) {
				IDesignArea area = wrapper.GetDesignArea ();
				area.ResetSelection (item);
				using (wrapper.UndoManager.AtomicChange) {
					nodes.Remove (item.Node);
					wrapper.LocalActionGroups [0].Actions.Remove (item.Node.Action);
				}
				SelectLastItem ();
			}
			else {
				if (args.ExitKey == Gdk.Key.Up || args.ExitKey == Gdk.Key.Down)
					ProcessKey (item, args.ExitKey, Gdk.ModifierType.None);
			}
		}
		
		void SelectLastItem ()
		{
			if (menuItems.Count > 0)
				((ActionMenuItem)menuItems [menuItems.Count - 1]).Select ();
			else if (parentMenu.Widget is ActionMenuBar) {
				ActionMenuBar bar = (ActionMenuBar) parentMenu.Widget;
				bar.Select (parentNode);
			}
			else if (parentMenu.Widget is ActionMenu) {
				ActionMenu parentAM = (ActionMenu) parentMenu.Widget;
				parentAM.Select (parentNode);
			}
		}
		
		void OnChildAdded (object ob, ActionTreeNodeArgs args)
		{
			Refresh ();
		}
		
		void OnChildRemoved (object ob, ActionTreeNodeArgs args)
		{
			IDesignArea area = wrapper.GetDesignArea ();
			IObjectSelection asel = area.GetSelection ();
			ActionMenuItem curSel = asel != null ? asel.DataObject as ActionMenuItem : null;
			int pos = menuItems.IndexOf (curSel);
			
			ActionMenuItem mi = FindMenuItem (args.Node);
			if (mi != null) {
				// Remove the table row that contains the menu item
				Gtk.Table.TableChild tc = (Gtk.Table.TableChild) table [mi];
				uint row = tc.TopAttach;
				mi.Detach ();
				menuItems.Remove (mi);
				foreach (Gtk.Widget w in table.Children) {
					tc = (Gtk.Table.TableChild) table [w];
					if (tc.TopAttach >= row)
						tc.TopAttach--;
					if (tc.BottomAttach > row)
						tc.BottomAttach--;
				}
				if (pos != -1 && pos < menuItems.Count)
					((ActionMenuItem)menuItems[pos]).Select ();
				else
					SelectLastItem ();
				GLib.Timeout.Add (50, new GLib.TimeoutHandler (RepositionSubmenu));
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			int w, h;
			this.GdkWindow.GetSize (out w, out h);
			Gdk.Rectangle clip = new Gdk.Rectangle (0,0,w,h);
			Gtk.Style.PaintBox (this.Style, this.GdkWindow, Gtk.StateType.Normal, Gtk.ShadowType.Out, clip, this, "menu", 0, 0, w, h);
			
			bool r = base.OnExposeEvent (ev);
			
			if (dropPosition != -1) {
				GdkWindow.DrawRectangle (this.Style.BlackGC, true, 0, dropPosition - 1, w - 2, 3);
			}
			
			return r;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			return true;
		}
		
		void OnAddClicked (object s, Gtk.ButtonPressEventArgs args)
		{
			InsertAction (menuItems.Count);
			args.RetVal = true;
		}

		protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
		{
			ActionPaletteItem dragItem = DND.DragWidget as ActionPaletteItem;
			if (dragItem == null)
				return false;
			
			if (nodes.Count > 0) {
				ActionMenuItem item = LocateWidget (x, y);
				if (item != null) {
				
					// Show the submenu to allow droping to it, but avoid
					// droping a submenu inside itself
					if (item.HasSubmenu && item.Node != dragItem.Node)
						item.ShowSubmenu (wrapper.GetDesignArea(), item);
					
					// Look for the index where to insert the new item
					dropIndex = nodes.IndexOf (item.Node);
					int mpos = item.Allocation.Y + item.Allocation.Height / 2;
					if (y > mpos)
						dropIndex++;
					
					// Calculate the drop position, used to show the drop bar
					if (dropIndex == 0)
						dropPosition = item.Allocation.Y;
					else if (dropIndex == menuItems.Count)
						dropPosition = item.Allocation.Bottom;
					else {
						item = (ActionMenuItem) menuItems [dropIndex];
						ActionMenuItem prevItem = (ActionMenuItem) menuItems [dropIndex - 1];
						dropPosition = prevItem.Allocation.Bottom + (item.Allocation.Y - prevItem.Allocation.Bottom)/2;
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
		
		protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y, Gtk.SelectionData data, uint info, uint time)
		{
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
			
			ActionTreeNode newNode = null;
			
			// Toolitems are copied, not moved
			
			using (wrapper.UndoManager.AtomicChange) {
				if (dropped.Node.ParentNode != null && dropped.Node.Type != Gtk.UIManagerItemType.Toolitem) {
					if (dropIndex < nodes.Count) {
						// Do nothing if trying to drop the node over the same node
						ActionTreeNode dropNode = nodes [dropIndex];
						if (dropNode == dropped.Node)
							return false;
							
						dropped.Node.ParentNode.Children.Remove (dropped.Node);
						
						// The drop position may have changed after removing the dropped node,
						// so get it again.
						dropIndex = nodes.IndexOf (dropNode);
						nodes.Insert (dropIndex, dropped.Node);
					} else {
						dropped.Node.ParentNode.Children.Remove (dropped.Node);
						nodes.Add (dropped.Node);
						dropIndex = nodes.Count - 1;
					}
				} else {
					newNode = new ActionTreeNode (Gtk.UIManagerItemType.Menuitem, null, dropped.Node.Action);
					nodes.Insert (dropIndex, newNode);
				}
				// Select the dropped node
				ActionMenuItem mi = (ActionMenuItem) menuItems [dropIndex];
				mi.Select ();
			}
			
			return base.OnDragDrop (context, x,	y, time);
		}
		
		[GLib.ConnectBefore]
		void OnItemKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			ActionMenuItem item = (ActionMenuItem) s;
			ProcessKey (item, args.Event.Key, args.Event.State);
			args.RetVal = true;
		}
		
		void ProcessKey (ActionMenuItem item, Gdk.Key key, Gdk.ModifierType modifier)
		{
			int pos = menuItems.IndexOf (item);
			
			switch (key) {
				case Gdk.Key.Up:
					if (pos > 0)
						((ActionMenuItem)menuItems[pos - 1]).Select ();
					else if (parentMenu.Widget is ActionMenuBar) {
						ActionMenuBar bar = (ActionMenuBar) parentMenu.Widget;
						bar.Select (parentNode);
					}
					break;
				case Gdk.Key.Down:
					if (pos < menuItems.Count - 1)
						((ActionMenuItem)menuItems[pos + 1]).Select ();
					else if (pos == menuItems.Count - 1) {
						InsertAction (menuItems.Count);
					}
					break;
				case Gdk.Key.Right:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0 && item.Node.Type == Gtk.UIManagerItemType.Menuitem) {
						// Create a submenu
						using (item.Node.Action.UndoManager.AtomicChange) {
							item.Node.Type = Gtk.UIManagerItemType.Menu;
						}
						item.Node.Action.NotifyChanged ();
					}
					if (item.HasSubmenu) {
						item.ShowSubmenu ();
						if (openSubmenu != null)
							openSubmenu.Select (null);
					} else if (parentNode != null) {
						ActionMenuBar parentMB = parentMenu.Widget as ActionMenuBar;
						if (parentMB != null) {
							int i = parentNode.ParentNode.Children.IndexOf (parentNode);
							if (i < parentNode.ParentNode.Children.Count - 1)
								parentMB.DropMenu (parentNode.ParentNode.Children [i + 1]);
						}
					}
					break;
				case Gdk.Key.Left:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0 && item.Node.Type == Gtk.UIManagerItemType.Menu) {
						// Remove the submenu
						OpenSubmenu = null;
						using (item.Node.Action.UndoManager.AtomicChange) {
							item.Node.Type = Gtk.UIManagerItemType.Menuitem;
							item.Node.Children.Clear ();
						}
						item.Node.Action.NotifyChanged ();
						break;
					}
					if (parentNode != null) {
						ActionMenu parentAM = parentMenu.Widget as ActionMenu;
						if (parentAM != null) {
							parentAM.Select (parentNode);
						}
						ActionMenuBar parentMB = parentMenu.Widget as ActionMenuBar;
						if (parentMB != null) {
							int i = parentNode.ParentNode.Children.IndexOf (parentNode);
							if (i > 0)
								parentMB.DropMenu (parentNode.ParentNode.Children [i - 1]);
						}
					}
					break;
				case Gdk.Key.Return:
					item.EditingDone += OnEditingDone;
					item.StartEditing ();
					break;
				case Gdk.Key.Insert:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0)
						InsertActionAt (item, true, true);
					else
						InsertActionAt (item, false, false);
					break;
				case Gdk.Key.Delete:
					DeleteAction (item);
					break;
			}
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
				nodes.Insert (pos, newNode);
				Select (newNode);
			} else
				InsertAction (pos);
		}
		
		void Paste (ActionMenuItem item)
		{
		}
		
		void IMenuItemContainer.ShowContextMenu (ActionItem aitem)
		{
			ActionMenuItem menuItem = aitem as ActionMenuItem;
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
			item.Visible = false;	// No copy & paste for now
			item = new Gtk.ImageMenuItem (Gtk.Stock.Copy, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				menuItem.Copy ();
			};
			item.Visible = false;
			item = new Gtk.ImageMenuItem (Gtk.Stock.Paste, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				Paste (menuItem);
			};
			item.Visible = false;
			
			item = new Gtk.ImageMenuItem (Gtk.Stock.Delete, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				DeleteAction (menuItem);
			};
			m.ShowAll ();
			m.Popup ();
		}
		
		internal void SaveStatus (ArrayList status)
		{
			for (int n=0; n<menuItems.Count; n++) {
				ActionMenuItem item = (ActionMenuItem) menuItems [n];
				if (item.IsSelected) {
					status.Add (n);
					return;
				}
				if (item.IsSubmenuVisible) {
					status.Add (n);
					OpenSubmenu.SaveStatus (status);
					return;
				}
			}
		}
		
		internal void RestoreStatus (ArrayList status, int index)
		{
			int pos = (int) status [index];
			if (pos >= menuItems.Count)
				return;
				
			ActionMenuItem item = (ActionMenuItem)menuItems [pos];
			if (index == status.Count - 1)	{
				// The last position in the status is the selected item
				item.Select ();
				if (item.Node.Action != null && item.Node.Action.Name.Length == 0) {
					// Then only case when there can have an action when an empty name
					// is when the user clicked on the "add action" link. In this case,
					// start editing the item again
					item.EditingDone += OnEditingDone;
					item.StartEditing ();
				}
			}
			else {
				item.ShowSubmenu ();
				if (OpenSubmenu != null)
					OpenSubmenu.RestoreStatus (status, index + 1);
			}
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
	
	interface IMenuItemContainer
	{
		ActionMenu OpenSubmenu { get; set; }
		bool IsTopMenu { get; }
		Gtk.Widget Widget { get; }
		void ShowContextMenu (ActionItem item);
	}
}
