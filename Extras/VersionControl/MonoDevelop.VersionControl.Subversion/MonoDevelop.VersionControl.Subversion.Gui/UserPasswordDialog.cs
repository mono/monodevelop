
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
			
			if (!showPassword)
				entryPwd.Visible = labelPwd.Visible = false;
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
		
		
		internal static bool Show (string realm, bool may_save, out LibSvnClient.svn_auth_cred_username_t data)
		{
			data = new LibSvnClient.svn_auth_cred_username_t ();
			int save;
			string pwd, user = "";
			bool ret = Show (false, realm, may_save, ref user, out pwd, out save);
			data.username = user;
			data.may_save = save;
			return ret;
		}
		
		internal static bool Show (string user_name, string realm, bool may_save, out LibSvnClient.svn_auth_cred_simple_t data)
		{
			data = new LibSvnClient.svn_auth_cred_simple_t ();
			int save;
			string pwd;
			bool ret = Show (true, realm, may_save, ref user_name, out pwd, out save);
			data.username = user_name;
			data.password = pwd;
			data.may_save = save;
			return ret;
		}
		
		internal static bool Show (bool showPwd, string realm, bool may_save, ref string user_name, out string password, out int save)
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
			save = s;
			password = pwd;
			return res;
		}
	}
}
