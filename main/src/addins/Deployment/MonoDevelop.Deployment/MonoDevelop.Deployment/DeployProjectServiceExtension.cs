
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	class DeployProjectServiceExtension: ProjectServiceExtension, IDirectoryResolver
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, string configuration)
		{
			BuildResult res = base.Build (monitor, item, configuration);
			Project project = item as Project;
			if (project == null)
				return res;
			
			string outfile = project.GetOutputFileName (configuration);
			if (string.IsNullOrEmpty (outfile))
				return res;
			
			outfile = FileService.GetFullPath (outfile);
			
			// Copy deploy files with ProgramFiles as target directory
			
			using (DeployContext ctx = new DeployContext (this, DeployService.CurrentPlatform, Path.GetDirectoryName (outfile))) {
				DeployFileCollection files = DeployService.GetDeployFiles (ctx, item, configuration);
				
				foreach (DeployFile file in files) {
					if (FileService.GetFullPath (file.SourcePath) == outfile)
						continue;
					
					if (file.TargetDirectoryID == TargetDirectory.ProgramFiles) {
						if (!File.Exists (file.SourcePath)) {
							res.AddError (GettextCatalog.GetString ("File '{0}' not found", file.SourcePath));
							continue;
						}
						string tfile = file.ResolvedTargetFile;
						if (FileService.GetFullPath (tfile) != FileService.GetFullPath (file.SourcePath)) {
							string tpath = Path.GetDirectoryName (tfile);
							if (!Directory.Exists (tpath))
								Directory.CreateDirectory (tpath);
							File.Copy (file.SourcePath, tfile, true);
						}
					}
				}
			}
			return res;
		}
	
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem item, string configuration)
		{
			base.Clean (monitor, item, configuration);
			Project project = item as Project;
			if (project == null)
				return;
			
			string path = project.GetOutputFileName (configuration);
			if (string.IsNullOrEmpty (path))
				return;
			
			path = Path.GetDirectoryName (path);
			
			using (DeployContext ctx = new DeployContext (this, DeployService.CurrentPlatform, path)) {
				DeployFileCollection files = DeployService.GetDeployFiles (ctx, item, configuration);
				foreach (DeployFile file in files) {
					if (file.TargetDirectoryID == TargetDirectory.ProgramFiles) {
						string tfile = file.ResolvedTargetFile;
						if (File.Exists (tfile))
							File.Delete (tfile);
					}
				}
			}
		}
		
		public string GetDirectory (DeployContext context, string folderId)
		{
			return context.Prefix;
		}

	}
}
