using System;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace PerformanceDiagnosticsAddIn
{
	public class GlobalOptionsPanel : OptionsPanel
	{
		FolderEntry folderEntry = new FolderEntry ();
		//Gtk.CheckButton uiCountersCheck;

		public override Control CreatePanelWidget()
		{
			var vbox = new Gtk.VBox();
			vbox.Spacing = 6;

			var generalSectionLabel = new Gtk.Label("<b>" + GettextCatalog.GetString("General") + "</b>");
			generalSectionLabel.UseMarkup = true;
			generalSectionLabel.Xalign = 0;
			vbox.PackStart(generalSectionLabel, false, false, 0);

			var outputDirectoryHBox = new Gtk.HBox();
			outputDirectoryHBox.BorderWidth = 10;
			outputDirectoryHBox.Spacing = 6;
			var outputDirectoryLabel = new Gtk.Label(GettextCatalog.GetString("Output directory:"));
			outputDirectoryLabel.Xalign = 0;
			outputDirectoryHBox.PackStart(outputDirectoryLabel, false, false, 0);
			folderEntry.Path = Options.OutputPath.Value;
			outputDirectoryHBox.PackStart(folderEntry, true, true, 0);

			vbox.PackStart(outputDirectoryHBox, false, false, 0);

			//if (Options.HasMemoryLeakFeature) {
			//	uiCountersCheck = new Gtk.CheckButton (GettextCatalog.GetString ("Show UI Widget counters in the toolbar"));
			//	uiCountersCheck.Active = Options.ShowUICounters.Value;

			//	vbox.PackStart (uiCountersCheck, false, false, 0);
			//}

			vbox.ShowAll();
			return vbox;
		}

		public override void ApplyChanges()
		{
			Options.OutputPath.Value = folderEntry.Path;

			//if (Options.HasMemoryLeakFeature)
				//Options.ShowUICounters.Value = uiCountersCheck.Active;
		}
	}
}

