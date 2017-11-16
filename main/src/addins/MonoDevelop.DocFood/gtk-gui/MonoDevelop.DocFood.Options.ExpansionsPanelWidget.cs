#pragma warning disable 436

namespace MonoDevelop.DocFood.Options
{
	internal partial class ExpansionsPanelWidget
	{
		private global::Gtk.HBox hbox1;

		private global::Gtk.ScrolledWindow GtkScrolledWindow1;

		private global::Gtk.TextView textview1;

		private global::Gtk.VBox vbox1;

		private global::Gtk.Label label1;

		private global::Gtk.HBox hbox3;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeviewAcronyms;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Button button1;

		private global::Gtk.Button button2;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.DocFood.Options.ExpansionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.DocFood.Options.ExpansionsPanelWidget";
			// Container child MonoDevelop.DocFood.Options.ExpansionsPanelWidget.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.GtkScrolledWindow1 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow1.Name = "GtkScrolledWindow1";
			this.GtkScrolledWindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow1.Gtk.Container+ContainerChild
			this.textview1 = new global::Gtk.TextView();
			this.textview1.Buffer.Text = global::Mono.Unix.Catalog.GetString("All words that consist of only consonants (like sql) and/or upper case letters (l" +
					"ike HTML) are treated as acronyms. However the acronym recognition can be improv" +
					"ed by defining acronyms.");
			this.textview1.CanFocus = true;
			this.textview1.Name = "textview1";
			this.textview1.Editable = false;
			this.textview1.CursorVisible = false;
			this.textview1.WrapMode = ((global::Gtk.WrapMode)(2));
			this.GtkScrolledWindow1.Add(this.textview1);
			this.hbox1.Add(this.GtkScrolledWindow1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.GtkScrolledWindow1]));
			w2.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("_Acronyms:");
			this.label1.UseUnderline = true;
			this.vbox1.Add(this.label1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label1]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeviewAcronyms = new global::Gtk.TreeView();
			this.treeviewAcronyms.CanFocus = true;
			this.treeviewAcronyms.Name = "treeviewAcronyms";
			this.GtkScrolledWindow.Add(this.treeviewAcronyms);
			this.hbox3.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.GtkScrolledWindow]));
			w5.Position = 0;
			// Container child hbox3.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.button1 = new global::Gtk.Button();
			this.button1.CanFocus = true;
			this.button1.Name = "button1";
			this.button1.UseStock = true;
			this.button1.UseUnderline = true;
			this.button1.Label = "gtk-add";
			this.vbox2.Add(this.button1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.button1]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.button2 = new global::Gtk.Button();
			this.button2.CanFocus = true;
			this.button2.Name = "button2";
			this.button2.UseStock = true;
			this.button2.UseUnderline = true;
			this.button2.Label = "gtk-remove";
			this.vbox2.Add(this.button2);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.button2]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			this.hbox3.Add(this.vbox2);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.vbox2]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			this.vbox1.Add(this.hbox3);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox3]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			this.hbox1.Add(this.vbox1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox1]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.Add(this.hbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label1.MnemonicWidget = this.treeviewAcronyms;
			this.Hide();
		}
	}
}
#pragma warning restore 436
