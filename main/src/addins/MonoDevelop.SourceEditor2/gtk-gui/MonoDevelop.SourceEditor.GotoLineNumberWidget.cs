#pragma warning disable 436

namespace MonoDevelop.SourceEditor
{
	internal partial class GotoLineNumberWidget
	{
		private global::Gtk.HBox hbox1;

		private global::Gtk.Entry entryLineNumber;

		private global::Gtk.Button buttonGoToLine;

		private global::MonoDevelop.Components.ImageView image2;

		private global::Gtk.Button closeButton;

		private global::MonoDevelop.Components.ImageView image1;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.SourceEditor.GotoLineNumberWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.SourceEditor.GotoLineNumberWidget";
			// Container child MonoDevelop.SourceEditor.GotoLineNumberWidget.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			// Container child hbox1.Gtk.Box+BoxChild
			this.entryLineNumber = new global::Gtk.Entry();
			this.entryLineNumber.CanFocus = true;
			this.entryLineNumber.Name = "entryLineNumber";
			this.entryLineNumber.IsEditable = true;
			this.entryLineNumber.InvisibleChar = '●';
			this.hbox1.Add(this.entryLineNumber);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.entryLineNumber]));
			w1.Position = 0;
			w1.Expand = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonGoToLine = new global::Gtk.Button();
			this.buttonGoToLine.CanDefault = true;
			this.buttonGoToLine.CanFocus = true;
			this.buttonGoToLine.Name = "buttonGoToLine";
			this.buttonGoToLine.Relief = ((global::Gtk.ReliefStyle)(2));
			// Container child buttonGoToLine.Gtk.Container+ContainerChild
			this.image2 = new global::MonoDevelop.Components.ImageView();
			this.image2.Name = "image2";
			this.image2.IconId = "gtk-jump-to";
			this.image2.IconSize = ((global::Gtk.IconSize)(1));
			this.buttonGoToLine.Add(this.image2);
			this.hbox1.Add(this.buttonGoToLine);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.buttonGoToLine]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.closeButton = new global::Gtk.Button();
			this.closeButton.CanFocus = true;
			this.closeButton.Name = "closeButton";
			this.closeButton.Relief = ((global::Gtk.ReliefStyle)(2));
			// Container child closeButton.Gtk.Container+ContainerChild
			this.image1 = new global::MonoDevelop.Components.ImageView();
			this.image1.Name = "image1";
			this.image1.IconId = "gtk-close";
			this.image1.IconSize = ((global::Gtk.IconSize)(1));
			this.closeButton.Add(this.image1);
			this.hbox1.Add(this.closeButton);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.closeButton]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			this.Add(this.hbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
