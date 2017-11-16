#pragma warning disable 436

namespace MonoDevelop.VersionControl
{
	public partial class CommitMessageStylePanelWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Table table2;

		private global::Gtk.Entry entryHeader;

		private global::Gtk.Label label4;

		private global::Gtk.Table tableFlags;

		private global::Gtk.CheckButton checkIncludeDirs;

		private global::Gtk.CheckButton checkIndent;

		private global::Gtk.CheckButton checkIndentEntries;

		private global::Gtk.CheckButton checkLineSep;

		private global::Gtk.CheckButton checkMsgInNewLine;

		private global::Gtk.CheckButton checkOneLinePerFile;

		private global::Gtk.CheckButton checkUseBullets;

		private global::Gtk.CheckButton checkWrap;

		private global::Gtk.Label label9;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TextView textview;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.CommitMessageStylePanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.VersionControl.CommitMessageStylePanelWidget";
			// Container child MonoDevelop.VersionControl.CommitMessageStylePanelWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.table2 = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table2.Name = "table2";
			this.table2.RowSpacing = ((uint)(6));
			this.table2.ColumnSpacing = ((uint)(6));
			// Container child table2.Gtk.Table+TableChild
			this.entryHeader = new global::Gtk.Entry();
			this.entryHeader.CanFocus = true;
			this.entryHeader.Name = "entryHeader";
			this.entryHeader.IsEditable = true;
			this.entryHeader.InvisibleChar = '●';
			this.table2.Add(this.entryHeader);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.table2[this.entryHeader]));
			w1.LeftAttach = ((uint)(1));
			w1.RightAttach = ((uint)(2));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Message Header:");
			this.table2.Add(this.label4);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table2[this.label4]));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox1.Add(this.table2);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.table2]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.tableFlags = new global::Gtk.Table(((uint)(4)), ((uint)(2)), false);
			this.tableFlags.Name = "tableFlags";
			this.tableFlags.RowSpacing = ((uint)(6));
			this.tableFlags.ColumnSpacing = ((uint)(6));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkIncludeDirs = new global::Gtk.CheckButton();
			this.checkIncludeDirs.CanFocus = true;
			this.checkIncludeDirs.Name = "checkIncludeDirs";
			this.checkIncludeDirs.Label = global::Mono.Unix.Catalog.GetString("Include file directories");
			this.checkIncludeDirs.DrawIndicator = true;
			this.checkIncludeDirs.UseUnderline = true;
			this.tableFlags.Add(this.checkIncludeDirs);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkIncludeDirs]));
			w4.TopAttach = ((uint)(3));
			w4.BottomAttach = ((uint)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkIndent = new global::Gtk.CheckButton();
			this.checkIndent.CanFocus = true;
			this.checkIndent.Name = "checkIndent";
			this.checkIndent.Label = global::Mono.Unix.Catalog.GetString("Align message text");
			this.checkIndent.DrawIndicator = true;
			this.checkIndent.UseUnderline = true;
			this.tableFlags.Add(this.checkIndent);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkIndent]));
			w5.TopAttach = ((uint)(1));
			w5.BottomAttach = ((uint)(2));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkIndentEntries = new global::Gtk.CheckButton();
			this.checkIndentEntries.CanFocus = true;
			this.checkIndentEntries.Name = "checkIndentEntries";
			this.checkIndentEntries.Label = global::Mono.Unix.Catalog.GetString("Indent entries");
			this.checkIndentEntries.DrawIndicator = true;
			this.checkIndentEntries.UseUnderline = true;
			this.tableFlags.Add(this.checkIndentEntries);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkIndentEntries]));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkLineSep = new global::Gtk.CheckButton();
			this.checkLineSep.CanFocus = true;
			this.checkLineSep.Name = "checkLineSep";
			this.checkLineSep.Label = global::Mono.Unix.Catalog.GetString("Add a blank line between messages");
			this.checkLineSep.DrawIndicator = true;
			this.checkLineSep.UseUnderline = true;
			this.tableFlags.Add(this.checkLineSep);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkLineSep]));
			w7.TopAttach = ((uint)(1));
			w7.BottomAttach = ((uint)(2));
			w7.LeftAttach = ((uint)(1));
			w7.RightAttach = ((uint)(2));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkMsgInNewLine = new global::Gtk.CheckButton();
			this.checkMsgInNewLine.CanFocus = true;
			this.checkMsgInNewLine.Name = "checkMsgInNewLine";
			this.checkMsgInNewLine.Label = global::Mono.Unix.Catalog.GetString("File list and message in separate lines");
			this.checkMsgInNewLine.DrawIndicator = true;
			this.checkMsgInNewLine.UseUnderline = true;
			this.tableFlags.Add(this.checkMsgInNewLine);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkMsgInNewLine]));
			w8.TopAttach = ((uint)(2));
			w8.BottomAttach = ((uint)(3));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkOneLinePerFile = new global::Gtk.CheckButton();
			this.checkOneLinePerFile.CanFocus = true;
			this.checkOneLinePerFile.Name = "checkOneLinePerFile";
			this.checkOneLinePerFile.Label = global::Mono.Unix.Catalog.GetString("One line per file");
			this.checkOneLinePerFile.DrawIndicator = true;
			this.checkOneLinePerFile.UseUnderline = true;
			this.tableFlags.Add(this.checkOneLinePerFile);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkOneLinePerFile]));
			w9.TopAttach = ((uint)(2));
			w9.BottomAttach = ((uint)(3));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkUseBullets = new global::Gtk.CheckButton();
			this.checkUseBullets.CanFocus = true;
			this.checkUseBullets.Name = "checkUseBullets";
			this.checkUseBullets.Label = global::Mono.Unix.Catalog.GetString("Use bullets");
			this.checkUseBullets.DrawIndicator = true;
			this.checkUseBullets.UseUnderline = true;
			this.tableFlags.Add(this.checkUseBullets);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkUseBullets]));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFlags.Gtk.Table+TableChild
			this.checkWrap = new global::Gtk.CheckButton();
			this.checkWrap.CanFocus = true;
			this.checkWrap.Name = "checkWrap";
			this.checkWrap.Label = global::Mono.Unix.Catalog.GetString("Wrap");
			this.checkWrap.DrawIndicator = true;
			this.checkWrap.UseUnderline = true;
			this.tableFlags.Add(this.checkWrap);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.tableFlags[this.checkWrap]));
			w11.TopAttach = ((uint)(3));
			w11.BottomAttach = ((uint)(4));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(2));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox1.Add(this.tableFlags);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.tableFlags]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label9 = new global::Gtk.Label();
			this.label9.Name = "label9";
			this.label9.Xalign = 0F;
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("Preview:");
			this.vbox1.Add(this.label9);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label9]));
			w13.Position = 2;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Sensitive = false;
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.VscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.GtkScrolledWindow.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.textview = new global::Gtk.TextView();
			this.textview.CanFocus = true;
			this.textview.Name = "textview";
			this.GtkScrolledWindow.Add(this.textview);
			this.vbox1.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkScrolledWindow]));
			w15.Position = 3;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.entryHeader.Changed += new global::System.EventHandler(this.OnEntryHeaderChanged);
			this.checkWrap.Toggled += new global::System.EventHandler(this.OnCheckWrapToggled);
			this.checkUseBullets.Toggled += new global::System.EventHandler(this.OnCheckUseBulletsToggled);
			this.checkOneLinePerFile.Toggled += new global::System.EventHandler(this.OnCheckOneLinePerFileToggled);
			this.checkMsgInNewLine.Toggled += new global::System.EventHandler(this.OnCheckMsgInNewLineToggled);
			this.checkLineSep.Toggled += new global::System.EventHandler(this.OnCheckLineSepToggled);
			this.checkIndentEntries.Toggled += new global::System.EventHandler(this.OnCheckIndentEntriesToggled);
			this.checkIndent.Toggled += new global::System.EventHandler(this.OnCheckIndentToggled);
			this.checkIncludeDirs.Toggled += new global::System.EventHandler(this.OnCheckIncludeDirsToggled);
		}
	}
}
#pragma warning restore 436
