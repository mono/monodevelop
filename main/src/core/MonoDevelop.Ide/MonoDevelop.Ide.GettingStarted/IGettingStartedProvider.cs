using System;
using MonoDevelop.Components;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.GettingStarted
{
	public interface IGettingStartedProvider
	{
		bool SupportsProject (Project project);
		Control GetGettingStartedWidget (Project project);
		void ShowGettingStarted (Project project, string pageHint = null);
		void SupportedProjectCreated (Project project, ProcessedTemplateResult templateResult);
	}
}

