#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class GitCommitDialogExtensionWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.CheckButton checkPush;

		private global::Gtk.CheckButton checkAuthor;

		private global::Gtk.Alignment authorBox;

		private global::Gtk.Table table1;

		private global::Gtk.Entry entryEmail;

		private global::Gtk.Entry entryName;

		private global::Gtk.Label labelMail;

		private global::Gtk.Label labelName;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.GitCommitDialogExtensionWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.VersionControl.Git.GitCommitDialogExtensionWidget";
			// Container child MonoDevelop.VersionControl.Git.GitCommitDialogExtensionWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkPush = new global::Gtk.CheckButton();
			this.checkPush.CanFocus = true;
			this.checkPush.Name = "checkPush";
			this.checkPush.Label = global::Mono.Unix.Catalog.GetString("Push changes to remote repository after commit");
			this.checkPush.DrawIndicator = true;
			this.checkPush.UseUnderline = true;
			this.vbox1.Add(this.checkPush);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkPush]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkAuthor = new global::Gtk.CheckButton();
			this.checkAuthor.CanFocus = true;
			this.checkAuthor.Name = "checkAuthor";
			this.checkAuthor.Label = global::Mono.Unix.Catalog.GetString("Override the commit author");
			this.checkAuthor.DrawIndicator = true;
			this.checkAuthor.UseUnderline = true;
			this.vbox1.Add(this.checkAuthor);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkAuthor]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.authorBox = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.authorBox.Name = "authorBox";
			this.authorBox.LeftPadding = ((uint)(24));
			// Container child authorBox.Gtk.Container+ContainerChild
			this.table1 = new global::Gtk.Table(((uint)(2)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.entryEmail = new global::Gtk.Entry();
			this.entryEmail.CanFocus = true;
			this.entryEmail.Name = "entryEmail";
			this.entryEmail.IsEditable = true;
			this.entryEmail.InvisibleChar = '●';
			this.table1.Add(this.entryEmail);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1[this.entryEmail]));
			w3.TopAttach = ((uint)(1));
			w3.BottomAttach = ((uint)(2));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryName = new global::Gtk.Entry();
			this.entryName.CanFocus = true;
			this.entryName.Name = "entryName";
			this.entryName.IsEditable = true;
			this.entryName.InvisibleChar = '●';
			this.table1.Add(this.entryName);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.entryName]));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(2));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.labelMail = new global::Gtk.Label();
			this.labelMail.Name = "labelMail";
			this.labelMail.Xalign = 0F;
			this.labelMail.LabelProp = global::Mono.Unix.Catalog.GetString("Author e-mail:");
			this.table1.Add(this.labelMail);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.labelMail]));
			w5.TopAttach = ((uint)(1));
			w5.BottomAttach = ((uint)(2));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.labelName = new global::Gtk.Label();
			this.labelName.Name = "labelName";
			this.labelName.Xalign = 0F;
			this.labelName.LabelProp = global::Mono.Unix.Catalog.GetString("Author name:");
			this.table1.Add(this.labelName);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.labelName]));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			this.authorBox.Add(this.table1);
			this.vbox1.Add(this.authorBox);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.authorBox]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.authorBox.Hide();
			this.Hide();
			this.checkAuthor.Toggled += new global::System.EventHandler(this.OnCheckAuthorToggled);
			this.entryName.Changed += new global::System.EventHandler(this.OnChanged);
			this.entryEmail.Changed += new global::System.EventHandler(this.OnChanged);
		}
	}
}
#pragma warning restore 436
