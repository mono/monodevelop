
using System;

namespace MonoDevelop.VersionControl
{
	public partial class UrlBasedRepositoryEditor : Gtk.Bin
	{
		UrlBasedRepository repo;
		bool updating;
		string[] protocols;
		
		public UrlBasedRepositoryEditor (UrlBasedRepository repo, string[] supportedProtocols)
		{
			Build ();
			protocols = supportedProtocols;
				
			this.repo = repo;
			foreach (string p in protocols)
				comboProtocol.AppendText (p);

			updating = true;
			repositoryUrlEntry.Text = repo.Url;
			Fill ();
			UpdateControls ();
			updating = false;
		}
		
		void Fill ()
		{
			if (repo.Name == repositoryServerEntry.Text)
				repo.Name = repo.Server;
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
			if (repo.Name == repositoryServerEntry.Text)
				repo.Name = repo.Server;
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
			if (repo.Name == repo.Server)
				repo.Name = repositoryServerEntry.Text;
			repo.Server = repositoryServerEntry.Text;
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

		protected virtual void OnRepositoryPortSpinValueChanged (object sender, System.EventArgs e)
		{
			if (updating) return;
			repo.Port = (int) repositoryPortSpin.Value;
			UpdateUrl ();
		}
		
		string Protocol {
			get {
				return protocols [comboProtocol.Active];
			}
		}
	}
}
