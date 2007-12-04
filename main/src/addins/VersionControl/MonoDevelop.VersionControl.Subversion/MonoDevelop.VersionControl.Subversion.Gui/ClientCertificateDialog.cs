
using System;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	public partial class ClientCertificateDialog : Gtk.Dialog
	{
		public ClientCertificateDialog (string realm, bool maySave)
		{
			this.Build();
			
			labelRealm.Text = GettextCatalog.GetString ("Authentication realm: ") + realm;
			
			if (!maySave)
				checkSave.Visible = false;
		}
		
		public string File {
			get { return fileentry.Path; }
		}
		
		public bool Save {
			get { return checkSave.Active; }
		}
		
		internal static bool Show (string realm, int may_save, out LibSvnClient.svn_auth_cred_ssl_client_cert_t retData)
		{
			LibSvnClient.svn_auth_cred_ssl_client_cert_t data = new LibSvnClient.svn_auth_cred_ssl_client_cert_t ();
			
			bool res = false;
			object monitor = new Object ();
			
			EventHandler del = delegate {
					ClientCertificateDialog dlg = new ClientCertificateDialog (realm, may_save != 0);
					try {
						res = (dlg.Run () == (int) Gtk.ResponseType.Ok);
						if (res) {
							data.may_save = dlg.Save ? 1 : 0;
							data.cert_file = dlg.File;
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
