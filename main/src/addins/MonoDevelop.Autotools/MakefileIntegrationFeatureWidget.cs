
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public partial class MakefileIntegrationFeatureWidget : Gtk.Bin
	{
		public MakefileIntegrationFeatureWidget (Project project)
		{
			this.Build();
		}
		
		public void Store ()
		{
		}
	}
}
