//
// CommandToolButton.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands
{
	class CommandToolButton: Gtk.ToolButton, ICommandUserItem
	{
		CommandManager commandManager;
		object commandId;
		string lastDesc;
		object initialTarget;
		CommandInfo lastCmdInfo;
		
		public CommandToolButton (object commandId, CommandManager commandManager): base ("")
		{
			this.commandId = commandId;
			this.commandManager = commandManager;
			UseUnderline = true;
		}
		
		protected override void OnParentSet (Gtk.Widget parent)
		{
			base.OnParentSet (parent);
			if (Parent == null) return;

			((ICommandUserItem)this).Update (new CommandTargetRoute ());
		}
		
		void ICommandUserItem.Update (CommandTargetRoute targetRoute)
		{
			if (commandManager != null) {
				CommandInfo cinfo = commandManager.GetCommandInfo (commandId, targetRoute);
				this.initialTarget = targetRoute.InitialTarget;
				Update (cinfo);
			}
		}
		
		protected override void OnClicked ()
		{
			base.OnClicked ();

			if (commandManager == null)
				throw new InvalidOperationException ();
				
			commandManager.DispatchCommand (commandId, null, initialTarget, CommandSource.MainToolbar, lastCmdInfo);
		}
		
		IconId stockId = null;
		ImageView iconWidget;
		
		void Update (CommandInfo cmdInfo)
		{
			lastCmdInfo = cmdInfo;
			if (lastDesc != cmdInfo.Description) {
				string toolTip;
				if (string.IsNullOrEmpty (cmdInfo.AccelKey)) {
					toolTip = cmdInfo.Description;
				} else {
					toolTip = cmdInfo.Description + " (" + KeyBindingManager.BindingToDisplayLabel (cmdInfo.AccelKey, false) + ")";
				}
				TooltipText = toolTip;
				lastDesc = cmdInfo.Description;
			}
			
			if (Label != cmdInfo.Text)
				Label = cmdInfo.Text;
			if (cmdInfo.Icon != stockId) {
				stockId = cmdInfo.Icon;
				this.IconWidget = iconWidget = new ImageView (cmdInfo.Icon, Gtk.IconSize.Menu);
			}
			if (IconWidget != null && cmdInfo.Enabled != Sensitive)
				iconWidget.Image = iconWidget.Image.WithStyles (cmdInfo.Enabled ? "" : "disabled").WithAlpha (cmdInfo.Enabled ? 1.0 : 0.4);
			if (cmdInfo.Enabled != Sensitive)
				Sensitive = cmdInfo.Enabled;
			if (cmdInfo.Visible != Visible)
				Visible = cmdInfo.Visible;
			if (cmdInfo.Icon.IsNull)
				IsImportant = true;
		}
	}
}
