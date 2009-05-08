
using System;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	public partial class SslServerTrustDialog : Gtk.Dialog
	{
		SslFailure failures;
		
		internal SslServerTrustDialog (string realm, SslFailure failures, CertficateInfo cert_info, bool may_save)
		{
			this.Build();
			
			this.failures = failures;
			labelRealm.Text = realm;
			labelHost.Text = cert_info.HostName;
			labelIssuer.Text = cert_info.IssuerName;
			labelFrom.Text = cert_info.ValidFrom;
			labelUntil.Text = cert_info.ValidUntil;
			labelFprint.Text = cert_info.Fingerprint;
			
			if (!may_save)
				radioAccept.Visible = false;
			
			string reason = "";
			if ((failures & SslFailure.NotYetValid) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate is not yet valid.");
			if ((failures & SslFailure.Expired) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate has expired.");
			if ((failures & SslFailure.CNMismatch) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate's CN (hostname) does not match the remote hostname.");
			if ((failures & SslFailure.UnknownCA) != 0)
				reason += "\n" + GettextCatalog.GetString ("Certificate authority is unknown (i.e. not trusted).");
			if (reason.Length > 0) {
				labelReason.Markup = "<b>" + reason.Substring (1) + "</b>";
			}
		}
		
		public bool Save {
			get { return radioAccept.Active; }
		}
		
		public SslFailure AcceptedFailures {
			get {
				if (radioNotAccept.Active)
					return SslFailure.None;
				else
					return failures;
			}
		}

		internal static bool Show (string realm, SslFailure failures, bool may_save, CertficateInfo certInfo, out SslFailure accepted_failures, out bool save)
		{
			SslFailure local_accepted_failures = SslFailure.None;
			bool local_save = false;
			
			bool res = false;
			object monitor = new Object ();
			
			EventHandler del = delegate {
					try {
						SslServerTrustDialog dlg = new SslServerTrustDialog (realm, failures, certInfo, may_save);
						res = (dlg.Run () == (int) Gtk.ResponseType.Ok);
						if (res) {
							local_save = dlg.Save;
							local_accepted_failures = dlg.AcceptedFailures;
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
			accepted_failures = local_accepted_failures;
			save = local_save;
			return res;
		}
	}
}
