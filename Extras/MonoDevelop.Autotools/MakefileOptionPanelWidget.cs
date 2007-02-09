using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using System;
using System.Collections.Generic;
using System.IO;
using Gtk;

using SPath = System.IO.Path;

namespace MonoDevelop.Autotools
{
	public partial class MakefileOptionPanelWidget : Gtk.Bin
	{
		MakefileData data;
		ComboBox [] combos = null;
		
		public MakefileOptionPanelWidget (Project project, MakefileData tmpData)
			: this ()
		{
			this.data = tmpData;
			
			if (data == null) {
				//Use defaults
				data = new MakefileData ();
				data.OwnerProject = project;

				this.cbEnableMakefileIntegration.Active = false;

				if (File.Exists (SPath.Combine (project.BaseDirectory, "Makefile.am")))
					this.fileEntryMakefilePath.Path = SPath.Combine (project.BaseDirectory, "Makefile.am");
				else if (File.Exists (SPath.Combine (project.BaseDirectory, "Makefile")))
					this.fileEntryMakefilePath.Path = SPath.Combine (project.BaseDirectory, "Makefile");

				this.fileEntryMakefilePath.DefaultPath = project.BaseDirectory;

				HandleEnableMakefileIntegrationClicked (false);
				//FIXME: Look for configure.in in parent dirs
			} else {
				this.fileEntryMakefilePath.Path = data.AbsoluteMakefileName;
				this.fileEntryMakefilePath.DefaultPath = data.AbsoluteMakefileName;
				this.cbEnableMakefileIntegration.Active = data.IntegrationEnabled;

				HandleEnableMakefileIntegrationClicked (cbEnableMakefileIntegration.Active);

				LoadVariables ();
			}

			//FIXME: ResetAll  : use for new data, use for new makefile
			//Load values

			this.fileEntryMakefilePath.BrowserTitle = GettextCatalog.GetString ("Makefile");
		
			this.cbKeepFilesSync.Active = data.BuildFilesVar.Sync;
			this.entryFilesPattern.Text = data.BuildFilesVar.Prefix;

			this.cbKeepDeployFilesSync.Active = data.DeployFilesVar.Sync;
			this.entryDeployFilesPattern.Text = data.DeployFilesVar.Prefix;
	
			this.cbKeepResourcesSync.Active = data.ResourcesVar.Sync;
			this.entryResourcesPattern.Text = data.ResourcesVar.Prefix;
			
			this.cbKeepOthersSync.Active = data.OthersVar.Sync;
			this.entryOthersPattern.Text = data.OthersVar.Prefix;
			
			if (data.BuildFilesVar.Sync || data.DeployFilesVar.Sync || data.ResourcesVar.Sync || data.OthersVar.Sync) {
				// Enable File sync if any of the filevars are set to sync
				this.cbFileSync.Active = true;
				HandleFileSyncClicked (cbFileSync);
			}
			
			//References
			this.cbKeepRefSync.Active = data.SyncReferences;

			this.entryGacRefPattern.Text = data.GacRefVar.Prefix;
			this.entryAsmRefPattern.Text = data.AsmRefVar.Prefix;
			this.entryProjectRefPattern.Text = data.ProjectRefVar.Prefix;
			
			this.cbAutotoolsProject.Active = data.IsAutotoolsProject;
			HandleCbAutotoolsProjectClicked (cbAutotoolsProject);

			this.fileEntryConfigureInPath.Path = data.AbsoluteConfigureInPath;
			if (String.IsNullOrEmpty (data.AbsoluteConfigureInPath))
				this.fileEntryConfigureInPath.DefaultPath = project.RootCombine.BaseDirectory;
			else
				this.fileEntryConfigureInPath.DefaultPath = data.AbsoluteConfigureInPath;

			this.BuildTargetName.Text = data.BuildTargetName;
			this.ExecuteTargetName.Text = data.ExecuteTargetName;
			this.CleanTargetName.Text = data.CleanTargetName;
				
			this.fileEntryMakefilePath.FocusOutEvent += new FocusOutEventHandler (OnMakefilePathFocusOut);
		}
		
		public void SetImportMode ()
		{
			lblMakefileName.Hide ();
			fileEntryMakefilePath.Hide ();
			cbEnableMakefileIntegration.Hide ();
			headerSep1.Hide ();
			headerSep2.Hide ();
		}
		
		public MakefileOptionPanelWidget()
		{
			this.Build();
			combos = new ComboBox [9] {
				comboFilesVar, comboDeployFilesVar, comboResourcesVar, comboOthersVar, 
				comboGacRefVar, comboAsmRefVar, comboProjectRefVar, 
				comboAssemblyName, comboOutputDir};
		}
		
