//
// CommandEntry.cs
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
	public class CommandEntry
	{
		object cmdId;
		
		public CommandEntry (object cmdId)
		{
			this.cmdId = cmdId;
		}
		
		public object CommandId {
			get { return cmdId; }
			set { cmdId = value; }
		}
		
		internal protected virtual Gtk.MenuItem CreateMenuItem (CommandManager manager)
		{
			return CreateMenuItem (manager, cmdId, true);
		}
		
		internal protected virtual Gtk.ToolItem CreateToolItem (CommandManager manager)
		{
			if (cmdId == Command.Separator)
				return new Gtk.SeparatorToolItem ();

			Command cmd = manager.GetCommand (cmdId);
			if (cmd is CustomCommand) {
				Gtk.Widget child = (Gtk.Widget) Activator.CreateInstance (((CustomCommand)cmd).WidgetType);
				Gtk.ToolItem ti = new Gtk.ToolItem ();
				ti.Child = child;
				if (cmd.Text != null && cmd.Text.Length > 0) {
					Gtk.Tooltips tips = new Gtk.Tooltips ();
					ti.SetTooltip (tips, cmd.Text, cmd.Text);
					tips.Enable ();
				}
				return ti;
			}
			
			ActionCommand acmd = cmd as ActionCommand;
			if (acmd == null)
				throw new InvalidOperationException ("Unknown cmd type.");

			if (acmd.CommandArray) {
				CommandMenu menu = new CommandMenu (manager);
				menu.Append (CreateMenuItem (manager));
				return new MenuToolButton (menu, acmd.Icon);
			}
			else if (acmd.ActionType == ActionType.Normal)
				return new CommandToolButton (cmdId, manager);
			else
				return new CommandToggleToolButton (cmdId, manager);
		}
		
		internal static Gtk.MenuItem CreateMenuItem (CommandManager manager, object cmdId, bool isArrayMaster)
		{
			if (cmdId == Command.Separator)
				return new Gtk.SeparatorMenuItem ();
				
			Command cmd = manager.GetCommand (cmdId);
			if (cmd is CustomCommand) {
				Gtk.Widget child = (Gtk.Widget) Activator.CreateInstance (((CustomCommand)cmd).WidgetType);
				CustomMenuItem ti = new CustomMenuItem ();
				ti.Child = child;
				return ti;
			}
			
			ActionCommand acmd = cmd as ActionCommand;
			if (acmd.ActionType == ActionType.Normal || (isArrayMaster && acmd.CommandArray))
				return new CommandMenuItem (cmdId, manager);
			else
				return new CommandCheckMenuItem (cmdId, manager);
		}	
	}
}

