//
// AspNetCoreProjectExtension.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	[ExportProjectModelExtension]
	class AspNetCoreProjectExtension : DotNetCoreProjectExtension
	{
		public const string TypeScriptCompile = "TypeScriptCompile";

		bool updating;
		Dictionary<string, AspNetCoreRunConfiguration> aspNetCoreRunConfs = new Dictionary<string, AspNetCoreRunConfiguration> ();
		LaunchProfileProvider launchProfileProvider;

		protected override ProjectRunConfiguration OnCreateRunConfiguration (string name)
		{
			InitLaunchSettingsProvider ();

			if (aspNetCoreRunConfs.TryGetValue (name, out var aspNetCoreRunConfiguration))
				return aspNetCoreRunConfiguration;

			var profile = new LaunchProfileData ();

			var key = name != "Default" ? name : this.Project.DefaultNamespace;

			if (!launchProfileProvider.Profiles.TryGetValue (key, out var _)) {
				profile = launchProfileProvider.AddNewProfile (key);
				launchProfileProvider.Profiles [key] = profile;
			} else {
				profile = launchProfileProvider.Profiles [key];
			}

			var aspnetconf = new AspNetCoreRunConfiguration (name, profile) {
				LaunchProfileProvider = launchProfileProvider
			};
			if (aspNetCoreRunConfs.TryGetValue (name, out var existingConf)) {
				// This can be called a few times with the same config at load time,
				// so make sure we clean up the previous version
				existingConf.SaveRequested -= Aspnetconf_Save;
			}
			aspnetconf.SaveRequested += Aspnetconf_Save;
			aspNetCoreRunConfs [name] = aspnetconf;
			return aspnetconf;
		}

		void Aspnetconf_Save (object sender, EventArgs e)
		{
			if (sender is AspNetCoreRunConfiguration config && config.IsDirty)
				launchProfileProvider.SaveLaunchSettings ();
		}

		void InitLaunchSettingsProvider ()
		{
			if (launchProfileProvider == null) {
				launchProfileProvider = new LaunchProfileProvider (this.Project);
			}
		}

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return DotNetCoreSupportsObject (item) && SupportsLaunchSettings ((DotNetProject)item);
		}

		protected override bool IsSupportedFramework (TargetFrameworkMoniker framework)
		{
			return framework.IsNetCoreApp ();
		}

		protected override ExecutionCommand OnCreateExecutionCommand (
			ConfigurationSelector configSel,
			DotNetProjectConfiguration configuration,
			TargetFrameworkMoniker framework,
			ProjectRunConfiguration runConfiguration)
		{
			var result = CreateAspNetCoreExecutionCommand (configSel, configuration, runConfiguration);
			if (result != null)
				return result;

			return base.OnCreateExecutionCommand (configSel, configuration, framework, runConfiguration);
		}

		private ExecutionCommand CreateAspNetCoreExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			FilePath outputFileName;
			if (!(runConfiguration is AspNetCoreRunConfiguration aspnetCoreRunConfiguration))
				return null;
			if (aspnetCoreRunConfiguration.StartAction == AssemblyRunConfiguration.StartActions.Program)
				outputFileName = aspnetCoreRunConfiguration.StartProgram;
			else
				outputFileName = GetOutputFileName (configuration);

			var applicationUrl = aspnetCoreRunConfiguration.CurrentProfile.TryGetApplicationUrl ();

			return new AspNetCoreExecutionCommand (
				string.IsNullOrWhiteSpace (aspnetCoreRunConfiguration.StartWorkingDirectory) ? Project.BaseDirectory : aspnetCoreRunConfiguration.StartWorkingDirectory,
				outputFileName,
				aspnetCoreRunConfiguration.StartArguments
			) {
				EnvironmentVariables = aspnetCoreRunConfiguration.EnvironmentVariables,
				PauseConsoleOutput = aspnetCoreRunConfiguration.PauseConsoleOutput,
				ExternalConsole = aspnetCoreRunConfiguration.ExternalConsole,
				LaunchBrowser = aspnetCoreRunConfiguration.CurrentProfile.LaunchBrowser ?? false,
				LaunchURL = aspnetCoreRunConfiguration.CurrentProfile.LaunchUrl,
				ApplicationURL = applicationUrl.GetFirstApplicationUrl (),
				ApplicationURLs = applicationUrl,
				PipeTransport = aspnetCoreRunConfiguration.PipeTransport
			};
		}

		protected override Task OnExecute (
			ProgressMonitor monitor,
			ExecutionContext context,
			ConfigurationSelector configuration,
			SolutionItemRunConfiguration runConfiguration)
		{
			TargetFrameworkMoniker framework = Project.TargetFramework.Id;
			if (Project.HasMultipleTargetFrameworks) {
				var frameworkContext = context?.ExecutionTarget as AspNetCoreTargetFrameworkExecutionTarget;
				if (frameworkContext != null) {
					framework = frameworkContext.Framework;
					if (!(configuration is DotNetProjectFrameworkConfigurationSelector))
						configuration = new DotNetProjectFrameworkConfigurationSelector (configuration, frameworkContext.FrameworkShortName);
				}
			}

			if (IsSupportedFramework (framework))
				return OnExecute (monitor, context, configuration, framework, runConfiguration);

			return base.OnExecute (monitor, context, configuration, runConfiguration);
		}

		protected override Task OnExecute (
			ProgressMonitor monitor,
			ExecutionContext context,
			ConfigurationSelector configuration,
			TargetFrameworkMoniker framework,
			SolutionItemRunConfiguration runConfiguration)
		{
			if (DotNetCoreRuntime.IsInstalled) {
				return CheckCertificateThenExecute (monitor, context, configuration, framework, runConfiguration);
			}
			return base.OnExecute (monitor, context, configuration, framework, runConfiguration);
		}

		protected override IEnumerable<ExecutionTarget> OnGetExecutionTargets (OperationContext ctx, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfig)
		{
			if (IsWeb) {
				var result = new ExecutionTargetGroup (GettextCatalog.GetString ("Browser"), "MonoDevelop.AspNetCore.BrowserExecutionTargets");
				foreach (var browser in IdeServices.DesktopService.GetApplications ("https://localhost", Ide.Desktop.DesktopApplicationRole.Viewer)) {
					if (browser.IsDefault) {
						if (Project.HasMultipleTargetFrameworks) {
							result.InsertRange (0, GetMultipleTargetFrameworkExecutionTargets (browser));
						} else {
							result.Insert (0, new AspNetCoreExecutionTarget (browser));
						}
					} else {
						if (Project.HasMultipleTargetFrameworks) {
							result.AddRange (GetMultipleTargetFrameworkExecutionTargets (browser));
						} else {
							result.Add (new AspNetCoreExecutionTarget (browser));
						}
					}
				}

				return result.Count > 0
					? new ExecutionTarget [] { result }
					: base.OnGetExecutionTargets (ctx, configuration, runConfig);
			} else {
				return base.OnGetExecutionTargets (ctx, configuration, runConfig);
			}
		}

		IEnumerable<ExecutionTarget> GetMultipleTargetFrameworkExecutionTargets (DesktopApplication browser)
		{
			var targets = new List<ExecutionTarget> (Project.TargetFrameworkMonikers.Length);
			foreach (TargetFrameworkMoniker framework in Project.TargetFrameworkMonikers) {
				var target = new AspNetCoreTargetFrameworkExecutionTarget (browser, framework);
				targets.Add (target);
			}
			return targets;
		}

		async Task CheckCertificateThenExecute (
			ProgressMonitor monitor,
			ExecutionContext context,
			ConfigurationSelector configuration,
			TargetFrameworkMoniker framework,
			SolutionItemRunConfiguration runConfiguration)
		{
			if (AspNetCoreCertificateManager.CheckDevelopmentCertificateIsTrusted (Project, runConfiguration)) {
				await AspNetCoreCertificateManager.TrustDevelopmentCertificate (monitor);
			}
			await base.OnExecute (monitor, context, configuration, framework, runConfiguration);
		}

		protected override string OnGetDefaultBuildAction (string fileName)
		{
			var mimeTypeChain = IdeServices.DesktopService.GetMimeTypeInheritanceChainForFile (fileName);
			if (mimeTypeChain.Contains ("text/x-typescript") || mimeTypeChain.Contains ("application/typescript"))
				return TypeScriptCompile;

			return base.OnGetDefaultBuildAction (fileName);
		}

		protected override void OnItemReady ()
		{
			base.OnItemReady ();
			FileService.FileChanged += FileService_FileChanged;

			InitLaunchSettingsProvider ();
			updating = true;

			launchProfileProvider.LoadLaunchSettings ();
			launchProfileProvider.SyncRunConfigurations();

			updating = false;
		}

		public override void Dispose ()
		{
			base.Dispose ();
			FileService.FileChanged -= FileService_FileChanged;
			foreach (var conf in aspNetCoreRunConfs) {
				conf.Value.SaveRequested -= Aspnetconf_Save;
			}
			aspNetCoreRunConfs.Clear ();
			aspNetCoreRunConfs = null;
		}

		async void FileService_FileChanged (object sender, FileEventArgs e)
		{
			var launchSettingsPath = launchProfileProvider?.LaunchSettingsJsonPath;
			var launchSettings = e.FirstOrDefault (x => x.FileName == launchSettingsPath && !x.FileName.IsDirectory);
			if (launchSettings == null)
				return;

			updating = true;

			launchProfileProvider.LoadLaunchSettings ();
			launchProfileProvider.SyncRunConfigurations();

			await IdeApp.ProjectOperations.SaveAsync (Project);

			updating = false;
		}

		protected override void OnRemoveRunConfiguration (IEnumerable<SolutionItemRunConfiguration> items)
		{
			if (updating || launchProfileProvider == null)
				return;
			foreach (var item in items) {
				launchProfileProvider.Profiles.TryRemove (item.Name, out var _);
			}
			launchProfileProvider.SaveLaunchSettings ();
		}

		protected override ProjectFeatures OnGetSupportedFeatures ()
		{
			return base.OnGetSupportedFeatures () & ~ProjectFeatures.UserSpecificRunConfigurations;
		}
	}
}
