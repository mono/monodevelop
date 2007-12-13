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
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class CombineConfigurationPanel : AbstractOptionPanel
	{
		CombineConfigurationPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new CombineConfigurationPanelWidget ((Properties) CustomizationObject));
		}

		public override bool StorePanelContents()
		{
	        bool success = widget.Store ();
			return success;			
       	}
	}

	partial class CombineConfigurationPanelWidget : Gtk.Bin
	{
		TreeStore store;
		CombineConfiguration configuration;
		
		public CombineConfigurationPanelWidget (Properties CustomizationObject)
		{
			Build ();
			configuration = ((Properties)CustomizationObject).Get<CombineConfiguration> ("Config");
			
			store = new TreeStore (typeof(object), typeof(string), typeof(bool));
			configsList.Model = store;
			configsList.HeadersVisible = true;
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererText sr = new CellRendererText ();
			col.PackStart (sr, true);
			col.Expand = true;
			col.AddAttribute (sr, "text", 1);
			col.Title = GettextCatalog.GetString ("Solution Item");
			configsList.AppendColumn (col);
			
			CellRendererToggle tt = new CellRendererToggle ();
			tt.Activatable = true;
			tt.Toggled += new ToggledHandler (OnBuildToggled);
			configsList.AppendColumn (GettextCatalog.GetString ("Build"), tt, "active", 2);
			
			CellRendererComboBox comboCell = new CellRendererComboBox ();
			comboCell.Changed += new ComboSelectionChangedHandler (OnConfigSelectionChanged);
			configsList.AppendColumn (GettextCatalog.GetString ("Configuration"), comboCell, new TreeCellDataFunc (OnSetConfigurationsData));
			
			foreach (CombineConfigurationEntry ce in configuration.Entries) {
				store.AppendValues (ce, ce.Entry.Name, ce.Build);
			}
		}
		
		void OnSetConfigurationsData (Gtk.TreeViewColumn treeColumn, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CombineConfigurationEntry entry = (CombineConfigurationEntry) store.GetValue (iter, 0);
			string[] values = new string [entry.Entry.Configurations.Count];
			for (int n=0; n<values.Length; n++)
				values [n] = entry.Entry.Configurations [n].Name;
			CellRendererComboBox comboCell = (CellRendererComboBox) cell;
			comboCell.Values = values;
			comboCell.Text = entry.ConfigurationName;
		}
		
		void OnBuildToggled (object sender, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIter (out iter, new TreePath (args.Path))) {
				CombineConfigurationEntry entry = (CombineConfigurationEntry) store.GetValue (iter, 0);
				entry.Build = !entry.Build;
				store.SetValue (iter, 2, entry.Build);
			}
		}
		
		void OnConfigSelectionChanged (object s, ComboSelectionChangedArgs args)
		{
			TreeIter iter;
			if (store.GetIter (out iter, new TreePath (args.Path))) {
				CombineConfigurationEntry entry = (CombineConfigurationEntry) store.GetValue (iter, 0);
				if (args.Active != -1)
					entry.ConfigurationName = entry.Entry.Configurations [args.Active].Name;
				else
					entry.ConfigurationName = null;
			}
		}
		
		public bool Store()
		{
			// Data stored at dialog level
			return true;
		}
	}

}

