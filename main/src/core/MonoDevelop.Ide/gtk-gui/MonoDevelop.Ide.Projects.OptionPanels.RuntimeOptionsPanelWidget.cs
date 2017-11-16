#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class RuntimeOptionsPanelWidget
	{
		private global::Gtk.VBox vbox81;

		private global::Gtk.HBox hbox68;

		private global::Gtk.Label label114;

		private global::Gtk.ComboBox runtimeVersionCombo;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.RuntimeOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.RuntimeOptionsPanelWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.RuntimeOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox81 = new global::Gtk.VBox();
			this.vbox81.Name = "vbox81";
			this.vbox81.Spacing = 12;
			// Container child vbox81.Gtk.Box+BoxChild
			this.hbox68 = new global::Gtk.HBox();
			this.hbox68.Name = "hbox68";
			this.hbox68.Spacing = 7;
			// Container child hbox68.Gtk.Box+BoxChild
			this.label114 = new global::Gtk.Label();
			this.label114.Name = "label114";
			this.label114.Xalign = 0F;
			this.label114.LabelProp = global::Mono.Unix.Catalog.GetString("Target _framework:");
			this.label114.UseUnderline = true;
			this.hbox68.Add(this.label114);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox68[this.label114]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child hbox68.Gtk.Box+BoxChild
			this.runtimeVersionCombo = global::Gtk.ComboBox.NewText();
			this.runtimeVersionCombo.Name = "runtimeVersionCombo";
			this.hbox68.Add(this.runtimeVersionCombo);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox68[this.runtimeVersionCombo]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			this.vbox81.Add(this.hbox68);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox81[this.hbox68]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			this.Add(this.vbox81);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
