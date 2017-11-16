#pragma warning disable 436

namespace MonoDevelop.PackageManagement
{
	internal partial class PackageSourcesWidget
	{
		private global::Gtk.VBox mainVBox;

		private global::Gtk.HBox packageSourceListHBox;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView packageSourcesTreeView;

		private global::Gtk.HBox packageSourceHBox;

		private global::Gtk.HButtonBox bottomButtonBox;

		private global::Gtk.Button removeButton;

		private global::Gtk.Button addButton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.PackageManagement.PackageSourcesWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.PackageManagement.PackageSourcesWidget";
			// Container child MonoDevelop.PackageManagement.PackageSourcesWidget.Gtk.Container+ContainerChild
			this.mainVBox = new global::Gtk.VBox();
			this.mainVBox.Name = "mainVBox";
			this.mainVBox.Spacing = 6;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.packageSourceListHBox = new global::Gtk.HBox();
			this.packageSourceListHBox.Name = "packageSourceListHBox";
			this.packageSourceListHBox.Spacing = 6;
			// Container child packageSourceListHBox.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.packageSourcesTreeView = new global::Gtk.TreeView();
			this.packageSourcesTreeView.CanFocus = true;
			this.packageSourcesTreeView.Name = "packageSourcesTreeView";
			this.packageSourcesTreeView.HeadersVisible = false;
			this.GtkScrolledWindow.Add(this.packageSourcesTreeView);
			this.packageSourceListHBox.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.packageSourceListHBox[this.GtkScrolledWindow]));
			w2.Position = 0;
			this.mainVBox.Add(this.packageSourceListHBox);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.packageSourceListHBox]));
			w3.Position = 0;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.packageSourceHBox = new global::Gtk.HBox();
			this.packageSourceHBox.Name = "packageSourceHBox";
			this.packageSourceHBox.Spacing = 6;
			// Container child packageSourceHBox.Gtk.Box+BoxChild
			this.bottomButtonBox = new global::Gtk.HButtonBox();
			this.bottomButtonBox.Name = "bottomButtonBox";
			this.bottomButtonBox.Spacing = 5;
			this.bottomButtonBox.BorderWidth = ((uint)(5));
			this.bottomButtonBox.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child bottomButtonBox.Gtk.ButtonBox+ButtonBoxChild
			this.removeButton = new global::Gtk.Button();
			this.removeButton.CanFocus = true;
			this.removeButton.Name = "removeButton";
			this.removeButton.UseUnderline = true;
			this.removeButton.Label = global::Mono.Unix.Catalog.GetString("Remove");
			this.bottomButtonBox.Add(this.removeButton);
			global::Gtk.ButtonBox.ButtonBoxChild w4 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.bottomButtonBox[this.removeButton]));
			w4.Expand = false;
			w4.Fill = false;
			// Container child bottomButtonBox.Gtk.ButtonBox+ButtonBoxChild
			this.addButton = new global::Gtk.Button();
			this.addButton.CanFocus = true;
			this.addButton.Name = "addButton";
			this.addButton.UseUnderline = true;
			this.addButton.Label = global::Mono.Unix.Catalog.GetString("Add");
			this.bottomButtonBox.Add(this.addButton);
			global::Gtk.ButtonBox.ButtonBoxChild w5 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.bottomButtonBox[this.addButton]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.packageSourceHBox.Add(this.bottomButtonBox);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.packageSourceHBox[this.bottomButtonBox]));
			w6.Position = 0;
			this.mainVBox.Add(this.packageSourceHBox);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.packageSourceHBox]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			this.Add(this.mainVBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
