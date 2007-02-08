
using System;
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	public class CombineConfigurationPanel : AbstractOptionPanel
	{
		CombineConfigurationPanelWidget widget;
		
		class CombineConfigurationPanelWidget : GladeWidgetExtract 
		{
 			[Glade.Widget] Gtk.TreeView configsList;
			TreeStore store;
			CombineConfiguration configuration;
			ConfigurationData configData;
			
			public CombineConfigurationPanelWidget (IProperties CustomizationObject): base ("Base.glade", "CombineConfigurationsPanel")
			{
				configuration = (CombineConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
				configData = (ConfigurationData)((IProperties)CustomizationObject).GetProperty("CombineConfigData");
				
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

		public override void LoadPanelContents()
		{
			Add (widget = new CombineConfigurationPanelWidget ((IProperties) CustomizationObject));
		}

		public override bool StorePanelContents()
		{
	        bool success = widget.Store ();
			return success;			
       	}
	}
}

