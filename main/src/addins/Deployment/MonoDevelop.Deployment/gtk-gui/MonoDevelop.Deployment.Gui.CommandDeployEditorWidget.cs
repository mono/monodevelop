#pragma warning disable 436

namespace MonoDevelop.Deployment.Gui
{
	internal partial class CommandDeployEditorWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Label label1;

		private global::Gtk.Entry cmdEntry;

		private global::Gtk.Label label2;

		private global::Gtk.Entry argsEntry;

		private global::Gtk.CheckButton checkExternal;

		private global::Gtk.CheckButton checkDisposeExternal;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Deployment.Gui.CommandDeployEditorWidget
			global::Stetic.BinContainer.Attach(this);
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.Deployment.Gui.CommandDeployEditorWidget";
			// Container child MonoDevelop.Deployment.Gui.CommandDeployEditorWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Command:");
			this.vbox1.Add(this.label1);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label1]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.cmdEntry = new global::Gtk.Entry();
			this.cmdEntry.CanFocus = true;
			this.cmdEntry.Name = "cmdEntry";
			this.cmdEntry.IsEditable = true;
			this.cmdEntry.InvisibleChar = '●';
			this.vbox1.Add(this.cmdEntry);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.cmdEntry]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Arguments:");
			this.vbox1.Add(this.label2);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label2]));
			w3.Position = 2;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.argsEntry = new global::Gtk.Entry();
			this.argsEntry.CanFocus = true;
			this.argsEntry.Name = "argsEntry";
			this.argsEntry.IsEditable = true;
			this.argsEntry.InvisibleChar = '●';
			this.vbox1.Add(this.argsEntry);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.argsEntry]));
			w4.Position = 3;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkExternal = new global::Gtk.CheckButton();
			this.checkExternal.CanFocus = true;
			this.checkExternal.Name = "checkExternal";
			this.checkExternal.Label = global::Mono.Unix.Catalog.GetString("Run in external console");
			this.checkExternal.DrawIndicator = true;
			this.vbox1.Add(this.checkExternal);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkExternal]));
			w5.Position = 4;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkDisposeExternal = new global::Gtk.CheckButton();
			this.checkDisposeExternal.CanFocus = true;
			this.checkDisposeExternal.Name = "checkDisposeExternal";
			this.checkDisposeExternal.Label = global::Mono.Unix.Catalog.GetString("Dispose console after running");
			this.checkDisposeExternal.DrawIndicator = true;
			this.vbox1.Add(this.checkDisposeExternal);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkDisposeExternal]));
			w6.Position = 5;
			w6.Expand = false;
			w6.Fill = false;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
			this.cmdEntry.Changed += new global::System.EventHandler(this.OnCmdEntryChanged);
			this.argsEntry.Changed += new global::System.EventHandler(this.OnArgsEntryChanged);
			this.checkExternal.Clicked += new global::System.EventHandler(this.OnCheckExternalClicked);
			this.checkDisposeExternal.Clicked += new global::System.EventHandler(this.OnCheckDisposeExternalClicked);
		}
	}
}
#pragma warning restore 436
