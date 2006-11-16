
using System;

namespace MonoDevelop.ChangeLogAddIn
{
	public class AddLogEntryDialog : Gtk.Dialog
	{
		protected Gtk.TextView textview;

		
		public AddLogEntryDialog()
		{
			Stetic.Gui.Build(this, typeof(ChangeLogAddIn.AddLogEntryDialog));
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
