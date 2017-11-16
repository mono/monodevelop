#pragma warning disable 436

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	internal partial class ClientCertificatePasswordDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label1;

		private global::Gtk.Label labelRealm;

		private global::Gtk.Label label2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label3;

		private global::Gtk.Entry entryPwd;

		private global::Gtk.CheckButton checkSave;

		private global::Gtk.Button button23;

		private global::Gtk.Button button28;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Subversion.Gui.ClientCertificatePasswordDialog
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.VersionControl.Subversion.Gui.ClientCertificatePasswordDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Password for client certificate");
			// Internal child MonoDevelop.VersionControl.Subversion.Gui.ClientCertificatePasswordDialog.VBox
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
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Please provide the passphrase required to access to the certificate:");
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
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Password:");
			this.hbox1.Add(this.label3);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label3]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.entryPwd = new global::Gtk.Entry();
			this.entryPwd.CanFocus = true;
			this.entryPwd.Name = "entryPwd";
			this.entryPwd.IsEditable = true;
			this.entryPwd.Visibility = false;
			this.entryPwd.InvisibleChar = '●';
			this.hbox1.Add(this.entryPwd);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.entryPwd]));
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
			this.checkSave.Label = global::Mono.Unix.Catalog.GetString("Remember password");
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
			// Internal child MonoDevelop.VersionControl.Subversion.Gui.ClientCertificatePasswordDialog.ActionArea
			global::Gtk.HButtonBox w10 = this.ActionArea;
			w10.Events = ((global::Gdk.EventMask)(256));
			w10.Name = "MonoDevelop.VersionControl.Subversion.ClientCertificatePasswordDialog_ActionArea";
			w10.Spacing = 10;
			w10.BorderWidth = ((uint)(5));
			w10.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child MonoDevelop.VersionControl.Subversion.ClientCertificatePasswordDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button23 = new global::Gtk.Button();
			this.button23.CanDefault = true;
			this.button23.CanFocus = true;
			this.button23.Name = "button23";
			this.button23.UseStock = true;
			this.button23.UseUnderline = true;
			this.button23.Label = "gtk-cancel";
			this.AddActionWidget(this.button23, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.button23]));
			w11.Expand = false;
			w11.Fill = false;
			// Container child MonoDevelop.VersionControl.Subversion.ClientCertificatePasswordDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button28 = new global::Gtk.Button();
			this.button28.CanDefault = true;
			this.button28.CanFocus = true;
			this.button28.Name = "button28";
			this.button28.UseStock = true;
			this.button28.UseUnderline = true;
			this.button28.Label = "gtk-ok";
			this.AddActionWidget(this.button28, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.button28]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 414;
			this.DefaultHeight = 217;
			this.Hide();
		}
	}
}
#pragma warning restore 436
