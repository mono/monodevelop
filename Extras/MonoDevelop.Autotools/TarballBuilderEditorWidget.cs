
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	
	
	public partial class TarballBuilderEditorWidget : Gtk.Bin
	{
		TarballDeployTarget target;
		
		public TarballBuilderEditorWidget (TarballDeployTarget target)
		{
			this.Build();
			
			this.target = target;
			CombineEntry targetCombine = target.RootCombineEntry;
			folderEntry.Path = target.TargetDir;
			
			if ((target.DefaultConfiguration == null || target.DefaultConfiguration == "") && targetCombine.ActiveConfiguration != null)
				target.DefaultConfiguration = targetCombine.ActiveConfiguration.Name;
			
			for (int ii=0; ii < targetCombine.Configurations.Count; ii++)
			{
				string cc = targetCombine.Configurations [ii].Name;
				comboConfigs.AppendText ( cc );
				if ( cc == target.DefaultConfiguration ) comboConfigs.Active = ii;
			}
			if (target.GenerateFiles)
				radioGenerate.Active = true;
			else
				radioUseExisting.Active = true;

			cbGenerateAutotools.Active = target.GenerateAutotools;
			
			UpdateControls ();
		}
		
		void UpdateControls ()
		{
			boxConfig.Sensitive = target.GenerateFiles;
			cbGenerateAutotools.Sensitive = target.GenerateFiles;
		}

		protected virtual void OnRadioGenerateClicked(object sender, System.EventArgs e)
		{
			target.GenerateFiles = true;
			UpdateControls ();
		}

		protected virtual void OnRadioUseExistingClicked(object sender, System.EventArgs e)
		{
			target.GenerateFiles = false;
			UpdateControls ();
		}

		protected virtual void OnComboConfigsChanged(object sender, System.EventArgs e)
		{
			target.DefaultConfiguration = comboConfigs.ActiveText;
		}

		protected virtual void OnFolderEntryPathChanged(object sender, System.EventArgs e)
		{
			target.TargetDir = folderEntry.Path;
		}

		protected virtual void OnCbGenerateAutotoolsClicked (object sender, System.EventArgs e)
		{
			target.GenerateAutotools = cbGenerateAutotools.Active;
		}
	}
}
