#pragma warning disable 436

namespace MonoDevelop.Autotools
{
	public partial class MakefileSwitchEditorWidget
	{
		private global::Gtk.VBox dialog1_VBox;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label1;

		private global::Gtk.Alignment alignment2;

		private global::Gtk.Label label2;

		private global::Gtk.Alignment alignment4;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Button addBtn;

		private global::Gtk.Button remBtn;

		private global::Gtk.Alignment alignment3;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView itemTv;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Autotools.MakefileSwitchEditorWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Autotools.MakefileSwitchEditorWidget";
			// Container child MonoDevelop.Autotools.MakefileSwitchEditorWidget.Gtk.Container+ContainerChild
			this.dialog1_VBox = new global::Gtk.VBox();
			this.dialog1_VBox.Name = "dialog1_VBox";
			this.dialog1_VBox.BorderWidth = ((uint)(11));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			// Container child alignment1.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Switch list</b>");
			this.label1.UseMarkup = true;
			this.vbox2.Add(this.label1);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label1]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment2 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment2.Name = "alignment2";
			this.alignment2.BorderWidth = ((uint)(3));
			// Container child alignment2.Gtk.Container+ContainerChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Use the following list to define new switches usable with autotools configure scr" +
					"ipt. Each switch is mapped to a define that you can use to do conditional compil" +
					"ation in your source files.");
			this.label2.Wrap = true;
			this.label2.WidthChars = 78;
			this.alignment2.Add(this.label2);
			this.vbox2.Add(this.alignment2);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment2]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment4 = new global::Gtk.Alignment(1F, 0.5F, 1F, 1F);
			this.alignment4.Name = "alignment4";
			// Container child alignment4.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			this.hbox1.BorderWidth = ((uint)(3));
			// Container child hbox1.Gtk.Box+BoxChild
			this.addBtn = new global::Gtk.Button();
			this.addBtn.CanFocus = true;
			this.addBtn.Name = "addBtn";
			this.addBtn.UseStock = true;
			this.addBtn.UseUnderline = true;
			this.addBtn.Label = "gtk-add";
			this.hbox1.Add(this.addBtn);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.addBtn]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.remBtn = new global::Gtk.Button();
			this.remBtn.CanFocus = true;
			this.remBtn.Name = "remBtn";
			this.remBtn.UseStock = true;
			this.remBtn.UseUnderline = true;
			this.remBtn.Label = "gtk-remove";
			this.hbox1.Add(this.remBtn);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.remBtn]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.alignment4.Add(this.hbox1);
			this.vbox2.Add(this.alignment4);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment4]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment3 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment3.Name = "alignment3";
			this.alignment3.BottomPadding = ((uint)(8));
			this.alignment3.BorderWidth = ((uint)(3));
			// Container child alignment3.Gtk.Container+ContainerChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.itemTv = new global::Gtk.TreeView();
			this.itemTv.CanFocus = true;
			this.itemTv.Name = "itemTv";
			this.GtkScrolledWindow.Add(this.itemTv);
			this.alignment3.Add(this.GtkScrolledWindow);
			this.vbox2.Add(this.alignment3);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment3]));
			w10.Position = 3;
			this.alignment1.Add(this.vbox2);
			this.dialog1_VBox.Add(this.alignment1);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.dialog1_VBox[this.alignment1]));
			w12.Position = 0;
			this.Add(this.dialog1_VBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.addBtn.Clicked += new global::System.EventHandler(this.OnAddBtnClicked);
			this.remBtn.Clicked += new global::System.EventHandler(this.OnRemBtnClicked);
		}
	}
}
#pragma warning restore 436
