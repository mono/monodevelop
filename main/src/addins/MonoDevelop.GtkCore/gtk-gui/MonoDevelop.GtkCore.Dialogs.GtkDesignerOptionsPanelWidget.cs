#pragma warning disable 436

namespace MonoDevelop.GtkCore.Dialogs
{
	public partial class GtkDesignerOptionsPanelWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.CheckButton checkSwitchLayout;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.GtkCore.Dialogs.GtkDesignerOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.GtkCore.Dialogs.GtkDesignerOptionsPanelWidget";
			// Container child MonoDevelop.GtkCore.Dialogs.GtkDesignerOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkSwitchLayout = new global::Gtk.CheckButton();
			this.checkSwitchLayout.CanFocus = true;
			this.checkSwitchLayout.Name = "checkSwitchLayout";
			this.checkSwitchLayout.Label = global::Mono.Unix.Catalog.GetString("Automatically switch to the \"GUI Builder\" layout when opening the designer");
			this.checkSwitchLayout.DrawIndicator = true;
			this.checkSwitchLayout.UseUnderline = true;
			this.vbox2.Add(this.checkSwitchLayout);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkSwitchLayout]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
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
