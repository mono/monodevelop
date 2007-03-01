
using System;

namespace MonoDevelop.ChangeLogAddIn
{
	public partial class AddLogEntryDialog : Gtk.Dialog
	{
		public AddLogEntryDialog()
		{
			Build ();
		}
		
		public string Message {
			get { return textview.Buffer.Text; }
			set { textview.Buffer.Text = value; }
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
	}
}
