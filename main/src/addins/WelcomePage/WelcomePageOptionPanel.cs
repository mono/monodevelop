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
			showOnStartCheckButton.Active = WelcomePageOptions.ShowOnStartup;
			vbox.PackStart(showOnStartCheckButton, false, false, 0);
			
			internetUpdateCheckButton.Label = GettextCatalog.GetString ("Update welcome page from internet");
			internetUpdateCheckButton.Active = WelcomePageOptions.UpdateFromInternet;
			vbox.PackStart(internetUpdateCheckButton, false, false, 0);
			
			closeOnOpenSlnCheckButton.Label = GettextCatalog.GetString ("Close welcome page after opening a solution");
			closeOnOpenSlnCheckButton.Active = WelcomePageOptions.CloseWhenSolutionOpened;
			vbox.PackStart(closeOnOpenSlnCheckButton, false, false, 0);
			
			vbox.ShowAll ();
			return vbox;
		}
		
		public override void ApplyChanges ()
		{
			WelcomePageOptions.ShowOnStartup = showOnStartCheckButton.Active;
			WelcomePageOptions.UpdateFromInternet = internetUpdateCheckButton.Active;
			WelcomePageOptions.CloseWhenSolutionOpened = closeOnOpenSlnCheckButton.Active;
		}
	}

}
