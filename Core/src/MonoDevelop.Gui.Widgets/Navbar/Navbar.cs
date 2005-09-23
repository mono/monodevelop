using System;
using Gtk;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Widgets
{
	public class Navbar : VBox
	{
		Entry address;

		const string uiInfo = 
            "<toolbar>" +
            "  <toolitem name=\"back\" action=\"back\" />" +
            "  <toolitem name=\"forward\" action=\"forward\" />" +
            "  <toolitem name=\"stop\" action=\"stop\" />" +
            "  <toolitem name=\"reload\" action=\"reload\" />" +
            "  <toolitem name=\"go\" action=\"go\" />" +
            "</toolbar>";

		public Navbar () : this (Gtk.IconSize.SmallToolbar)
		{
		}

		public Navbar (Gtk.IconSize size)
		{
			address = new Entry ("address");
			// FIXME: this doesnt't seem to work yet
			// address.Completion = new EntryCompletion ();
			address.WidthChars = 50;
			address.Activated += new EventHandler (OnGoUrl);

			ActionEntry[] actions = new ActionEntry[]
			{
				new ActionEntry ("back", Gtk.Stock.GoBack, null, null, GettextCatalog.GetString ("Go back"), new EventHandler (OnBackClicked)),
				new ActionEntry ("forward", Gtk.Stock.GoForward, null, null, GettextCatalog.GetString ("Go forward"), new EventHandler (OnForwardClicked)),
				new ActionEntry ("stop", Gtk.Stock.Stop, null, null, GettextCatalog.GetString ("Stop loading"), new EventHandler (OnStopClicked)),
				new ActionEntry ("reload", Gtk.Stock.Refresh, null, null, GettextCatalog.GetString ("Address"), new EventHandler (OnReloadClicked)),
				new ActionEntry ("go", Gtk.Stock.Ok, null, null, GettextCatalog.GetString ("Load address"), new EventHandler (OnGoUrl))
			};

			ActionGroup ag = new ActionGroup ("navbarGroup");
			ag.Add (actions);

			UIManager uim = new UIManager ();
			uim.InsertActionGroup (ag, 0);
			uim.AddWidget += new AddWidgetHandler (OnAddWidget);
			uim.AddUiFromString (uiInfo);

			ToolItem item = new ToolItem ();
			item.Add (address);
	
			Toolbar tb = uim.GetWidget ("/ui/toolbar") as Toolbar;
			tb.IconSize = size;
			tb.Add (item);
			this.ShowAll ();
		}

		void OnAddWidget (object sender, AddWidgetArgs a)
		{
			a.Widget.Show ();
			this.Add (a.Widget);
		}

		public string Url {
			get {
				return address.Text;
			}
			set {
				address.Text = value;
			}
		}

		void OnGoUrl (object o, EventArgs args)
		{
			if (Go != null)
				Go (this, EventArgs.Empty);
		}

		void OnBackClicked (object o, EventArgs args)
		{
			if (Back != null)
				Back (this, EventArgs.Empty);
		}

		void OnForwardClicked (object o, EventArgs args)
		{
			if (Forward != null)
				Forward (this, EventArgs.Empty);
		}

		void OnStopClicked (object o, EventArgs args)
		{
			if (Stop != null)
				Stop (this, EventArgs.Empty);
		}

		void OnReloadClicked (object o, EventArgs args)
		{
			if (Reload != null)
				Reload (this, EventArgs.Empty);
		}

		public event EventHandler Back;
		public event EventHandler Forward;
		public event EventHandler Stop;
		public event EventHandler Reload;
		public event EventHandler Go;
	}
}

