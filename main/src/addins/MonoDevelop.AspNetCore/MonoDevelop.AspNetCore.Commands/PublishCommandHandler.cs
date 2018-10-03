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

namespace MonoDevelop.AspNetCore.Commands
{
	public enum Commands
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
				info.Add (GettextCatalog.GetString ("Publish to ") + profile.Name + " - " + GettextCatalog.GetString ("Folder"), new PublishCommandItem (project, profile));
			}
		}

		protected override async void Run (object dataItem)
		{
			if (dataItem is PublishCommandItem publishCommandItem) {

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
		}

		void CloseDialog ()
		{
			if (dialog == null)
				return;

			dialog.Close ();
			dialog.PublishToFolderRequested -= Dialog_PublishToFolderRequested;
			dialog.Dispose ();
		}

		async void Dialog_PublishToFolderRequested (object sender, PublishCommandItem item) => await Publish (item);

		public async Task Publish (PublishCommandItem item)
		{
			var currentConfig = item.Project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration).Name;
			using (var progressMonitor = CreateProgressMonitor ()) {
				var dotnetPath = new DotNetCore.DotNetCorePath ().FileName;
				progressMonitor.BeginTask ("dotnet restore", 1);
				var proc = Runtime.ProcessService.StartConsoleProcess (
					dotnetPath,
					$"publish --configuration {currentConfig} --output {item.Profile.FileName}",
					item.Project.BaseDirectory, 
					consoleMonitor.Console);

				await proc.Task;

				if (proc.ExitCode != 0) {
					progressMonitor.ReportError ($"dotnet publish returned {proc.ExitCode}");
					LoggingService.LogError ($"Unknown exit code returned from 'dotnet publish --output {item.Profile.FileName}': {proc.ExitCode}");
				} else {
					DesktopService.OpenFolder (item.Profile.FileName);
					if (!item.IsReentrant)
						item.Project.AddPublishProfiles (item.Profile);
					if (dialog != null) {
						CloseDialog ();
					}
				}
				progressMonitor.EndTask ();
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
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Publishing to folder..."),
				Stock.CopyIcon,
				showErrorDialogs: true,
				showTaskTitle: false,
				lockGui: false,
				statusSourcePad: pad,
				showCancelButton: true);
		}

		static bool ProjectSupportsAzurePublishing (DotNetProject project)
		{
			return IsWebProject (project) || IsFunctionsProject (project);
		}

		internal static bool IsWebProject (DotNetProject project)
		{
			if (project == null)
				return false;

			return project.MSBuildProject.EvaluatedItems.Where (i => i.Include == "Web" && i.Name == "ProjectCapability").Any ();
		}

		internal static bool IsFunctionsProject (DotNetProject project)
		{
			if (project == null)
				return false;

			return project.MSBuildProject.EvaluatedItems.Where (i => i.Include == "AzureFunctions" && i.Name == "ProjectCapability").Any ();
		}
	}
}