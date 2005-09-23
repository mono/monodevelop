//
// CommandEntrySet.cs
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
using System.Collections;

namespace MonoDevelop.Commands
{
	public class CommandEntrySet: CommandEntry, IEnumerable
	{
		ArrayList cmds = new ArrayList ();
		string name;
		string icon;
		bool autoHide;
		
		public CommandEntrySet (): base (null)
		{
		}
		
		public CommandEntrySet (string name, string icon): base (null)
		{
			this.name = name;
			this.icon = icon;
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public string Icon {
			get { return icon; }
			set { icon = value; }
		}
		
		// If true, the set will be automatically hidden if all
		// items it contains are hidden or disabled
		public bool AutoHide {
			get { return autoHide; }
			set { autoHide = value; }
		}
		
		public void Add (CommandEntry entry)
		{
			cmds.Add (entry);
		}
		
		public CommandEntry AddItem (object cmdId)
		{
			CommandEntry cmd = new CommandEntry (cmdId);
			cmds.Add (cmd);
			return cmd;
		}
		
		public CommandEntrySet AddItemSet (string name)
		{
			return AddItemSet (name, "");
		}
		
		public CommandEntrySet AddItemSet (string name, string icon)
		{
			CommandEntrySet cmdset = new CommandEntrySet (name, icon);
			cmds.Add (cmdset);
			return cmdset;
		}
		
		public IEnumerator GetEnumerator ()
		{
			return cmds.GetEnumerator ();
		}
		
		internal protected override Gtk.MenuItem CreateMenuItem (CommandManager manager)
		{
			Gtk.MenuItem mi = new Gtk.MenuItem (name != null ? name : "");
			mi.Submenu = manager.CreateMenu (this);
			return mi;
		}

		internal protected override Gtk.ToolItem CreateToolItem (CommandManager manager)
		{
			Gtk.Menu menu = manager.CreateMenu (this);
			return new MenuToolButton (menu, icon);
		}
	}
	
	public class AutoHideMenuItem: Gtk.ImageMenuItem
	{
		public AutoHideMenuItem (string name): base (name)
		{
			Console.WriteLine ("MM:" + name);
		}
/*		
		protected override void OnShown ()
		{
			base.OnShown ();
		}
*/	}
}

