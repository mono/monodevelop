#pragma warning disable 436

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	internal partial class UserPasswordDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label4;

		private global::Gtk.Label labelRealm;

		private global::Gtk.Table table1;

		private global::Gtk.Entry entryPwd;

		private global::Gtk.Entry entryUser;

		private global::Gtk.Label label2;

		private global::Gtk.Label labelPwd;

		private global::Gtk.CheckButton checkSavePwd;

		private global::Gtk.Button button1;

		private global::Gtk.Button button7;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Subversion.Gui.UserPasswordDialog
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.VersionControl.Subversion.Gui.UserPasswordDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Subversion");
			this.Modal = true;
			this.Resizable = false;
			this.AllowGrow = false;
			// Internal child MonoDevelop.VersionControl.Subversion.Gui.UserPasswordDialog.VBox
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
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("<b>User credentials are required to access the Subversion repository.</b>");
			this.label4.UseMarkup = true;
			this.vbox2.Add(this.label4);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label4]));
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
			this.table1 = new global::Gtk.Table(((uint)(2)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.entryPwd = new global::Gtk.Entry();
			this.entryPwd.CanFocus = true;
			this.entryPwd.Name = "entryPwd";
			this.entryPwd.IsEditable = true;
			this.entryPwd.Visibility = false;
			this.entryPwd.InvisibleChar = '●';
			this.table1.Add(this.entryPwd);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.entryPwd]));
			w4.TopAttach = ((uint)(1));
			w4.BottomAttach = ((uint)(2));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(2));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryUser = new global::Gtk.Entry();
			this.entryUser.CanFocus = true;
			this.entryUser.Name = "entryUser";
			this.entryUser.IsEditable = true;
			this.entryUser.InvisibleChar = '●';
			this.table1.Add(this.entryUser);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.entryUser]));
			w5.LeftAttach = ((uint)(1));
			w5.RightAttach = ((uint)(2));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("User:");
			this.table1.Add(this.label2);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.label2]));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.labelPwd = new global::Gtk.Label();
			this.labelPwd.Name = "labelPwd";
			this.labelPwd.Xalign = 0F;
			this.labelPwd.LabelProp = global::Mono.Unix.Catalog.GetString("Password:");
			this.table1.Add(this.labelPwd);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table1[this.labelPwd]));
			w7.TopAttach = ((uint)(1));
			w7.BottomAttach = ((uint)(2));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox2.Add(this.table1);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.table1]));
			w8.Position = 2;
			w8.Padding = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkSavePwd = new global::Gtk.CheckButton();
			this.checkSavePwd.CanFocus = true;
			this.checkSavePwd.Name = "checkSavePwd";
			this.checkSavePwd.Label = global::Mono.Unix.Catalog.GetString("Remember password");
			this.checkSavePwd.DrawIndicator = true;
			this.checkSavePwd.UseUnderline = true;
			this.vbox2.Add(this.checkSavePwd);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkSavePwd]));
			w9.Position = 3;
			w9.Expand = false;
			w9.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w10.Position = 0;
			// Internal child MonoDevelop.VersionControl.Subversion.Gui.UserPasswordDialog.ActionArea
			global::Gtk.HButtonBox w11 = this.ActionArea;
			w11.Events = ((global::Gdk.EventMask)(256));
			w11.Name = "MonoDevelop.VersionControl.Subversion.UserPasswordDialog_ActionArea";
			w11.Spacing = 10;
			w11.BorderWidth = ((uint)(5));
			w11.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child MonoDevelop.VersionControl.Subversion.UserPasswordDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button1 = new global::Gtk.Button();
			this.button1.CanDefault = true;
			this.button1.CanFocus = true;
			this.button1.Name = "button1";
			this.button1.UseStock = true;
			this.button1.UseUnderline = true;
			this.button1.Label = "gtk-cancel";
			this.AddActionWidget(this.button1, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w11[this.button1]));
			w12.Expand = false;
			w12.Fill = false;
			// Container child MonoDevelop.VersionControl.Subversion.UserPasswordDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button7 = new global::Gtk.Button();
			this.button7.CanDefault = true;
			this.button7.CanFocus = true;
			this.button7.Name = "button7";
			this.button7.UseStock = true;
			this.button7.UseUnderline = true;
			this.button7.Label = "gtk-ok";
			this.AddActionWidget(this.button7, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w13 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w11[this.button7]));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 433;
			this.DefaultHeight = 229;
			this.Hide();
		}
	}
}
#pragma warning restore 436
