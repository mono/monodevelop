
using System;
using System.Text;
using System.Collections;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Deployment;
using MonoDevelop.Projects.Deployment.Extensions;
using MonoDevelop.Projects.Gui.Deployment;

namespace MonoDevelop.Autotools
{
	public class TarballDeployTarget: DeployTarget
	{
		[ItemProperty ("TargetDirectory")]
		string targetDir;
		
		[ItemProperty ("DefaultConfiguration")]
		string defaultConfig;
		
		public override string Description {
			get { return GettextCatalog.GetString ("Tarball"); }
		}
		
		public override void CopyFrom (DeployTarget other)
		{
			base.CopyFrom (other);
			TarballDeployTarget target = other as TarballDeployTarget;
			targetDir = target.targetDir;
			defaultConfig = target.defaultConfig;
		}
		
		public string TargetDir {
			get { return targetDir; }
			set { targetDir = value; }
		}
		
		public string DefaultConfiguration {
			get { return defaultConfig; }
			set { defaultConfig = value; }
		}

		public Combine TargetCombine {
			get { return base.CombineEntry as Combine; }
		}
		
		public override bool CanDeploy (CombineEntry entry)
		{
			Combine combine = entry as Combine;
			if ( combine == null ) return false;
			SolutionDeployer deployer = new SolutionDeployer ();
			return deployer.CanDeploy ( combine );
		}
		
		protected override void OnInitialize (CombineEntry entry)
		{
			if (string.IsNullOrEmpty (targetDir))
				targetDir = entry.BaseDirectory;
			if (string.IsNullOrEmpty (defaultConfig)) {
				if (entry.ActiveConfiguration != null)
					defaultConfig = entry.ActiveConfiguration.Name;
			}
		}

		
		protected override void OnDeploy (IProgressMonitor monitor)
		{
			Combine combine = CombineEntry as Combine;
			SolutionDeployer deployer = new SolutionDeployer ();
			
			if (DefaultConfiguration == null || DefaultConfiguration == "")
				deployer.Deploy ( combine, TargetDir, monitor );
			else
				deployer.Deploy ( combine, DefaultConfiguration, TargetDir, monitor );
		}

	}
	
	public class TarballTargetEditor: IDeployTargetEditor
	{
		public bool CanEdit (DeployTarget target)
		{
			return target is TarballDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (DeployTarget target)
		{
			return new TarballTargetEditorWidget ((TarballDeployTarget) target);
		}
	}
}
