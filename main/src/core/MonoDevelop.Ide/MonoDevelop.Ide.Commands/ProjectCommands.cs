// ProjectCommands.cs
//
// Author:
//   Mike Krüger (mkrueger@novell.com)
//   Lluis Sanchez Gual (lluis@novell.com)
//   Michael Hutchinson (mhutchinson@novell.com)
//   Ankit Jain (jankit@novell.com)
//   Marek Sieradzki  <marek.sieradzki@gmail.com>
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009
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
//
//

using System;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using System.IO;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Execution;
using CustomCommand = MonoDevelop.Projects.CustomCommand;
using System.Linq;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Commands
{
	public enum ProjectCommands
	{
		AddNewProject,
		AddNewWorkspace,
		AddNewSolution,
		AddSolutionFolder,
		AddProject,
		AddItem,
		RemoveFromProject,
		Options,
		SolutionOptions,
		ProjectOptions,
		
		AddReference,
		AddNewFiles,
		AddFiles,
		NewFolder,
		AddFilesFromFolder,
		AddExistingFolder,
		IncludeToProject,
		BuildSolution,
		Build,
		RebuildSolution,
		Rebuild,
		SetAsStartupProject,
		Run,
		RunWithList,
		RunEntry,
		RunEntryWithList,
		Clean,
		CleanSolution,
		LocalCopyReference,
		Stop,
		ConfigurationSelector,
		CustomCommandList,
		Reload,
		ExportSolution,
		SpecificAssemblyVersion,
		SelectActiveConfiguration,
		SelectActiveRuntime,
		EditSolutionItem,
		Unload
	}

	internal class SolutionOptionsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}
	
	internal class ProjectOptionsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			Project project = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as Project;
			info.Enabled = project != null;
			info.Text = project != null ? GettextCatalog.GetString ("{0} _Options", project.Name) : GettextCatalog.GetString ("Project _Options");
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedObject);
		}
	}
	
	internal class SolutionItemOptionsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedBuildTarget != null;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedObject);
		}
	}

	internal class EditReferencesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (!(IdeApp.ProjectOperations.CurrentSelectedProject is DotNetProject)) {
				info.Enabled = false;
				info.Bypass = true;
			}
			else
				info.Bypass = false;
		}

		protected override async void Run ()
		{
			//Edit references
			DotNetProject p = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (IdeApp.ProjectOperations.AddReferenceToProject (p))
				await IdeApp.ProjectOperations.SaveAsync (p);

		}
	}

	internal class BuildSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ((IdeApp.ProjectOperations.CurrentSelectedSolution != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
		}

		protected override void Run ()
		{
			//Build solution
			IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}

	internal class BuildHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				IBuildTarget buildTarget = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
				info.Enabled = (buildTarget != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) && buildTarget.CanBuild (IdeApp.Workspace.ActiveConfiguration);
				if (buildTarget != null) {
					info.Text = GettextCatalog.GetString ("B_uild {0}", buildTarget.Name.Replace ("_","__"));
					if (buildTarget is SolutionFolder)
						info.Description = GettextCatalog.GetString ("Build solution {0}", buildTarget.Name);
					else if (buildTarget is Project)
						info.Description = GettextCatalog.GetString ("Build project {0}", buildTarget.Name);
					else
						info.Description = GettextCatalog.GetString ("Build {0}", buildTarget.Name);
				}
			}
			else
				info.Enabled = false;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
		}
	}

	internal class RebuildSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ((IdeApp.ProjectOperations.CurrentSelectedSolution != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
		}

		protected override void Run ()
		{
			//Build solution
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}

	internal class RebuildHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				IBuildTarget buildTarget = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
				info.Enabled = (buildTarget != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) && buildTarget.CanBuild (IdeApp.Workspace.ActiveConfiguration);
				if (buildTarget != null) {
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name.Replace ("_","__"));
					info.Description = GettextCatalog.GetString ("Rebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
				}
			}
			else
				info.Enabled = false;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
		}
	}

	internal class RunHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if ((IdeApp.Workspace.IsOpen) && (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted))
				info.Text = GettextCatalog.GetString ("Restart Without Debugging");
			else
				info.Text = GettextCatalog.GetString ("Start Without Debugging");

			info.Enabled = CanRun (Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		static IBuildTarget GetRunTarget ()
		{
			return IdeApp.ProjectOperations.CurrentSelectedSolution ?? IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
		}
		
		public static bool CanRun (IExecutionHandler executionHandler)
		{
			if (IdeApp.Workspace.IsOpen) {
				var target = GetRunTarget ();
				return target != null && IdeApp.ProjectOperations.CanExecute (target, executionHandler);
			} else
				return false;
		}

        public static async void RunMethod (IExecutionHandler executionHandler)
        {
            if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("An application is already running. Do you want to stop it?"), AlertButton.Stop))
					return;
                StopHandler.StopBuildOperations ();
                await IdeApp.ProjectOperations.CurrentRunOperation.Task;
            }

			if (IdeApp.Workspace.IsOpen) {
				var target = GetRunTarget ();
				IdeApp.ProjectOperations.Execute (target, executionHandler, true);
			}
        }

		protected override void Run ()
		{
            RunMethod (Runtime.ProcessService.DefaultExecutionHandler);
		}

	}

	internal class RunWithHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			Solution sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol != null && sol.StartupItem != null)
				ExecutionModeCommandService.GenerateExecutionModeCommands (sol.StartupItem, info);
		}

		protected override void Run (object dataItem)
		{
			Solution sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol != null && sol.StartupItem != null)
				ExecutionModeCommandService.ExecuteCommand (sol.StartupItem, dataItem);
		}
	}

	internal class RunEntryHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			IBuildTarget buildTarget = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			info.Enabled = ((buildTarget != null) && (!(buildTarget is Workspace)) && IdeApp.ProjectOperations.CanExecute (buildTarget));
		}

		protected override void Run ()
		{
			var target = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			IdeApp.ProjectOperations.Execute (target);
		}
	}

	internal class RunEntryWithHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			SolutionItem item = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as SolutionItem;
			if (item != null) {
				ExecutionModeCommandService.GenerateExecutionModeCommands (item, info);
			}
		}

		protected override void Run (object dataItem)
		{
			SolutionItem item = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as SolutionItem;
			if (item != null)
				ExecutionModeCommandService.ExecuteCommand (item, dataItem);
		}
	}

	internal class CleanHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget == null)
				info.Enabled = false;
			else {
				info.Enabled = IdeApp.ProjectOperations.CurrentSelectedBuildTarget.CanBuild (IdeApp.Workspace.ActiveConfiguration);
				info.Text = GettextCatalog.GetString ("C_lean {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name.Replace ("_","__"));
				info.Description = GettextCatalog.GetString ("Clean {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
			}
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
		}
	}

	internal class CleanSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}

	public class StopHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if ((IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) && (IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted))
				info.Enabled = false;
		}

		protected override void Run ()
		{
			StopBuildOperations ();
		}
		
		public static void StopBuildOperations ()
		{
			if (!IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentBuildOperation.Cancel ();
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentRunOperation.Cancel ();
		}
	}

	internal class CustomCommandListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			IConfigurationTarget ce = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as IConfigurationTarget;
			if (ce != null) {
				ItemConfiguration conf = ce.DefaultConfiguration;
				if (conf != null) {
					foreach (CustomCommand cmd in conf.CustomCommands)
						if (cmd.Type == CustomCommandType.Custom)
							info.Add (cmd.Name, cmd);
				}
			}
		}

		protected override void Run (object dataItem)
		{
			var ce = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as WorkspaceObject;
			CustomCommand cmd = (CustomCommand) dataItem;
			ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor (cmd.Name);
			
			Thread t = new Thread (
				async delegate () {
					using (monitor) {
						try {
							await cmd.Execute (monitor, ce, IdeApp.Workspace.ActiveConfiguration);
						} catch (Exception ex) {
							monitor.ReportError (GettextCatalog.GetString ("Command execution failed"), ex);
						}
					}
				}
			);
			t.IsBackground = true;
			t.Start ();
		}
	}

	internal class ExportSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			// FIXME: Once we fix Workspaces to offer Visual Studio formats (instead of the deprecated MonoDevelop 1.0 format), we can allow exporting of Workspaces as well.
			if (!(IdeApp.ProjectOperations.CurrentSelectedItem is Solution) && !(IdeApp.ProjectOperations.CurrentSelectedItem is SolutionItem))
				info.Enabled = false;
		}

		protected override void Run ()
		{
			Solution workspace;
			
			if (!(IdeApp.ProjectOperations.CurrentSelectedItem is WorkspaceItem))
				workspace = ((SolutionItem) IdeApp.ProjectOperations.CurrentSelectedItem).ParentSolution;
			else
				workspace = (Solution) IdeApp.ProjectOperations.CurrentSelectedItem;
			
			IdeApp.ProjectOperations.Export (workspace, null);
		}
	}
	
	internal class SelectActiveConfigurationHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				foreach (var conf in IdeApp.Workspace.GetConfigurations ()) {
					var i = info.Add (conf, conf);
					if (conf == IdeApp.Workspace.ActiveConfigurationId)
						i.Checked  = true;
				}
			}
		}

		protected override void Run (object dataItem)
		{
			IdeApp.Workspace.ActiveConfigurationId = (string) dataItem;
		}
	}
	
	internal class SelectActiveRuntimeHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			if (IdeApp.Workspace.IsOpen && Runtime.SystemAssemblyService.GetTargetRuntimes ().Count () > 1) {
				foreach (var tr in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
					var item = info.Add (tr.DisplayName, tr);
					if (tr == IdeApp.Workspace.ActiveRuntime)
						item.Checked = true;
				}
			}
		}

		protected override void Run (object dataItem)
		{
			IdeApp.Workspace.ActiveRuntime = (MonoDevelop.Core.Assemblies.TargetRuntime) dataItem;
		}
	}
	
	class ApplyPolicyHandler: CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null || IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}
		
		protected override void Run ()
		{
			Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			using (var dlg = new ApplyPolicyDialog ((IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolutionItem ?? (IPolicyProvider)solution)) {
				if (MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok) {
					if (project != null)
						IdeApp.ProjectOperations.SaveAsync (project);
					else
						IdeApp.ProjectOperations.SaveAsync (solution);
				}
			}
		}
	}
	
	class ExportPolicyHandler: CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null || IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}
		
		protected override void Run ()
		{
			using (ExportProjectPolicyDialog dlg = new ExportProjectPolicyDialog ((IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolutionItem ?? (IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolution))
				MessageService.ShowCustomDialog (dlg);
		}
	}

	internal class RunCodeAnalysisSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = (IdeApp.ProjectOperations.CurrentSelectedSolution != null) &&
				(IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) &&
				IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ().Any (p => p.SupportsTarget ("RunCodeAnalysis"));
		}

		protected override void Run ()
		{
			var context = new ProjectOperationContext ();
			context.GlobalProperties.SetValue ("RunCodeAnalysisOnce", "true");
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedSolution, context);
		}
	}

	internal class RunCodeAnalysisProjectHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				var project = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (project != null) {
					info.Enabled = project.SupportsTarget ("RunCodeAnalysis");
					info.Text = GettextCatalog.GetString ("Run Code Analysis on {0}", project.Name.Replace ("_","__"));
					return;
				}
			}
			info.Text = GettextCatalog.GetString ("Run Code Analysis on Project");
			info.Enabled = false;
		}

		protected override void Run ()
		{
			var context = new ProjectOperationContext ();
			context.GlobalProperties.SetValue ("RunCodeAnalysisOnce", "true");
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedProject, context);
		}
	}
}
