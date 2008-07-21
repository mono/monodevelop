
using System;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.VersionControl
{
	public abstract class UrlBasedRepository: Repository
	{
		private string dir = "";
		private string user = "";
		private string pass = "";
		private int port = 0;
		private string server = "";
		private string method = "";
		
		public UrlBasedRepository ()
		{
		}
		
		public UrlBasedRepository (VersionControlSystem vcs): base (vcs)
		{
		}
		
		public override void CopyConfigurationFrom (Repository other)
		{
			base.CopyConfigurationFrom (other);
			
			UrlBasedRepository ot = (UrlBasedRepository) other;
			dir = ot.dir;
			user = ot.user;
			pass = ot.pass;
			port = ot.port;
			server = ot.server;
			method = ot.method;
		}

		public override string LocationDescription {
			get { return Url; }
		}
		
		public virtual string Url
		{
			get {
				if (method.Length == 0)
					return "";
				string sdir = dir.StartsWith ("/") ? dir.Substring (1) : dir;
				return Root + "/" + sdir;
			}
			set {
				try {
					pass = string.Empty;
					Uri uri = new Uri (value);
					method = uri.Scheme;
					user = uri.UserInfo;
					server = uri.Host;
					port = uri.Port;
					dir = uri.PathAndQuery;
				} catch {
					pass = user = server = dir = string.Empty;
					port = 0;
					method = "";
				}
			}
		}
		
		[ItemProperty]
		public string Dir
		{
			get { return dir; }
			set { dir = value; }
		}
		
		[ItemProperty]
		public string User
		{
			get { return user; }
			set { user = value; }
		}
		
		[ItemProperty]
		public string Pass
		{
			get { return pass; }
			set { pass = value; }
		}
		
		[ItemProperty]
		public int Port
		{
			get { return port; }
			set { port = value; }
		}
		
		[ItemProperty]
		public string Server
		{
			get { return server; }
			set { server = value; }
		}
		
		[ItemProperty]
		public string Method
		{
			get { return method; }
			set { method = value; }
		}
		
		public string Root {
			get {
				if (method.Length == 0)
					return "";
				string login = "";
				if (this.User != "") {
					login += this.User;
					if (this.Pass != "")
						login += ":" + this.Pass;
					login += "@";
				}
				return method + "://" + login + this.Server + (port > 0 ? ":"+port.ToString() : "");
			}
		}
	}
}
