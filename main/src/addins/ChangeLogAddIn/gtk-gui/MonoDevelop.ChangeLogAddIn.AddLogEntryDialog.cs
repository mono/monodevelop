#pragma warning disable 436

namespace MonoDevelop.ChangeLogAddIn
{
	internal partial class AddLogEntryDialog
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.VPaned vpaned1;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label1;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.TreeView fileList;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Label label3;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TextView textview;

		private global::Gtk.HBox boxNewFile;

		private global::MonoDevelop.Components.ImageView image36;

		private global::Gtk.Label label7;

		private global::Gtk.HBox boxNoFile;

		private global::MonoDevelop.Components.ImageView image37;

		private global::Gtk.Label label8;

		private global::Gtk.HBox hbox3;

		private global::Gtk.VBox vbox4;

		private global::MonoDevelop.Components.ImageView image38;

		private global::Gtk.Label label9;

		private global::Gtk.Button button7;

		private global::Gtk.Button button119;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.ChangeLogAddIn.AddLogEntryDialog
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.ChangeLogAddIn.AddLogEntryDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("ChangeLog");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.ChangeLogAddIn.AddLogEntryDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Events = ((global::Gdk.EventMask)(256));
			w1.Name = "dialog_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog_VBox.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			this.vbox1.BorderWidth = ((uint)(6));
			// Container child vbox1.Gtk.Box+BoxChild
			this.vpaned1 = new global::Gtk.VPaned();
			this.vpaned1.CanFocus = true;
			this.vpaned1.Name = "vpaned1";
			this.vpaned1.Position = 116;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Modified ChangeLog files:");
			this.vbox2.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.fileList = new global::Gtk.TreeView();
			this.fileList.CanFocus = true;
			this.fileList.Name = "fileList";
			this.fileList.HeadersVisible = false;
			this.scrolledwindow2.Add(this.fileList);
			this.vbox2.Add(this.scrolledwindow2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.scrolledwindow2]));
			w4.Position = 1;
			this.vpaned1.Add(this.vbox2);
			global::Gtk.Paned.PanedChild w5 = ((global::Gtk.Paned.PanedChild)(this.vpaned1[this.vbox2]));
			w5.Resize = false;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("ChangeLog entry:");
			this.vbox3.Add(this.label3);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.label3]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.textview = new global::Gtk.TextView();
			this.textview.CanFocus = true;
			this.textview.Name = "textview";
			this.scrolledwindow1.Add(this.textview);
			this.vbox3.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.scrolledwindow1]));
			w8.Position = 1;
			this.vpaned1.Add(this.vbox3);
			this.vbox1.Add(this.vpaned1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.vpaned1]));
			w10.Position = 0;
			// Container child vbox1.Gtk.Box+BoxChild
			this.boxNewFile = new global::Gtk.HBox();
			this.boxNewFile.Name = "boxNewFile";
			this.boxNewFile.Spacing = 6;
			// Container child boxNewFile.Gtk.Box+BoxChild
			this.image36 = new global::MonoDevelop.Components.ImageView();
			this.image36.Name = "image36";
			this.image36.IconId = "gtk-new";
			this.image36.IconSize = ((global::Gtk.IconSize)(2));
			this.boxNewFile.Add(this.image36);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.boxNewFile[this.image36]));
			w11.Position = 0;
			w11.Expand = false;
			w11.Fill = false;
			// Container child boxNewFile.Gtk.Box+BoxChild
			this.label7 = new global::Gtk.Label();
			this.label7.WidthRequest = 500;
			this.label7.Name = "label7";
			this.label7.Xalign = 0F;
			this.label7.LabelProp = global::Mono.Unix.Catalog.GetString("This ChangeLog file does not exist and will be created.");
			this.label7.Wrap = true;
			this.boxNewFile.Add(this.label7);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.boxNewFile[this.label7]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			this.vbox1.Add(this.boxNewFile);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.boxNewFile]));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.boxNoFile = new global::Gtk.HBox();
			this.boxNoFile.Name = "boxNoFile";
			this.boxNoFile.Spacing = 6;
			// Container child boxNoFile.Gtk.Box+BoxChild
			this.image37 = new global::MonoDevelop.Components.ImageView();
			this.image37.Name = "image37";
			this.image37.IconId = "gtk-dialog-warning";
			this.image37.IconSize = ((global::Gtk.IconSize)(2));
			this.boxNoFile.Add(this.image37);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.boxNoFile[this.image37]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			// Container child boxNoFile.Gtk.Box+BoxChild
			this.label8 = new global::Gtk.Label();
			this.label8.WidthRequest = 500;
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("This ChangeLog file does not exist and will <b>not</b> be created.");
			this.label8.UseMarkup = true;
			this.label8.Wrap = true;
			this.boxNoFile.Add(this.label8);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.boxNoFile[this.label8]));
			w15.Position = 1;
			w15.Expand = false;
			w15.Fill = false;
			this.vbox1.Add(this.boxNoFile);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.boxNoFile]));
			w16.Position = 2;
			w16.Expand = false;
			w16.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.image38 = new global::MonoDevelop.Components.ImageView();
			this.image38.Name = "image38";
			this.image38.IconId = "gtk-dialog-info";
			this.image38.IconSize = ((global::Gtk.IconSize)(2));
			this.vbox4.Add(this.image38);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.image38]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			this.hbox3.Add(this.vbox4);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.vbox4]));
			w18.Position = 0;
			w18.Expand = false;
			w18.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.label9 = new global::Gtk.Label();
			this.label9.WidthRequest = 500;
			this.label9.Name = "label9";
			this.label9.Xalign = 0F;
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("To change the ChangeLog creation and update policies, open the options dialog of " +
					"the project or solution and click on the \'ChangeLog Integration\" section.");
			this.label9.UseMarkup = true;
			this.label9.Wrap = true;
			this.hbox3.Add(this.label9);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.label9]));
			w19.Position = 1;
			w19.Expand = false;
			w19.Fill = false;
			this.vbox1.Add(this.hbox3);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox3]));
			w20.Position = 3;
			w20.Expand = false;
			w20.Fill = false;
			w1.Add(this.vbox1);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(w1[this.vbox1]));
			w21.Position = 0;
			// Internal child MonoDevelop.ChangeLogAddIn.AddLogEntryDialog.ActionArea
			global::Gtk.HButtonBox w22 = this.ActionArea;
			w22.Events = ((global::Gdk.EventMask)(256));
			w22.Name = "ChangeLogAddIn.AddLogEntryDialog_ActionArea";
			w22.Spacing = 6;
			w22.BorderWidth = ((uint)(5));
			w22.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child ChangeLogAddIn.AddLogEntryDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button7 = new global::Gtk.Button();
			this.button7.CanDefault = true;
			this.button7.CanFocus = true;
			this.button7.Name = "button7";
			this.button7.UseStock = true;
			this.button7.UseUnderline = true;
			this.button7.Label = "gtk-cancel";
			this.AddActionWidget(this.button7, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w23 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w22[this.button7]));
			w23.Expand = false;
			w23.Fill = false;
			// Container child ChangeLogAddIn.AddLogEntryDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button119 = new global::Gtk.Button();
			this.button119.CanDefault = true;
			this.button119.CanFocus = true;
			this.button119.Name = "button119";
			this.button119.UseStock = true;
			this.button119.UseUnderline = true;
			this.button119.Label = "gtk-ok";
			this.AddActionWidget(this.button119, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w24 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w22[this.button119]));
			w24.Position = 1;
			w24.Expand = false;
			w24.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 573;
			this.DefaultHeight = 510;
			this.boxNewFile.Hide();
			this.boxNoFile.Hide();
			this.Hide();
		}
	}
}
#pragma warning restore 436
