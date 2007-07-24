//

using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{


	public partial class GenerateMakefilesDialog : Gtk.Dialog
	{

		public GenerateMakefilesDialog (Combine combine)
		{
			this.Build();

			for (int i = 0; i < combine.Configurations.Count; i ++) {
				CombineConfiguration cc = (CombineConfiguration) combine.Configurations [i];
				comboConfigs.AppendText (cc.Name);
				if (cc.Name == combine.ActiveConfiguration.Name)
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
