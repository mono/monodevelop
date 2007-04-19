
using System;
using System.ComponentModel;
using MonoDevelop.Projects;
using MonoDevelop.DesignerSupport;

namespace MonoDevelop.Deployment.Gui
{
	class PropertyProvider: IPropertyProvider
	{
		public bool SupportsObject (object obj)
		{
			return obj is ProjectFile;
		}

		public object CreateProvider (object obj)
		{
			return new ProjectFileWrapper ((ProjectFile) obj);
		}
	}
	
	class ProjectFileWrapper
	{
		DeployProperties props;
		
		public ProjectFileWrapper (ProjectFile file)
		{
			props = DeployService.GetDeployProperties (file);
		}
		
		[Category ("Deployment")]
		[Description ("Target Directory")]
		public DeployDirectoryInfo TargetDirectory {
			get {
				string dirId = props.TargetDirectory;
				foreach (DeployDirectoryInfo di in DeployService.GetDeployDirectoryInfo ()) {
					if (di.Id == dirId)
						return di;
				}
				return null;
			}
			set {
				props.TargetDirectory = value.Id;
			}
		}
		
		[Category ("Deployment")]
		[Description ("Relative path of the file in the installation directory.")]
		public string RelativeDeployPath {
			get { return props.RelativeDeployPath; }
			set { props.RelativeDeployPath = value; }
		}
		
		[Category ("Deployment")]
		[Description ("Set to 'true' if the text file contains unresolved references to paths (e.g. @ProgramFiles@)")]
		public bool HasPathReferences {
			get { return props.HasPathReferences; }
			set { props.HasPathReferences = value; }
		}
	}
}
