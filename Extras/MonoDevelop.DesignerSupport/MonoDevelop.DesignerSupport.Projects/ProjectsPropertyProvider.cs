
using System;
using System.ComponentModel;
using MonoDevelop.Projects;

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
	
	class ProjectFileWrapper: CustomDescriptor
	{
		ProjectFile file;
		
		public ProjectFileWrapper (ProjectFile file)
		{
			this.file = file;
		}
		
		[Category ("Build")]
		[DisplayName ("Build action")]
		[Description ("Action to perform when building this file.")]
		public BuildAction BuildAction {
			get { return file.BuildAction; }
			set { file.BuildAction = value; }
		}
		
		[Category ("Build")]
		[DisplayName ("Resource ID")]
		[Description ("Identifier of the embedded resource.")]
		public string ResourceId {
			get { return file.ResourceId; }
			set { file.ResourceId = value; }
		}
		
		protected override bool IsReadOnly (string propertyName)
		{
			if (propertyName == "ResourceId" && file.BuildAction != BuildAction.EmbedAsResource)
				return true;
			return false;
		}
	}
}
