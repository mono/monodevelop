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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using Gtk;
using MonoDevelop.Ide.Gui.Components;

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
		static readonly int visibleCol = 7;
		
		bool accelIncomplete = false;
		bool accelComplete = false;
		TreeStore keyStore;
		string mode;
		KeyBindingSet currentBindings;
		bool internalUpdate;
		List<KeyBindingScheme> schemes;
		
		TreeModelFilter filterModel;
		bool filterChanged;
		string[] processedFilterTerms;
		bool filterTimeoutRunning;
		
		public KeyBindingsPanel ()
		{
			this.Build ();
			
			keyStore = new TreeStore (typeof (Command), typeof (string), typeof (string), typeof (string), typeof (int), typeof(string), typeof(bool), typeof (bool));
			keyTreeView.Model = filterModel = new TreeModelFilter (keyStore, null);
			filterModel.VisibleColumn = visibleCol;
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Command");
			col.Spacing = 4;
			CellRendererIcon crp = new CellRendererIcon ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock-id", iconCol);
			col.AddAttribute (crp, "visible", iconVisibleCol);
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", labelCol);
			col.AddAttribute (crt, "weight", boldCol);
			keyTreeView.AppendColumn (col);
			
			TreeViewColumn bindingTVCol = new TreeViewColumn ();
			bindingTVCol.Title = GettextCatalog.GetString ("Key Binding");
			CellRendererText bindingRenderer = new CellRendererText ();
			bindingTVCol.PackStart (bindingRenderer, false);
			bindingTVCol.SetCellDataFunc (bindingRenderer, delegate (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) {
				string binding = (model.GetValue (iter, bindingCol) as string) ?? "";
				((CellRendererText)cell).Text = binding.Length > 0
					? KeyBindingManager.BindingToDisplayLabel (binding, false)
					: binding;
			});
			keyTreeView.AppendColumn (bindingTVCol);
			
			keyTreeView.AppendColumn (GettextCatalog.GetString ("Description"), new CellRendererText (), "text", descCol);
			
			keyTreeView.Selection.Changed += OnKeysTreeViewSelectionChange;
			
			accelEntry.KeyPressEvent += OnAccelEntryKeyPress;
			accelEntry.KeyReleaseEvent += OnAccelEntryKeyRelease;
			accelEntry.Changed += delegate {
				UpdateWarningLabel ();
			};
			updateButton.Clicked += OnUpdateButtonClick;

			currentBindings = KeyBindingService.CurrentKeyBindingSet.Clone ();

			schemes = new List<KeyBindingScheme> (KeyBindingService.Schemes);
			schemeCombo.AppendText (GettextCatalog.GetString ("Custom"));
			
			foreach (KeyBindingScheme s in schemes)
				schemeCombo.AppendText (s.Name);

			SelectCurrentScheme ();
			schemeCombo.Changed += OnKeyBindingSchemeChanged;
			
			searchEntry.Changed += delegate {
				processedFilterTerms = searchEntry.Text.Split (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
					.Select (s => s.ToLower ()).ToArray ();;
				filterChanged = true;
				if (!filterTimeoutRunning) {
					filterTimeoutRunning = true;
					GLib.Timeout.Add (300, delegate {
						if (!filterChanged) {
							if (filterTimeoutRunning)
								Refilter ();
							filterTimeoutRunning = false;
							return false;
						}
						filterChanged = false;
						return true;
					});
				};
			};
			
			clearFilterButton.Clicked += ClearFilter;
			
			//HACK: workaround for MD Bug 608021: Stetic loses values assigned to "new" properties of custom widget
			conflicButton.Label = GettextCatalog.GetString ("_View Conflicts");
			conflicButton.UseUnderline = true;
		}

		void ClearFilter (object sender, EventArgs e)
		{
			searchEntry.Text = "";
			Refilter ();
			//stop the timeout from refiltering, if it's already running
			filterTimeoutRunning = false;
		}
		
		void Refilter ()
		{
			keyTreeView.Model = null;
			TreeIter iter;
			bool allVisible = processedFilterTerms == null || processedFilterTerms.Length == 0;
			if (keyStore.GetIterFirst (out iter))
				Refilter (iter, allVisible);
			keyTreeView.Model = filterModel;
			keyTreeView.ExpandAll ();
			keyTreeView.ColumnsAutosize ();
		}
		
		bool Refilter (TreeIter iter, bool allVisible)
		{
			int visibleCount = 0;
			
			do {
				TreeIter child;
				if (keyStore.IterChildren (out child, iter)) {
					bool catAllVisible = allVisible || IsSearchMatch ((string) keyStore.GetValue (iter, labelCol));
					bool childVisible = Refilter (child, catAllVisible);
					keyStore.SetValue (iter, visibleCol, childVisible);
					if (childVisible)
						visibleCount++;
				} else {
					bool visible = allVisible
						|| IsSearchMatch ((string) keyStore.GetValue (iter, labelCol))
						|| IsSearchMatch ((string) keyStore.GetValue (iter, descCol))
						|| IsSearchMatch ((string) keyStore.GetValue (iter, bindingCol));
					keyStore.SetValue (iter, visibleCol, visible);
					if (visible)
						visibleCount++;
				}
			} while (keyStore.IterNext (ref iter));
			
			return visibleCount > 0;
		}
		
		bool IsSearchMatch (string cmp)
		{
			if (cmp == null)
				return false;
			
			var lower = cmp.ToLower ();
			foreach (var term in processedFilterTerms)
				if (!lower.Contains (term))
					return false;
			return true;
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
				if (cmd == null || cmd.CommandArray || cmd.Category == GettextCatalog.GetString ("Hidden"))
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
					icat = keyStore.AppendValues (null, name, String.Empty, String.Empty, (int) Pango.Weight.Bold, null, false, true);
				}
				string label = cmd.Text.Replace ("_", String.Empty);
				keyStore.AppendValues (icat, cmd, label, cmd.AccelKey != null ? cmd.AccelKey : String.Empty, cmd.Description, (int) Pango.Weight.Normal, (string)cmd.Icon, true, true);
			}
			UpdateGlobalWarningLabel ();
			Refilter ();
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
				CurrentBinding = (string) model.GetValue (iter, bindingCol);
				//grab focus AFTER the selection event, or focus gets screwy
				GLib.Timeout.Add (10, delegate {
					accelEntry.GrabFocus ();
					return false;
				});
				accelIncomplete = false;
				accelComplete = true;
			} else {
				accelEntry.Sensitive = false;
				CurrentBinding = string.Empty;
			}
		}
		
		[GLib.ConnectBefore]
		void OnAccelEntryKeyPress (object sender, KeyPressEventArgs e)
		{
			Gdk.Key key = e.Event.Key;
			string accel;
			
			e.RetVal = true;
			
			if (accelComplete) {
				CurrentBinding = String.Empty;
				accelIncomplete = false;
				accelComplete = false;
				mode = null;
				
				if (key.Equals (Gdk.Key.BackSpace))
					return;
			}
			
			accelComplete = false;
			bool combinationComplete;
			accel = KeyBindingManager.AccelFromKey (e.Event, out combinationComplete);
			if (combinationComplete) {
				CurrentBinding = KeyBindingManager.Binding (mode, accel);
				accelIncomplete = false;
				if (mode != null)
					accelComplete = true;
				else
					mode = accel;
			} else {
				accel = (mode != null ? mode + "|" : String.Empty) + accel;
				accelIncomplete = true;
				CurrentBinding = accel;
			}
		}
		
		string _realBinding;
		string CurrentBinding {
			get {
				return _realBinding;
			}
			set {
				_realBinding = value;
				accelEntry.Text = _realBinding == null? "" : KeyBindingManager.BindingToDisplayLabel (_realBinding, false, true);
			}
		}
		
		void OnAccelEntryKeyRelease (object sender, KeyReleaseEventArgs e)
		{
			if (accelIncomplete)
				CurrentBinding = mode != null ? mode : String.Empty;
		}
		
		void OnUpdateButtonClick (object sender, EventArgs e)
		{
			TreeIter iter;
			Command cmd;
			if (GetSelectedCommandIter (out iter, out cmd)) {
				keyStore.SetValue (iter, bindingCol, CurrentBinding);
				currentBindings.SetBinding (cmd, CurrentBinding);
				SelectCurrentScheme ();
			}
			UpdateGlobalWarningLabel ();
		}
		
		bool GetSelectedCommandIter (out TreeIter iter, out Command cmd)
		{
			TreeSelection sel = keyTreeView.Selection;
			if (!sel.GetSelected (out iter)) {
				cmd = null;
				return false;
			}
			
			cmd = (Command)filterModel.GetValue (iter, commandCol);
			if (cmd == null)
				return false;
			
			if (keyStore.GetIterFirst (out iter) && FindIterForCommand (cmd, iter, out iter))
				return true;
			
			throw new Exception ("Did not find command in underlying model");
		}
		
		bool FindIterForCommand (object cmd, TreeIter iter, out TreeIter found)
		{
			do {
				TreeIter child;
				if (keyStore.IterChildren (out child, iter) && FindIterForCommand (cmd, child, out found))
					return true;
				if (keyStore.GetValue (iter, commandCol) == cmd) {
					found = iter;
					return true;
				}
			} while (keyStore.IterNext (ref iter));
			found = TreeIter.Zero;
			return false;
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
			//item may not be visible if the list is filtered
			ClearFilter (null, EventArgs.Empty);
			
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
			if (CurrentBinding.Length == 0) {
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
			
			var bindings = FindBindings (CurrentBinding);
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
					if (Conflicts (binding, accel)) {
						Command command = (Command) model.GetValue (citer, commandCol);
						bindings.Add (command);
					}
				} while (model.IterNext (ref citer));
			} while (model.IterNext (ref iter));
			return bindings;
		}
		
		bool Conflicts (string b1, string b2)
		{
			// Control+X conflicts with Control+X, Control+X|Y
			// Control+X|Y conflicts with Control+X|Y, Control+X
			if (b1 == b2)
				return true;
			int i = b1.IndexOf ('|');
			if (i == -1)
				return b2.StartsWith (b1 + "|");
			else
				return b1.Substring (0, i) == b2;
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
