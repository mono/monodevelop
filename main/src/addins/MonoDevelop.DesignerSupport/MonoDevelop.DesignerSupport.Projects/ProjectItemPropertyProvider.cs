
using System;
using System.ComponentModel;
using MonoDevelop.Projects;

namespace MonoDevelop.DesignerSupport.Projects
{
	public class ProjectItemPropertyProvider: IPropertyProvider
	{
		public object CreateProvider (object obj)
		{
			if (obj is ProjectFile)
				return new ProjectFileDescriptor ((ProjectFile)obj);
			else
				return new ProjectReferenceDescriptor ((ProjectReference)obj);
		}

		public bool SupportsObject (object obj)
		{
			return obj is ProjectFile || obj is ProjectReference;
		}
	}
}
