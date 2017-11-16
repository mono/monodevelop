#pragma warning disable 436

namespace MonoDevelop.CodeActions
{
	internal partial class ContextActionPanelWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.HBox hbox1;

		private global::MonoDevelop.Components.SearchEntry searchentryFilter;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeviewContextActions;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.CodeActions.ContextActionPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.CodeActions.ContextActionPanelWidget";
			// Container child MonoDevelop.CodeActions.ContextActionPanelWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.searchentryFilter = new global::MonoDevelop.Components.SearchEntry();
			this.searchentryFilter.Name = "searchentryFilter";
			this.searchentryFilter.ForceFilterButtonVisible = false;
			this.searchentryFilter.HasFrame = false;
			this.searchentryFilter.RoundedShape = false;
			this.searchentryFilter.IsCheckMenu = false;
			this.searchentryFilter.ActiveFilterID = 0;
			this.searchentryFilter.Ready = true;
			this.searchentryFilter.HasFocus = false;
			this.hbox1.Add(this.searchentryFilter);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.searchentryFilter]));
			w1.Position = 0;
			this.vbox1.Add(this.hbox1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeviewContextActions = new global::Gtk.TreeView();
			this.treeviewContextActions.CanFocus = true;
			this.treeviewContextActions.Name = "treeviewContextActions";
			this.GtkScrolledWindow.Add(this.treeviewContextActions);
			this.vbox1.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkScrolledWindow]));
			w4.Position = 1;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
