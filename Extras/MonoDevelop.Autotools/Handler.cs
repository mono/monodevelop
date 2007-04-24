
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
		
		[ItemProperty ("GenerateFiles", DefaultValue=true)]
		bool generateFiles = true;
		
		public override string Description {
			get { return GettextCatalog.GetString ("Tarball"); }
		}
		
		public override void CopyFrom (PackageBuilder other)
		{
			base.CopyFrom (other);
			TarballDeployTarget target = other as TarballDeployTarget;
			targetDir = target.targetDir;
			defaultConfig = target.defaultConfig;
			generateFiles = target.generateFiles;
		}
		
		public bool GenerateFiles {
			get { return generateFiles; }
			set { generateFiles = value; }
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
			if (combine == null)
				combine = entry.ParentCombine;
			if (combine == null)
				return false;
			
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
			if (File.Exists (Path.Combine (entry.BaseDirectory, "autogen.sh")) ||
			    File.Exists (Path.Combine (entry.BaseDirectory, "configure"))) {
				generateFiles = false;
			}
			else
				generateFiles = true;
		}

		
		protected override void OnBuild (IProgressMonitor monitor, DeployContext ctx)
		{
			string tmpFolder = Runtime.FileService.CreateTempDirectory ();
			Combine combine = null;
			CombineEntry entry = RootCombineEntry;
			
			try {
				if (generateFiles) {
					string[] childEntries;
					if (entry is Combine) {
						CombineEntry[] ents = GetChildEntries ();
						childEntries = new string [ents.Length];
						for (int n=0; n<ents.Length; n++)
							childEntries [n] = ents [n].FileName;
					}
					else {
						// If the entry is not a combine, use the parent combine as base combine
						childEntries = new string [] { entry.FileName };
						entry = entry.ParentCombine;
					}
					
					string efile = Services.ProjectService.Export (new NullProgressMonitor (), entry.FileName, childEntries, tmpFolder, null);
					combine = Services.ProjectService.ReadCombineEntry (efile, new NullProgressMonitor ()) as Combine;
				}
				else {
					if (entry is Combine)
						combine = (Combine) entry;
					else 
						combine = entry.ParentCombine;
				}
				
				combine.Build (monitor);
			
				if (monitor.IsCancelRequested || !monitor.AsyncOperation.Success)
					return;
			
				SolutionDeployer deployer = new SolutionDeployer ();
				
				if (DefaultConfiguration == null || DefaultConfiguration == "")
					deployer.Deploy ( ctx, combine, TargetDir, generateFiles, monitor );
				else
					deployer.Deploy ( ctx, combine, DefaultConfiguration, TargetDir, generateFiles, monitor );
				
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
		public bool CanEdit (PackageBuilder target)
		{
			return target is TarballDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (PackageBuilder target)
		{
			return new TarballBuilderEditorWidget ((TarballDeployTarget) target);
		}
	}
}
