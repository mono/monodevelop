
using System;
using MonoDevelop.Ide;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl
{
	public partial class UrlBasedRepositoryEditor : Gtk.Bin, IRepositoryEditor
	{
		UrlBasedRepository repo;
		bool updating;
		List<string> protocols = new List<string> ();

		public UrlBasedRepositoryEditor (UrlBasedRepository repo)
		{
			Build ();
			protocols = new List<string> (repo.SupportedProtocols);
			protocols.AddRange (repo.SupportedNonUrlProtocols);
				
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
				comboProtocol.Active = protocols.IndexOf (repo.Uri.Scheme);
			} else {
				// The url may have a scheme, but it may be an incomplete or incorrect url. Do the best to select
				// the correct value in the protocol combo
				string prot = repo.SupportedProtocols.FirstOrDefault (p => repo.Url.StartsWith (p + "://"));
				if (prot != null) {
					repositoryServerEntry.Text = string.Empty;
					repositoryPortSpin.Value = 0;
					repositoryPathEntry.Text = string.Empty;
					repositoryUserEntry.Text = string.Empty;
					comboProtocol.Active = protocols.IndexOf (prot);
				}
				else
					comboProtocol.Active = protocols.IndexOf (repo.Protocol);
			}
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
			if (repo.Uri != null || repo.SupportedProtocols.Any (p => repositoryUrlEntry.Text.StartsWith (p + "://"))) {
				repositoryPathEntry.Sensitive = true;
				bool isUrl = Protocol != "file";
				repositoryServerEntry.Sensitive = isUrl;
				repositoryUserEntry.Sensitive = isUrl;
				repositoryPortSpin.Sensitive = isUrl;
			} else {
				repositoryPathEntry.Sensitive = false;
				repositoryServerEntry.Sensitive = false;
				repositoryUserEntry.Sensitive = false;
				repositoryPortSpin.Sensitive = false;
			}
		}
		
		void SetRepoUrl ()
		{
			if (!repo.SupportedProtocols.Contains (Protocol)) {
				repo.Url = string.Empty;
				return;
			}
			UriBuilder ub = new UriBuilder ();
			ub.Host = repositoryServerEntry.Text;
			ub.Scheme = Protocol;
			ub.UserName = repositoryUserEntry.Text;
			ub.Port = (int)repositoryPortSpin.Value;
			ub.Path = repositoryPathEntry.Text;
			repo.Url = ub.ToString ();
		}

		protected virtual void OnRepositoryServerEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			SetRepoUrl ();
			UpdateUrl ();
		}

		protected virtual void OnRepositoryPathEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			SetRepoUrl ();
			UpdateUrl ();
		}

		protected virtual void OnRepositoryUserEntryChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			SetRepoUrl ();
			UpdateUrl ();
		}

		protected virtual void OnComboProtocolChanged(object sender, System.EventArgs e)
		{
			if (updating) return;
			SetRepoUrl ();
			UpdateUrl ();
			UpdateControls ();
		}

		protected virtual void OnRepositoryPortSpinValueChanged (object sender, System.EventArgs e)
		{
			if (updating) return;
			SetRepoUrl ();
			UpdateUrl ();
		}
		
		string Protocol {
			get {
				return comboProtocol.Active != -1 ? protocols [comboProtocol.Active] : null;
			}
		}
	}
}
