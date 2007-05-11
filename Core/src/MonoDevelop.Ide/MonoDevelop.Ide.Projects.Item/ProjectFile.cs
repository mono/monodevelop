
using System;

namespace MonoDevelop.Ide.Projects.Item
{
	public enum FileType {
		None,
		Compile,
		EmbeddedResource,
		Resource,
		Content,
		Folder
	}
	
	public class ProjectFile : ProjectItem
	{
		FileType fileType;
		
		public FileType FileType {
			get { return this.fileType; }
			set { this.fileType = value; }
		}
		
		public static FileType GetFileType (string str)
		{
			try {
				return (FileType)Enum.Parse (typeof(FileType), str);
			} catch (Exception) {
				return FileType.None;
			}
		}
		
		public string DependentUpon {
			get {
				return base.GetMetadata ("DependentUpon");
			}
			set {
				base.SetMetadata ("DependentUpon", value);
			}
		}
		
		public string SubType {
			get {
				return base.GetMetadata ("SubType");
			}
			set {
				base.SetMetadata ("SubType", value);
			}
		}
		
//		Never,
//		Always,
//		PreserveNewest
		public string CopyToOutputDirectory {
			get {
				return base.GetMetadata ("CopyToOutputDirectory");
			}
			set {
				base.SetMetadata ("CopyToOutputDirectory", value);
			}
		}
		
		public string CustomTool {
			get {
				return base.GetMetadata ("Generator");
			}
			set {
				base.SetMetadata ("Generator", value);
			}
		}
		
		public string CustomToolNamespace {
			get {
				return base.GetMetadata ("CustomToolNamespace");
			}
			set {
				base.SetMetadata ("CustomToolNamespace", value);
			}
		}
		
		public ProjectFile (FileType fileType)
		{
			this.FileType = fileType;
		}
		
	}
}
