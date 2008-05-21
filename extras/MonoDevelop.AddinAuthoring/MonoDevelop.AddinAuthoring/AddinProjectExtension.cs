
using System;
using System.Collections.Specialized;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins.Description;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinProjectExtension: ProjectServiceExtension
	{
		protected override ICompilerResult Build (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			ICompilerResult res = base.Build (monitor, entry, configuration);
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
			if (file.BuildAction == BuildAction.EmbedAsResource)
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
	}
}
