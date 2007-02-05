using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;

namespace MonoDevelop.Autotools
{
	public class MakefileOptionPanel : AbstractOptionPanel
	{
		MakefileOptionPanelWidget widget;

		public MakefileOptionPanel ()
		{
		}

		public override void LoadPanelContents()
		{
			try {
				Project project = (Project) ((IProperties) CustomizationObject).GetProperty ("Project");
				MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;

				MakefileData tmpData = null;
				if (data != null) {
					tmpData = (MakefileData) data.Clone ();
				}
				Add (widget = new MakefileOptionPanelWidget (project, tmpData));
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public override bool StorePanelContents()
		{
			Project project = (Project) ((IProperties) CustomizationObject).GetProperty ("Project");
			return widget.Store (project);
		}

		bool CheckNonEmptyFileVar (MakefileVar var, string id)
		{
			if (var.Sync && String.IsNullOrEmpty (var.Name.Trim ())) {
				IdeApp.Services.MessageService.ShowError (null, GettextCatalog.GetString (
					"File variable ({0}) is set for sync'ing, but no valid variable is selected." + 
					"Either disable the sync'ing or select a variable name.", id),
					(Window) widget.Toplevel, true);

				return false;
			}

			return true;
		}

	}
}
