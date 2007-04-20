
using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Deployment
{
	public class BinariesZipPackageBuilder: PackageBuilder
	{
		[ProjectPathItemProperty]
		string targetFile;
		
		[ItemProperty]
		string platform;
		
		public string TargetFile {
			get { return targetFile != null ? targetFile : string.Empty; }
			set { targetFile = value; }
		}
		
		public string Platform {
			get { return platform; }
			set { platform = value; }
		}
		
		public override string Description {
			get { return "Archive of Binaries"; }
		}
		
		public override void InitializeSettings (CombineEntry entry)
		{
			targetFile = Path.Combine (entry.BaseDirectory, entry.Name) + ".tar.gz";
		}

		public override DeployContext CreateDeployContext ()
		{
			return new DeployContext (this, platform, null);
		}
		
		protected override void OnBuild (IProgressMonitor monitor, DeployContext ctx)
		{
			string tmpFolder = null;
				
			try {
				tmpFolder = Runtime.FileService.CreateTempDirectory ();
				
				string tf = Path.GetFileNameWithoutExtension (targetFile);
				if (tf.EndsWith (".tar")) tf = Path.GetFileNameWithoutExtension (tf);
				string folder = Runtime.FileService.GetFullPath (Path.Combine (tmpFolder, tf));
				
				// Export the binary files
				DeployFileCollection deployFiles = DeployService.GetDeployFiles (ctx, GetAllEntries ());
				foreach (DeployFile file in deployFiles) {
					string tfile = Path.Combine (folder, file.ResolvedTargetFile);
					string tdir = Runtime.FileService.GetFullPath (Path.GetDirectoryName (tfile));
					if (!Directory.Exists (tdir))
						Directory.CreateDirectory (tdir);
					File.Copy (file.SourcePath, tfile, true);
				}
				
				// Create the archive
				string td = Path.GetDirectoryName (targetFile);
				if (!Directory.Exists (td))
					Directory.CreateDirectory (td);
				DeployService.CreateArchive (monitor, tmpFolder, targetFile);
				
			}
			finally {
				if (tmpFolder != null)
					Directory.Delete (tmpFolder, true);
			}
		}
		
		protected override string OnResolveDirectory (DeployContext ctx, string folderId)
		{
			return ".";
		}
		
		public override void CopyFrom (PackageBuilder other)
		{
			base.CopyFrom (other);
			BinariesZipPackageBuilder builder = (BinariesZipPackageBuilder) other;
			targetFile = builder.targetFile;
			platform = builder.platform;
		}

	}
}
