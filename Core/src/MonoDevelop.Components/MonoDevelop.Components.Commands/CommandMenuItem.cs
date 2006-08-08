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

namespace MonoDevelop.Components.Commands
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
		bool wasButtonActivation;
		object initialTarget;
		
		public CommandMenuItem (object commandId, CommandManager commandManager): base ("")
		{
			this.commandId = commandId;
			this.commandManager = commandManager;
			ActionCommand cmd = commandManager.GetCommand (commandId) as ActionCommand;
			if (cmd != null)
				isArray = cmd.CommandArray;
		}
		
		void ICommandUserItem.Update (object initialTarget)
		{
			if (commandManager != null && !isArrayItem) {
				CommandInfo cinfo = commandManager.GetCommandInfo (commandId, initialTarget);
				this.initialTarget = initialTarget;
				Update (cinfo);
			}
		}
		
		void ICommandMenuItem.SetUpdateInfo (CommandInfo cmdInfo, object initialTarget)
		{
			isArrayItem = true;
			this.initialTarget = initialTarget;
			arrayDataItem = cmdInfo.DataItem;
			Update (cmdInfo);
		}
		
		protected override void OnParentSet (Gtk.Widget parent)
		{
			base.OnParentSet (parent);
			if (Parent == null) return;
			
			((ICommandUserItem)this).Update (null);
			
			if (!isArrayItem) {
				// Make sure the accelerators allways work for this item
				// while the menu is hidden
				Sensitive = true;
				Visible = true;
			}
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton ev)
		{
			wasButtonActivation = true;
			return base.OnButtonReleaseEvent (ev);
		}
		
		protected override void OnActivated ()
		{
			base.OnActivated ();

			if (commandManager == null)
				throw new InvalidOperationException ();
			
			if (!wasButtonActivation) {
				// It's being activated by an accelerator.
				commandManager.DispatchCommandFromAccel (commandId, arrayDataItem, initialTarget);
			}
			else {
				wasButtonActivation = false;
				commandManager.DispatchCommand (commandId, arrayDataItem, initialTarget);
			}
		}
		
		void Update (CommandInfo cmdInfo)
		{
			if (isArray && !isArrayItem)
			{
				this.Visible = false;
				Gtk.Menu menu = (Gtk.Menu) Parent;  
				
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
							mi.SetUpdateInfo (info, initialTarget);
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
				
				if (cmdInfo is CommandInfoSet) {
					CommandInfoSet ciset = (CommandInfoSet) cmdInfo;
					Gtk.Menu smenu = new Gtk.Menu ();
					Submenu = smenu;
					foreach (CommandInfo info in ciset.CommandInfos) {
						Gtk.MenuItem item;
						if (info.IsArraySeparator) {
							item = new Gtk.SeparatorMenuItem ();
							item.Show ();
						} else {
							item = CommandEntry.CreateMenuItem (commandManager, commandId, false);
							ICommandMenuItem mi = (ICommandMenuItem) item; 
							mi.SetUpdateInfo (info, initialTarget);
						}
						smenu.Add (item);
					}
				}
			}
		}
	}
}
