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
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.DotNetCore.GlobalTools;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class ScaffolderWizard : ScaffolderDialogController
	{
		static readonly ScaffolderArgs args = new ScaffolderArgs ();
		readonly DotNetProject project;
		readonly FilePath parentFolder;

		public ScaffolderWizard (DotNetProject project, FilePath parentFolder) : base ("Add New Scaffolded Item", StockIcons.Information, null, args)
		{
			this.DefaultPageSize = new Size (600, 500);

			var rightSideImage = new Xwt.ImageView (Image.FromResource ("aspnet-wizard-page.png"));
			var rightSideWidget = new FrameBox (rightSideImage);
			rightSideWidget.BackgroundColor = Styles.Wizard.PageBackgroundColor;
			this.RightSideWidget = new XwtControl (rightSideWidget);
			this.Completed += (sender, e) => Task.Run (() => OnCompletedAsync ());
			this.project = project;
			this.parentFolder = parentFolder;
			args.Project = project;
			args.ParentFolder = parentFolder;
		}

		const string toolName = "dotnet-aspnet-codegenerator";

		async Task OnCompletedAsync ()
		{
			using var progressMonitor = CreateProgressMonitor ();

			// Install the tool
			if (!DotNetCoreGlobalToolManager.IsInstalled (toolName)) {
				await DotNetCoreGlobalToolManager.Install (toolName, progressMonitor.CancellationToken);
			}

			// Run the tool
			var dotnet = DotNetCoreRuntime.FileName;
			var argBuilder = new ProcessArgumentBuilder ();
			argBuilder.Add ("aspnet-codegenerator");
			argBuilder.Add ("--project");
			argBuilder.AddQuoted (project.FileName);
			argBuilder.Add (args.Scaffolder.CommandLineName);

			foreach (var field in args.Scaffolder.Fields) {
				argBuilder.Add (field.CommandLineName);
				argBuilder.Add (field.SelectedValue);
			}

			argBuilder.Add ("--no-build"); //TODO: when do we need to build?
			argBuilder.Add ("-outDir");
			argBuilder.AddQuoted (parentFolder);

			foreach (var arg in args.Scaffolder.DefaultArgs) {
				argBuilder.Add (arg.ToString ());
			}

			var commandLineArgs = argBuilder.ToString ();

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
				await progressMonitor.Log.WriteLineAsync (ex.Message);
				LoggingService.LogError ($"Failed to run {dotnet} {commandLineArgs}", ex);
			}
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
