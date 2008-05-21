using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
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
		bool isDotNetProject;
		
		public MakefileOptionPanelWidget (Project project, MakefileData tmpData)
			: this ()
		{
			this.data = tmpData;
			isDotNetProject = (project is DotNetProject);
			if (!isDotNetProject) {
				// Disable all References combos etc for non-dotnet projects
				cbKeepRefSync.Sensitive = false;
				HandleKeepRefSyncClicked (cbKeepRefSync);
			}

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

				FillCompilerMessageCombo ();

				HandleEnableMakefileIntegrationClicked (false);
				//FIXME: Look for configure.in in parent dirs
			} else {
				this.fileEntryMakefilePath.Path = data.AbsoluteMakefileName;
				this.fileEntryMakefilePath.DefaultPath = data.AbsoluteMakefileName;
				this.cbEnableMakefileIntegration.Active = data.IntegrationEnabled;

				FillCompilerMessageCombo ();
				SetActiveVar (comboMessageType, data.MessageRegexName);

				HandleEnableMakefileIntegrationClicked (cbEnableMakefileIntegration.Active);
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
				this.fileEntryConfigureInPath.DefaultPath = project.ParentSolution.BaseDirectory;
			else
				this.fileEntryConfigureInPath.DefaultPath = data.AbsoluteConfigureInPath;

			this.BuildTargetName.Text = data.BuildTargetName;
			this.ExecuteTargetName.Text = data.ExecuteTargetName;
			this.CleanTargetName.Text = data.CleanTargetName;
			
			cbBuildTarget.Active = BuildTargetName.Sensitive = data.BuildTargetName != string.Empty;
			cbRunTarget.Active = ExecuteTargetName.Sensitive = data.ExecuteTargetName != string.Empty;
			cbCleanTarget.Active = CleanTargetName.Sensitive = data.CleanTargetName != string.Empty;

			HandleComboMessageTypeChanged (comboMessageType);

			this.fileEntryMakefilePath.FocusChildSet += new FocusChildSetHandler (OnMakefilePathFocusChildSet);
			
			((Gtk.Container) comboAssemblyName.Parent).Remove (comboAssemblyName);
			((Gtk.Container) lblAssemblyNameVar.Parent).Remove (lblAssemblyNameVar);

			((Gtk.Container) comboOutputDir.Parent).Remove (comboOutputDir);
			((Gtk.Container) lblOutputDirVar.Parent).Remove (lblOutputDirVar);
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
			combos = new ComboBox [7] {
				comboFilesVar, comboDeployFilesVar, comboResourcesVar, comboOthersVar, 
				comboGacRefVar, comboAsmRefVar, comboProjectRefVar}; 
				//comboAssemblyName, comboOutputDir};
		}
		
		public bool ValidateChanges (Project project)
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
			data.GacRefVar.Sync = this.cbKeepRefSync.Active;
			data.GacRefVar.Name = GetActiveVar (comboGacRefVar);
			data.GacRefVar.Prefix = this.entryGacRefPattern.Text.Trim ();

			data.AsmRefVar.Sync = this.cbKeepRefSync.Active;
			data.AsmRefVar.Name = GetActiveVar (comboAsmRefVar);
			data.AsmRefVar.Prefix = this.entryAsmRefPattern.Text.Trim ();

			data.ProjectRefVar.Sync = this.cbKeepRefSync.Active;
			data.ProjectRefVar.Name = GetActiveVar (comboProjectRefVar);
			data.ProjectRefVar.Prefix = this.entryProjectRefPattern.Text.Trim ();

			data.IsAutotoolsProject = this.cbAutotoolsProject.Active;
			if (this.cbAutotoolsProject.Active)
				data.RelativeConfigureInPath = this.fileEntryConfigureInPath.Path;
			
			//data.AssemblyNameVar = GetActiveVar (comboAssemblyName);
			//data.OutputDirVar = GetActiveVar (comboOutputDir);
			data.BuildTargetName = this.BuildTargetName.Text.Trim ();
			data.ExecuteTargetName = this.ExecuteTargetName.Text.Trim ();
			data.CleanTargetName = this.CleanTargetName.Text.Trim ();
			
			data.MessageRegexName = GetActiveVar (comboMessageType);
			if (data.MessageRegexName == "Custom") {
				data.CustomErrorRegex = this.entryErrorRegex.Text;
				data.CustomWarningRegex = this.entryWarningRegex.Text;
			}
			
			// Data validation

			MakefileData oldData = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			MakefileData tmpData = data;

			if (tmpData.IntegrationEnabled) {
				//Validate
				try {
					tmpData.Makefile.GetVariables ();
				} catch (FileNotFoundException e) {
					ShowMakefileNotFoundError (e);
					return false;
				} catch (Exception e) {
					MessageService.ShowException ((Window) Toplevel, e, GettextCatalog.GetString ("Specified makefile is invalid: {0}", tmpData.AbsoluteMakefileName));
					return false;
				}

				if (tmpData.IsAutotoolsProject &&
					!File.Exists (System.IO.Path.Combine (tmpData.AbsoluteConfigureInPath, "configure.in")) &&
				    !File.Exists (System.IO.Path.Combine (tmpData.AbsoluteConfigureInPath, "configure.ac")))
				{
					MessageService.ShowError ((Window)Toplevel, GettextCatalog.GetString ("Path specified for configure.in is invalid: {0}", tmpData.RelativeConfigureInPath));
					return false;
				}

				if (tmpData.SyncReferences &&
					(String.IsNullOrEmpty (tmpData.GacRefVar.Name) ||
					String.IsNullOrEmpty (tmpData.AsmRefVar.Name) ||
					String.IsNullOrEmpty (tmpData.ProjectRefVar.Name))) {

					MessageService.ShowError ((Window) Toplevel, GettextCatalog.GetString ("'Sync References' is enabled, but one of Reference variables is not set. Please correct this."));
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
				try {
					tmpData.GetErrorRegex (true);
				} catch (Exception e) {
					MessageService.ShowError ((Window) Toplevel, GettextCatalog.GetString ("Invalid regex for Error messages: {0}", e.Message));
					return false;
				}

				try {
					tmpData.GetWarningRegex (true);
				} catch (Exception e) {
					MessageService.ShowError ((Window) Toplevel, GettextCatalog.GetString (
						"Invalid regex for Warning messages: {0}", e.Message));
					return false;
				}

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

		public void Store (Project project)
		{
			// FIXME: Storing currently done in ValidateChanges. It should be done here.
		}

		bool CheckNonEmptyFileVar (MakefileVar var, string id)
		{
			if (var.Sync && String.IsNullOrEmpty (var.Name.Trim ())) {
				MessageService.ShowError ((Window) Toplevel,GettextCatalog.GetString (
					"File variable ({0}) is set for sync'ing, but no valid variable is selected. Either disable the sync'ing or select a variable name.", id));

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

			//SetActiveVar (comboAssemblyName, data.AssemblyNameVar);
			//SetActiveVar (comboOutputDir, data.OutputDirVar);
		}
		
		void FillCompilerMessageCombo ()
		{
			foreach (string s in MakefileData.CompilerMessageRegex.Keys)
				comboMessageType.AppendText (s);
				
			comboMessageType.AppendText ("Custom");		
			comboMessageType.Active = 0;
		}

		protected virtual void OnEnableMakefileIntegrationClicked (object sender, System.EventArgs e)
		{
			HandleEnableMakefileIntegrationClicked (((CheckButton) sender).Active);
		}
		
		void HandleEnableMakefileIntegrationClicked (bool active)
		{
			table1.Sensitive = active;
			if (active) {
				bool first_load = String.IsNullOrEmpty (data.RelativeMakefileName);
				if (TryLoadMakefile (false)) {
					if (first_load)
						GuessVariables ();
					else
						LoadVariables ();
				} else {
					fileEntryMakefilePath.Path = fileEntryMakefilePath.DefaultPath;
				}
			} else {
				SetActive (active);
			}
		}

		// return true if all went fine
		bool TryLoadMakefile (bool showError)
		{
			try {
				data.RelativeMakefileName = fileEntryMakefilePath.Path;
				ICollection<string> vars = TryGetVariables (showError);
				bool active = vars != null;

				if (active)
					FillCombos (vars);

				SetActive (active);

				return active;
			} catch {
				return false;
			}
		}

		string FindConfigureScript (string startpath)
		{
			if (String.IsNullOrEmpty (startpath))
				return null;

			string path = startpath;
			while (true) {
				string fname = SPath.Combine (path, "configure.in");
				if (File.Exists (fname))
					return fname;

				fname = SPath.Combine (path, "configure.ac");
				if (File.Exists (fname))
					return fname;

				string parentpath = SPath.GetFullPath (SPath.Combine (path, ".."));
				if (parentpath == path)
					//reached root
					return null;

				path = parentpath;
			}
		}

		// Try to guess suitable variables for build files, references and resources
		void GuessVariables ()
		{
			ICollection<string> vars = TryGetVariables (false);
			if (vars == null)
				return;

			string files_var = GetActiveVar (comboFilesVar);
			string res_var = GetActiveVar (comboResourcesVar);
			string ref_var = GetActiveVar (comboGacRefVar);

			string prefix;
			foreach (string var in vars) {
				if (ref_var.Length > 0 && res_var.Length > 0 && files_var.Length > 0)
					break;

				if (files_var.Length == 0 && CheckSourceCode (data.Makefile.GetListVariable (var))) {
					files_var = var;
					SetFilesVariable (files_var);
					continue;
				}

				if (res_var.Length == 0 && CheckRes (data.Makefile.GetListVariable (var), out prefix)) {
					res_var = var;
					SetResourcesVariable (res_var, prefix);
					continue;
				}

				// We only try to find one variable for references
				if (ref_var.Length == 0 && CheckRefs (data.Makefile.GetListVariable (var), out prefix)) {
					ref_var = var;
					SetReferencesVariable (ref_var, prefix);
					continue;
				}
			}

			// Try to guess using some common variable names
			if (files_var.Length == 0) {
				string [] files_var_names = {"FILES"};

				foreach (string var in files_var_names) {
					if (data.Makefile.GetListVariable (var) != null) {
						SetFilesVariable (var);
						break;
					}
				}
			}

			//as these vars would've been already selected if a valid prefix was there
			if (res_var.Length == 0) {
				string [] res_var_names = {"RESOURCES", "RES"};

				foreach (string var in res_var_names) {
					if (data.Makefile.GetListVariable (var) != null) {
						SetResourcesVariable (var, GuessResPrefix (data.Makefile.GetListVariable (var)));
						break;
					}
				}
			}

			if (ref_var.Length == 0) {
				string [] ref_var_names = {"REFERENCES", "REFS"};

				foreach (string var in ref_var_names) {
					if (data.Makefile.GetListVariable (var) != null) {
						SetReferencesVariable (var, GuessRefPrefix (data.Makefile.GetListVariable (var)));
						break;
					}
				}
			}

			// Try to find configure.(in|ac) string
			string path = FindConfigureScript (SPath.GetDirectoryName (data.AbsoluteMakefileName));
			if (path != null) {
				fileEntryConfigureInPath.Path = fileEntryConfigureInPath.DefaultPath = SPath.GetDirectoryName (path);
				cbAutotoolsProject.Active = true;
				HandleCbAutotoolsProjectClicked (cbAutotoolsProject);
			}
		}

		void ResetAll ()
		{
			cbFileSync.Active = false;

			cbKeepFilesSync.Active = false;
			entryFilesPattern.Text = String.Empty;

			cbKeepDeployFilesSync.Active = false;
			entryDeployFilesPattern.Text = String.Empty;

			cbKeepResourcesSync.Active = false;
			entryResourcesPattern.Text = String.Empty;

			cbKeepOthersSync.Active = false;
			entryOthersPattern.Text = String.Empty;

			cbKeepRefSync.Active = false;

			entryGacRefPattern.Text = String.Empty;
			entryAsmRefPattern.Text = String.Empty;
			entryProjectRefPattern.Text = String.Empty;

			fileEntryConfigureInPath.Path = String.Empty;
			cbAutotoolsProject.Active = false;

			SetActive (false);
		}

		void SetFilesVariable (string files_var)
		{
			cbFileSync.Sensitive = true;
			cbFileSync.Active = true;
			HandleFileSyncClicked (cbFileSync);

			cbKeepFilesSync.Sensitive = true;
			cbKeepFilesSync.Active = true;
			HandleKeepFilesSyncClicked (cbKeepFilesSync);

			SetActiveVar (comboFilesVar, files_var);
		}

		void SetResourcesVariable (string res_var, string prefix)
		{
			cbFileSync.Sensitive = true;
			cbFileSync.Active = true;
			HandleFileSyncClicked (cbFileSync);

			cbKeepResourcesSync.Sensitive = true;
			cbKeepResourcesSync.Active = true;

			SetActiveVar (comboResourcesVar, res_var);
			entryResourcesPattern.Text = prefix;
		}

		void SetReferencesVariable (string ref_var, string prefix)
		{
			cbKeepRefSync.Sensitive = true;
			cbKeepRefSync.Active = true;
			HandleKeepRefSyncClicked (cbKeepRefSync);

			SetActiveVar (comboGacRefVar, ref_var);
			SetActiveVar (comboAsmRefVar, ref_var);
			SetActiveVar (comboProjectRefVar, ref_var);

			entryGacRefPattern.Text = prefix;
			entryAsmRefPattern.Text = prefix;
			entryProjectRefPattern.Text = prefix;
		}

		void SetActive (bool active)
		{
			this.cbBuildTarget.Sensitive = active;
			OnCbBuildTargetClicked (null, null);

			this.cbRunTarget.Sensitive = active;
			OnCbRunTargetClicked (null, null);

			this.cbCleanTarget.Sensitive = active;
			OnCbCleanTargetClicked (null, null);

			/*this.lblAssemblyNameVar.Sensitive = active;
			this.comboAssemblyName.Sensitive = active;

			this.lblOutputDirVar.Sensitive = active;
			this.comboOutputDir.Sensitive = active;*/

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

			if (isDotNetProject) {
				this.cbKeepRefSync.Sensitive = active;
				HandleKeepRefSyncClicked (cbKeepRefSync);
			}

			this.cbAutotoolsProject.Sensitive = active;
			HandleCbAutotoolsProjectClicked (cbAutotoolsProject);

			this.comboMessageType.Sensitive = active;
			label7.Sensitive = active;
			lblErrorRegex.Sensitive = active;
			lblMessageType.Sensitive = active;
			lblWarningRegex.Sensitive = active;
			HandleComboMessageTypeChanged (comboMessageType);
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
			
			this.label6.Sensitive = state;
			this.lblCol5.Sensitive = state;
			this.lblCol6.Sensitive = state;
			
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

		void OnMakefilePathFocusChildSet (object sender, FocusChildSetArgs e)
		{
			if (data.AbsoluteMakefileName != fileEntryMakefilePath.Path) {
				ResetAll ();
				if (TryLoadMakefile (true))
					GuessVariables ();
			}
		}
		
		void FillCombos (ICollection<string> vars)
		{
			if (vars == null)
				return;

			try {
				//Clearing
				for (int i = 0; i < combos.Length; i ++)
					((ListStore) combos [i].Model).Clear ();
				
				List<string> list = new List<string> (vars);
				list.Sort ();
				
				comboFilesVar.AppendText ("(None)");
				foreach (string item in list)
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
			} catch (FileNotFoundException e) {
				if (showError)
					ShowMakefileNotFoundError (e);
			} catch (Exception e) {
				if (showError)
					MessageService.ShowException ((Window) this.Toplevel,e,
						GettextCatalog.GetString ("Error while trying to read the specified Makefile"));
				return null;
			}

			if (vars != null && vars.Count == 0) {
				if (showError)
					MessageService.ShowError ((Window) this.Toplevel, 
						GettextCatalog.GetString ("No variables found in the selected Makefile"));
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

		protected virtual void OnComboMessageTypeChanged(object sender, System.EventArgs e)
		{
			HandleComboMessageTypeChanged (comboMessageType);
		}
		
		void HandleComboMessageTypeChanged (ComboBox cb)
		{
			string active = GetActiveVar (cb);
			bool isCustom = (active == "Custom");
			bool state;
			if (cb.Sensitive)
				state = isCustom;
			else
				state = false;
			
			if (!isCustom) {
				this.entryErrorRegex.Text = MakefileData.CompilerMessageRegex [active][0];
				this.entryWarningRegex.Text = MakefileData.CompilerMessageRegex [active][1];
			} else if (data.MessageRegexName == "Custom") {
				// Custom selected and data.MessageRegexName == "Custom"
				this.entryErrorRegex.Text = data.CustomErrorRegex;
				this.entryWarningRegex.Text = data.CustomWarningRegex;
			}
			
			this.entryErrorRegex.Sensitive = state;
			this.entryWarningRegex.Sensitive = state;
		}

		protected virtual void OnCbBuildTargetClicked(object sender, System.EventArgs e)
		{
			if (cbBuildTarget.Sensitive && cbBuildTarget.Active) {
				BuildTargetName.Sensitive = true;
				BuildTargetName.Text = "all";
			} else {
				BuildTargetName.Sensitive = false;
				BuildTargetName.Text = "";
			}
		}

		protected virtual void OnCbRunTargetClicked(object sender, System.EventArgs e)
		{
			if (cbRunTarget.Sensitive && cbRunTarget.Active) {
				ExecuteTargetName.Sensitive = true;
				ExecuteTargetName.Text = "run";
			} else {
				ExecuteTargetName.Sensitive = false;
				ExecuteTargetName.Text = "";
			}
		}

		protected virtual void OnCbCleanTargetClicked(object sender, System.EventArgs e)
		{
			if (cbCleanTarget.Sensitive && cbCleanTarget.Active) {
				CleanTargetName.Sensitive = true;
				CleanTargetName.Text = "clean";
			} else {
				CleanTargetName.Sensitive = false;
				CleanTargetName.Text = "";
			}
		}

		protected virtual void OnComboGacRefVarChanged (object sender, System.EventArgs e)
		{
			HandleComboGacRefVarChanged ((ComboBox) sender);
		}

		void HandleComboGacRefVarChanged (ComboBox cb)
		{
			string active = GetActiveVar (cb);
			entryGacRefPattern.Text = GuessRefPrefix (data.Makefile.GetListVariable (active));
		}

		protected virtual void OnComboAsmRefVarChanged (object sender, System.EventArgs e)
		{
			HandleComboAsmRefVarChanged ((ComboBox) sender);
		}

		void HandleComboAsmRefVarChanged (ComboBox cb)
		{
			string active = GetActiveVar (cb);
			entryAsmRefPattern.Text = GuessRefPrefix (data.Makefile.GetListVariable (active));
		}

		protected virtual void OnComboProjectRefVarChanged (object sender, System.EventArgs e)
		{
			HandleComboProjectRefVarChanged ((ComboBox) sender);
		}

		void HandleComboProjectRefVarChanged (ComboBox cb)
		{
			string active = GetActiveVar (cb);
			entryProjectRefPattern.Text = GuessRefPrefix (data.Makefile.GetListVariable (active));
		}

		protected virtual void OnComboResourcesVarChanged (object sender, System.EventArgs e)
		{
			HandleComboResourcesVarChanged ((ComboBox) sender);
		}

		void HandleComboResourcesVarChanged (ComboBox cb)
		{
			string active = GetActiveVar (cb);
			entryResourcesPattern.Text = GuessResPrefix (data.Makefile.GetListVariable (active));
		}

		void ShowMakefileNotFoundError (Exception e)
		{
				MessageService.ShowException ((Window) this.Toplevel, 
			                                  e,
			                                  GettextCatalog.GetString ("Unable to find the specified Makefile. You need to specify the path to an existing Makefile for use with the 'Makefile Integration' feature."));
		}

		// Returns true if either
		//	- has a valid prefix
		//	- Or all entries are
		//		assembly names from packages (eg. System, gtk-sharp) or
		//		variables like $(FOO) or
		//		*.dll
		bool CheckRefs (List<string> list, out string prefix)
		{
			prefix = GuessRefPrefix (list);
			if (prefix.Length > 0)
				return true;

			// 'core' here simply means any assemblies in the gac
			bool has_core_or_pkgref = false;
			foreach (string file in list) {
				if (MakefileData.CorePackageAssemblyNames.ContainsKey (file) || IsPkgRef (file)) {
					has_core_or_pkgref = true;
					continue;
				}

				// invalid if any entry isn't one of core/variable/dll/pkgrefs
				if (!IsVariable (file) && !IsDll (file))
					return false;
			}

			return has_core_or_pkgref;
		}

		bool CheckRes (List<string> list, out string prefix)
		{
			prefix = GuessResPrefix (list);
			if (prefix.Length > 0)
				return true;

			// no consistent prefix found
			// FIXME: any other checks? check for *.resx/*.resources?
			return false;
		}

		// Returns the prefix if all files,
		//	other than variables like $(FOO) and
		//	pkg references like -pkg:foo,
		// have the same prefix.
		// Valid prefixes : -r: /r: -reference: /reference:
		string GuessRefPrefix (List<string> list)
		{
			if (list == null || list.Count == 0)
				return String.Empty;

			string prefix = String.Empty;
			int i = 0;

			for (i = 0; i < list.Count; i ++) {
				string file = list [i];
				if (IsVariable (file) || IsPkgRef (file))
					continue;

				//check for prefix
				if (file.Length > 3 &&
					(file [0] == '-' || file [0] == '/') && file [1] == 'r') {
					if (file [2] == ':' ||
						(file.Length > 12 && file.Substring (2, 9) == "eference:")) {
						prefix = file.Substring (0, file.IndexOf (':') + 1);
					}
				}
				break;
			}

			if (prefix.Length > 0) {
				// Ensure that all remaining entries are valid
				for (; i < list.Count; i ++) {
					string s = list [i];
					if (! ((s.StartsWith (prefix) && s.Length > prefix.Length) || IsVariable (s) || IsPkgRef (s)))
						return String.Empty;
				}
			}

			return prefix;
		}

		// Returns the prefix if all files,
		//	other than variables like $(FOO),
		// have the same prefix.
		// Valid prefixes : -res: /res: -resource: /resource:
		string GuessResPrefix (List<string> list)
		{
			if (list == null || list.Count == 0)
				return String.Empty;

			string prefix = String.Empty;
			int i = 0;

			for (i = 0; i < list.Count; i ++) {
				string file = list [i];
				if (IsVariable (file))
					continue;

				if (file.Length > 5 &&
					(file [0] == '-' || file [0] == '/') &&
						file [1] == 'r' && file [2] == 'e' && file [3] == 's') {
					//check for prefix
					if (file [4] == ':' || (file.Length > 11 && file.Substring (2, 8) == "esource:"))
						prefix = file.Substring (0, file.IndexOf (':') + 1);
				}
				break;
			}

			if (prefix.Length > 0) {
				// Ensure that all remaining entries are valid
				for (; i < list.Count; i ++) {
					string file = list [i];
					if (! ((file.StartsWith (prefix) && file.Length > prefix.Length) || IsVariable (file)))
						return String.Empty;
				}
			}

			return prefix;
		}

		// Return true if entries are either source code files
		// or variables. Atleast one source file must be present.
		bool CheckSourceCode (List<string> list)
		{
			if (!isDotNetProject || list == null || list.Count == 0)
				return false;

			bool has_source = false;
			DotNetProject dnp = (DotNetProject) data.OwnerProject;

			foreach (string s in list) {
				if (dnp.LanguageBinding.IsSourceCodeFile (s))
					has_source = true;
				else if (!IsVariable (s))
					return false;
			}

			return has_source;
		}

		bool IsVariable (string file)
		{
			return (file.Length > 3 && file [0] == '$' && file [1] == '(' &&
				file.IndexOf (')') == file.Length - 1);
		}

		bool IsDll (string file)
		{
			return SPath.GetExtension (file).ToUpper () == ".DLL";
		}

		bool IsPkgRef (string file)
		{
			return (file.Length > 5 &&
				file [0] == '-' && file [1] == 'p' && file [2] == 'k' &&
				file [3] == 'g' && file [4] == ':');
		}
	}
}
