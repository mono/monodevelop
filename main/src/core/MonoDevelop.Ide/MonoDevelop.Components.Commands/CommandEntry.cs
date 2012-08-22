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

namespace MonoDevelop.Components.Commands
{
	public class CommandEntry
	{
		object cmdId;
		string overrideLabel;
		bool disabledVisible = true;
		Command localCmd;

		public CommandEntry (object cmdId, string overrideLabel, bool disabledVisible)
		{
			this.cmdId         = CommandManager.ToCommandId (cmdId);
			this.overrideLabel = overrideLabel;
			this.disabledVisible = disabledVisible;
		}
		
		public CommandEntry (object cmdId, string overrideLabel): this (cmdId, overrideLabel, true)
		{
		}
		
		public CommandEntry (object cmdId) : this (cmdId, null, true)
		{
		}
		
		public CommandEntry (Command localCmd) : this (localCmd.Id, null, true)
		{
			this.localCmd = localCmd;
		}
		
		public object CommandId {
			get { return cmdId; }
			set { cmdId = CommandManager.ToCommandId (value); }
		}
		
		public bool DisabledVisible {
			get { return disabledVisible; }
			set { disabledVisible = value; }
		}
		
		public virtual Command GetCommand (CommandManager manager)
		{
			if (localCmd != null) {
				if (manager.GetCommand (localCmd.Id) == null)
					manager.RegisterCommand (localCmd);
				localCmd = null;
			}
				
			return manager.GetCommand (cmdId);
		}
		
		internal protected virtual Gtk.MenuItem CreateMenuItem (CommandManager manager)
		{
			return CreateMenuItem (manager, GetCommand (manager), cmdId, true, overrideLabel, disabledVisible);
		}
		
		internal protected virtual Gtk.ToolItem CreateToolItem (CommandManager manager)
		{
			if (cmdId == CommandManager.ToCommandId (Command.Separator))
				return new Gtk.SeparatorToolItem ();

			Command cmd = GetCommand (manager);
			if (cmd == null)
				return new Gtk.ToolItem ();
			
			if (cmd is CustomCommand) {
				Gtk.Widget child = (Gtk.Widget) Activator.CreateInstance (((CustomCommand)cmd).WidgetType);
				Gtk.ToolItem ti;
				if (child is Gtk.ToolItem)
					ti = (Gtk.ToolItem) child;
				else {
					ti = new Gtk.ToolItem ();
					ti.Child = child;
				}
				if (cmd.Text != null && cmd.Text.Length > 0) {
					//strip "_" accelerators from tooltips
					string text = cmd.Text;
					while (true) {
						int underscoreIndex = text.IndexOf ('_');
						if (underscoreIndex > -1)
							text = text.Remove (underscoreIndex, 1);
						else
							break;
					}
					ti.TooltipText = text;
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
			return CreateMenuItem (manager, null, cmdId, isArrayMaster, null, true);
		}

		static Gtk.MenuItem CreateMenuItem (CommandManager manager, Command cmd, object cmdId, bool isArrayMaster, string overrideLabel, bool disabledVisible)
		{
			cmdId = CommandManager.ToCommandId (cmdId);
			if (cmdId == CommandManager.ToCommandId (Command.Separator))
				return new Gtk.SeparatorMenuItem ();
			
			if (cmd == null)
				cmd = manager.GetCommand (cmdId);

			if (cmd == null) {
				MonoDevelop.Core.LoggingService.LogWarning ("Unknown command '{0}'", cmdId);
				return new Gtk.MenuItem ("<Unknown Command>");
			}
			
			if (cmd is CustomCommand) {
				Gtk.Widget child = (Gtk.Widget) Activator.CreateInstance (((CustomCommand)cmd).WidgetType);
				CustomMenuItem ti = new CustomMenuItem ();
				ti.Child = child;
				return ti;
			}
			
			ActionCommand acmd = cmd as ActionCommand;
			if (acmd.ActionType == ActionType.Normal || (isArrayMaster && acmd.CommandArray))
				return new CommandMenuItem (cmdId, manager, overrideLabel, disabledVisible);
			else
				return new CommandCheckMenuItem (cmdId, manager, overrideLabel, disabledVisible);
		}	
	}
}

