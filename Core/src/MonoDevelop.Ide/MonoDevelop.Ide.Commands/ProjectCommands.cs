// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects.Deployment;
using CustomCommand = MonoDevelop.Projects.CustomCommand;

namespace MonoDevelop.Ide.Commands
{
	public enum ProjectCommands
	{
		AddNewProject,
		AddNewCombine,
		AddProject,
		AddCombine,
		RemoveFromProject,
		Options,
		AddResource,
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
		IncludeInBuild,
		IncludeInDeploy,
		Deploy,
		ConfigurationSelector,
		Debug,
		DebugEntry,
		DebugApplication,
		Stop,
		Clean,
		CleanSolution,
		LocalCopyReference,
		DeployTargetList,
		ConfigureDeployTargets,
		CustomCommandList
	}
	
	internal class RunHandler: CommandHandler
	{
		Document doc;
		
		protected override void Run ()
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				IAsyncOperation op = IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentOpenCombine);
				op.Completed += new OperationHandler (ExecuteCombine);
			} else {
				doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					IAsyncOperation op = doc.Build ();
					op.Completed += new OperationHandler (ExecuteFile);
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				info.Enabled = IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
			} else {
				info.Enabled = (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget);
			}
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
			if (op.Success)
				// FIXME: check RunWithWarnings
				IdeApp.ProjectOperations.Execute (IdeApp.ProjectOperations.CurrentOpenCombine);
		}
		
		void ExecuteFile (IAsyncOperation op)
		{
			if (op.Success)
				doc.Run ();
		}
	}
	
	
	internal class RunEntryHandler: CommandHandler
	{
		CombineEntry entry;
		
		protected override void Run ()
		{
			entry = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			IAsyncOperation op = IdeApp.ProjectOperations.Build (entry);
			op.Completed += new OperationHandler (ExecuteCombine);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null && 
							IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
			if (op.Success)
				IdeApp.ProjectOperations.Execute (entry);
		}
	}
	
	
	internal class DebugHandler: CommandHandler
	{
		Document doc;
		
		protected override void Run ()
		{
			if (Services.DebuggingService != null && Services.DebuggingService.IsDebugging && !Services.DebuggingService.IsRunning) {
				Services.DebuggingService.Resume ();
				return;
			}
			
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				IAsyncOperation op = IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentOpenCombine);
				op.Completed += new OperationHandler (ExecuteCombine);
			} else {
				doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					doc.Save ();
					IAsyncOperation op = doc.Build ();
					op.Completed += new OperationHandler (ExecuteFile);
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService != null && Services.DebuggingService.IsDebugging && !Services.DebuggingService.IsRunning) {
				info.Enabled = true;
				info.Text = GettextCatalog.GetString ("Resume");
				return;
			}

			if (Services.DebuggingService == null) {
				info.Enabled = false;
				return;
			}
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				info.Enabled = IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
			} else {
				info.Enabled = (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget);
			}
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
			if (op.Success)
				IdeApp.ProjectOperations.Debug (IdeApp.ProjectOperations.CurrentOpenCombine);
		}
		
		void ExecuteFile (IAsyncOperation op)
		{
			if (op.Success)
				doc.Debug ();
		}
	}
	
	internal class DebugEntryHandler: CommandHandler
	{
		CombineEntry entry;
		
		protected override void Run ()
		{
			entry = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			IAsyncOperation op = IdeApp.ProjectOperations.Build (entry);
			op.Completed += new OperationHandler (ExecuteCombine);
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService == null) {
				info.Enabled = false;
				return;
			}

			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null && 
							IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
			if (op.Success)
				IdeApp.ProjectOperations.Debug (entry);
		}
	}
	
	internal class BuildHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				if (IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null)
					IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedCombineEntry);
			}
			else if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Build ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				CombineEntry entry = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
				if (entry != null) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					info.Text = GettextCatalog.GetString ("Build {0}", entry.Name);
					if (entry is Combine)
						info.Description = GettextCatalog.GetString ("Build Solution {0}", entry.Name);
					else if (entry is Project)
						info.Description = GettextCatalog.GetString ("Build Project {0}", entry.Name);
					else
						info.Description = info.Text;
				} else {
					info.Enabled = false;
				}
			} else {
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					string file = Path.GetFileName (IdeApp.Workbench.ActiveDocument.FileName);
					info.Text = info.Description = GettextCatalog.GetString ("Build {0}", file);
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
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				if (IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null)
					IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedCombineEntry);
			}
			else if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Build ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				CombineEntry entry = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
				if (entry != null) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					info.Text = info.Description = GettextCatalog.GetString ("Rebuild {0}", entry.Name);
				} else {
					info.Enabled = false;
				}
			} else {
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget) {
					info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
					string file = Path.GetFileName (IdeApp.Workbench.ActiveDocument.FileName);
					info.Text = info.Description = GettextCatalog.GetString ("Rebuild {0}", file);
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
			IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentOpenCombine);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted &&
							(IdeApp.ProjectOperations.CurrentOpenCombine != null);
		}
	}
	
	internal class RebuildSolutionHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentOpenCombine);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted &&
							(IdeApp.ProjectOperations.CurrentOpenCombine != null);
		}
	}
	
	internal class CleanSolutionHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.ProjectOperations.CurrentOpenCombine);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentOpenCombine != null;
		}
	}
	
	internal class CleanHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.ProjectOperations.CurrentSelectedCombineEntry);
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null) {
				info.Enabled = IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null;
				info.Text = info.Description = GettextCatalog.GetString ("Clean {0}", IdeApp.ProjectOperations.CurrentSelectedCombineEntry.Name);
			} else {
				info.Enabled = false;
			}
		}
	}
	
	internal class StopHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (!IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentBuildOperation.Cancel ();
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentRunOperation.Cancel ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = !IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted ||
							!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted;
		}
	}
	
	internal class DeployHandler: CommandHandler
	{
		protected override void Run ()
		{
			CombineEntry e = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			IAsyncOperation op = IdeApp.ProjectOperations.Build (e);
			op.Completed += delegate {
				if (op.Success)
					IdeApp.ProjectOperations.Deploy (e, e.DefaultDeployTarget);
			};
		}
		
		protected override void Update (CommandInfo info)
		{
			CombineEntry e = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			if (e != null && e.DefaultDeployTarget != null) {
				info.Enabled = true;
			} else
				info.Enabled = false;
		}
	}
	
	internal class DeployTargetListHandler: CommandHandler
	{
		protected override void Run (object dt)
		{
			CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			IAsyncOperation op = IdeApp.ProjectOperations.Build (ce);
			op.Completed += delegate {
				if (op.Success)
					IdeApp.ProjectOperations.Deploy (ce, (DeployTarget)dt);
			};
		}
		
		protected override void Update (CommandArrayInfo info)
		{
			CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			if (ce != null) {
				if (ce.DeployTargets.Count > 0) {
					foreach (DeployTarget dt in ce.DeployTargets) {
						if (dt is UnknownDeployTarget)
							continue;
						CommandInfo cinfo = new CommandInfo ();
						cinfo.Text = dt.Name;
						if (ce.DefaultDeployTarget == dt)
							cinfo.Text += " " + GettextCatalog.GetString ("(default)");
						cinfo.Icon = dt.DeployHandler.Icon;
						info.Add (cinfo, dt);
					}
				} else {
					CommandInfo cinfo = new CommandInfo ();
					cinfo.Text = GettextCatalog.GetString ("No deploy targets available");
					cinfo.Enabled = false;
					info.Add (cinfo, null);
				}
			}
		}
	}
	
	internal class ConfigureDeployTargetsHandler: CommandHandler
	{
		protected override void Run ()
		{
			CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			if (ce != null)
				IdeApp.ProjectOperations.ShowOptions (ce, "Deployment");
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null;
		}
	}
	
	internal class CombineEntryOptionsHandler: CommandHandler
	{
		protected override void Run ()
		{
			CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			if (ce != null)
				IdeApp.ProjectOperations.ShowOptions (ce);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedCombineEntry != null;
		}
	}
	
	internal class CustomCommandListHandler: CommandHandler
	{
		protected override void Run (object c)
		{
			CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			CustomCommand cmd = (CustomCommand) c;
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			
			Thread t = new Thread (
				delegate () {
					using (monitor) {
						cmd.Execute (monitor, ce);
					}
				}
			);
			t.IsBackground = true;
			t.Start ();
		}
		
		protected override void Update (CommandArrayInfo info)
		{
			CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			if (ce != null) {
				AbstractConfiguration conf = ce.ActiveConfiguration as AbstractConfiguration;
				if (conf != null) {
					foreach (CustomCommand cmd in conf.CustomCommands)
						if (cmd.Type == CustomCommandType.Custom)
							info.Add (cmd.Name, cmd);
				}
			}
		}
	}
	
	internal class GenerateProjectDocumentation : CommandHandler
	{
		protected override void Run ()
		{
			try {
				if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
					string assembly    = IdeApp.ProjectOperations.CurrentSelectedProject.GetOutputFileName ();
					string projectFile = Path.ChangeExtension(assembly, ".ndoc");
					if (!File.Exists(projectFile)) {
						StreamWriter sw = File.CreateText(projectFile);
						sw.WriteLine("<project>");
						sw.WriteLine("    <assemblies>");
						sw.WriteLine("        <assembly location=\""+ assembly +"\" documentation=\"" + Path.ChangeExtension(assembly, ".xml") + "\" />");
						sw.WriteLine("    </assemblies>");
						/*
						sw.WriteLine("    				    <documenters>");
						sw.WriteLine("    				        <documenter name=\"JavaDoc\">");
						sw.WriteLine("    				            <property name=\"Title\" value=\"NDoc\" />");
						sw.WriteLine("    				            <property name=\"OutputDirectory\" value=\".\\docs\\JavaDoc\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingSummaries\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingRemarks\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingParams\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingReturns\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingValues\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentInternals\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentProtected\" value=\"True\" />");
						sw.WriteLine("    				            <property name=\"DocumentPrivates\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentEmptyNamespaces\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"IncludeAssemblyVersion\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"CopyrightText\" value=\"\" />");
						sw.WriteLine("    				            <property name=\"CopyrightHref\" value=\"\" />");
						sw.WriteLine("    				        </documenter>");
						sw.WriteLine("    				        <documenter name=\"MSDN\">");
						sw.WriteLine("    				            <property name=\"OutputDirectory\" value=\".\\docs\\MSDN\" />");
						sw.WriteLine("    				            <property name=\"HtmlHelpName\" value=\"NDoc\" />");
						sw.WriteLine("    				            <property name=\"HtmlHelpCompilerFilename\" value=\"C:\\Program Files\\HTML Help Workshop\\hhc.exe\" />");
						sw.WriteLine("    				            <property name=\"IncludeFavorites\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"Title\" value=\"An NDoc Documented Class Library\" />");
						sw.WriteLine("    				            <property name=\"SplitTOCs\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DefaulTOC\" value=\"\" />");
						sw.WriteLine("    				            <property name=\"ShowVisualBasic\" value=\"True\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingSummaries\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingRemarks\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingParams\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingValues\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentInternals\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentProtected\" value=\"True\" />");
						sw.WriteLine("    				            <property name=\"DocumentPrivates\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentEmptyNamespaces\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"IncludeAssemblyVersion\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"CopyrightText\" value=\"\" />");
						sw.WriteLine("                <property name=\"CopyrightHref\" value=\"\" />");
						sw.WriteLine("            </documenter>");
						sw.WriteLine("    				        <documenter name=\"XML\">");
						sw.WriteLine("    				            <property name=\"OutputFile\" value=\".\\docs\\doc.xml\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingSummaries\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingRemarks\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingParams\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingReturns\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"ShowMissingValues\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentInternals\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentProtected\" value=\"True\" />");
						sw.WriteLine("    				            <property name=\"DocumentPrivates\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"DocumentEmptyNamespaces\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"IncludeAssemblyVersion\" value=\"False\" />");
						sw.WriteLine("    				            <property name=\"CopyrightText\" value=\"\" />");
						sw.WriteLine("    				            <property name=\"CopyrightHref\" value=\"\" />");
						sw.WriteLine("    				        </documenter>");
						sw.WriteLine("    				    </documenters>");*/
						sw.WriteLine("    				</project>");
						sw.Close();
					}
					string command = Runtime.FileService.SharpDevelopRootPath +
					Path.DirectorySeparatorChar + "bin" +
					Path.DirectorySeparatorChar + "ndoc" +
					Path.DirectorySeparatorChar + "NDocGui.exe";
					string args    = '"' + projectFile + '"';
					
					ProcessStartInfo psi = new ProcessStartInfo(command, args);
					psi.WorkingDirectory = Runtime.FileService.SharpDevelopRootPath +
					Path.DirectorySeparatorChar + "bin" +
					Path.DirectorySeparatorChar + "ndoc";
					psi.UseShellExecute = false;
					Process p = new Process();
					p.StartInfo = psi;
					p.Start();
				}
			} catch (Exception) {
				//MessageBox.Show("You need to compile the project first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
			}
		}
	}
}
