#pragma warning disable 436

namespace MonoDevelop.Deployment.Linux
{
	public partial class MenuCategorySelectorDialog
	{
		private global::Gtk.VBox vbox5;

		private global::Gtk.Label label19;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView tree;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Deployment.Linux.MenuCategorySelectorDialog
			this.CanFocus = true;
			this.Name = "MonoDevelop.Deployment.Linux.MenuCategorySelectorDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Menu Category Selection");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.Deployment.Linux.MenuCategorySelectorDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			this.vbox5.BorderWidth = ((uint)(6));
			// Container child vbox5.Gtk.Box+BoxChild
			this.label19 = new global::Gtk.Label();
			this.label19.Name = "label19";
			this.label19.Xalign = 0F;
			this.label19.LabelProp = global::Mono.Unix.Catalog.GetString("Select the categories you want to assign to the desktop entry:");
			this.vbox5.Add(this.label19);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.label19]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.tree = new global::Gtk.TreeView();
			this.tree.CanFocus = true;
			this.tree.Name = "tree";
			this.scrolledwindow1.Add(this.tree);
			this.vbox5.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.scrolledwindow1]));
			w4.Position = 1;
			w1.Add(this.vbox5);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(w1[this.vbox5]));
			w5.Position = 0;
			// Internal child MonoDevelop.Deployment.Linux.MenuCategorySelectorDialog.ActionArea
			global::Gtk.HButtonBox w6 = this.ActionArea;
			w6.Name = "dialog1_ActionArea";
			w6.Spacing = 10;
			w6.BorderWidth = ((uint)(5));
			w6.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w7 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w6[this.buttonCancel]));
			w7.Expand = false;
			w7.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w8 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w6[this.buttonOk]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 652;
			this.DefaultHeight = 484;
			this.Hide();
		}
	}
}
#pragma warning restore 436
