
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	class DefaultDeployServiceExtension: DeployServiceExtension
	{
		public override DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionItem entry, ConfigurationSelector configuration)
		{
			if (entry is IDeployable)
				return new DeployFileCollection (((IDeployable)entry).GetDeployFiles (configuration));
			
			return base.GetDeployFiles (ctx, entry, configuration);
		}
		
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project, ConfigurationSelector configuration)
		{
			DeployFileCollection deployFiles = new DeployFileCollection ();
			base.GetProjectDeployFiles (ctx, project, configuration);
			
			// Add the compiled output files
			
			ProjectConfiguration pconf = (ProjectConfiguration) project.GetConfiguration (configuration);
			FilePath outDir = pconf.OutputDirectory;
			foreach (FilePath file in project.GetOutputFiles (configuration)) {
				deployFiles.Add (new DeployFile (project, file, file.ToRelative (outDir), TargetDirectory.ProgramFiles));
			}
			
			FilePath outputFile = project.GetOutputFileName (configuration);
			
			// Collect deployable files
			foreach (ProjectFile file in project.Files) {
				// skip CopyToOutputDirectory files when it's just a project build, because 
				// MonoDevelop.Project.Projects already copies these files using more subtle overwriting
				// semantics
				if (file.CopyToOutputDirectory != FileCopyMode.None)
					continue;
				    
				DeployProperties props = new DeployProperties (file);
				if (props.ShouldDeploy) {
					DeployFile dp = new DeployFile (file);
					deployFiles.Add (dp);
				}
			}
			
			foreach (FileCopySet.Item item in project.GetSupportFileList (configuration)) {
				 deployFiles.Add (new DeployFile (project, item.Src, item.Target, TargetDirectory.ProgramFiles));
			}
			
			return deployFiles;
		}
	}
}


