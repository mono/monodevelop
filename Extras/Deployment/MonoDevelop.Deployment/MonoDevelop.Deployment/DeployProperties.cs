
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
				return file.ExtendedProperties ["DeployService.TargetDirectoryId"] as string;
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
	}
}
