//
// Scaffolder.cs
//
// Author:
//       jasonimison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebTools.Scaffolding.Core;
using Microsoft.WebTools.Scaffolding.Core.Config;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.DotNetCore.GlobalTools;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class ScaffolderWizard : ScaffolderDialogController
	{
		readonly DotNetProject project;
		readonly FilePath parentFolder;
		readonly ScaffolderArgs args;

		public ScaffolderWizard (DotNetProject project, FilePath parentFolder, ScaffolderTemplateSelectPage selectPage, ScaffolderArgs args) : base (GettextCatalog.GetString ("Add New Scaffolding"), selectPage, args)
		{
			this.DefaultPageSize = new Size (500, 400);

			var rightSideImage = new Xwt.ImageView (Image.FromResource ("aspnet-wizard-page.png"));
			var rightSideWidget = new FrameBox (rightSideImage);
			rightSideWidget.BackgroundColor = Styles.Wizard.PageBackgroundColor;
			this.RightSideWidget = new XwtControl (rightSideWidget);
			this.project = project;
			this.parentFolder = parentFolder;
			this.args = args;
			args.Project = project;
			args.ParentFolder = parentFolder;

			this.Completed += (_, __) => Task.Run (() => OnCompletedAsync ());
			selectPage.ScaffolderSelected -= ScaffolderSelected;
			selectPage.ScaffolderSelected += ScaffolderSelected;
		}

		void ScaffolderSelected (object sender, EventArgs e)
		{
			Task.Run (async () =>
				 await Runtime.RunInMainThread (async () => {
					 LoggingService.LogInfo ($"{args.Scaffolder.Name} selected");
					 await Xwt.Toolkit.NativeEngine.Invoke (async () => {
						 if (!CurrentPageIsLast)
							 await this.GoNext (args.CancellationToken);
					 });
				 }));
		}

		const string toolName = "dotnet-aspnet-codegenerator";

		async Task<bool> InstallDotNetToolAsync (OutputProgressMonitor progressMonitor)
		{
			if (!DotNetCoreGlobalToolManager.IsInstalled (toolName)) {
				if (!await DotNetCoreGlobalToolManager.Install (toolName, progressMonitor.CancellationToken)) {
					progressMonitor.ReportError ($"Could not install {toolName} tool");
					return false;
				}
			}

			return true;
		}

		async Task<bool> InstallNuGetPackagesAsync (OutputProgressMonitor progressMonitor)
		{
			progressMonitor.Console.Debug (0, "", GettextCatalog.GetString ("Checking if needed NuGet packages are already installed...\n"));
			var refsToAdd = new List<PackageManagementPackageReference> ();
			var installedPackages = PackageManagementServices.ProjectOperations.GetInstalledPackages (project);

			var packagesToInstall = await GetPackagesToInstallAsync ();
			foreach (var dep in packagesToInstall) {
				if (installedPackages.FirstOrDefault (x => x.Id.Equals (dep.PackageId, StringComparison.Ordinal)) == null) {
					refsToAdd.Add (new PackageManagementPackageReference (dep.PackageId, dep.MaxVersion));
				}
			}

			if (refsToAdd.Count > 0) {
				await progressMonitor.Console.Log.WriteLineAsync ($"Adding needed NuGet packages ({string.Join (", ", refsToAdd.Select (x => x.Id))})");
				try {
					await PackageManagementServices.ProjectOperations.InstallPackagesAsync (project, refsToAdd, licensesAccepted: true)
						.ConfigureAwait (false);
				} catch (Exception ex) {
					progressMonitor.ReportError ($"Failed adding packages: {ex.Message}", ex);
					return false;
				}
			}

			return true;
		}

		async Task<IEnumerable<PackageDescription>> GetPackagesToInstallAsync ()
		{
			var scaffoldingConfig = await ScaffoldingConfig.LoadFromJsonAsync ();
			var frameworkVersion = project.TargetFramework.Id.Version;

			if (SupportPolicyVersion.TryCreateFromVersionString (frameworkVersion, out var policyVersion)) {
				if (scaffoldingConfig.TryGetPackagesForSupportPolicyVersion (policyVersion, out PackageDescription [] packageDescriptions)) {
					return packageDescriptions
						// We don't support Identity scaffolders yet
						.Where (p => !p.IsOptionalIdentityPackage);
				}
			}
			return Enumerable.Empty<PackageDescription> ();
        }

		async Task OnCompletedAsync ()
		{
			using var progressMonitor = CreateProgressMonitor ();

			// Pre-setup
			if (!await InstallDotNetToolAsync (progressMonitor) ||
				!await InstallNuGetPackagesAsync (progressMonitor)) {
				return;
			}

			// Build the project to make sure the just added NuGet's get all the needed bits
			// for the next step. If the project is already built, this is a no-op
			progressMonitor.Console.Debug (0, "", "Building project...\n");
			var buildResult = await Runtime.RunInMainThread (() => IdeApp.ProjectOperations.Build (project).Task);
			if (buildResult.Failed) {
				return;
			}

			// Run the tool
			var dotnet = DotNetCoreRuntime.FileName;
			var commandLineArgs = GetArguments (args);

			var msg = $"Running {dotnet} {commandLineArgs}\n";
			progressMonitor.Console.Debug (0, "", msg);

			try {
				var process = Runtime.ProcessService.StartConsoleProcess (
					dotnet,
					commandLineArgs,
					parentFolder,
					progressMonitor.Console
				);

				await process.Task;
			} catch (Exception ex) {
				progressMonitor.ReportError (ex.Message, ex);
				LoggingService.LogError ($"Failed to run {dotnet} {commandLineArgs}", ex);
			}
		}

		internal string GetArguments (ScaffolderArgs args)
		{
			var argBuilder = new ProcessArgumentBuilder ();
			argBuilder.Add ("aspnet-codegenerator");
			argBuilder.Add ("--project");
			argBuilder.AddQuoted (project.FileName);
			argBuilder.Add (args.Scaffolder.CommandLineName);

			foreach (var field in args.Scaffolder.Fields) {
				argBuilder.Add (field.CommandLineName);
				argBuilder.Add (field.SelectedValue);
			}

			argBuilder.Add ("--no-build");
			argBuilder.Add ("-outDir");
			argBuilder.AddQuoted (parentFolder);

			foreach (var arg in args.Scaffolder.DefaultArgs) {
				argBuilder.Add (arg.ToString ());
			}

			return argBuilder.ToString ();
		}

		static OutputProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"AspNetCoreScaffolder",
				GettextCatalog.GetString ("ASP.NET Core Scaffolder"),
				Stock.Console,
				true,
				true);
		}
	}
}
