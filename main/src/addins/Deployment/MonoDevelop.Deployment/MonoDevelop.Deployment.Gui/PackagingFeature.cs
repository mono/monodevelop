
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Deployment.Gui
{
	internal class PackagingFeature: ICombineEntryFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Packaging"); }
		}
		
		public string Description {
			get { return GettextCatalog.GetString ("Add a Packaging Project to the solution for generating different kinds of packages for the new project."); }
		}

		public bool SupportsCombineEntry (Combine parentCombine, CombineEntry entry)
		{
			return ((entry is Project) || (entry is PackagingProject)) && parentCombine != null;
		}
		
		public Widget CreateFeatureEditor (Combine parentCombine, CombineEntry entry)
		{
			return new PackagingFeatureWidget (parentCombine, entry);
		}

		public void ApplyFeature (Combine parentCombine, CombineEntry entry, Widget editor)
		{
			((PackagingFeatureWidget)editor).ApplyFeature ();
		}
		
		public string Validate (Combine parentCombine, CombineEntry entry, Gtk.Widget editor)
		{
			return null;
		}
		
		public bool IsEnabled (Combine parentCombine, CombineEntry entry) 
		{
			return entry is PackagingProject;
		}
	}
}
