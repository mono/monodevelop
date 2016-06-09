// CombineConfigurationPanel.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2004 Todd Berman
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
using System.Linq;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class CombineConfigurationPanel : MultiConfigItemOptionsPanel
	{
		CombineConfigurationPanelWidget widget;
		
		public override Control CreatePanelWidget()
		{
			return widget = new CombineConfigurationPanelWidget ((MultiConfigItemOptionsDialog) ParentDialog, ConfiguredSolution);
		}

		public override void LoadConfigData ()
		{
			widget.Load ((SolutionConfiguration) CurrentConfiguration);
		}
		
		public override void ApplyChanges ()
		{
	        widget.Store ();
       	}
	}

	partial class CombineConfigurationPanelWidget : Gtk.Bin
	{
		ListStore store;
		SolutionConfiguration configuration;
		MultiConfigItemOptionsDialog parentDialog;
		Solution solution;

		const int ProjectNameCol = 0;
		const int BuildFlagCol = 1;
		const int ProjectCol = 2;
		
		public CombineConfigurationPanelWidget (MultiConfigItemOptionsDialog parentDialog, Solution solution)
		{
			Build ();
			
			this.parentDialog = parentDialog;
			this.solution = solution;

			store = new ListStore (typeof(string), typeof(bool), typeof(SolutionItem));
			configsList.Model = store;
			configsList.SearchColumn = -1; // disable the interactive search
			configsList.HeadersVisible = true;
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererText sr = new CellRendererText ();
			col.PackStart (sr, true);
			col.Expand = true;
			col.AddAttribute (sr, "text", ProjectNameCol);
			col.Title = GettextCatalog.GetString ("Solution Item");
			configsList.AppendColumn (col);
			col.SortColumnId = ProjectNameCol;
			
			CellRendererToggle tt = new CellRendererToggle ();
			tt.Activatable = true;
			tt.Toggled += new ToggledHandler (OnBuildToggled);
			configsList.AppendColumn (GettextCatalog.GetString ("Build"), tt, "active", BuildFlagCol);
			
			CellRendererComboBox comboCell = new CellRendererComboBox ();
			comboCell.Changed += new ComboSelectionChangedHandler (OnConfigSelectionChanged);
			configsList.AppendColumn (GettextCatalog.GetString ("Configuration"), comboCell, new TreeCellDataFunc (OnSetConfigurationsData));
			store.SetSortColumnId (ProjectNameCol, SortType.Ascending);
		}
		
		public void Load (SolutionConfiguration config)
		{
			configuration = config;

			store.Clear ();
			foreach (var it in solution.GetAllSolutionItems ().OfType<SolutionItem> ().Where (s => s.SupportsBuild ())) {
				var ce = config.GetEntryForItem (it);
				store.AppendValues (it.Name, ce != null && ce.Build, it);
			}
		}
		
		void OnSetConfigurationsData (Gtk.TreeViewColumn treeColumn, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			var item = (SolutionItem) store.GetValue (iter, ProjectCol);
			ConfigurationData data = parentDialog.ConfigurationData.FindConfigurationData (item);
			
			CellRendererComboBox comboCell = (CellRendererComboBox) cell;
			comboCell.Values = data.Configurations.Select (c => c.Id).ToArray ();

			var conf = GetSelectedConfiguration (item);
			var escaped = GLib.Markup.EscapeText (conf);
			if (item.Configurations [conf] == null)
				comboCell.Markup = string.Format ("<span color='red'>{0}</span>", escaped);
			else
				comboCell.Markup = escaped;
		}

		string GetSelectedConfiguration (SolutionItem item)
		{
			var entry = configuration.GetEntryForItem (item);
			return entry != null ? entry.ItemConfiguration : (item.DefaultConfiguration != null ? item.DefaultConfiguration.Id : "");
		}
		
		void OnBuildToggled (object sender, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIter (out iter, new TreePath (args.Path))) {
				var item = (SolutionItem) store.GetValue (iter, ProjectCol);
				var entry = configuration.GetEntryForItem (item);
				if (entry == null) {
					entry = CreateDefaultMapping (item);
					entry.Build = true;
				} else
					entry.Build = !entry.Build;
				store.SetValue (iter, BuildFlagCol, entry.Build);
			}
		}

		SolutionConfigurationEntry CreateDefaultMapping (SolutionItem item)
		{
			var conf = GetSelectedConfiguration (item);
			var entry = configuration.AddItem (item);
			entry.ItemConfiguration = conf;
			return entry;
		}
		
		void OnConfigSelectionChanged (object s, ComboSelectionChangedArgs args)
		{
			TreeIter iter;
			if (store.GetIter (out iter, new TreePath (args.Path))) {
				var item = (SolutionItem) store.GetValue (iter, ProjectCol);
				var entry = configuration.GetEntryForItem (item);
				if (entry == null) {
					entry = CreateDefaultMapping (item);
					entry.Build = false;
				}

				if (args.Active != -1) {
					ConfigurationData data = parentDialog.ConfigurationData.FindConfigurationData (entry.Item);
					entry.ItemConfiguration = data.Configurations [args.Active].Id;
				}
				else
					entry.ItemConfiguration = null;
			}
		}
		
		public void Store ()
		{
			// Data stored at dialog level
		}
	}

}

