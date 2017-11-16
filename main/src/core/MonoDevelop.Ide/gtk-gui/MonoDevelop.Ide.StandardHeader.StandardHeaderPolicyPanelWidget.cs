#pragma warning disable 436

namespace MonoDevelop.Ide.StandardHeader
{
	public partial class StandardHeaderPolicyPanelWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox2;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TextView headerText;

		private global::Gtk.ScrolledWindow GtkScrolledWindow1;

		private global::Gtk.TreeView treeviewTemplates;

		private global::Gtk.CheckButton includeAutoCheck;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.StandardHeader.StandardHeaderPolicyPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.StandardHeader.StandardHeaderPolicyPanelWidget";
			// Container child MonoDevelop.Ide.StandardHeader.StandardHeaderPolicyPanelWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.headerText = new global::Gtk.TextView();
			this.headerText.CanFocus = true;
			this.headerText.Name = "headerText";
			this.GtkScrolledWindow.Add(this.headerText);
			this.hbox2.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.GtkScrolledWindow]));
			w2.Position = 0;
			// Container child hbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow1 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow1.Name = "GtkScrolledWindow1";
			this.GtkScrolledWindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow1.Gtk.Container+ContainerChild
			this.treeviewTemplates = new global::Gtk.TreeView();
			this.treeviewTemplates.CanFocus = true;
			this.treeviewTemplates.Name = "treeviewTemplates";
			this.GtkScrolledWindow1.Add(this.treeviewTemplates);
			this.hbox2.Add(this.GtkScrolledWindow1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.GtkScrolledWindow1]));
			w4.Position = 1;
			this.vbox2.Add(this.hbox2);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox2]));
			w5.Position = 0;
			// Container child vbox2.Gtk.Box+BoxChild
			this.includeAutoCheck = new global::Gtk.CheckButton();
			this.includeAutoCheck.CanFocus = true;
			this.includeAutoCheck.Name = "includeAutoCheck";
			this.includeAutoCheck.Label = global::Mono.Unix.Catalog.GetString("_Include standard header in new files");
			this.includeAutoCheck.DrawIndicator = true;
			this.includeAutoCheck.UseUnderline = true;
			this.vbox2.Add(this.includeAutoCheck);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.includeAutoCheck]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
