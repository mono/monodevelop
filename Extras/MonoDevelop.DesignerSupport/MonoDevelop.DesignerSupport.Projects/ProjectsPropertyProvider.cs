
using System;
using System.ComponentModel;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.DesignerSupport.Projects
{
	public class ProjectsPropertyProvider: IPropertyProvider
	{
		public object CreateProvider (object obj)
		{
			return new ProjectFileWrapper ((ProjectFile)obj);
		}

		public bool SupportsObject (object obj)
		{
			return obj is ProjectFile;
		}
	}
	
	class ProjectFileWrapper
	{
		ProjectFile file;
		
		public ProjectFileWrapper (ProjectFile file)
		{
			this.file = file;
		}

//TODO: Project Conversion ?	
//		[Category ("Build")]
//		public BuildAction BuildAction {
//			get { return file.BuildAction; }
//			set { file.BuildAction = value; }
//		}
//		
//		[Category ("Build")]
//		public string ResourceId {
//			get { return file.ResourceId; }
//			set { file.ResourceId = value; }
//		}
	}
}
