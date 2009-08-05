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
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core.Gui;
using System.IO;
using Gtk;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Execution;
using CustomCommand = MonoDevelop.Projects.CustomCommand;

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
		AddReference,
		AddNewFiles,
		AddFiles,
		NewFolder,
		IncludeToProject,
		BuildSolution,
		Build,
		RebuildSolution,
		Rebuild,
		SetAsStartupProject,
		Run,
		RunWithList,
		RunEntry,
		Clean,
		CleanSolution,
		LocalCopyReference,
		Stop,
		ConfigurationSelector,
		CustomCommandList,
		Reload,
		ExportProject,
		SpecificAssemblyVersion
	}

	internal class SolutionItemOptionsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget == null)
				info.Enabled = false;
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
			info.Enabled = ((IdeApp.Workspace.IsOpen) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
		}

		protected override void Run ()
		{
			//Build solution
			IdeApp.ProjectOperations.Build (IdeApp.Workspace);
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
					info.Text = GettextCatalog.GetString ("B_uild {0}", buildTarget.Name);
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
					info.Text = GettextCatalog.GetString ("B_uild {0}", Path.GetFileName (IdeApp.Workbench.ActiveDocument.Name));
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
			info.Enabled = ((IdeApp.Workspace.IsOpen) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
		}

		protected override void Run ()
		{
			//Build solution
			IdeApp.ProjectOperations.Rebuild (IdeApp.Workspace);
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
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
					info.Description = GettextCatalog.GetString ("Rebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
				}
			}
			else {
				info.Enabled = ((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.IsBuildTarget) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
				if (info.Enabled) {
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", IdeApp.Workbench.ActiveDocument.FileName);
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
				info.Text = GettextCatalog.GetString ("_Run again");
			else
				info.Text = GettextCatalog.GetString ("_Run");

			info.Enabled = CanRun (Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		public static bool CanRun (IExecutionHandler executionHandler)
		{
            if (IdeApp.Workspace.IsOpen)
                return (IdeApp.ProjectOperations.CanExecute (IdeApp.Workspace, executionHandler)) && !(IdeApp.ProjectOperations.CurrentSelectedItem is Workspace);
            else
                return (IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.CanRun (executionHandler));
		}

        public static void RunMethod (IExecutionHandler executionHandler)
        {
            if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
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
                if (!IdeApp.Preferences.BuildBeforeExecuting)
                    IdeApp.ProjectOperations.Execute (IdeApp.Workspace, executionHandler);
                else {
                    IAsyncOperation asyncOperation = IdeApp.ProjectOperations.Build (IdeApp.Workspace);
                    asyncOperation.Completed += delegate {
                        if ((asyncOperation.Success) || (IdeApp.Preferences.RunWithWarnings && asyncOperation.SuccessWithWarnings))
                            IdeApp.ProjectOperations.Execute (IdeApp.Workspace, executionHandler);
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
			ExecutionModeCommandService.GenerateExecutionModeCommands (
			    IdeApp.ProjectOperations.CurrentSelectedProject,
			    RunHandler.CanRun,
			    info);
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

	internal class CleanHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget == null)
				info.Enabled = false;
			else {
				info.Text = GettextCatalog.GetString ("C_lean {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
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
			if (!IdeApp.Workspace.IsOpen)
				info.Enabled = false;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.Workspace);
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
						cmd.Execute (monitor, ce, IdeApp.Workspace.ActiveConfiguration);
					}
				}
			);
			t.IsBackground = true;
			t.Start ();
		}
	}

	internal class ExportProjectHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (!(IdeApp.ProjectOperations.CurrentSelectedItem is WorkspaceItem) && !(IdeApp.ProjectOperations.CurrentSelectedItem is SolutionEntityItem))
				info.Enabled = false;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Export (((IWorkspaceObject)IdeApp.ProjectOperations.CurrentSelectedItem), null);
		}
	}
}
