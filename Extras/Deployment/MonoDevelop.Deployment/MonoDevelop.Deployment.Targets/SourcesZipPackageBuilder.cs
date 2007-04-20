
using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Deployment
{
	public class SourcesZipPackageBuilder: PackageBuilder
	{
		[ProjectPathItemProperty]
		string targetFile;

		[ItemProperty]
		string format;
		
		IFileFormat fileFormat;
		
		public override string Description {
			get { return "Archive of Sources"; }
		}
		
		public IFileFormat FileFormat {
			get {
				if (fileFormat == null) {
					if (string.IsNullOrEmpty (format))
						return null;
					foreach (IFileFormat f in Services.ProjectService.FileFormats.GetAllFileFormats ()) {
						if (f.GetType ().FullName == format) {
							fileFormat = f;
							break;
						}
					}
				}
				return fileFormat; 
			}
			set {
				fileFormat = value; 
				if (fileFormat == null)
					format = null;
				else
					format = fileFormat.GetType().FullName;
			}
		}
		
		public string TargetFile {
			get { return targetFile != null ? targetFile : string.Empty; }
			set { targetFile = value; }
		}
		
		protected override void OnBuild (IProgressMonitor monitor, DeployContext ctx)
		{
			CombineEntry entry = RootCombineEntry;
			string sourceFile = entry.FileName;
			
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor ();
			mon.AddSlaveMonitor (monitor, MonitorAction.WriteLog|MonitorAction.ReportError|MonitorAction.ReportWarning|MonitorAction.ReportSuccess);
			
			string tmpFolder = Runtime.FileService.CreateTempDirectory ();
			
			try {
				string tf = Path.GetFileNameWithoutExtension (targetFile);
				if (tf.EndsWith (".tar")) tf = Path.GetFileNameWithoutExtension (tf);
				
				string folder = Runtime.FileService.GetFullPath (Path.Combine (tmpFolder, tf));
				Directory.CreateDirectory (folder);
				
				// Export the project
				
				Services.ProjectService.Export (mon, sourceFile, folder, FileFormat);
				
				// Create the archive
				string td = Path.GetDirectoryName (targetFile);
				if (!Directory.Exists (td))
					Directory.CreateDirectory (td);
				DeployService.CreateArchive (mon, tmpFolder, targetFile);
			}
			finally {
				Directory.Delete (tmpFolder, true);
			}
			if (monitor.AsyncOperation.Success)
				monitor.Log.WriteLine (GettextCatalog.GetString ("Created file: {0}", targetFile));
		}
		
		public override void InitializeSettings (CombineEntry entry)
		{
			targetFile = Path.Combine (entry.BaseDirectory, entry.Name) + ".tar.gz";
		}

		
		public override string Validate ()
		{
			if (string.IsNullOrEmpty (TargetFile))
				return GettextCatalog.GetString ("Target file name not provided.");
			if (fileFormat == null)
				return GettextCatalog.GetString ("File format not provided.");
			return null;
		}
		
		public override void CopyFrom (PackageBuilder other)
		{
			base.CopyFrom (other);
			SourcesZipPackageBuilder builder = (SourcesZipPackageBuilder) other;
			targetFile = builder.targetFile;
			format = builder.format;
			fileFormat = builder.fileFormat;
		}
	}
}
