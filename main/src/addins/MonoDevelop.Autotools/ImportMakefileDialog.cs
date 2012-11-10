
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.Autotools
{
	class ImportMakefileDialog: Gtk.Dialog
	{
		Entry nameEntry;
		MakefileOptionPanelWidget optionsWidget;
		Project project;
		
		public ImportMakefileDialog (Project project, MakefileData tmpData, string name)
		{
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.project = project;
			
			Title = GettextCatalog.GetString ("Makefile Project Import");
			Modal = true;
			
			VBox box = new VBox ();
			box.Spacing = 6;
			
			Gtk.Label lab = new Gtk.Label ();
			lab.Wrap = true;
			lab.Xalign = 0;
			lab.WidthRequest = 500;
			lab.Text = GettextCatalog.GetString (
				"{0} is going to create a project bound to a Makefile. Please enter the name you want to give to the new project.",
				BrandingService.ApplicationName
			);
			box.PackStart (lab, false, false, 0);
			
			HBox hb = new HBox ();
			hb.Spacing = 6;
			hb.PackStart (new Gtk.Label (GettextCatalog.GetString ("Project Name:")), false, false, 0);
			nameEntry = new Gtk.Entry ();
			nameEntry.Text = name;
			hb.PackStart (nameEntry, true, true, 0);
			box.PackStart (hb, false, false, 0);
			
			box.PackStart (new Gtk.HSeparator (), false, false, 0);
			
			optionsWidget = new MakefileOptionPanelWidget (this, project, tmpData);
			
			box.PackStart (optionsWidget, false, false, 0);
			box.BorderWidth = 6;
			
			this.VBox.PackStart (box, true, true, 0);
			
			this.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			this.AddButton (Gtk.Stock.Ok, ResponseType.Ok);
			ShowAll ();
			
			optionsWidget.SetImportMode ();
		}
		
		public bool Store ()
		{
			if (nameEntry.Text.Length == 0) {
				MessageService.ShowError (GettextCatalog.GetString ("Please enter a valid project name"));
				return false;
			}
			project.Name = nameEntry.Text;
			if (!optionsWidget.ValidateChanges (project))
				return false;
			optionsWidget.Store (project);
			return true;
		}
	}
}
