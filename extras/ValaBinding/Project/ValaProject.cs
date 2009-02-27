//
// ValaProject.cs: Vala Project
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Diagnostics;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Deployment;
using MonoDevelop.Deployment.Linux;

using MonoDevelop.ValaBinding.Parser;

namespace MonoDevelop.ValaBinding
{
	public enum ValaProjectCommands {
		AddPackage,
		ShowPackageDetails,
		UpdateClassPad
	}
	
	[DataInclude(typeof(ValaProjectConfiguration))]
	public class ValaProject : Project, IDeployable
	{
		[ItemProperty ("compiler", ValueType = typeof(ValaCompiler))]
		private ICompiler compiler_manager;
		
		private ProjectPackageCollection packages = new ProjectPackageCollection ();
		public static string vapidir;
		
		public event ProjectPackageEventHandler PackageAddedToProject;
		public event ProjectPackageEventHandler PackageRemovedFromProject;
		
		private void Init ()
		{
			packages.Project = this;
			this.PackageAddedToProject += AddDependencies; // Special handling for project packages
			//IdeApp.ProjectOperations.EntryAddedToCombine += OnEntryAddedToCombine;
		}
		
		static ValaProject()
		{
			try {
				Process pkgconfig = new Process();
				pkgconfig.StartInfo.FileName = "pkg-config";
				pkgconfig.StartInfo.Arguments = "--variable=vapidir vala-1.0";
				pkgconfig.StartInfo.CreateNoWindow = true;
				pkgconfig.StartInfo.RedirectStandardOutput = true;
				pkgconfig.StartInfo.UseShellExecute = false;
				pkgconfig.Start();
				vapidir = pkgconfig.StandardOutput.ReadToEnd().Trim();
				pkgconfig.WaitForExit();
				pkgconfig.Dispose();
			} catch(Exception e) {
				MessageService.ShowError("Unable to detect VAPI path", string.Format("{0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace));
			}
			
			if(!Directory.Exists(vapidir)){ vapidir = "/usr/share/vala/vapi"; }
		}
		

		
		public ValaProject ()
		{
			Init ();
		}
		
		public ValaProject (ProjectCreateInformation info,
						 XmlElement projectOptions, string language)
		{
			Init ();
			string binPath = ".";
			
			if (info != null) {
				Name = info.ProjectName;
				binPath = info.BinPath;
			}
			
			Compiler = null;
			
			ValaProjectConfiguration configuration =
				(ValaProjectConfiguration)CreateConfiguration ("Debug");
			
			configuration.DebugMode = true;
			((ValaCompilationParameters)configuration.CompilationParameters).DefineSymbols = "DEBUG MONODEVELOP";		
				
			Configurations.Add (configuration);
			
			configuration =
				(ValaProjectConfiguration)CreateConfiguration ("Release");
				
			configuration.DebugMode = false;
			((ValaCompilationParameters)configuration.CompilationParameters).OptimizationLevel = 3;
			((ValaCompilationParameters)configuration.CompilationParameters).DefineSymbols = "MONODEVELOP";
			Configurations.Add (configuration);
			
			foreach (ValaProjectConfiguration c in Configurations) {
				c.OutputDirectory = Path.Combine (binPath, c.Name);
				c.SourceDirectory = info.ProjectBasePath;
				c.Output = Name;
				ValaCompilationParameters parameters = c.CompilationParameters as ValaCompilationParameters;
				
				if (projectOptions != null) {
					if (projectOptions.Attributes["Target"] != null) {
						c.CompileTarget = (ValaBinding.CompileTarget)Enum.Parse (
							typeof(ValaBinding.CompileTarget),
							projectOptions.Attributes["Target"].InnerText);
					}
					if (projectOptions.Attributes["PauseConsoleOutput"] != null) {
						c.PauseConsoleOutput = bool.Parse (
							projectOptions.Attributes["PauseConsoleOutput"].InnerText);
					}
					if (projectOptions.Attributes["CompilerArgs"].InnerText != null) {
						if (parameters != null) {
							parameters.ExtraCompilerArguments = projectOptions.Attributes["CompilerArgs"].InnerText;
						}
					}
					if (projectOptions.Attributes["Packages"].InnerText != null) {
						List<ProjectPackage> packs = new List<ProjectPackage>();
						foreach(string pack in projectOptions.Attributes["Packages"].InnerText.Split('|')) {
							packs.Add(new ProjectPackage(
								string.Format("{0}{1}{2}.vapi", 
									vapidir, 
									Path.DirectorySeparatorChar, 
									pack)));
						}
						packages.AddRange(packs);
					}
				}
			}
		}
		
		public override string ProjectType {
			get { return "Vala"; }
		}
		
		public override string[] SupportedLanguages {
			get { return new string[] { "Vala" }; }
		}
		
		public override bool IsCompileable (string fileName)
		{
			string ext = Path.GetExtension(fileName);
			return (ext.Equals(".vala", StringComparison.CurrentCultureIgnoreCase) ||
				ext.Equals(".vapi", StringComparison.CurrentCultureIgnoreCase));
		}
		
		public List<ValaProject> DependedOnProjects ()
		{
			List<string> project_names = new List<string> ();
			List<ValaProject> projects = new List<ValaProject> ();
			
			foreach (ProjectPackage p in Packages) {
				if (p.IsProject && p.Name != Name) {
					project_names.Add (p.Name);
				}
			}
			
			foreach (SolutionItem e in ParentFolder.Items) {
				if (e is ValaProject && project_names.Contains (e.Name)) {
					projects.Add ((ValaProject)e);
				}
			}
			
			return projects;
		}
		
		/// <summary>
		/// Ths pkg-config package is for internal MonoDevelop use only, it is not deployed.
		/// </summary>
		public void WriteMDPkgPackage (string configuration)
		{
			string pkgfile = Path.Combine (BaseDirectory, Name + ".md.pc");
			
			ValaProjectConfiguration config = (ValaProjectConfiguration)GetConfiguration(configuration);
			
			using (StreamWriter writer = new StreamWriter (pkgfile)) {
				writer.WriteLine ("Name: {0}", Name);
				writer.WriteLine ("Description: {0}", Description);
				writer.WriteLine ("Version: {0}", Version);
				writer.WriteLine ("Libs: -L{0} -l{1}", config.OutputDirectory, config.Output);
//				writer.WriteLine ("Cflags: -I{0}", BaseDirectory);
			}
			
			// If this project compiles into a shared object we need to
			// export the output path to the LD_LIBRARY_PATH
			string literal = "LD_LIBRARY_PATH";
			string ld_library_path = Environment.GetEnvironmentVariable (literal);
			
			if (string.IsNullOrEmpty (ld_library_path)) {
				Environment.SetEnvironmentVariable (literal, config.OutputDirectory);
			} else if (!ld_library_path.Contains (config.OutputDirectory)) {
				ld_library_path = string.Format ("{0}:{1}", config.OutputDirectory, ld_library_path);
				Environment.SetEnvironmentVariable (literal, ld_library_path);
			}
		}
		
		/// <summary>
		/// This is the pkg-config package that gets deployed.
		/// <returns>The pkg-config package's filename</returns>
		/// </summary>
		private string WriteDeployablePkgPackage (string configuration)
		{
			// FIXME: This should probably be grabed from somewhere.
			string prefix = "/usr/local";
			string pkgfile = Path.Combine (BaseDirectory, Name + ".pc");
			ValaProjectConfiguration config = (ValaProjectConfiguration)GetConfiguration(configuration);
			
			using (StreamWriter writer = new StreamWriter (pkgfile)) {
				writer.WriteLine ("prefix={0}", prefix);
				writer.WriteLine ("exec_prefix=${prefix}");
				writer.WriteLine ("libdir=${exec_prefix}/lib");
				writer.WriteLine ("includedir=${prefix}/include");
				writer.WriteLine ();
				writer.WriteLine ("Name: {0}", Name);
				writer.WriteLine ("Description: {0}", Description);
				writer.WriteLine ("Version: {0}", Version);
				writer.WriteLine ("Requires: {0}", string.Join (" ", Packages.ToStringArray ()));
				// TODO: How should I get this?
				writer.WriteLine ("Conflicts: {0}", string.Empty);
				writer.Write ("Libs: -L${libdir} ");
				writer.WriteLine ("-l{0}", config.Output);
				writer.Write ("Cflags: -I${includedir}/");
				writer.WriteLine ("{0} {1}", Name, Regex.Replace(((ValaCompilationParameters)config.CompilationParameters).DefineSymbols, @"(^|\s+)(\w+)", "-D$2 ", RegexOptions.Compiled));
			}
			
			return pkgfile;
		}
		
		protected override BuildResult DoBuild (IProgressMonitor monitor, string configuration)
		{
			ValaProjectConfiguration pc = (ValaProjectConfiguration)GetConfiguration(configuration);
			pc.SourceDirectory = BaseDirectory;
			
			return compiler_manager.Compile (
				Files, packages,
				pc,
				monitor);
		}

		protected override bool OnGetCanExecute (MonoDevelop.Projects.ExecutionContext context, string solutionConfiguration)
		{
			ValaProjectConfiguration conf = (ValaProjectConfiguration)GetConfiguration(solutionConfiguration);
			return (conf.CompileTarget == ValaBinding.CompileTarget.Bin) &&
				context.ExecutionHandlerFactory.SupportsPlatform (ExecutionPlatform.Native);
		}
		
		protected override void DoExecute (IProgressMonitor monitor,
										   ExecutionContext context,
		                                   string configuration)
		{
			ValaProjectConfiguration conf = (ValaProjectConfiguration)GetConfiguration(configuration);
			string command = conf.Output;
			string args = conf.CommandLineParameters;
			string dir = Path.GetFullPath (conf.OutputDirectory);
			string platform = "Native";
			bool pause = conf.PauseConsoleOutput;
			IConsole console;
			
			if (conf.CompileTarget != ValaBinding.CompileTarget.Bin) {
				MessageService.ShowMessage ("Compile target is not an executable!");
				return;
			}
			
			monitor.Log.WriteLine ("Running project...");
			
			if (conf.ExternalConsole)
				console = context.ExternalConsoleFactory.CreateConsole (!pause);
			else
				console = context.ConsoleFactory.CreateConsole (!pause);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				IExecutionHandler handler = context.ExecutionHandlerFactory.CreateExecutionHandler (platform);
				
				if (handler == null) {
					monitor.ReportError ("Cannot execute \"" + command + "\". The selected execution mode is not supported in the " + platform + " platform.", null);
					return;
				}
				
				IProcessAsyncOperation op = handler.Execute (Path.Combine (dir, command), args, dir, null, console);
				
				operationMonitor.AddOperation (op);
				op.WaitForCompleted ();
				
				monitor.Log.WriteLine ("The operation exited with code: {0}", op.ExitCode);
			} catch (Exception ex) {
				monitor.ReportError ("Cannot execute \"" + command + "\"", ex);
			} finally {			
				operationMonitor.Dispose ();			
				console.Dispose ();
			}
		}
		
		protected override string OnGetOutputFileName (string configuration)
		{
			ValaProjectConfiguration conf = (ValaProjectConfiguration)GetConfiguration(configuration);
			return Path.Combine (conf.OutputDirectory, conf.CompiledOutputName);
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			ValaProjectConfiguration conf = new ValaProjectConfiguration ();
			
			conf.Name = name;
			conf.CompilationParameters = new ValaCompilationParameters ();
			
			return conf;
		}
		
		public ICompiler Compiler {
			get { return compiler_manager; }
			set {
				if (value != null) {
					compiler_manager = value;
				} else {
					object[] compilers = AddinManager.GetExtensionObjects ("/ValaBinding/Compilers");
					string compiler = PropertyService.Get ("ValaBinding.DefaultValaCompiler", new ValaCompiler().Name);
					
					foreach (ICompiler c in compilers) {
						if (compiler == c.Name) {
							compiler_manager = c;
						}
					}
				}
			}
		}
		
		[Browsable(false)]
		[ItemProperty ("Packages")]
		public ProjectPackageCollection Packages {
			get { return packages; }
			set {
				packages = value;
				packages.Project = this;
				ProjectInformation pi = ProjectInformationManager.Instance.Get (this);
				foreach(ProjectPackage p in packages) {
					if (!p.IsProject){ pi.AddPackage (p.Name); }
				}
			}
		}
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			base.OnFileAddedToProject (e);
			
			if (!IsCompileable (e.ProjectFile.Name) &&
				e.ProjectFile.BuildAction == BuildAction.Compile) {
				e.ProjectFile.BuildAction = BuildAction.None;
			}
			
			if (e.ProjectFile.BuildAction == BuildAction.Compile)
				ProjectInformationManager.Instance.Get (this).AddFile (e.ProjectFile.FilePath);
		}
		
