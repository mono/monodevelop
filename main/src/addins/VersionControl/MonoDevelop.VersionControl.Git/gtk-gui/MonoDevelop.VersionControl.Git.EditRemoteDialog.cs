#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class EditRemoteDialog
	{
		private global::Gtk.VBox vbox7;

		private global::Gtk.Table table3;

		private global::Gtk.Entry entryName;

		private global::Gtk.Entry entryPushUrl;

		private global::Gtk.Entry entryUrl;

		private global::Gtk.Label label7;

		private global::Gtk.Label label8;

		private global::Gtk.Label label9;

		private global::Gtk.CheckButton checkImportTags;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.EditRemoteDialog
			this.Name = "MonoDevelop.VersionControl.Git.EditRemoteDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Remote Source");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.VersionControl.Git.EditRemoteDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox7 = new global::Gtk.VBox();
			this.vbox7.Name = "vbox7";
			this.vbox7.Spacing = 6;
			this.vbox7.BorderWidth = ((uint)(9));
			// Container child vbox7.Gtk.Box+BoxChild
			this.table3 = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table3.Name = "table3";
			this.table3.RowSpacing = ((uint)(6));
			this.table3.ColumnSpacing = ((uint)(6));
			// Container child table3.Gtk.Table+TableChild
			this.entryName = new global::Gtk.Entry();
			this.entryName.CanFocus = true;
			this.entryName.Name = "entryName";
			this.entryName.IsEditable = true;
			this.entryName.InvisibleChar = '●';
			this.table3.Add(this.entryName);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table3[this.entryName]));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.entryPushUrl = new global::Gtk.Entry();
			this.entryPushUrl.CanFocus = true;
			this.entryPushUrl.Name = "entryPushUrl";
			this.entryPushUrl.IsEditable = true;
			this.entryPushUrl.InvisibleChar = '●';
			this.table3.Add(this.entryPushUrl);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table3[this.entryPushUrl]));
			w3.TopAttach = ((uint)(2));
			w3.BottomAttach = ((uint)(3));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.entryUrl = new global::Gtk.Entry();
			this.entryUrl.CanFocus = true;
			this.entryUrl.Name = "entryUrl";
			this.entryUrl.IsEditable = true;
			this.entryUrl.InvisibleChar = '●';
			this.table3.Add(this.entryUrl);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table3[this.entryUrl]));
			w4.TopAttach = ((uint)(1));
			w4.BottomAttach = ((uint)(2));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(2));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.label7 = new global::Gtk.Label();
			this.label7.Name = "label7";
			this.label7.Xalign = 0F;
			this.label7.LabelProp = global::Mono.Unix.Catalog.GetString("Name:");
			this.table3.Add(this.label7);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table3[this.label7]));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("Url:");
			this.table3.Add(this.label8);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table3[this.label8]));
			w6.TopAttach = ((uint)(1));
			w6.BottomAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.label9 = new global::Gtk.Label();
			this.label9.Name = "label9";
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("Push Url:");
			this.table3.Add(this.label9);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table3[this.label9]));
			w7.TopAttach = ((uint)(2));
			w7.BottomAttach = ((uint)(3));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox7.Add(this.table3);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.table3]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox7.Gtk.Box+BoxChild
			this.checkImportTags = new global::Gtk.CheckButton();
			this.checkImportTags.CanFocus = true;
			this.checkImportTags.Name = "checkImportTags";
			this.checkImportTags.Label = global::Mono.Unix.Catalog.GetString("Import tags");
			this.checkImportTags.Active = true;
			this.checkImportTags.DrawIndicator = true;
			this.checkImportTags.UseUnderline = true;
			this.vbox7.Add(this.checkImportTags);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.checkImportTags]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			w1.Add(this.vbox7);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(w1[this.vbox7]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			// Internal child MonoDevelop.VersionControl.Git.EditRemoteDialog.ActionArea
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
			this.DefaultWidth = 422;
			this.DefaultHeight = 206;
			this.Hide();
			this.entryUrl.Changed += new global::System.EventHandler(this.OnEntryUrlChanged);
			this.entryPushUrl.Changed += new global::System.EventHandler(this.OnEntryPushUrlChanged);
			this.entryName.Changed += new global::System.EventHandler(this.OnEntryNameChanged);
		}
	}
}
#pragma warning restore 436
