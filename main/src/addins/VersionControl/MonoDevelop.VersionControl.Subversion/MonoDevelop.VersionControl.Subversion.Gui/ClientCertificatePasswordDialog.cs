
using System;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	public partial class ClientCertificatePasswordDialog : Gtk.Dialog
	{
		public ClientCertificatePasswordDialog (string realm, bool maySave)
		{
			this.Build();
			
			labelRealm.Text = GettextCatalog.GetString ("Authentication realm: ") + realm;
			
			if (!maySave)
				checkSave.Visible = false;
		}
		
		public string Password {
			get { return entryPwd.Text; }
		}
		
		public bool Save {
			get { return checkSave.Active; }
		}

		internal static bool Show (string realm, bool may_save, out string password, out bool save)
		{
			string local_password = null;
			bool local_save = false;
			
			bool res = false;
			object monitor = new Object ();
			
			EventHandler del = delegate {
					ClientCertificatePasswordDialog dlg = new ClientCertificatePasswordDialog (realm, may_save);
					try {
						res = (dlg.Run () == (int) Gtk.ResponseType.Ok);
						if (res) {
							local_save = dlg.Save;
							local_password = dlg.Password;
						}
					} finally {
						dlg.Destroy ();
						lock (monitor) {
							System.Threading.Monitor.Pulse (monitor);
						}
					}
				};
			
			if (GLib.MainContext.Depth > 0) {
				// Already in GUI thread
				del (null, null);
			}
			else {
				lock (monitor) {
					Gtk.Application.Invoke (del);
					System.Threading.Monitor.Wait (monitor);
				}
			}
			
			password = local_password;
			save = local_save;
			
			return res;
		}	
	}
}
