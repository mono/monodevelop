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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
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
			return DotNetCoreSupportsObject(item) && !IsWebProject ((DotNetProject)item);
		}

		protected bool DotNetCoreSupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && IsSdkProject ((DotNetProject)item);
		}

		protected override void Initialize ()
		{
			RequiresMicrosoftBuild = true;
			base.Initialize ();
		}

		protected override bool OnGetSupportsFramework (TargetFramework framework)
		{
			if (framework.IsNetCoreApp () ||
				framework.IsNetStandard ())
				return true;
			return base.OnGetSupportsFramework (framework);
		}

		/// <summary>
		/// Currently this project extension is enabled for all SDK style projects and
		/// not just for .NET Core and .NET Standard projects. SDK project support
		/// should be separated out from this extension so it can be enabled only for
		/// .NET Core and .NET Standard projects.
		/// </summary>
		bool IsSdkProject (DotNetProject project)
		{
			return project.MSBuildProject.Sdk != null;
		}

		protected override bool OnGetCanReferenceProject (DotNetProject targetProject, out string reason)
		{
			if (base.OnGetCanReferenceProject (targetProject, out reason))
				return true;

			return CanReferenceProject (targetProject);
		}

		bool CanReferenceProject (DotNetProject targetProject)
		{
			if (!targetProject.TargetFramework.IsNetStandard ())
				return false;

			if (!Project.TargetFramework.IsNetCoreApp ())
				return false;

			return DotNetCoreFrameworkCompatibility.CanReferenceNetStandardProject (Project.TargetFramework.Id, targetProject);
		}

		protected override void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			// Do not read the project header when re-evaluating to prevent the
			// ToolsVersion that was initially read from the project being changed.
			if (!Project.IsReevaluating)
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

			dotNetCoreMSBuildProject.WriteProject (msproject, Project.TargetFramework.Id);
		}

		protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			if (Project.TargetFramework.IsNetCoreApp ()) {
				return CreateDotNetCoreExecutionCommand (configSel, configuration, runConfiguration);
			}
			return base.OnCreateExecutionCommand (configSel, configuration, runConfiguration);
		}

		DotNetCoreExecutionCommand CreateDotNetCoreExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			FilePath outputFileName;
			var dotnetCoreRunConfiguration = runConfiguration as DotNetCoreRunConfiguration;
			if (dotnetCoreRunConfiguration?.StartAction == AssemblyRunConfiguration.StartActions.Program)
				outputFileName = dotnetCoreRunConfiguration.StartProgram;
			else
				outputFileName = GetOutputFileName (configuration);

			return new DotNetCoreExecutionCommand (
				string.IsNullOrEmpty (dotnetCoreRunConfiguration?.StartWorkingDirectory) ? Project.BaseDirectory : dotnetCoreRunConfiguration.StartWorkingDirectory,
				outputFileName,
				dotnetCoreRunConfiguration?.StartArguments
			) {
				EnvironmentVariables = dotnetCoreRunConfiguration?.EnvironmentVariables,
				PauseConsoleOutput = dotnetCoreRunConfiguration?.PauseConsoleOutput ?? false,
				ExternalConsole = dotnetCoreRunConfiguration?.ExternalConsole ?? false,
#pragma warning disable CS0618 // Type or member is obsolete
				LaunchBrowser = dotnetCoreRunConfiguration?.LaunchBrowser ?? false,
				LaunchURL = dotnetCoreRunConfiguration?.LaunchUrl,
				ApplicationURL = dotnetCoreRunConfiguration?.ApplicationURL,
#pragma warning restore CS0618 // Type or member is obsolete
				PipeTransport = dotnetCoreRunConfiguration?.PipeTransport
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

		protected FilePath GetOutputFileName (DotNetProjectConfiguration configuration)
		{
			FilePath outputDirectory = GetOutputDirectory (configuration);
			string assemblyName = Project.Name;
			return outputDirectory.Combine (configuration.OutputAssembly + ".dll");
		}

		protected override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (Project.TargetFramework.IsNetCoreApp () && DotNetCoreRuntime.IsMissing) {
				return ShowCannotExecuteDotNetCoreApplicationDialog ();
			}

			return base.OnExecute (monitor, context, configuration, runConfiguration);
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

		Task ShowDotNetCoreNotInstalledDialog (bool unsupportedSdkVersion)
		{
			return Runtime.RunInMainThread (() => {
				if (ShownDotNetCoreSdkNotInstalledDialogForSolution ())
					return;

				Project.ParentSolution.ExtendedProperties [ShownDotNetCoreSdkInstalledExtendedPropertyName] = "true";

				using (var dialog = new DotNetCoreNotInstalledDialog ()) {
					dialog.IsUnsupportedVersion = unsupportedSdkVersion;
					dialog.RequiresDotNetCore20 = Project.TargetFramework.IsNetStandard20OrNetCore20 ();
					dialog.Show ();
				}
			});
		}

		bool ShownDotNetCoreSdkNotInstalledDialogForSolution ()
		{
			return Project.ParentSolution.ExtendedProperties.Contains (ShownDotNetCoreSdkInstalledExtendedPropertyName);
		}

		protected override Task OnExecuteCommand (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, ExecutionCommand executionCommand)
		{
			if (Project.TargetFramework.IsNetCoreApp ()) {
				return OnExecuteDotNetCoreCommand (monitor, context, configuration, executionCommand);
			}
			return base.OnExecuteCommand (monitor, context, configuration, executionCommand);
		}

		async Task OnExecuteDotNetCoreCommand (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, ExecutionCommand executionCommand)
		{
			bool externalConsole = false;
			bool pauseConsole = false;

			var dotNetCoreExecutionCommand = executionCommand as DotNetCoreExecutionCommand;
			if (dotNetCoreExecutionCommand != null) {
				externalConsole = dotNetCoreExecutionCommand.ExternalConsole;
				pauseConsole = dotNetCoreExecutionCommand.PauseConsoleOutput;
			}

			OperationConsole console = externalConsole ? context.ExternalConsoleFactory.CreateConsole (!pauseConsole, monitor.CancellationToken)
				: context.ConsoleFactory.CreateConsole (OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (Project.Name), monitor.CancellationToken);

			using (console) {
				ProcessAsyncOperation asyncOp = context.ExecutionHandler.Execute (executionCommand, console);

				try {
					using (var stopper = monitor.CancellationToken.Register (asyncOp.Cancel))
						await asyncOp.Task;

					monitor.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}", asyncOp.ExitCode));
				} catch (OperationCanceledException) {
				}
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

			if (HasSdk && !IsDotNetCoreSdkInstalled ()) {
				ShowDotNetCoreNotInstalledDialog (sdkPaths.IsUnsupportedSdkVersion);
			}
		}

		public override void Dispose ()
		{
			Project.Modified -= OnProjectModified;
			base.Dispose ();
		}

		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			BuildResult result = CheckCanRunCleanOrBuild ();
			if (result != null) {
				return Task.FromResult (result);
			}
			return base.OnClean (monitor, configuration, operationContext);
		}

		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			BuildResult result = CheckCanRunCleanOrBuild ();
			if (result != null) {
				return Task.FromResult (result);
			}
			return base.OnBuild (monitor, configuration, operationContext);
		}

		BuildResult CheckCanRunCleanOrBuild ()
		{
			if (ProjectNeedsRestore ()) {
				return CreateNuGetRestoreRequiredBuildResult ();
			} else if (HasSdk && !IsDotNetCoreSdkInstalled ()) {
				return CreateDotNetCoreSdkRequiredBuildResult (sdkPaths.IsUnsupportedSdkVersion);
			}
			return null;
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
			return CreateBuildError (GettextCatalog.GetString ("NuGet packages need to be restored before building. NuGet MSBuild targets are missing and are needed for building. The NuGet MSBuild targets are generated when the NuGet packages are restored."));
		}

		BuildResult CreateBuildError (string message)
		{
			var result = new BuildResult ();
			result.SourceTarget = Project;
			result.AddError (message);
			return result;
		}

		BuildResult CreateDotNetCoreSdkRequiredBuildResult (bool isUnsupportedVersion)
		{
			bool requiresDotNetCoreSdk20 = Project.TargetFramework.IsNetStandard20OrNetCore20 ();
			return CreateBuildError (GetDotNetCoreSdkRequiredBuildErrorMessage (isUnsupportedVersion, requiresDotNetCoreSdk20));
		}

		internal string GetDotNetCoreSdkRequiredMessage ()
		{
			return GetDotNetCoreSdkRequiredBuildErrorMessage (
				IsUnsupportedDotNetCoreSdkInstalled (),
				Project.TargetFramework.IsNetStandard20OrNetCore20 ());
		}

		static string GetDotNetCoreSdkRequiredBuildErrorMessage (bool isUnsupportedVersion, bool requiresDotNetCoreSdk20)
		{
			if (isUnsupportedVersion)
				return GettextCatalog.GetString ("The .NET Core SDK installed is not supported. Please install a more recent version. {0}", DotNetCoreNotInstalledDialog.DotNetCoreDownloadUrl);
			else if (requiresDotNetCoreSdk20)
				return GettextCatalog.GetString (".NET Core 2.0 SDK is not installed. This is required to build .NET Core 2.0 projects. {0}", DotNetCoreNotInstalledDialog.DotNetCore20DownloadUrl);

			return GettextCatalog.GetString (".NET Core SDK is not installed. This is required to build .NET Core projects. {0}", DotNetCoreNotInstalledDialog.DotNetCoreDownloadUrl);
		}

		protected override void OnBeginLoad ()
		{
			dotNetCoreMSBuildProject.Sdk = Project.MSBuildProject.Sdk;
			base.OnBeginLoad ();
		}

		public bool HasSdk {
			get { return dotNetCoreMSBuildProject.HasSdk; }
		}

		protected bool IsWebProject (DotNetProject project)
		{
			return (project.MSBuildProject.Sdk?.IndexOf ("Microsoft.NET.Sdk.Web", System.StringComparison.OrdinalIgnoreCase) ?? -1) != -1;
		}

		public bool IsWeb {
			get { return IsWebProject (Project);  }
		}

		protected override void OnPrepareForEvaluation (MSBuildProject project)
		{
			base.OnPrepareForEvaluation (project);

			if (!HasSdk)
				return;

			sdkPaths = DotNetCoreSdk.FindSdkPaths (dotNetCoreMSBuildProject.Sdk);
		}

		protected override async Task<ProjectFile[]> OnGetSourceFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var sourceFiles = await base.OnGetSourceFiles (monitor, configuration);

			return AddMissingProjectFiles (sourceFiles);
		}

		ProjectFile[] AddMissingProjectFiles (ProjectFile[] files)
		{
			List<ProjectFile> missingFiles = null;
			foreach (ProjectFile existingFile in Project.Files.Where (file => file.BuildAction == BuildAction.Compile)) {
				if (!files.Any (file => file.FilePath == existingFile.FilePath)) {
					if (missingFiles == null)
						missingFiles = new List<ProjectFile> ();
					missingFiles.Add (existingFile);
				}
			}

			if (missingFiles == null)
				return files;

			missingFiles.AddRange (files);
			return missingFiles.ToArray ();
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

		/// <summary>
		/// Shared projects can trigger a reference change during re-evaluation so do not
		/// restore if the project is being re-evaluated. Otherwise this could cause the
		/// restore to be run repeatedly.
		/// </summary>
		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceAddedToProject (e);

			if (!IsLoadingOrReevaluating ())
				RestoreNuGetPackages ();
		}

		/// <summary>
		/// Shared projects can trigger a reference change during re-evaluation so do not
		/// restore if the project is being re-evaluated. Otherwise this could cause the
		/// restore to be run repeatedly.
		/// </summary>
		protected override void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceRemovedFromProject (e);

			if (!IsLoadingOrReevaluating ())
				RestoreNuGetPackages ();
		}

		bool IsLoadingOrReevaluating ()
		{
			return Project.Loading || Project.IsReevaluating;
		}

		void RestoreNuGetPackages ()
		{
			Runtime.AssertMainThread ();
			RestorePackagesInProjectHandler.Run (Project);
		}

		public bool IsDotNetCoreSdkInstalled ()
		{
			if (DotNetCoreSdk.IsInstalled || MSBuildSdks.Installed)
				return DotNetCoreSdk.IsSupported (Project.TargetFramework);
			return false;
		}

		public bool IsUnsupportedDotNetCoreSdkInstalled ()
		{
			if (sdkPaths != null)
				return sdkPaths.IsUnsupportedSdkVersion;
			return false;
		}

		bool IsFSharpSdkProject ()
		{
			return HasSdk && dotNetCoreMSBuildProject.Sdk.Contains ("FSharp");
		}

		/// <summary>
		/// Handle a new project being created and added to a new solution. In this case
		/// the NuGet packages should be restored. Need to avoid running a restore when
		/// a solution is being opened so check that project's parent solution is open in
		/// the IDE.
		/// </summary>
		protected override void OnBoundToSolution ()
		{
			base.OnBoundToSolution ();

			if (Project.Loading)
				return;

			if (IdeApp.ProjectOperations.CurrentSelectedSolution != Project.ParentSolution)
				return;

			if (ProjectNeedsRestore ())
				RestorePackagesInProjectHandler.Run (Project);
		}

		internal bool RestoreAfterSave { get; set; }

		protected override Task OnSave (ProgressMonitor monitor)
		{
			if (RestoreAfterSave) {
				RestoreAfterSave = false;
				if (!PackageManagementServices.BackgroundPackageActionRunner.IsRunning) {
					return OnRestoreAfterSave (monitor);
				}
			}
			return base.OnSave (monitor);
		}

		/// <summary>
		/// This is currently only called after the target framework of the project
		/// is modified. The project is saved, then re-evaluated and finally the NuGet
		/// packages are restored. The project re-evaluation is done so any target
		/// framework changes are available in the MSBuildProject's EvaluatedProperties
		/// otherwise the restore uses the wrong target framework.
		/// Also using a GLib.Timeout since triggering the reload straight away can
		/// cause the Save to fail with an index out of range exception when
		/// MSBuildPropertyGroup.Add is called when the DotNetProjectConfiguration
		/// is written.
		/// </summary>
		async Task OnRestoreAfterSave (ProgressMonitor monitor)
		{
			await base.OnSave (monitor);
			await Runtime.RunInMainThread (() => {
				GLib.Timeout.Add (0, () => {
					Project.NeedsReload = true;
					FileService.NotifyFileChanged (Project.FileName);
					return false;
				});
			});
		}

		protected override bool OnGetSupportsImportedItem (IMSBuildItemEvaluated buildItem)
		{
			if (!BuildAction.DotNetActions.Contains (buildItem.Name))
				return false;

			if (IsFSharpSdkProject ()) {
				// Ignore imported F# files. F# files are defined in the main project.
				// This prevents duplicate F# files when a new project is first created.
				if (buildItem.Include.EndsWith (".fs", StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (IsFromSharedProject (buildItem))
				return false;

			// HACK: Remove any imported items that are not in the EvaluatedItems
			// This may happen if a condition excludes the item. All items passed to the
			// OnGetSupportsImportedItem are from the EvaluatedItemsIgnoringCondition
			return Project.MSBuildProject.EvaluatedItems
				.Any (item => item.IsImported && item.Name == buildItem.Name && item.Include == buildItem.Include);
		}

		/// <summary>
		/// Checks that the project has the HasSharedItems property set to true and the SharedGUID
		/// property in its global property group. Otherwise it is not considered to be a shared project.
		/// </summary>
		bool IsFromSharedProject (IMSBuildItemEvaluated buildItem)
		{
			var globalGroup = buildItem?.SourceItem?.ParentProject?.GetGlobalPropertyGroup ();
			return globalGroup?.GetValue<bool> ("HasSharedItems") == true &&
				globalGroup?.HasProperty ("SharedGUID") == true;
		}

		protected override ProjectRunConfiguration OnCreateRunConfiguration (string name)
		{
			return new DotNetCoreRunConfiguration (name, IsWeb);
		}

		/// <summary>
		/// HACK: Hide certain files that are currently being added to the Solution window.
		/// </summary>
		protected override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			if (Project.Loading) {
				UpdateHiddenFiles (objs.OfType<ProjectFile> ());
			}
			base.OnItemsAdded (objs);
		}

		void UpdateHiddenFiles (IEnumerable<ProjectFile> files)
		{
			foreach (var file in files) {
				if (file.FilePath.ShouldBeHidden ())
					file.Flags = ProjectItemFlags.Hidden;
			}
		}

		protected override async Task OnReevaluateProject (ProgressMonitor monitor)
		{
			await base.OnReevaluateProject (monitor);
			UpdateHiddenFiles (Project.Files);
		}

		/// <summary>
		/// Returns all transitive references.
		/// </summary>
		protected override async Task<List<AssemblyReference>> OnGetReferences (
			ConfigurationSelector configuration,
			System.Threading.CancellationToken token)
		{
			var references = new List<AssemblyReference> ();

			var traversedProjects = new HashSet<string> ();
			traversedProjects.Add (Project.ItemId);

			await GetTransitiveAssemblyReferences (traversedProjects, references, configuration, true, token);

			return references;
		}

		/// <summary>
		/// Recursively gets all transitive project references for .NET Core projects
		/// and if includeNonProjectReferences is true also returns non project
		/// assembly references.
		/// 
		/// Calling base.OnGetReferences returns the directly referenced projects and
		/// also all transitive references which are not project references.
		/// 
		/// includeNonProjectReferences should be set to false when getting the
		/// assembly references for referenced projects since the assembly references
		/// from OnGetReferences already contains any transitive references which are
		/// not projects.
		/// </summary>
		async Task GetTransitiveAssemblyReferences (
			HashSet<string> traversedProjects,
			List<AssemblyReference> references,
			ConfigurationSelector configuration,
			bool includeNonProjectReferences,
			System.Threading.CancellationToken token)
		{
			foreach (var reference in await base.OnGetReferences (configuration, token)) {
				if (!reference.IsProjectReference) {
					if (includeNonProjectReferences) {
						references.Add (reference);
					}
					continue;
				}

				// Project references with ReferenceOutputAssembly false should be
				// added but there is no need to check any further since there will not
				// any transitive project references.
				if (!reference.ReferenceOutputAssembly) {
					references.Add (reference);
					continue;
				}

				var project = reference.GetReferencedItem (Project.ParentSolution) as DotNetProject;
				if (project == null)
					continue;

				if (traversedProjects.Contains (project.ItemId))
					continue;

				references.Add (reference);
				traversedProjects.Add (project.ItemId);

				var extension = project.AsFlavor<DotNetCoreProjectExtension> ();
				if (extension != null)
					await extension.GetTransitiveAssemblyReferences (traversedProjects, references, configuration, false, token);
			}
		}
	}
}
