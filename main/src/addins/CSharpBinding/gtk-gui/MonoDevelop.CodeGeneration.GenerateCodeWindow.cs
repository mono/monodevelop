#pragma warning disable 436

namespace MonoDevelop.CodeGeneration
{
	public partial class GenerateCodeWindow
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Label label2;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.Label labelDescription;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.CodeGeneration.GenerateCodeWindow
			this.Name = "MonoDevelop.CodeGeneration.GenerateCodeWindow";
			this.Title = global::Mono.Unix.Catalog.GetString("GenerateCodeWindow");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Decorated = false;
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			// Container child MonoDevelop.CodeGeneration.GenerateCodeWindow.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("_Generate");
			this.label2.UseUnderline = true;
			this.vbox1.Add(this.label2);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label2]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox1.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.scrolledwindow1]));
			w2.Position = 1;
			// Container child vbox1.Gtk.Box+BoxChild
			this.labelDescription = new global::Gtk.Label();
			this.labelDescription.Name = "labelDescription";
			this.labelDescription.Xalign = 0F;
			this.vbox1.Add(this.labelDescription);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.labelDescription]));
			w3.Position = 2;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox1.Add(this.scrolledwindow2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.scrolledwindow2]));
			w4.Position = 3;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 288;
			this.DefaultHeight = 369;
			this.Hide();
		}
	}
}
#pragma warning restore 436
