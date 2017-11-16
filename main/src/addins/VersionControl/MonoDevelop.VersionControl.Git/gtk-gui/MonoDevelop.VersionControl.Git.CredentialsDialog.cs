#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class CredentialsDialog
	{
		private global::Gtk.VBox vbox;

		private global::Gtk.Label labelTop;

		private global::Gtk.Label labelTop1;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.CredentialsDialog
			this.Name = "MonoDevelop.VersionControl.Git.CredentialsDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Git Credentials");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.VersionControl.Git.CredentialsDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox = new global::Gtk.VBox();
			this.vbox.Name = "vbox";
			this.vbox.Spacing = 6;
			this.vbox.BorderWidth = ((uint)(9));
			// Container child vbox.Gtk.Box+BoxChild
			this.labelTop = new global::Gtk.Label();
			this.labelTop.Name = "labelTop";
			this.labelTop.Xalign = 0F;
			this.labelTop.LabelProp = global::Mono.Unix.Catalog.GetString("Credentials required for the repository:");
			this.vbox.Add(this.labelTop);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox[this.labelTop]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox.Gtk.Box+BoxChild
			this.labelTop1 = new global::Gtk.Label();
			this.labelTop1.Name = "labelTop1";
			this.labelTop1.Xalign = 0F;
			this.labelTop1.LabelProp = global::Mono.Unix.Catalog.GetString("<b>{0}</b>");
			this.labelTop1.UseMarkup = true;
			this.vbox.Add(this.labelTop1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox[this.labelTop1]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			w1.Add(this.vbox);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(w1[this.vbox]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Internal child MonoDevelop.VersionControl.Git.CredentialsDialog.ActionArea
			global::Gtk.HButtonBox w5 = this.ActionArea;
			w5.Name = "dialog1_ActionArea";
			w5.Spacing = 10;
			w5.BorderWidth = ((uint)(5));
			w5.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w6 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5[this.buttonCancel]));
			w6.Expand = false;
			w6.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w7 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5[this.buttonOk]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 500;
			this.DefaultHeight = 132;
			this.Show();
		}
	}
}
#pragma warning restore 436
