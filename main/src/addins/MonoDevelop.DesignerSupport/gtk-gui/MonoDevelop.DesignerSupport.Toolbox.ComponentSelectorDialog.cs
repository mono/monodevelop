#pragma warning disable 436

namespace MonoDevelop.DesignerSupport.Toolbox
{
	internal partial class ComponentSelectorDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label1;

		private global::Gtk.ComboBox comboType;

		private global::Gtk.VSeparator vseparator1;

		private global::Gtk.Button button24;

		private global::Gtk.HBox hbox2;

		private global::MonoDevelop.Components.ImageView imageview1;

		private global::Gtk.Label label2;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView listView;

		private global::Gtk.CheckButton checkGroupByCat;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.DesignerSupport.Toolbox.ComponentSelectorDialog
			this.Name = "MonoDevelop.DesignerSupport.Toolbox.ComponentSelectorDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Toolbox Item Selector");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.DesignerSupport.Toolbox.ComponentSelectorDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Type of component:");
			this.hbox1.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.comboType = global::Gtk.ComboBox.NewText();
			this.comboType.Name = "comboType";
			this.hbox1.Add(this.comboType);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.comboType]));
			w3.Position = 1;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vseparator1 = new global::Gtk.VSeparator();
			this.vseparator1.Name = "vseparator1";
			this.hbox1.Add(this.vseparator1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vseparator1]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.button24 = new global::Gtk.Button();
			this.button24.CanFocus = true;
			this.button24.Name = "button24";
			// Container child button24.Gtk.Container+ContainerChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 2;
			// Container child hbox2.Gtk.Box+BoxChild
			this.imageview1 = new global::MonoDevelop.Components.ImageView();
			this.imageview1.Name = "imageview1";
			this.imageview1.IconId = "gtk-add";
			this.imageview1.IconSize = ((global::Gtk.IconSize)(1));
			this.hbox2.Add(this.imageview1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.imageview1]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Add Assembly...");
			this.label2.UseUnderline = true;
			this.hbox2.Add(this.label2);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label2]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.button24.Add(this.hbox2);
			this.hbox1.Add(this.button24);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.button24]));
			w8.Position = 3;
			w8.Expand = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.listView = new global::Gtk.TreeView();
			this.listView.CanFocus = true;
			this.listView.Name = "listView";
			this.scrolledwindow1.Add(this.listView);
			this.vbox2.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.scrolledwindow1]));
			w11.Position = 1;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkGroupByCat = new global::Gtk.CheckButton();
			this.checkGroupByCat.CanFocus = true;
			this.checkGroupByCat.Name = "checkGroupByCat";
			this.checkGroupByCat.Label = global::Mono.Unix.Catalog.GetString("Group by component category");
			this.checkGroupByCat.DrawIndicator = true;
			this.checkGroupByCat.UseUnderline = true;
			this.vbox2.Add(this.checkGroupByCat);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkGroupByCat]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w13.Position = 0;
			// Internal child MonoDevelop.DesignerSupport.Toolbox.ComponentSelectorDialog.ActionArea
			global::Gtk.HButtonBox w14 = this.ActionArea;
			w14.Name = "dialog1_ActionArea";
			w14.Spacing = 10;
			w14.BorderWidth = ((uint)(5));
			w14.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w15 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w14[this.buttonCancel]));
			w15.Expand = false;
			w15.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			w14.Add(this.buttonOk);
			global::Gtk.ButtonBox.ButtonBoxChild w16 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w14[this.buttonOk]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 642;
			this.DefaultHeight = 433;
			this.Hide();
			this.comboType.Changed += new global::System.EventHandler(this.OnComboTypeChanged);
			this.button24.Clicked += new global::System.EventHandler(this.OnButton24Clicked);
			this.checkGroupByCat.Clicked += new global::System.EventHandler(this.OnCheckbutton1Clicked);
			this.buttonOk.Clicked += new global::System.EventHandler(this.OnButtonOkClicked);
		}
	}
}
#pragma warning restore 436
