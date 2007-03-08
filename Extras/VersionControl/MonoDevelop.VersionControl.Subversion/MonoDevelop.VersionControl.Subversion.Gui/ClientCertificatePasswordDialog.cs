
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
		
		internal static bool Show (string realm, int may_save, out SvnClient.svn_auth_cred_ssl_client_cert_pw_t retData)
		{
			SvnClient.svn_auth_cred_ssl_client_cert_pw_t data = new SvnClient.svn_auth_cred_ssl_client_cert_pw_t ();
			
			bool res = false;
			object monitor = new Object ();
			
			EventHandler del = delegate {
					ClientCertificatePasswordDialog dlg = new ClientCertificatePasswordDialog (realm, may_save != 0);
					try {
						res = (dlg.Run () == (int) Gtk.ResponseType.Ok);
						if (res) {
							data.may_save = dlg.Save ? 1 : 0;
							data.password = dlg.Password;
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
			retData = data;
			return res;
		}	
	}
}
