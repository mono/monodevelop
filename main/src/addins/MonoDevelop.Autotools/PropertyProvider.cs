
using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.DesignerSupport;

namespace MonoDevelop.Autotools
{
	public class PropertyProvider: IPropertyProvider
	{
		public bool SupportsObject (object obj)
		{
			ProjectFile file = obj as ProjectFile;
			if (file != null && file.Project != null) {
				MakefileData data = file.Project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
				if (data != null && data.IsFileIntegrationEnabled (file.BuildAction))
					return true;
			}
			return false;
		}

		public object CreateProvider (object obj)
		{
			return new ProjectFileWrapper ((ProjectFile) obj);
		}
	}
	
	class ProjectFileWrapper
	{
		ProjectFile file;
		MakefileData data;
		
		public ProjectFileWrapper (ProjectFile file)
		{
			this.file = file;
			data = file.Project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
		}
		
		[LocalizedCategory ("Makefile Integration")]
		[LocalizedDisplayName ("Include in Makefile")]
		[LocalizedDescription ("Include this file in the file list of the synchronized Makefile")]
		public bool IncludeInMakefile {
			get {
				return !data.IsFileExcluded (file.FilePath); 
			}
			set {
				data.SetFileExcluded (file.FilePath, !value);
			}
		}
	}
}
