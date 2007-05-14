
using System;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	
	
	public partial class DeleteFileDialog : Gtk.Dialog
	{
		
		public DeleteFileDialog(string question)
		{
			this.Build();
			this.QuestionLabel.Text = question;
		}

		public new bool Run ()
		{
			int response = base.Run ();
			Hide ();
			return (response == (int) Gtk.ResponseType.Ok);
		}

		protected virtual void OnYesButtonClicked(object sender, System.EventArgs e)
		{
			this.Respond (Gtk.ResponseType.Ok);
		}

		protected virtual void OnNoButtonClicked(object sender, System.EventArgs e)
		{
			this.Respond (Gtk.ResponseType.Cancel);
		}
		
		public bool DeleteFromDisk {
			get {
				return cbDeleteFromDisk.Active;
			}
		}
		
	}
}
