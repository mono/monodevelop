using System;

namespace Stetic.Wrapper {

	public class AboutDialog : Window {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);

			if (!initialized) {
				// FIXME; set from Project eventually
				about.Name = "My Application";
			}
		}

		Gtk.AboutDialog about {
			get {
				return (Gtk.AboutDialog)Wrapped;
			}
		}

		string logo;
		public string Logo {
			get {
				return logo;
			}
			set {
				logo = value;
				about.Logo = new Gdk.Pixbuf (logo);
			}
		}

		// In the underlying representation, WebsiteLabel is always set
		// if Website is; if you set Website to something, and WebsiteLabel
		// is null, then WebsiteLabel gets set to match Website. There are
		// two problems with this for us:
		//
		//   1. If you type "http..." into Website while WebsiteLabel is
		//      blank, WebsiteLabel ends up being forcibly set to just "h".
		//
		//   2. If the user decides s/he wants to get rid of WebsiteLabel,
		//      they have to actually copy the URL from Website over it.
		//
		// In Stetic's representation, WebsiteLabel is always "what to show
		// *instead of* Website", and if it's empty, then you see the raw URL.

		public string Website {
			get {
				return about.Website;
			}
			set {
				if (website_label == null)
					about.WebsiteLabel = value;
				about.Website = value;
			}
		}

		string website_label;
		public string WebsiteLabel {
			get {
				return website_label;
			}
			set {
				if (value == "" || value == null) {
					about.WebsiteLabel = about.Website;
					website_label = null;
				} else {
					about.WebsiteLabel = value;
					website_label = value;
				}
			}
		}
	}
}
