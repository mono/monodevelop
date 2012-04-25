using System;
using Gtk;

using MonoDevelop.Projects;

namespace MonoDevelop.GtkCore.Dialogs
{
	public partial class ProjectConversionDialog : Gtk.Dialog
	{		
		public ProjectConversionDialog (IntPtr raw)
			: base (raw)
		{
		}
		
		public string GuiFolderName { get; private set; }
		
		public bool MakeBackup { get; private set; }
		
		
		public ProjectConversionDialog (Project project, string guiFolderName)
		{
			this.Build ();
			
			entryFolder.Text = guiFolderName;
			Title = project.Name;
			entryFolder.Position = -1;
			
			buttonConvert.Clicked += HandleButtonConvertClicked;
		}

		void HandleButtonConvertClicked (object sender, EventArgs e)
		{
			GuiFolderName = entryFolder.Text;
			MakeBackup = checkBackup.Active;
				
			Respond (ResponseType.Yes);
		}
	}
}

