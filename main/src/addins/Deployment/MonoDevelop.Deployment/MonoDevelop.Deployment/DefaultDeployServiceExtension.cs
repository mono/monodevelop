
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	class DefaultDeployServiceExtension: DeployServiceExtension
	{
		public override DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionItem entry, string configuration)
		{
			if (entry is IDeployable)
				return new DeployFileCollection (((IDeployable)entry).GetDeployFiles (configuration));
			
			return base.GetDeployFiles (ctx, entry, configuration);
		}
		
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project, string solutionConfiguration)
		{
			DeployFileCollection deployFiles = new DeployFileCollection ();
			
			// Add the compiled output file
			
			string outputFile = project.GetOutputFileName (solutionConfiguration);
			if (!string.IsNullOrEmpty (outputFile))
				deployFiles.Add (new DeployFile (project, outputFile, Path.GetFileName (outputFile), TargetDirectory.ProgramFiles));
			
			// Collect deployable files
			
			foreach (ProjectFile file in project.Files) {
				if (file.BuildAction == BuildAction.FileCopy) {
					DeployFile dp = new DeployFile (file);
					deployFiles.Add (dp);
					
					if (string.Compare (Path.GetFileName (dp.SourcePath), "app.config", true)==0 && string.Compare (Path.GetFileName (dp.RelativeTargetPath), "app.config", true)==0) {
						string newName = Path.GetFileName (outputFile) + ".config";
						dp.RelativeTargetPath = Path.Combine (Path.GetDirectoryName (dp.RelativeTargetPath), newName);
					}
				}
			}

			DotNetProject netProject = project as DotNetProject;
			if (netProject != null) {
				DotNetProjectConfiguration conf = (DotNetProjectConfiguration) project.GetActiveConfiguration (solutionConfiguration);
				
				if (conf.DebugMode) {
					string mdbFile = conf.CompiledOutputName + ".mdb";
					deployFiles.Add (new DeployFile (project, mdbFile, Path.GetFileName (mdbFile), TargetDirectory.ProgramFiles));
				}
				// Collect referenced assemblies
				foreach (string refFile in netProject.GetReferenceDeployFiles (false, solutionConfiguration)) {
					deployFiles.Add (new DeployFile (project, refFile, Path.GetFileName (refFile), TargetDirectory.ProgramFiles));
				}
			}
			
			return deployFiles;
		}
	}
}


