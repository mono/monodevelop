#pragma warning disable 436

namespace MonoDevelop.SourceEditor.OptionPanels
{
	internal partial class HighlightingPanel
	{
		private global::Gtk.VBox vbox5;

		private global::Gtk.TextView textview1;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView styleTreeview;

		private global::Gtk.VBox vbox1;

		private global::Gtk.Button addButton;

		private global::Gtk.Button removeButton;

		private global::Gtk.Button buttonOpenFolder;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.SourceEditor.OptionPanels.HighlightingPanel
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.SourceEditor.OptionPanels.HighlightingPanel";
			// Container child MonoDevelop.SourceEditor.OptionPanels.HighlightingPanel.Gtk.Container+ContainerChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.textview1 = new global::Gtk.TextView();
			this.textview1.Buffer.Text = global::Mono.Unix.Catalog.GetString("The following color theme formats are supported: Visual Studio (.vssettings), Xam" +
					"arin Studio (.json), TextMate (.tmTheme). Changes in the theme folder require a " +
					"restart.");
			this.textview1.Sensitive = false;
			this.textview1.Name = "textview1";
			this.textview1.Editable = false;
			this.textview1.CursorVisible = false;
			this.textview1.AcceptsTab = false;
			this.textview1.WrapMode = ((global::Gtk.WrapMode)(2));
			this.textview1.PixelsBelowLines = 6;
			this.textview1.PixelsInsideWrap = 3;
			this.vbox5.Add(this.textview1);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.textview1]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			global::Gtk.Viewport w2 = new global::Gtk.Viewport();
			w2.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child GtkViewport.Gtk.Container+ContainerChild
			this.styleTreeview = new global::Gtk.TreeView();
			this.styleTreeview.CanFocus = true;
			this.styleTreeview.Name = "styleTreeview";
			this.styleTreeview.HeadersVisible = false;
			w2.Add(this.styleTreeview);
			this.scrolledwindow1.Add(w2);
			this.hbox1.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.scrolledwindow1]));
			w5.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.addButton = new global::Gtk.Button();
			this.addButton.CanFocus = true;
			this.addButton.Name = "addButton";
			this.addButton.UseStock = true;
			this.addButton.UseUnderline = true;
			this.addButton.Label = "gtk-add";
			this.vbox1.Add(this.addButton);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.addButton]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.removeButton = new global::Gtk.Button();
			this.removeButton.CanFocus = true;
			this.removeButton.Name = "removeButton";
			this.removeButton.UseStock = true;
			this.removeButton.UseUnderline = true;
			this.removeButton.Label = "gtk-remove";
			this.vbox1.Add(this.removeButton);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.removeButton]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.buttonOpenFolder = new global::Gtk.Button();
			this.buttonOpenFolder.CanFocus = true;
			this.buttonOpenFolder.Name = "buttonOpenFolder";
			this.buttonOpenFolder.UseUnderline = true;
			this.buttonOpenFolder.Label = global::Mono.Unix.Catalog.GetString("Open Themes Folder");
			this.vbox1.Add(this.buttonOpenFolder);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.buttonOpenFolder]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			this.hbox1.Add(this.vbox1);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox1]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			this.vbox5.Add(this.hbox1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox1]));
			w10.Position = 1;
			this.Add(this.vbox5);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
