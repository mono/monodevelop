
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.GtkCore.Dialogs
{
	class GtkFeatureWidget : Gtk.VBox
	{
		ComboBox versionCombo;
		
		public GtkFeatureWidget (DotNetProject project)
		{
			Spacing = 6;
			
			versionCombo = Gtk.ComboBox.NewText ();
			ReferenceManager refmgr = new ReferenceManager (project);
			foreach (string v in refmgr.SupportedGtkVersions)
				versionCombo.AppendText (v);
			versionCombo.Active = 0;
			refmgr.Dispose ();
			
			// GTK# version selector
			HBox box = new HBox (false, 6);
			Gtk.Label vlab = new Label (GettextCatalog.GetString ("Target GTK# version:"));
			box.PackStart (vlab, false, false, 0);
			box.PackStart (versionCombo, false, false, 0);
			box.PackStart (new Label (GettextCatalog.GetString ("(or upper)")), false, false, 0);
			PackStart (box, false, false, 0);
			
			ShowAll ();
		}
		
		public string SelectedVersion {
			get { return versionCombo.ActiveText; }
		}
	}
}
