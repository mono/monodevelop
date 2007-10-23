using Gtk;
using System;

namespace Stetic.Editor {

	[PropertyEditor ("Value", "Changed")]
	public class ThemedIcon : Gtk.HBox, IPropertyEditor {
		Gtk.Image image;
		Gtk.Entry entry;
		Gtk.Button button;

		public ThemedIcon () : base (false, 6)
		{
			image = new Gtk.Image (Gnome.Stock.Blank, Gtk.IconSize.Button);
			PackStart (image, false, false, 0);

			entry = new Gtk.Entry ();
			PackStart (entry, true, true, 0);
			entry.Changed += entry_Changed;

			button = new Gtk.Button ("...");
			PackStart (button, false, false, 0);
			button.Clicked += button_Clicked;
		}

		public void Initialize (PropertyDescriptor prop)
		{
			if (prop.PropertyType != typeof(string))
				throw new ApplicationException ("ThemedIcon editor does not support editing values of type " + prop.PropertyType);
		}
		
		public void AttachObject (object ob)
		{
		}

		public event EventHandler ValueChanged;

		bool syncing;

		void entry_Changed (object obj, EventArgs args)
		{
			if (!syncing)
				Value = entry.Text;
		}

		void button_Clicked (object obj, EventArgs args)
		{
			Gtk.Window parent = (Gtk.Window)GetAncestor (Gtk.Window.GType);
			Value = ThemedIconBrowser.Browse (parent, (string) Value);
		}

		string icon;
		public object Value {
			get {
				return icon; 
			}
			set {
				string val = (string) value;
				if (icon == val)
					return;

				icon = val;
				Gdk.Pixbuf pix = WidgetUtils.LoadIcon (icon, Gtk.IconSize.Menu);
				if (pix != null) {
					image.Pixbuf = pix;
				} else {
					image.Stock = Gnome.Stock.Blank;
				}

				syncing = true;
				entry.Text = icon;
				syncing = false;

				if (ValueChanged != null)
					ValueChanged (this, EventArgs.Empty);
			}
		}
	}

	public class ThemedIconBrowser : Gtk.Dialog {

		public ThemedIconBrowser (Gtk.Window parent) :
			base ("Select a Themed Icon", parent, Gtk.DialogFlags.Modal,
			      Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			      Gtk.Stock.Ok, Gtk.ResponseType.Ok)
		{
			HasSeparator = false;
			BorderWidth = 12;
			VBox.Spacing = 18;
			VBox.BorderWidth = 0;

			DefaultResponse = Gtk.ResponseType.Ok;

			Gtk.HBox hbox = new Gtk.HBox (false, 12);
			VBox.PackStart (hbox, false, false, 0);

			entry = new Gtk.Entry ();
			entry.Activated += DoFind;
			hbox.PackStart (entry);

			Gtk.Button button = new Gtk.Button (Gtk.Stock.Find);
			button.Clicked += DoFind;
			hbox.PackStart (button, false, false, 0);

			ScrolledWindow scwin = new Gtk.ScrolledWindow ();
			scwin.SizeRequested += ScrolledWindowSizeRequested;
			VBox.PackStart (scwin, true, true, 0);
			scwin.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Automatic);
			scwin.ShadowType = Gtk.ShadowType.In;

			list = new ThemedIconList ();
			scwin.Add (list);
			list.SelectionChanged += ListSelectionChanged;
			list.Activated += ListActivated;
			SetResponseSensitive (Gtk.ResponseType.Ok, false);

			VBox.ShowAll ();
		}

		public static string Browse (Gtk.Window parent, string selection)
		{
			ThemedIconBrowser browser = new ThemedIconBrowser (parent);
			browser.list.Selection = selection;
			int response = browser.Run ();
			if (response == (int)Gtk.ResponseType.Ok)
				selection = browser.list.Selection;
			browser.Destroy ();
			return selection;
		}

		Gtk.Entry entry;
		ThemedIconList list;

		void ScrolledWindowSizeRequested (object obj, SizeRequestedArgs args)
		{
			Gtk.Requisition req = list.SizeRequest ();
			if (req.Width <= 0)
				return;

			Gtk.ScrolledWindow scwin = ((Gtk.ScrolledWindow)obj);
			scwin.SizeRequested -= ScrolledWindowSizeRequested;
			scwin.SetSizeRequest (req.Width, req.Width * 2 / 3);
			ActionArea.BorderWidth = 0; // has to happen post-realize
		}

		void ListSelectionChanged (object obj, EventArgs args)
		{
			SetResponseSensitive (Gtk.ResponseType.Ok, list.Selection != null);
		}

		void ListActivated (object obj, EventArgs args)
		{
			Respond (Gtk.ResponseType.Ok);
		}

		void DoFind (object obj, EventArgs args)
		{
			list.Find (entry.Text);
		}

	}
}
