#pragma warning disable 436

namespace MonoDevelop.Refactoring
{
	internal partial class OverridesImplementsDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeview;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Button buttonSelectAll;

		private global::Gtk.Button buttonUnselectAll;

		private global::Gtk.HSeparator hseparator1;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Refactoring.OverridesImplementsDialog
			this.Name = "MonoDevelop.Refactoring.OverridesImplementsDialog";
			this.Title = "";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.Refactoring.OverridesImplementsDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeview = new global::Gtk.TreeView();
			this.treeview.CanFocus = true;
			this.treeview.Name = "treeview";
			this.GtkScrolledWindow.Add(this.treeview);
			this.vbox2.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.GtkScrolledWindow]));
			w3.Position = 0;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonSelectAll = new global::Gtk.Button();
			this.buttonSelectAll.CanFocus = true;
			this.buttonSelectAll.Name = "buttonSelectAll";
			this.buttonSelectAll.UseUnderline = true;
			this.buttonSelectAll.Label = global::Mono.Unix.Catalog.GetString("Select All");
			this.hbox1.Add(this.buttonSelectAll);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.buttonSelectAll]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonUnselectAll = new global::Gtk.Button();
			this.buttonUnselectAll.CanFocus = true;
			this.buttonUnselectAll.Name = "buttonUnselectAll";
			this.buttonUnselectAll.UseUnderline = true;
			this.buttonUnselectAll.Label = global::Mono.Unix.Catalog.GetString("Unselect All");
			this.hbox1.Add(this.buttonUnselectAll);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.buttonUnselectAll]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hseparator1 = new global::Gtk.HSeparator();
			this.hseparator1.Name = "hseparator1";
			this.vbox2.Add(this.hseparator1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hseparator1]));
			w7.Position = 3;
			w7.Expand = false;
			w7.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w8.Position = 0;
			// Internal child MonoDevelop.Refactoring.OverridesImplementsDialog.ActionArea
			global::Gtk.HButtonBox w9 = this.ActionArea;
			w9.Name = "dialog1_ActionArea";
			w9.Spacing = 6;
			w9.BorderWidth = ((uint)(5));
			w9.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w10 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w9[this.buttonCancel]));
			w10.Expand = false;
			w10.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w9[this.buttonOk]));
			w11.Position = 1;
			w11.Expand = false;
			w11.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 595;
			this.DefaultHeight = 469;
			this.Hide();
		}
	}
}
#pragma warning restore 436
