
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
		
		public string TargetDirectory {
			get {
				string d = file.ExtendedProperties ["DeployService.TargetDirectoryId"] as string;
				if (string.IsNullOrEmpty (d))
					return MonoDevelop.Deployment.TargetDirectory.ProgramFiles;
				else
					return d;
			}
			set {
				if (string.IsNullOrEmpty (value))
					file.ExtendedProperties.Remove ("DeployService.TargetDirectoryId");
				else
					file.ExtendedProperties ["DeployService.TargetDirectoryId"] = value;
			}
		}
		
		public string RelativeDeployPath {
			get {
				if (UseProjectRelativePath)
					return file.RelativePath;
				string s = file.ExtendedProperties ["DeployService.RelativeDeployPath"] as string;
				if (string.IsNullOrEmpty (s))
					return Path.GetFileName (file.Name);
				else
					return s;
			}
			set {
				if (string.IsNullOrEmpty (value) || value == Path.GetFileName (file.Name))
					file.ExtendedProperties.Remove ("DeployService.RelativeDeployPath");
				else
					file.ExtendedProperties ["DeployService.RelativeDeployPath"] = value;
			}
		}
		
		public bool HasPathReferences {
			get {
				object val = file.ExtendedProperties ["DeployService.HasPathReferences"];
				return val != null && (bool) val;
			}
			set {
				if (!value)
					file.ExtendedProperties.Remove ("DeployService.HasPathReferences");
				else
					file.ExtendedProperties ["DeployService.HasPathReferences"] = true;
			}
		}
		
		// When set, the file will be deployed to the same relative path it has in the project.
		public bool UseProjectRelativePath {
			get {
				object val = file.ExtendedProperties ["DeployService.UseProjectRelativePath"];
				return val != null && (bool) val;
			}
			set {
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
				object val = file.ExtendedProperties ["DeployService.FileAttributes"];
				return val != null ? (DeployFileAttributes) val : DeployFileAttributes.None;
			}
			set {
				if (value == DeployFileAttributes.None)
					file.ExtendedProperties.Remove ("DeployService.FileAttributes");
				else
					file.ExtendedProperties ["DeployService.FileAttributes"] = value;
			}
		}
	}
}
