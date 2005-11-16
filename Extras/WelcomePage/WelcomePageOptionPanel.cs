using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.WelcomePage
{
	public class WelcomePageOptionPanel : AbstractOptionPanel
	{
		CheckButton showOnStartCheckButton = new CheckButton();
		
		public override void LoadPanelContents()
		{
			VBox vbox = new VBox();
			this.Add(vbox);
			
			showOnStartCheckButton.Label = GettextCatalog.GetString ("Show welcome page on startup");
			showOnStartCheckButton.Active = Runtime.Properties.GetProperty("WelcomePage.ShowOnStartup", true);
			vbox.PackStart(showOnStartCheckButton, false, false, 0);
		}
		
		public override bool StorePanelContents()
		{
			Runtime.Properties.SetProperty("WelcomePage.ShowOnStartup", showOnStartCheckButton.Active);
			return true;
		}
	}

}
