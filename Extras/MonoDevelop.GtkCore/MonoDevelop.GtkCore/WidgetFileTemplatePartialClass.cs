
using System;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.GtkCore
{
	public class WidgetFileTemplatePartialClass: FileTemplate
	{
		protected override bool IsValidForProject (IProject project)
		{
			if (!base.IsValidForProject (project))
				return false;
			
			MSBuildProject prj = project as MSBuildProject;
			if (prj == null)
				return false;
				
			if (BackendBindingService.GetBackendBinding (prj) == null)
				return false;

			return GtkCoreService.SupportsPartialTypes (prj);
		}
	}
}