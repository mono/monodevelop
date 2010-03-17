//

using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.Autotools
{
	public partial class GenerateMakefilesDialog : Gtk.Dialog
	{

		public GenerateMakefilesDialog (Solution solution)
		{
			this.Build();

			for (int i = 0; i < solution.Configurations.Count; i ++) {
				SolutionConfiguration cc = (SolutionConfiguration) solution.Configurations [i];
				comboConfigs.AppendText (cc.Id);
				if (cc.Id == IdeApp.Workspace.ActiveConfigurationId)
					comboConfigs.Active = i;
			}
		}

		public bool GenerateAutotools {
			get { return rbAutotools.Active; }
		}

		public string DefaultConfiguration {
			get { return comboConfigs.ActiveText; }
		}
	}
}
