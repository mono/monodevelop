
using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Linux
{
	public class BasicOptionPanel: ItemOptionsPanel
	{
		BasicOptionPanelWidget widget;
		
		public BasicOptionPanel()
		{
		}
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return (widget = new BasicOptionPanelWidget (ConfiguredSolutionItem, false));
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
