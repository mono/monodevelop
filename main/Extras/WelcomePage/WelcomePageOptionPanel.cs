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
			showOnStartCheckButton.Active = PropertyService.Get("WelcomePage.ShowOnStartup", true);
			showOnStartCheckButton.Sensitive = MonoDevelop.Core.Gui.WebBrowserService.CanGetWebBrowser;
			vbox.PackStart(showOnStartCheckButton, false, false, 0);
		}
		
		public override bool StorePanelContents()
		{
			PropertyService.Set("WelcomePage.ShowOnStartup", showOnStartCheckButton.Active);
			return true;
		}
	}

}
