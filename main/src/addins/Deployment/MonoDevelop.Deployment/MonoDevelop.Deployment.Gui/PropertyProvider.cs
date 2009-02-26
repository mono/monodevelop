
using System;
using MonoDevelop.Core;
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
	
	class ProjectFileWrapper: CustomDescriptor
	{
		DeployProperties props;
		ProjectFile file;
		
		public ProjectFileWrapper (ProjectFile file)
		{
			props = DeployService.GetDeployProperties (file);
			this.file = file;
		}
		
		[LocalizedCategory ("Deployment")]
		[LocalizedDisplayName ("Target directory")]
		[LocalizedDescription ("Target Directory")]
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
		
		[LocalizedCategory ("Deployment")]
		[LocalizedDisplayName ("Relative target path")]
		[LocalizedDescription ("Relative path of the file in the installation directory.")]
		public string RelativeDeployPath {
			get { return props.RelativeDeployPath; }
			set { props.RelativeDeployPath = value; }
		}
		
		[LocalizedCategory ("Deployment")]
		[LocalizedDisplayName ("Has path references")]
		[LocalizedDescription ("Set to 'true' if the text file contains unresolved references to paths (e.g. @ProgramFiles@)")]
		public bool HasPathReferences {
			get { return props.HasPathReferences; }
			set { props.HasPathReferences = value; }
		}
		
		[LocalizedCategory ("Deployment")]
		[LocalizedDisplayName ("Use project relative path")]
		[LocalizedDescription ("Use the relative path of the file in the project when deploying to the target directory.")]
		public bool UseProjectRelativePath {
			get { return props.UseProjectRelativePath; }
			set { props.UseProjectRelativePath = value; }
		}
		
		[LocalizedCategory ("Deployment")]
		[LocalizedDisplayName ("File attributes")]
		[LocalizedDescription ("Attributes to apply to the target file.")]
		public DeployFileAttributes FileAttributes {
			get { return props.FileAttributes; }
			set { props.FileAttributes = value; }
		}
		
		[LocalizedCategory ("Deployment")]
		[LocalizedDisplayName ("Include in deploy")]
		[LocalizedDescription ("Include the file in deployment in addition to the files included automatically.")]
		public bool ShouldDeploy {
			get { return props.ShouldDeploy; }
			set { props.ShouldDeploy = value; }
		}
		
		protected override bool IsReadOnly (string propertyName)
		{
			if (file.CopyToOutputDirectory != FileCopyMode.None)
				return true;
			
			if (!ShouldDeploy && propertyName != "ShouldDeploy")
				return true;
			if (UseProjectRelativePath) {
				if (propertyName == "RelativeDeployPath")
					return true;
			}
			return false;
		}

	}
}
