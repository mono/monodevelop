
using System;
using MonoDevelop.Projects;
using MonoDevelop.Deployment.Targets;

namespace MonoDevelop.Deployment.Gui
{
	internal partial class BinariesZipEditorWidget : Gtk.Bin
	{
		BinariesZipPackageBuilder builder;
		bool loading;
		DeployPlatformInfo[] platforms;
		
		public BinariesZipEditorWidget (BinariesZipPackageBuilder builder)
		{
			this.Build();
			
			this.builder = builder;
			loading = true;
			
			int pel = 0;
			platforms = DeployService.GetDeployPlatformInfo ();
			for (int n=0; n<platforms.Length; n++) {
				comboPlatform.AppendText (platforms[n].Description);
				if (platforms[n].Id == builder.Platform)
					pel = n;
			}
			
			comboPlatform.Active = pel;
			builder.Platform = platforms [pel].Id;
			
			string[] archiveFormats = DeployService.SupportedArchiveFormats;
			
			int zel = 1;
			for (int n=0; n<archiveFormats.Length; n++) {
				comboZip.AppendText (archiveFormats [n]);
				if (builder.TargetFile.EndsWith (archiveFormats [n]))
					zel = n;
			}
			
			if (!string.IsNullOrEmpty (builder.TargetFile)) {
				string ext = archiveFormats [zel];
				folderEntry.Path = System.IO.Path.GetDirectoryName (builder.TargetFile);
				entryZip.Text = System.IO.Path.GetFileName (builder.TargetFile.Substring (0, builder.TargetFile.Length - ext.Length));
				comboZip.Active = zel;
			}
			
			// Fill configurations
			zel = 0;
			foreach (string conf in builder.RootSolutionItem.ParentSolution.GetConfigurations ()) {
				comboConfiguration.AppendText (conf);
				if (conf == builder.Configuration)
					comboConfiguration.Active = zel;
				zel++;
			}
			
			loading = false;
		}
		
		public string TargetFolder {
			get { return folderEntry.Path; }
		}
		
		public string TargetZipFile {
			get {
				if (TargetFolder.Length == 0 || entryZip.Text.Length == 0)
					return "";
				else
					return System.IO.Path.Combine (TargetFolder, entryZip.Text + comboZip.ActiveText);
			}
		}
		
		void UpdateTarget ()
		{
			if (loading)
				return;
			builder.TargetFile = TargetZipFile;
			builder.Platform = platforms [comboPlatform.Active].Id;
			if (comboConfiguration.Active != -1)
				builder.Configuration = comboConfiguration.ActiveText;
		}

		protected virtual void OnFolderEntryPathChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}

		protected virtual void OnEntryZipChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}

		protected virtual void OnComboZipChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}

		protected virtual void OnComboPlatformChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}

		protected virtual void OnComboConfigurationChanged (object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}
	}
	
	class BinariesZipDeployEditor: IPackageBuilderEditor
	{
		public bool CanEdit (PackageBuilder target)
		{
			return target is BinariesZipPackageBuilder;
		}
		
		public Gtk.Widget CreateEditor (PackageBuilder target)
		{
			return new BinariesZipEditorWidget ((BinariesZipPackageBuilder)target);
		}
	}
}
