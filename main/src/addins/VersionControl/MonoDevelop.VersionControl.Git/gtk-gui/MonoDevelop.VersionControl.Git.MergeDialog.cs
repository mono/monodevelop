#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class MergeDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label labelHeader;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView tree;

		private global::Gtk.Label labelOper;

		private global::Gtk.CheckButton checkStage;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.MergeDialog
			this.Name = "MonoDevelop.VersionControl.Git.MergeDialog";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.VersionControl.Git.MergeDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(9));
			// Container child vbox2.Gtk.Box+BoxChild
			this.labelHeader = new global::Gtk.Label();
			this.labelHeader.Name = "labelHeader";
			this.labelHeader.Xalign = 0F;
			this.labelHeader.LabelProp = global::Mono.Unix.Catalog.GetString("Select the branch to be merged with the current branch:");
			this.vbox2.Add(this.labelHeader);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.labelHeader]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.tree = new global::Gtk.TreeView();
			this.tree.CanFocus = true;
			this.tree.Name = "tree";
			this.tree.HeadersVisible = false;
			this.GtkScrolledWindow.Add(this.tree);
			this.vbox2.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.GtkScrolledWindow]));
			w4.Position = 1;
			// Container child vbox2.Gtk.Box+BoxChild
			this.labelOper = new global::Gtk.Label();
			this.labelOper.WidthRequest = 443;
			this.labelOper.Name = "labelOper";
			this.labelOper.Xalign = 0F;
			this.labelOper.LabelProp = "The remote branch <b>origin/blablabla</b> will be merged into the branch <b>maste" +
				"r</b>.";
			this.labelOper.UseMarkup = true;
			this.labelOper.Wrap = true;
			this.vbox2.Add(this.labelOper);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.labelOper]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkStage = new global::Gtk.CheckButton();
			this.checkStage.CanFocus = true;
			this.checkStage.Name = "checkStage";
			this.checkStage.Label = global::Mono.Unix.Catalog.GetString("Stash/unstash local changes before/after the merge");
			this.checkStage.DrawIndicator = true;
			this.checkStage.UseUnderline = true;
			this.vbox2.Add(this.checkStage);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkStage]));
			w6.Position = 3;
			w6.Expand = false;
			w6.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w7.Position = 0;
			// Internal child MonoDevelop.VersionControl.Git.MergeDialog.ActionArea
			global::Gtk.HButtonBox w8 = this.ActionArea;
			w8.Name = "dialog1_ActionArea";
			w8.Spacing = 10;
			w8.BorderWidth = ((uint)(5));
			w8.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w9 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w8[this.buttonCancel]));
			w9.Expand = false;
			w9.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = global::Mono.Unix.Catalog.GetString("Merge");
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w10 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w8[this.buttonOk]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 469;
			this.DefaultHeight = 487;
			this.Hide();
		}
	}
}
#pragma warning restore 436
