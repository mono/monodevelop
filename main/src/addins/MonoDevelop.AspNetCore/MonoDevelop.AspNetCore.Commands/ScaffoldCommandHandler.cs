using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore.Commands
{
	enum AspNetCoreCommands
	{ 
		Scaffold
    }

	class ScaffoldNodeExtension : NodeBuilderExtension
	{
		public override Type CommandHandlerType {
			get { return typeof (ScaffoldCommandHandler); }
		}

		public override bool CanBuildNode (Type dataType)
		{
			return true;
			//return typeof (ProjectFile).IsAssignableFrom (dataType);
		}
	}

	class ScaffoldCommandHandler : NodeCommandHandler
	{
		static OutputProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"AspNetCoreScaffolder",
				GettextCatalog.GetString ("ASP.NET Core Scaffolder"),
				Stock.Console,
				false,
				true);
		}

		[CommandHandler (AspNetCoreCommands.Scaffold)]
		public async void Scaffold ()
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (project == null)
				return;

			var dotnet = DotNetCoreRuntime.FileName;

			var args = $"aspnet-codegenerator --project {project.FileName} controller -name Generated";

			var folder = CurrentNode.GetParentDataItem (typeof (ProjectFolder), true) as ProjectFolder;
			string path = folder?.Path ?? project.BaseDirectory;

			using (var progressMonitor = CreateProgressMonitor ()) {
				try {
					var process = Runtime.ProcessService.StartConsoleProcess (
						dotnet,
						args,
						path,
						progressMonitor.Console
					);

					await process.Task;
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					progressMonitor.Log.WriteLine (ex.Message);
					LoggingService.LogError ($"Failed to run {dotnet} {args}", ex);
				}
			}
		}

		[CommandUpdateHandler (AspNetCoreCommands.Scaffold)]
		public void ScaffoldUpdate (CommandInfo info)
		{
			var project = CurrentNode.GetParentDataItem (typeof (DotNetProject), true) as DotNetProject;

			info.Enabled = info.Visible = IsAspNetCoreProject (project);
		}
		
		//[CommandUpdateHandler (AspNetCoreCommands.Scaffold)]
		//protected override void Update (CommandInfo info)
		//{
		//	var project = CurrentNode.GetParentDataItem (typeof (DotNetProject), true) as DotNetProject;

		//	info.Enabled = info.Visible = IsAspNetCoreProject (project);
		//}

		bool IsAspNetCoreProject (Project project)
		{
			//TODO: this only checks for SDK style project
			return project != null
				&& project.MSBuildProject.GetReferencedSDKs ().Any ();
		}
	}
}
