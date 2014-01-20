
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Deployment.Gui
{
	internal class PackagingFeature: ISolutionItemFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Packaging"); }
		}
		
		public string Description {
			get { return GettextCatalog.GetString ("Add a Packaging Project to the solution for generating different kinds of packages for the new project."); }
		}

		public FeatureSupportLevel GetSupportLevel (SolutionFolder parentCombine, SolutionFolderItem entry)
		{
			if (parentCombine == null)
				return FeatureSupportLevel.NotSupported;
			
			if (entry is PackagingProject)
				return FeatureSupportLevel.Enabled;
			else if (entry is Project)
				return FeatureSupportLevel.SupportedByDefault;
			else
				return FeatureSupportLevel.NotSupported;
		}
		
		public Widget CreateFeatureEditor (SolutionFolder parentCombine, SolutionFolderItem entry)
		{
			return new PackagingFeatureWidget (parentCombine, entry);
		}

		public void ApplyFeature (SolutionFolder parentCombine, SolutionFolderItem entry, Widget editor)
		{
			((PackagingFeatureWidget)editor).ApplyFeature ();
		}
		
		public string Validate (SolutionFolder parentCombine, SolutionFolderItem entry, Gtk.Widget editor)
		{
			return null;
		}
	}
}
