//
// DotNetCoreProjectExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore
{
	[ExportProjectModelExtension]
	public class DotNetCoreProjectExtension: DotNetProjectExtension
	{
		List<string> targetFrameworks;
		bool outputTypeDefined;
		string toolsVersion;

		public DotNetCoreProjectExtension ()
		{
		}

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && IsDotNetCoreProject ((DotNetProject)item);
		}

		protected override void Initialize ()
		{
			RequiresMicrosoftBuild = true;
			base.Initialize ();
		}

		protected override bool OnGetSupportsFramework (Core.Assemblies.TargetFramework framework)
		{
			if (framework.Id.Identifier == ".NETCoreApp" ||
			    framework.Id.Identifier == ".NETStandard")
				return true;
			return base.OnGetSupportsFramework (framework);
		}

		bool IsDotNetCoreProject (DotNetProject project)
		{
			var properties = project.MSBuildProject.EvaluatedProperties;
			return properties.HasProperty ("TargetFramework") ||
				properties.HasProperty ("TargetFrameworks");
		}

		protected override bool OnGetCanReferenceProject (DotNetProject targetProject, out string reason)
		{
			if (base.OnGetCanReferenceProject (targetProject, out reason))
				return true;

			return CanReferenceProject (targetProject);
		}

		bool CanReferenceProject (DotNetProject targetProject)
		{
			if (targetProject.TargetFramework.Id.Identifier != ".NETStandard")
				return false;

			if (Project.TargetFramework.Id.Identifier != ".NETCoreApp")
				return false;

			return DotNetCoreFrameworkCompatibility.CanReferenceNetStandardProject (Project.TargetFramework.Id, targetProject);
		}

		protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnReadProject (monitor, msproject);

			toolsVersion = msproject.ToolsVersion;

			outputTypeDefined = IsOutputTypeDefined (msproject);
			if (!outputTypeDefined)
				Project.CompileTarget = CompileTarget.Library;

			targetFrameworks = GetTargetFrameworks (msproject).ToList ();

			Project.UseAdvancedGlobSupport = true;
		}

		static bool IsOutputTypeDefined (MSBuildProject msproject)
		{
			var globalPropertyGroup = msproject.GetGlobalPropertyGroup ();
			if (globalPropertyGroup != null)
				return globalPropertyGroup.HasProperty ("OutputType");

			return false;
		}

		static IEnumerable<string> GetTargetFrameworks (MSBuildProject msproject)
		{
			var properties = msproject.EvaluatedProperties;
			if (properties != null) {
				string targetFramework = properties.GetValue ("TargetFramework");
				if (targetFramework != null) {
					return new [] { targetFramework };
				}

				string targetFrameworks = properties.GetValue ("TargetFrameworks");
				if (targetFrameworks != null) {
					return targetFrameworks.Split (';');
				}
			}

			return new string[0];
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			var globalPropertyGroup = msproject.GetGlobalPropertyGroup ();
			globalPropertyGroup.RemoveProperty ("ProjectGuid");

			if (!outputTypeDefined) {
				globalPropertyGroup.RemoveProperty ("OutputType");
			}

			msproject.DefaultTargets = null;

			if (!string.IsNullOrEmpty (toolsVersion))
				msproject.ToolsVersion = toolsVersion;
		}

		protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			return CreateDotNetCoreExecutionCommand (configSel, configuration, runConfiguration);
		}

		DotNetCoreExecutionCommand CreateDotNetCoreExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			FilePath outputDirectory = GetOutputDirectory (configuration);
			FilePath outputFileName = outputDirectory.Combine (Project.Name + ".dll");

			var assemblyRunConfiguration = runConfiguration as AssemblyRunConfiguration;

			return new DotNetCoreExecutionCommand (
				assemblyRunConfiguration?.StartWorkingDirectory ?? Project.BaseDirectory,
				outputFileName,
				assemblyRunConfiguration?.StartArguments
			) {
				EnvironmentVariables = assemblyRunConfiguration?.EnvironmentVariables,
				PauseConsoleOutput = assemblyRunConfiguration?.PauseConsoleOutput ?? false,
				ExternalConsole = assemblyRunConfiguration?.ExternalConsole ?? false
			};
		}

		FilePath GetOutputDirectory (DotNetProjectConfiguration configuration)
		{
			string targetFramework = targetFrameworks.FirstOrDefault ();
			FilePath outputDirectory = configuration.OutputDirectory;

			if (outputDirectory.IsAbsolute)
				return outputDirectory;
			else if (outputDirectory == "./")
				outputDirectory = Path.Combine ("bin", configuration.Name);

			return Project.BaseDirectory.Combine (outputDirectory.ToString (), targetFramework);
		}

		protected override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (!IsDotNetCoreInstalled ()) {
				return ShowDotNetCoreNotInstalledDialog ();
			}

			return base.OnExecute (monitor, context, configuration, runConfiguration);
		}

		bool IsDotNetCoreInstalled ()
		{
			var dotNetCorePath = new DotNetCorePath ();
			return !dotNetCorePath.IsMissing;
		}

		Task ShowDotNetCoreNotInstalledDialog ()
		{
			return Runtime.RunInMainThread (() => {
				using (var dialog = new DotNetCoreNotInstalledDialog ()) {
					dialog.Show ();
				}
			});
		}

		protected override async Task OnExecuteCommand (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, ExecutionCommand executionCommand)
		{
			bool externalConsole = false;
			bool pauseConsole = false;

			var dotNetCoreExecutionCommand = executionCommand as DotNetCoreExecutionCommand;
			if (dotNetCoreExecutionCommand != null) {
				externalConsole = dotNetCoreExecutionCommand.ExternalConsole;
				pauseConsole = dotNetCoreExecutionCommand.PauseConsoleOutput;
			}

			OperationConsole console = externalConsole ? context.ExternalConsoleFactory.CreateConsole (!pauseConsole, monitor.CancellationToken)
				: context.ConsoleFactory.CreateConsole (monitor.CancellationToken);

			using (console) {
				ProcessAsyncOperation asyncOp = context.ExecutionHandler.Execute (executionCommand, console);

				using (var stopper = monitor.CancellationToken.Register (asyncOp.Cancel))
					await asyncOp.Task;

				monitor.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}", asyncOp.ExitCode));
			}
		}

		/// <summary>
		/// Cannot use SolutionItemExtension.OnModified. It does not seem to be called.
		/// </summary>
		protected void OnProjectModified (object sender, SolutionItemModifiedEventArgs args)
		{
			if (Project.Loading)
				return;

			var fileNameChange = args.LastOrDefault (arg => arg.Hint == "FileName");
			if (fileNameChange != null) {
				DotNetCoreProjectFileRenamedHandler.OnProjectFileRenamed (Project);
			}
		}

		protected override void OnItemReady ()
		{
			base.OnItemReady ();
			Project.Modified += OnProjectModified;
		}

		public override void Dispose ()
		{
			Project.Modified -= OnProjectModified;
			base.Dispose ();
		}

		/// <summary>
		/// Clean errors are not currently reported by the IDE so the error information is reported 
		/// directly to the progress monitor instead of just returning a BuildResult. The OnClean
		/// method is overridden and an error is reported since this is called when a Rebuild is run.
		/// Without the error being reported here the information about the NuGet restore being
		/// needed would not be shown when a Rebuild was run.
		/// </summary>
		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (ProjectNeedsRestore ()) {
				var result = CreateNuGetRestoreRequiredBuildResult ();
				monitor.Log.WriteLine (result.Errors[0].ErrorText);
				monitor.ReportError (GettextCatalog.GetString ("Clean failed: Packages not restored"));
				return Task.FromResult (result);
			}
			return base.OnClean (monitor, configuration, operationContext);
		}

		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (ProjectNeedsRestore ()) {
				return Task.FromResult (CreateNuGetRestoreRequiredBuildResult ());
			}
			return base.OnBuild (monitor, configuration, operationContext);
		}

		bool ProjectNeedsRestore ()
		{
			if (Project.NuGetAssetsFileExists () && Project.DotNetCoreNuGetMSBuildFilesExist ())
				return false;

			return true;
		}

		BuildResult CreateNuGetRestoreRequiredBuildResult ()
		{
			var result = new BuildResult ();
			result.SourceTarget = Project;
			result.AddError (GettextCatalog.GetString ("NuGet packages need to be restored before building. NuGet MSBuild targets are missing and are needed for building. The NuGet MSBuild targets are generated when the NuGet packages are restored."));
			return result;
		}
	}
}
