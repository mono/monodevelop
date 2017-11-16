#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal partial class AddInsPanelWidget
	{
		private global::Gtk.VBox vbox72;

		private global::Gtk.Label label2;

		private global::Gtk.Alignment alignment3;

		private global::Gtk.VBox vbox5;

		private global::Gtk.RadioButton radioHour;

		private global::Gtk.RadioButton radioDay;

		private global::Gtk.RadioButton radioMonth;

		private global::Gtk.RadioButton radioNever;

		private global::Gtk.CheckButton checkUnstable;

		private global::Gtk.HBox hbox47;

		private global::Gtk.Alignment boxUnstable;

		private global::Gtk.VBox vbox6;

		private global::Gtk.RadioButton radioBeta;

		private global::Gtk.RadioButton radioAlpha;

		private global::Gtk.RadioButton radioTest;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Button buttonUpdateNow;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.OptionPanels.AddInsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.OptionPanels.AddInsPanelWidget";
			// Container child MonoDevelop.Ide.Gui.OptionPanels.AddInsPanelWidget.Gtk.Container+ContainerChild
			this.vbox72 = new global::Gtk.VBox();
			this.vbox72.Name = "vbox72";
			this.vbox72.Spacing = 6;
			// Container child vbox72.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Automatically check for updates:");
			this.vbox72.Add(this.label2);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox72[this.label2]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox72.Gtk.Box+BoxChild
			this.alignment3 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment3.Name = "alignment3";
			this.alignment3.LeftPadding = ((uint)(24));
			this.alignment3.BottomPadding = ((uint)(6));
			// Container child alignment3.Gtk.Container+ContainerChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.radioHour = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Every hour"));
			this.radioHour.CanFocus = true;
			this.radioHour.Name = "radioHour";
			this.radioHour.Active = true;
			this.radioHour.DrawIndicator = true;
			this.radioHour.UseUnderline = true;
			this.radioHour.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.vbox5.Add(this.radioHour);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.radioHour]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.radioDay = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Every day"));
			this.radioDay.CanFocus = true;
			this.radioDay.Name = "radioDay";
			this.radioDay.DrawIndicator = true;
			this.radioDay.UseUnderline = true;
			this.radioDay.Group = this.radioHour.Group;
			this.vbox5.Add(this.radioDay);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.radioDay]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.radioMonth = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Every month"));
			this.radioMonth.CanFocus = true;
			this.radioMonth.Name = "radioMonth";
			this.radioMonth.DrawIndicator = true;
			this.radioMonth.UseUnderline = true;
			this.radioMonth.Group = this.radioHour.Group;
			this.vbox5.Add(this.radioMonth);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.radioMonth]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.radioNever = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Never"));
			this.radioNever.CanFocus = true;
			this.radioNever.Name = "radioNever";
			this.radioNever.DrawIndicator = true;
			this.radioNever.UseUnderline = true;
			this.radioNever.Group = this.radioHour.Group;
			this.vbox5.Add(this.radioNever);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.radioNever]));
			w5.Position = 3;
			w5.Expand = false;
			w5.Fill = false;
			this.alignment3.Add(this.vbox5);
			this.vbox72.Add(this.alignment3);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox72[this.alignment3]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox72.Gtk.Box+BoxChild
			this.checkUnstable = new global::Gtk.CheckButton();
			this.checkUnstable.CanFocus = true;
			this.checkUnstable.Name = "checkUnstable";
			this.checkUnstable.Label = global::Mono.Unix.Catalog.GetString("Install unstable developer updates");
			this.checkUnstable.Active = true;
			this.checkUnstable.DrawIndicator = true;
			this.checkUnstable.UseUnderline = true;
			this.vbox72.Add(this.checkUnstable);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox72[this.checkUnstable]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox72.Gtk.Box+BoxChild
			this.hbox47 = new global::Gtk.HBox();
			this.hbox47.Name = "hbox47";
			// Container child hbox47.Gtk.Box+BoxChild
			this.boxUnstable = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.boxUnstable.Name = "boxUnstable";
			this.boxUnstable.LeftPadding = ((uint)(24));
			// Container child boxUnstable.Gtk.Container+ContainerChild
			this.vbox6 = new global::Gtk.VBox();
			this.vbox6.Name = "vbox6";
			this.vbox6.Spacing = 6;
			// Container child vbox6.Gtk.Box+BoxChild
			this.radioBeta = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Beta updates (weekly)"));
			this.radioBeta.CanFocus = true;
			this.radioBeta.Name = "radioBeta";
			this.radioBeta.Active = true;
			this.radioBeta.DrawIndicator = true;
			this.radioBeta.UseUnderline = true;
			this.radioBeta.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.vbox6.Add(this.radioBeta);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.radioBeta]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.radioAlpha = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Alpha updates (very often, very unstable)"));
			this.radioAlpha.CanFocus = true;
			this.radioAlpha.Name = "radioAlpha";
			this.radioAlpha.DrawIndicator = true;
			this.radioAlpha.UseUnderline = true;
			this.radioAlpha.Group = this.radioBeta.Group;
			this.vbox6.Add(this.radioAlpha);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.radioAlpha]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.radioTest = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Test"));
			this.radioTest.CanFocus = true;
			this.radioTest.Name = "radioTest";
			this.radioTest.DrawIndicator = true;
			this.radioTest.UseUnderline = true;
			this.radioTest.Group = this.radioBeta.Group;
			this.vbox6.Add(this.radioTest);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.radioTest]));
			w11.Position = 2;
			w11.Expand = false;
			w11.Fill = false;
			this.boxUnstable.Add(this.vbox6);
			this.hbox47.Add(this.boxUnstable);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.hbox47[this.boxUnstable]));
			w13.Position = 0;
			this.vbox72.Add(this.hbox47);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox72[this.hbox47]));
			w14.Position = 3;
			w14.Expand = false;
			w14.Fill = false;
			// Container child vbox72.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			this.hbox3.BorderWidth = ((uint)(12));
			// Container child hbox3.Gtk.Box+BoxChild
			this.buttonUpdateNow = new global::Gtk.Button();
			this.buttonUpdateNow.CanFocus = true;
			this.buttonUpdateNow.Name = "buttonUpdateNow";
			this.buttonUpdateNow.UseUnderline = true;
			this.buttonUpdateNow.Label = global::Mono.Unix.Catalog.GetString("Check for Updates Now");
			this.hbox3.Add(this.buttonUpdateNow);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.buttonUpdateNow]));
			w15.Position = 0;
			w15.Expand = false;
			w15.Fill = false;
			this.vbox72.Add(this.hbox3);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox72[this.hbox3]));
			w16.Position = 4;
			w16.Expand = false;
			w16.Fill = false;
			this.Add(this.vbox72);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.radioTest.Hide();
			this.Show();
			this.checkUnstable.Toggled += new global::System.EventHandler(this.OnCheckUnstableToggled);
			this.buttonUpdateNow.Clicked += new global::System.EventHandler(this.OnButtonUpdateNowClicked);
		}
	}
}
#pragma warning restore 436
