
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

		
		protected override void OnBuild (IProgressMonitor monitor, CombineEntry entry)
		{
			string tmpFolder = null;
			DeployContext ctx = new DeployContext (this, platform, null);
				
			try {
				tmpFolder = Runtime.FileService.CreateTempDirectory ();
				
				string tf = Path.GetFileNameWithoutExtension (targetFile);
				if (tf.EndsWith (".tar")) tf = Path.GetFileNameWithoutExtension (tf);
				string folder = Runtime.FileService.GetFullPath (Path.Combine (tmpFolder, tf));
				
				CopyFiles (ctx, entry, folder);
				
				// Create the archive
				string td = Path.GetDirectoryName (targetFile);
				if (!Directory.Exists (td))
					Directory.CreateDirectory (td);
				DeployService.CreateArchive (monitor, tmpFolder, targetFile);
				
			}
			finally {
				ctx.Dispose ();
				if (tmpFolder != null)
					Directory.Delete (tmpFolder, true);
			}
		}
		
		void CopyFiles (DeployContext ctx, CombineEntry entry, string folder)
		{
			// Export the binary files
			DeployFileCollection deployFiles = DeployService.GetDeployFiles (ctx, entry);
			foreach (DeployFile file in deployFiles) {
				string tfile = Path.Combine (folder, file.ResolvedTargetFile);
				string tdir = Runtime.FileService.GetFullPath (Path.GetDirectoryName (tfile));
				if (!Directory.Exists (tdir))
					Directory.CreateDirectory (tdir);
				File.Copy (file.SourcePath, tfile, true);
			}
			
			Combine c = entry as Combine;
			if (c != null) {
				foreach (CombineEntry ce in c.Entries) {
					CopyFiles (ctx, ce, folder);
				}
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
