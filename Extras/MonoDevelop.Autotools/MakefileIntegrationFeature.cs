
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Autotools
{
	class MakefileIntegrationFeature: ICombineEntryFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Makefile Integration"); }
		}
		
		public string Description {
			get { return string.Empty; }
		}

		public bool SupportsCombineEntry (Combine parentCombine, CombineEntry entry)
		{
			return entry is Project;
		}
		
		public Widget CreateFeatureEditor (Combine parentCombine, CombineEntry entry)
		{
			return new MakefileIntegrationFeatureWidget ((Project)entry);
		}

		public void ApplyFeature (Combine parentCombine, CombineEntry entry, Widget editor)
		{
			((MakefileIntegrationFeatureWidget)editor).Store ();
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
