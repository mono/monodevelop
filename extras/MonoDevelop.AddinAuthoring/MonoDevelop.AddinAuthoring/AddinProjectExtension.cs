
using System;
using System.Collections.Specialized;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins.Description;

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
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Verifying add-in description"));
			ProjectFile file = data.GetAddinManifestFile ();
			if (file == null)
				return res;
			
			string addinFile;
			if (file.BuildAction == BuildAction.EmbedAsResource)
				addinFile = project.GetOutputFileName ();
			else
				addinFile = file.FilePath;
			
			AddinDescription desc = data.AddinRegistry.GetAddinDescription (new ProgressStatusMonitor (monitor), addinFile);
			Console.WriteLine ("pp: " + addinFile);
			Console.WriteLine ("pp1: " + desc.AddinFile);
			StringCollection errors = desc.Verify ();
			
			foreach (string err in errors)
				res.AddError (err);
			
			return res;
		}

	}
}
