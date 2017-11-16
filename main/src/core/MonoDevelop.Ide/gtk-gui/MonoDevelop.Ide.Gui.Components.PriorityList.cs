#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class PriorityList
	{
		private global::Gtk.HBox hbox;

		private global::Gtk.ScrolledWindow scrolledWindow;

		private global::Gtk.TreeView treeview;

		private global::Gtk.VBox controls;

		private global::Gtk.Button buttonUp;

		private global::Gtk.Button buttonDown;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Components.PriorityList
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.Components.PriorityList";
			// Container child MonoDevelop.Ide.Gui.Components.PriorityList.Gtk.Container+ContainerChild
			this.hbox = new global::Gtk.HBox();
			this.hbox.Name = "hbox";
			this.hbox.Spacing = 6;
			// Container child hbox.Gtk.Box+BoxChild
			this.scrolledWindow = new global::Gtk.ScrolledWindow();
			this.scrolledWindow.Name = "scrolledWindow";
			this.scrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledWindow.Gtk.Container+ContainerChild
			this.treeview = new global::Gtk.TreeView();
			this.treeview.CanFocus = true;
			this.treeview.Name = "treeview";
			this.treeview.HeadersVisible = false;
			this.scrolledWindow.Add(this.treeview);
			this.hbox.Add(this.scrolledWindow);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox[this.scrolledWindow]));
			w2.Position = 0;
			// Container child hbox.Gtk.Box+BoxChild
			this.controls = new global::Gtk.VBox();
			this.controls.Name = "controls";
			this.controls.Spacing = 6;
			// Container child controls.Gtk.Box+BoxChild
			this.buttonUp = new global::Gtk.Button();
			this.buttonUp.CanFocus = true;
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.UseStock = true;
			this.buttonUp.UseUnderline = true;
			this.buttonUp.Label = "gtk-go-up";
			this.controls.Add(this.buttonUp);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.controls[this.buttonUp]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child controls.Gtk.Box+BoxChild
			this.buttonDown = new global::Gtk.Button();
			this.buttonDown.CanFocus = true;
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.UseStock = true;
			this.buttonDown.UseUnderline = true;
			this.buttonDown.Label = "gtk-go-down";
			this.controls.Add(this.buttonDown);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.controls[this.buttonDown]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			this.hbox.Add(this.controls);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox[this.controls]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.Add(this.hbox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.buttonUp.Clicked += new global::System.EventHandler(this.OnButtonUpClicked);
			this.buttonDown.Clicked += new global::System.EventHandler(this.OnButtonDownClicked);
		}
	}
}
#pragma warning restore 436
