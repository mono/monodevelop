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
		EditSolutionItem
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
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
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
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
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

		protected override void Run ()
		{
			//Edit references
			DotNetProject p = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (IdeApp.ProjectOperations.AddReferenceToProject (p))
                IdeApp.ProjectOperations.Save (p);

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
				info.Enabled = ((buildTarget != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
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
			else {
				info.Enabled = ((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.IsBuildTarget) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
				if (IdeApp.Workbench.ActiveDocument != null) {
					info.Text = GettextCatalog.GetString ("B_uild {0}", Path.GetFileName (IdeApp.Workbench.ActiveDocument.Name).Replace ("_","__"));
                    info.Description = GettextCatalog.GetString ("Build {0}", Path.GetFileName (IdeApp.Workbench.ActiveDocument.Name));
				}
			}
		}

		protected override void Run ()
		{
			if (IdeApp.Workspace.IsOpen) {
                IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
			}
			else {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Build ();
			}
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
				info.Enabled = ((buildTarget != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
				if (buildTarget != null) {
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name.Replace ("_","__"));
					info.Description = GettextCatalog.GetString ("Rebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
				}
			}
			else {
				info.Enabled = ((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.IsBuildTarget) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
				if (info.Enabled) {
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", IdeApp.Workbench.ActiveDocument.FileName.FileName.Replace ("_","__"));
					info.Description = GettextCatalog.GetString ("Rebuild {0}", IdeApp.Workbench.ActiveDocument.FileName);
				}
			}
		}

		protected override void Run ()
		{
			if (IdeApp.Workspace.IsOpen) {
				IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
			}
			else {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Rebuild ();
			}
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
			return IdeApp.ProjectOperations.CurrentSelectedSolution != null && IdeApp.ProjectOperations.CurrentSelectedSolution.StartupItem != null ? 
				IdeApp.ProjectOperations.CurrentSelectedSolution.StartupItem : 
					IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
		}
		
		public static bool CanRun (IExecutionHandler executionHandler)
		{
            if (IdeApp.Workspace.IsOpen) {
				var target = GetRunTarget ();
				return target != null && IdeApp.ProjectOperations.CanExecute (target, executionHandler);
			}
            else
                return (IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.CanRun (executionHandler));
		}

        public static void RunMethod (IExecutionHandler executionHandler)
        {
            if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("An application is already running. Do you want to stop it?"), AlertButton.Stop))
					return;
                StopHandler.StopBuildOperations ();
                IdeApp.ProjectOperations.CurrentRunOperation.WaitForCompleted ();
            }

            if (!IdeApp.Workspace.IsOpen) {
                if (!IdeApp.Preferences.BuildBeforeExecuting)
                    IdeApp.Workbench.ActiveDocument.Run (executionHandler);
                else {
                    IAsyncOperation asyncOperation = IdeApp.Workbench.ActiveDocument.Build ();
                    asyncOperation.Completed += delegate {
                        if ((asyncOperation.Success) || (IdeApp.Preferences.RunWithWarnings && asyncOperation.SuccessWithWarnings))
                            IdeApp.Workbench.ActiveDocument.Run (executionHandler);
                    };
                }
            }
            else {
				var target = GetRunTarget ();
                if (!IdeApp.Preferences.BuildBeforeExecuting)
					IdeApp.ProjectOperations.Execute (target, executionHandler);
                else {
                    IAsyncOperation asyncOperation = IdeApp.ProjectOperations.Build (target);
                    asyncOperation.Completed += delegate {
                        if ((asyncOperation.Success) || (IdeApp.Preferences.RunWithWarnings && asyncOperation.SuccessWithWarnings))
                            IdeApp.ProjectOperations.Execute (target, executionHandler);
                    };
                }

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
			if (sol != null) {
				ExecutionModeCommandService.GenerateExecutionModeCommands (
				    sol.StartupItem as Project,
				    RunHandler.CanRun,
				    info);
			}
		}

		protected override void Run (object dataItem)
		{
			IExecutionHandler h = ExecutionModeCommandService.GetExecutionModeForCommand (dataItem);
			if (h != null)
				RunHandler.RunMethod (h);
		}
	}

	internal class RunEntryHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			IBuildTarget buildTarget = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			info.Enabled = ((buildTarget != null) && (!(buildTarget is Workspace)) && IdeApp.ProjectOperations.CanExecute (buildTarget) && IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted);
		}

		protected override void Run ()
		{
			if (!IdeApp.Preferences.BuildBeforeExecuting)
				IdeApp.ProjectOperations.Execute (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
			else {
				IAsyncOperation asyncOperation = IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
				asyncOperation.Completed += delegate
				{
					if ((asyncOperation.Success) || (IdeApp.Preferences.RunWithWarnings && asyncOperation.SuccessWithWarnings))
                        IdeApp.ProjectOperations.Execute (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
				};
			}
		}
	}

	internal class RunEntryWithHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			SolutionEntityItem item = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as SolutionEntityItem;
			if (item != null) {
				ExecutionModeCommandService.GenerateExecutionModeCommands (
				    item,
				    delegate (IExecutionHandler h) {
						return IdeApp.ProjectOperations.CanExecute (item, h);
					},
				    info);
			}
		}

		protected override void Run (object dataItem)
		{
			IExecutionHandler h = ExecutionModeCommandService.GetExecutionModeForCommand (dataItem);
			IBuildTarget target = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			if (h == null || !IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
				return;
			
			if (!IdeApp.Preferences.BuildBeforeExecuting)
				IdeApp.ProjectOperations.Execute (target, h);
			else {
				IAsyncOperation asyncOperation = IdeApp.ProjectOperations.Build (target);
				asyncOperation.Completed += delegate
				{
					if ((asyncOperation.Success) || (IdeApp.Preferences.RunWithWarnings && asyncOperation.SuccessWithWarnings))
						IdeApp.ProjectOperations.Execute (target, h);
				};
			}
		}
	}

	internal class CleanHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget == null)
				info.Enabled = false;
			else {
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
			IWorkspaceObject ce = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			CustomCommand cmd = (CustomCommand) dataItem;
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			
			Thread t = new Thread (
				delegate () {
					using (monitor) {
						try {
							cmd.Execute (monitor, ce, IdeApp.Workspace.ActiveConfiguration);
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
			if (!(IdeApp.ProjectOperations.CurrentSelectedItem is Solution) && !(IdeApp.ProjectOperations.CurrentSelectedItem is SolutionEntityItem))
				info.Enabled = false;
		}

		protected override void Run ()
		{
			WorkspaceItem workspace;
			
			if (!(IdeApp.ProjectOperations.CurrentSelectedItem is WorkspaceItem))
				workspace = ((SolutionEntityItem) IdeApp.ProjectOperations.CurrentSelectedItem).ParentSolution;
			else
				workspace = (WorkspaceItem) IdeApp.ProjectOperations.CurrentSelectedItem;
			
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
			ApplyPolicyDialog dlg = new ApplyPolicyDialog ((IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolutionItem ?? (IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolution);
			MessageService.RunCustomDialog (dlg);
			dlg.Destroy ();
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
			ExportProjectPolicyDialog dlg = new ExportProjectPolicyDialog ((IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolutionItem ?? (IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolution);
			MessageService.RunCustomDialog (dlg);
			dlg.Destroy ();
		}
	}
}
