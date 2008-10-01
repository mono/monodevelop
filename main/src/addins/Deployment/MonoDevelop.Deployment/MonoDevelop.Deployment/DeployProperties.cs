
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
				
				object val = file.ExtendedProperties ["DeployService.Deploy"];
				return val != null && (bool) val;
			}
			set {
				AssertNotCopyToOutput ();
				if (!value)
					file.ExtendedProperties.Remove ("DeployService.Deploy");
				else
					file.ExtendedProperties ["DeployService.Deploy"] = true;
			}
		
		}
		
		public string TargetDirectory {
			get {
				if (MarkedCopyToOutput)
					return MonoDevelop.Deployment.TargetDirectory.ProgramFiles;
				
				string d = file.ExtendedProperties ["DeployService.TargetDirectoryId"] as string;
				if (string.IsNullOrEmpty (d))
					return MonoDevelop.Deployment.TargetDirectory.ProgramFiles;
				else
					return d;
			}
			set {
				AssertNotCopyToOutput ();
				if (string.IsNullOrEmpty (value) || value == MonoDevelop.Deployment.TargetDirectory.ProgramFiles)
					file.ExtendedProperties.Remove ("DeployService.TargetDirectoryId");
				else
					file.ExtendedProperties ["DeployService.TargetDirectoryId"] = value;
			}
		}
		
		public string RelativeDeployPath {
			get {
				if (MarkedCopyToOutput)
					return Path.GetFileName (file.Name);
				
				if (UseProjectRelativePath)
					return file.RelativePath;
				string s = file.ExtendedProperties ["DeployService.RelativeDeployPath"] as string;
				if (string.IsNullOrEmpty (s))
					return Path.GetFileName (file.Name);
				else
					return s;
			}
			set {
				AssertNotCopyToOutput ();
				if (string.IsNullOrEmpty (value) || value == Path.GetFileName (file.Name))
					file.ExtendedProperties.Remove ("DeployService.RelativeDeployPath");
				else
					file.ExtendedProperties ["DeployService.RelativeDeployPath"] = value;
			}
		}
		
		public bool HasPathReferences {
			get {
				if (MarkedCopyToOutput)
					return false;
				
				object val = file.ExtendedProperties ["DeployService.HasPathReferences"];
				return val != null && (bool) val;
			}
			set {
				AssertNotCopyToOutput ();
				if (!value)
					file.ExtendedProperties.Remove ("DeployService.HasPathReferences");
				else
					file.ExtendedProperties ["DeployService.HasPathReferences"] = true;
			}
		}
		
		// When set, the file will be deployed to the same relative path it has in the project.
		public bool UseProjectRelativePath {
			get {
				if (MarkedCopyToOutput)
					return false;
				
				object val = file.ExtendedProperties ["DeployService.UseProjectRelativePath"];
				return val != null && (bool) val;
			}
			set {
				AssertNotCopyToOutput ();
				if (!value)
					file.ExtendedProperties.Remove ("DeployService.UseProjectRelativePath");
				else {
					RelativeDeployPath = "";
					file.ExtendedProperties ["DeployService.UseProjectRelativePath"] = true;
				}
			}
		}
		
		public DeployFileAttributes FileAttributes {
			get {
				if (MarkedCopyToOutput)
					return DeployFileAttributes.None;
				
				object val = file.ExtendedProperties ["DeployService.FileAttributes"];
				return val != null ? (DeployFileAttributes) val : DeployFileAttributes.None;
			}
			set {
				AssertNotCopyToOutput ();
				if (value == DeployFileAttributes.None)
					file.ExtendedProperties.Remove ("DeployService.FileAttributes");
				else
					file.ExtendedProperties ["DeployService.FileAttributes"] = value;
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
