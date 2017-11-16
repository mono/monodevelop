#pragma warning disable 436

namespace MonoDevelop.SourceEditor.OptionPanels
{
	internal partial class GeneralOptionsPanel
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Label GtkLabel13;

		private global::Gtk.Alignment alignment2;

		private global::Gtk.VBox vbox4;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Fixed fixed2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label1;

		private global::Gtk.ComboBox comboboxLineEndings;

		private global::Gtk.Label GtkLabel14;

		private global::Gtk.Alignment alignment3;

		private global::Gtk.VBox vbox5;

		private global::Gtk.CheckButton foldingCheckbutton;

		private global::Gtk.CheckButton foldregionsCheckbutton;

		private global::Gtk.CheckButton foldCommentsCheckbutton;

		private global::Gtk.Label GtkLabel15;

		private global::Gtk.Alignment alignment4;

		private global::Gtk.VBox vbox6;

		private global::Gtk.CheckButton wordWrapCheckbutton;

		private global::Gtk.CheckButton antiAliasingCheckbutton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.SourceEditor.OptionPanels.GeneralOptionsPanel
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.SourceEditor.OptionPanels.GeneralOptionsPanel";
			// Container child MonoDevelop.SourceEditor.OptionPanels.GeneralOptionsPanel.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkLabel13 = new global::Gtk.Label();
			this.GtkLabel13.Name = "GtkLabel13";
			this.GtkLabel13.Xalign = 0F;
			this.GtkLabel13.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Coding</b>");
			this.GtkLabel13.UseMarkup = true;
			this.vbox1.Add(this.GtkLabel13);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkLabel13]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment2 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment2.Name = "alignment2";
			this.alignment2.LeftPadding = ((uint)(12));
			// Container child alignment2.Gtk.Container+ContainerChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.fixed2 = new global::Gtk.Fixed();
			this.fixed2.Name = "fixed2";
			this.fixed2.HasWindow = false;
			this.hbox3.Add(this.fixed2);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.fixed2]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Padding = ((uint)(6));
			this.vbox4.Add(this.hbox3);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox3]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("_Line ending conversion:");
			this.label1.UseUnderline = true;
			this.hbox1.Add(this.label1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label1]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.comboboxLineEndings = global::Gtk.ComboBox.NewText();
			this.comboboxLineEndings.Name = "comboboxLineEndings";
			this.hbox1.Add(this.comboboxLineEndings);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.comboboxLineEndings]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.vbox4.Add(this.hbox1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox1]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.alignment2.Add(this.vbox4);
			this.vbox1.Add(this.alignment2);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.alignment2]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkLabel14 = new global::Gtk.Label();
			this.GtkLabel14.Name = "GtkLabel14";
			this.GtkLabel14.Xalign = 0F;
			this.GtkLabel14.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Code Folding</b>");
			this.GtkLabel14.UseMarkup = true;
			this.vbox1.Add(this.GtkLabel14);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkLabel14]));
			w9.Position = 2;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment3 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment3.Name = "alignment3";
			this.alignment3.LeftPadding = ((uint)(12));
			// Container child alignment3.Gtk.Container+ContainerChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.foldingCheckbutton = new global::Gtk.CheckButton();
			this.foldingCheckbutton.CanFocus = true;
			this.foldingCheckbutton.Name = "foldingCheckbutton";
			this.foldingCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Enable code _folding");
			this.foldingCheckbutton.DrawIndicator = true;
			this.foldingCheckbutton.UseUnderline = true;
			this.vbox5.Add(this.foldingCheckbutton);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.foldingCheckbutton]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.foldregionsCheckbutton = new global::Gtk.CheckButton();
			this.foldregionsCheckbutton.CanFocus = true;
			this.foldregionsCheckbutton.Name = "foldregionsCheckbutton";
			this.foldregionsCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Fold #_regions by default");
			this.foldregionsCheckbutton.DrawIndicator = true;
			this.foldregionsCheckbutton.UseUnderline = true;
			this.vbox5.Add(this.foldregionsCheckbutton);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.foldregionsCheckbutton]));
			w11.Position = 1;
			w11.Expand = false;
			w11.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.foldCommentsCheckbutton = new global::Gtk.CheckButton();
			this.foldCommentsCheckbutton.CanFocus = true;
			this.foldCommentsCheckbutton.Name = "foldCommentsCheckbutton";
			this.foldCommentsCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Fold _comments by default");
			this.foldCommentsCheckbutton.DrawIndicator = true;
			this.foldCommentsCheckbutton.UseUnderline = true;
			this.vbox5.Add(this.foldCommentsCheckbutton);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.foldCommentsCheckbutton]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			this.alignment3.Add(this.vbox5);
			this.vbox1.Add(this.alignment3);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.alignment3]));
			w14.Position = 3;
			w14.Expand = false;
			w14.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkLabel15 = new global::Gtk.Label();
			this.GtkLabel15.Name = "GtkLabel15";
			this.GtkLabel15.Xalign = 0F;
			this.GtkLabel15.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Appearance</b>");
			this.GtkLabel15.UseMarkup = true;
			this.vbox1.Add(this.GtkLabel15);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkLabel15]));
			w15.Position = 4;
			w15.Expand = false;
			w15.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment4 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment4.Name = "alignment4";
			this.alignment4.LeftPadding = ((uint)(12));
			// Container child alignment4.Gtk.Container+ContainerChild
			this.vbox6 = new global::Gtk.VBox();
			this.vbox6.Name = "vbox6";
			this.vbox6.Spacing = 6;
			// Container child vbox6.Gtk.Box+BoxChild
			this.wordWrapCheckbutton = new global::Gtk.CheckButton();
			this.wordWrapCheckbutton.CanFocus = true;
			this.wordWrapCheckbutton.Name = "wordWrapCheckbutton";
			this.wordWrapCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Word wrap");
			this.wordWrapCheckbutton.DrawIndicator = true;
			this.wordWrapCheckbutton.UseUnderline = true;
			this.vbox6.Add(this.wordWrapCheckbutton);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.wordWrapCheckbutton]));
			w16.Position = 0;
			w16.Expand = false;
			w16.Fill = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.antiAliasingCheckbutton = new global::Gtk.CheckButton();
			this.antiAliasingCheckbutton.CanFocus = true;
			this.antiAliasingCheckbutton.Name = "antiAliasingCheckbutton";
			this.antiAliasingCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Use anti aliasing");
			this.antiAliasingCheckbutton.DrawIndicator = true;
			this.antiAliasingCheckbutton.UseUnderline = true;
			this.vbox6.Add(this.antiAliasingCheckbutton);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.antiAliasingCheckbutton]));
			w17.Position = 1;
			w17.Expand = false;
			w17.Fill = false;
			this.alignment4.Add(this.vbox6);
			this.vbox1.Add(this.alignment4);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.alignment4]));
			w19.Position = 5;
			w19.Expand = false;
			w19.Fill = false;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label1.MnemonicWidget = this.comboboxLineEndings;
			this.Show();
		}
	}
}
#pragma warning restore 436
