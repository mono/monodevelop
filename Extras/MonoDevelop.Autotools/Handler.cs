
using System;
using System.Text;
using System.Collections;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Deployment;
using MonoDevelop.Deployment.Gui;

namespace MonoDevelop.Autotools
{
	public class TarballDeployTarget: PackageBuilder
	{
		[ItemProperty ("TargetDirectory")]
		string targetDir;
		
		[ItemProperty ("DefaultConfiguration")]
		string defaultConfig;
		
		public override string Description {
			get { return GettextCatalog.GetString ("Tarball"); }
		}
		
		public override void CopyFrom (PackageBuilder other)
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

		public override bool CanBuild (CombineEntry entry)
		{
			Combine combine = entry as Combine;
			if ( combine == null ) return false;
			SolutionDeployer deployer = new SolutionDeployer ();
			return deployer.CanDeploy ( combine );
		}
		
		public override void InitializeSettings (CombineEntry entry)
		{
			if (string.IsNullOrEmpty (targetDir))
				targetDir = entry.BaseDirectory;
			if (string.IsNullOrEmpty (defaultConfig)) {
				if (entry.ActiveConfiguration != null)
					defaultConfig = entry.ActiveConfiguration.Name;
			}
		}

		
		protected override void OnBuild (IProgressMonitor monitor, CombineEntry entry)
		{
			string tmpFolder = Runtime.FileService.CreateTempDirectory ();
			Combine combine = null;
			
			try {
				string efile = Services.ProjectService.Export (new NullProgressMonitor (), entry.FileName, tmpFolder, null, true);
				combine = Services.ProjectService.ReadCombineEntry (efile, new NullProgressMonitor ()) as Combine;
				combine.Build (monitor);
			
				if (monitor.IsCancelRequested || !monitor.AsyncOperation.Success)
					return;
			
				SolutionDeployer deployer = new SolutionDeployer ();
				
				using (DeployContext ctx = new DeployContext (this, "Linux", null)) {
					if (DefaultConfiguration == null || DefaultConfiguration == "")
						deployer.Deploy ( ctx, combine, TargetDir, monitor );
					else
						deployer.Deploy ( ctx, combine, DefaultConfiguration, TargetDir, monitor );
				}
			} finally {
				if (combine != null)
					combine.Dispose ();
				Directory.Delete (tmpFolder, true);
			}
		}

		protected override string OnResolveDirectory (DeployContext ctx, string folderId)
		{
			switch (folderId) {
			case TargetDirectory.ProgramFilesRoot:
				return "@prefix@/lib";
			case TargetDirectory.ProgramFiles:
				return "@prefix@/lib/@PACKAGE@";
			case TargetDirectory.Binaries:
				return "@prefix@/bin";
			case TargetDirectory.CommonApplicationDataRoot:
				return "@prefix@/share";
			case TargetDirectory.CommonApplicationData:
				return "@prefix@/share/@PACKAGE@";
			}
			return null;
		}
	}
	
	public class TarballTargetEditor: IPackageBuilderEditor
	{
		public bool CanEdit (PackageBuilder target, CombineEntry entry)
		{
			return target is TarballDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (PackageBuilder target, CombineEntry entry)
		{
			return new TarballTargetEditorWidget ((TarballDeployTarget) target, (Combine) entry);
		}
	}
}
