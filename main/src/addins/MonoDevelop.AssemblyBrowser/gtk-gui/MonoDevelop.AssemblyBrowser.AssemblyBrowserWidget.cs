#pragma warning disable 436

namespace MonoDevelop.AssemblyBrowser
{
	public partial class AssemblyBrowserWidget
	{
		private global::Gtk.UIManager UIManager;

		private global::Gtk.VBox vbox1;

		private global::Gtk.HPaned hpaned1;

		private global::Gtk.Alignment treeViewPlaceholder;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Notebook notebook1;

		private global::Gtk.HBox documentationScrolledWindow;

		private global::Gtk.VBox searchWidget;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView searchTreeview;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.AssemblyBrowser.AssemblyBrowserWidget
			Stetic.BinContainer w1 = global::Stetic.BinContainer.Attach(this);
			this.UIManager = new global::Gtk.UIManager();
			global::Gtk.ActionGroup w2 = new global::Gtk.ActionGroup("Default");
			this.UIManager.InsertActionGroup(w2, 0);
			this.Name = "MonoDevelop.AssemblyBrowser.AssemblyBrowserWidget";
			// Container child MonoDevelop.AssemblyBrowser.AssemblyBrowserWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 2;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hpaned1 = new global::Gtk.HPaned();
			this.hpaned1.CanFocus = true;
			this.hpaned1.Name = "hpaned1";
			this.hpaned1.Position = 271;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.treeViewPlaceholder = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.treeViewPlaceholder.Name = "treeViewPlaceholder";
			this.hpaned1.Add(this.treeViewPlaceholder);
			global::Gtk.Paned.PanedChild w3 = ((global::Gtk.Paned.PanedChild)(this.hpaned1[this.treeViewPlaceholder]));
			w3.Resize = false;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.notebook1 = new global::Gtk.Notebook();
			this.notebook1.CanFocus = true;
			this.notebook1.Name = "notebook1";
			this.notebook1.CurrentPage = 0;
			this.notebook1.ShowBorder = false;
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.documentationScrolledWindow = new global::Gtk.HBox();
			this.documentationScrolledWindow.Name = "documentationScrolledWindow";
			this.documentationScrolledWindow.Spacing = 6;
			this.notebook1.Add(this.documentationScrolledWindow);
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.searchWidget = new global::Gtk.VBox();
			this.searchWidget.Name = "searchWidget";
			this.searchWidget.Spacing = 6;
			// Container child searchWidget.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.searchTreeview = new global::Gtk.TreeView();
			this.searchTreeview.CanFocus = true;
			this.searchTreeview.Name = "searchTreeview";
			this.scrolledwindow1.Add(this.searchTreeview);
			this.searchWidget.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.searchWidget[this.scrolledwindow1]));
			w6.Position = 0;
			this.notebook1.Add(this.searchWidget);
			global::Gtk.Notebook.NotebookChild w7 = ((global::Gtk.Notebook.NotebookChild)(this.notebook1[this.searchWidget]));
			w7.Position = 1;
			this.vbox3.Add(this.notebook1);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.notebook1]));
			w8.Position = 0;
			this.hpaned1.Add(this.vbox3);
			this.vbox1.Add(this.hpaned1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hpaned1]));
			w10.Position = 0;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			w1.SetUiManager(UIManager);
			this.Hide();
		}
	}
}
#pragma warning restore 436
