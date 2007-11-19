
namespace MonoDevelop.Projects.Gui.Dialogs
{
	partial class NewConfigurationDialog : Gtk.Dialog
	{
		public NewConfigurationDialog ()
		{
			this.Build();
		}
		
		public string ConfigName {
			get { return nameEntry.Text; }
			set { nameEntry.Text = value; }
		}
		
		public bool CreateChildren {
			get { return createChildrenCheck.Active; }
		}
	}
}
