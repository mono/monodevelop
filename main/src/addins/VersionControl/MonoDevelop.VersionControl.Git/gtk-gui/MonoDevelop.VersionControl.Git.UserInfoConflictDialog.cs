#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class UserInfoConflictDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label1;

		private global::Gtk.RadioButton radioMD;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.Label labelMD;

		private global::Gtk.Alignment alignment3;

		private global::Gtk.Label label6;

		private global::Gtk.RadioButton radiobutton2;

		private global::Gtk.Alignment alignment2;

		private global::Gtk.Label labelGit;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.UserInfoConflictDialog
			this.Name = "MonoDevelop.VersionControl.Git.UserInfoConflictDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("User Information Conflict");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.VersionControl.Git.UserInfoConflictDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(9));
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.WidthRequest = 503;
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("The user name and email configured for the Git repository does not match the user" +
					" information configured in MonoDevelop. Which user information do you want to us" +
					"e?");
			this.label1.Wrap = true;
			this.vbox2.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.radioMD = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Use the MonoDevelop configuration:"));
			this.radioMD.CanFocus = true;
			this.radioMD.Name = "radioMD";
			this.radioMD.Active = true;
			this.radioMD.DrawIndicator = true;
			this.radioMD.UseUnderline = true;
			this.radioMD.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.vbox2.Add(this.radioMD);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.radioMD]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.LeftPadding = ((uint)(27));
			// Container child alignment1.Gtk.Container+ContainerChild
			this.labelMD = new global::Gtk.Label();
			this.labelMD.Name = "labelMD";
			this.labelMD.Xalign = 0F;
			this.labelMD.LabelProp = "name <email>";
			this.alignment1.Add(this.labelMD);
			this.vbox2.Add(this.alignment1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment1]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment3 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment3.Name = "alignment3";
			this.alignment3.LeftPadding = ((uint)(27));
			// Container child alignment3.Gtk.Container+ContainerChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("If you chose this option the Git configuration will be overwritten.");
			this.alignment3.Add(this.label6);
			this.vbox2.Add(this.alignment3);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment3]));
			w7.Position = 3;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.radiobutton2 = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Use the Git configuration:"));
			this.radiobutton2.CanFocus = true;
			this.radiobutton2.Name = "radiobutton2";
			this.radiobutton2.DrawIndicator = true;
			this.radiobutton2.UseUnderline = true;
			this.radiobutton2.Group = this.radioMD.Group;
			this.vbox2.Add(this.radiobutton2);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.radiobutton2]));
			w8.Position = 4;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment2 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment2.Name = "alignment2";
			this.alignment2.LeftPadding = ((uint)(27));
			// Container child alignment2.Gtk.Container+ContainerChild
			this.labelGit = new global::Gtk.Label();
			this.labelGit.Name = "labelGit";
			this.labelGit.Xalign = 0F;
			this.labelGit.LabelProp = "name <email>";
			this.alignment2.Add(this.labelGit);
			this.vbox2.Add(this.alignment2);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment2]));
			w10.Position = 5;
			w10.Expand = false;
			w10.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w11.Position = 0;
			w11.Expand = false;
			w11.Fill = false;
			// Internal child MonoDevelop.VersionControl.Git.UserInfoConflictDialog.ActionArea
			global::Gtk.HButtonBox w12 = this.ActionArea;
			w12.Name = "dialog1_ActionArea";
			w12.Spacing = 10;
			w12.BorderWidth = ((uint)(5));
			w12.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w13 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w12[this.buttonCancel]));
			w13.Expand = false;
			w13.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w14 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w12[this.buttonOk]));
			w14.Position = 1;
			w14.Expand = false;
			w14.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 529;
			this.DefaultHeight = 249;
			this.Hide();
		}
	}
}
#pragma warning restore 436
