//
// CommandMenuItem.cs
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
	public class CommandMenuItem: Gtk.ImageMenuItem, ICommandMenuItem
	{
		CommandManager commandManager;
		object commandId;
		bool isArray;
		bool isArrayItem;
		object arrayDataItem;
		ArrayList itemArray;
		string lastIcon;
		
		public CommandMenuItem (object commandId, CommandManager commandManager): base ("")
		{
			this.commandId = commandId;
			this.commandManager = commandManager;
			ActionCommand cmd = commandManager.GetCommand (commandId) as ActionCommand;
			if (cmd != null)
				isArray = cmd.CommandArray;
		}
		
		void ICommandUserItem.Update ()
		{
			if (commandManager != null && !isArrayItem) {
				CommandInfo cinfo = commandManager.GetCommandInfo (commandId);
				Update (cinfo);
			}
		}
		
		void ICommandMenuItem.SetUpdateInfo (CommandInfo cmdInfo)
		{
			isArrayItem = true;
			arrayDataItem = cmdInfo.DataItem;
			Update (cmdInfo);
		}
		
		protected override void OnParentSet (Gtk.Widget parent)
		{
			base.OnParentSet (parent);
			if (Parent == null) return;
			
			((ICommandUserItem)this).Update ();
			
			// Make sure the accelerators allways work for this item
			// while the menu is hidden
			Sensitive = true;
			Visible = true;
		}
		
		protected override void OnActivated ()
		{
			base.OnActivated ();

			if (commandManager == null)
				throw new InvalidOperationException ();
				
			commandManager.DispatchCommand (commandId, arrayDataItem);
		}
		
		void Update (CommandInfo cmdInfo)
		{
			if (isArray && !isArrayItem)
			{
				this.Visible = false;
				CommandMenu menu = (CommandMenu) Parent;  
				
				if (itemArray != null) {
					foreach (Gtk.MenuItem item in itemArray)
						menu.Remove (item);
				}
				
				itemArray = new ArrayList ();
				int i = Array.IndexOf (menu.Children, this);
				
				if (cmdInfo.ArrayInfo != null) {
					foreach (CommandInfo info in cmdInfo.ArrayInfo) {
						Gtk.MenuItem item;
						if (info.IsArraySeparator) {
							item = new Gtk.SeparatorMenuItem ();
							item.Show ();
						} else {
							item = CommandEntry.CreateMenuItem (commandManager, commandId, false);
							ICommandMenuItem mi = (ICommandMenuItem) item; 
							mi.SetUpdateInfo (info);
						}
						menu.Insert (item, ++i);
						itemArray.Add (item);
					}
				}
			}
			else {
				Gtk.AccelLabel child = (Gtk.AccelLabel)Child;
				if (child == null) return;
				child.Show ();
				child.Xalign = 0;
				if (cmdInfo.UseMarkup) {
					child.Markup = cmdInfo.Text;
					child.UseMarkup = true;
				} else {
					child.Text = cmdInfo.Text;
					child.UseMarkup = false;
				}
				child.UseUnderline = true;
				child.AccelWidget = this;
				this.Sensitive = cmdInfo.Enabled;
				this.Visible = cmdInfo.Visible;
				
				if (cmdInfo.AccelKey != null && cmdInfo.AccelKey != "")
					this.AccelPath = commandManager.GetAccelPath (cmdInfo.AccelKey);
				
				if (cmdInfo.Icon != null && cmdInfo.Icon != "" && cmdInfo.Icon != lastIcon) {
					Image = new Gtk.Image (cmdInfo.Icon, Gtk.IconSize.Menu);
					lastIcon = cmdInfo.Icon;
				}
			}
		}
	}
}
