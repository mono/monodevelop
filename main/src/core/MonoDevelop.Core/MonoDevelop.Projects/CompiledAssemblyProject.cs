// 
// CompiledAssemblyProject.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using Mono.Cecil;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using System.Collections.Generic;
using Mono.Cecil.Mdb;
using Mono.Cecil.Cil;

namespace MonoDevelop.Projects
{
	public class CompiledAssemblyProject: Project, IAssemblyProject
	{
		TargetFramework targetFramework;
		
		public CompiledAssemblyProject ()
		{
			AddNewConfiguration ("Default");
		}
		
		public override string ProjectType {
			get {
				return "CompiledAssembly";
			}
		}
		
		public override IconId StockIcon {
			get {
				return "md-assembly-project";
			}
		}

		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			return new ProjectConfiguration (name);
		}
		
		public TargetFramework TargetFramework {
			get {
				return targetFramework;
			}
		}
		
		public MonoDevelop.Core.Assemblies.TargetRuntime TargetRuntime {
			get {
				return Runtime.SystemAssemblyService.DefaultRuntime;
			}
		}
		
		public void LoadFrom (FilePath assemblyPath)
		{
			FileName = assemblyPath;
			
			var tid = Runtime.SystemAssemblyService.GetTargetFrameworkForAssembly (Runtime.SystemAssemblyService.DefaultRuntime, assemblyPath);
			if (tid != null)
				targetFramework = Runtime.SystemAssemblyService.GetTargetFramework (tid);
			
			AssemblyDefinition adef = AssemblyDefinition.ReadAssembly (assemblyPath);
			MdbReaderProvider mdbProvider = new MdbReaderProvider ();
			try {
				ISymbolReader reader = mdbProvider.GetSymbolReader (adef.MainModule, assemblyPath);
				adef.MainModule.ReadSymbols (reader);
			} catch {
				// Ignore
			}
			var files = new HashSet<FilePath> ();
			
			foreach (TypeDefinition type in adef.MainModule.Types) {
				foreach (MethodDefinition met in type.Methods) {
					if (met.HasBody && met.Body.Instructions != null && met.Body.Instructions.Count > 0) {
						SequencePoint sp = met.Body.Instructions[0].SequencePoint;
						if (sp != null)
							files.Add (sp.Document.Url);
					}
				}
			}
			
			FilePath rootPath = FilePath.Empty;
			foreach (FilePath file in files) {
				AddFile (file, BuildAction.Compile);
				if (rootPath.IsNullOrEmpty)
					rootPath = file.ParentDirectory;
				else if (!file.IsChildPathOf (rootPath))
					rootPath = FindCommonRoot (rootPath, file);
			}
			
			if (!rootPath.IsNullOrEmpty)
				BaseDirectory = rootPath;
/*
			foreach (AssemblyNameReference aref in adef.MainModule.AssemblyReferences) {
				if (aref.Name == "mscorlib")
					continue;
				string asm = assemblyPath.ParentDirectory.Combine (aref.Name);
				if (File.Exists (asm + ".dll"))
					References.Add (new ProjectReference (ReferenceType.Assembly, asm + ".dll"));
				else if (File.Exists (asm + ".exe"))
					References.Add (new ProjectReference (ReferenceType.Assembly, asm + ".exe"));
				else
					References.Add (new ProjectReference (ReferenceType.Package, aref.FullName));
			}*/
		}
		
		FilePath FindCommonRoot (FilePath p1, FilePath p2)
		{
			string[] s1 = p1.ToString ().Split (Path.DirectorySeparatorChar);
			string[] s2 = p2.ToString ().Split (Path.DirectorySeparatorChar);
			
			int n;
			for (n=0; n<s1.Length && n<s2.Length && s1[n] == s2[n]; n++);
			return string.Join (Path.DirectorySeparatorChar.ToString (), s1, 0, n);
		}
		
		protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return new BuildResult ();
		}
		
		internal protected override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			return false;
		}
		
		internal protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			ProjectConfiguration conf = (ProjectConfiguration) GetConfiguration (configuration);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Running {0} ...", FileName));

			IConsole console = conf.ExternalConsole
				? context.ExternalConsoleFactory.CreateConsole (!conf.PauseConsoleOutput)
				: context.ConsoleFactory.CreateConsole (!conf.PauseConsoleOutput);
			
			AggregatedOperationMonitor aggregatedOperationMonitor = new AggregatedOperationMonitor (monitor);

			try {
				try {
					ExecutionCommand executionCommand = CreateExecutionCommand (configuration, conf);

					if (!context.ExecutionHandler.CanExecute (executionCommand)) {
						monitor.ReportError (GettextCatalog.GetString ("Can not execute \"{0}\". The selected execution mode is not supported for .NET projects.", FileName), null);
						return;
					}

					IProcessAsyncOperation asyncOp = context.ExecutionHandler.Execute (executionCommand, console);
					aggregatedOperationMonitor.AddOperation (asyncOp);
					asyncOp.WaitForCompleted ();

					monitor.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}", asyncOp.ExitCode));
				} finally {
					console.Dispose ();
					aggregatedOperationMonitor.Dispose ();
				}
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Cannot execute \"{0}\"", FileName), ex);
				monitor.ReportError (GettextCatalog.GetString ("Cannot execute \"{0}\"", FileName), ex);
			}
		}

		internal protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = (ProjectConfiguration) GetConfiguration (configuration);
			if (config == null)
				return false;
			if (FileName.Extension.ToLower () != ".exe")
				return false;
			ExecutionCommand cmd = CreateExecutionCommand (configuration, config);
			return context.ExecutionHandler.CanExecute (cmd);
		}

		protected virtual ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel, ProjectConfiguration configuration)
		{
			DotNetExecutionCommand cmd = new DotNetExecutionCommand (FileName);
			cmd.Arguments = configuration.CommandLineParameters;
			cmd.WorkingDirectory = Path.GetDirectoryName (FileName);
			cmd.EnvironmentVariables = new Dictionary<string, string> (configuration.EnvironmentVariables);
			return cmd;
		}
	}
	
	public class CompiledAssemblyExtension: ProjectServiceExtension
	{
		public override bool IsSolutionItemFile (string fileName)
		{
			if (fileName.ToLower().EndsWith (".exe") || fileName.ToLower().EndsWith (".dll"))
				return true;
			return base.IsSolutionItemFile (fileName);
		}
		
		protected override SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName)
		{
			if (fileName.ToLower().EndsWith (".exe") || fileName.ToLower().EndsWith (".dll")) {
				CompiledAssemblyProject p = new CompiledAssemblyProject ();
				p.LoadFrom (fileName);
				return p;
			}
			return base.LoadSolutionItem (monitor, fileName);
		}
		
		public override void Save (IProgressMonitor monitor, SolutionEntityItem item)
		{
//			if (item is CompiledAssemblyProject)
//				return;
			base.Save (monitor, item);
		}
	}
}

