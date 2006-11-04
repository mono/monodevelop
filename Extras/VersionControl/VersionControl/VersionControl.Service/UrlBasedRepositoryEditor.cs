
using System;

namespace VersionControl.Service
{
	public class UrlBasedRepositoryEditor : Gtk.Bin
	{
		protected Gtk.Entry repositoryUrlEntry;
		protected Gtk.Entry repositoryServerEntry;
		protected Gtk.SpinButton repositoryPortSpin;
		protected Gtk.Entry repositoryPathEntry;
		protected Gtk.Entry repositoryUserEntry;
		protected Gtk.Entry repositoryPassEntry;
		protected Gtk.ComboBox comboProtocol;
		
		UrlBasedRepository repo;
		bool updating;
		readonly string[] protocolsSvn = {"svn", "svn+ssh", "http", "https", "file"};
		readonly string[] protocolsCvs = {"ext", "fork", "gserver", "kserver", "local", "pserver", "server"};
		string[] protocols;
		
		public UrlBasedRepositoryEditor (UrlBasedRepository repo)
		{
			Stetic.Gui.Build(this, typeof(UrlBasedRepositoryEditor));
			if (repo is VersionControl.Service.Cvs.CvsRepository)
				protocols = protocolsCvs;
			else
				protocols = protocolsSvn;
				
			this.repo = repo;
			foreach (string p in protocols)
				comboProtocol.AppendText (p);

			updating = true;
			repositoryUrlEntry.Text = repo.Url;
			Fill ();
			comboProtocol.Active = 0;
			UpdateControls ();
			updating = false;
		}
		
		void Fill ()
		{
			repositoryServerEntry.Text = repo.Server;
			repositoryPortSpin.Value = repo.Port;
			repositoryPathEntry.Text = repo.Dir;
			repositoryUserEntry.Text = repo.User;
			repositoryPassEntry.Text = repo.Pass;
			comboProtocol.Active = Array.IndexOf (protocols, repo.Method);
		}

		protected virtual void OnRepositoryUrlEntryChanged(object sender, System.EventArgs e)
		{
			if (!updating) {
				updating = true;
				repo.Url = repositoryUrlEntry.Text;
				Fill ();
				updating = false;
			}
		}
		
		void UpdateUrl ()
		{
			updating = true;
			repositoryUrlEntry.Text = repo.Url;
			updating = false;
		}
		
		void UpdateControls ()
		{
			switch (Protocol) {
				case "svn":
				case "svn+ssh":
				case "http":
				case "https":
					repositoryServerEntry.Sensitive = true;
					repositoryUserEntry.Sensitive = true;
					repositoryPassEntry.Sensitive = true;
					repositoryPortSpin.Sensitive = true;
					break;
				case "file":
					repositoryServerEntry.Sensitive = false;
					repositoryUserEntry.Sensitive = false;
					repositoryPassEntry.Sensitive = false;
					repositoryPortSpin.Sensitive = false;
					break;
			}
		}

		protected virtual void OnRepositoryServerEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			repo.Server = repositoryServerEntry.Text;
			UpdateUrl ();
		}

		protected virtual void OnRepositoryPortSpinChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			repo.Port = (int) repositoryPortSpin.Value;
			UpdateUrl ();
		}

		protected virtual void OnRepositoryPathEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			repo.Dir = repositoryPathEntry.Text;
			UpdateUrl ();
		}

		protected virtual void OnRepositoryUserEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			repo.User = repositoryUserEntry.Text;
			UpdateUrl ();
		}

		protected virtual void OnRepositoryPassEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			repo.Pass = repositoryPassEntry.Text;
			UpdateUrl ();
		}

		protected virtual void OnComboProtocolChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			repo.Method = Protocol;
			UpdateUrl ();
			UpdateControls ();
		}
		
		string Protocol {
			get {
				return protocols [comboProtocol.Active];
			}
		}
	}
}
