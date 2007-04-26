
using System;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Linux
{
	public class BasicOptionPanel: AbstractOptionPanel
	{
		BasicOptionPanelWidget widget;
		CombineEntry entry;
		
		public BasicOptionPanel()
		{
		}
		
		public override void LoadPanelContents ()
		{
			IProperties props = (IProperties) CustomizationObject;
			entry = (CombineEntry) props.GetProperty ("CombineEntry");
			Add (widget = new BasicOptionPanelWidget (entry, false));
		}
		
		public override bool StorePanelContents ()
		{
			return widget.Store ();
		}
	}
}
