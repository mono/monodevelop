#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class CustomCommandWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.HSeparator hseparator2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ComboBox comboType;

		private global::Gtk.Button buttonRemove;

		private global::Gtk.Table tableData;

		private global::Gtk.Entry entryCommand;

		private global::Gtk.Entry entryName;

		private global::Gtk.HBox hbox2;

		private global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton tagSelectorDirectory;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Button buttonBrowse;

		private global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton tagSelectorCommand;

		private global::Gtk.Label label1;

		private global::Gtk.Label label3;

		private global::Gtk.Label labelName;

		private global::Gtk.Entry workingdirEntry;

		private global::Gtk.HBox boxData;

		private global::Gtk.CheckButton checkExternalCons;

		private global::Gtk.CheckButton checkPauseCons;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.CustomCommandWidget
			global::Stetic.BinContainer.Attach(this);
			this.CanFocus = true;
			this.Events = ((global::Gdk.EventMask)(16384));
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.CustomCommandWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.CustomCommandWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.CanFocus = true;
			this.vbox1.Events = ((global::Gdk.EventMask)(28672));
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hseparator2 = new global::Gtk.HSeparator();
			this.hseparator2.Name = "hseparator2";
			this.vbox1.Add(this.hseparator2);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hseparator2]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.comboType = global::Gtk.ComboBox.NewText();
			this.comboType.CanFocus = true;
			this.comboType.Name = "comboType";
			this.hbox1.Add(this.comboType);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.comboType]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonRemove = new global::Gtk.Button();
			this.buttonRemove.CanFocus = true;
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseStock = true;
			this.buttonRemove.UseUnderline = true;
			this.buttonRemove.Label = "gtk-remove";
			this.hbox1.Add(this.buttonRemove);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.buttonRemove]));
			w3.PackType = ((global::Gtk.PackType)(1));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			this.vbox1.Add(this.hbox1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.tableData = new global::Gtk.Table(((uint)(3)), ((uint)(3)), false);
			this.tableData.Name = "tableData";
			this.tableData.RowSpacing = ((uint)(6));
			this.tableData.ColumnSpacing = ((uint)(6));
			// Container child tableData.Gtk.Table+TableChild
			this.entryCommand = new global::Gtk.Entry();
			this.entryCommand.CanFocus = true;
			this.entryCommand.Name = "entryCommand";
			this.entryCommand.IsEditable = true;
			this.entryCommand.InvisibleChar = '●';
			this.tableData.Add(this.entryCommand);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.tableData[this.entryCommand]));
			w5.TopAttach = ((uint)(1));
			w5.BottomAttach = ((uint)(2));
			w5.LeftAttach = ((uint)(1));
			w5.RightAttach = ((uint)(2));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableData.Gtk.Table+TableChild
			this.entryName = new global::Gtk.Entry();
			this.entryName.CanFocus = true;
			this.entryName.Name = "entryName";
			this.entryName.IsEditable = true;
			this.entryName.InvisibleChar = '●';
			this.tableData.Add(this.entryName);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.tableData[this.entryName]));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableData.Gtk.Table+TableChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.tagSelectorDirectory = new global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton();
			this.tagSelectorDirectory.Events = ((global::Gdk.EventMask)(256));
			this.tagSelectorDirectory.Name = "tagSelectorDirectory";
			this.hbox2.Add(this.tagSelectorDirectory);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.tagSelectorDirectory]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			this.tableData.Add(this.hbox2);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.tableData[this.hbox2]));
			w8.TopAttach = ((uint)(2));
			w8.BottomAttach = ((uint)(3));
			w8.LeftAttach = ((uint)(2));
			w8.RightAttach = ((uint)(3));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableData.Gtk.Table+TableChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.buttonBrowse = new global::Gtk.Button();
			this.buttonBrowse.CanFocus = true;
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.UseUnderline = true;
			this.buttonBrowse.Label = global::Mono.Unix.Catalog.GetString("Browse...");
			this.hbox3.Add(this.buttonBrowse);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.buttonBrowse]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.tagSelectorCommand = new global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton();
			this.tagSelectorCommand.Events = ((global::Gdk.EventMask)(256));
			this.tagSelectorCommand.Name = "tagSelectorCommand";
			this.hbox3.Add(this.tagSelectorCommand);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.tagSelectorCommand]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.tableData.Add(this.hbox3);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.tableData[this.hbox3]));
			w11.TopAttach = ((uint)(1));
			w11.BottomAttach = ((uint)(2));
			w11.LeftAttach = ((uint)(2));
			w11.RightAttach = ((uint)(3));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableData.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Working Directory:");
			this.tableData.Add(this.label1);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.tableData[this.label1]));
			w12.TopAttach = ((uint)(2));
			w12.BottomAttach = ((uint)(3));
			w12.XOptions = ((global::Gtk.AttachOptions)(4));
			w12.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableData.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Command:");
			this.tableData.Add(this.label3);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.tableData[this.label3]));
			w13.TopAttach = ((uint)(1));
			w13.BottomAttach = ((uint)(2));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableData.Gtk.Table+TableChild
			this.labelName = new global::Gtk.Label();
			this.labelName.Name = "labelName";
			this.labelName.Xalign = 0F;
			this.labelName.LabelProp = global::Mono.Unix.Catalog.GetString("Name:");
			this.tableData.Add(this.labelName);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.tableData[this.labelName]));
			w14.XOptions = ((global::Gtk.AttachOptions)(4));
			w14.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableData.Gtk.Table+TableChild
			this.workingdirEntry = new global::Gtk.Entry();
			this.workingdirEntry.CanFocus = true;
			this.workingdirEntry.Name = "workingdirEntry";
			this.workingdirEntry.IsEditable = true;
			this.workingdirEntry.InvisibleChar = '●';
			this.tableData.Add(this.workingdirEntry);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.tableData[this.workingdirEntry]));
			w15.TopAttach = ((uint)(2));
			w15.BottomAttach = ((uint)(3));
			w15.LeftAttach = ((uint)(1));
			w15.RightAttach = ((uint)(2));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox1.Add(this.tableData);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.tableData]));
			w16.Position = 2;
			w16.Expand = false;
			w16.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.boxData = new global::Gtk.HBox();
			this.boxData.Name = "boxData";
			this.boxData.Spacing = 6;
			// Container child boxData.Gtk.Box+BoxChild
			this.checkExternalCons = new global::Gtk.CheckButton();
			this.checkExternalCons.CanFocus = true;
			this.checkExternalCons.Name = "checkExternalCons";
			this.checkExternalCons.Label = global::Mono.Unix.Catalog.GetString("Run on e_xternal console");
			this.checkExternalCons.DrawIndicator = true;
			this.checkExternalCons.UseUnderline = true;
			this.boxData.Add(this.checkExternalCons);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.boxData[this.checkExternalCons]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			// Container child boxData.Gtk.Box+BoxChild
			this.checkPauseCons = new global::Gtk.CheckButton();
			this.checkPauseCons.CanFocus = true;
			this.checkPauseCons.Name = "checkPauseCons";
			this.checkPauseCons.Label = global::Mono.Unix.Catalog.GetString("Pause _console output");
			this.checkPauseCons.DrawIndicator = true;
			this.checkPauseCons.UseUnderline = true;
			this.boxData.Add(this.checkPauseCons);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.boxData[this.checkPauseCons]));
			w18.Position = 1;
			w18.Expand = false;
			w18.Fill = false;
			this.vbox1.Add(this.boxData);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.boxData]));
			w19.Position = 3;
			w19.Expand = false;
			w19.Fill = false;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
			this.comboType.Changed += new global::System.EventHandler(this.OnComboTypeChanged);
			this.buttonRemove.Clicked += new global::System.EventHandler(this.OnButtonRemoveClicked);
			this.workingdirEntry.Changed += new global::System.EventHandler(this.OnWorkingdirEntryChanged);
			this.buttonBrowse.Clicked += new global::System.EventHandler(this.OnButtonBrowseClicked);
			this.entryName.Changed += new global::System.EventHandler(this.OnEntryNameChanged);
			this.entryCommand.Changed += new global::System.EventHandler(this.OnEntryCommandChanged);
			this.checkExternalCons.Clicked += new global::System.EventHandler(this.OnCheckExternalConsClicked);
			this.checkPauseCons.Clicked += new global::System.EventHandler(this.OnCheckPauseConsClicked);
		}
	}
}
#pragma warning restore 436
