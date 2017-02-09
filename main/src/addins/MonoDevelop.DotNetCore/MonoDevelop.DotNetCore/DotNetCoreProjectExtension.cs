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
using MonoDevelop.PackageManagement.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Ide;

namespace MonoDevelop.DotNetCore
{
	[ExportProjectModelExtension]
	public class DotNetCoreProjectExtension: DotNetProjectExtension
	{
		const string ShownDotNetCoreSdkInstalledExtendedPropertyName = "DotNetCore.ShownDotNetCoreSdkNotInstalledDialog";

		DotNetCoreMSBuildProject dotNetCoreMSBuildProject = new DotNetCoreMSBuildProject ();
		DotNetCoreSdkPaths sdkPaths;

		public DotNetCoreProjectExtension ()
		{
			DotNetCoreProjectReloadMonitor.Initialize ();
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

		protected override void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			dotNetCoreMSBuildProject.ReadProjectHeader (msproject);
			base.OnReadProjectHeader (monitor, msproject);
		}

		protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			dotNetCoreMSBuildProject.AddKnownItemAttributes (Project.MSBuildProject);

			base.OnReadProject (monitor, msproject);

			dotNetCoreMSBuildProject.ReadProject (msproject);

			if (!dotNetCoreMSBuildProject.IsOutputTypeDefined)
				Project.CompileTarget = dotNetCoreMSBuildProject.DefaultCompileTarget;

			Project.UseAdvancedGlobSupport = true;
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			dotNetCoreMSBuildProject.WriteProject (msproject);
		}

		protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			return CreateDotNetCoreExecutionCommand (configSel, configuration, runConfiguration);
		}

		DotNetCoreExecutionCommand CreateDotNetCoreExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			FilePath outputFileName = GetOutputFileName (configuration);
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
			string targetFramework = dotNetCoreMSBuildProject.TargetFrameworks.FirstOrDefault ();
			FilePath outputDirectory = configuration.OutputDirectory;

			if (outputDirectory.IsAbsolute)
				return outputDirectory;
			else if (outputDirectory == "./")
				outputDirectory = Path.Combine ("bin", configuration.Name);

			return Project.BaseDirectory.Combine (outputDirectory.ToString (), targetFramework);
		}

		protected override FilePath OnGetOutputFileName (ConfigurationSelector configuration)
		{
			var dotNetConfiguration = configuration.GetConfiguration (Project) as DotNetProjectConfiguration;
			if (dotNetConfiguration != null)
				return GetOutputFileName (dotNetConfiguration);

			return FilePath.Null;
		}

		FilePath GetOutputFileName (DotNetProjectConfiguration configuration)
		{
			FilePath outputDirectory = GetOutputDirectory (configuration);
			string assemblyName = Project.Name;
			return outputDirectory.Combine (configuration.OutputAssembly + ".dll");
		}

		protected override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (!IdeApp.Preferences.BuildBeforeExecuting && !IsDotNetCoreInstalled ()) {
				return ShowCannotExecuteDotNetCoreApplicationDialog ();
			}

			return base.OnExecute (monitor, context, configuration, runConfiguration);
		}

		bool IsDotNetCoreInstalled ()
		{
			var dotNetCorePath = new DotNetCorePath ();
			return !dotNetCorePath.IsMissing;
		}

		Task ShowCannotExecuteDotNetCoreApplicationDialog ()
		{
			return Runtime.RunInMainThread (() => {
				using (var dialog = new DotNetCoreNotInstalledDialog ()) {
					dialog.Message = GettextCatalog.GetString (".NET Core is required to run this application.");
					dialog.Show ();
				}
			});
		}

		Task ShowDotNetCoreNotInstalledDialog ()
		{
			return Runtime.RunInMainThread (() => {
				if (ShownDotNetCoreSdkNotInstalledDialogForSolution ())
					return;

				Project.ParentSolution.ExtendedProperties [ShownDotNetCoreSdkInstalledExtendedPropertyName] = "true";

				using (var dialog = new DotNetCoreNotInstalledDialog ()) {
					dialog.Show ();
				}
			});
		}

		bool ShownDotNetCoreSdkNotInstalledDialogForSolution ()
		{
			return Project.ParentSolution.ExtendedProperties.Contains (ShownDotNetCoreSdkInstalledExtendedPropertyName);
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

			if (!IdeApp.IsInitialized)
				return;

			if (HasSdk && !sdkPaths.Exist) {
				ShowDotNetCoreNotInstalledDialog ();
			}
		}

		public override void Dispose ()
		{
			Project.Modified -= OnProjectModified;
			base.Dispose ();
		}

		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (ProjectNeedsRestore ()) {
				return Task.FromResult (CreateNuGetRestoreRequiredBuildResult ());
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
			if (Project.NuGetAssetsFileExists () &&
				(HasSdk || Project.DotNetCoreNuGetMSBuildFilesExist ())) {
				return false;
			}

			return true;
		}

		BuildResult CreateNuGetRestoreRequiredBuildResult ()
		{
			var result = new BuildResult ();
			result.SourceTarget = Project;
			result.AddError (GettextCatalog.GetString ("NuGet packages need to be restored before building. NuGet MSBuild targets are missing and are needed for building. The NuGet MSBuild targets are generated when the NuGet packages are restored."));
			return result;
		}

		protected override void OnBeginLoad ()
		{
			dotNetCoreMSBuildProject.Sdk = DotNetCoreProjectReader.GetDotNetCoreSdk (Project.FileName);
			base.OnBeginLoad ();
		}

		public bool HasSdk {
			get { return dotNetCoreMSBuildProject.HasSdk; }
		}

		protected override void OnPrepareForEvaluation (MSBuildProject project)
		{
			base.OnPrepareForEvaluation (project);

			if (!HasSdk)
				return;

			sdkPaths = new DotNetCoreSdkPaths ();
			sdkPaths.FindSdkPaths (dotNetCoreMSBuildProject.Sdk);
			if (!sdkPaths.Exist)
				return;

			if (dotNetCoreMSBuildProject.AddInternalSdkImports (project, sdkPaths)) {
				project.Evaluate ();
				dotNetCoreMSBuildProject.ReadDefaultCompileTarget (project);
			}
		}

		/// <summary>
		/// HACK: Remove any C# files found in the intermediate obj directory. This avoids
		/// a type system error if a file in the obj directory is modified but the type
		/// system does not have that file in the workspace. This can happen if the file
		/// was not filtered out initially and added to the project by the wildcard import
		/// and then later on after a re-evaluation of the project is filtered out from the
		/// source files returned by Project.OnGetSourceFiles.
		/// </summary>
		protected override async Task<ProjectFile[]> OnGetSourceFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var sourceFiles = await base.OnGetSourceFiles (monitor, configuration);

			return RemoveFilesFromIntermediateDirectory (sourceFiles);
		}

		ProjectFile[] RemoveFilesFromIntermediateDirectory (ProjectFile[] files)
		{
			var filteredFiles = new List<ProjectFile> ();
			FilePath intermediateOutputPath = Project.BaseIntermediateOutputPath;

			foreach (var file in files) {
				if (!file.FilePath.IsChildPathOf (intermediateOutputPath)) {
					filteredFiles.Add (file);
				}
			}

			return filteredFiles.ToArray ();
		}

		protected override void OnSetFormat (MSBuildFileFormat format)
		{
			// Do not call base class since the solution's FileFormat will be used which is
			// VS 2012 and this will set the ToolsVersion to "4.0" which we are preventing.
			// Setting the ToolsVersion to "4.0" can cause the MSBuild tasks such as
			// ResolveAssemblyReferences to fail for .NET Core projects when the project
			// xml is generated in memory for the project builder at the same time as the
			// project file is being saved.
		}

		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceAddedToProject (e);

			if (!Project.Loading)
				RestoreNuGetPackages ();
		}

		protected override void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceRemovedFromProject (e);

			if (!Project.Loading)
				RestoreNuGetPackages ();
		}

		void RestoreNuGetPackages ()
		{
			Runtime.AssertMainThread ();
			RestorePackagesInProjectHandler.Run (Project);
		}
	}
}
