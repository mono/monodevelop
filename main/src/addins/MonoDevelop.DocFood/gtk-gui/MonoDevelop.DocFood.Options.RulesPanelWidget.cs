#pragma warning disable 436

namespace MonoDevelop.DocFood.Options
{
	internal partial class RulesPanelWidget
	{
		private global::Gtk.VBox vbox4;

		private global::Gtk.Label label2;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Button button3;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.DocFood.Options.RulesPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.DocFood.Options.RulesPanelWidget";
			// Container child MonoDevelop.DocFood.Options.RulesPanelWidget.Gtk.Container+ContainerChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("The rules are defined using XML. Click HELP for a full explaination.");
			this.vbox4.Add(this.label2);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.label2]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox4.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.scrolledwindow1]));
			w2.Position = 1;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.button3 = new global::Gtk.Button();
			this.button3.CanFocus = true;
			this.button3.Name = "button3";
			this.button3.UseStock = true;
			this.button3.UseUnderline = true;
			this.button3.Label = "gtk-help";
			this.hbox2.Add(this.button3);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.button3]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			this.vbox4.Add(this.hbox2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox2]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			this.Add(this.vbox4);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
