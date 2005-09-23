//
// MenuButtonEntry.cs
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
using MonoDevelop.Commands;

namespace MonoDevelop.Gui.Components
{
	public class MenuButtonEntry : Gtk.HBox
	{
		Gtk.Entry entry;
		ArrayList options = new ArrayList ();
		
		CommandManager manager;
		CommandEntrySet entrySet;
		
		public MenuButtonEntry (): this (null, null)
		{
		}
		
		public MenuButtonEntry (string [,] options): this (null, null, options)
		{
		}
		
		public MenuButtonEntry (Gtk.Entry entry, Gtk.Button button, string [,] options): this (entry, button)
		{
			for (int n=0; n<options.GetLength (0); n++)
				AddOption (options [n,0], options [n,1]);
		}
		
		public MenuButtonEntry (Gtk.Entry entry, Gtk.Button button)
		{
			if (entry == null) entry = new Gtk.Entry ();
			if (button == null) button = new Gtk.Button (">");
			
			this.entry = entry;
			
			manager = new CommandManager ();
			manager.RegisterGlobalHandler (this);
			
			PackStart (entry, true, true, 0);
			PackStart (button, false, false, 6);
			
			ActionCommand cmd = new ActionCommand ("InsertOption", "InsertOption", null);
			cmd.CommandArray = true;
			manager.RegisterCommand (cmd);
			entrySet = new CommandEntrySet ();
			entrySet.AddItem ("InsertOption");
			
			button.Clicked += new EventHandler (ShowQuickInsertMenu);
		}
		
		public void AddOption (string name, string value)
		{
			options.Add (new string[] { name, value });
		}
		
		public void AddSeparator ()
		{
			options.Add (new string[] {"-", null});
		}
		
		public void ShowQuickInsertMenu (object sender, EventArgs args)
		{
			manager.ShowContextMenu (entrySet);
		}
		
		[CommandHandler ("InsertOption")]
		protected void OnUpdateInsertOption (object selection)
		{
			int tempInt = entry.Position;
			entry.DeleteSelection();
			entry.InsertText ((string)selection, ref tempInt);
		}
		
		[CommandUpdateHandler ("InsertOption")]
		protected void OnUpdateInsertOption (CommandArrayInfo info)
		{
			foreach (string[] op in options) {
				if (op [0] == "-")
					info.AddSeparator ();
				else
					info.Add (op [0], op [1]);
			}
		}
	}
}
