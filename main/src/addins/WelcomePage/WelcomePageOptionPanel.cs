using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.WelcomePage
{
	public class WelcomePageOptionPanel : OptionsPanel
	{
		CheckButton showOnStartCheckButton = new CheckButton ();
		CheckButton internetUpdateCheckButton = new CheckButton ();
		CheckButton closeOnOpenSlnCheckButton = new CheckButton ();
		
		public override Widget CreatePanelWidget ()
		{
			VBox vbox = new VBox();
			showOnStartCheckButton.Label = GettextCatalog.GetString ("Show welcome page on startup");
			showOnStartCheckButton.Active = PropertyService.Get("WelcomePage.ShowOnStartup", true);
			vbox.PackStart(showOnStartCheckButton, false, false, 0);
			
			internetUpdateCheckButton.Label = GettextCatalog.GetString ("Update welcome page from internet");
			internetUpdateCheckButton.Active = PropertyService.Get("WelcomePage.UpdateFromInternet", true);
			vbox.PackStart(internetUpdateCheckButton, false, false, 0);
			
			closeOnOpenSlnCheckButton.Label = GettextCatalog.GetString ("Close welcome page after opening a solution");
			closeOnOpenSlnCheckButton.Active = PropertyService.Get("WelcomePage.CloseWhenSolutionOpened", true);
			vbox.PackStart(closeOnOpenSlnCheckButton, false, false, 0);
			
			vbox.ShowAll ();
			return vbox;
		}
		
		public override void ApplyChanges ()
		{
			PropertyService.Set ("WelcomePage.ShowOnStartup", showOnStartCheckButton.Active);
			PropertyService.Set ("WelcomePage.UpdateFromInternet", internetUpdateCheckButton.Active);
			PropertyService.Set ("WelcomePage.CloseWhenSolutionOpened", closeOnOpenSlnCheckButton.Active);
		}
	}

}
