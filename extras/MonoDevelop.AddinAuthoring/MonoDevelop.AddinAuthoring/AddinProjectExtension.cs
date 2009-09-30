
using System;
using System.Collections.Specialized;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins.Description;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinProjectExtension: ProjectServiceExtension
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			BuildResult res = base.Build (monitor, entry, configuration);
			if (res.ErrorCount > 0 || !(entry is DotNetProject))
				return res;
			
			DotNetProject project = (DotNetProject) entry;
			AddinData data = AddinData.GetAddinData (project);
			if (data == null)
				return res;
			
			monitor.Log.WriteLine (AddinManager.CurrentLocalizer.GetString ("Verifying add-in description..."));
			string fileName = data.AddinManifestFileName;
			ProjectFile file = data.Project.Files.GetFile (fileName);
			if (file == null)
				return res;
			
			string addinFile;
			if (file.BuildAction == BuildAction.EmbeddedResource)
				addinFile = project.GetOutputFileName (project.DefaultConfigurationId);
			else
				addinFile = file.FilePath;
			
			AddinDescription desc = data.AddinRegistry.GetAddinDescription (new ProgressStatusMonitor (monitor), addinFile);
			StringCollection errors = desc.Verify ();
			
			foreach (string err in errors) {
				res.AddError (data.AddinManifestFileName, 0, 0, "", err);
				monitor.Log.WriteLine ("ERROR: " + err);
			}
			
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
		
		protected override bool CanExecute (SolutionEntityItem item, ExecutionContext context, string configuration)
		{
			ExecutionCommand cmd = CreateCommand (item);
			if (cmd != null && context.ExecutionHandler.CanExecute (cmd))
				return true;
			return base.CanExecute (item, context, configuration);
		}

		protected override void Execute (IProgressMonitor monitor, SolutionEntityItem item, ExecutionContext context, string configuration)
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
