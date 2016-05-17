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
			CellRendererKeyButtons bindingRenderer = new CellRendererKeyButtons (this);
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
			addButton.Clicked += OnAddButtonClick;

			currentBindings = KeyBindingService.CurrentKeyBindingSet.Clone ();

			schemes = new List<KeyBindingScheme> (KeyBindingService.Schemes);
			schemeCombo.AppendText (GettextCatalog.GetString ("Custom"));
			
			foreach (KeyBindingScheme s in schemes)
				schemeCombo.AppendText (s.Name);

			SelectCurrentScheme ();
			schemeCombo.Changed += OnKeyBindingSchemeChanged;

			searchEntry.Ready = true;
			searchEntry.Visible = true;
			searchEntry.Changed += delegate {
				processedFilterTerms = searchEntry.Entry.Text.Split (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
					.Select (s => s.ToLower ()).ToArray ();;
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
			
			//HACK: workaround for MD Bug 608021: Stetic loses values assigned to "new" properties of custom widget
			conflicButton.Label = GettextCatalog.GetString ("_View Conflicts");
			conflicButton.UseUnderline = true;
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
				
				if (key.Equals (Gdk.Key.BackSpace))
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

				if (CurrentSelectedBinding != null) {
					if (string.IsNullOrEmpty (value))
						updateButton.Label = GettextCatalog.GetString ("Delete");
					else
						updateButton.Label = GettextCatalog.GetString ("Apply");

					if (CurrentSelectedBinding.AllKeys.Count == 0) {
						updateButton.Sensitive = false;
						addButton.Sensitive = true;
					} else
						updateButton.Sensitive = addButton.Sensitive = !CurrentSelectedBinding.AllKeys.Contains (value);
				} else {
					updateButton.Sensitive = addButton.Sensitive = false;
				}
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
		
		void OnUpdateButtonClick (object sender, EventArgs e)
		{
			if (CurrentSelectedBinding != null) {
				if (string.IsNullOrEmpty (CurrentKey))
					CurrentSelectedBinding.AllKeys.RemoveAt (CurrentSelectedBinding.SelectedKey);
				else
					CurrentSelectedBinding.AllKeys [CurrentSelectedBinding.SelectedKey] = CurrentKey;
				var binding = string.Join (" ", CurrentSelectedBinding.AllKeys);
				keyStore.SetValue (currentSelectedBinding.Iter, bindingCol, binding);
				currentBindings.SetBinding (currentSelectedBinding.Command, CurrentSelectedBinding.AllKeys.ToArray ());
				SelectCurrentScheme ();
				keyTreeView.QueueDraw ();
			}
			UpdateConflictsWarning ();
		}

		void OnAddButtonClick (object sender, EventArgs e)
		{
			if (CurrentSelectedBinding != null && !string.IsNullOrEmpty (CurrentKey)) {
				CurrentSelectedBinding.AllKeys.Add (CurrentKey);

				var binding = string.Join (" ", CurrentSelectedBinding.AllKeys);

				keyStore.SetValue (currentSelectedBinding.Iter, bindingCol, binding);
				currentBindings.SetBinding (currentSelectedBinding.Command, CurrentSelectedBinding.AllKeys.ToArray ());
				SelectCurrentScheme ();
				keyTreeView.QueueDraw ();
			}
			UpdateConflictsWarning ();
		}

		void UpdateConflictsWarning ()
		{
			duplicates = currentBindings.CheckKeyBindingConflicts (IdeApp.CommandService.GetCommands ())
			                            .ToDictionary (dup => dup.Key, dup => new HashSet<Command> (dup.Commands));
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
			if (CurrentKey.Length == 0) {
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
			
			var bindings = FindBindings (CurrentKey);
			bindings.Remove (cmd);
			
			if (bindings.Count > 0) {
				labelMessage.Markup = "<b>" + GettextCatalog.GetString ("This key combination is already bound to command '{0}'", bindings [0].DisplayName) + "</b>";
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

		protected override void OnDestroyed ()
		{
			keyStore.Dispose ();
			filterModel.Dispose ();
			base.OnDestroyed ();
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
			const int KeyVPadding = 0;
			const int KeyHPadding = 6;
			const int KeyBgRadius = 4;
			const int Spacing = 6;
			KeyBindingsPanel keyBindingsPanel;
			TreeView keyBindingsTree;

			TooltipPopoverWindow tooltipWindow;

			[GLib.Property ("command")]
			public Command Command { get; set; }

			public event EventHandler<KeyBindingSelectedEventArgs> KeyBindingSelected;

			public CellRendererKeyButtons (KeyBindingsPanel panel)
			{
				keyBindingsPanel = panel;
				keyBindingsTree = panel.keyTreeView;
				keyBindingsTree.ButtonPressEvent += HandleKeyTreeButtonPressEvent;
				keyBindingsTree.MotionNotifyEvent += HandleKeyTreeMotionNotifyEvent;
				Ypad = 0;
			}

			void HideConflictTooltip ()
			{
				if (tooltipWindow != null) {
					tooltipWindow.Destroy ();
					tooltipWindow = null;
				}
			}

			[GLib.ConnectBefore ()]
			void HandleKeyTreeMotionNotifyEvent (object o, MotionNotifyEventArgs args)
			{
				if (keyBindingsPanel.duplicates?.Count <= 0)
					return;

				var hit = HitTest (args.Event.X, args.Event.Y);
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
						tooltipWindow = new TooltipPopoverWindow ();
						tooltipWindow.ShowArrow = true;
						tooltipWindow.LeaveNotifyEvent += delegate { HideConflictTooltip (); };
					}

					var text = string.Empty;
					HashSet<Command> cmdConflicts = null;
					bool hasConflict = false;
					if (keyBindingsPanel.conflicts != null && keyBindingsPanel.conflicts.TryGetValue (hit.AllKeys [hit.SelectedKey], out cmdConflicts))
						hasConflict = cmdConflicts.Contains (hit.Command);

					if (hasConflict) {
						text += GettextCatalog.GetString ("Conflicts:");
						foreach (var conflict in cmdConflicts.Where (cmd => cmd != hit.Command))
							text += "\n\u2022 " + conflict.Category + " \u2013 " + conflict.DisplayName;
						cmdDuplicates = cmdDuplicates.Except (cmdConflicts);
					}
					if (cmdDuplicates.Count () > 0) {
						if (hasConflict)
							text += "\n\n";
						text += GettextCatalog.GetString ("Duplicates:");

						foreach (var cmd in cmdDuplicates)
							text += "\n\u2022 " + cmd.Category + " \u2013 " + cmd.DisplayName;
					}

					tooltipWindow.Text = text;
					tooltipWindow.Severity = hasConflict ? Tasks.TaskSeverity.Error : Tasks.TaskSeverity.Warning;

					tooltipWindow.ShowPopup (keyBindingsTree, hit.ButtonBounds, PopupPosition.Top);
				} else
					HideConflictTooltip ();
			}

			[GLib.ConnectBefore ()]
			void HandleKeyTreeButtonPressEvent (object o, ButtonPressEventArgs args)
			{
				var hit = HitTest (args.Event.X, args.Event.Y);
				if (!hit.ButtonBounds.IsEmpty && KeyBindingSelected != null) {
					var a = new KeyBindingSelectedEventArgs (hit.AllKeys, hit.SelectedKey, hit.Command, hit.Iter);
					KeyBindingSelected (this, a);
				}
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

					var xpad = (int)Xpad;
					int i = 0;
					foreach (var key in result.AllKeys) {
						layout.SetText (KeyBindingManager.BindingToDisplayLabel (key, false));
						layout.FontDescription = FontDesc;
						layout.FontDescription.Family = Family;
						int w, h;
						layout.GetPixelSize (out w, out h);

						int buttonWidth = w + (2 * KeyHPadding);

						if (cellx > xpad + 1 && cellx <= xpad + buttonWidth + 2 &&
						    celly > Ypad + 1 && celly <= Ypad + h + (2 * KeyVPadding) + 2) {
							var cellBounds = keyBindingsTree.GetCellArea (path, keyBindingsPanel.bindingTVCol);
							keyBindingsPanel.bindingTVCol.CellGetPosition (this, out cellx, out w);
							// GetCellArea reports the outer bounds, therefore we need to add 1px
							cellBounds.X += cellx + 2;
							cellBounds.Y += 2;
							keyBindingsTree.ConvertBinWindowToWidgetCoords (cellBounds.X, cellBounds.Y, out cellBounds.X, out cellBounds.Y);

							result.SelectedKey = i;
							result.ButtonBounds = new Gdk.Rectangle (cellBounds.X + xpad, cellBounds.Y + (int)Ypad, buttonWidth, h + KeyVPadding * 2);
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
						var ypad = (int)Ypad;
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
							layout.FontDescription = FontDesc;
							layout.FontDescription.Family = Family;
							layout.GetPixelSize (out w, out h);

							int buttonWidth = w + (2 * KeyHPadding);
							cr.RoundedRectangle (
								cell_area.X + xpad,
								cell_area.Y + ypad + (cell_area.Height - h) / 2d,
								buttonWidth,
								h + KeyVPadding * 2,
								KeyBgRadius);
							cr.LineWidth = 1;
							cr.SetSourceColor (bgColor);
							cr.FillPreserve ();
							cr.SetSourceColor (bgColor);
							cr.Stroke ();

							cr.SetSourceColor (fgColor);
							cr.MoveTo (cell_area.X + KeyHPadding + xpad, cell_area.Y + ypad + (cell_area.Height - h) / 2d + KeyVPadding);
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
						layout.FontDescription = FontDesc;
						layout.FontDescription.Family = Family;
						layout.GetPixelSize (out w, out h);
						if (height == 0)
							height = h + (KeyVPadding * 2) + (int)Ypad * 2;
						
						buttonWidth = w + (2 * KeyHPadding);
						width += buttonWidth + Spacing;
					}
				}
			}
		}
	}
}
