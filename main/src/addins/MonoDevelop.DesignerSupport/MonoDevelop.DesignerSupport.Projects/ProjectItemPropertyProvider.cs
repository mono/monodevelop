
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace MonoDevelop.DesignerSupport.Projects
{
	public class ProjectItemPropertyProvider: IPropertyProvider
	{
		public object CreateProvider (object obj)
		{
			var projectFile = obj as ProjectFile;
			if (projectFile != null)
				return new ProjectFileDescriptor (projectFile);

			var projectReference = obj as ProjectReference;
			if (projectReference != null)
				return new ProjectReferenceDescriptor (projectReference);

			return new ImplicitFrameworkAssemblyReferenceDescriptor ((ImplicitFrameworkAssemblyReference)obj);
		}

		public bool SupportsObject (object obj)
		{
			return obj is ProjectFile || obj is ProjectReference || obj is ImplicitFrameworkAssemblyReference;
		}
	}
}
