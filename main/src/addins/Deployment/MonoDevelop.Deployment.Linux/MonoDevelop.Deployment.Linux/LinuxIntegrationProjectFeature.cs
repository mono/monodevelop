
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Deployment.Linux
{
	public class LinuxIntegrationProjectFeature: ISolutionItemFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Unix Integration"); }
		}
		
		public string Description {
			get { return GettextCatalog.GetString ("Set options for generating files to better integrate the application or library in a Unix system."); }
		}
		
		public FeatureSupportLevel GetSupportLevel (SolutionFolder parentCombine, SolutionFolderItem entry)
		{
			if (entry is DotNetProject)
				return FeatureSupportLevel.SupportedByDefault;
			else
				return FeatureSupportLevel.NotSupported;
		}

		public bool SupportsSolutionItem (SolutionFolder parentCombine, SolutionFolderItem entry)
		{
			return entry is DotNetProject;
		}
		
		public Widget CreateFeatureEditor (SolutionFolder parentCombine, SolutionFolderItem entry)
		{
			return new BasicOptionPanelWidget ((DotNetProject) entry, true);
		}

		public void ApplyFeature (SolutionFolder parentCombine, SolutionFolderItem entry, Widget editor)
		{
			((BasicOptionPanelWidget)editor).Store ();
		}
		
		public string Validate (SolutionFolder parentCombine, SolutionFolderItem entry, Gtk.Widget editor)
		{
			return ((BasicOptionPanelWidget)editor).Validate ();
		}
	}
}
