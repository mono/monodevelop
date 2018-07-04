using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Deployment
{
	class DefaultDeployServiceExtension: DeployServiceExtension
	{
		public override DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionFolderItem entry, ConfigurationSelector configuration)
		{
			if (entry is IDeployable)
				return new DeployFileCollection (((IDeployable)entry).GetDeployFiles (configuration));
			
			return base.GetDeployFiles (ctx, entry, configuration);
		}
		
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project, ConfigurationSelector configuration)
		{
			var deployFiles = new DeployFileCollection ();
			base.GetProjectDeployFiles (ctx, project, configuration);
			
			// Add the compiled output files
			
			var pconf = (ProjectConfiguration) project.GetConfiguration (configuration);
			var evalCtx = new TargetEvaluationContext ();
			evalCtx.ItemsToEvaluate.Add ("AllPublishItemsFullPathWithTargetPath");

			if (project.MSBuildProject.UseMSBuildEngine) {
				var result = project.RunTarget (null, "GetCopyToPublishDirectoryItems", configuration, evalCtx).Result;
				foreach (var item in result.Items) {
					if (item.Name == "AllPublishItemsFullPathWithTargetPath") {
						var fromPath = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, item.Include);
						var toPath = item.Metadata.GetPathValue ("TargetPath", relativeToPath: pconf.OutputDirectory);
						deployFiles.Add (new DeployFile (project, fromPath, toPath, TargetDirectory.ProgramFiles));
					}
				}
			} else {
#pragma warning disable 618 //obsolete
				FilePath outDir = pconf.OutputDirectory;
				foreach (FilePath file in project.GetOutputFiles (configuration)) {
					deployFiles.Add (new DeployFile (project, file, file.ToRelative (outDir), TargetDirectory.ProgramFiles));
				}
				foreach (FileCopySet.Item item in project.GetSupportFileList (configuration)) {
					deployFiles.Add (new DeployFile (project, item.Src, item.Target, TargetDirectory.ProgramFiles));
				}
#pragma warning restore 618
			}
			
			// Collect deployable files
			foreach (ProjectFile file in project.Files) {
				// skip CopyToOutputDirectory files when it's just a project build, because 
				// MonoDevelop.Project.Projects already copies these files using more subtle overwriting
				// semantics
				if (file.CopyToOutputDirectory != FileCopyMode.None)
					continue;
				    
				var props = new DeployProperties (file);
				if (props.ShouldDeploy) {
					var dp = new DeployFile (file);
					deployFiles.Add (dp);
				}
			}

			return deployFiles;
		}
	}
}


