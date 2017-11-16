#pragma warning disable 436

namespace MonoDevelop.Ide.CodeTemplates
{
	internal partial class CodeTemplatePanelWidget
	{
		private global::Gtk.VPaned vpaned1;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeviewCodeTemplates;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Button buttonAdd;

		private global::Gtk.Button buttonEdit;

		private global::Gtk.Button buttonRemove;

		private global::Gtk.VBox vbox1;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label label3;

		private global::Gtk.Fixed fixed1;

		private global::Gtk.CheckButton checkbuttonWhiteSpaces;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.CodeTemplates.CodeTemplatePanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.CodeTemplates.CodeTemplatePanelWidget";
			// Container child MonoDevelop.Ide.CodeTemplates.CodeTemplatePanelWidget.Gtk.Container+ContainerChild
			this.vpaned1 = new global::Gtk.VPaned();
			this.vpaned1.CanFocus = true;
			this.vpaned1.Name = "vpaned1";
			this.vpaned1.Position = 127;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeviewCodeTemplates = new global::Gtk.TreeView();
			this.treeviewCodeTemplates.CanFocus = true;
			this.treeviewCodeTemplates.Name = "treeviewCodeTemplates";
			this.treeviewCodeTemplates.HeadersVisible = false;
			this.GtkScrolledWindow.Add(this.treeviewCodeTemplates);
			this.hbox1.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.GtkScrolledWindow]));
			w2.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.buttonAdd = new global::Gtk.Button();
			this.buttonAdd.CanFocus = true;
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.UseStock = true;
			this.buttonAdd.UseUnderline = true;
			this.buttonAdd.Label = "gtk-add";
			this.vbox2.Add(this.buttonAdd);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonAdd]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.buttonEdit = new global::Gtk.Button();
			this.buttonEdit.CanFocus = true;
			this.buttonEdit.Name = "buttonEdit";
			this.buttonEdit.UseStock = true;
			this.buttonEdit.UseUnderline = true;
			this.buttonEdit.Label = "gtk-edit";
			this.vbox2.Add(this.buttonEdit);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonEdit]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.buttonRemove = new global::Gtk.Button();
			this.buttonRemove.CanFocus = true;
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseStock = true;
			this.buttonRemove.UseUnderline = true;
			this.buttonRemove.Label = "gtk-remove";
			this.vbox2.Add(this.buttonRemove);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonRemove]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			this.hbox1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox2]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.vpaned1.Add(this.hbox1);
			global::Gtk.Paned.PanedChild w7 = ((global::Gtk.Paned.PanedChild)(this.vpaned1[this.hbox1]));
			w7.Resize = false;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Preview:");
			this.hbox2.Add(this.label3);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label3]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.fixed1 = new global::Gtk.Fixed();
			this.fixed1.Name = "fixed1";
			this.fixed1.HasWindow = false;
			this.hbox2.Add(this.fixed1);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.fixed1]));
			w9.Position = 1;
			// Container child hbox2.Gtk.Box+BoxChild
			this.checkbuttonWhiteSpaces = new global::Gtk.CheckButton();
			this.checkbuttonWhiteSpaces.CanFocus = true;
			this.checkbuttonWhiteSpaces.Name = "checkbuttonWhiteSpaces";
			this.checkbuttonWhiteSpaces.Label = global::Mono.Unix.Catalog.GetString("S_how whitespaces");
			this.checkbuttonWhiteSpaces.DrawIndicator = true;
			this.checkbuttonWhiteSpaces.UseUnderline = true;
			this.hbox2.Add(this.checkbuttonWhiteSpaces);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.checkbuttonWhiteSpaces]));
			w10.Position = 2;
			w10.Expand = false;
			this.vbox1.Add(this.hbox2);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox2]));
			w11.Position = 0;
			w11.Expand = false;
			w11.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox1.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.scrolledwindow1]));
			w12.Position = 1;
			this.vpaned1.Add(this.vbox1);
			this.Add(this.vpaned1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
