
using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Deployment.Targets
{
	public class SourcesZipPackageBuilder: PackageBuilder
	{
		[ProjectPathItemProperty]
		string targetFile;

		[ItemProperty]
		string format;
		
		MSBuildFileFormat fileFormat;
		
		public override string Description {
			get { return "Archive of Sources"; }
		}
		
		public override bool CanBuild (SolutionFolderItem entry)
		{
			return entry is SolutionFolder || entry is SolutionItem;
		}

		
		public MSBuildFileFormat FileFormat {
			get {
				if (fileFormat == null) {
					if (string.IsNullOrEmpty (format))
						return null;
					foreach (var f in MSBuildFileFormat.GetSupportedFormats ()) {
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
		
		protected override bool OnBuild (ProgressMonitor monitor, DeployContext ctx)
		{
			string sourceFile;
			SolutionFolderItem entry = RootSolutionItem;
			if (entry is SolutionFolder)
				sourceFile = entry.ParentSolution.FileName;
			else
				sourceFile = ((SolutionItem)entry).FileName;
			
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor ();
			mon.AddSlaveMonitor (monitor, MonitorAction.WriteLog|MonitorAction.ReportError|MonitorAction.ReportWarning|MonitorAction.ReportSuccess);
			
			string tmpFolder = FileService.CreateTempDirectory ();
			
			try {
				string tf = Path.GetFileNameWithoutExtension (targetFile);
				if (tf.EndsWith (".tar")) tf = Path.GetFileNameWithoutExtension (tf);
				
				string folder = FileService.GetFullPath (Path.Combine (tmpFolder, tf));
				Directory.CreateDirectory (folder);
				
				// Export the project
				
				SolutionFolderItem[] ents = GetChildEntries ();
				string[] epaths = new string [ents.Length];
				for (int n=0; n<ents.Length; n++)
					epaths [n] = ents [n].ItemId;
				
				var r = Services.ProjectService.Export (mon, sourceFile, epaths, folder, FileFormat).Result;
				if (string.IsNullOrEmpty (r))
					return false;
				
				// Create the archive
				string td = Path.GetDirectoryName (targetFile);
				if (!Directory.Exists (td))
					Directory.CreateDirectory (td);
				DeployService.CreateArchive (mon, tmpFolder, targetFile);
			}
			finally {
				Directory.Delete (tmpFolder, true);
			}
			monitor.Log.WriteLine (GettextCatalog.GetString ("Created file: {0}", targetFile));
			return true;
		}
		
		public override void InitializeSettings (SolutionFolderItem entry)
		{
			targetFile = Path.Combine (entry.BaseDirectory, entry.Name) + ".tar.gz";
			if (entry.ParentSolution != null)
				fileFormat = entry.ParentSolution.FileFormat;
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
		
		public override DeployContext CreateDeployContext ()
		{
			return null;
		}

		public override string DefaultName {
			get {
				if (FileFormat != null)
					return GettextCatalog.GetString ("{0} Sources", FileFormat.Name);
				else
					return base.DefaultName;
			}
		}
		
		public override PackageBuilder[] CreateDefaultBuilders ()
		{
			List<PackageBuilder> list = new List<PackageBuilder> ();

			IMSBuildFileObject root = RootSolutionItem is SolutionItem ? (IMSBuildFileObject)RootSolutionItem : (IMSBuildFileObject) RootSolutionItem.ParentSolution;
			foreach (MSBuildFileFormat format in MSBuildFileFormat.GetSupportedFormats (root)) {
				SourcesZipPackageBuilder pb = (SourcesZipPackageBuilder) Clone ();
				pb.FileFormat = format;
				
				// The suffix for the archive will be the extension of the file format.
				// If there is no extension, use the whole file name.
				string fname = format.GetValidFormatName (RootSolutionItem, RootSolutionItem.ParentSolution.FileName);
				string suffix = Path.GetExtension (fname);
				if (suffix.Length > 0)
					suffix = suffix.Substring (1).ToLower (); // Remove the initial dot
				else
					suffix = Path.GetFileNameWithoutExtension (suffix).ToLower ();
				
				// Change the name in the target file
				string ext = DeployService.GetArchiveExtension (pb.TargetFile);
				string fn = TargetFile.Substring (0, TargetFile.Length - ext.Length);
				pb.TargetFile = fn + "-" + suffix + ext;
				list.Add (pb);
			}
			return list.ToArray ();
		}
	}
}
