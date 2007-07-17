//
// KeyBindingsPanel.cs
//
// Authors: Jeffrey Stedfast <fejj@novell.com>
//          Balaji Rao <balajirrao@gmail.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using Gtk;

namespace MonoDevelop.Ide.Gui.OptionPanels {
	public partial class KeyBindingsPanel : Gtk.Bin, IDialogPanel {
		static readonly int commandCol = 0;
		static readonly int labelCol = 1;
		static readonly int bindingCol = 2;
		static readonly int descCol = 3;
		
		bool accelIncomplete = false;
		bool accelComplete = false;
		ListStore keyStore;
		string mode;
		
		public KeyBindingsPanel ()
		{
			this.Build ();
			
			keyStore = new ListStore (typeof (Command), typeof (string), typeof (string), typeof (string));
			keyTreeView.Model = keyStore;
			keyTreeView.AppendColumn ("Command", new CellRendererText (), "text", labelCol);
			keyTreeView.AppendColumn ("Key Binding", new CellRendererText (), "text", bindingCol);
			keyTreeView.AppendColumn ("Description", new CellRendererText (), "text", descCol);
			
			keyTreeView.Selection.Changed += new EventHandler (OnKeysTreeViewSelectionChange);
			
			accelEntry.KeyPressEvent += new KeyPressEventHandler (OnAccelEntryKeyPress);
			accelEntry.KeyReleaseEvent += new KeyReleaseEventHandler (OnAccelEntryKeyRelease);
			updateButton.Clicked += new EventHandler (OnUpdateButtonClick);
			
			schemeCombo.AppendText ("Current");
			List<string> schemes = KeyBindingService.SchemeNames;
			for (int i = 0; i < schemes.Count; i++)
				schemeCombo.AppendText (schemes[i]);
			
			schemeCombo.Active = 0;
			schemeCombo.Changed += new EventHandler (OnKeyBindingSchemeChanged);
		}
		
		public bool StorePanelContents ()
		{
			TreeModel model = (TreeModel) keyStore;
			Command command;
			TreeIter iter;
			
			if (!model.GetIterFirst (out iter))
				return true;
			
			do {
				command = (Command) model.GetValue (iter, commandCol);
				command.AccelKey = (string) model.GetValue (iter, bindingCol);
				KeyBindingService.StoreBinding (command);
			} while (model.IterNext (ref iter));
			
			KeyBindingService.SaveCurrentBindings ();
			
			return true;
		}
		
		public void LoadPanelContents ()
		{
			SortedDictionary<string, Command> commands = new SortedDictionary<string, Command> ();
			object[] cmds = AddinManager.GetExtensionObjects ("/SharpDevelop/Commands");
			
			foreach (object c in cmds) {
				Command cmd = c as Command;
				string label = cmd.Text.Replace ("_", String.Empty);
				
				if (label == String.Empty)
					continue;
				
				if (commands.ContainsKey (label)) {
					if (commands[label].AccelKey == null)
						commands[label] = cmd;
				} else {
					commands.Add (label, cmd);
				}
			}
			
			foreach (KeyValuePair<string, Command> pair in commands) {
				Command cmd = pair.Value;
				string label = pair.Key;
				
				keyStore.AppendValues (cmd, label, cmd.AccelKey != null ? cmd.AccelKey : String.Empty, cmd.Description);
			}
		}
		
		void OnKeyBindingSchemeChanged (object sender, EventArgs e)
		{
			TreeModel model = (TreeModel) keyStore;
			ComboBox combo = (ComboBox) sender;
			Command command;
			string binding;
			TreeIter iter;
			
			if (!model.GetIterFirst (out iter))
				return;
			
			if (combo.Active > 0) {
				// Load a key binding template
				KeyBindingService.LoadScheme (combo.ActiveText);
				
				do {
					command = (Command) model.GetValue (iter, commandCol);
					binding = KeyBindingService.SchemeBinding (command);
					model.SetValue (iter, bindingCol, binding);
				} while (model.IterNext (ref iter));
				
				KeyBindingService.UnloadScheme ();
			} else {
				// Restore back to current settings...
				
				do {
					command = (Command) model.GetValue (iter, commandCol);
					model.SetValue (iter, bindingCol, command.AccelKey != null ? command.AccelKey : String.Empty);
				} while (model.IterNext (ref iter));
			}
		}
		
