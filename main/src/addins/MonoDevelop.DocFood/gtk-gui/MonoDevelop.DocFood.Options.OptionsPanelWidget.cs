#pragma warning disable 436

namespace MonoDevelop.DocFood.Options
{
	internal partial class OptionsPanelWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.CheckButton checkbutton1;

		private global::Gtk.Frame frame1;

		private global::Gtk.Alignment GtkAlignment;

		private global::Gtk.VBox vbox3;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeview1;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Button button1;

		private global::Gtk.Button button2;

		private global::Gtk.Label GtkLabel2;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.DocFood.Options.OptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.DocFood.Options.OptionsPanelWidget";
			// Container child MonoDevelop.DocFood.Options.OptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkbutton1 = new global::Gtk.CheckButton();
			this.checkbutton1.CanFocus = true;
			this.checkbutton1.Name = "checkbutton1";
			this.checkbutton1.Label = global::Mono.Unix.Catalog.GetString("Enable automatic documentation");
			this.checkbutton1.DrawIndicator = true;
			this.checkbutton1.UseUnderline = true;
			this.vbox1.Add(this.checkbutton1);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkbutton1]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.frame1 = new global::Gtk.Frame();
			this.frame1.Name = "frame1";
			this.frame1.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child frame1.Gtk.Container+ContainerChild
			this.GtkAlignment = new global::Gtk.Alignment(0F, 0F, 1F, 1F);
			this.GtkAlignment.Name = "GtkAlignment";
			this.GtkAlignment.LeftPadding = ((uint)(12));
			// Container child GtkAlignment.Gtk.Container+ContainerChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeview1 = new global::Gtk.TreeView();
			this.treeview1.CanFocus = true;
			this.treeview1.Name = "treeview1";
			this.GtkScrolledWindow.Add(this.treeview1);
			this.vbox3.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.GtkScrolledWindow]));
			w3.Position = 0;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.button1 = new global::Gtk.Button();
			this.button1.CanFocus = true;
			this.button1.Name = "button1";
			this.button1.UseStock = true;
			this.button1.UseUnderline = true;
			this.button1.Label = "gtk-add";
			this.hbox1.Add(this.button1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.button1]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.button2 = new global::Gtk.Button();
			this.button2.CanFocus = true;
			this.button2.Name = "button2";
			this.button2.UseStock = true;
			this.button2.UseUnderline = true;
			this.button2.Label = "gtk-remove";
			this.hbox1.Add(this.button2);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.button2]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.vbox3.Add(this.hbox1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox1]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.GtkAlignment.Add(this.vbox3);
			this.frame1.Add(this.GtkAlignment);
			this.GtkLabel2 = new global::Gtk.Label();
			this.GtkLabel2.Name = "GtkLabel2";
			this.GtkLabel2.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Macros</b>");
			this.GtkLabel2.UseMarkup = true;
			this.frame1.LabelWidget = this.GtkLabel2;
			this.vbox1.Add(this.frame1);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.frame1]));
			w9.Position = 1;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
