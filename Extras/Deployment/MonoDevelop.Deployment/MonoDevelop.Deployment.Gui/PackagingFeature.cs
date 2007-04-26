
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Deployment.Gui
{
	public class PackagingFeature: ICombineEntryFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Packaging"); }
		}

		public bool SupportsCombineEntry (Combine parentCombine, CombineEntry entry)
		{
			return (entry is Project) && parentCombine != null;
		}
		
		public Widget CreateFeatureEditor (Combine parentCombine, CombineEntry entry)
		{
			return new PackagingFeatureWidget (parentCombine, (Project)entry);
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
			return false;
		}
	}
}
