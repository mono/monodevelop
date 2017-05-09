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
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands
{
	class CommandMenuItem: Gtk.ImageMenuItem, ICommandMenuItem
	{
		CommandManager commandManager;
		object commandId;
		bool isArray;
		bool isArrayItem;
		object arrayDataItem;
		ArrayList itemArray;
		IconId lastIcon;
		string overrideLabel;
		bool wasButtonActivation;
		object initialTarget;
		bool disabledVisible = true;
		CommandInfo lastCmdInfo;
		
		public CommandMenuItem (object commandId, CommandManager commandManager, string overrideLabel, bool disabledVisible): base ("")
		{
			this.commandId = commandId;
			this.commandManager = commandManager;
			this.overrideLabel = overrideLabel;
			this.disabledVisible = disabledVisible;
			ActionCommand cmd = commandManager.GetCommand (commandId) as ActionCommand;
			if (cmd != null)
				isArray = cmd.CommandArray;
		}
		
		public CommandMenuItem (object commandId, CommandManager commandManager): this (commandId, commandManager, null, true)
		{
		}

		void ICommandUserItem.Update (CommandTargetRoute targetRoute)
		{
			if (commandManager != null && !isArrayItem) {
				CommandInfo cinfo = commandManager.GetCommandInfo (commandId, targetRoute);
				this.initialTarget = targetRoute.InitialTarget;
				Update (cinfo);
			}
		}
		
		internal static CommandSource GetMenuCommandSource (Gtk.MenuItem item)
		{
			Gtk.Widget w = item.Parent;
			while (w != null) {
				if (w is Gtk.MenuBar)
					return CommandSource.MainMenu;
				else if (!(w is Gtk.MenuItem) && !(w is Gtk.MenuShell))
					return CommandSource.ContextMenu;
				else
					w = w.Parent;
			}
			return CommandSource.Unknown;
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
			if (Parent == null)
				return;

			if (Parent.Visible)
				((ICommandUserItem)this).Update (new CommandTargetRoute ());
			
			if (!isArrayItem) {
				// Make sure the accelerators always work for this item
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
				if (Submenu == null)
					commandManager.DispatchCommandFromAccel (commandId, arrayDataItem, initialTarget);
			} else {
				wasButtonActivation = false;
				commandManager.DispatchCommand (commandId, arrayDataItem, initialTarget, GetMenuCommandSource (this), lastCmdInfo);
			}
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

		void CommandInfoChanged (object sender, EventArgs e)
		{
			Update ((CommandInfo)sender);
		}

		void Update (CommandInfo cmdInfo)
		{
			if (lastCmdInfo != null) {
				lastCmdInfo.CancelAsyncUpdate ();
				lastCmdInfo.Changed -= CommandInfoChanged;
			}
			lastCmdInfo = cmdInfo;
			lastCmdInfo.Changed += CommandInfoChanged;

			if (isArray && !isArrayItem) {
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
			} else {
				Gtk.Widget child = Child;
				if (child == null)
					return;
				
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

				if (!string.IsNullOrEmpty (cmdInfo.Description) && label.TooltipText != cmdInfo.Description)
					label.TooltipText = cmdInfo.Description;
				label.UseUnderline = true;
				
				this.Sensitive = cmdInfo.Enabled;
				this.Visible = cmdInfo.Visible && (disabledVisible || cmdInfo.Enabled);
				
				if (!cmdInfo.Icon.IsNull && cmdInfo.Icon != lastIcon) {
					Image = new ImageView (cmdInfo.Icon, Gtk.IconSize.Menu);
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

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			itemArray = null;
			initialTarget = null;
			arrayDataItem = null;
			if (lastCmdInfo != null) {
				lastCmdInfo.CancelAsyncUpdate ();
				lastCmdInfo.Changed -= CommandInfoChanged;
				lastCmdInfo = null;
			}
		}
	}
}