		protected override void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			base.OnFileChangedInProject (e);
//			ProjectInformationManager.Instance.Get (this).Reparse ();
			ProjectInformationManager.Instance.Get (this).AddFile (e.ProjectFile.FilePath);
		}
		
		protected override void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			base.OnFileRemovedFromProject(e);
			ProjectInformationManager.Instance.Get (this).RemoveFile (e.ProjectFile.FilePath);
		}
		
		private static void OnEntryAddedToCombine (object sender, SolutionItemEventArgs e)
		{
			ValaProject p = e.SolutionItem as ValaProject;
			
			if (p == null)
				return;
			
			foreach (ProjectPackage package in p.Packages)
				if (!package.IsProject){ ProjectInformationManager.Instance.Get (p).AddPackage (package.Name); }
			foreach (ProjectFile f in p.Files)
				ProjectInformationManager.Instance.Get (p).AddFile (f.FilePath);
		}
		
		internal void NotifyPackageRemovedFromProject (ProjectPackage package)
		{
			if (null != PackageRemovedFromProject) {
				PackageRemovedFromProject (this, new ProjectPackageEventArgs (this, package));
			}
		}
		
		internal void NotifyPackageAddedToProject (ProjectPackage package)
		{
			if(null != PackageAddedToProject) {
				PackageAddedToProject (this, new ProjectPackageEventArgs (this, package));
			}
			if (!package.IsProject){ ProjectInformationManager.Instance.Get (this).AddPackage (package.Name); }
		}

		public DeployFileCollection GetDeployFiles (string configuration)
		{
			DeployFileCollection deployFiles = new DeployFileCollection ();
			
			CompileTarget target = ((ValaProjectConfiguration)GetConfiguration(configuration)).CompileTarget;
			
			// Headers and resources
			foreach (ProjectFile f in Files) {
				if (f.BuildAction == BuildAction.Content) {
					string targetDirectory =
						(/*IsHeaderFile (f.Name) ? TargetDirectory.Include :*/ TargetDirectory.ProgramFiles);
					
					deployFiles.Add (new DeployFile (this, f.FilePath, f.RelativePath, targetDirectory));
				}
			}
			
			// Output
			string output = GetOutputFileName (configuration);
			if (!string.IsNullOrEmpty (output)) {
				string targetDirectory = string.Empty;
				
				switch (target) {
				case CompileTarget.Bin:
					targetDirectory = TargetDirectory.ProgramFiles;
					break;
				case CompileTarget.SharedLibrary:
					targetDirectory = TargetDirectory.ProgramFiles;
					break;
				case CompileTarget.StaticLibrary:
					targetDirectory = TargetDirectory.ProgramFiles;
					break;
				}					
				
				deployFiles.Add (new DeployFile (this, output, Path.GetFileName (output), targetDirectory));
			}
			
			// PkgPackage
			if (target != CompileTarget.Bin) {
				string pkgfile = WriteDeployablePkgPackage (configuration);
				deployFiles.Add (new DeployFile (this, Path.Combine (BaseDirectory, pkgfile), pkgfile, LinuxTargetDirectory.PkgConfig));
			}
			
			return deployFiles;
		}

		/// <summary>
		/// Add dependencies of project packages to current project,
		/// and add cflags for project package and LD_LIBRARY_PATH
		/// </summary>
		private void AddDependencies (object obj, ProjectPackageEventArgs args) {
			ProjectPackage package = args.Package;
			if (!package.IsProject){ return; }

			string depsfile = Path.ChangeExtension (package.File, ".deps");
			try {
				string[] lines = File.ReadAllLines (depsfile);
				List<ProjectPackage> deps = new List<ProjectPackage>();
				foreach (string line in lines) {
					deps.Add(new ProjectPackage(Path.Combine(vapidir, line) + ".vapi"));
				}// add package for each dep
				packages.AddRange(deps);

				// Currently, we need to add include directory and linker flags - this should be obsoleted
				string ccargs = string.Format (" --Xcc=\\\\\\\"-I{0}\\\\\\\" --Xcc=\\\\\\\"-L{0}\\\\\\\" --Xcc=\\\\\\\"-l{1}\\\\\\\" ", Path.GetDirectoryName (depsfile), package.Name);
				string ldpath = string.Empty;
				string packagePath = Path.GetDirectoryName(package.File);
				
				foreach (string pc in GetConfigurations ()) {
					ValaProjectConfiguration valapc = GetConfiguration (pc) as ValaProjectConfiguration;
					if (null == valapc){ continue; }

					ValaCompilationParameters vcp = (ValaCompilationParameters)valapc.CompilationParameters;
					if (!vcp.ExtraCompilerArguments.Contains (ccargs)){ vcp.ExtraCompilerArguments += ccargs; }

					if(valapc.EnvironmentVariables.TryGetValue ("LD_LIBRARY_PATH", out ldpath)) {
						if (!ldpath.Contains (packagePath)){ ldpath += Path.PathSeparator + packagePath; }
					} else {
						ldpath = packagePath;
					}
					
					valapc.EnvironmentVariables["LD_LIBRARY_PATH"] = ldpath;
				}// add compilation parameters and LD_LIBRARY_PATH
			} catch { /* Do anything here? */ }
		}// AddDependencies
	}
}
