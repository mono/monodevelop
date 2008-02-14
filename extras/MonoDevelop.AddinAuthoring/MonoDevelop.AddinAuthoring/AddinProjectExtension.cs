
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
		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			ICompilerResult res = base.Build (monitor, entry);
			if (res.ErrorCount > 0 || !(entry is DotNetProject))
				return res;
			
			DotNetProject project = (DotNetProject) entry;
			AddinData data = AddinData.GetAddinData (project);
			if (data == null)
				return res;
			
			monitor.Log.WriteLine (AddinManager.CurrentLocalizer.GetString ("Verifying add-in description"));
			string fileName = data.AddinManifestFileName;
			ProjectFile file = data.Project.ProjectFiles.GetFile (fileName);
			if (file == null)
				return res;
			
			string addinFile;
			if (file.BuildAction == BuildAction.EmbedAsResource)
				addinFile = project.GetOutputFileName ();
			else
				addinFile = file.FilePath;
			
			AddinDescription desc = data.AddinRegistry.GetAddinDescription (new ProgressStatusMonitor (monitor), addinFile);
			StringCollection errors = desc.Verify ();
			
			foreach (string err in errors)
				res.AddError (err);
			
			return res;
		}

	}
}
