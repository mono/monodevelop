
using System;
using System.Collections;

using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Widgets;
using Gtk;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{
	public class CombineConfigurationPanel : AbstractOptionPanel
	{
		CombineConfigurationPanelWidget widget;
		
		class CombineConfigurationPanelWidget : GladeWidgetExtract 
		{
 			[Glade.Widget] Gtk.TreeView configsList;
			TreeStore store;
			CombineConfiguration configuration;
			
			public CombineConfigurationPanelWidget (IProperties CustomizationObject): base ("Base.glade", "CombineConfigurationsPanel")
			{
				configuration = (CombineConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
				
				store = new TreeStore (typeof(object), typeof(string), typeof(bool), typeof(string));
				configsList.Model = store;
				configsList.HeadersVisible = true;
				
				TreeViewColumn col = new TreeViewColumn ();
				CellRendererText sr = new CellRendererText ();
				col.PackStart (sr, true);
				col.Expand = true;
				col.AddAttribute (sr, "text", 1);
				col.Title = "Solution Item";
				configsList.AppendColumn (col);
				
				CellRendererToggle tt = new CellRendererToggle ();
				tt.Activatable = true;
				tt.Toggled += new ToggledHandler (OnBuildToggled);
				configsList.AppendColumn ("Build", tt, "active", 2);
				configsList.AppendColumn ("Configuration", new CellRendererText (), "text", 3);
				
				foreach (CombineConfigurationEntry ce in configuration.Entries)
					store.AppendValues (ce, ce.Entry.Name, ce.Build, ce.ConfigurationName);
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
			
			public bool Store()
			{
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

