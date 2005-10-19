
using System;
using Gtk;
using Glade;

namespace MonoDevelop.Core.Gui.Dialogs
{
	class NewSiteDialog : IDisposable
	{
		[Glade.Widget ("NewSiteDialog")] Dialog dialog;
		[Glade.Widget] RadioButton btnOnlineRep;
		[Glade.Widget] Button btnOk;
		[Glade.Widget] Entry urlText;
		[Glade.Widget] Entry pathText;
		[Glade.Widget] Gnome.FileEntry pathEntry;
		
		public NewSiteDialog ()
		{
			new Glade.XML (null, "Base.glade", "NewSiteDialog", null).Autoconnect (this);
			CheckValues ();
		}
		
		public string Url {
			get {
				if (btnOnlineRep.Active)
					return urlText.Text;
				else if (pathText.Text != "")
					return "file://" + pathText.Text;
				else
					return "";
			}
		}
		
		void CheckValues ()
		{
			btnOk.Sensitive = (Url != "");
		}
		
		public bool Run ()
		{
			dialog.ShowAll ();
			return ((ResponseType)dialog.Run ()) == ResponseType.Ok;
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
			dialog.Dispose ();
		}
		
		protected void OnTextChanged (object sender, EventArgs args)
		{
			CheckValues ();
		}
		
		protected void OnClose (object sender, EventArgs args)
		{
			dialog.Destroy ();
		}
		
		protected void OnOptionClicked (object sender, EventArgs e)
		{
			if (btnOnlineRep.Active) {
				urlText.Sensitive = true;
				pathEntry.Sensitive = false;
			} else {
				urlText.Sensitive = false;
				pathEntry.Sensitive = true;
			}
			CheckValues ();
		}
	}
}
