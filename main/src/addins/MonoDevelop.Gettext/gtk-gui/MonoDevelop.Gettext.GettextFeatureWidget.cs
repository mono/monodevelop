#pragma warning disable 436

namespace MonoDevelop.Gettext
{
	internal partial class GettextFeatureWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label4;

		private global::Gtk.HBox hbox1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Frame frame1;

		private global::Gtk.TreeView treeviewTranslations;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Button buttonAdd;

		private global::Gtk.Button buttonRemove;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Gettext.GettextFeatureWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Gettext.GettextFeatureWidget";
			// Container child MonoDevelop.Gettext.GettextFeatureWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(8));
			// Container child vbox2.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Select the languages you want to support (more languages can be added later):");
			this.vbox2.Add(this.label4);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label4]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.frame1 = new global::Gtk.Frame();
			this.frame1.Name = "frame1";
			this.frame1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child frame1.Gtk.Container+ContainerChild
			this.treeviewTranslations = new global::Gtk.TreeView();
			this.treeviewTranslations.WidthRequest = 200;
			this.treeviewTranslations.CanFocus = true;
			this.treeviewTranslations.Name = "treeviewTranslations";
			this.frame1.Add(this.treeviewTranslations);
			this.vbox3.Add(this.frame1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.frame1]));
			w3.Position = 0;
			this.hbox1.Add(this.vbox3);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox3]));
			w4.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.buttonAdd = new global::Gtk.Button();
			this.buttonAdd.CanFocus = true;
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.UseStock = true;
			this.buttonAdd.UseUnderline = true;
			this.buttonAdd.Label = "gtk-add";
			this.vbox4.Add(this.buttonAdd);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.buttonAdd]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.buttonRemove = new global::Gtk.Button();
			this.buttonRemove.CanFocus = true;
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseStock = true;
			this.buttonRemove.UseUnderline = true;
			this.buttonRemove.Label = "gtk-remove";
			this.vbox4.Add(this.buttonRemove);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.buttonRemove]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.hbox1.Add(this.vbox4);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox4]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
