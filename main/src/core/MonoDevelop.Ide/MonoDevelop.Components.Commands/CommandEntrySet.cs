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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands
{
	public class CommandEntrySet: CommandEntry, IEnumerable<CommandEntry>
	{
		List<CommandEntry> cmds = new List<CommandEntry> ();
		string name;
		IconId icon;
		bool autoHide;
		
		public CommandEntrySet (): base ((object)null)
		{
		}
		
		public CommandEntrySet (string name, IconId icon): base ((object)null)
		{
			this.name = name;
			this.icon = icon;
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public IconId Icon {
			get { return icon; }
			set { icon = value; }
		}
		
		public bool AutoHide {
			get { return autoHide; }
			set { autoHide = value; }
		}
		
		public void Add (CommandEntry entry)
		{
			var cset = entry as CommandEntrySet;
			// If entry is just a normal CommandEntry then add it to the commands as normal
			if (cset == null) {
				cmds.Add (entry);
				return;
			}

			// If entry is a CommandEntrySet then attempt to de-duplicate it
			// by looking for an existing entry with the matching Id and adding
			// entry's commands to it
			foreach (var e in cmds) {
				if (Equals (e.CommandId, entry.CommandId)) {
					var eset = e as CommandEntrySet;
					if (eset == null) {
						continue;
					}

					eset.cmds.AddRange (cset.cmds);
					return;
				}
			}

			// Couldn't find a valid duplicate command set so just add as normal
			cmds.Add (entry);
		}
		
		public void Add (Command cmd)
		{
			cmds.Add (new CommandEntry (cmd));
		}
		
		public CommandEntry AddItem (object cmdId)
		{
			CommandEntry cmd = new CommandEntry (cmdId);
			cmds.Add (cmd);
			return cmd;
		}
		
		public void AddSeparator ()
		{
			AddItem (Command.Separator);
		}
		
		public CommandEntrySet AddItemSet (string name)
		{
			return AddItemSet (name, "");
		}
		
		public CommandEntrySet AddItemSet (string name, IconId icon)
		{
			CommandEntrySet cmdset = new CommandEntrySet (name, icon);
			cmds.Add (cmdset);
			return cmdset;
		}

		public List<CommandEntry>.Enumerator GetEnumerator ()
		{
			return cmds.GetEnumerator ();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return cmds.GetEnumerator ();
		}

		IEnumerator<CommandEntry> IEnumerable<CommandEntry>.GetEnumerator ()
		{
			return cmds.GetEnumerator ();
		}
		
		public int Count {
			get { return cmds.Count; }
		}
		
		internal override Gtk.MenuItem CreateMenuItem (CommandManager manager)
		{
			Gtk.MenuItem mi;
			if (autoHide)
				mi = new AutoHideMenuItem (name != null ? name : "");
			else
				mi = new Gtk.MenuItem (name != null ? name : "");
			mi.Submenu = new CommandMenu (manager, this);
			return mi;
		}

		internal override Gtk.ToolItem CreateToolItem (CommandManager manager)
		{
			Gtk.Menu menu = manager.CreateMenu (this);
			return new MenuToolButton (menu, icon);
		}
	}
	
	class AutoHideMenuItem: Gtk.ImageMenuItem
	{
		public AutoHideMenuItem (string name): base (name)
		{
			ShowAll ();
		}
		
		public bool HasVisibleChildren {
			get {
				if (!Submenu.IsRealized)
					Submenu.Realize ();
				foreach (Gtk.Widget item in ((Gtk.Menu)Submenu).Children) {
					if (item.Visible)
						return true;
				}
				return false;
			}
		}
	}
}

