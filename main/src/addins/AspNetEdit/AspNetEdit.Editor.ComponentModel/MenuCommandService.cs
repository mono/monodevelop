 /* 
 * MenuCommandService.cs - Provides access to commands, and tracks designer verbs
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.ComponentModel.Design;
using Gtk;
using System.Collections;

namespace AspNetEdit.Editor.ComponentModel
{
	public class MenuCommandService : IMenuCommandService
	{
		private DesignerVerbCollection verbs;
		private ArrayList commands;
		private Menu contextMenu;
		private MenuBar menuBar;
		private int x = 0, y = 0;

		public MenuCommandService()
		{
			commands = new ArrayList ();
			this.contextMenu = new Menu ();
			this.menuBar = new MenuBar ();
		}

		#region IMenuCommandService Members

		public void AddCommand (MenuCommand command)
		{
			if (commands.Contains (command))
				throw new InvalidOperationException ("A command with that CommandID already exists in the menu");

			commands.Add (command);
			//menuBar
		}

		public void AddVerb (DesignerVerb verb)
		{
			if (verbs.Contains (verb))
				throw new InvalidOperationException ("The MenuCommandService already contains that Designer Verb");
			verbs.Add (verb);
		}

		public MenuCommand FindCommand (CommandID commandID)
		{
			foreach (MenuCommand command in commands)
				if (command.CommandID == commandID)
					return command;
			return null;
		}

		public bool GlobalInvoke (CommandID commandID)
		{
			MenuCommand command = FindCommand (commandID);
			if (command == null)
				return false;

			command.Invoke ();
			return true;
		}

		public void RemoveCommand (MenuCommand command)
		{
			if (commands.Contains (command))
				commands.Remove (command);
		}

		public void RemoveVerb (DesignerVerb verb)
		{
			if (verbs.Contains (verb))
				verbs.Remove (verb);
		}

		public void ShowContextMenu (CommandID menuID, int x, int y)
		{
			// Launch out menu as a GTK popup
			// Due to weird callback semantics, have to cache x and y values
			// Delegate doesn't accept the data pointer...?!
			this.x = x;
			this.y = y;
			contextMenu.Popup(null, null, new MenuPositionFunc (positionFunc), 2, Gtk.Global.CurrentEventTime);
		}

		private void positionFunc (Gtk.Menu menu, out int x, out int y, out bool pushIn)
		{
			x = this.x;
			y = this.y;
			pushIn = false;
		}

		public DesignerVerbCollection Verbs {
			get { return verbs; }
		}

		public MenuBar MenuBar {
			get { return menuBar; }
		}

		#endregion
}
}
