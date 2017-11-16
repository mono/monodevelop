#pragma warning disable 436

namespace MonoDevelop.VersionControl.Dialogs
{
	internal partial class CommitDialog
	{
		private global::Gtk.VBox mainBox;

		private global::Gtk.Label label1;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView fileList;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label2;

		private global::Gtk.Label label3;

		private global::Gtk.VBox vboxExtensions;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.TextView textview;

		private global::Gtk.Button button29;

		private global::Gtk.Button buttonCommit;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Dialogs.CommitDialog
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.VersionControl.Dialogs.CommitDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Commit Files");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.VersionControl.Dialogs.CommitDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Events = ((global::Gdk.EventMask)(256));
			w1.Name = "dialog_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog_VBox.Gtk.Box+BoxChild
			this.mainBox = new global::Gtk.VBox();
			this.mainBox.Name = "mainBox";
			this.mainBox.Spacing = 6;
			this.mainBox.BorderWidth = ((uint)(6));
			// Container child mainBox.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("The following files will be committed:");
			this.mainBox.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.mainBox[this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child mainBox.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.fileList = new global::Gtk.TreeView();
			this.fileList.CanFocus = true;
			this.fileList.Name = "fileList";
			this.fileList.SearchColumn = 2;
			this.scrolledwindow1.Add(this.fileList);
			this.mainBox.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.mainBox[this.scrolledwindow1]));
			w4.Position = 1;
			// Container child mainBox.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Commit _message:");
			this.label2.UseUnderline = true;
			this.hbox1.Add(this.label2);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label2]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("1/1");
			this.hbox1.Add(this.label3);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label3]));
			w6.PackType = ((global::Gtk.PackType)(1));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.mainBox.Add(this.hbox1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.mainBox[this.hbox1]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			// Container child mainBox.Gtk.Box+BoxChild
			this.vboxExtensions = new global::Gtk.VBox();
			this.vboxExtensions.Name = "vboxExtensions";
			this.vboxExtensions.Spacing = 6;
			this.mainBox.Add(this.vboxExtensions);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.mainBox[this.vboxExtensions]));
			w8.PackType = ((global::Gtk.PackType)(1));
			w8.Position = 3;
			w8.Expand = false;
			w8.Fill = false;
			// Container child mainBox.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.textview = new global::Gtk.TextView();
			this.textview.CanFocus = true;
			this.textview.Name = "textview";
			this.scrolledwindow2.Add(this.textview);
			this.mainBox.Add(this.scrolledwindow2);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.mainBox[this.scrolledwindow2]));
			w10.PackType = ((global::Gtk.PackType)(1));
			w10.Position = 4;
			w1.Add(this.mainBox);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(w1[this.mainBox]));
			w11.Position = 0;
			// Internal child MonoDevelop.VersionControl.Dialogs.CommitDialog.ActionArea
			global::Gtk.HButtonBox w12 = this.ActionArea;
			w12.Events = ((global::Gdk.EventMask)(256));
			w12.Name = "VersionControlAddIn.CommitDialog_ActionArea";
			w12.Spacing = 6;
			w12.BorderWidth = ((uint)(5));
			w12.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child VersionControlAddIn.CommitDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button29 = new global::Gtk.Button();
			this.button29.CanDefault = true;
			this.button29.CanFocus = true;
			this.button29.Name = "button29";
			this.button29.UseStock = true;
			this.button29.UseUnderline = true;
			this.button29.Label = "gtk-cancel";
			this.AddActionWidget(this.button29, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w13 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w12[this.button29]));
			w13.Expand = false;
			w13.Fill = false;
			// Container child VersionControlAddIn.CommitDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCommit = new global::Gtk.Button();
			this.buttonCommit.CanDefault = true;
			this.buttonCommit.CanFocus = true;
			this.buttonCommit.Name = "buttonCommit";
			this.buttonCommit.UseUnderline = true;
			this.buttonCommit.Label = global::Mono.Unix.Catalog.GetString("C_ommit");
			this.AddActionWidget(this.buttonCommit, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w14 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w12[this.buttonCommit]));
			w14.Position = 1;
			w14.Expand = false;
			w14.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 628;
			this.DefaultHeight = 481;
			this.label2.MnemonicWidget = this.textview;
			this.label3.MnemonicWidget = this.textview;
			this.Hide();
		}
	}
}
#pragma warning restore 436
