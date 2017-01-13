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
using System.Collections.Generic;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Components
{
	[System.ComponentModel.Category("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem (true)]
	public class MenuButtonEntry : Gtk.HBox
	{
		Gtk.Entry entry;
		Gtk.Button button;
		bool isOpen;
		List<string[]> options = new List<string[]> ();
		
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
			AddOptions (options);
		}
		
		public MenuButtonEntry (Gtk.Entry entry, Gtk.Button button)
		{
			if (entry == null) entry = new Gtk.Entry ();
			if (button == null) button = new Gtk.Button (new Gtk.Arrow (Gtk.ArrowType.Right, Gtk.ShadowType.Out));
			
			this.entry = entry;
			this.button = button;
			
			manager = new CommandManager ();
			manager.RegisterGlobalHandler (this);
			
			if (entry.Parent == null)
				PackStart (entry, true, true, 0);
			if (button.Parent == null)
				PackStart (button, false, false, 2);
			
			ActionCommand cmd = new ActionCommand ("InsertOption", "InsertOption", null);
			cmd.CommandArray = true;
			manager.RegisterCommand (cmd);
			entrySet = new CommandEntrySet ();
			entrySet.AddItem ("InsertOption");
			
			button.Clicked += ShowQuickInsertMenu;
			button.StateChanged += ButtonStateChanged;
		}

		void ButtonStateChanged (object o, Gtk.StateChangedArgs args)
		{
			//while the menu's open, make sure the button looks depressed
			if (isOpen && button.State != Gtk.StateType.Active)
				button.State = Gtk.StateType.Active;
		}
		
		public void AddOption (string name, string value)
		{
			options.Add (new string[] { name, value });
		}
		
		public void AddOptions (string [,] options)
		{
			if (options.GetLength (1) != 2)
				throw new ArgumentException ("The second dimension must be of size 2", "options");
			for (int n=0; n<options.GetLength (0); n++)
				AddOption (options [n,0], options [n,1]);
		}
		
		public void AddOptions (IEnumerable<string[]> options)
		{
			foreach (string[] optionPair in options) {
				if (optionPair.Length != 2)
					throw new ArgumentException ("One of the string arrays contains <> 2 items", "options");
				AddOption (optionPair[0], optionPair[1]);
			}
		}
		
		public void AddSeparator ()
		{
			options.Add (new string[] {"-", null});
		}
		
		public void ShowQuickInsertMenu (object sender, EventArgs args)
		{
			var menu = manager.CreateMenu (entrySet);
			
			//FIXME: taken from MonoDevelop.Components.MenuButton. should share this.
			
			isOpen = true;
			
			//make sure the button looks depressed
			Gtk.ReliefStyle oldRelief = button.Relief;
			button.Relief = Gtk.ReliefStyle.Normal;
			
			//clean up after the menu's done
			menu.Hidden += delegate {
				button.Relief = oldRelief ;
				isOpen = false;
				button.State = Gtk.StateType.Normal;
				
				//FIXME: for some reason the menu's children don't get activated if we destroy 
				//directly here, so use a timeout to delay it
				GLib.Timeout.Add (100, delegate {
					menu.Destroy ();
					return false;
				});
			};
			menu.Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
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
		
		public Gtk.Entry Entry {
			get { return entry; }
		}
		
		public string Text {
			get { return entry.Text; }
			set { entry.Text = value; }
		}
		
		//FIXME: taken from MonoDevelop.Components.MenuButton. should share this.
		void PositionFunc (Gtk.Menu mn, out int x, out int y, out bool push_in)
		{
			button.GdkWindow.GetOrigin (out x, out y);
			Gdk.Rectangle rect = button.Allocation;
			x += rect.X;
			y += rect.Y + rect.Height;
			
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > button.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			
			//let GTK reposition the menu if it still doesn't fit on the screen
			push_in = true;
		}

		protected override void OnDestroyed ()
		{
			if (manager != null) {
				manager.Dispose ();
				manager = null;
			}
			base.OnDestroyed ();
		}
	}
}
