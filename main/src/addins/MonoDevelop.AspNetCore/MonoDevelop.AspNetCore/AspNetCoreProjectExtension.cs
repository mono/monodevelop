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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	[ExportProjectModelExtension]
	class AspNetCoreProjectExtension : DotNetCoreProjectExtension
	{
		AspNetCoreRunConfiguration aspNetCoreRunConf;
		public const string TypeScriptCompile = "TypeScriptCompile";

		protected override ProjectRunConfiguration OnCreateRunConfiguration (string name)
		{
			if (aspNetCoreRunConf == null) {
				aspNetCoreRunConf = new AspNetCoreRunConfiguration (name, this.Project);
			}
			return aspNetCoreRunConf;
		}

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return DotNetCoreSupportsObject (item) && IsWebProject ((DotNetProject)item);
		}

		protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			if (Project.TargetFramework.IsNetCoreApp ()) {
				var result = CreateAspNetCoreExecutionCommand (configSel, configuration, runConfiguration);
				if (result != null)
					return result;
			}
			return base.OnCreateExecutionCommand (configSel, configuration, runConfiguration);
		}

		private ExecutionCommand CreateAspNetCoreExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			FilePath outputFileName;
			var aspnetCoreRunConfiguration = runConfiguration as AspNetCoreRunConfiguration;
			if (aspnetCoreRunConfiguration == null)
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

		protected override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (DotNetCoreRuntime.IsInstalled) {
				return CheckCertificateThenExecute (monitor, context, configuration, runConfiguration);
			}
			return base.OnExecute (monitor, context, configuration, runConfiguration);
		}

		async Task CheckCertificateThenExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (AspNetCoreCertificateManager.CheckDevelopmentCertificateIsTrusted (Project, runConfiguration)) {
				await AspNetCoreCertificateManager.TrustDevelopmentCertificate (monitor);
			}
			await base.OnExecute (monitor, context, configuration, runConfiguration);
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
		}

		public override void Dispose ()
		{
			base.Dispose ();
			FileService.FileChanged -= FileService_FileChanged;
		}

		void FileService_FileChanged (object sender, FileEventArgs e)
		{
			var launchSettingsPath = aspNetCoreRunConf?.launchProfileProvider?.launchSettingsJsonPath;
			var launchSettings = e.FirstOrDefault (x => x.FileName == launchSettingsPath && !x.FileName.IsDirectory);
			if (launchSettings == null)
				return;

			aspNetCoreRunConf.RefreshLaunchSettings ();
		}
	}
}
