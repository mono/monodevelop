#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class CombineEntryConfigurationsPanelWidget
	{
		private global::Gtk.VBox vbox75;

		private global::Gtk.HBox hbox61;

		private global::Gtk.ScrolledWindow scrolledwindow12;

		private global::Gtk.TreeView configsList;

		private global::Gtk.VBox vbox76;

		private global::Gtk.Button addButton;

		private global::Gtk.Button copyButton;

		private global::Gtk.Button removeButton;

		private global::Gtk.Button renameButton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.CombineEntryConfigurationsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.CombineEntryConfigurationsPanelWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.CombineEntryConfigurationsPanelWidget.Gtk.Container+ContainerChild
			this.vbox75 = new global::Gtk.VBox();
			this.vbox75.Name = "vbox75";
			// Container child vbox75.Gtk.Box+BoxChild
			this.hbox61 = new global::Gtk.HBox();
			this.hbox61.Name = "hbox61";
			this.hbox61.Spacing = 6;
			// Container child hbox61.Gtk.Box+BoxChild
			this.scrolledwindow12 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow12.Name = "scrolledwindow12";
			this.scrolledwindow12.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow12.Gtk.Container+ContainerChild
			this.configsList = new global::Gtk.TreeView();
			this.configsList.Name = "configsList";
			this.scrolledwindow12.Add(this.configsList);
			this.hbox61.Add(this.scrolledwindow12);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox61[this.scrolledwindow12]));
			w2.Position = 0;
			// Container child hbox61.Gtk.Box+BoxChild
			this.vbox76 = new global::Gtk.VBox();
			this.vbox76.Name = "vbox76";
			this.vbox76.Spacing = 6;
			// Container child vbox76.Gtk.Box+BoxChild
			this.addButton = new global::Gtk.Button();
			this.addButton.Name = "addButton";
			this.addButton.UseStock = true;
			this.addButton.UseUnderline = true;
			this.addButton.Label = "gtk-add";
			this.vbox76.Add(this.addButton);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox76[this.addButton]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox76.Gtk.Box+BoxChild
			this.copyButton = new global::Gtk.Button();
			this.copyButton.Name = "copyButton";
			this.copyButton.UseStock = true;
			this.copyButton.UseUnderline = true;
			this.copyButton.Label = "gtk-copy";
			this.vbox76.Add(this.copyButton);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox76[this.copyButton]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox76.Gtk.Box+BoxChild
			this.removeButton = new global::Gtk.Button();
			this.removeButton.Name = "removeButton";
			this.removeButton.UseStock = true;
			this.removeButton.UseUnderline = true;
			this.removeButton.Label = "gtk-remove";
			this.vbox76.Add(this.removeButton);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox76[this.removeButton]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox76.Gtk.Box+BoxChild
			this.renameButton = new global::Gtk.Button();
			this.renameButton.Name = "renameButton";
			this.renameButton.UseUnderline = true;
			this.renameButton.Label = global::Mono.Unix.Catalog.GetString("Rename");
			this.vbox76.Add(this.renameButton);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox76[this.renameButton]));
			w6.Position = 3;
			w6.Expand = false;
			w6.Fill = false;
			this.hbox61.Add(this.vbox76);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox61[this.vbox76]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			this.vbox75.Add(this.hbox61);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox75[this.hbox61]));
			w8.Position = 0;
			this.Add(this.vbox75);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
