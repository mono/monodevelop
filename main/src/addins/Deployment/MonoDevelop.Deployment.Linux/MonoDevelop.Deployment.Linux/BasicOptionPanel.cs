
using MonoDevelop.Components;
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
		
		public override Control CreatePanelWidget ()
		{
			return (widget = new BasicOptionPanelWidget ((Project) ConfiguredSolutionItem, false));
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
