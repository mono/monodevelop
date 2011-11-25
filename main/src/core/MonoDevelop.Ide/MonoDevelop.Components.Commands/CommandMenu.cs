//
// CommandMenu.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Components.Commands
{
	public class CommandMenu: Gtk.Menu
	{
		CommandEntrySet commandEntrySet;
		CommandManager manager;
		object initialCommandTarget;

		public CommandMenu (CommandManager manager)
		{
			this.manager = manager;
			this.AccelGroup = manager.AccelGroup;
		}
		
		public CommandMenu (CommandManager manager, CommandEntrySet commandEntrySet) : this (manager)
		{
			this.commandEntrySet = commandEntrySet;
		}
		
		
		public object InitialCommandTarget {
			get { return initialCommandTarget; }
			set { initialCommandTarget = value; }
		}
		
		internal CommandManager CommandManager {
			get {
				if (manager == null) {
					Gtk.Widget w = Parent;
					while (w != null && !(w is CommandMenu) && !(w is CommandMenuBar)) {
						w = w.Parent;
					}
					if (w is CommandMenu)
						manager = ((CommandMenu)w).CommandManager;
					else if (w is CommandMenuBar)
						manager = ((CommandMenuBar)w).CommandManager;
					else
						throw new InvalidOperationException ("Menu not bound to a CommandManager.");
				}
				return manager;
			}
		}
		
		bool populated;
		void EnsurePopulated ()
		{
			if (populated)
				return;
			populated = true;
			if (commandEntrySet != null) {
				manager.CreateMenu (commandEntrySet, this);
				Update ();
				commandEntrySet = null;
			}
		}
		
		protected CommandMenu (IntPtr ptr): base (ptr)
		{
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();
			manager.RegisterUserInteraction ();
			Update ();
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			EnsurePopulated ();
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			EnsurePopulated ();
			base.OnSizeRequested (ref requisition);
		}

		internal void Update ()
		{
			CommandTargetRoute targetRoute = new CommandTargetRoute (initialCommandTarget);
			foreach (Gtk.Widget item in Children) {
				if (item is ICommandUserItem)
					((ICommandUserItem)item).Update (targetRoute);
				else if (item is Gtk.MenuItem) {
					Gtk.MenuItem mitem = (Gtk.MenuItem) item;
					CommandMenu men = mitem.Submenu as CommandMenu;
					if (men != null)
						men.InitialCommandTarget = initialCommandTarget;
					item.Show ();
					if (item is AutoHideMenuItem) {
						men.Update ();
						if (!((AutoHideMenuItem)item).HasVisibleChildren)
							item.Hide ();
					}
				}
				else
					item.Show ();
			}
			
			// After updating the menu, hide the separators which don't actually
			// separate items.
			bool prevWasItem = false;
			Gtk.Widget lastSeparator = null;
			foreach (Gtk.Widget item in Children) {
				if (item is Gtk.SeparatorMenuItem) {
					if (!prevWasItem)
						item.Hide ();
					else {
						prevWasItem = false;
						lastSeparator = item;
					}
				} else if (item.Visible)
					prevWasItem = true;
			}
			if (!prevWasItem && lastSeparator != null)
				lastSeparator.Hide ();
		}
		
		protected override void OnHidden ()
		{
			base.OnHidden ();

			// Make sure the accelerators allways work for this item
			// while the menu is hidden
			foreach (Gtk.MenuItem item in Children) {
				item.Sensitive = true;
				item.Visible = true;
			}
		}
	}
}
