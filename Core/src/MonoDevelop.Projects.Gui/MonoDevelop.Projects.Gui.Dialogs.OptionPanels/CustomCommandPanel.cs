
using System;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	public class CustomCommandPanel: AbstractOptionPanel
	{
		AbstractConfiguration configuration;
		CustomCommandCollection commands;
		
		public override void LoadPanelContents ()
		{
			Properties props = (Properties) CustomizationObject;
			configuration = props.Get<AbstractConfiguration> ("Config");
			if (configuration != null) {
				CombineEntry entry = props.Get<CombineEntry> ("CombineEntry");
				commands = configuration.CustomCommands.Clone ();
				Add (new CustomCommandPanelWidget (entry, commands));
			}
		}

		public override bool StorePanelContents ()
		{
			if (configuration != null) {
				configuration.CustomCommands.CopyFrom (commands);
				// Remove empty commands
				for (int n=0; n<configuration.CustomCommands.Count; n++) {
					if (configuration.CustomCommands [n].Command == "") {
						configuration.CustomCommands.RemoveAt (n);
						n--;
					}
				}
			}
			return true;
		}
	}
}
