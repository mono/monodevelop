//
// CommandCheckMenuItem.cs
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

namespace MonoDevelop.Commands
{
	internal class CommandCheckMenuItem: Gtk.CheckMenuItem, ICommandMenuItem
	{
		CommandManager commandManager;
		object commandId;
		bool updating;
		bool isArrayItem;
		object arrayDataItem;
		
		public CommandCheckMenuItem (object commandId, CommandManager commandManager): base ("")
		{
			this.commandId = commandId;
			this.commandManager = commandManager;
			ActionCommand cmd = commandManager.GetCommand (commandId) as ActionCommand;
			if (cmd != null && cmd.ActionType == ActionType.Radio)
				this.DrawAsRadio = true; 
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
			
			if (updating) return;

			if (commandManager == null)
				throw new InvalidOperationException ();

			commandManager.DispatchCommand (commandId, arrayDataItem);
		}
		
		void Update (CommandInfo cmdInfo)
		{
			updating = true;
			Gtk.AccelLabel child = (Gtk.AccelLabel)Child;
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
			Sensitive = cmdInfo.Enabled;
			child.ShowAll ();
			Visible = cmdInfo.Visible;
			Active = cmdInfo.Checked;
			if (cmdInfo.AccelKey != null && cmdInfo.AccelKey != "")
				this.AccelPath = commandManager.GetAccelPath (cmdInfo.AccelKey);
				
			updating = false;
		}
	}
}
