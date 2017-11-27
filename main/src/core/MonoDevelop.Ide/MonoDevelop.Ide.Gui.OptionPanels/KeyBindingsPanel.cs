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
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

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
		string chord;
		KeyBindingSet currentBindings;
		bool internalUpdate;
		List<KeyBindingScheme> schemes;
		
		TreeModelFilter filterModel;
		bool filterChanged;
		string[] processedFilterTerms;
		bool filterTimeoutRunning;

		TreeViewColumn bindingTVCol;

		Dictionary<string, HashSet<Command>> duplicates;
		Dictionary<string, HashSet<Command>> conflicts;

		CellRendererKeyButtons bindingRenderer;
		
		public KeyBindingsPanel ()
		{
			this.Build ();
			
			keyStore = new TreeStore (typeof (Command), typeof (string), typeof (string), typeof (string), typeof (int), typeof(string), typeof(bool), typeof (bool));
			keyTreeView.Model = filterModel = new TreeModelFilter (keyStore, null);
			filterModel.VisibleColumn = visibleCol;
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Command");
			col.Spacing = 4;
			CellRendererImage crp = new CellRendererImage ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock-id", iconCol);
			col.AddAttribute (crp, "visible", iconVisibleCol);
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", labelCol);
			col.AddAttribute (crt, "weight", boldCol);
			keyTreeView.AppendColumn (col);
			
			bindingTVCol = new TreeViewColumn ();
			bindingTVCol.Title = GettextCatalog.GetString ("Key Binding");
			bindingRenderer = new CellRendererKeyButtons (this);
			bindingRenderer.KeyBindingSelected += BindingRenderer_KeyBindingSelected;
			bindingTVCol.PackStart (bindingRenderer, false);
			bindingTVCol.AddAttribute (bindingRenderer, "text", bindingCol);
			bindingTVCol.AddAttribute (bindingRenderer, "command", commandCol);
			keyTreeView.AppendColumn (bindingTVCol);
			
			keyTreeView.AppendColumn (GettextCatalog.GetString ("Description"), new CellRendererText (), "text", descCol);
			
			keyTreeView.Selection.Changed += OnKeysTreeViewSelectionChange;
			
			accelEntry.KeyPressEvent += OnAccelEntryKeyPress;
			accelEntry.KeyReleaseEvent += OnAccelEntryKeyRelease;
			accelEntry.Changed += delegate {
				UpdateWarningLabel ();
			};
			updateButton.Clicked += OnUpdateButtonClick;
			addButton.Clicked += OnAddRemoveButtonClick;

			currentBindings = KeyBindingService.CurrentKeyBindingSet.Clone ();

			schemes = new List<KeyBindingScheme> (KeyBindingService.Schemes);
			
			foreach (KeyBindingScheme s in schemes)
				schemeCombo.AppendText (s.Name);
			
			if (schemes.Count > 0) {
				schemeCombo.RowSeparatorFunc = (TreeModel model, TreeIter iter) => {
					if (model.GetValue (iter, 0) as string == "---")
						return true;
					return false;
				};
				schemeCombo.AppendText ("---");
			}
			schemeCombo.AppendText (GettextCatalog.GetString ("Custom"));

			SelectCurrentScheme ();
			schemeCombo.Changed += OnKeyBindingSchemeChanged;

			searchEntry.Ready = true;
			searchEntry.Visible = true;
			searchEntry.Changed += delegate {
				processedFilterTerms = searchEntry.Entry.Text.ToLower ().Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				filterChanged = true;
				if (!filterTimeoutRunning) {
					filterTimeoutRunning = true;
					GLib.Timeout.Add (50, delegate {
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

			keyTreeView.SearchColumn = -1; // disable the interactive search

			//HACK: workaround for MD Bug 608021: Stetic loses values assigned to "new" properties of custom widget
			conflicButton.Label = GettextCatalog.GetString ("_View Conflicts");
			conflicButton.UseUnderline = true;

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			schemeCombo.Accessible.Name = "KeyBindingsPanel.schemeCombo";
			schemeCombo.Accessible.Description = GettextCatalog.GetString ("Select a predefined keybindings scheme");
			schemeCombo.SetAccessibilityLabelRelationship (labelScheme);

			searchEntry.Entry.SetCommonAccessibilityAttributes ("KeyBindingsPanel.searchEntry", GettextCatalog.GetString ("Search"),
																GettextCatalog.GetString ("Enter a search term to find it in the keybindings list"));

			accelEntry.SetCommonAccessibilityAttributes ("KeyBindingsPanel.accelEntry", labelEditBinding,
			                                             GettextCatalog.GetString ("Enter the keybinding for this command"));

			addButton.SetCommonAccessibilityAttributes ("KeyBindingsPanel.addButton", "",
			                                            GettextCatalog.GetString ("Add a new binding for this command"));
			updateButton.SetCommonAccessibilityAttributes ("KeyBindingsPanel.updateButton", "",
			                                               GettextCatalog.GetString ("Update the binding for this command"));
		}

		void Refilter ()
		{
			keyTreeView.Model = null;
			TreeIter iter;
			bool allVisible = processedFilterTerms == null || processedFilterTerms.Length == 0;
			if (keyStore.GetIterFirst (out iter))
				Refilter (iter, allVisible);
			keyTreeView.Model = filterModel;
			keyTreeView.SearchColumn = -1; // disable the interactive search
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
						schemeCombo.Active = n;
						return;
					}
				}
				schemeCombo.Active = schemes.Count + 1;
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

		public Control CreatePanelWidget ()
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
				string t1 = c1.DisplayName;
				string t2 = c2.DisplayName;
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
				string label = cmd.DisplayName;
				string accels = cmd.AccelKey != null ? cmd.AccelKey : String.Empty;
				if (cmd.AlternateAccelKeys != null && cmd.AlternateAccelKeys.Length > 0)
					accels += " " + string.Join (" ", cmd.AlternateAccelKeys);
				keyStore.AppendValues (icat, cmd, label, accels, cmd.Description, (int) Pango.Weight.Normal, (string)cmd.Icon, true, true);
			}
			UpdateConflictsWarning ();
			Refilter ();
			return this;
		}
		
		void OnKeyBindingSchemeChanged (object sender, EventArgs e)
		{
			if (internalUpdate)
				return;

			if (schemeCombo.Active == schemes.Count + 1)
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
					binding = string.Join (" ", currentBindings.GetBindings (command));
					keyStore.SetValue (citer, bindingCol, binding);
				} while (keyStore.IterNext (ref citer));
			} while (keyStore.IterNext (ref iter));

			UpdateConflictsWarning ();
		}

		void BindingRenderer_KeyBindingSelected (object sender, KeyBindingSelectedEventArgs e)
		{
			accelComplete = false;

			accelEntry.Sensitive = true;
			CurrentSelectedBinding = e;
			//grab focus AFTER the event, or focus gets screwy
			GLib.Timeout.Add (10, delegate {
				accelEntry.GrabFocus ();
				return false;
			});
			accelIncomplete = false;
			accelComplete = true;
		}

		void OnKeysTreeViewSelectionChange (object sender, EventArgs e)
		{
			TreeSelection sel = sender as TreeSelection;
			TreeModel model;
			TreeIter iter;
			Command selCommand = null;
			if (sel.GetSelected (out model, out iter) && model.GetValue (iter, commandCol) != null) {
				selCommand = model.GetValue (iter, commandCol) as Command;
				if (CurrentSelectedBinding?.Command == selCommand) // command is already selected
					return;

				accelComplete = false;
				var binding = model.GetValue (iter, bindingCol) as string;
				iter = filterModel.ConvertIterToChildIter (iter);
				CurrentSelectedBinding = new KeyBindingSelectedEventArgs (binding.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries), 0, selCommand, iter);
				accelIncomplete = false;
				accelComplete = true;
				accelEntry.Sensitive = true;
			} else {
				accelEntry.Sensitive = updateButton.Sensitive = addButton.Sensitive = false;
				CurrentSelectedBinding = null;
			}
		}
		
		[GLib.ConnectBefore]
		void OnAccelEntryKeyPress (object sender, KeyPressEventArgs e)
		{
			Gdk.Key key = e.Event.Key;
			string accel;
			
			e.RetVal = true;
			
			if (accelComplete) {
				CurrentKey = String.Empty;
				accelIncomplete = false;
				accelComplete = false;
				chord = null;
				
				if (key == Gdk.Key.BackSpace)
					return;
			}
			
			accelComplete = false;
			bool combinationComplete;
			accel = KeyBindingManager.AccelLabelFromKey (e.Event, out combinationComplete);
			if (combinationComplete) {
				CurrentKey = KeyBindingManager.Binding (chord, accel);
				accelIncomplete = false;
				if (chord != null)
					accelComplete = true;
				else
					chord = accel;
			} else {
				accel = (chord != null ? chord + "|" : string.Empty) + accel;
				accelIncomplete = true;
				CurrentKey = accel;
			}
		}

		string currentKey;
		string CurrentKey {
			get {
				return currentKey ?? string.Empty;
			}
			set {
				currentKey = value;
				accelEntry.Text = value == null? "" : KeyBindingManager.BindingToDisplayLabel (value, false, true);
				UpdateButtons ();
				UpdateWarningLabel ();
			}
		}

		KeyBindingSelectedEventArgs currentSelectedBinding;
		KeyBindingSelectedEventArgs CurrentSelectedBinding {
			get { return currentSelectedBinding; }
			set {
				currentSelectedBinding = value;
				if (value == null) {
					accelEntry.Text = string.Empty;
					return;
				}

				CurrentKey = currentSelectedBinding.AllKeys.Count > 0 ? currentSelectedBinding.AllKeys [currentSelectedBinding.SelectedKey] : String.Empty;
			}
		}
		
		void OnAccelEntryKeyRelease (object sender, KeyReleaseEventArgs e)
		{
			if (accelIncomplete)
				CurrentKey = chord != null ? chord : string.Empty;
		}

		void UpdateButtons ()
		{
			if (CurrentSelectedBinding != null) {

				if (CurrentSelectedBinding.AllKeys.Count == 0 && string.IsNullOrEmpty (currentKey)) {
					updateButton.Sensitive = false;
					addButton.Sensitive = false;
				} else {
					if (CurrentSelectedBinding.AllKeys.Contains (currentKey)) {
						addButton.Sensitive = true;
						addButton.Label = GettextCatalog.GetString ("Delete");
					} else {
						addButton.Sensitive = true;
						addButton.Label = GettextCatalog.GetString ("Add");
					}
					updateButton.Sensitive = !CurrentSelectedBinding.AllKeys.Contains (currentKey);
				}
			} else {
				updateButton.Sensitive = addButton.Sensitive = false;
			}
		}
		
		void OnUpdateButtonClick (object sender, EventArgs e)
		{
			if (CurrentSelectedBinding != null) {
				if (string.IsNullOrEmpty (CurrentKey))
					CurrentSelectedBinding.AllKeys.RemoveAt (CurrentSelectedBinding.SelectedKey);
				else if (CurrentSelectedBinding.AllKeys.Count == 0)
					CurrentSelectedBinding.AllKeys.Add (CurrentKey);
				else
					CurrentSelectedBinding.AllKeys [CurrentSelectedBinding.SelectedKey] = CurrentKey;
				var binding = string.Join (" ", CurrentSelectedBinding.AllKeys);
				keyStore.SetValue (currentSelectedBinding.Iter, bindingCol, binding);
				currentBindings.SetBinding (currentSelectedBinding.Command, CurrentSelectedBinding.AllKeys.ToArray ());
				SelectCurrentScheme ();
				keyTreeView.QueueDraw ();
				UpdateButtons ();
			}
			UpdateConflictsWarning ();
		}

		void OnAddRemoveButtonClick (object sender, EventArgs e)
		{
			if (CurrentSelectedBinding != null) {
				if (string.IsNullOrEmpty (CurrentKey) || CurrentSelectedBinding.AllKeys.Contains (CurrentKey))
					CurrentSelectedBinding.AllKeys.RemoveAt (CurrentSelectedBinding.SelectedKey);
				else
					CurrentSelectedBinding.AllKeys.Add (CurrentKey);

				var binding = string.Join (" ", CurrentSelectedBinding.AllKeys);
				keyStore.SetValue (currentSelectedBinding.Iter, bindingCol, binding);
				currentBindings.SetBinding (currentSelectedBinding.Command, CurrentSelectedBinding.AllKeys.ToArray ());
				SelectCurrentScheme ();
				keyTreeView.QueueDraw ();
				UpdateButtons ();
			}
			UpdateConflictsWarning ();
		}

		void UpdateConflictsWarning ()
		{
			duplicates = new Dictionary<string, HashSet<Command>> ();
			foreach (var conflict in currentBindings.CheckKeyBindingConflicts (IdeApp.CommandService.GetCommands ())) {
				HashSet<Command> cmds = null;
				if (!duplicates.TryGetValue (conflict.Key, out cmds))
					duplicates [conflict.Key] = cmds = new HashSet<Command> ();
				foreach (var cmd in conflict.Commands)
					cmds.Add (cmd);
			}
			conflicts = new Dictionary<string, HashSet<Command>> ();

			foreach (var dup in duplicates) {
				foreach (var cmd in dup.Value) {
					HashSet<Command> cmdDuplicates;
					if (IdeApp.CommandService.Conflicts.TryGetValue (cmd, out cmdDuplicates)) {
						cmdDuplicates = new HashSet<Command> (cmdDuplicates.Intersect (dup.Value));
						if (cmdDuplicates.Count > 0) {
							HashSet<Command> cmdConflicts;
							if (!conflicts.TryGetValue (dup.Key, out cmdConflicts))
								conflicts [dup.Key] = cmdConflicts = new HashSet<Command> ();
							conflicts [dup.Key].UnionWith (cmdDuplicates);
						}
					}
				}
			}

			if (conflicts.Count == 0) {
				globalWarningBox.Hide ();
				return;
			}

			globalWarningBox.Show ();

			conflicButton.ContextMenuRequested = delegate {
				ContextMenu menu = new ContextMenu ();
				bool first = true;

				foreach (var conf in conflicts) {
					if (first == false) {
						ContextMenuItem item = new SeparatorContextMenuItem ();
						menu.Items.Add (item);
					}

					foreach (Command cmd in conf.Value.OrderBy (cmd => cmd.DisplayName)) {
						string txt = conf.Key + " \u2013 " + cmd.DisplayName;
						ContextMenuItem item = new ContextMenuItem (txt);
						Command localCmd = cmd;

						item.Clicked += (sender, e) => SelectCommand (localCmd);

						menu.Items.Add (item);
						first = false;
					}
				}

				return menu;
			};
		}

		void SelectCommand (Command cmd)
		{
			//item may not be visible if the list is filtered
			searchEntry.Entry.Text = "";
			
			TreeIter iter;
			if (!keyStore.GetIterFirst (out iter))
				return;
			do {
				TreeIter citer;
				keyStore.IterChildren (out citer, iter);
				do {
					Command command = (Command) keyStore.GetValue (citer, commandCol);
					if (command == cmd) {
						TreePath path = filterModel.ConvertChildPathToPath (keyStore.GetPath (citer));
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
			if (CurrentKey.Length == 0 || CurrentSelectedBinding?.Command == null) {
				labelMessage.Visible = false;
				return;
			}

			var bindings = FindBindings (CurrentKey);
			bindings.Remove (CurrentSelectedBinding.Command);
			
			if (bindings.Count > 0) {
				HashSet<Command> cmdConflicts = null;
				if (IdeApp.CommandService.Conflicts.TryGetValue (CurrentSelectedBinding.Command, out cmdConflicts)) {
					foreach (var confl in cmdConflicts) {
						if (bindings.Contains (confl)) {
							var conflName = "<span foreground='" + Styles.ErrorForegroundColor.ToHexString (false) + "'>" + confl.DisplayName + "</span>";
							labelMessage.Markup = "<b>" + GettextCatalog.GetString ("This key combination is already bound to command '{0}' in the same context", conflName) + "</b>";
							labelMessage.Visible = true;
							return;
						}
					}
				}
				var cmdname = "<span foreground='" + Styles.WarningForegroundColor.ToHexString (false) + "'>" + bindings [0].DisplayName + "</span>";
				labelMessage.Markup = "<b>" + GettextCatalog.GetString ("This key combination is already bound to command '{0}'", cmdname) + "</b>";
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

			int i = -1;
			// If it ends with | then we're matching something like Cmd-|
			// and it's not being used as an 'or'.
			if (!b1.EndsWith ("|"))
				i = b1.IndexOf ('|');
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

		class KeyBindingSelectedEventArgs : EventArgs
		{
			public int SelectedKey { get; private set; }
			public List<string> AllKeys { get; private set; }
			public Command Command { get; private set; }
			public TreeIter Iter { get; private set; }

			public KeyBindingSelectedEventArgs (IEnumerable<string> keys, int selectedKey, Command command, TreeIter iter)
			{
				if (command == null)
					throw new ArgumentNullException (nameof (command));
				AllKeys = new List<string> (keys);
				if (selectedKey < 0 || ((selectedKey != 0 && AllKeys.Count != 0) && selectedKey >= AllKeys.Count))
					throw new ArgumentOutOfRangeException (nameof (selectedKey));
				SelectedKey =  selectedKey;
				Command = command;
				Iter = iter;
			}
		}

		protected override void OnDestroyed()
		{
			if (bindingRenderer != null) {
				bindingRenderer.KeyBindingSelected -= BindingRenderer_KeyBindingSelected;
				bindingRenderer = null;
			}
			base.OnDestroyed();
		}

		struct KeyBindingHitTestResult
		{
			public int SelectedKey { get; set; }
			public List<string> AllKeys { get; set; }
			public Command Command { get; set; }
			public TreeIter Iter { get; set; }
			public Gdk.Rectangle ButtonBounds { get; set; }
		}

		class CellRendererKeyButtons : CellRendererText
		{
			static Pango.FontDescription KeySymbolFont = Styles.DefaultFont.Copy ();
			
			const int KeyVPadding = 1;
			const int KeyHPadding = 6;
			const int KeyBgRadius = 3;
			const int Spacing = 6;
			KeyBindingsPanel keyBindingsPanel;
			TreeView keyBindingsTree;

			TooltipPopoverWindow tooltipWindow;

			[GLib.Property ("command")]
			public Command Command { get; set; }

			public event EventHandler<KeyBindingSelectedEventArgs> KeyBindingSelected;

			static CellRendererKeyButtons ()
			{
				// only a couple of OSX fonts support the home/end keys
				// and only Lucida Grande with an appropriate symbol size
				if (Platform.IsMac)
					KeySymbolFont.Family = "Lucida Grande";
				KeySymbolFont.Size -= (int) Pango.Scale.PangoScale * 2;
			}

			public CellRendererKeyButtons (KeyBindingsPanel panel)
			{
				keyBindingsPanel = panel;
				keyBindingsTree = panel.keyTreeView;
				keyBindingsTree.ButtonPressEvent += HandleKeyTreeButtonPressEvent;
				keyBindingsTree.MotionNotifyEvent += HandleKeyTreeMotionNotifyEvent;
				keyBindingsTree.ScrollEvent += HandleKeyTreeScrollEvent;
				keyBindingsTree.LeaveNotifyEvent += HandleKeyTreeLeaveNotifyEvent;
				keyBindingsTree.Unrealized += HandleKeyTreeUnrealized;
			}

			void HideConflictTooltip ()
			{
				if (tooltipWindow != null) {
					tooltipWindow.Destroy ();
					tooltipWindow = null;
				}
			}

			void HandleKeyTreeLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
			{
				HideConflictTooltip ();
			}

			void HandleKeyTreeUnrealized (object sender, EventArgs e)
			{
				HideConflictTooltip ();
			}

			void HandleKeyTreeScrollEvent (object o, ScrollEventArgs args)
			{
				HandleKeyTreeMotion (args.Event.X, args.Event.Y);
			}

			[GLib.ConnectBefore ()]
			void HandleKeyTreeMotionNotifyEvent (object o, MotionNotifyEventArgs args)
			{
				HandleKeyTreeMotion (args.Event.X, args.Event.Y);
			}

			void HandleKeyTreeMotion (double mouseX, double mouseY)
			{
				if (keyBindingsPanel.duplicates?.Count <= 0)
					return;

				var hit = HitTest (mouseX, mouseY);
				if (hit.ButtonBounds.IsEmpty) {
					HideConflictTooltip ();
					return;
				}

				if (hit.AllKeys.Count == 0)
					return;

				HashSet<Command> keyDuplicates = null;
				if (keyBindingsPanel.duplicates.TryGetValue (hit.AllKeys [hit.SelectedKey], out keyDuplicates)) {

					var cmdDuplicates = keyDuplicates.Where (cmd => cmd != hit.Command);
					if (tooltipWindow == null) {
						tooltipWindow = TooltipPopoverWindow.Create ();
						tooltipWindow.ShowArrow = true;
						//tooltipWindow.LeaveNotifyEvent += delegate { HideConflictTooltip (); };
					}

					var text = string.Empty;
					HashSet<Command> cmdConflicts = null;
					bool hasConflict = false;
					if (keyBindingsPanel.conflicts != null && keyBindingsPanel.conflicts.TryGetValue (hit.AllKeys [hit.SelectedKey], out cmdConflicts))
						hasConflict = cmdConflicts.Contains (hit.Command);

					if (hasConflict) {
						var acmdConflicts = cmdConflicts.Where (cmd => cmd != hit.Command).ToArray ();
						text += GettextCatalog.GetPluralString (
							"This shortcut is assigned to another command that is available\nin the same context. Please set a different shortcut.",
							"This shortcut is assigned to other commands that are available\nin the same context. Please set a different shortcut.",
							acmdConflicts.Length) + "\n\n";
						text += GettextCatalog.GetString ("Conflicts:");
						foreach (var conflict in acmdConflicts)
							text += "\n\u2022 " + conflict.Category + " \u2013 " + conflict.DisplayName;
						cmdDuplicates = cmdDuplicates.Except (acmdConflicts);
					}
					if (cmdDuplicates.Count () > 0) {
						if (hasConflict)
							text += "\n\n";
						text += GettextCatalog.GetString ("Duplicates:");

						foreach (var cmd in cmdDuplicates)
							text += "\n\u2022 " + cmd.Category + " \u2013 " + cmd.DisplayName;
					}

					tooltipWindow.Markup = text;
					tooltipWindow.Severity = hasConflict ? Tasks.TaskSeverity.Error : Tasks.TaskSeverity.Warning;

					tooltipWindow.ShowPopup (keyBindingsTree, hit.ButtonBounds, PopupPosition.Top);
				} else
					HideConflictTooltip ();
			}

			[GLib.ConnectBefore ()]
			void HandleKeyTreeButtonPressEvent (object o, ButtonPressEventArgs args)
			{
				if (KeyBindingSelected == null)
					return;
				var hit = HitTest (args.Event.X, args.Event.Y);
				if (hit.Command == null)
					return;
				var a = new KeyBindingSelectedEventArgs (hit.AllKeys, hit.SelectedKey, hit.Command, hit.Iter);
				KeyBindingSelected (this, a);
			}

			KeyBindingHitTestResult HitTest (double mouseX, double mouseY)
			{
				KeyBindingHitTestResult result = new KeyBindingHitTestResult ();
				TreeIter iter;
				TreePath path;
				int cellx, celly, mx, my;
				mx = (int)mouseX;
				my = (int)mouseY;

				if (!GetCellPosition (mx, my, out cellx, out celly, out iter, out path))
					return result;

				Text = keyBindingsTree.Model.GetValue (iter, bindingCol) as string ?? string.Empty;
				Command = keyBindingsTree.Model.GetValue (iter, commandCol) as Command;

				var filter = keyBindingsTree.Model as TreeModelFilter;
				if (filter != null)
					iter = filter.ConvertIterToChildIter (iter);

				result.Command = Command;
				result.AllKeys = Text.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList ();
				result.Iter = iter;

				using (var layout = new Pango.Layout (keyBindingsTree.PangoContext)) {

					// GetCellArea reports the outer cell bounds, therefore we need to add 2px
					var xpad = (int)Xpad + 2;
					var cellBounds = keyBindingsTree.GetCellArea (path, keyBindingsPanel.bindingTVCol);
					keyBindingsTree.ConvertBinWindowToWidgetCoords (cellBounds.X, cellBounds.Y, out cellBounds.X, out cellBounds.Y);
					int i = 0;
					foreach (var key in result.AllKeys) {
						layout.SetText (KeyBindingManager.BindingToDisplayLabel (key, false));
						layout.FontDescription = KeySymbolFont;
						int w, h;
						layout.GetPixelSize (out w, out h);

						int buttonWidth = w + (2 * KeyHPadding);
						int buttonHeight = h + (2 * KeyVPadding);
						var ypad = 2 + ((cellBounds.Height / 2) - (buttonHeight / 2));

						if (cellx > xpad && cellx <= xpad + buttonWidth &&
						    celly > ypad && celly <= ypad + buttonHeight) {
							keyBindingsPanel.bindingTVCol.CellGetPosition (this, out cellx, out w);
							cellBounds.X += cellx;

							result.SelectedKey = i;
							result.ButtonBounds = new Gdk.Rectangle (cellBounds.X + xpad, cellBounds.Y + ypad, buttonWidth, buttonHeight);
							result.ButtonBounds.Inflate (0, 2);
							return result;
						}

						xpad += buttonWidth + Spacing;
						i++;
					}
				}
				return result;
			}

			bool GetCellPosition (int mx, int my, out int cellx, out int celly, out TreeIter iter, out TreePath path)
			{
				TreeViewColumn col;
				iter = TreeIter.Zero;

				if (!keyBindingsTree.GetPathAtPos (mx, my, out path, out col, out cellx, out celly))
					return false;

				if (!keyBindingsTree.Model.GetIterFromString (out iter, path.ToString ()))
					return false;

				int sp, w;
				if (col.CellGetPosition (this, out sp, out w)) {
					if (cellx >= sp && cellx < sp + w)
						return true;
				}
				return false;
			}

			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				if (string.IsNullOrEmpty (Text))
					return;

				using (var cr = Gdk.CairoHelper.Create (window)) {
					using (var layout = new Pango.Layout (widget.PangoContext)) {
						var xpad = (int)Xpad;
						int w, h;
						Cairo.Color bgColor, fgColor;
						foreach (var key in Text.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {

							HashSet<Command> bindingConflicts;
							if (keyBindingsPanel.conflicts.TryGetValue (key, out bindingConflicts) && bindingConflicts.Contains (Command)) {
								bgColor = Styles.KeyBindingsPanel.KeyConflictBackgroundColor.ToCairoColor ();
								fgColor = Styles.KeyBindingsPanel.KeyConflictForegroundColor.ToCairoColor ();
							} else if (keyBindingsPanel.duplicates.ContainsKey (key)) {
								bgColor = Styles.KeyBindingsPanel.KeyDuplicateBackgroundColor.ToCairoColor ();
								fgColor = Styles.KeyBindingsPanel.KeyDuplicateForegroundColor.ToCairoColor ();
							} else {
								bgColor = Styles.KeyBindingsPanel.KeyBackgroundColor.ToCairoColor ();
								fgColor = Styles.KeyBindingsPanel.KeyForegroundColor.ToCairoColor ();
							}

							layout.SetText (KeyBindingManager.BindingToDisplayLabel (key, false));
							layout.FontDescription = KeySymbolFont;
							layout.GetPixelSize (out w, out h);

							int buttonWidth = w + (2 * KeyHPadding);
							int buttonHeight = h + (2 * KeyVPadding);
							int x = cell_area.X + xpad;
							double y = cell_area.Y + ((cell_area.Height / 2) - (buttonHeight / 2));

							cr.RoundedRectangle (x, y, buttonWidth, buttonHeight, KeyBgRadius);
							cr.LineWidth = 1;
							cr.SetSourceColor (bgColor);
							cr.FillPreserve ();
							cr.SetSourceColor (bgColor);
							cr.Stroke ();

							cr.SetSourceColor (fgColor);
							cr.MoveTo (x + KeyHPadding, y + KeyVPadding);
							cr.ShowLayout (layout);
							xpad += buttonWidth + Spacing;
						}
					}
				}
			}

			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
				x_offset = y_offset = 0;
				if (string.IsNullOrEmpty (Text)) {
					width = 0;
					height = 0;
					return;
				}

				using (var layout = new Pango.Layout (widget.PangoContext)) {
					height = 0;
					width = (int)Xpad;
					int w, h, buttonWidth;
					foreach (var key in Text.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
						layout.SetText (KeyBindingManager.BindingToDisplayLabel (key, false));
						layout.FontDescription = KeySymbolFont;
						layout.GetPixelSize (out w, out h);
						if (height == 0)
							height = h + (KeyVPadding * 2) + 1;
						
						buttonWidth = w + (2 * KeyHPadding);
						width += buttonWidth + Spacing;
					}
				}
			}

			protected override void OnDestroyed()
			{
				keyBindingsPanel = null;
				HideConflictTooltip ();
				base.OnDestroyed();
			}
		}
	}
}
