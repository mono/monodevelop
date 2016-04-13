
using System;
using System.IO;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment
{
	public class DeployProperties
	{
		ProjectFile file;
		
		internal DeployProperties (ProjectFile file)
		{
			this.file = file;
		}
		
		public bool ShouldDeploy {
			get {
				if (MarkedCopyToOutput)
					return true;

				return file.Metadata.GetValue<bool> ("DeployService.Deploy", false);
			}
			set {
				AssertNotCopyToOutput ();
				file.Metadata.SetValue ("DeployService.Deploy", value, false);
			}
		
		}
		
		public string TargetDirectory {
			get {
				if (MarkedCopyToOutput)
					return MonoDevelop.Deployment.TargetDirectory.ProgramFiles;
				return file.Metadata.GetValue ("DeployService.TargetDirectoryId", MonoDevelop.Deployment.TargetDirectory.ProgramFiles);
			}
			set {
				AssertNotCopyToOutput ();
				if (string.IsNullOrEmpty (value))
					value = MonoDevelop.Deployment.TargetDirectory.ProgramFiles;
				file.Metadata.SetValue ("DeployService.TargetDirectoryId", value, MonoDevelop.Deployment.TargetDirectory.ProgramFiles);
			}
		}
		
		public string RelativeDeployPath {
			get {
				if (MarkedCopyToOutput)
					return Path.GetFileName (file.Name);
				
				if (UseProjectRelativePath)
					return file.ProjectVirtualPath;
				return file.Metadata.GetValue ("DeployService.RelativeDeployPath", Path.GetFileName (file.Name));
			}
			set {
				AssertNotCopyToOutput ();
				var defname = Path.GetFileName (file.Name);
				if (string.IsNullOrEmpty (value))
					value = defname;
				file.Metadata.SetValue ("DeployService.RelativeDeployPath", value, defname);
			}
		}
		
		public bool HasPathReferences {
			get {
				if (MarkedCopyToOutput)
					return false;
				return file.Metadata.GetValue ("DeployService.HasPathReferences", false);
			}
			set {
				AssertNotCopyToOutput ();
				file.Metadata.SetValue ("DeployService.HasPathReferences", value, false);
			}
		}
		
		// When set, the file will be deployed to the same relative path it has in the project.
		public bool UseProjectRelativePath {
			get {
				if (MarkedCopyToOutput)
					return false;
				
				return file.Metadata.GetValue ("DeployService.UseProjectRelativePath", false);
			}
			set {
				AssertNotCopyToOutput ();
				if (value)
					RelativeDeployPath = "";
				file.Metadata.SetValue ("DeployService.UseProjectRelativePath", value, false);
			}
		}
		
		public DeployFileAttributes FileAttributes {
			get {
				if (MarkedCopyToOutput)
					return DeployFileAttributes.None;

				return file.Metadata.GetValue ("DeployService.FileAttributes", DeployFileAttributes.None);
			}
			set {
				AssertNotCopyToOutput ();
				file.Metadata.SetValue ("DeployService.FileAttributes", value, DeployFileAttributes.None);
			}
		}
		
		void AssertNotCopyToOutput ()
		{
			if (MarkedCopyToOutput)
				throw new InvalidOperationException ("Cannot change value when CopyToOutputDirectory is set");
		}
		
		bool MarkedCopyToOutput {
			get { return file.CopyToOutputDirectory != FileCopyMode.None; }
		}
	}
}
