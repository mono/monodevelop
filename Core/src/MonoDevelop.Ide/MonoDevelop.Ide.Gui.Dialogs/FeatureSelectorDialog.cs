
using System;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	
	
	public partial class FeatureSelectorDialog : Gtk.Dialog
	{
		public FeatureSelectorDialog (Combine parentCombine, CombineEntry entry)
		{
			this.Build();
			featureList.Fill (parentCombine, entry, CombineEntryFeatures.GetFeatures (parentCombine, entry));
		}

		protected virtual void OnButtonOkClicked(object sender, System.EventArgs e)
		{
			featureList.ApplyFeatures ();
			Respond (Gtk.ResponseType.Ok);
		}
	}
}
