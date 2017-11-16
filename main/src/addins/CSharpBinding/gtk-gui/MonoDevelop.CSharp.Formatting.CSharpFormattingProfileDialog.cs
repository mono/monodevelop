#pragma warning disable 436

namespace MonoDevelop.CSharp.Formatting
{
	internal partial class CSharpFormattingProfileDialog
	{
		private global::Gtk.VBox vbox5;

		private global::Gtk.HPaned hpaned1;

		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox4;

		private global::Gtk.Label label12;

		private global::Gtk.ComboBox comboboxCategories;

		private global::Gtk.Notebook notebookCategories;

		private global::Gtk.VBox vbox8;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeviewIndentOptions;

		private global::Gtk.Label label8;

		private global::Gtk.ScrolledWindow GtkScrolledWindow1;

		private global::Gtk.TreeView treeviewNewLines;

		private global::Gtk.Label label9;

		private global::Gtk.ScrolledWindow GtkScrolledWindow5;

		private global::Gtk.TreeView treeviewSpacing;

		private global::Gtk.Label label14;

		private global::Gtk.ScrolledWindow GtkScrolledWindow2;

		private global::Gtk.TreeView treeviewWrapping;

		private global::Gtk.Label label10;

		private global::Gtk.ScrolledWindow GtkScrolledWindow3;

		private global::Gtk.TreeView treeviewStyle;

		private global::Gtk.Label label2;

		private global::Gtk.VBox vbox6;

		private global::Gtk.Label label13;

