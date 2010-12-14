
using System;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl
{
	public partial class UrlBasedRepositoryEditor : Gtk.Bin, IRepositoryEditor
	{
		UrlBasedRepository repo;
		bool updating;
		string[] protocols;
		
		public UrlBasedRepositoryEditor (UrlBasedRepository repo)
		{
			Build ();
			protocols = repo.SupportedProtocols;
				
			this.repo = repo;
			foreach (string p in protocols)
				comboProtocol.AppendText (p);

			updating = true;
			repositoryUrlEntry.Text = repo.Url;
			Fill ();
			UpdateControls ();
			updating = false;
		}
		
		Gtk.Widget IRepositoryEditor.Widget {
			get { return this; }
		}
		
		public bool Validate ()
		{
			if (!repo.IsUrlValid (repositoryUrlEntry.Text)) {
				labelError.Show ();
				return false;
			} else {
				return true;
			}
		}
		
		void Fill ()
		{
			if (repo.Uri != null) {
				if (repo.Name == repositoryServerEntry.Text)
					repo.Name = repo.Uri.Host;
				repositoryServerEntry.Text = repo.Uri.Host;
				repositoryPortSpin.Value = repo.Uri.Port;
				repositoryPathEntry.Text = repo.Uri.PathAndQuery;
				repositoryUserEntry.Text = repo.Uri.UserInfo;
				comboProtocol.Active = Array.IndexOf (protocols, repo.Uri.Scheme);
			} else
				comboProtocol.Active = -1;
			repositoryPassEntry.Text = repo.Pass;
		}

		protected virtual void OnRepositoryUrlEntryChanged(object sender, System.EventArgs e)
		{
			if (!updating) {
				updating = true;
				repo.Url = repositoryUrlEntry.Text;
				Fill ();
				UpdateControls ();
				labelError.Hide ();
				updating = false;
			}
		}
		
		void UpdateUrl ()
		{
			updating = true;
			repositoryUrlEntry.Text = repo.Url;
			if (repo.Uri != null && repo.Name == repositoryServerEntry.Text)
				repo.Name = repo.Uri.Host;
			updating = false;
		}
		
		void UpdateControls ()
		{
			if (repo.Uri != null) {
				repositoryPathEntry.Sensitive = true;
				bool isUrl = (Protocol != "file");
				repositoryServerEntry.Sensitive = isUrl;
				repositoryUserEntry.Sensitive = isUrl;
				repositoryPassEntry.Sensitive = isUrl;
				repositoryPortSpin.Sensitive = isUrl;
			} else {
				repositoryPathEntry.Sensitive = false;
				repositoryServerEntry.Sensitive = false;
				repositoryUserEntry.Sensitive = false;
				repositoryPassEntry.Sensitive = false;
				repositoryPortSpin.Sensitive = false;
			}
		}

		protected virtual void OnRepositoryServerEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			if (repo.Name == repo.Uri.Host)
				repo.Name = repositoryServerEntry.Text;
			UriBuilder ub = new UriBuilder (repo.Uri);
			ub.Host = repositoryServerEntry.Text;
			repo.Url = ub.ToString ();
			UpdateUrl ();
		}

		protected virtual void OnRepositoryPathEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			UriBuilder ub = new UriBuilder (repo.Url);
			ub.Path = repositoryPathEntry.Text;
			repo.Url = ub.ToString ();
			UpdateUrl ();
		}

		protected virtual void OnRepositoryUserEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			UriBuilder ub = new UriBuilder (repo.Url);
			ub.UserName = repositoryUserEntry.Text;
			repo.Url = ub.ToString ();
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
			UriBuilder ub = new UriBuilder (repo.Url);
			ub.Scheme = Protocol;
			repo.Url = ub.ToString ();
			UpdateUrl ();
			UpdateControls ();
		}

		protected virtual void OnRepositoryPortSpinValueChanged (object sender, System.EventArgs e)
		{
			if (updating) return;
			UriBuilder ub = new UriBuilder (repo.Url);
			ub.Port = (int) repositoryPortSpin.Value;
			repo.Url = ub.ToString ();
			UpdateUrl ();
		}
		
		string Protocol {
			get {
				return protocols [comboProtocol.Active];
			}
		}
	}
}
