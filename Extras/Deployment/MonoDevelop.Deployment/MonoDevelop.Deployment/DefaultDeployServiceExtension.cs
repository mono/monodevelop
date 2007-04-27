
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	class DefaultDeployServiceExtension: DeployServiceExtension
	{
		public override DeployFileCollection GetDeployFiles (DeployContext ctx, CombineEntry entry)
		{
			if (entry is IDeployable)
				return new DeployFileCollection (((IDeployable)entry).GetDeployFiles ());
			
			return base.GetDeployFiles (ctx, entry);
		}
		
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project)
		{
			DeployFileCollection deployFiles = new DeployFileCollection ();
			
			// Add the compiled output file
			
			string outputFile = project.GetOutputFileName ();
			if (!string.IsNullOrEmpty (outputFile))
				deployFiles.Add (new DeployFile (project, outputFile, Path.GetFileName (outputFile), TargetDirectory.ProgramFiles));
			
			// Collect deployable files
			
			foreach (ProjectFile file in project.ProjectFiles) {
				if (file.BuildAction == BuildAction.FileCopy) {
					DeployFile dp = new DeployFile (file);
					deployFiles.Add (dp);
					
					if (Path.GetFileName (dp.SourcePath) == "app.config" && Path.GetFileName (dp.RelativeTargetPath) == "app.config") {
						string newName = Path.GetFileName (outputFile) + ".config";
						dp.RelativeTargetPath = Path.Combine (Path.GetDirectoryName (dp.RelativeTargetPath), newName);
					}
				}
			}

			// Collect referenced assemblies
			
			foreach (string refFile in project.GetReferenceDeployFiles (false)) {
				deployFiles.Add (new DeployFile (project, refFile, Path.GetFileName (refFile), TargetDirectory.ProgramFiles));
			}
			
			return deployFiles;
		}
	}
}


