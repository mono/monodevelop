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

namespace MonoDevelop.Components.Commands
{
	internal class CommandCheckMenuItem: Gtk.CheckMenuItem, ICommandMenuItem
	{
		CommandManager commandManager;
		object commandId;
		bool updating;
		bool isArrayItem;
		object arrayDataItem;
		object initialTarget;
		string overrideLabel;
		CommandInfo lastCmdInfo;
		bool disabledVisible = true;
		
		public CommandCheckMenuItem (object commandId, CommandManager commandManager, string overrideLabel, bool disabledVisible): base ("")
		{
			this.commandId = commandId;
			this.commandManager = commandManager;
			this.overrideLabel = overrideLabel;
			this.disabledVisible = disabledVisible;
			
			ActionCommand cmd = commandManager.GetCommand (commandId) as ActionCommand;
			if (cmd != null && cmd.ActionType == ActionType.Radio)
				this.DrawAsRadio = true;
		}

		public CommandCheckMenuItem (object commandId, CommandManager commandManager): this (commandId, commandManager, null, true)
		{
		}
		
		void ICommandUserItem.Update (CommandTargetRoute targetChain)
		{
			if (commandManager != null && !isArrayItem) {
				CommandInfo cinfo = commandManager.GetCommandInfo (commandId, targetChain);
				this.initialTarget = targetChain.InitialTarget;
				Update (cinfo);
			}
		}
		
		void ICommandMenuItem.SetUpdateInfo (CommandInfo cmdInfo, object initialTarget)
		{
			isArrayItem = true;
			arrayDataItem = cmdInfo.DataItem;
			this.initialTarget = initialTarget;
			Update (cmdInfo);
		}
		
		protected override void OnParentSet (Gtk.Widget parent)
		{
			base.OnParentSet (parent);
			if (Parent == null)
				return;
			
			((ICommandUserItem)this).Update (new CommandTargetRoute ());
			
			// Make sure the accelerators always work for this item
			// while the menu is hidden
			Sensitive = true;
			Visible = true;
		}
		
		protected override void OnActivated ()
		{
			base.OnActivated ();
			
			if (updating)
				return;
			
			if (commandManager == null)
				throw new InvalidOperationException ();
			
			commandManager.DispatchCommand (commandId, arrayDataItem, initialTarget, CommandMenuItem.GetMenuCommandSource (this), lastCmdInfo);
		}
		
		protected override void OnSelected ()
		{
			if (commandManager != null)
				commandManager.NotifySelected (lastCmdInfo);
			base.OnSelected ();
		}
		
		protected override void OnDeselected ()
		{
			if (commandManager != null)
				commandManager.NotifyDeselected ();
			base.OnDeselected ();
		}
		
		void Update (CommandInfo cmdInfo)
		{
			lastCmdInfo = cmdInfo;
			
			Gtk.Widget child = Child;
			if (child == null)
				return;
			
			updating = true;
			
			Gtk.Label accel_label = null;
			Gtk.Label label = null;
			
			if (!(child is Gtk.HBox)) {
				child = new Gtk.HBox (false, 0);
				accel_label = new Gtk.Label ("");
				accel_label.UseUnderline = false;
				accel_label.Xalign = 1.0f;
				accel_label.Show ();
				
				label = new Gtk.Label ("");
				label.UseUnderline = true;
				label.Xalign = 0.0f;
				label.Show ();
				
				((Gtk.Box) child).PackStart (label);
				((Gtk.Box) child).PackStart (accel_label);
				child.Show ();
				
				this.Remove (Child);
				this.Add (child);
			} else {
				accel_label = (Gtk.Label) ((Gtk.Box) child).Children[1];
				label = (Gtk.Label) ((Gtk.Box) child).Children[0];
			}
			
			if (cmdInfo.AccelKey != null)
				accel_label.Text = "    " + KeyBindingManager.BindingToDisplayLabel (cmdInfo.AccelKey, true);
			else
				accel_label.Text = String.Empty;
			
			if (cmdInfo.UseMarkup) {
				label.Markup = overrideLabel ?? cmdInfo.Text;
				label.UseMarkup = true;
			} else {
				label.Text = overrideLabel ?? cmdInfo.Text;
				label.UseMarkup = false;
			}
			
			label.UseUnderline = true;
			
			Sensitive = cmdInfo.Enabled;
			Visible = cmdInfo.Visible && (disabledVisible || cmdInfo.Enabled);
			Active = cmdInfo.Checked;
			Inconsistent = cmdInfo.CheckedInconsistent;
			
			updating = false;
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			initialTarget = null;
			arrayDataItem = null;
			lastCmdInfo = null;
		}
	}
}
