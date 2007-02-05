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
				Add (widget = new MakefileOptionPanelWidget ((IProperties) CustomizationObject, tmpData));
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public override bool StorePanelContents()
		{
			Project project = (Project) ((IProperties) CustomizationObject).GetProperty ("Project");
			MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;

			MakefileData tmpData = widget.Store ((IProperties) CustomizationObject);

			if (tmpData.IntegrationEnabled) {
				//Validate
				try {
					tmpData.Makefile.GetVariables ();	
				} catch (Exception e) {
					IdeApp.Services.MessageService.ShowError (e, GettextCatalog.GetString (
						"Specified makefile is invalid: {0}", tmpData.AbsoluteMakefileName),
						(Window) widget.Toplevel, true);
					return false;
				}

				if (tmpData.IsAutotoolsProject &&
					!File.Exists (System.IO.Path.Combine (tmpData.AbsoluteConfigureInPath, "configure.in"))) {
					IdeApp.Services.MessageService.ShowError (null, GettextCatalog.GetString (
						"Path specified for configure.in is invalid: {0}", tmpData.RelativeConfigureInPath),
						(Window) widget.Toplevel, true);
					return false;
				}

				if (tmpData.SyncReferences &&
					(String.IsNullOrEmpty (tmpData.GacRefVar.Name) ||
					String.IsNullOrEmpty (tmpData.AsmRefVar.Name) ||
					String.IsNullOrEmpty (tmpData.ProjectRefVar.Name))) {

					IdeApp.Services.MessageService.ShowError (null, GettextCatalog.GetString (
						"'Sync References' is enabled, but one of Reference variables is not set. Please correct this."),
						(Window) widget.Toplevel, true);
					return false;
				}
			
				if (!CheckNonEmptyFileVar (tmpData.BuildFilesVar, "Build"))
					return false;

				if (!CheckNonEmptyFileVar (tmpData.DeployFilesVar, "Deploy"))
					return false;

				if (!CheckNonEmptyFileVar (tmpData.ResourcesVar, "Resources"))
					return false;

				if (!CheckNonEmptyFileVar (tmpData.OthersVar, "Others"))
					return false;

				//FIXME: All file vars must be distinct

				//FIXME: Do this only if there are changes b/w tmpData and Data
				project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] = tmpData;

				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
					GettextCatalog.GetString ("Updating project"), "gtk-run", true);

				tmpData.UpdateProject (monitor, data == null || (!data.IntegrationEnabled && tmpData.IntegrationEnabled));
			}

 			return true;
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
