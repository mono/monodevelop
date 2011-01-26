
using System;
using System.Collections.Specialized;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins.Description;
using Mono.Addins;
using MonoDevelop.Ide;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinProjectExtension: ProjectServiceExtension
	{
		static bool buildingSolution;
		
		public override bool SupportsItem (IBuildTarget item)
		{
			return IdeApp.IsInitialized && (item is Solution);
		}
		
		protected override BuildResult Build (IProgressMonitor monitor, Solution solution, ConfigurationSelector configuration)
		{
			try {
				buildingSolution = true;
				BuildResult res = base.Build (monitor, solution, configuration);
				if (res.ErrorCount == 0) {
					SolutionAddinData data = solution.GetAddinData ();
					if (data != null && data.Registry != null) {
						data.Registry.Update (new ProgressStatusMonitor (monitor));
						DispatchService.GuiDispatch (delegate {
							data.NotifyChanged ();
						});
					}
				}
				return res;
			} finally {
				buildingSolution = false;
			}
		}

		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem entry, ConfigurationSelector configuration)
		{
			DotNetProject project = entry as DotNetProject;
			AddinData data = project != null ? AddinData.GetAddinData (project) : null;
			if (data != null)
				monitor.BeginTask (null, buildingSolution ? 2 : 3);
			
			BuildResult res = base.Build (monitor, entry, configuration);
			if (res.ErrorCount > 0 || data == null)
				return res;
			
			monitor.Step (1);
			
			monitor.Log.WriteLine (AddinManager.CurrentLocalizer.GetString ("Verifying add-in description..."));
			string fileName = data.AddinManifestFileName;
			ProjectFile file = data.Project.Files.GetFile (fileName);
			if (file == null)
				return res;
			
			string addinFile;
			if (file.BuildAction == BuildAction.EmbeddedResource)
				addinFile = project.GetOutputFileName (ConfigurationSelector.Default);
			else
				addinFile = file.FilePath;
			
			AddinDescription desc = data.AddinRegistry.GetAddinDescription (new ProgressStatusMonitor (monitor), addinFile);
			StringCollection errors = desc.Verify ();
			
			foreach (string err in errors) {
				res.AddError (data.AddinManifestFileName, 0, 0, "", err);
				monitor.Log.WriteLine ("ERROR: " + err);
			}
			
			if (!buildingSolution && project.ParentSolution != null) {
				monitor.Step (1);
				SolutionAddinData sdata = project.ParentSolution.GetAddinData ();
				if (sdata != null && sdata.Registry != null) {
					sdata.Registry.Update (new ProgressStatusMonitor (monitor));
					DispatchService.GuiDispatch (delegate {
						sdata.NotifyChanged ();
					});
				}
			}
			
			monitor.EndTask ();
			
			return res;
		}
		
		public override void Save (IProgressMonitor monitor, SolutionEntityItem entry)
		{
			base.Save (monitor, entry);
			
			DotNetProject project = entry as DotNetProject;
			if (project != null) {
				AddinData data = AddinData.GetAddinData (project);
				if (data != null) {
					Gtk.Application.Invoke (delegate {
						data.CheckOutputPath ();
					});
				}
			}
		}
		
		ExecutionCommand CreateCommand (SolutionEntityItem item)
		{
			DotNetProject project = item as DotNetProject;
			if (project == null || project.CompileTarget != CompileTarget.Library || project.ParentSolution == null)
				return null;
			
			SolutionAddinData sdata = project.ParentSolution.GetAddinData ();
			if (sdata == null || project.GetAddinData () == null || project.GetAddinData ().IsRoot)
				return null;
				
			RegistryInfo ri = sdata.ExternalRegistryInfo;
			if (ri == null || string.IsNullOrEmpty (ri.TestCommand))
				return null;

			FilePath cmd;
			string args;
			if (ri.TestCommand [0] == '"') {
				// If the file name is quoted, unquote it
				int i = ri.TestCommand.IndexOf ('"', 1);
				if (i == -1)
					throw new UserException ("Invalid add-in test command: " + ri.TestCommand);
				cmd = ri.TestCommand.Substring (1, i - 1);
				args = ri.TestCommand.Substring (i + 1).Trim ();
			} else {
				int i = ri.TestCommand.IndexOf (' ');
				if (i == -1) {
					cmd = ri.TestCommand;
					args = string.Empty;
				} else {
					cmd = ri.TestCommand.Substring (0, i);
					args = ri.TestCommand.Substring (i + 1).Trim ();
				}
			}
			
			// If the command is an absolute file, take it
			// It not, consider it is a file relative to the startup path
			// If a relative file can't be found, use it as is
			
			if (!cmd.IsAbsolute) {
				FilePath absCmd = cmd.ToAbsolute (ri.ApplicationPath);
				if (System.IO.File.Exists (absCmd))
					cmd = absCmd;
			}
			
			ProcessExecutionCommand pcmd = Runtime.ProcessService.CreateCommand (cmd) as ProcessExecutionCommand;
			if (pcmd == null)
				return null;
			pcmd.Arguments = args;
			pcmd.EnvironmentVariables ["MONO_ADDINS_REGISTRY"] = sdata.TestRegistryPath;
			return pcmd;
		}
		
		protected override bool CanExecute (SolutionEntityItem item, ExecutionContext context, ConfigurationSelector configuration)
		{
			ExecutionCommand cmd = CreateCommand (item);
			if (cmd != null && context.ExecutionHandler.CanExecute (cmd))
				return true;
			return base.CanExecute (item, context, configuration);
		}

		protected override void Execute (IProgressMonitor monitor, SolutionEntityItem item, ExecutionContext context, ConfigurationSelector configuration)
		{
			ExecutionCommand cmd = CreateCommand (item);
			if (cmd != null && context.ExecutionHandler.CanExecute (cmd)) {
				item.ParentSolution.GetAddinData ().SetupTestRegistry ();
				context.ExecutionHandler.Execute (cmd, context.ConsoleFactory.CreateConsole (true));
			}
			base.Execute (monitor, item, context, configuration);
		}

	}
}
