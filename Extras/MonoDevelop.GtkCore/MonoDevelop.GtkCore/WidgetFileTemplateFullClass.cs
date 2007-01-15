
using System;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.GtkCore
{
	public class WidgetFileTemplateFullClass: FileTemplate
	{
		protected override bool IsValidForProject (Project project)
		{
			if (!base.IsValidForProject (project))
				return false;
			
			DotNetProject prj = project as DotNetProject;
			if (prj == null)
				return false;
			
			
			if (prj.LanguageBinding == null)
				return false;

			return !GtkCoreService.SupportsPartialTypes (prj);
		}
	}
}