using System;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.AspNetCore.Dialogs;
using MonoDevelop.DotNetCore;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.AspNetCore.Commands
{
	enum Commands
	{
		PublishToFolder
	}

	sealed class PublishCommandHandler : CommandHandler
	{
		PublishToFolderDialog dialog;
		OutputProgressMonitor consoleMonitor;

		protected override void Update (CommandArrayInfo info)
		{
			base.Update (info);

			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;

			if (!ProjectSupportsAzurePublishing (project)) {
				return;
			}

			info.Add (GettextCatalog.GetString ("Publish to Folder\u2026"), new PublishCommandItem (project, null));

			var profiles = project.GetPublishProfiles ();
			if (profiles.Length > 0) {
				info.AddSeparator ();
			}

			foreach (var profile in profiles) {
				info.Add (GettextCatalog.GetString ("Publish to {0} - Folder", profile.Name), new PublishCommandItem (project, profile));
			}
		}

		protected override async void Run (object dataItem)
		{
			if (!(dataItem is PublishCommandItem publishCommandItem))
				return;

			try {
				if (publishCommandItem.Profile != null) {
					await Publish (publishCommandItem);
				} else {
					dialog = new PublishToFolderDialog (publishCommandItem);
					dialog.PublishToFolderRequested += Dialog_PublishToFolderRequested;
					if (dialog.Run () == Xwt.Command.Close) {
						CloseDialog ();
					}
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
			var config = string.Empty;
			if (item.Project.GetActiveConfiguration () != null)
				config = $"--configuration {item.Project.GetActiveConfiguration ()}";

			return $"publish {config} --output {item.Profile.FileName}";
		}

		public async Task Publish (PublishCommandItem item)
		{
			using (var progressMonitor = CreateProgressMonitor ()) {
				try {
					if (!DotNetCoreRuntime.IsInstalled) {
						progressMonitor.ReportError (GettextCatalog.GetString (".NET Core Runtime not installed"));
						return;
					}
					var dotnetPath = DotNetCoreRuntime.FileName;
					progressMonitor.BeginTask ("dotnet publish", 1);
					var process = Runtime.ProcessService.StartConsoleProcess (
						dotnetPath,
						BuildArgs (item),
						item.Project.BaseDirectory,
						consoleMonitor.Console);

					using (progressMonitor.CancellationToken.Register (process.Cancel)) {
						await process.Task;
					}

					if (!progressMonitor.CancellationToken.IsCancellationRequested) {
						if (process.ExitCode != 0) {
							progressMonitor.ReportError (GettextCatalog.GetString ("dotnet publish returned: {0}", process.ExitCode));
							LoggingService.LogError ($"Unknown exit code returned from 'dotnet publish --output {item.Profile.FileName}': {process.ExitCode}");
						} else {
							DesktopService.OpenFolder (item.Profile.FileName);
							if (!item.IsReentrant)
								item.Project.AddPublishProfiles (item.Profile);
						}
					}
					CloseDialog ();
					progressMonitor.EndTask ();
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					progressMonitor.Log.WriteLine (ex.Message);
					LoggingService.LogError ("Failed to exexute dotnet publish.", ex);
				}
			}
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			consoleMonitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"dotnet publish",
				GettextCatalog.GetString ("Publishing to folder..."),
				Stock.Console,
				bringToFront: false,
				allowMonitorReuse: true);

			var pad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor (consoleMonitor);

			var mon = new AggregatedProgressMonitor (consoleMonitor);
			mon.AddFollowerMonitor (IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Publishing to folder..."),
				Stock.CopyIcon,
				showErrorDialogs: true,
				showTaskTitle: false,
				lockGui: false,
				statusSourcePad: pad,
				showCancelButton: true));
			return mon; 
		}

		static bool ProjectSupportsAzurePublishing (DotNetProject project)
		{
			return project != null && project.GetProjectCapabilities ().Any (i => i == "Web" || i == "AzureFunctions");
		}
	}
}