#pragma warning disable 436

namespace MonoDevelop.Components
{
	public partial class FolderListSelector
	{
		private global::Gtk.HBox hbox1;

		private global::Gtk.VBox vbox1;

		private global::MonoDevelop.Components.FolderEntry folderentry;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView dirList;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Button buttonAdd;

		private global::Gtk.Button buttonRemove;

		private global::Gtk.Button buttonUpdate;

		private global::Gtk.Button buttonUp;

		private global::Gtk.Button buttonDown;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Components.FolderListSelector
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Components.FolderListSelector";
			// Container child MonoDevelop.Components.FolderListSelector.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.folderentry = new global::MonoDevelop.Components.FolderEntry();
			this.folderentry.Name = "folderentry";
			this.vbox1.Add(this.folderentry);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.folderentry]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.dirList = new global::Gtk.TreeView();
			this.dirList.CanFocus = true;
			this.dirList.Name = "dirList";
			this.dirList.HeadersVisible = false;
			this.GtkScrolledWindow.Add(this.dirList);
			this.vbox1.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkScrolledWindow]));
			w3.Position = 1;
			this.hbox1.Add(this.vbox1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox1]));
			w4.Position = 0;
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
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonAdd]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.buttonRemove = new global::Gtk.Button();
			this.buttonRemove.CanFocus = true;
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseStock = true;
			this.buttonRemove.UseUnderline = true;
			this.buttonRemove.Label = "gtk-remove";
			this.vbox2.Add(this.buttonRemove);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonRemove]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.buttonUpdate = new global::Gtk.Button();
			this.buttonUpdate.CanFocus = true;
			this.buttonUpdate.Name = "buttonUpdate";
			this.buttonUpdate.UseUnderline = true;
			this.buttonUpdate.Label = global::Mono.Unix.Catalog.GetString("Update");
			this.vbox2.Add(this.buttonUpdate);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonUpdate]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.buttonUp = new global::Gtk.Button();
			this.buttonUp.CanFocus = true;
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.UseStock = true;
			this.buttonUp.UseUnderline = true;
			this.buttonUp.Label = "gtk-go-up";
			this.vbox2.Add(this.buttonUp);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonUp]));
			w8.Position = 3;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.buttonDown = new global::Gtk.Button();
			this.buttonDown.CanFocus = true;
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.UseStock = true;
			this.buttonDown.UseUnderline = true;
			this.buttonDown.Label = "gtk-go-down";
			this.vbox2.Add(this.buttonDown);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.buttonDown]));
			w9.Position = 4;
			w9.Expand = false;
			w9.Fill = false;
			this.hbox1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox2]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.Add(this.hbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.buttonAdd.Clicked += new global::System.EventHandler(this.OnButtonAddClicked);
			this.buttonRemove.Clicked += new global::System.EventHandler(this.OnButtonRemoveClicked);
			this.buttonUpdate.Clicked += new global::System.EventHandler(this.OnButtonUpdateClicked);
			this.buttonUp.Clicked += new global::System.EventHandler(this.OnButtonUpClicked);
			this.buttonDown.Clicked += new global::System.EventHandler(this.OnButtonDownClicked);
		}
	}
}
#pragma warning restore 436