		public bool Store (Project project)
		{
			data.IntegrationEnabled = this.cbEnableMakefileIntegration.Active;
			data.RelativeMakefileName = this.fileEntryMakefilePath.Path;
			
			data.BuildFilesVar.Sync = this.cbKeepFilesSync.Active;
			data.BuildFilesVar.Name = GetActiveVar (comboFilesVar);
			data.BuildFilesVar.Prefix = this.entryFilesPattern.Text.Trim ();

			data.DeployFilesVar.Sync = this.cbKeepDeployFilesSync.Active;
			data.DeployFilesVar.Name = GetActiveVar (comboDeployFilesVar);
			data.DeployFilesVar.Prefix = this.entryDeployFilesPattern.Text.Trim ();

			data.ResourcesVar.Sync = this.cbKeepResourcesSync.Active;
			data.ResourcesVar.Name = GetActiveVar (comboResourcesVar);
			data.ResourcesVar.Prefix = this.entryResourcesPattern.Text.Trim ();

			data.OthersVar.Sync = this.cbKeepOthersSync.Active;
			data.OthersVar.Name = GetActiveVar (comboOthersVar);
			data.OthersVar.Prefix = this.entryOthersPattern.Text.Trim ();

			if (!this.cbFileSync.Active) {
				// Files sync is unchecked, disable syncing of all files
				data.BuildFilesVar.Sync = false;
				data.DeployFilesVar.Sync = false;
				data.ResourcesVar.Sync = false;
				data.OthersVar.Sync = false;
			}

			// References
			data.SyncReferences = this.cbKeepRefSync.Active;
			data.GacRefVar.Sync = this.cbKeepResourcesSync.Active;
			data.GacRefVar.Name = GetActiveVar (comboGacRefVar);
			data.GacRefVar.Prefix = this.entryGacRefPattern.Text.Trim ();

			data.AsmRefVar.Sync = this.cbKeepResourcesSync.Active;
			data.AsmRefVar.Name = GetActiveVar (comboAsmRefVar);
			data.AsmRefVar.Prefix = this.entryAsmRefPattern.Text.Trim ();

			data.ProjectRefVar.Sync = this.cbKeepResourcesSync.Active;
			data.ProjectRefVar.Name = GetActiveVar (comboProjectRefVar);
			data.ProjectRefVar.Prefix = this.entryProjectRefPattern.Text.Trim ();

			data.IsAutotoolsProject = this.cbAutotoolsProject.Active;
			if (this.cbAutotoolsProject.Active)
				data.RelativeConfigureInPath = this.fileEntryConfigureInPath.Path;
			
			data.AssemblyNameVar = GetActiveVar (comboAssemblyName);
			data.OutputDirVar = GetActiveVar (comboOutputDir);
			data.BuildTargetName = this.BuildTargetName.Text.Trim ();
			data.ExecuteTargetName = this.ExecuteTargetName.Text.Trim ();
			data.CleanTargetName = this.CleanTargetName.Text.Trim ();
			
			// Data validation

			MakefileData oldData = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			MakefileData tmpData = data;

			if (tmpData.IntegrationEnabled) {
				//Validate
				try {
					tmpData.Makefile.GetVariables ();	
				} catch (Exception e) {
					IdeApp.Services.MessageService.ShowError (e, GettextCatalog.GetString (
						"Specified makefile is invalid: {0}", tmpData.AbsoluteMakefileName),
						(Window) Toplevel, true);
					return false;
				}

				if (tmpData.IsAutotoolsProject &&
					!File.Exists (System.IO.Path.Combine (tmpData.AbsoluteConfigureInPath, "configure.in"))) {
					IdeApp.Services.MessageService.ShowError (null, GettextCatalog.GetString (
						"Path specified for configure.in is invalid: {0}", tmpData.RelativeConfigureInPath),
						(Window) Toplevel, true);
					return false;
				}

				if (tmpData.SyncReferences &&
					(String.IsNullOrEmpty (tmpData.GacRefVar.Name) ||
					String.IsNullOrEmpty (tmpData.AsmRefVar.Name) ||
					String.IsNullOrEmpty (tmpData.ProjectRefVar.Name))) {

					IdeApp.Services.MessageService.ShowError (null, GettextCatalog.GetString (
						"'Sync References' is enabled, but one of Reference variables is not set. Please correct this."),
						(Window) Toplevel, true);
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

				tmpData.UpdateProject (monitor, oldData == null || (!oldData.IntegrationEnabled && tmpData.IntegrationEnabled));
			} else {
				if (oldData != null)
					oldData.IntegrationEnabled = false;
			}

 			return true;
		}

		bool CheckNonEmptyFileVar (MakefileVar var, string id)
		{
			if (var.Sync && String.IsNullOrEmpty (var.Name.Trim ())) {
				IdeApp.Services.MessageService.ShowError (null, GettextCatalog.GetString (
					"File variable ({0}) is set for sync'ing, but no valid variable is selected." + 
					"Either disable the sync'ing or select a variable name.", id),
					(Window) Toplevel, true);

				return false;
			}

			return true;
		}
		
		string GetActiveVar (ComboBox combo)
		{
			Gtk.TreeIter iter;
			if (!combo.GetActiveIter (out iter))
				return string.Empty;
				
			string var = (string) combo.Model.GetValue (iter, 0);
			if (String.Compare (var, "(None)") == 0)
				return String.Empty;
			else
				return var.Trim ();
		}

		void SetActiveVar (ComboBox combo, string val)
		{
			if (String.IsNullOrEmpty (val)) {
				combo.Active = 0;
				return;
			}

			int i = 0;
			foreach (object [] o in (ListStore)combo.Model) {
				string item = o [0] as string;
				if (item == null)
					continue;

				if (String.Compare (val, item) == 0) {
					combo.Active = i;
					return;
				}
				i ++;
			}
			//If not found!
			combo.Active = 0;
		}

		void LoadVariables ()
		{
			SetActiveVar (comboFilesVar, data.BuildFilesVar.Name);
			SetActiveVar (comboDeployFilesVar, data.DeployFilesVar.Name);
			SetActiveVar (comboResourcesVar, data.ResourcesVar.Name);
			SetActiveVar (comboOthersVar, data.OthersVar.Name);

			SetActiveVar (comboGacRefVar, data.GacRefVar.Name);
			SetActiveVar (comboAsmRefVar, data.AsmRefVar.Name);
			SetActiveVar (comboProjectRefVar, data.ProjectRefVar.Name);

			SetActiveVar (comboAssemblyName, data.AssemblyNameVar);
			SetActiveVar (comboOutputDir, data.OutputDirVar);
		}

		protected virtual void OnEnableMakefileIntegrationClicked (object sender, System.EventArgs e)
		{
			HandleEnableMakefileIntegrationClicked (((CheckButton) sender).Active);
		}
		
		void HandleEnableMakefileIntegrationClicked (bool active)
		{
			table1.Sensitive = active;
			if (active) {
				HandleMakefileNameChanged (fileEntryMakefilePath.Path, false);
				LoadVariables ();
			} else {
				SetActive (active);
			}
		}

		void HandleMakefileNameChanged (string fname, bool showError)
		{
			try {
				data.RelativeMakefileName = fileEntryMakefilePath.Path;
				ICollection<string> vars = TryGetVariables (showError);
				bool active = vars != null;

				if (active)
					FillCombos (vars);

				SetActive (active);
			} catch {
				// Ignore
			}
		}

		void SetActive (bool active)
		{
			this.lblAssemblyNameVar.Sensitive = active;
			this.comboAssemblyName.Sensitive = active;

			this.lblOutputDirVar.Sensitive = active;
			this.comboOutputDir.Sensitive = active;

			this.cbFileSync.Sensitive = active;
			HandleFileSyncClicked (cbFileSync);

			this.cbKeepFilesSync.Sensitive = active;
			HandleKeepFilesSyncClicked (cbKeepFilesSync);
			
			this.cbKeepDeployFilesSync.Sensitive = active;
			HandleKeepDeployFilesSyncClicked (cbKeepDeployFilesSync);
			
			this.cbKeepResourcesSync.Sensitive = active;
			HandleKeepResourcesSyncClicked (cbKeepResourcesSync);

			this.cbKeepOthersSync.Sensitive = active;
			HandleKeepOthersSyncClicked (cbKeepOthersSync);

			this.cbKeepRefSync.Sensitive = active;
			HandleKeepRefSyncClicked (cbKeepRefSync);

			this.cbAutotoolsProject.Sensitive = active;
			HandleCbAutotoolsProjectClicked (cbAutotoolsProject);
		}

		protected virtual void OnCbFileSyncClicked(object sender, System.EventArgs e)
		{
			HandleFileSyncClicked ((CheckButton) sender);
		}
		
		void HandleFileSyncClicked (CheckButton cb)
		{
			if (cb.Sensitive)
				table3.Sensitive = cb.Active;
			else
				table3.Sensitive = false;
		}

		protected virtual void OnCbKeepRefSyncClicked(object sender, System.EventArgs e)
		{
			HandleKeepRefSyncClicked ((CheckButton) sender);
		}
		
		void HandleKeepRefSyncClicked (CheckButton cb)
		{
			bool state;
			if (cb.Sensitive)
				state = cb.Active;
			else
				state = false;
			
			this.lblGacRef.Sensitive = state;
			this.comboGacRefVar.Sensitive = state;
			this.entryGacRefPattern.Sensitive = state;

			this.lblAsmRef.Sensitive = state;
			this.comboAsmRefVar.Sensitive = state;
			this.entryAsmRefPattern.Sensitive = state;

			this.lblProjectRef.Sensitive = state;
			this.comboProjectRefVar.Sensitive = state;
			this.entryProjectRefPattern.Sensitive = state;
		}

		protected virtual void OnCbAutotoolsProjectClicked(object sender, System.EventArgs e)
		{
			HandleCbAutotoolsProjectClicked ((CheckButton) sender);
		}
		
		void HandleCbAutotoolsProjectClicked (CheckButton cb)
		{
			bool state;
			if (cb.Sensitive)
				state = cb.Active;
			else
				state = false;
			
			this.lblConfigureInPath.Sensitive = state;
			this.fileEntryConfigureInPath.Sensitive = state;
		}

		void OnMakefilePathFocusOut (object sender, FocusOutEventArgs e)
		{
			HandleMakefileNameChanged (fileEntryMakefilePath.Path, true);
		}
		
		void FillCombos (ICollection<string> vars)
		{
			if (vars == null)
				return;

			try {
				//Clearing
				for (int i = 0; i < combos.Length; i ++)
					((ListStore) combos [i].Model).Clear ();
				
				comboFilesVar.AppendText ("(None)");
				foreach (string item in vars)
					combos [0].AppendText (item);
				
				combos [0].Active = 0;
				for (int i = 1; i < combos.Length; i ++) {
					combos [i].Model = combos [0].Model;
					combos [i].Active = 0;
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		ICollection<string> TryGetVariables (bool showError)
		{
			ICollection<string> vars = null;
			try {
				vars = data.Makefile.GetVariables ();
			} catch (Exception e) {
				if (showError)
					IdeApp.Services.MessageService.ShowError (e,
						GettextCatalog.GetString ("Error while trying to read the specified Makefile"),
						(Window) this.Toplevel, true);
				return null;
			}

			if (vars != null && vars.Count == 0) {
				if (showError)
					IdeApp.Services.MessageService.ShowError (null, 
						GettextCatalog.GetString ("No variables found in the selected Makefile"),
						(Window) this.Toplevel, true);
				return null;
			}

			return vars;
		}
		
		protected virtual void OnFileEntryMakefilePathFocusOutEvent (object sender, System.EventArgs e)
		{
		}

		protected virtual void OnCbKeepFilesSyncClicked(object sender, System.EventArgs e)
		{
			HandleKeepFilesSyncClicked ((CheckButton) sender);
		}

		void HandleKeepFilesSyncClicked (CheckButton cb)
		{
			bool state;
			if (cb.Sensitive)
				state = cb.Active;
			else
				state = false;

			this.comboFilesVar.Sensitive = state;
			this.entryFilesPattern.Sensitive = state;
		}

		protected virtual void OnCbKeepDeployFilesSyncClicked(object sender, System.EventArgs e)
		{
			HandleKeepDeployFilesSyncClicked ((CheckButton) sender);
		}

		void HandleKeepDeployFilesSyncClicked (CheckButton cb)
		{
			bool state;
			if (cb.Sensitive)
				state = cb.Active;
			else
				state = false;

			this.comboDeployFilesVar.Sensitive = state;
			this.entryDeployFilesPattern.Sensitive = state;
		}

		protected virtual void OnCbKeepResourcesSyncClicked(object sender, System.EventArgs e)
		{
			HandleKeepResourcesSyncClicked ((CheckButton) sender);
		}

		void HandleKeepResourcesSyncClicked (CheckButton cb)
		{
			bool state;
			if (cb.Sensitive)
				state = cb.Active;
			else
				state = false;

			this.comboResourcesVar.Sensitive = state;
			this.entryResourcesPattern.Sensitive = state;
		}

		protected virtual void OnCbKeepOthersSyncClicked(object sender, System.EventArgs e)
		{
			HandleKeepOthersSyncClicked ((CheckButton) sender);
		}

		void HandleKeepOthersSyncClicked (CheckButton cb)
		{
			bool state;
			if (cb.Sensitive)
				state = cb.Active;
			else
				state = false;

			this.comboOthersVar.Sensitive = state;
			this.entryOthersPattern.Sensitive = state;
		}
	}
}
