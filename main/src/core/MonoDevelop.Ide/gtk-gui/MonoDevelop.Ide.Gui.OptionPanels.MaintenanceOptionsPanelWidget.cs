#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal partial class MaintenanceOptionsPanelWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.CheckButton checkInstr;

		private global::Gtk.CheckButton checkAutoTest;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.OptionPanels.MaintenanceOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.OptionPanels.MaintenanceOptionsPanelWidget";
			// Container child MonoDevelop.Ide.Gui.OptionPanels.MaintenanceOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkInstr = new global::Gtk.CheckButton();
			this.checkInstr.CanFocus = true;
			this.checkInstr.Name = "checkInstr";
			this.checkInstr.Label = global::Mono.Unix.Catalog.GetString("Enable MonoDevelop Instrumentation");
			this.checkInstr.DrawIndicator = true;
			this.checkInstr.UseUnderline = true;
			this.vbox2.Add(this.checkInstr);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkInstr]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkAutoTest = new global::Gtk.CheckButton();
			this.checkAutoTest.CanFocus = true;
			this.checkAutoTest.Name = "checkAutoTest";
			this.checkAutoTest.Label = global::Mono.Unix.Catalog.GetString("Enable automated test support");
			this.checkAutoTest.DrawIndicator = true;
			this.checkAutoTest.UseUnderline = true;
			this.vbox2.Add(this.checkAutoTest);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkAutoTest]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
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
