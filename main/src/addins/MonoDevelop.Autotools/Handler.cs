
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

		[ItemProperty ("GenerateAutotools", DefaultValue = true)]
		bool generateAutotools = true;
		
		public TarballDeployTarget ()
		{
		}

		public TarballDeployTarget (bool generateAutotools)
		{
			this.generateAutotools = generateAutotools;
		}

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
			generateAutotools = target.generateAutotools;
		}
		
		public bool GenerateFiles {
			get { return generateFiles; }
			set { generateFiles = value; }
		}
		
		public bool GenerateAutotools {
			get { return generateAutotools; }
			set { generateAutotools = value; }
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
			SolutionDeployer deployer = new SolutionDeployer (generateAutotools);
			return deployer.CanDeploy ( entry );
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
			string tmpFolder = FileService.CreateTempDirectory ();
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
					
					string efile = Services.ProjectService.Export (new FilteredProgressMonitor (monitor), entry.FileName, childEntries, tmpFolder, null);
					if (efile == null) {
						monitor.ReportError (GettextCatalog.GetString ("The project could not be exported."), null);
						return;
					}
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
			
				SolutionDeployer deployer = new SolutionDeployer (generateAutotools);
				
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
			/*string prefix_var = generateAutotools ? "@prefix@" : "$(prefix)";
			string package_var = generateAutotools ? "@PACKAGE@" : "$(PACKAGE)";*/
			//FIXME: Temp till we find a proper solution
			string prefix_var = "@prefix@";
	  		string package_var = "@PACKAGE@";

			switch (folderId) {
			case TargetDirectory.ProgramFilesRoot:
				return "@expanded_libdir@";
			case TargetDirectory.ProgramFiles:
				return "@expanded_libdir@/" + package_var;
			case TargetDirectory.Binaries:
				return "@expanded_bindir@";
			case TargetDirectory.CommonApplicationDataRoot:
				return "@expanded_datadir@";
			case TargetDirectory.CommonApplicationData:
				return "@expanded_datadir@/" + package_var;
			case TargetDirectory.IncludeRoot:
				return prefix_var + "/include";
			case TargetDirectory.Include:
				return prefix_var + "/include/" + package_var;
			}
			return null;
		}

		public override PackageBuilder[] CreateDefaultBuilders ()
		{
			return new PackageBuilder [] { this };
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
