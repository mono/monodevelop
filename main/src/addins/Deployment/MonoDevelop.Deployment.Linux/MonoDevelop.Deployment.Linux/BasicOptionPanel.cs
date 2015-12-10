
 
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

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
			return (widget = new BasicOptionPanelWidget ((Project) ConfiguredSolutionItem, false));
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
