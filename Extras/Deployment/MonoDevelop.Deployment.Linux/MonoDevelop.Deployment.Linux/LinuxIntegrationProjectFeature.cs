
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Deployment.Linux
{
	public class LinuxIntegrationProjectFeature: ICombineEntryFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Unix Integration"); }
		}

		public bool SupportsCombineEntry (Combine parentCombine, CombineEntry entry)
		{
			return entry is DotNetProject;
		}
		
		public Widget CreateFeatureEditor (Combine parentCombine, CombineEntry entry)
		{
			return new BasicOptionPanelWidget ((DotNetProject) entry, true);
		}

		public void ApplyFeature (Combine parentCombine, CombineEntry entry, Widget editor)
		{
			((BasicOptionPanelWidget)editor).Store ();
		}
		
		public string Validate (Combine parentCombine, CombineEntry entry, Gtk.Widget editor)
		{
			return ((BasicOptionPanelWidget)editor).Validate ();
		}
		
		public bool IsEnabled (Combine parentCombine, CombineEntry entry) 
		{
			return false;
		}
	}
}
