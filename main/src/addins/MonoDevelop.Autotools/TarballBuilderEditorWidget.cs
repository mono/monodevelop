
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
			alignment1.Xalign = 0.16f;
			alignment1.Xscale = 0.04f;
			
			this.target = target;
			SolutionItem targetCombine = target.RootSolutionItem;
			folderEntry.Path = target.TargetDir;
			
			if (string.IsNullOrEmpty (target.DefaultConfiguration)) {
				target.DefaultConfiguration = targetCombine.ParentSolution.GetConfigurations () [0];
			}
			
			for (int ii=0; ii < targetCombine.ParentSolution.Configurations.Count; ii++)
			{
				string cc = targetCombine.ParentSolution.Configurations [ii].Id;
				comboConfigs.AppendText ( cc );
				if ( cc == target.DefaultConfiguration ) comboConfigs.Active = ii;
			}
			if (target.GenerateFiles)
				radioGenerate.Active = true;
			else
				radioUseExisting.Active = true;

			rbAutotools.Active = target.GenerateAutotools;
			rbSimple.Active = !target.GenerateAutotools;
			
			UpdateControls ();
		}
		
		void UpdateControls ()
		{
			boxGenerate.Sensitive = target.GenerateFiles;
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

		protected virtual void OnRbSimpleToggled (object sender, System.EventArgs e)
		{
			target.GenerateAutotools = rbAutotools.Active;
		}

		protected virtual void OnRbAutotoolsToggled (object sender, System.EventArgs e)
		{
			target.GenerateAutotools = rbAutotools.Active;
		}

		protected virtual void OnAutofooPropertiesClicked (object sender, System.EventArgs e)
		{
			MakefileSwitchEditor editor = new MakefileSwitchEditor (target);
			editor.TransientFor = this.Toplevel as Gtk.Window;
			editor.ShowAll ();
			editor.Run ();
			editor.Destroy ();
		}
	}
}
