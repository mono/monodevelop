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
using MonoDevelop.Ide.Projects;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
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
		ExcludeFromProject,
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
		CustomCommandList,
		Reload,
		ExportProject
	}
	
	internal class RunHandler: CommandHandler
	{
		Document doc;
		
		protected override void Run ()
		{
			if (MonoDevelop.Ide.Projects.ProjectService.Solution != null) {
				IAsyncOperation op = MonoDevelop.Ide.Projects.ProjectService.BuildSolution ();
				if (op.IsCompleted) 
					ExecuteCombine (op);
				else
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
			if (MonoDevelop.Ide.Projects.ProjectService.Solution != null) {
				info.Enabled = MonoDevelop.Ide.Projects.ProjectService.CurrentRunOperation.IsCompleted;
			} else {
				info.Enabled = (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget);
			}
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
			Console.WriteLine ("exec !!!");
			if (op.Success)
				// FIXME: check RunWithWarnings
				MonoDevelop.Ide.Projects.ProjectService.StartSolution ();
		}
		
		void ExecuteFile (IAsyncOperation op)
		{
			if (op.Success)
				doc.Run ();
		}
	}
	
	
	internal class RunEntryHandler: CommandHandler
	{
		MonoDevelop.Ide.Projects.SolutionProject entry;
		
		protected override void Run ()
		{
			entry = MonoDevelop.Ide.Projects.ProjectService.ActiveProject;
			IAsyncOperation op = MonoDevelop.Ide.Projects.ProjectService.BuildProject (entry);
			op.Completed += new OperationHandler (ExecuteCombine);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = MonoDevelop.Ide.Projects.ProjectService.ActiveProject != null && 
							MonoDevelop.Ide.Projects.ProjectService.CurrentRunOperation.IsCompleted;
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
			if (op.Success)
				MonoDevelop.Ide.Projects.ProjectService.StartProject (entry);
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
			
			if (MonoDevelop.Ide.Projects.ProjectService.Solution != null) {
				IAsyncOperation op = MonoDevelop.Ide.Projects.ProjectService.BuildSolution ();
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
			if (ProjectService.Solution != null) {
				info.Enabled = ProjectService.CurrentRunOperation.IsCompleted;
			} else {
				info.Enabled = (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget);
			}
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
			// TODO:
//			if (op.Success)
//				IdeApp.ProjectOperations.Debug (IdeApp.ProjectOperations.CurrentOpenCombine);
		}
		
		void ExecuteFile (IAsyncOperation op)
		{
			if (op.Success)
				doc.Debug ();
		}
	}
	
	internal class DebugEntryHandler: CommandHandler
	{
		SolutionProject entry;
		
		protected override void Run ()
		{
			entry = ProjectService.ActiveProject;
			IAsyncOperation op = ProjectService.BuildProject (entry);
			op.Completed += new OperationHandler (ExecuteCombine);
		}
		
		protected override void Update (CommandInfo info)
		{
			if (Services.DebuggingService == null) {
				info.Enabled = false;
				return;
			}

			info.Enabled = ProjectService.ActiveProject != null && 
							ProjectService.CurrentRunOperation.IsCompleted;
		}
		
		void ExecuteCombine (IAsyncOperation op)
		{
// TODO: Project conversion.
//	if (op.Success)
//		
//				IdeApp.ProjectOperations.Debug (entry);
		}
	}
	
	internal class BuildHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (MonoDevelop.Ide.Projects.ProjectService.Solution != null) {
				if (MonoDevelop.Ide.Projects.ProjectService.ActiveProject != null)
					MonoDevelop.Ide.Projects.ProjectService.BuildProject (MonoDevelop.Ide.Projects.ProjectService.ActiveProject);
			} else if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Build ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (MonoDevelop.Ide.Projects.ProjectService.Solution != null) {
				if (MonoDevelop.Ide.Projects.ProjectService.ActiveProject != null) {
					info.Enabled = MonoDevelop.Ide.Projects.ProjectService.CurrentBuildOperation.IsCompleted;
					string name = MonoDevelop.Ide.Projects.ProjectService.ActiveProject.Name; 
					info.Text = GettextCatalog.GetString ("Build {0}", name);
/*					if (entry is Combine)
						info.Description = GettextCatalog.GetString ("Build Solution {0}", name);
					else if (entry is Project)*/
						info.Description = GettextCatalog.GetString ("Build Project {0}", name);
/*					else
						info.Description = info.Text;*/
				} else {
					info.Enabled = false;
				}
			} else {
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget) {
					info.Enabled = ProjectService.CurrentBuildOperation.IsCompleted;
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
			if (MonoDevelop.Ide.Projects.ProjectService.Solution != null) {
				if (MonoDevelop.Ide.Projects.ProjectService.ActiveProject != null)
					MonoDevelop.Ide.Projects.ProjectService.RebuildProject (MonoDevelop.Ide.Projects.ProjectService.ActiveProject);
			}
			else if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.Save ();
				IdeApp.Workbench.ActiveDocument.Build ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			if (MonoDevelop.Ide.Projects.ProjectService.Solution != null) {
				if (MonoDevelop.Ide.Projects.ProjectService.ActiveProject != null) {
					info.Enabled = MonoDevelop.Ide.Projects.ProjectService.CurrentBuildOperation.IsCompleted;
					info.Text = info.Description = GettextCatalog.GetString ("Rebuild {0}", MonoDevelop.Ide.Projects.ProjectService.ActiveProject.Name);
				} else {
					info.Enabled = false;
				}
			} else {
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsBuildTarget) {
					info.Enabled = ProjectService.CurrentBuildOperation.IsCompleted;
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
			MonoDevelop.Ide.Projects.ProjectService.BuildSolution ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = MonoDevelop.Ide.Projects.ProjectService.Solution != null && MonoDevelop.Ide.Projects.ProjectService.CurrentBuildOperation.IsCompleted;
		}
	}
	
	internal class RebuildSolutionHandler: CommandHandler
	{
		protected override void Run ()
		{
			MonoDevelop.Ide.Projects.ProjectService.RebuildSolution ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = MonoDevelop.Ide.Projects.ProjectService.Solution != null && MonoDevelop.Ide.Projects.ProjectService.CurrentBuildOperation.IsCompleted;
		}
	}
	
	internal class CleanSolutionHandler: CommandHandler
	{
		protected override void Run ()
		{
			MonoDevelop.Ide.Projects.ProjectService.CleanSolution ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = MonoDevelop.Ide.Projects.ProjectService.Solution != null;
		}
	}
	
	internal class CleanHandler: CommandHandler
	{
		protected override void Run ()
		{
			MonoDevelop.Ide.Projects.ProjectService.CleanProject (MonoDevelop.Ide.Projects.ProjectService.ActiveProject);
		}
		
		protected override void Update (CommandInfo info)
		{
			if (MonoDevelop.Ide.Projects.ProjectService.ActiveProject != null) {
				info.Enabled = MonoDevelop.Ide.Projects.ProjectService.ActiveProject != null;
				info.Text = info.Description = GettextCatalog.GetString ("Clean {0}", Path.GetFileNameWithoutExtension (MonoDevelop.Ide.Projects.ProjectService.ActiveProject.Project.FileName));
			} else {
				info.Enabled = false;
			}
		}
	}
	
	internal class StopHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (!ProjectService.CurrentBuildOperation.IsCompleted)
				ProjectService.CurrentBuildOperation.Cancel ();
			if (!ProjectService.CurrentRunOperation.IsCompleted)
				ProjectService.CurrentRunOperation.Cancel ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = !ProjectService.CurrentBuildOperation.IsCompleted ||
							!ProjectService.CurrentRunOperation.IsCompleted;
		}
	}
		
	internal class CombineEntryOptionsHandler: CommandHandler
	{
		protected override void Run ()
		{
/* TODO: Project Conversion			
CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
			if (ce != null)
				IdeApp.ProjectOperations.ShowOptions (ce);*/
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ProjectService.ActiveProject != null;
		}
	}
	
	internal class CustomCommandListHandler: CommandHandler
	{
		protected override void Run (object c)
		{
// TODO: Project Conversion
//			IProject ce = ProjectService.ActiveProject.Project;
//			CustomCommand cmd = (CustomCommand) c;
//			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
//			
//			Thread t = new Thread (
//				delegate () {
//					using (monitor) {
//						cmd.Execute (monitor, ce);
//					}
//				}
//			);
//			t.IsBackground = true;
//			t.Start ();
		}
		
		protected override void Update (CommandArrayInfo info)
		{
// TODO: Project Conversion.
//			IProject ce = ProjectService.ActiveProject.Project;
//			if (ce != null) {
//				AbstractConfiguration conf = ce.ActiveConfiguration as AbstractConfiguration;
//				if (conf != null) {
//					foreach (CustomCommand cmd in conf.CustomCommands)
//						if (cmd.Type == CustomCommandType.Custom)
//							info.Add (cmd.Name, cmd);
//				}
//			}
		}
	}
	
	internal class ExportProjectHandler: CommandHandler
	{
		protected override void Run ()
		{
// TODO: Project Conversion.
//			CombineEntry ce = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
//			IdeApp.ProjectOperations.Export (ce, null);
		}
	}
	
	internal class GenerateProjectDocumentation : CommandHandler
	{
		protected override void Run ()
		{
			try {
				if (ProjectService.ActiveProject != null) {
					string assembly    = ProjectService.GetOutputFileName (ProjectService.ActiveProject.Project);
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
