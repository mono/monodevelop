#pragma warning disable 436

namespace MonoDevelop.DocFood.Options
{
	internal partial class OfTheReorderingWidget
	{
		private global::Gtk.HBox hbox1;

		private global::Gtk.ScrolledWindow GtkScrolledWindow1;

		private global::Gtk.TextView textview1;

		private global::Gtk.VBox vbox5;

		private global::Gtk.VBox vbox1;

		private global::Gtk.Label label1;

		private global::Gtk.HBox hbox3;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeviewAcronyms;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Button button2;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label label2;

		private global::Gtk.Entry entry1;

		private global::Gtk.Button button1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Label label3;

		private global::Gtk.HBox hbox4;

		private global::Gtk.ScrolledWindow GtkScrolledWindow2;

		private global::Gtk.TreeView treeviewAcronyms1;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Button button3;

		private global::Gtk.HBox hbox5;

		private global::Gtk.Label label4;

		private global::Gtk.Entry entry2;

		private global::Gtk.Button button4;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.DocFood.Options.OfTheReorderingWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.DocFood.Options.OfTheReorderingWidget";
			// Container child MonoDevelop.DocFood.Options.OfTheReorderingWidget.Gtk.Container+ContainerChild
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
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("\"of the\" reordering words:");
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
			this.button2 = new global::Gtk.Button();
			this.button2.CanFocus = true;
			this.button2.Name = "button2";
			this.button2.UseStock = true;
			this.button2.UseUnderline = true;
			this.button2.Label = "gtk-remove";
			this.vbox2.Add(this.button2);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.button2]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			this.hbox3.Add(this.vbox2);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.vbox2]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			this.vbox1.Add(this.hbox3);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox3]));
			w8.Position = 1;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("A_cronym:");
			this.label2.UseUnderline = true;
			this.hbox2.Add(this.label2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label2]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.entry1 = new global::Gtk.Entry();
			this.entry1.CanFocus = true;
			this.entry1.Name = "entry1";
			this.entry1.IsEditable = true;
			this.entry1.InvisibleChar = '●';
			this.hbox2.Add(this.entry1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.entry1]));
			w10.Position = 1;
			// Container child hbox2.Gtk.Box+BoxChild
			this.button1 = new global::Gtk.Button();
			this.button1.CanFocus = true;
			this.button1.Name = "button1";
			this.button1.UseStock = true;
			this.button1.UseUnderline = true;
			this.button1.Label = "gtk-add";
			this.hbox2.Add(this.button1);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.button1]));
			w11.Position = 2;
			w11.Expand = false;
			w11.Fill = false;
			this.vbox1.Add(this.hbox2);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox2]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			this.vbox5.Add(this.vbox1);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.vbox1]));
			w13.Position = 0;
			// Container child vbox5.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Prefix words for the reordering:");
			this.label3.UseUnderline = true;
			this.vbox3.Add(this.label3);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.label3]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			// Container child hbox4.Gtk.Box+BoxChild
			this.GtkScrolledWindow2 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow2.Name = "GtkScrolledWindow2";
			this.GtkScrolledWindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow2.Gtk.Container+ContainerChild
			this.treeviewAcronyms1 = new global::Gtk.TreeView();
			this.treeviewAcronyms1.CanFocus = true;
			this.treeviewAcronyms1.Name = "treeviewAcronyms1";
			this.GtkScrolledWindow2.Add(this.treeviewAcronyms1);
			this.hbox4.Add(this.GtkScrolledWindow2);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.GtkScrolledWindow2]));
			w16.Position = 0;
			// Container child hbox4.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.button3 = new global::Gtk.Button();
			this.button3.CanFocus = true;
			this.button3.Name = "button3";
			this.button3.UseStock = true;
			this.button3.UseUnderline = true;
			this.button3.Label = "gtk-remove";
			this.vbox4.Add(this.button3);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.button3]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			this.hbox4.Add(this.vbox4);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.vbox4]));
			w18.Position = 1;
			w18.Expand = false;
			w18.Fill = false;
			this.vbox3.Add(this.hbox4);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox4]));
			w19.Position = 1;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox5 = new global::Gtk.HBox();
			this.hbox5.Name = "hbox5";
			this.hbox5.Spacing = 6;
			// Container child hbox5.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("A_cronym:");
			this.label4.UseUnderline = true;
			this.hbox5.Add(this.label4);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.label4]));
			w20.Position = 0;
			w20.Expand = false;
			w20.Fill = false;
			// Container child hbox5.Gtk.Box+BoxChild
			this.entry2 = new global::Gtk.Entry();
			this.entry2.CanFocus = true;
			this.entry2.Name = "entry2";
			this.entry2.IsEditable = true;
			this.entry2.InvisibleChar = '●';
			this.hbox5.Add(this.entry2);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.entry2]));
			w21.Position = 1;
			// Container child hbox5.Gtk.Box+BoxChild
			this.button4 = new global::Gtk.Button();
			this.button4.CanFocus = true;
			this.button4.Name = "button4";
			this.button4.UseStock = true;
			this.button4.UseUnderline = true;
			this.button4.Label = "gtk-add";
			this.hbox5.Add(this.button4);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.button4]));
			w22.Position = 2;
			w22.Expand = false;
			w22.Fill = false;
			this.vbox3.Add(this.hbox5);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox5]));
			w23.Position = 2;
			w23.Expand = false;
			w23.Fill = false;
			this.vbox5.Add(this.vbox3);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.vbox3]));
			w24.Position = 1;
			this.hbox1.Add(this.vbox5);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox5]));
			w25.Position = 1;
			w25.Expand = false;
			w25.Fill = false;
			this.Add(this.hbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label1.MnemonicWidget = this.treeviewAcronyms;
			this.label3.MnemonicWidget = this.treeviewAcronyms;
			this.Hide();
		}
	}
}
#pragma warning restore 436
