#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class CombineConfigurationPanelWidget
	{
		private global::Gtk.VBox vbox74;

		private global::Gtk.HBox hbox60;

		private global::Gtk.Label label104;

		private global::Gtk.ScrolledWindow scrolledwindow11;

		private global::Gtk.TreeView configsList;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.CombineConfigurationPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.CombineConfigurationPanelWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.CombineConfigurationPanelWidget.Gtk.Container+ContainerChild
			this.vbox74 = new global::Gtk.VBox();
			this.vbox74.Name = "vbox74";
			this.vbox74.Spacing = 6;
			// Container child vbox74.Gtk.Box+BoxChild
			this.hbox60 = new global::Gtk.HBox();
			this.hbox60.Name = "hbox60";
			this.hbox60.Spacing = 6;
			// Container child hbox60.Gtk.Box+BoxChild
			this.label104 = new global::Gtk.Label();
			this.label104.Name = "label104";
			this.label104.LabelProp = global::Mono.Unix.Catalog.GetString("Select a target configuration for each solution item:");
			this.hbox60.Add(this.label104);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox60[this.label104]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			this.vbox74.Add(this.hbox60);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox74[this.hbox60]));
			w2.Position = 0;
			w2.Expand = false;
			// Container child vbox74.Gtk.Box+BoxChild
			this.scrolledwindow11 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow11.Name = "scrolledwindow11";
			this.scrolledwindow11.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow11.Gtk.Container+ContainerChild
			this.configsList = new global::Gtk.TreeView();
			this.configsList.Name = "configsList";
			this.scrolledwindow11.Add(this.configsList);
			this.vbox74.Add(this.scrolledwindow11);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox74[this.scrolledwindow11]));
			w4.Position = 1;
			this.Add(this.vbox74);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
