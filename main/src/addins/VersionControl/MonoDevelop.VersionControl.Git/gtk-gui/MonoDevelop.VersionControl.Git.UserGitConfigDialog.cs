#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class UserGitConfigDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox5;

		private global::Gtk.Label label1;

		private global::Gtk.Alignment alignment8;

		private global::Gtk.Entry usernameEntry;

		private global::Gtk.HBox hbox6;

		private global::Gtk.Label label2;

		private global::Gtk.Alignment alignment7;

		private global::Gtk.Entry emailEntry;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.UserGitConfigDialog
			this.Name = "MonoDevelop.VersionControl.Git.UserGitConfigDialog";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.VersionControl.Git.UserGitConfigDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox5 = new global::Gtk.HBox();
			this.hbox5.Name = "hbox5";
			this.hbox5.Spacing = 6;
			// Container child hbox5.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Username:");
			this.hbox5.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox5.Gtk.Box+BoxChild
			this.alignment8 = new global::Gtk.Alignment(1F, 1F, 0.9F, 1F);
			this.alignment8.Name = "alignment8";
			// Container child alignment8.Gtk.Container+ContainerChild
			this.usernameEntry = new global::Gtk.Entry();
			this.usernameEntry.CanFocus = true;
			this.usernameEntry.Name = "usernameEntry";
			this.usernameEntry.IsEditable = true;
			this.usernameEntry.InvisibleChar = '●';
			this.alignment8.Add(this.usernameEntry);
			this.hbox5.Add(this.alignment8);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.alignment8]));
			w4.Position = 1;
			this.vbox2.Add(this.hbox5);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox5]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox6 = new global::Gtk.HBox();
			this.hbox6.Name = "hbox6";
			this.hbox6.Spacing = 6;
			// Container child hbox6.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Email:");
			this.hbox6.Add(this.label2);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.label2]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child hbox6.Gtk.Box+BoxChild
			this.alignment7 = new global::Gtk.Alignment(1F, 1F, 0.71F, 1F);
			this.alignment7.Name = "alignment7";
			// Container child alignment7.Gtk.Container+ContainerChild
			this.emailEntry = new global::Gtk.Entry();
			this.emailEntry.CanFocus = true;
			this.emailEntry.Name = "emailEntry";
			this.emailEntry.IsEditable = true;
			this.emailEntry.InvisibleChar = '●';
			this.alignment7.Add(this.emailEntry);
			this.hbox6.Add(this.alignment7);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.alignment7]));
			w8.Position = 1;
			this.vbox2.Add(this.hbox6);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox6]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			// Internal child MonoDevelop.VersionControl.Git.UserGitConfigDialog.ActionArea
			global::Gtk.HButtonBox w11 = this.ActionArea;
			w11.Name = "dialog1_ActionArea";
			w11.Spacing = 10;
			w11.BorderWidth = ((uint)(5));
			w11.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w11[this.buttonCancel]));
			w12.Expand = false;
			w12.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w13 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w11[this.buttonOk]));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 332;
			this.DefaultHeight = 184;
			this.Show();
			this.usernameEntry.Changed += new global::System.EventHandler(this.OnChanged);
			this.emailEntry.Changed += new global::System.EventHandler(this.OnChanged);
		}
	}
}
#pragma warning restore 436
