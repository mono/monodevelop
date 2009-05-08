
using System;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	public partial class UserPasswordDialog : Gtk.Dialog
	{
		public UserPasswordDialog (string user, string realm, bool mayRemember, bool showPassword)
		{
			Build ();
			
			if (user != null && user.Length > 0) {
				entryUser.Text = user;
				entryPwd.HasFocus = true;
			}
			
			labelRealm.Text = GettextCatalog.GetString ("Authentication realm: ") + realm;
			if (!mayRemember)
				checkSavePwd.Visible = false;
			
			if (!showPassword) {
				entryUser.Activated += new EventHandler (OnPasswdActivated);
				entryPwd.Visible = labelPwd.Visible = false;
			} else
				entryPwd.Activated += new EventHandler (OnPasswdActivated);
		}
		
		public string User {
			get { return entryUser.Text; }
		}
		
		public string Password {
			get { return entryPwd.Text; }
		}
		
		public bool SavePassword {
			get { return checkSavePwd.Visible && checkSavePwd.Active; }
		}
		
		void OnPasswdActivated (object o, EventArgs e)
		{
			this.Respond ((int) Gtk.ResponseType.Ok);
		}

		internal static bool Show (bool showPwd, string realm, bool may_save, ref string user_name, out string password, out bool save)
		{
			string pwd = "", user = user_name;
			int s = 0;
			
			bool res = false;
			object monitor = new Object ();
			
			EventHandler del = delegate {
					try {
						UserPasswordDialog dlg = new UserPasswordDialog (user, realm, may_save, showPwd);
						res = (dlg.Run () == (int) Gtk.ResponseType.Ok);
						if (res) {
							s = dlg.SavePassword ? 1 : 0;
							pwd = dlg.Password;
							user = dlg.User;
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
			
			user_name = user;
			save = s != 0;
			password = pwd;
			return res;
		}
	}
}
