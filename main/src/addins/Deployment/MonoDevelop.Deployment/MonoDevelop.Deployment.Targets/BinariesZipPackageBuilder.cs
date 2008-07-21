
using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Deployment.Targets
{
	public class BinariesZipPackageBuilder: PackageBuilder
	{
		[ProjectPathItemProperty]
		string targetFile;
		
		[ItemProperty]
		string platform;
		
		[ItemProperty]
		string configuration;
		
		public string TargetFile {
			get { return targetFile != null ? targetFile : string.Empty; }
			set { targetFile = value; }
		}
		
		public string Platform {
			get { return platform; }
			set { platform = value; }
		}

		public string Configuration {
			get { return configuration; }
			set { configuration = value; }
		}
		
		public override string Description {
			get { return "Archive of Binaries"; }
		}
		
		public override void InitializeSettings (SolutionItem entry)
		{
			targetFile = Path.Combine (entry.BaseDirectory, entry.Name) + ".tar.gz";
			if (entry.ParentSolution != null)
				configuration = entry.ParentSolution.DefaultConfigurationId;
		}
		
		public override string[] GetSupportedConfigurations ()
		{
			return configuration != null ? new string [] { configuration } : new string [0];
		}
		
		public override bool CanBuild (SolutionItem entry)
		{
			// Can build anything but PackagingProject
			return !(entry is PackagingProject);
		}

		public override DeployContext CreateDeployContext ()
		{
			return new DeployContext (this, platform, null);
		}
		
		protected override bool OnBuild (IProgressMonitor monitor, DeployContext ctx)
		{
			string tmpFolder = null;
			
			try {
				if (RootSolutionItem.NeedsBuilding (configuration)) {
					BuildResult res = RootSolutionItem.Build (monitor, configuration);
					if (res.ErrorCount > 0)
						return false;
				}
				
				tmpFolder = FileService.CreateTempDirectory ();
				
				string tf = Path.GetFileNameWithoutExtension (targetFile);
				if (tf.EndsWith (".tar")) tf = Path.GetFileNameWithoutExtension (tf);
				string folder = FileService.GetFullPath (Path.Combine (tmpFolder, tf));
				
				// Export the binary files
				DeployFileCollection deployFiles = GetDeployFiles (ctx, configuration);
				foreach (DeployFile file in deployFiles) {
					string tfile = Path.Combine (folder, file.ResolvedTargetFile);
					string tdir = FileService.GetFullPath (Path.GetDirectoryName (tfile));
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
			catch (Exception ex) {
				monitor.ReportError ("Package creation failed", ex);
				LoggingService.LogError ("Package creation failed", ex);
				return false;
			}
			finally {
				if (tmpFolder != null)
					Directory.Delete (tmpFolder, true);
			}
			if (monitor.AsyncOperation.Success)
				monitor.Log.WriteLine (GettextCatalog.GetString ("Created file: {0}", targetFile));
			return true;
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
			configuration = builder.configuration;
		}

		public override string DefaultName {
			get {
				foreach (DeployPlatformInfo plat in DeployService.GetDeployPlatformInfo ()) {
					if (plat.Id == Platform)
						return GettextCatalog.GetString ("{0} Binaries", plat.Description);
				}
				return base.DefaultName;
			}
		}

		public override PackageBuilder[] CreateDefaultBuilders ()
		{
			List<PackageBuilder> list = new List<PackageBuilder> ();
			foreach (DeployPlatformInfo plat in DeployService.GetDeployPlatformInfo ()) {
				BinariesZipPackageBuilder pb = (BinariesZipPackageBuilder) Clone ();
				pb.Platform = plat.Id;
				string ext = DeployService.GetArchiveExtension (pb.TargetFile);
				string fn = TargetFile.Substring (0, TargetFile.Length - ext.Length);
				pb.TargetFile = fn + "-" + plat.Id.ToLower () + ext;
				list.Add (pb);
			}
			return list.ToArray ();
		}
	}
}
