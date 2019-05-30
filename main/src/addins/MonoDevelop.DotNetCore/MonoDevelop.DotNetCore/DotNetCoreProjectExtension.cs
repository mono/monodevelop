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
	public class DotNetCoreProjectExtension: SdkProjectExtension
	{
		const string ShownDotNetCoreSdkInstalledExtendedPropertyName = "DotNetCore.ShownDotNetCoreSdkNotInstalledDialog";
		const string GlobalJsonPathExtendedPropertyName = "DotNetCore.GlobalJsonPath";

		DotNetCoreSdkPaths sdkPaths;

		public DotNetCoreProjectExtension ()
		{
			try {
				DotNetCoreSdk.EnsureInitialized ();
			} catch (Exception ex) {
				LoggingService.LogInternalError ("DotNetCoreProjectExtension sdk initialization failed", ex);
			}
		}

		void FileService_FileChanged (object sender, FileEventArgs e)
		{
			var globalJson = e.FirstOrDefault (x => x.FileName.FileName.IndexOf ("global.json", StringComparison.OrdinalIgnoreCase) == 0 && !x.FileName.IsDirectory);
			if (globalJson == null)
				return;

			// make sure the global.json file that has been changed is the one we got when loading the project
			if (Project.ParentSolution.ExtendedProperties [GlobalJsonPathExtendedPropertyName] is string globalJsonPath 
				&& globalJsonPath.IndexOf (globalJson.FileName, StringComparison.OrdinalIgnoreCase) == 0) {
				DetectSDK (restore: true);
			}
		}

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return DotNetCoreSupportsObject (item) && !IsWebProject ((DotNetProject)item);
		}

		protected bool DotNetCoreSupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && HasSupportedFramework ((DotNetProject)item);
		}

		/// <summary>
		/// Cannot check TargetFramework property since it may not be set.
		/// Currently support .NET Core and .NET Standard.
		/// </summary>
		bool HasSupportedFramework (DotNetProject project)
		{
			string framework = project.MSBuildProject.EvaluatedProperties.GetValue ("TargetFrameworkIdentifier");
			if (framework != null)
				return framework == ".NETCoreApp" || framework == ".NETStandard";

			return false;
		}

		protected override bool OnGetSupportsFramework (TargetFramework framework)
		{
			return framework.IsNetCoreApp () || framework.IsNetStandard ();
		}

		protected override bool OnGetCanReferenceProject (DotNetProject targetProject, out string reason)
		{
			if (base.OnGetCanReferenceProject (targetProject, out reason))
				return true;

			return CanReferenceProject (targetProject);
		}

		bool CanReferenceProject (DotNetProject targetProject)
		{
			if (targetProject.IsPortableLibrary)
				return true;

			if (!targetProject.TargetFramework.IsNetStandard ())
				return false;

			if (!Project.TargetFramework.IsNetCoreApp ())
				return false;

			return DotNetCoreFrameworkCompatibility.CanReferenceNetStandardProject (Project.TargetFramework.Id, targetProject);
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

			var workingDirectory =  Project.GetOutputFileName (configSel).ParentDirectory;

			return new DotNetCoreExecutionCommand (
				string.IsNullOrEmpty (dotnetCoreRunConfiguration?.StartWorkingDirectory) ? workingDirectory : dotnetCoreRunConfiguration.StartWorkingDirectory,
				outputFileName,
				dotnetCoreRunConfiguration?.StartArguments
			) {
				EnvironmentVariables = dotnetCoreRunConfiguration?.EnvironmentVariables,
				PauseConsoleOutput = dotnetCoreRunConfiguration?.PauseConsoleOutput ?? false,
				ExternalConsole = dotnetCoreRunConfiguration?.ExternalConsole ?? false,
				PipeTransport = dotnetCoreRunConfiguration?.PipeTransport
			};
		}

		FilePath GetOutputDirectory (DotNetProjectConfiguration configuration)
		{
			string targetFramework = TargetFrameworks.FirstOrDefault ();
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
					dialog.RequiredDotNetCoreVersion = DotNetCoreVersion.Parse (Project.TargetFramework.Id.Version);
					dialog.CurrentDotNetCorePath = sdkPaths.MSBuildSDKsPath;
					dialog.IsNetStandard = Project.TargetFramework.Id.IsNetStandard ();
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

		protected override void OnItemReady ()
		{
			base.OnItemReady ();
			FileService.FileChanged += FileService_FileChanged;

			if (!IdeApp.IsInitialized)
				return;

			if (HasSdk && !IsDotNetCoreSdkInstalled ()) {
				ShowDotNetCoreNotInstalledDialog (sdkPaths.IsUnsupportedSdkVersion);
			}

			if (Project.ParentSolution == null)
				return;

			if (Project.ParentSolution.ExtendedProperties.Contains (GlobalJsonPathExtendedPropertyName))
				return;

			//detect globaljson
			var globalJsonPath = sdkPaths.LookUpGlobalJson (Project.ParentSolution.BaseDirectory); 
			if (globalJsonPath == null)
				return;

			Project.ParentSolution.ExtendedProperties [GlobalJsonPathExtendedPropertyName] = globalJsonPath;
			DetectSDK ();
		}

		void DetectSDK (bool restore = false)
		{
			if (Project.ParentSolution.ExtendedProperties [GlobalJsonPathExtendedPropertyName] is string globalJsonPathProperty && File.Exists (globalJsonPathProperty)) {
				sdkPaths.GlobalJsonPath = globalJsonPathProperty;
			} else {
				sdkPaths.GlobalJsonPath = string.Empty;
			}

			sdkPaths.ResolveSDK (Project.ParentSolution.BaseDirectory);
			DotNetCoreSdk.Update (sdkPaths);
			if (restore && sdkPaths.Exist)
				ReevaluateAllOpenDotNetCoreProjects ();
		}

		void ReevaluateAllOpenDotNetCoreProjects ()
		{
			if (!IdeApp.Workspace.IsOpen)
				return;
				
			foreach (var project in IdeApp.Workspace.GetAllItems<DotNetProject> ()) {
				if (project.HasFlavor<DotNetCoreProjectExtension> ()) {
					RestorePackagesInProjectHandler.Run (project, restoreTransitiveProjectReferences: true, reevaluateBeforeRestore: true);
				}
			}
		}

		public override void Dispose ()
		{
			FileService.FileChanged -= FileService_FileChanged;

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
			if (!Project.TargetFramework.Id.IsNetStandardOrNetCoreApp ()) {
				return null;
			}

			if ((HasSdk && !IsDotNetCoreSdkInstalled ()) || (sdkPaths != null && sdkPaths.IsUnsupportedSdkVersion)) {
				return CreateDotNetCoreSdkRequiredBuildResult ();
			}
			return null;
		}

		BuildResult CreateBuildError (string message)
		{
			var result = new BuildResult ();
			result.SourceTarget = Project;
			result.AddError (message);
			return result;
		}

		BuildResult CreateDotNetCoreSdkRequiredBuildResult ()
		{
			return CreateBuildError (GetDotNetCoreSdkRequiredMessage ());
		}

		internal string GetDotNetCoreSdkRequiredMessage ()
		{
			return GetDotNetCoreSdkRequiredBuildErrorMessage (
				IsUnsupportedDotNetCoreSdkInstalled (),
				Project.TargetFramework);
		}

		string GetDotNetCoreSdkRequiredBuildErrorMessage (bool isUnsupportedVersion, TargetFramework targetFramework)
		{
			string message;

			if (isUnsupportedVersion) {
				message = DotNetCoreSdk.GetNotSupportedVersionMessage ();
			} else {
				message = DotNetCoreSdk.GetNotSupportedVersionMessage (targetFramework.Id.Version);
			}

			return message;
		}

		public bool HasSdk => Project.MSBuildProject.GetReferencedSDKs ().Length > 0;

		protected bool IsWebProject (DotNetProject project)
		{
			return (project.MSBuildProject.GetReferencedSDKs ().FirstOrDefault (x => x.IndexOf ("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) != -1) != null);
		}

		public bool IsWeb {
			get { return IsWebProject (Project);  }
		}

		protected override void OnPrepareForEvaluation (MSBuildProject project)
		{
			base.OnPrepareForEvaluation (project);

			if (!HasSdk)
				return;

			var referencedSdks = project.GetReferencedSDKs ();
			sdkPaths = DotNetCoreSdk.FindSdkPaths (referencedSdks);
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

		protected override ProjectRunConfiguration OnCreateRunConfiguration (string name)
		{
			return new DotNetCoreRunConfiguration (name, IsWeb);
		}
	}
}
