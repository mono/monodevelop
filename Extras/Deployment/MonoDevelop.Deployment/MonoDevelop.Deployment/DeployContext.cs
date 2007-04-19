
using System;
using System.Collections;
using System.IO;

namespace MonoDevelop.Deployment
{
	public class DeployContext: IDisposable
	{
		string platform;
		string prefix;
		IDirectoryResolver resolver;
		
		Random rand = new Random ();
		ArrayList tempFiles = new ArrayList ();
		
		public DeployContext (IDirectoryResolver resolver, string platform, string prefix)
		{
			this.platform = platform;
			this.prefix = prefix;
			this.resolver = resolver;
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
		
		internal IDirectoryResolver Resolver {
			get { return resolver; }
			set { resolver = value; }
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
					Console.WriteLine (ex);
				}
			}
		}
	}
}
