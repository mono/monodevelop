#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class TipOfTheDayWindow
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.VBox vbox3;

		private global::Gtk.HBox hbox7;

		private global::MonoDevelop.Components.ImageView iconInfo;

		private global::Gtk.Label categoryLabel;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.TextView tipTextview;

		private global::Gtk.HBox hbox5;

		private global::Gtk.CheckButton noshowCheckbutton;

		private global::Gtk.HButtonBox hbuttonbox1;

		private global::Gtk.Button nextButton;

		private global::Gtk.Button closeButton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Dialogs.TipOfTheDayWindow
			this.Name = "MonoDevelop.Ide.Gui.Dialogs.TipOfTheDayWindow";
			this.Title = "Tip of the Day";
			this.WindowPosition = ((global::Gtk.WindowPosition)(1));
			this.BorderWidth = ((uint)(6));
			// Container child MonoDevelop.Ide.Gui.Dialogs.TipOfTheDayWindow.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 12;
			this.vbox3.BorderWidth = ((uint)(6));
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox7 = new global::Gtk.HBox();
			this.hbox7.Name = "hbox7";
			this.hbox7.Spacing = 5;
			// Container child hbox7.Gtk.Box+BoxChild
			this.iconInfo = new global::MonoDevelop.Components.ImageView();
			this.iconInfo.Name = "iconInfo";
			this.iconInfo.Xalign = 0F;
			this.iconInfo.Yalign = 0F;
			this.iconInfo.IconId = "gtk-dialog-info";
			this.iconInfo.IconSize = ((global::Gtk.IconSize)(6));
			this.hbox7.Add(this.iconInfo);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox7[this.iconInfo]));
			w1.Position = 0;
			w1.Expand = false;
			// Container child hbox7.Gtk.Box+BoxChild
			this.categoryLabel = new global::Gtk.Label();
			this.categoryLabel.Name = "categoryLabel";
			this.categoryLabel.Xalign = 0F;
			this.categoryLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Did you know...?");
			this.categoryLabel.UseMarkup = true;
			this.categoryLabel.Wrap = true;
			this.hbox7.Add(this.categoryLabel);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox7[this.categoryLabel]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			this.vbox3.Add(this.hbox7);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox7]));
			w3.Position = 0;
			w3.Expand = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.tipTextview = new global::Gtk.TextView();
			this.tipTextview.Buffer.Text = global::Mono.Unix.Catalog.GetString("Did you know that you can design lots of cool things with glade?");
			this.tipTextview.Name = "tipTextview";
			this.tipTextview.Editable = false;
			this.tipTextview.CursorVisible = false;
			this.tipTextview.WrapMode = ((global::Gtk.WrapMode)(2));
			this.scrolledwindow2.Add(this.tipTextview);
			this.vbox3.Add(this.scrolledwindow2);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.scrolledwindow2]));
			w5.Position = 1;
			this.vbox2.Add(this.vbox3);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.vbox3]));
			w6.Position = 0;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox5 = new global::Gtk.HBox();
			this.hbox5.Name = "hbox5";
			this.hbox5.Spacing = 12;
			this.hbox5.BorderWidth = ((uint)(6));
			// Container child hbox5.Gtk.Box+BoxChild
			this.noshowCheckbutton = new global::Gtk.CheckButton();
			this.noshowCheckbutton.Name = "noshowCheckbutton";
			this.noshowCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Show at startup");
			this.noshowCheckbutton.DrawIndicator = true;
			this.noshowCheckbutton.UseUnderline = true;
			this.hbox5.Add(this.noshowCheckbutton);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.noshowCheckbutton]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child hbox5.Gtk.Box+BoxChild
			this.hbuttonbox1 = new global::Gtk.HButtonBox();
			this.hbuttonbox1.Spacing = 10;
			this.hbuttonbox1.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child hbuttonbox1.Gtk.ButtonBox+ButtonBoxChild
			this.nextButton = new global::Gtk.Button();
			this.nextButton.Name = "nextButton";
			this.nextButton.UseUnderline = true;
			this.nextButton.Label = global::Mono.Unix.Catalog.GetString("_Next Tip");
			this.hbuttonbox1.Add(this.nextButton);
			global::Gtk.ButtonBox.ButtonBoxChild w8 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.hbuttonbox1[this.nextButton]));
			w8.Expand = false;
			w8.Fill = false;
			// Container child hbuttonbox1.Gtk.ButtonBox+ButtonBoxChild
			this.closeButton = new global::Gtk.Button();
			this.closeButton.CanDefault = true;
			this.closeButton.Name = "closeButton";
			this.closeButton.UseStock = true;
			this.closeButton.UseUnderline = true;
			this.closeButton.Label = "gtk-close";
			this.hbuttonbox1.Add(this.closeButton);
			global::Gtk.ButtonBox.ButtonBoxChild w9 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.hbuttonbox1[this.closeButton]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			this.hbox5.Add(this.hbuttonbox1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.hbuttonbox1]));
			w10.Position = 1;
			this.vbox2.Add(this.hbox5);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox5]));
			w11.Position = 1;
			w11.Expand = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 500;
			this.DefaultHeight = 285;
			this.Show();
		}
	}
}
#pragma warning restore 436
