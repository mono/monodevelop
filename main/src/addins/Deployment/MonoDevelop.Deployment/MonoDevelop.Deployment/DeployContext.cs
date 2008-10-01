
using System;
using System.Collections;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	public class DeployContext: IDisposable
	{
		string platform;
		string prefix;
		IDirectoryResolver resolver;
		PackageBuilder fileFilter;
		
		Random rand = new Random ();
		ArrayList tempFiles = new ArrayList ();
		
		public DeployContext (IDirectoryResolver resolver, string platform, string prefix)
		{
			this.platform = platform;
			this.prefix = prefix;
			this.resolver = resolver;
		}
		
		///<summary>
		///Set when the deployment is only for a project's build, becuase it only uses the ProgramFiles type.
		///</summary>
		public bool ProjectBuildOnly {
			get { return resolver is DefaultDeployServiceExtension; }
		}
		
		public string GetDirectory (string folderId)
		{
			string dir = resolver.GetDirectory (this, folderId);
			if (dir != null)
				return dir;
			else
				return DeployService.GetDeployDirectory (this, folderId);
		}
		
		public string GetResolvedPath (string folderId, string relativeTargetPath)
		{
			string dir = GetDirectory (folderId);
			if (dir == null)
				return null;
			return Path.Combine (dir, relativeTargetPath);
		}
		
		public virtual bool IncludeFile (DeployFile file)
		{
			if (fileFilter != null)
				return fileFilter.IsFileIncluded (file);
			else
				return true;
		}
		
		internal IDirectoryResolver Resolver {
			get { return resolver; }
			set { resolver = value; }
		}
		
		internal PackageBuilder FileFilter {
			get { return fileFilter; }
			set { fileFilter = value; }
		}
		
		public string Platform {
			get { return platform; }
		}
		
		public string Prefix {
			get { return prefix; }
		}
		
		public string CreateTempFile ()
		{
			return CreateTempFile ("");
		}
		
		public string CreateTempFile (string extension)
		{
			string file;
			do {
				file = Path.Combine (Path.GetTempPath (), "tmp" + rand.Next (0, int.MaxValue) + extension);
			}
			while (File.Exists (file));
			
			tempFiles.Add (file);
			return file;
		}
		
		public void Dispose ()
		{
			foreach (string file in tempFiles) {
				try {
					File.Delete (file);
				} catch (Exception ex) {
					// Ignore exception
					LoggingService.LogWarning (ex.ToString ());
				}
			}
		}
	}
}
