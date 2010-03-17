
 
using MonoDevelop.Ide.Gui.Dialogs;

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
