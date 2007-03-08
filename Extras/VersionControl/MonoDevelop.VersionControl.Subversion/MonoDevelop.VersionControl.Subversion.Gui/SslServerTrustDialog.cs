
using System;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	public partial class SslServerTrustDialog : Gtk.Dialog
	{
		uint failures;
		
		internal SslServerTrustDialog (string realm, uint failures, SvnClient.svn_auth_ssl_server_cert_info_t cert_info, bool may_save)
		{
			this.Build();
			
			this.failures = failures;
			labelRealm.Text = realm;
			labelHost.Text = cert_info.hostname;
			labelIssuer.Text = cert_info.issuer_dname;
			labelFrom.Text = cert_info.valid_from;
			labelUntil.Text = cert_info.valid_until;
			labelFprint.Text = cert_info.fingerprint;
			
			if (!may_save)
				radioAccept.Visible = false;
			
			string reason = "";
			if ((failures & SvnClient.SVN_AUTH_SSL_NOTYETVALID) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate is not yet valid.");
			if ((failures & SvnClient.SVN_AUTH_SSL_EXPIRED) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate has expired.");
			if ((failures & SvnClient.SVN_AUTH_SSL_CNMISMATCH) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate's CN (hostname) does not match the remote hostname.");
			if ((failures & SvnClient.SVN_AUTH_SSL_UNKNOWNCA) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate authority is unknown (i.e. not trusted).");
			if (reason.Length > 0) {
				labelReason.Markup = "<b>" + reason.Substring (1) + "</b>";
			}
		}
		
		public bool Save {
			get { return radioAccept.Active; }
		}
		
		public uint AcceptedFailures {
			get {
				if (radioNotAccept.Active)
					return 0;
				else
					return failures;
			}
		}
		
		internal static bool Show (string realm, uint failures, int may_save, SvnClient.svn_auth_ssl_server_cert_info_t cert_info, out SvnClient.svn_auth_cred_ssl_server_trust_t retData)
		{
			SvnClient.svn_auth_cred_ssl_server_trust_t data = new SvnClient.svn_auth_cred_ssl_server_trust_t ();
			
			bool res = false;
			object monitor = new Object ();
			
			EventHandler del = delegate {
					try {
						SslServerTrustDialog dlg = new SslServerTrustDialog (realm, failures, cert_info, may_save != 0);
						res = (dlg.Run () == (int) Gtk.ResponseType.Ok);
						if (res) {
							data.may_save = dlg.Save ? 1 : 0;
							data.accepted_failures = dlg.AcceptedFailures;
						} else {
							data.may_save = 0;
							data.accepted_failures = 0;
							res = true;
						}
					
						dlg.Destroy ();
					} finally {
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
