#pragma warning disable 436

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	internal partial class ClientCertificateDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label1;

		private global::Gtk.Label labelRealm;

		private global::Gtk.Label label2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label3;

		private global::MonoDevelop.Components.FileEntry fileentry;

		private global::Gtk.CheckButton checkSave;

		private global::Gtk.Button button34;

		private global::Gtk.Button button24;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Subversion.Gui.ClientCertificateDialog
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.VersionControl.Subversion.Gui.ClientCertificateDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Client Certificate Required");
			// Internal child MonoDevelop.VersionControl.Subversion.Gui.ClientCertificateDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Events = ((global::Gdk.EventMask)(256));
			w1.Name = "dialog_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("<b>A client certificate is needed to connect to the repository</b>");
			this.label1.UseMarkup = true;
			this.vbox2.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.labelRealm = new global::Gtk.Label();
			this.labelRealm.Name = "labelRealm";
			this.labelRealm.Xalign = 0F;
			this.labelRealm.LabelProp = "Realm";
			this.vbox2.Add(this.labelRealm);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.labelRealm]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Please provide a path to the required certificate:");
			this.vbox2.Add(this.label2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label2]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			w4.Padding = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("File:");
			this.hbox1.Add(this.label3);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label3]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.fileentry = new global::MonoDevelop.Components.FileEntry();
			this.fileentry.Name = "fileentry";
			this.hbox1.Add(this.fileentry);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.fileentry]));
			w6.Position = 1;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w7.Position = 3;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkSave = new global::Gtk.CheckButton();
			this.checkSave.CanFocus = true;
			this.checkSave.Name = "checkSave";
			this.checkSave.Label = global::Mono.Unix.Catalog.GetString("Remember certificate location");
			this.checkSave.DrawIndicator = true;
			this.checkSave.UseUnderline = true;
			this.vbox2.Add(this.checkSave);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkSave]));
			w8.Position = 4;
			w8.Expand = false;
			w8.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Internal child MonoDevelop.VersionControl.Subversion.Gui.ClientCertificateDialog.ActionArea
			global::Gtk.HButtonBox w10 = this.ActionArea;
			w10.Events = ((global::Gdk.EventMask)(256));
			w10.Name = "MonoDevelop.VersionControl.Subversion.ClientCertificateDialog_ActionArea";
			w10.Spacing = 10;
			w10.BorderWidth = ((uint)(5));
			w10.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child MonoDevelop.VersionControl.Subversion.ClientCertificateDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button34 = new global::Gtk.Button();
			this.button34.CanDefault = true;
			this.button34.CanFocus = true;
			this.button34.Name = "button34";
			this.button34.UseStock = true;
			this.button34.UseUnderline = true;
			this.button34.Label = "gtk-cancel";
			this.AddActionWidget(this.button34, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.button34]));
			w11.Expand = false;
			w11.Fill = false;
			// Container child MonoDevelop.VersionControl.Subversion.ClientCertificateDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button24 = new global::Gtk.Button();
			this.button24.CanDefault = true;
			this.button24.CanFocus = true;
			this.button24.Name = "button24";
			this.button24.UseStock = true;
			this.button24.UseUnderline = true;
			this.button24.Label = "gtk-ok";
			this.AddActionWidget(this.button24, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.button24]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 492;
			this.DefaultHeight = 213;
			this.Hide();
		}
	}
}
#pragma warning restore 436
