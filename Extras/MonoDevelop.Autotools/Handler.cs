
using System;
using System.Text;
using System.Collections;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Deployment;
using MonoDevelop.Projects.Gui.Deployment;

namespace MonoDevelop.Autotools
{
	public class Handler: IDeployHandler
	{
		public string Id { 
			get { return "MonoDevelop.Autotools.Deployer"; }
		}
		
		public string Description {
			get { return "Tarball"; }
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
			TarballDeployTarget target = new TarballDeployTarget (Id);
			target.TargetCombine = entry as Combine;
			return target;
		}
	}
	
	public class TarballDeployTarget: DeployTarget
	{
		[ItemProperty ("TargetDirectory")]
		string targetDir;
		
		[ItemProperty ("DefaultConfiguration")]
		string defaultConfig;
		
		Combine target_combine;
		
		public TarballDeployTarget ()
		{
		}
		
		public TarballDeployTarget (string handlerId): base (handlerId)
		{
		}
		
		public override void Deploy (IProgressMonitor monitor, CombineEntry entry)
		{
			Combine combine = entry as Combine;
			SolutionDeployer deployer = new SolutionDeployer ();
			if ( defaultConfig == null || defaultConfig == "" )
				deployer.Deploy ( combine, targetDir, monitor );
			else deployer.Deploy ( combine, defaultConfig, targetDir, monitor );
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
			get { return target_combine; }
			set { target_combine = value; }
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
