﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.AspNetCore.Dialogs;
using MonoDevelop.DotNetCore;
using MonoDevelop.Core.ProgressMonitoring;
using Xwt;

namespace MonoDevelop.AspNetCore.Commands
{
	abstract class PublishToFolderBaseCommandHandler : CommandHandler
	{
		PublishToFolderDialog dialog;
		OutputProgressMonitor consoleMonitor;
		protected DotNetProject project;

		protected override async void Run (object dataItem)
		{
			try {

				if (!(dataItem is PublishCommandItem publishCommandItem)) {
					dialog = new PublishToFolderDialog (new PublishCommandItem (project, null));
					dialog.PublishToFolderRequested += Dialog_PublishToFolderRequested;
					var parent = Toolkit.CurrentEngine.WrapWindow (IdeApp.Workbench.RootWindow);
					var result = dialog.Run (parent);
					CloseDialog ();
				} else {
					await Publish (publishCommandItem);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to publish project", ex);
			} 
		}

		void CloseDialog ()
		{
			if (dialog == null)
				return;

			dialog.Close ();
			dialog.PublishToFolderRequested -= Dialog_PublishToFolderRequested;
			dialog.Dispose ();
		}

		async void Dialog_PublishToFolderRequested (object sender, PublishCommandItem item)
		{
			try {
				await Publish (item);
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to publish project", ex);
			}
		}

		string BuildArgs (PublishCommandItem item)
		{
			var args = string.Empty;
			if (item.Project.GetActiveConfiguration () != null)
				args = $"--configuration {item.Profile.LastUsedBuildConfiguration}";

			if (bool.Parse (item.Profile.SelfContained))
				args += $" --self-contained --runtime {item.Profile.RuntimeIdentifier}";

			return $"publish {args} --output {item.Profile.PublishUrl}";
		}

		public async Task Publish (PublishCommandItem item)
		{
			using (var progressMonitor = CreateProgressMonitor ()) {
				var counterMetadata = new AspNetCoreCounterMetadata ();

				using (Counters.PublishToFolder.BeginTiming (counterMetadata)) {
					try {

						IdeApp.Workbench.SaveAll ();
						if (!DotNetCoreRuntime.IsInstalled) {
							progressMonitor.ReportError (GettextCatalog.GetString (".NET Core Runtime not installed"));
							counterMetadata.SetFailure ();
							return;
						}

						progressMonitor.BeginTask ("dotnet publish", 1);

						int publishExitCode = await RunPublishCommand (
							BuildArgs (item),
							item.Project.BaseDirectory,
							consoleMonitor.Console,
							progressMonitor.CancellationToken);

						if (!progressMonitor.CancellationToken.IsCancellationRequested) {
							if (publishExitCode != 0) {
								counterMetadata.SetFailure ();
								progressMonitor.ReportError (GettextCatalog.GetString ("dotnet publish returned: {0}", publishExitCode));
								LoggingService.LogError ($"Unknown exit code returned from 'dotnet publish --output {item.Profile.PublishUrl}': {publishExitCode}");
							} else {
								counterMetadata.SetSuccess ();
								OpenFolder (item.Profile.PublishUrl);
								if (!item.IsReentrant)
									item.Project.CreatePublishProfileFile (item.Profile);
							}
						} else {
							counterMetadata.SetUserCancel ();
						}

						CloseDialog ();
						progressMonitor.EndTask ();
					} catch (OperationCanceledException) {
						throw;
					} catch (Exception ex) {
						counterMetadata.SetUserFault ();
						progressMonitor.Log.WriteLine (ex.Message);
						LoggingService.LogError ("Failed to exexute dotnet publish.", ex);
					}
				}
			}
		}

		internal async Task<int> RunPublishCommand (string args, string wokingDirectory, OperationConsole console, CancellationToken сancellationToken)
		{
			var process = Runtime.ProcessService.StartConsoleProcess (DotNetCoreRuntime.FileName, args, wokingDirectory, console);

			using (сancellationToken.Register (process.Cancel)) {
				await process.Task;
			}

			return process.ExitCode;
		}

		void OpenFolder (string path)
		{
			if (!Uri.TryCreate (path, UriKind.Absolute, out var pathUri)) {
				var binBaseUri = new Uri (Path.Combine (project.BaseDirectory));
				path = Path.Combine (binBaseUri.AbsolutePath, path);
			}

			if (Directory.Exists (path))
				IdeServices.DesktopService.OpenFolder (path);
			else
				LoggingService.LogError ("Trying to open {0} but it does not exists.", path);
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			consoleMonitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"MonoDevelop.Publish",
				GettextCatalog.GetString ("Publishing to folder..."),
				Stock.Console,
				bringToFront: false,
				allowMonitorReuse: true);

			var pad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor (consoleMonitor);

			var mon = new AggregatedProgressMonitor (consoleMonitor);
			mon.AddFollowerMonitor (IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Publishing to folder..."),
				Stock.StatusDownload,
				showErrorDialogs: true,
				showTaskTitle: false,
				lockGui: false,
				statusSourcePad: pad,
				showCancelButton: true));
			return mon;
		}

		protected static bool ProjectSupportsFolderPublishing (DotNetProject project)
		{
			// All .Net Core and .Net Standard library projects support folder publishing at this time. We
			// previously determined this support by checking for the project capabilities as follows:
			//
			//  - 'Web' Project Capability : Specified only for Asp .Net Core web templates - not Asp.Net
			//  - 'AzureFunctions' Project Capability : Specified only for C# Azure Function templates
			//  - 'FolderPublish' Project Capability : Specified in SDK previously for all .Net Core Project
			//    templates, but it was removed and added to design time targets which we do not use in
			//    VS4Mac project system
			//
			// The previous approach fully intersected with all .Net Core and Standard projects and hence
			// we can simplify our filter logic, but we should ensure we have a better deterministic plan
			// to determine this support going forward
			return project != null && project.TargetFramework.Id.IsNetStandardOrNetCoreApp ();
		}
	}
}