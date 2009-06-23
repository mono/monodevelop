
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
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
		
		[ItemProperty ("UserSwitchs")]
		[ItemProperty ("Switch", ValueType = typeof (Switch), Scope = "*")]
		List<Switch> switchs = new List<Switch> ();
		
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
			switchs = new List<Switch> (target.GetSwitches ());
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

		public override bool CanBuild (SolutionItem entry)
		{
			SolutionDeployer deployer = new SolutionDeployer (generateAutotools);
			return deployer.CanDeploy ( entry );
		}
		
		public override void InitializeSettings (SolutionItem entry)
		{
			if (string.IsNullOrEmpty (targetDir))
				targetDir = entry.BaseDirectory;
			if (string.IsNullOrEmpty (defaultConfig)) {
				SolutionEntityItem se = entry as SolutionEntityItem;
				defaultConfig = se != null ? se.GetConfigurations () [0] : null;
			}
			if (File.Exists (Path.Combine (entry.BaseDirectory, "autogen.sh")) ||
			    File.Exists (Path.Combine (entry.BaseDirectory, "configure"))) {
				generateFiles = false;
			}
			else
				generateFiles = true;
		}

		
		protected override bool OnBuild (IProgressMonitor monitor, DeployContext ctx)
		{
			string tmpFolder = FileService.CreateTempDirectory ();
			Solution solution = null;
			SolutionItem entry = RootSolutionItem;
			
			try {
				if (generateFiles) {
					List<string> childEntries = new List<string> ();
					if (entry is SolutionFolder) {
						SolutionItem[] ents = GetChildEntries ();
						foreach (SolutionItem it in ents)
							childEntries.Add (it.ItemId);
					}
					else {
						// If the entry is not a combine, use the parent combine as base combine
						childEntries.Add (entry.ItemId);
						entry = entry.ParentFolder;
					}
							
					string sourceFile;
					if (entry is SolutionFolder)
						sourceFile = entry.ParentSolution.FileName;
					else
						sourceFile = ((SolutionEntityItem)entry).FileName;
					
					string efile = Services.ProjectService.Export (new FilteredProgressMonitor (monitor), sourceFile, childEntries.ToArray (), tmpFolder, null);
					if (efile == null) {
						monitor.ReportError (GettextCatalog.GetString ("The project could not be exported."), null);
						return false;
					}
					solution = Services.ProjectService.ReadWorkspaceItem (new NullProgressMonitor (), efile) as Solution;
				}
				else {
					solution = entry.ParentSolution;
				}
				
				solution.Build (monitor, defaultConfig);
			
				if (monitor.IsCancelRequested || !monitor.AsyncOperation.Success)
					return false;
			
				SolutionDeployer deployer = new SolutionDeployer (generateAutotools);
				deployer.AddSwitches (switchs);
				
				if (!deployer.Deploy ( ctx, solution, DefaultConfiguration, TargetDir, generateFiles, monitor ))
					return false;
				
			} finally {
				if (solution != null)
					solution.Dispose ();
				Directory.Delete (tmpFolder, true);
			}
			return true;
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
		
		public void AddSwitch (Switch s)
		{
			switchs.Add (s);
			Console.WriteLine (switchs.Count);
		}
		
		public void RemoveSwitch (Switch s)
		{
			switchs.RemoveAll ((swit) => s.SwitchName == swit.SwitchName);
		}
		
		public IEnumerable<Switch> GetSwitches ()
		{
			return switchs.AsReadOnly ();
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
