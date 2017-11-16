#pragma warning disable 436

namespace MonoDevelop.Deployment.Linux
{
	public partial class BasicOptionPanelWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.VBox boxExe;

		private global::Gtk.CheckButton checkScript;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label3;

		private global::Gtk.Label label2;

		private global::Gtk.Entry entryScript;

		private global::Gtk.CheckButton checkDesktop;

		private global::Gtk.VBox boxLibrary;

		private global::Gtk.CheckButton checkPcFile;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Deployment.Linux.BasicOptionPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.Deployment.Linux.BasicOptionPanelWidget";
			// Container child MonoDevelop.Deployment.Linux.BasicOptionPanelWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Events = ((global::Gdk.EventMask)(256));
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.boxExe = new global::Gtk.VBox();
			this.boxExe.Name = "boxExe";
			this.boxExe.Spacing = 6;
			// Container child boxExe.Gtk.Box+BoxChild
			this.checkScript = new global::Gtk.CheckButton();
			this.checkScript.CanFocus = true;
			this.checkScript.Name = "checkScript";
			this.checkScript.Label = global::Mono.Unix.Catalog.GetString("Generate launch script");
			this.checkScript.DrawIndicator = true;
			this.boxExe.Add(this.checkScript);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.boxExe[this.checkScript]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child boxExe.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.CanFocus = true;
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.WidthRequest = 24;
			this.label3.CanFocus = true;
			this.label3.Name = "label3";
			this.hbox1.Add(this.label3);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label3]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Script name:");
			this.hbox1.Add(this.label2);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label2]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.entryScript = new global::Gtk.Entry();
			this.entryScript.CanFocus = true;
			this.entryScript.Name = "entryScript";
			this.entryScript.IsEditable = true;
			this.entryScript.InvisibleChar = '●';
			this.hbox1.Add(this.entryScript);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.entryScript]));
			w4.Position = 2;
			this.boxExe.Add(this.hbox1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.boxExe[this.hbox1]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			// Container child boxExe.Gtk.Box+BoxChild
			this.checkDesktop = new global::Gtk.CheckButton();
			this.checkDesktop.CanFocus = true;
			this.checkDesktop.Name = "checkDesktop";
			this.checkDesktop.Label = global::Mono.Unix.Catalog.GetString("Generate .desktop file");
			this.checkDesktop.DrawIndicator = true;
			this.boxExe.Add(this.checkDesktop);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.boxExe[this.checkDesktop]));
			w6.Position = 2;
			w6.Expand = false;
			w6.Fill = false;
			this.vbox2.Add(this.boxExe);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.boxExe]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.boxLibrary = new global::Gtk.VBox();
			this.boxLibrary.Name = "boxLibrary";
			this.boxLibrary.Spacing = 6;
			// Container child boxLibrary.Gtk.Box+BoxChild
			this.checkPcFile = new global::Gtk.CheckButton();
			this.checkPcFile.CanFocus = true;
			this.checkPcFile.Name = "checkPcFile";
			this.checkPcFile.Label = global::Mono.Unix.Catalog.GetString("Generate .pc file for the library");
			this.checkPcFile.DrawIndicator = true;
			this.boxLibrary.Add(this.checkPcFile);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.boxLibrary[this.checkPcFile]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			this.vbox2.Add(this.boxLibrary);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.boxLibrary]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
			this.checkScript.Clicked += new global::System.EventHandler(this.OnCheckScriptClicked);
		}
	}
}
#pragma warning restore 436
