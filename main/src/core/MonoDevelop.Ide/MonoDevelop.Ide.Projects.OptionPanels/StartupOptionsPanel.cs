// StartupOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	[System.ComponentModel.Category("MonoDevelop.Projects.Gui")]
	[System.ComponentModel.ToolboxItem(true)]
	partial class StartupOptionsPanelWidget : Gtk.Bin
	{
		Solution sol;
		ListStore listStore;
		List<SolutionItem> startupItems;
		
		public StartupOptionsPanelWidget (Solution sol)
		{
			this.Build();
			this.sol = sol;
			
			startupItems = new List<SolutionItem> ();
			foreach (SolutionItem it in sol.GetAllItems<SolutionItem> ()) {
				// Include in the list if it can run in any of the existing execution modes and configurations
				foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
					bool matched = false;
					foreach (IExecutionMode mode in mset.ExecutionModes) {
						foreach (SolutionConfiguration sc in sol.Configurations) {
							if (it.CanExecute (new ExecutionContext (mode, null, IdeApp.Workspace.ActiveExecutionTarget), sc.Selector)) {
								startupItems.Add (it);
								matched = true;
								break;
							}
						}
						if (matched)
							break;
					}
					if (matched)
						break;
				}
			}
			
			listStore = new ListStore (typeof(SolutionFolderItem), typeof(bool), typeof(string));
			treeItems.Model = listStore;
			treeItems.SearchColumn = -1; // disable the interactive search

			CellRendererToggle crt = new CellRendererToggle ();
			treeItems.AppendColumn ("", crt, "active", 1);
			treeItems.AppendColumn (GettextCatalog.GetString ("Project"), new CellRendererText (), "text", 2);
			
			if (startupItems.Count > 0) {
				for (int n=0; n<startupItems.Count; n++) {
					SolutionItem it = startupItems [n];
					comboItems.AppendText (it.Name);
					listStore.AppendValues (it, sol.MultiStartupItems.Contains (it), it.Name);
					if (sol.StartupItem == it)
						comboItems.Active = n;
				}
			}
			else {
				comboItems.AppendText (GettextCatalog.GetString ("The solution does not contain any executable project"));
				comboItems.Active = 0;
				comboItems.Sensitive = false;
				radioMulti.Sensitive = false;
				radioSingle.Sensitive = false;
			}
			
			radioSingle.Active = sol.SingleStartup;
			radioMulti.Active = !sol.SingleStartup;
			UpdateButtons ();
			
			crt.Toggled += OnItemToggled;
			treeItems.Selection.Changed += OnSelectionChanged;
		}
		
		void UpdateButtons ()
		{
			TreeIter iter;
			if (radioSingle.Active || !treeItems.Selection.GetSelected (out iter)) {
				buttonUp.Sensitive = false;
				buttonDown.Sensitive = false;
			}
			else {
				TreeIter first;
				listStore.GetIterFirst (out first);
				buttonUp.Sensitive = !listStore.GetPath (iter).Equals (listStore.GetPath (first));
				buttonDown.Sensitive = listStore.IterNext (ref iter);
			}
			
			treeItems.Sensitive = !radioSingle.Active;
			comboItems.Sensitive = radioSingle.Active;
		}
		
		void OnItemToggled (object s, ToggledArgs args)
		{
			Gtk.TreeIter it;
			listStore.GetIterFromString (out it, args.Path);
			bool run = (bool) listStore.GetValue (it, 1);
			listStore.SetValue (it, 1, !run);
		}

		protected virtual void OnButtonUpClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (!treeItems.Selection.GetSelected (out iter))
				return;
		
			TreePath tp = listStore.GetPath (iter);
			Gtk.TreeIter pi;
			if (tp.Prev () && listStore.GetIter (out pi, tp)) {
				listStore.Swap (pi, iter);
				treeItems.ScrollToCell (listStore.GetPath (iter), treeItems.Columns[0], false, 0, 0);
				UpdateButtons ();
			}
		}

		protected virtual void OnButtonDownClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (!treeItems.Selection.GetSelected (out iter))
				return;
			
			TreeIter pit = iter;
			listStore.IterNext (ref iter);
			listStore.Swap (pit, iter);
			treeItems.ScrollToCell (listStore.GetPath (pit), treeItems.Columns[0], false, 0, 0);
			UpdateButtons ();
		}
		
		void OnSelectionChanged (object s, EventArgs args)
		{
			UpdateButtons ();
		}
		
		public void ApplyChanges ()
		{
			sol.SingleStartup = radioSingle.Active;
			sol.MultiStartupItems.Clear ();
			
			if (sol.SingleStartup) {
				if (comboItems.Active != -1 && startupItems.Count > 0)
					sol.StartupItem = startupItems [comboItems.Active];
				else
					sol.StartupItem = null;
			} else {
				TreeIter it;
				if (listStore.GetIterFirst (out it)) {
					do {
						if ((bool) listStore.GetValue (it, 1))
							sol.MultiStartupItems.Add ((SolutionItem) listStore.GetValue (it, 0));
					} while (listStore.IterNext (ref it));
				}
				sol.StartupItem = null;
			}
		}

		protected virtual void OnRadioSingleToggled (object sender, System.EventArgs e)
		{
			UpdateButtons ();
		}
	}
	
	class StartupOptionsPanel: ItemOptionsPanel
	{
		StartupOptionsPanelWidget widget;
		
		public override Control CreatePanelWidget ()
		{
			return widget = new StartupOptionsPanelWidget (ConfiguredSolution);
		}
		
		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
	}
}
