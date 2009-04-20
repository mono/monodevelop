//  ProjectCommands.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using CustomCommand = MonoDevelop.Projects.CustomCommand;

namespace MonoDevelop.Ide.Commands
{
	public enum ProjectCommands
	{
		AddNewProject,
		AddNewSolution,
		AddNewWorkspace,
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
		Build,
		BuildSolution,
		Rebuild,
		RebuildSolution,
		SetAsStartupProject,
		GenerateMakefiles,
		RunEntry,
		Run,
		RunWithList,
		IncludeInBuild,
		IncludeInDeploy,
		Deploy,
		ConfigurationSelector,
		Stop,
		Clean,
		CleanSolution,
		LocalCopyReference,
		DeployTargetList,
		ConfigureDeployTargets,
		CustomCommandList,
		Reload,
		ExportProject
	}
	
	internal class RunHandler: CommandHandler
	{
		Document doc;
		
		protected override void Run ()
		{
			Run (new DefaultExecutionHandlerFactory ());
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Text = GettextCatalog.GetString ("_Run");
			if (IdeApp.Workspace.IsOpen) {
				if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
					info.Text = GettextCatalog.GetString ("_Run again");
			}
			info.Enabled = CanRun (new DefaultExecutionHandlerFactory ());
		}
		
		protected void Run (IExecutionHandler handler)
		{
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
				StopHandler.StopBuildOperations ();
				IdeApp.ProjectOperations.CurrentRunOperation.WaitForCompleted ();
			} 
			if (IdeApp.Workspace.IsOpen) {
				if (IdeApp.Preferences.BuildBeforeExecuting) {
					IAsyncOperation op = IdeApp.ProjectOperations.Build (IdeApp.Workspace);
					op.Completed += delegate { ExecuteCombine (op, handler); };
				} else
					IdeApp.ProjectOperations.Execute (IdeApp.Workspace, handler);
			} else {
				doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					if (IdeApp.Preferences.BuildBeforeExecuting) {
						IAsyncOperation op = doc.Build ();
						op.Completed += delegate { ExecuteFile (op, doc, handler); };
					} else {
						doc.Run (handler);
					}
				}
			}
		}
		
		protected bool CanRun (IExecutionHandler handler)
		{
			if (IdeApp.Workspace.IsOpen)
				return !(IdeApp.ProjectOperations.CurrentSelectedItem is Workspace) && IdeApp.ProjectOperations.CanExecute (IdeApp.Workspace, handler);
			else
				return IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.CanRun (handler);
		}
		
		void ExecuteCombine (IAsyncOperation op, IExecutionHandler handler)
		{
			if (op.SuccessWithWarnings && !IdeApp.Preferences.RunWithWarnings)
				return;
			if (op.Success)
				IdeApp.ProjectOperations.Execute (IdeApp.Workspace, handler);
		}
		
		void ExecuteFile (IAsyncOperation op, Document doc, IExecutionHandler handler)
		{
			if (op.SuccessWithWarnings && !IdeApp.Preferences.RunWithWarnings)
				return;
			if (op.Success)
				doc.Run (handler);
		}
	}
	
	internal class RunWithHandler: RunHandler
	{
		protected override void Run (object dataItem)
		{
			Run ((IExecutionHandler) dataItem);
		}

		protected override void Update (CommandArrayInfo info)
		{
			foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
				foreach (IExecutionMode mode in mset.ExecutionModes) {
					if (CanRun (mode.ExecutionHandler))
						info.Add (mode.Name, mode.ExecutionHandler);
				}
				info.AddSeparator ();
			}
		}

	}
	
	internal class RunEntryHandler: CommandHandler
	{
		protected override void Run ()
		{
			IBuildTarget entry = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			if (IdeApp.Preferences.BuildBeforeExecuting) {
				IAsyncOperation op = IdeApp.ProjectOperations.Build (entry);
				op.Completed += delegate {
					if (op.SuccessWithWarnings && !IdeApp.Preferences.RunWithWarnings)
						return;
					if (op.Success)
						IdeApp.ProjectOperations.Execute (entry);
				};
			} else
				IdeApp.ProjectOperations.Execute (entry);
		}
		
		protected override void Update (CommandInfo info)
		{
			IBuildTarget target = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			info.Enabled = target != null &&
					!(target is Workspace) && IdeApp.ProjectOperations.CanExecute (target) &&
					IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
		}
	}
	
	internal class BuildHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workspace.IsOpen) {
				if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget != null)
					IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
			}
			else if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Build ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				IBuildTarget entry = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
				if (entry != null) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					info.Text = GettextCatalog.GetString ("B_uild {0}", entry.Name);
					if (entry is SolutionFolder)
						info.Description = GettextCatalog.GetString ("Build solution {0}", entry.Name);
					else if (entry is Project)
						info.Description = GettextCatalog.GetString ("Build project {0}", entry.Name);
					else
						info.Description = GettextCatalog.GetString ("Build {0}", entry.Name);;
				} else {
					info.Enabled = false;
				}
			} else {
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					string file = Path.GetFileName (IdeApp.Workbench.ActiveDocument.Name);
					info.Text = GettextCatalog.GetString ("B_uild {0}", file);
					info.Description = GettextCatalog.GetString ("Build {0}", file);
				} else {
					info.Enabled = false;
				}
			}
		}
	}
	
	
	internal class RebuildHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workspace.IsOpen) {
				if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget != null)
					IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
			}
			else if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Build ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				IBuildTarget entry = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
				if (entry != null) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", entry.Name);
					info.Description = GettextCatalog.GetString ("Rebuild {0}", entry.Name);
				} else {
					info.Enabled = false;
				}
			} else {
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					string file = Path.GetFileName (IdeApp.Workbench.ActiveDocument.Name);
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", file);
					info.Description = GettextCatalog.GetString ("Rebuild {0}", file);
				} else {
					info.Enabled = false;
				}
			}
		}
	}
	
	internal class BuildSolutionHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.Build (IdeApp.Workspace);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted &&
							(IdeApp.Workspace.IsOpen);
		}
	}
	
	internal class RebuildSolutionHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.Rebuild (IdeApp.Workspace);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted &&
							(IdeApp.Workspace.IsOpen);
		}
	}
	
	internal class CleanSolutionHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.Workspace);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace.IsOpen;
		}
	}
	
	internal class CleanHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget != null) {
				info.Text = GettextCatalog.GetString ("C_lean {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
				info.Description = GettextCatalog.GetString ("Clean {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
			} else {
				info.Enabled = false;
			}
		}
	}
	
	public class StopHandler: CommandHandler
	{
		public static void StopBuildOperations ()
		{
			if (!IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentBuildOperation.Cancel ();
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentRunOperation.Cancel ();
		}
		
		protected override void Run ()
		{
			StopBuildOperations ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = !IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted ||
							!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
		}
	}
	
	internal class SolutionItemOptionsHandler: CommandHandler
	{
		protected override void Run ()
		{
			IBuildTarget ce = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			if (ce != null)
				IdeApp.ProjectOperations.ShowOptions (ce);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedBuildTarget != null;
		}
	}
	
	internal class CustomCommandListHandler: CommandHandler
	{
		protected override void Run (object c)
		{
			IWorkspaceObject ce = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			CustomCommand cmd = (CustomCommand) c;
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
	}
	
	internal class ExportProjectHandler: CommandHandler
	{
		protected override void Run ()
		{
			IWorkspaceObject ce = IdeApp.ProjectOperations.CurrentSelectedItem as IWorkspaceObject;
			IdeApp.ProjectOperations.Export (ce, null);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedItem is WorkspaceItem || IdeApp.ProjectOperations.CurrentSelectedItem is SolutionEntityItem;
		}

	}
}
