
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

		public bool SupportsSolutionItem (SolutionFolder parentCombine, SolutionItem entry)
		{
			return ((entry is Project) || (entry is PackagingProject)) && parentCombine != null;
		}
		
		public Widget CreateFeatureEditor (SolutionFolder parentCombine, SolutionItem entry)
		{
			return new PackagingFeatureWidget (parentCombine, entry);
		}

		public void ApplyFeature (SolutionFolder parentCombine, SolutionItem entry, Widget editor)
		{
			((PackagingFeatureWidget)editor).ApplyFeature ();
		}
		
		public string Validate (SolutionFolder parentCombine, SolutionItem entry, Gtk.Widget editor)
		{
			return null;
		}
		
		public bool IsEnabled (SolutionFolder parentCombine, SolutionItem entry) 
		{
			return entry is PackagingProject;
		}
	}
}