		private global::Gtk.ScrolledWindow scrolledwindow;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.CSharp.Formatting.CSharpFormattingProfileDialog
			this.Name = "MonoDevelop.CSharp.Formatting.CSharpFormattingProfileDialog";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.CSharp.Formatting.CSharpFormattingProfileDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hpaned1 = new global::Gtk.HPaned();
			this.hpaned1.CanFocus = true;
			this.hpaned1.Name = "hpaned1";
			this.hpaned1.Position = 380;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			// Container child hbox4.Gtk.Box+BoxChild
			this.label12 = new global::Gtk.Label();
			this.label12.Name = "label12";
			this.label12.LabelProp = global::Mono.Unix.Catalog.GetString("_Category:");
			this.label12.UseUnderline = true;
			this.hbox4.Add(this.label12);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.label12]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox4.Gtk.Box+BoxChild
			this.comboboxCategories = global::Gtk.ComboBox.NewText();
			this.comboboxCategories.Name = "comboboxCategories";
			this.hbox4.Add(this.comboboxCategories);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.comboboxCategories]));
			w3.Position = 1;
			this.vbox2.Add(this.hbox4);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox4]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.notebookCategories = new global::Gtk.Notebook();
			this.notebookCategories.CanFocus = true;
			this.notebookCategories.Name = "notebookCategories";
			this.notebookCategories.CurrentPage = 2;
			// Container child notebookCategories.Gtk.Notebook+NotebookChild
			this.vbox8 = new global::Gtk.VBox();
			this.vbox8.Name = "vbox8";
			this.vbox8.Spacing = 6;
			// Container child vbox8.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeviewIndentOptions = new global::Gtk.TreeView();
			this.treeviewIndentOptions.CanFocus = true;
			this.treeviewIndentOptions.Name = "treeviewIndentOptions";
			this.GtkScrolledWindow.Add(this.treeviewIndentOptions);
			this.vbox8.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox8[this.GtkScrolledWindow]));
			w6.Position = 0;
			this.notebookCategories.Add(this.vbox8);
			// Notebook tab
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("Indentation");
			this.notebookCategories.SetTabLabel(this.vbox8, this.label8);
			this.label8.ShowAll();
			// Container child notebookCategories.Gtk.Notebook+NotebookChild
			this.GtkScrolledWindow1 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow1.Name = "GtkScrolledWindow1";
			this.GtkScrolledWindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow1.Gtk.Container+ContainerChild
			this.treeviewNewLines = new global::Gtk.TreeView();
			this.treeviewNewLines.CanFocus = true;
			this.treeviewNewLines.Name = "treeviewNewLines";
			this.GtkScrolledWindow1.Add(this.treeviewNewLines);
			this.notebookCategories.Add(this.GtkScrolledWindow1);
			global::Gtk.Notebook.NotebookChild w9 = ((global::Gtk.Notebook.NotebookChild)(this.notebookCategories[this.GtkScrolledWindow1]));
			w9.Position = 1;
			// Notebook tab
			this.label9 = new global::Gtk.Label();
			this.label9.Name = "label9";
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("NewLines");
			this.notebookCategories.SetTabLabel(this.GtkScrolledWindow1, this.label9);
			this.label9.ShowAll();
			// Container child notebookCategories.Gtk.Notebook+NotebookChild
			this.GtkScrolledWindow5 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow5.Name = "GtkScrolledWindow5";
			this.GtkScrolledWindow5.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow5.Gtk.Container+ContainerChild
			this.treeviewSpacing = new global::Gtk.TreeView();
			this.treeviewSpacing.CanFocus = true;
			this.treeviewSpacing.Name = "treeviewSpacing";
			this.GtkScrolledWindow5.Add(this.treeviewSpacing);
			this.notebookCategories.Add(this.GtkScrolledWindow5);
			global::Gtk.Notebook.NotebookChild w11 = ((global::Gtk.Notebook.NotebookChild)(this.notebookCategories[this.GtkScrolledWindow5]));
			w11.Position = 2;
			// Notebook tab
			this.label14 = new global::Gtk.Label();
			this.label14.Name = "label14";
			this.label14.LabelProp = global::Mono.Unix.Catalog.GetString("Spacing");
			this.notebookCategories.SetTabLabel(this.GtkScrolledWindow5, this.label14);
			this.label14.ShowAll();
			// Container child notebookCategories.Gtk.Notebook+NotebookChild
			this.GtkScrolledWindow2 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow2.Name = "GtkScrolledWindow2";
			this.GtkScrolledWindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow2.Gtk.Container+ContainerChild
			this.treeviewWrapping = new global::Gtk.TreeView();
			this.treeviewWrapping.CanFocus = true;
			this.treeviewWrapping.Name = "treeviewWrapping";
			this.GtkScrolledWindow2.Add(this.treeviewWrapping);
			this.notebookCategories.Add(this.GtkScrolledWindow2);
			global::Gtk.Notebook.NotebookChild w13 = ((global::Gtk.Notebook.NotebookChild)(this.notebookCategories[this.GtkScrolledWindow2]));
			w13.Position = 3;
			// Notebook tab
			this.label10 = new global::Gtk.Label();
			this.label10.Name = "label10";
			this.label10.LabelProp = global::Mono.Unix.Catalog.GetString("Wrapping");
			this.notebookCategories.SetTabLabel(this.GtkScrolledWindow2, this.label10);
			this.label10.ShowAll();
			// Container child notebookCategories.Gtk.Notebook+NotebookChild
			this.GtkScrolledWindow3 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow3.Name = "GtkScrolledWindow3";
			this.GtkScrolledWindow3.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow3.Gtk.Container+ContainerChild
			this.treeviewStyle = new global::Gtk.TreeView();
			this.treeviewStyle.CanFocus = true;
			this.treeviewStyle.Name = "treeviewStyle";
			this.GtkScrolledWindow3.Add(this.treeviewStyle);
			this.notebookCategories.Add(this.GtkScrolledWindow3);
			global::Gtk.Notebook.NotebookChild w15 = ((global::Gtk.Notebook.NotebookChild)(this.notebookCategories[this.GtkScrolledWindow3]));
			w15.Position = 4;
			// Notebook tab
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Style");
			this.notebookCategories.SetTabLabel(this.GtkScrolledWindow3, this.label2);
			this.label2.ShowAll();
			this.vbox2.Add(this.notebookCategories);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.notebookCategories]));
			w16.Position = 1;
			this.hpaned1.Add(this.vbox2);
			global::Gtk.Paned.PanedChild w17 = ((global::Gtk.Paned.PanedChild)(this.hpaned1[this.vbox2]));
			w17.Resize = false;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.vbox6 = new global::Gtk.VBox();
			this.vbox6.Name = "vbox6";
			this.vbox6.Spacing = 6;
			// Container child vbox6.Gtk.Box+BoxChild
			this.label13 = new global::Gtk.Label();
			this.label13.Name = "label13";
			this.label13.Xalign = 0F;
			this.label13.LabelProp = global::Mono.Unix.Catalog.GetString("Preview:");
			this.vbox6.Add(this.label13);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.label13]));
			w18.Position = 0;
			w18.Expand = false;
			w18.Fill = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.scrolledwindow = new global::Gtk.ScrolledWindow();
			this.scrolledwindow.CanFocus = true;
			this.scrolledwindow.Name = "scrolledwindow";
			this.scrolledwindow.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox6.Add(this.scrolledwindow);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.scrolledwindow]));
			w19.Position = 1;
			this.hpaned1.Add(this.vbox6);
			this.vbox5.Add(this.hpaned1);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hpaned1]));
			w21.Position = 0;
			w1.Add(this.vbox5);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(w1[this.vbox5]));
			w22.Position = 0;
			// Internal child MonoDevelop.CSharp.Formatting.CSharpFormattingProfileDialog.ActionArea
			global::Gtk.HButtonBox w23 = this.ActionArea;
			w23.Name = "dialog1_ActionArea";
			w23.Spacing = 10;
			w23.BorderWidth = ((uint)(5));
			w23.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w24 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w23[this.buttonCancel]));
			w24.Expand = false;
			w24.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w25 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w23[this.buttonOk]));
			w25.Position = 1;
			w25.Expand = false;
			w25.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 880;
			this.DefaultHeight = 555;
			this.Hide();
		}
	}
}
#pragma warning restore 436
