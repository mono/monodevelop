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

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using Gtk;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	public partial class KeyBindingsPanel : Gtk.Bin, IOptionsPanel
	{
		static readonly int commandCol = 0;
		static readonly int labelCol = 1;
		static readonly int bindingCol = 2;
		static readonly int descCol = 3;
		static readonly int boldCol = 4;
		static readonly int iconCol = 5;
		static readonly int iconVisibleCol = 6;
		
		bool accelIncomplete = false;
		bool accelComplete = false;
		TreeStore keyStore;
		string mode;
		
		public KeyBindingsPanel ()
		{
			this.Build ();
			
			keyStore = new TreeStore (typeof (Command), typeof (string), typeof (string), typeof (string), typeof (int), typeof(string), typeof(bool));
			keyTreeView.Model = keyStore;
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Command");
			col.Spacing = 4;
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock-id", iconCol);
			col.AddAttribute (crp, "visible", iconVisibleCol);
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", labelCol);
			col.AddAttribute (crt, "weight", boldCol);
			keyTreeView.AppendColumn (col);
			
			keyTreeView.AppendColumn (GettextCatalog.GetString ("Key Binding"), new CellRendererText (), "text", bindingCol);
			keyTreeView.AppendColumn (GettextCatalog.GetString ("Description"), new CellRendererText (), "text", descCol);
			
			keyTreeView.Selection.Changed += new EventHandler (OnKeysTreeViewSelectionChange);
			
			accelEntry.KeyPressEvent += new KeyPressEventHandler (OnAccelEntryKeyPress);
			accelEntry.KeyReleaseEvent += new KeyReleaseEventHandler (OnAccelEntryKeyRelease);
			updateButton.Clicked += new EventHandler (OnUpdateButtonClick);
			
			schemeCombo.AppendText (GettextCatalog.GetString ("Current"));
			foreach (string s in KeyBindingService.SchemeNames)
				schemeCombo.AppendText (s);
			
			schemeCombo.Active = 0;
			schemeCombo.Changed += new EventHandler (OnKeyBindingSchemeChanged);
		}
		
		public void ApplyChanges ()
		{
			TreeModel model = (TreeModel) keyStore;
			Command command;
			TreeIter iter;
			
			if (!model.GetIterFirst (out iter))
				return;
			
			do {
				TreeIter citer;
				model.IterChildren (out citer, iter);
				do {
					command = (Command) model.GetValue (citer, commandCol);
					command.AccelKey = (string) model.GetValue (citer, bindingCol);
					KeyBindingService.StoreBinding (command);
				} while (model.IterNext (ref citer));
			} while (model.IterNext (ref iter));
			
			KeyBindingService.SaveCurrentBindings ();
		}

		public Gtk.Widget CreatePanelWidget ()
		{
			SortedDictionary<string, Command> commands = new SortedDictionary<string, Command> ();
			List<string> catNames = new List<string> ();
			
			foreach (object c in IdeApp.CommandService.GetCommands ()) {
				ActionCommand cmd = c as ActionCommand;
				if (cmd == null || cmd.CommandArray)
					continue;
				
				string key;
				
				if (cmd.Id is Enum)
					key = cmd.Id.GetType () + "." + cmd.Id;
				else
					key = cmd.Id.ToString ();
				
				if (commands.ContainsKey (key)) {
					if (commands[key].AccelKey == null || commands[key].Text == String.Empty)
						commands[key] = cmd;
				} else {
					commands.Add (key, cmd);
				}
				
				if (!catNames.Contains (cmd.Category))
					catNames.Add (cmd.Category);
			}
			
			// Add the categories, sorted
			catNames.Sort ();
			Dictionary <string,TreeIter> categories = new Dictionary<string,TreeIter> ();
			foreach (string cat in catNames) {
				TreeIter icat;
				if (!categories.TryGetValue (cat, out icat)) {
					string name = cat.Length == 0 ? GettextCatalog.GetString ("Other") : cat;
					icat = keyStore.AppendValues (null, name, String.Empty, String.Empty, (int) Pango.Weight.Bold, null, false);
					categories [cat] = icat;
				}
			}
			
			foreach (KeyValuePair<string, Command> pair in commands) {
				Command cmd = pair.Value;
				
				string label = cmd.Text.Replace ("_", String.Empty);
				
				TreeIter icat = categories [cmd.Category];
				keyStore.AppendValues (icat, cmd, label, cmd.AccelKey != null ? cmd.AccelKey : String.Empty, cmd.Description, (int) Pango.Weight.Normal, cmd.Icon, true);
			}
			return this;
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
					TreeIter citer;
					model.IterChildren (out citer, iter);
					do {
						command = (Command) model.GetValue (citer, commandCol);
						binding = KeyBindingService.SchemeBinding (command);
						model.SetValue (citer, bindingCol, binding);
					} while (model.IterNext (ref citer));
				} while (model.IterNext (ref iter));
				
				KeyBindingService.UnloadScheme ();
			} else {
				// Restore back to current settings...
				
				do {
					TreeIter citer;
					model.IterChildren (out citer, iter);
					do {
						command = (Command) model.GetValue (citer, commandCol);
						model.SetValue (citer, bindingCol, command.AccelKey != null ? command.AccelKey : String.Empty);
					} while (model.IterNext (ref citer));
				} while (model.IterNext (ref iter));
			}
		}
		
		void OnKeysTreeViewSelectionChange (object sender, EventArgs e)
		{
			TreeSelection sel = sender as TreeSelection;
			TreeModel model;
			TreeIter iter;
			
			accelComplete = false;
			
			if (sel.GetSelected (out model, out iter) && model.GetValue (iter,commandCol) != null) {
				accelEntry.Sensitive = true;
				accelEntry.Text = (string) model.GetValue (iter, bindingCol);
				accelEntry.GrabFocus ();
				accelIncomplete = false;
				accelComplete = true;
			} else {
				accelEntry.Sensitive = false;
				accelEntry.Text = string.Empty;
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
			
			if (sel.GetSelected (out model, out iter) && model.GetValue (iter, commandCol) != null)
				keyStore.SetValue (iter, bindingCol, accelEntry.Text);
		}
		
		public bool ValidateChanges ()
		{
			return true;
		}
		
		public bool IsVisible ()
		{
			return true;
		}
		
		public void Initialize (OptionsDialog dialog, object dataObject)
		{
		}
	}
}