		void OnKeysTreeViewSelectionChange (object sender, EventArgs e)
		{
			TreeSelection sel = sender as TreeSelection;
			TreeModel model;
			TreeIter iter;
			
			accelComplete = false;
			
			if (sel.GetSelected (out model, out iter)) {
				accelEntry.Text = (string) model.GetValue (iter, bindingCol);
				accelEntry.GrabFocus ();
				accelIncomplete = false;
				accelComplete = true;
			}
		}
		
		[GLib.ConnectBefore]
		void OnAccelEntryKeyPress (object sender, KeyPressEventArgs e)
		{
			Gdk.ModifierType mod = e.Event.State;
			Gdk.Key key = e.Event.Key;
			string accel;
			
			e.RetVal = true;
			
			if (accelComplete) {
				accelEntry.Text = String.Empty;
				accelIncomplete = false;
				accelComplete = false;
				mode = null;
				
				if (key.Equals (Gdk.Key.BackSpace))
					return;
			}
			
			accelComplete = false;
			if ((accel = KeyBindingManager.AccelFromKey (key, mod)) != null) {
				accelEntry.Text = KeyBindingManager.Binding (mode, accel);
				accelIncomplete = false;
				if (mode != null)
					accelComplete = true;
				else
					mode = accel;
			} else {
				accel = mode != null ? mode + "|" : String.Empty;
				accelIncomplete = true;
				
				if ((mod & Gdk.ModifierType.ControlMask) != 0)
					accel += "Control+";
				if ((mod & Gdk.ModifierType.Mod1Mask) != 0 ||
				    (key.Equals (Gdk.Key.Meta_L) || key.Equals (Gdk.Key.Meta_R)))
					accel += "Alt+";
				if ((mod & Gdk.ModifierType.ShiftMask) != 0)
					accel += "Shift+";
				
				if (key.Equals (Gdk.Key.Control_L) || key.Equals (Gdk.Key.Control_R))
					accel += "Control+";
				else if (key.Equals (Gdk.Key.Alt_L) || key.Equals (Gdk.Key.Alt_R))
					accel += "Alt+";
				else if (key.Equals (Gdk.Key.Shift_L) || key.Equals (Gdk.Key.Shift_R))
					accel += "Shift+";
				
				accelEntry.Text = accel;
			}
		}
		
		void OnAccelEntryKeyRelease (object sender, KeyReleaseEventArgs e)
		{
			if (accelIncomplete)
				accelEntry.Text = mode != null ? mode : String.Empty;
		}
		
		void OnUpdateButtonClick (object sender, EventArgs e)
		{
			TreeSelection sel = keyTreeView.Selection;
			TreeModel model;
			TreeIter iter;
			
			if (sel.GetSelected (out model, out iter))
				keyStore.SetValue (iter, bindingCol, accelEntry.Text);
		}
		
#region Cut & Paste from abstract option panel
		object customizationObject = null;
		bool wasActivated = false;
		bool isFinished = true;
		
		public Widget Control {
			get { return this; }
		}
		
		public virtual Gtk.Image Icon {
			get { return null; }
		}
		
		public bool WasActivated {
			get { return wasActivated; }
		}
		
		public virtual object CustomizationObject {
			get { return customizationObject; }
			set {
				customizationObject = value;
				OnCustomizationObjectChanged ();
			}
		}
		
		public virtual bool EnableFinish {
			get { return isFinished; }
			set {
				if (isFinished != value) {
					isFinished = value;
					OnEnableFinishChanged ();
				}
			}
		}
		
		public virtual bool ReceiveDialogMessage (DialogMessage message)
		{
			try {
				switch (message) {
				case DialogMessage.Activated:
					if (!wasActivated) {
						LoadPanelContents ();
						wasActivated = true;
					}
					break;
				case DialogMessage.OK:
					if (wasActivated)
						return StorePanelContents ();
					break;
				}
			} catch (Exception ex) {
				Services.MessageService.ShowError (ex);
			}
			
			return true;
		}
		
		protected virtual void OnEnableFinishChanged ()
		{
			if (EnableFinishChanged != null)
				EnableFinishChanged (this, null);
		}
		
		protected virtual void OnCustomizationObjectChanged ()
		{
			if (CustomizationObjectChanged != null)
				CustomizationObjectChanged (this, null);
		}
		
		public event EventHandler CustomizationObjectChanged;
		public event EventHandler EnableFinishChanged;
#endregion
	}
}
