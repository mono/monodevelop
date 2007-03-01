
using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.ChangeLogAddIn
{
	public partial class ChangeLogAddInOptionPanelWidget : Gtk.Bin
	{
		public ChangeLogAddInOptionPanelWidget()
		{
			Build ();
		}
		
		public void LoadPanelContents()
		{
			nameEntry.Text = Runtime.Properties.GetProperty ("ChangeLogAddIn.Name", "Full Name");
			emailEntry.Text = Runtime.Properties.GetProperty ("ChangeLogAddIn.Email", "Email Address");
			integrationCheck.Active = Runtime.Properties.GetProperty ("ChangeLogAddIn.VersionControlIntegration", true);
		}
		
		public bool StorePanelContents()
		{
			Runtime.Properties.SetProperty("ChangeLogAddIn.Name", nameEntry.Text);
			Runtime.Properties.SetProperty("ChangeLogAddIn.Email", emailEntry.Text);
			Runtime.Properties.SetProperty("ChangeLogAddIn.VersionControlIntegration", integrationCheck.Active);
			Runtime.Properties.SaveProperties ();
			return true;
		}
	}
}
