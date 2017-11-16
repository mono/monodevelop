#pragma warning disable 436

namespace MonoDevelop.Ide.Execution
{
	internal partial class MonoExecutionParametersWidget
	{
		private global::Gtk.HBox hbox1;

		private global::MonoDevelop.Components.PropertyGrid.PropertyGrid propertyGrid;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Button buttonReset;

		private global::Gtk.Button buttonPreview;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Execution.MonoExecutionParametersWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Execution.MonoExecutionParametersWidget";
			// Container child MonoDevelop.Ide.Execution.MonoExecutionParametersWidget.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			this.hbox1.BorderWidth = ((uint)(6));
			// Container child hbox1.Gtk.Box+BoxChild
			this.propertyGrid = new global::MonoDevelop.Components.PropertyGrid.PropertyGrid();
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.ShowToolbar = false;
			this.propertyGrid.ShowHelp = true;
			this.hbox1.Add(this.propertyGrid);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.propertyGrid]));
			w1.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.buttonReset = new global::Gtk.Button();
			this.buttonReset.CanFocus = true;
			this.buttonReset.Name = "buttonReset";
			this.buttonReset.UseUnderline = true;
			this.buttonReset.Label = global::Mono.Unix.Catalog.GetString("Clear All Options");
			this.vbox4.Add(this.buttonReset);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.buttonReset]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.buttonPreview = new global::Gtk.Button();
			this.buttonPreview.CanFocus = true;
			this.buttonPreview.Name = "buttonPreview";
			this.buttonPreview.UseUnderline = true;
			this.buttonPreview.Label = global::Mono.Unix.Catalog.GetString("Preview Options");
			this.vbox4.Add(this.buttonPreview);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.buttonPreview]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			this.hbox1.Add(this.vbox4);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox4]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			this.Add(this.hbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.buttonReset.Clicked += new global::System.EventHandler(this.OnButtonResetClicked);
			this.buttonPreview.Clicked += new global::System.EventHandler(this.OnButtonPreviewClicked);
		}
	}
}
#pragma warning restore 436
