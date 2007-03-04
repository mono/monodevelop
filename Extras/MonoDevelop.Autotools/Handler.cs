
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
	public class Handler: IDeployHandler
	{
		public string Id { 
			get { return "MonoDevelop.Autotools.Deployer"; }
		}
		
		public string Description {
			get { return GettextCatalog.GetString ("Tarball"); }
		}
		
		public string Icon {
			get { return null; }
		}
		
		public bool CanDeploy (CombineEntry entry)
		{
			Combine combine = entry as Combine;
			if ( combine == null ) return false;
			SolutionDeployer deployer = new SolutionDeployer ();
			return deployer.CanDeploy ( combine );
		}
		
		public DeployTarget CreateTarget (CombineEntry entry)
		{
			return new TarballDeployTarget ();
		}
		
		public void Deploy (IProgressMonitor monitor, DeployTarget target)
		{
			TarballDeployTarget tar = (TarballDeployTarget) target;
			Combine combine = target.CombineEntry as Combine;
			SolutionDeployer deployer = new SolutionDeployer ();
			if ( tar.DefaultConfiguration == null || tar.DefaultConfiguration == "" )
				deployer.Deploy ( combine, tar.TargetDir, monitor );
			else deployer.Deploy ( combine, tar.DefaultConfiguration, tar.TargetDir, monitor );
		}
	}
	
	public class TarballDeployTarget: DeployTarget
	{
		[ItemProperty ("TargetDirectory")]
		string targetDir;
		
		[ItemProperty ("DefaultConfiguration")]
		string defaultConfig;
		
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
