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
		KeyBindingSet currentBindings;
		bool internalUpdate;
		List<KeyBindingScheme> schemes;
		
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
			accelEntry.Changed += delegate {
				UpdateWarningLabel ();
			};
			updateButton.Clicked += new EventHandler (OnUpdateButtonClick);

			currentBindings = KeyBindingService.CurrentKeyBindingSet.Clone ();

			schemes = new List<KeyBindingScheme> (KeyBindingService.Schemes);
			schemeCombo.AppendText (GettextCatalog.GetString ("Custom"));
			
			foreach (KeyBindingScheme s in schemes)
				schemeCombo.AppendText (s.Name);

			SelectCurrentScheme ();
			schemeCombo.Changed += OnKeyBindingSchemeChanged;
		}

		void SelectCurrentScheme ()
		{
			try {
				internalUpdate = true;
				for (int n=0; n<schemes.Count; n++) {
					KeyBindingScheme s = schemes [n];
					if (currentBindings.Equals (s.GetKeyBindingSet ())) {
						schemeCombo.Active = n + 1;
						return;
					}
				}
				schemeCombo.Active = 0;
			} finally {
				internalUpdate = false;
			}
		}
		
		public void ApplyChanges ()
		{
			KeyBindingService.ResetCurrent (currentBindings);
			IdeApp.CommandService.LoadUserBindings ();
			KeyBindingService.SaveCurrentBindings ();
		}

		public Gtk.Widget CreatePanelWidget ()
		{
			SortedDictionary<string, Command> commands = new SortedDictionary<string, Command> ();
			string translatedOther = GettextCatalog.GetString ("Other");
			
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
			}
			
			List<Command> sortedCommands = new List<Command> (commands.Values);
			sortedCommands.Sort (delegate (Command c1, Command c2) {
				string cat1 = c1.Category.Length == 0? translatedOther : c1.Category;
				string cat2 = c2.Category.Length == 0? translatedOther : c2.Category;
				int catCompare = cat1.CompareTo (cat2);
				if (catCompare != 0)
					return catCompare;
				string t1 = c1.Text.Replace ("_", String.Empty);
				string t2 = c2.Text.Replace ("_", String.Empty);
				return t1.CompareTo (t2);
			});
			
			string currentCat = null;
			TreeIter icat = TreeIter.Zero;
			foreach (Command cmd in sortedCommands) {
				if (currentCat != cmd.Category) {
					currentCat = cmd.Category;
					string name = currentCat.Length == 0? translatedOther : currentCat;
					icat = keyStore.AppendValues (null, name, String.Empty, String.Empty, (int) Pango.Weight.Bold, null, false);
				}
				string label = cmd.Text.Replace ("_", String.Empty);
				keyStore.AppendValues (icat, cmd, label, cmd.AccelKey != null ? cmd.AccelKey : String.Empty, cmd.Description, (int) Pango.Weight.Normal, cmd.Icon, true);
			}
			UpdateGlobalWarningLabel ();
			return this;
		}
		
		void OnKeyBindingSchemeChanged (object sender, EventArgs e)
		{
			if (internalUpdate)
				return;

			if (schemeCombo.Active == 0)
				return;
			
			Command command;
			string binding;
			TreeIter iter;
			
			if (!keyStore.GetIterFirst (out iter))
				return;
			
			// Load a key binding template
			KeyBindingScheme scheme = KeyBindingService.GetSchemeByName (schemeCombo.ActiveText);
			currentBindings = scheme.GetKeyBindingSet ().Clone ();
			
			do {
				TreeIter citer;
				keyStore.IterChildren (out citer, iter);
				do {
					command = (Command) keyStore.GetValue (citer, commandCol);
					binding = currentBindings.GetBinding (command);
					keyStore.SetValue (citer, bindingCol, binding);
				} while (keyStore.IterNext (ref citer));
			} while (keyStore.IterNext (ref iter));

			UpdateGlobalWarningLabel ();
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
			TreeIter iter;
			
			if (sel.GetSelected (out iter)) {
				Command cmd = (Command) keyStore.GetValue (iter, commandCol);
				if (cmd != null) {
					keyStore.SetValue (iter, bindingCol, accelEntry.Text);
					currentBindings.SetBinding (cmd, accelEntry.Text);
					SelectCurrentScheme ();
				}
			}
			UpdateGlobalWarningLabel ();
		}

		void UpdateGlobalWarningLabel ()
		{
			KeyBindingConflict[] conflicts = currentBindings.CheckKeyBindingConflicts (IdeApp.CommandService.GetCommands ());
			if (conflicts.Length == 0) {
				globalWarningBox.Hide ();
				return;
			}
			globalWarningBox.Show ();
			conflicButton.MenuCreator = delegate {
				Menu menu = new Menu ();
				foreach (KeyBindingConflict conf in conflicts) {
					if (menu.Children.Length > 0) {
						SeparatorMenuItem it = new SeparatorMenuItem ();
						it.Show ();
						menu.Insert (it, -1);
					}
					foreach (Command cmd in conf.Commands) {
						string txt = currentBindings.GetBinding (cmd) + " - " + cmd.Text;
						MenuItem item = new MenuItem (txt);
						Command localCmd = cmd;
						item.Activated += delegate {
							SelectCommand (localCmd);
						};
						item.Show ();
						menu.Insert (item, -1);
					}
				}
				return menu;
			};
		}

		void SelectCommand (Command cmd)
		{
			TreeIter iter;
			if (!keyStore.GetIterFirst (out iter))
				return;
			do {
				TreeIter citer;
				keyStore.IterChildren (out citer, iter);
				do {
					Command command = (Command) keyStore.GetValue (citer, commandCol);
					if (command == cmd) {
						TreePath path = keyStore.GetPath (citer);
						keyTreeView.ExpandToPath (path);
						keyTreeView.Selection.SelectPath (path);
						keyTreeView.ScrollToCell (path, keyTreeView.Columns[0], true, 0.5f, 0f);
						return;
					}
				} while (keyStore.IterNext (ref citer));
			} while (keyStore.IterNext (ref iter));
		}

		void UpdateWarningLabel ()
		{
			if (accelEntry.Text.Length == 0) {
				labelMessage.Visible = false;
				return;
			}

			Command cmd = null;
			TreeIter iter;
			if (keyTreeView.Selection.GetSelected (out iter))
				cmd = (Command) keyTreeView.Model.GetValue (iter, commandCol);
			
			if (cmd == null) {
				labelMessage.Visible = false;
				return;
			}
			
			var bindings = FindBindings (accelEntry.Text);
			bindings.Remove (cmd);
			
			if (bindings.Count > 0) {
				labelMessage.Markup = "<b>" + GettextCatalog.GetString ("This key combination is already bound to command '{0}'", bindings [0].Text.Replace ("_","")) + "</b>";
				labelMessage.Visible = true;
			}
			else
				labelMessage.Visible = false;
		}

		List<Command> FindBindings (string accel)
		{
			List<Command> bindings = new List<Command> ();
			TreeModel model = (TreeModel) keyStore;
			TreeIter iter;
			if (!model.GetIterFirst (out iter))
				return bindings;
			do {
				TreeIter citer;
				model.IterChildren (out citer, iter);
				do {
					string binding = (string) model.GetValue (citer, bindingCol);
					if (binding == accel) {
						Command command = (Command) model.GetValue (citer, commandCol);
						bindings.Add (command);
					}
				} while (model.IterNext (ref citer));
			} while (model.IterNext (ref iter));
			return bindings;
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
