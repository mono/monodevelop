#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class CustomCommandPanelWidget
	{
		private global::Gtk.VBox vbox;

		private global::Gtk.Label label3;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.VBox vboxCommands;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.CustomCommandPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.CustomCommandPanelWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.CustomCommandPanelWidget.Gtk.Container+ContainerChild
			this.vbox = new global::Gtk.VBox();
			this.vbox.Name = "vbox";
			this.vbox.Spacing = 6;
			// Container child vbox.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.WidthRequest = 470;
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("MonoDevelop can execute user specified commands or scripts before, after or as a " +
					"replacement of common project operations. It is also possible to enter custom co" +
					"mmands which will be available in the project or solution menu.");
			this.label3.Wrap = true;
			this.vbox.Add(this.label3);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox[this.label3]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			global::Gtk.Viewport w2 = new global::Gtk.Viewport();
			w2.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child GtkViewport.Gtk.Container+ContainerChild
			this.vboxCommands = new global::Gtk.VBox();
			this.vboxCommands.CanFocus = true;
			this.vboxCommands.Name = "vboxCommands";
			w2.Add(this.vboxCommands);
			this.scrolledwindow1.Add(w2);
			this.vbox.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox[this.scrolledwindow1]));
			w5.Position = 1;
			this.Add(this.vbox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
