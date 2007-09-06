//
// CProject.cs: C/C++ Project
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
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

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Deployment;
using MonoDevelop.Deployment.Linux;

using CBinding.Parser;

namespace CBinding
{
	public enum Language {
		C,
		CPP
	}
	
	public enum CProjectCommands {
		AddPackage,
		UpdateClassPad
	}
	
	[DataInclude(typeof(CProjectConfiguration))]
	public class CProject : Project, IDeployable
	{
		[ItemProperty ("compiler", ValueType = typeof(CCompiler))]
		private ICompiler compiler_manager;
		
		[ItemProperty]
		private Language language;
		
    	private ProjectPackageCollection packages = new ProjectPackageCollection ();
		
		public event ProjectPackageEventHandler PackageAddedToProject;
		public event ProjectPackageEventHandler PackageRemovedFromProject;
		
		private void Init ()
		{
			packages.Project = this;
		}
		
		public CProject ()
		{
			Init ();
		}
		
		public CProject (ProjectCreateInformation info,
		                 XmlElement projectOptions, string language)
		{
			Init ();
			string binPath = ".";
			
			if (info != null) {
				Name = info.ProjectName;
				binPath = info.BinPath;
			}
			
			switch (language)
			{
			case "C":
				this.language = Language.C;
				break;
			case "CPP":
				this.language = Language.CPP;
				break;
			}
			
			Compiler = null; // use default compiler depending on language
			
			CProjectConfiguration configuration =
				(CProjectConfiguration)CreateConfiguration ("Debug");
			
			((CCompilationParameters)configuration.CompilationParameters).DefineSymbols = "DEBUG MONODEVELOP";		
				
			Configurations.Add (configuration);
			
			configuration =
				(CProjectConfiguration)CreateConfiguration ("Release");
				
			configuration.DebugMode = false;
			((CCompilationParameters)configuration.CompilationParameters).OptimizationLevel = 3;
			((CCompilationParameters)configuration.CompilationParameters).DefineSymbols = "MONODEVELOP";
			Configurations.Add (configuration);
			
			foreach (CProjectConfiguration c in Configurations) {
				c.OutputDirectory = Path.Combine (binPath, c.Name);
				c.SourceDirectory = BaseDirectory;
				c.Output = Name;
				CCompilationParameters parameters = c.CompilationParameters as CCompilationParameters;
				
				if (projectOptions != null) {
					if (projectOptions.Attributes["Target"] != null) {
						c.CompileTarget = (CBinding.CompileTarget)Enum.Parse (
						    typeof(CBinding.CompileTarget),
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
					if (projectOptions.Attributes["LinkerArgs"].InnerText != null) {
						if (parameters != null) {
							parameters.ExtraLinkerArguments = projectOptions.Attributes["LinkerArgs"].InnerText;
						}
					}
				}
			}			
		}
		
		public override string ProjectType {
			get { return "Native"; }
		}
		
		public override string[] SupportedLanguages {
			get { return new string[] { "C", "CPP" }; }
		}
		
		public override bool IsCompileable (string fileName)
		{
			if (language == Language.C) {
				return (Path.GetExtension (fileName.ToUpper ()) == ".C");
			} else {
				return (Path.GetExtension (fileName.ToUpper ()) == ".CPP" ||
				        Path.GetExtension (fileName.ToUpper ()) == ".CXX" ||
				        Path.GetExtension (fileName.ToUpper ()) == ".C"   ||
				        Path.GetExtension (fileName.ToUpper ()) == ".CC");
			}
		}
		
		public List<CProject> DependedOnProjects ()
		{
			List<string> project_names = new List<string> ();
			List<CProject> projects = new List<CProject> ();
			
			foreach (ProjectPackage p in Packages) {
				if (p.IsProject && p.Name != Name) {
					project_names.Add (p.Name);
				}
			}
			
			foreach (CombineEntry e in ParentCombine.Entries) {
				if (e is CProject && project_names.Contains (e.Name)) {
					projects.Add ((CProject)e);
				}
			}
			
			return projects;
		}
		
		public static bool IsHeaderFile (string filename)
		{
			return (Path.GetExtension (filename.ToUpper ()) == ".H" ||
			        Path.GetExtension (filename.ToUpper ()) == ".HPP");
		}
		
		/// <summary>
		/// Ths pkg-config package is for internal MonoDevelop use only, it is not deployed.
		/// </summary>
		public void WriteMDPkgPackage ()
		{
			string pkgfile = Path.Combine (BaseDirectory, Name + ".md.pc");
			
			CProjectConfiguration config = (CProjectConfiguration)ActiveConfiguration;
			
			List<string> headerDirectories = new List<string> ();
			
			foreach (ProjectFile f in ProjectFiles) {
				if (IsHeaderFile (f.Name)) {
					string dir = Path.GetDirectoryName (f.FilePath);
					
					if (!headerDirectories.Contains (dir)) {
						headerDirectories.Add (dir);
					}
				}
			}
			
			using (StreamWriter writer = new StreamWriter (pkgfile)) {
				writer.WriteLine ("Name: {0}", Name);
				writer.WriteLine ("Description: {0}", Description);
				writer.WriteLine ("Version: {0}", Version);
				writer.WriteLine ("Libs: -L{0} -l{1}", config.OutputDirectory, config.Output);
//				writer.WriteLine ("Cflags: -I{0}", BaseDirectory);
				writer.WriteLine ("Cflags: -I{0}", string.Join (" -I", headerDirectories.ToArray ()));
			}
			
			// If this project compiles into a shared object we need to
			// export the output path to the LD_LIBRARY_PATH
			string literal = "LD_LIBRARY_PATH";
			string ld_library_path = Environment.GetEnvironmentVariable (literal);
			
			if (!ld_library_path.Contains (config.OutputDirectory)) {
				ld_library_path = string.Format ("{0}:{1}", config.OutputDirectory, ld_library_path);
				Environment.SetEnvironmentVariable (literal, ld_library_path);
			}
		}
		
		/// <summary>
		/// This is the pkg-config package that gets deployed.
		/// <returns>The pkg-config package's filename</returns>
		/// </summary>
		private string WriteDeployablePgkPackage ()
		{
			// FIXME: This should probably be grabed from somewhere.
			string prefix = "/usr/local";
			string pkgfile = Path.Combine (BaseDirectory, Name + ".pc");
			CProjectConfiguration config = (CProjectConfiguration)ActiveConfiguration;
			
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
				writer.WriteLine ("{0} {1}", Name, Compiler.GetDefineFlags (config));
			}
			
			return pkgfile;
		}
		
		protected override ICompilerResult DoBuild (IProgressMonitor monitor)
		{
			CProjectConfiguration pc = (CProjectConfiguration)ActiveConfiguration;
			pc.SourceDirectory = BaseDirectory;
			
			return compiler_manager.Compile (
				ProjectFiles, packages,
				(CProjectConfiguration)ActiveConfiguration,
			    monitor);
		}
		
		protected override void DoExecute (IProgressMonitor monitor,
		                                   ExecutionContext context)
		{
			CProjectConfiguration conf = (CProjectConfiguration)ActiveConfiguration;
			string command = conf.Output;
			string args = conf.CommandLineParameters;
			string dir = Path.GetFullPath (conf.OutputDirectory);
			string platform = "Native";
			bool pause = conf.PauseConsoleOutput;
			IConsole console;
			
			if (conf.CompileTarget != CBinding.CompileTarget.Bin) {
				IdeApp.Services.MessageService.ShowMessage ("Compile target is not an executable!");
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
				
				IProcessAsyncOperation op = handler.Execute (Path.Combine (dir, command), args, dir, console);
				
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
		
		public override string GetOutputFileName ()
		{
			CProjectConfiguration conf = (CProjectConfiguration)ActiveConfiguration;
			return Path.Combine (conf.OutputDirectory, conf.CompiledOutputName);
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			CProjectConfiguration conf = new CProjectConfiguration ();
			
			conf.Name = name;
			conf.CompilationParameters = new CCompilationParameters ();
			
			return conf;
		}
		
		public Language Language {
			get { return language; }
			set { language = value; }
		}
		
		public ICompiler Compiler {
			get { return compiler_manager; }
			set {
				if (value != null) {
					compiler_manager = value;
				} else {
					if (language == Language.C)
						compiler_manager = new GccCompiler ();
					else
						compiler_manager = new GppCompiler ();
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
			}
		}
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			base.OnFileAddedToProject (e);
			
			if (!IsCompileable (e.ProjectFile.Name) &&
			    e.ProjectFile.BuildAction == BuildAction.Compile) {
				e.ProjectFile.BuildAction = BuildAction.Nothing;
			}
		}
		
		protected override void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			base.OnFileChangedInProject (e);
			
			TagDatabaseManager.Instance.UpdateFileTags (this, e.ProjectFile.Name);
		}
		
		internal void NotifyPackageRemovedFromProject (ProjectPackage package)
		{
			PackageRemovedFromProject (this, new ProjectPackageEventArgs (this, package));
		}
		
		internal void NotifyPackageAddedToProject (ProjectPackage package)
		{
			PackageAddedToProject (this, new ProjectPackageEventArgs (this, package));
		}

		public DeployFileCollection GetDeployFiles ()
		{
			DeployFileCollection deployFiles = new DeployFileCollection ();
			
			CompileTarget target = ((CProjectConfiguration)ActiveConfiguration).CompileTarget;
			
			// Headers and resources
			foreach (ProjectFile f in ProjectFiles) {
				if (f.BuildAction == BuildAction.FileCopy) {
					string targetDirectory =
						(IsHeaderFile (f.Name) ? TargetDirectory.Include : TargetDirectory.CommonApplicationData);
					
					deployFiles.Add (new DeployFile (this, f.FilePath, f.RelativePath, targetDirectory));
				}
			}
			
			// Output
			string output = GetOutputFileName ();		
			if (!string.IsNullOrEmpty (output)) {
				string targetDirectory = string.Empty;
				
				switch (target) {
				case CompileTarget.Bin:
					targetDirectory = TargetDirectory.Binaries;
					break;
				case CompileTarget.SharedLibrary:
					targetDirectory = TargetDirectory.ProgramFilesRoot;
					break;
				case CompileTarget.StaticLibrary:
					targetDirectory = TargetDirectory.ProgramFilesRoot;
					break;
				}					
				
				deployFiles.Add (new DeployFile (this, output, Path.GetFileName (output), targetDirectory));
			}
			
			// PkgPackage
			if (target != CompileTarget.Bin) {
				string pkgfile = WriteDeployablePgkPackage ();
				deployFiles.Add (new DeployFile (this, Path.Combine (BaseDirectory, pkgfile), pkgfile, LinuxTargetDirectory.PkgConfig));
			}
			
			return deployFiles;
		}
	}
}
