//
// CommandFrame.cs
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
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Commands
{
	public class CommandFrame: DockToolbarFrame
	{
		CommandManager manager;
		
		public CommandFrame (CommandManager manager)
		{
			this.manager = manager;
			manager.RegisterGlobalHandler (this);
		}
		
		[CommandHandler (CommandSystemCommands.ToolbarList)]
		protected void OnViewToolbar (object ob)
		{
			IDockToolbar bar = (IDockToolbar) ob;
			bar.Visible = !bar.Visible;
		}
		
		[CommandUpdateHandler (CommandSystemCommands.ToolbarList)]
		protected void OnUpdateViewToolbar (CommandArrayInfo info)
		{
			foreach (IDockToolbar bar in Toolbars) {
				CommandInfo cmd = new CommandInfo (bar.Title);
				cmd.Checked = bar.Visible;
				info.Add (cmd, bar);
			}
		}
		
		protected override void OnPanelClick (Gdk.EventButton e, Placement placement)
		{
			if (e.Button == 3) {
				CommandEntrySet opset = new CommandEntrySet ();
				opset.AddItem (CommandSystemCommands.ToolbarList);
				Gtk.Menu menu = manager.CreateMenu (opset);
				menu.Popup (null, null, null, 0, e.Time);
			}
			base.OnPanelClick (e, placement);
		}
	}
}
