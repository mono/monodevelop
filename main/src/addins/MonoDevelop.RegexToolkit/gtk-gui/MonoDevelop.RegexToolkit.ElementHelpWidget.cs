#pragma warning disable 436

namespace MonoDevelop.RegexToolkit
{
	internal partial class ElementHelpWidget
	{
		private global::Gtk.VBox vbox4;

		private global::Gtk.Label label11;

		private global::Gtk.ScrolledWindow elementsscrolledwindow;

		private global::Gtk.TreeView elementsTreeview;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.RegexToolkit.ElementHelpWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.RegexToolkit.ElementHelpWidget";
			// Container child MonoDevelop.RegexToolkit.ElementHelpWidget.Gtk.Container+ContainerChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label11 = new global::Gtk.Label();
			this.label11.Name = "label11";
			this.label11.Xalign = 0F;
			this.label11.LabelProp = global::Mono.Unix.Catalog.GetString("_Elements:");
			this.label11.UseMarkup = true;
			this.label11.UseUnderline = true;
			this.vbox4.Add(this.label11);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.label11]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.elementsscrolledwindow = new global::Gtk.ScrolledWindow();
			this.elementsscrolledwindow.CanFocus = true;
			this.elementsscrolledwindow.Name = "elementsscrolledwindow";
			this.elementsscrolledwindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child elementsscrolledwindow.Gtk.Container+ContainerChild
			this.elementsTreeview = new global::Gtk.TreeView();
			this.elementsTreeview.CanFocus = true;
			this.elementsTreeview.Name = "elementsTreeview";
			this.elementsscrolledwindow.Add(this.elementsTreeview);
			this.vbox4.Add(this.elementsscrolledwindow);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.elementsscrolledwindow]));
			w3.Position = 1;
			this.Add(this.vbox4);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label11.MnemonicWidget = this.elementsTreeview;
			this.Hide();
		}
	}
}
#pragma warning restore 436
