#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal partial class LoadSavePanelWidget
	{
		private global::Gtk.VBox vbox17;

		private global::Gtk.VBox vbox26;

		private global::Gtk.Label label1;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ComboBox comboFileFormats;

		private global::Gtk.Label locationLabel;

		private global::MonoDevelop.Components.FolderEntry folderEntry;

		private global::Gtk.VBox vbox18;

		private global::Gtk.Label loadLabel;

		private global::Gtk.HBox hbox14;

		private global::Gtk.Label label25;

		private global::Gtk.HBox hbox10;

		private global::Gtk.VBox vbox65;

		private global::Gtk.CheckButton loadUserDataCheckButton;

		private global::Gtk.CheckButton loadPrevProjectCheckButton;

		private global::Gtk.VBox vbox19;

		private global::Gtk.Label saveLabel;

		private global::Gtk.HBox hbox11;

		private global::Gtk.Label label21;

		private global::Gtk.VBox vbox20;

		private global::Gtk.CheckButton createBackupCopyCheckButton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.OptionPanels.LoadSavePanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.OptionPanels.LoadSavePanelWidget";
			// Container child MonoDevelop.Ide.Gui.OptionPanels.LoadSavePanelWidget.Gtk.Container+ContainerChild
			this.vbox17 = new global::Gtk.VBox();
			this.vbox17.Name = "vbox17";
			this.vbox17.Spacing = 6;
			// Container child vbox17.Gtk.Box+BoxChild
			this.vbox26 = new global::Gtk.VBox();
			this.vbox26.Name = "vbox26";
			this.vbox26.Spacing = 6;
			// Container child vbox26.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Project file format to use when creating new projects:");
			this.vbox26.Add(this.label1);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox26[this.label1]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox26.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.comboFileFormats = global::Gtk.ComboBox.NewText();
			this.comboFileFormats.Name = "comboFileFormats";
			this.hbox1.Add(this.comboFileFormats);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.comboFileFormats]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			this.vbox26.Add(this.hbox1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox26[this.hbox1]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox26.Gtk.Box+BoxChild
			this.locationLabel = new global::Gtk.Label();
			this.locationLabel.Name = "locationLabel";
			this.locationLabel.Xalign = 0F;
			this.locationLabel.Yalign = 0F;
			this.locationLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Default _Solution location");
			this.locationLabel.UseUnderline = true;
			this.vbox26.Add(this.locationLabel);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox26[this.locationLabel]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox26.Gtk.Box+BoxChild
			this.folderEntry = new global::MonoDevelop.Components.FolderEntry();
			this.folderEntry.Name = "folderEntry";
			this.vbox26.Add(this.folderEntry);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox26[this.folderEntry]));
			w5.Position = 3;
			w5.Expand = false;
			w5.Fill = false;
			this.vbox17.Add(this.vbox26);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox17[this.vbox26]));
			w6.Position = 0;
			w6.Expand = false;
			// Container child vbox17.Gtk.Box+BoxChild
			this.vbox18 = new global::Gtk.VBox();
			this.vbox18.Name = "vbox18";
			this.vbox18.Spacing = 6;
			// Container child vbox18.Gtk.Box+BoxChild
			this.loadLabel = new global::Gtk.Label();
			this.loadLabel.Name = "loadLabel";
			this.loadLabel.Xalign = 0F;
			this.loadLabel.Yalign = 0F;
			this.loadLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Load</b>");
			this.loadLabel.UseMarkup = true;
			this.vbox18.Add(this.loadLabel);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox18[this.loadLabel]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox18.Gtk.Box+BoxChild
			this.hbox14 = new global::Gtk.HBox();
			this.hbox14.Name = "hbox14";
			this.hbox14.Spacing = 6;
			// Container child hbox14.Gtk.Box+BoxChild
			this.label25 = new global::Gtk.Label();
			this.label25.Name = "label25";
			this.label25.Xalign = 0F;
			this.label25.Yalign = 0F;
			this.label25.LabelProp = "    ";
			this.hbox14.Add(this.label25);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox14[this.label25]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			// Container child hbox14.Gtk.Box+BoxChild
			this.hbox10 = new global::Gtk.HBox();
			this.hbox10.Name = "hbox10";
			this.hbox10.Spacing = 6;
			// Container child hbox10.Gtk.Box+BoxChild
			this.vbox65 = new global::Gtk.VBox();
			this.vbox65.Name = "vbox65";
			this.vbox65.Spacing = 6;
			// Container child vbox65.Gtk.Box+BoxChild
			this.loadUserDataCheckButton = new global::Gtk.CheckButton();
			this.loadUserDataCheckButton.Name = "loadUserDataCheckButton";
			this.loadUserDataCheckButton.Label = global::Mono.Unix.Catalog.GetString("Load user-specific settings with the document");
			this.loadUserDataCheckButton.DrawIndicator = true;
			this.loadUserDataCheckButton.UseUnderline = true;
			this.vbox65.Add(this.loadUserDataCheckButton);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox65[this.loadUserDataCheckButton]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox65.Gtk.Box+BoxChild
			this.loadPrevProjectCheckButton = new global::Gtk.CheckButton();
			this.loadPrevProjectCheckButton.Name = "loadPrevProjectCheckButton";
			this.loadPrevProjectCheckButton.Label = global::Mono.Unix.Catalog.GetString("_Load previous solution on startup");
			this.loadPrevProjectCheckButton.DrawIndicator = true;
			this.loadPrevProjectCheckButton.UseUnderline = true;
			this.vbox65.Add(this.loadPrevProjectCheckButton);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox65[this.loadPrevProjectCheckButton]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.hbox10.Add(this.vbox65);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.hbox10[this.vbox65]));
			w11.Position = 0;
			w11.Expand = false;
			w11.Fill = false;
			this.hbox14.Add(this.hbox10);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox14[this.hbox10]));
			w12.Position = 1;
			this.vbox18.Add(this.hbox14);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox18[this.hbox14]));
			w13.Position = 1;
			this.vbox17.Add(this.vbox18);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox17[this.vbox18]));
			w14.Position = 1;
			w14.Expand = false;
			// Container child vbox17.Gtk.Box+BoxChild
			this.vbox19 = new global::Gtk.VBox();
			this.vbox19.Name = "vbox19";
			this.vbox19.Spacing = 6;
			// Container child vbox19.Gtk.Box+BoxChild
			this.saveLabel = new global::Gtk.Label();
			this.saveLabel.Name = "saveLabel";
			this.saveLabel.Xalign = 0F;
			this.saveLabel.Yalign = 0F;
			this.saveLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Save</b>");
			this.saveLabel.UseMarkup = true;
			this.vbox19.Add(this.saveLabel);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox19[this.saveLabel]));
			w15.Position = 0;
			w15.Expand = false;
			w15.Fill = false;
			// Container child vbox19.Gtk.Box+BoxChild
			this.hbox11 = new global::Gtk.HBox();
			this.hbox11.Name = "hbox11";
			this.hbox11.Spacing = 6;
			// Container child hbox11.Gtk.Box+BoxChild
			this.label21 = new global::Gtk.Label();
			this.label21.Name = "label21";
			this.label21.Xalign = 0F;
			this.label21.Yalign = 0F;
			this.label21.LabelProp = "    ";
			this.hbox11.Add(this.label21);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox11[this.label21]));
			w16.Position = 0;
			w16.Expand = false;
			w16.Fill = false;
			// Container child hbox11.Gtk.Box+BoxChild
			this.vbox20 = new global::Gtk.VBox();
			this.vbox20.Name = "vbox20";
			this.vbox20.Spacing = 6;
			// Container child vbox20.Gtk.Box+BoxChild
			this.createBackupCopyCheckButton = new global::Gtk.CheckButton();
			this.createBackupCopyCheckButton.Name = "createBackupCopyCheckButton";
			this.createBackupCopyCheckButton.Label = global::Mono.Unix.Catalog.GetString("Always create backup copy");
			this.createBackupCopyCheckButton.DrawIndicator = true;
			this.createBackupCopyCheckButton.UseUnderline = true;
			this.vbox20.Add(this.createBackupCopyCheckButton);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox20[this.createBackupCopyCheckButton]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			this.hbox11.Add(this.vbox20);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.hbox11[this.vbox20]));
			w18.Position = 1;
			this.vbox19.Add(this.hbox11);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox19[this.hbox11]));
			w19.Position = 1;
			this.vbox17.Add(this.vbox19);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.vbox17[this.vbox19]));
			w20.Position = 2;
			this.Add(this.vbox17);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
