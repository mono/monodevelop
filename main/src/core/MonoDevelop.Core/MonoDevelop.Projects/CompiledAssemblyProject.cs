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
using System.Threading.Tasks;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	[ExportProjectType ("{8BC9CEB9-8B4A-11D0-8D11-00A0C91BC942}")]
	public class CompiledAssemblyProject: Project, IAssemblyProject
	{
		TargetFramework targetFramework;
		
		public CompiledAssemblyProject ()
		{
			Initialize (this);
			AddNewConfiguration ("Default");
			StockIcon = "md-assembly-project";
		}

		protected override void OnGetTypeTags (HashSet<string> types)
		{
			base.OnGetTypeTags (types);
			types.Add ("CompiledAssembly");
		}

		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
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
		
		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return Task.FromResult (BuildResult.CreateSuccess ());
		}
		
		protected async override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			ProjectConfiguration conf = (ProjectConfiguration) GetConfiguration (configuration);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Running {0} ...", FileName));

			OperationConsole console = conf.ExternalConsole
				? context.ExternalConsoleFactory.CreateConsole (!conf.PauseConsoleOutput, monitor.CancellationToken)
										   : context.ConsoleFactory.CreateConsole (
											   OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (Path.GetFileName (FileName)), monitor.CancellationToken);
			
			try {
				try {
					ExecutionCommand executionCommand = CreateExecutionCommand (configuration, conf);

					if (!context.ExecutionHandler.CanExecute (executionCommand)) {
						monitor.ReportError (GettextCatalog.GetString ("Can not execute \"{0}\". The selected execution mode is not supported for .NET projects.", FileName), null);
						return;
					}

					ProcessAsyncOperation asyncOp = context.ExecutionHandler.Execute (executionCommand, console);
					var stopper = monitor.CancellationToken.Register (asyncOp.Cancel);

					await asyncOp.Task;

					stopper.Dispose ();

					monitor.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}", asyncOp.ExitCode));
				} finally {
					console.Dispose ();
				}
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Cannot execute \"{0}\"", FileName), ex);
				monitor.ReportError (GettextCatalog.GetString ("Cannot execute \"{0}\"", FileName), ex);
			}
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
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

		internal protected override Task OnSave (ProgressMonitor monitor)
		{
			// Compiled assemblies can't be saved
			return Task.FromResult (0);
		}
	}
	
	public class CompiledAssemblyExtension: WorkspaceObjectReader
	{
		public override bool CanRead (FilePath file, Type expectedType)
		{
			return expectedType.IsAssignableFrom (typeof(SolutionItem)) && (file.Extension.ToLower() == ".exe" || file.Extension.ToLower() ==  ".dll");
		}

		public override Task<SolutionItem> LoadSolutionItem (ProgressMonitor monitor, SolutionLoadContext ctx, string fileName, MSBuildFileFormat expectedFormat, string typeGuid, string itemGuid)
		{
			return Task<SolutionItem>.Factory.StartNew (delegate {
				CompiledAssemblyProject p = new CompiledAssemblyProject ();
				p.LoadFrom (fileName);
				return p;
			});
		}
	}
}

