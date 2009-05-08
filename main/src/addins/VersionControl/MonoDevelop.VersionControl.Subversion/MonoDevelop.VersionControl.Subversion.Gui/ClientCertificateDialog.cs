
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
		
		internal static bool Show (string realm, int may_save, out string cert_file, out int save)
		{
			string local_cert_file = null;
			int local_save = 0;
			
			bool res = false;
			object monitor = new Object ();
			
			EventHandler del = delegate {
					ClientCertificateDialog dlg = new ClientCertificateDialog (realm, may_save != 0);
					try {
						res = (dlg.Run () == (int) Gtk.ResponseType.Ok);
						if (res) {
							local_save = dlg.Save ? 1 : 0;
							local_cert_file = dlg.File;
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
			cert_file = local_cert_file;
			save = local_save;
			return res;
		}
	}
}
