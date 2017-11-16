#pragma warning disable 436

namespace MonoDevelop.Gettext
{
	internal partial class POEditorWidget
	{
		private global::Gtk.UIManager UIManager;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Notebook notebookPages;

		private global::Gtk.VBox vbox7;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label label2;

		private global::MonoDevelop.Components.SearchEntry searchEntryFilter;

		private global::Gtk.ToggleButton togglebuttonOk;

		private global::Gtk.HBox togglebuttonOkHbox;

		private global::MonoDevelop.Components.ImageView togglebuttonOkIcon;

		private global::Gtk.Label togglebuttonOkLabel;

		private global::Gtk.ToggleButton togglebuttonMissing;

		private global::Gtk.HBox togglebuttonMissingHbox;

		private global::MonoDevelop.Components.ImageView togglebuttonMissingIcon;

		private global::Gtk.Label togglebuttonMissingLabel;

		private global::Gtk.ToggleButton togglebuttonFuzzy;

		private global::Gtk.HBox togglebuttonFuzzyHbox;

		private global::MonoDevelop.Components.ImageView togglebuttonFuzzyIcon;

		private global::Gtk.Label togglebuttonFuzzyLabel;

		private global::Gtk.VPaned vpaned2;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView treeviewEntries;

		private global::Gtk.Table table1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Label label6;

		private global::Gtk.ScrolledWindow scrolledwindow3;

		private global::Gtk.TextView textviewComments;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Label label7;

		private global::Gtk.Notebook notebookTranslated;

		private global::Gtk.Label label1;

		private global::Gtk.VBox vbox5;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Label label8;

		private global::Gtk.CheckButton checkbuttonWhiteSpaces;

		private global::Gtk.ScrolledWindow scrolledwindowOriginal;

		private global::Gtk.VBox vbox8;

		private global::Gtk.Label label9;

		private global::Gtk.ScrolledWindow scrolledwindowPlural;

		private global::Gtk.VBox vbox6;

		private global::Gtk.Label label4;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.TreeView treeviewFoundIn;

		private global::Gtk.Label label5;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Toolbar toolbarPages;

		private global::Gtk.ProgressBar progressbar1;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Gettext.POEditorWidget
			Stetic.BinContainer w1 = global::Stetic.BinContainer.Attach(this);
			this.UIManager = new global::Gtk.UIManager();
			global::Gtk.ActionGroup w2 = new global::Gtk.ActionGroup("Default");
			this.UIManager.InsertActionGroup(w2, 0);
			this.Name = "MonoDevelop.Gettext.POEditorWidget";
			// Container child MonoDevelop.Gettext.POEditorWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.notebookPages = new global::Gtk.Notebook();
			this.notebookPages.CanFocus = true;
			this.notebookPages.Name = "notebookPages";
			this.notebookPages.CurrentPage = 0;
			this.notebookPages.ShowBorder = false;
			this.notebookPages.ShowTabs = false;
			// Container child notebookPages.Gtk.Notebook+NotebookChild
			this.vbox7 = new global::Gtk.VBox();
			this.vbox7.Name = "vbox7";
			this.vbox7.Spacing = 6;
			// Container child vbox7.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("_Filter:");
			this.label2.UseUnderline = true;
			this.hbox2.Add(this.label2);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label2]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.searchEntryFilter = new global::MonoDevelop.Components.SearchEntry();
			this.searchEntryFilter.Name = "searchEntryFilter";
			this.searchEntryFilter.ForceFilterButtonVisible = false;
			this.searchEntryFilter.HasFrame = false;
			this.searchEntryFilter.RoundedShape = false;
			this.searchEntryFilter.IsCheckMenu = false;
			this.searchEntryFilter.ActiveFilterID = 0;
			this.searchEntryFilter.Ready = false;
			this.searchEntryFilter.HasFocus = false;
			this.hbox2.Add(this.searchEntryFilter);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.searchEntryFilter]));
			w4.Position = 1;
			// Container child hbox2.Gtk.Box+BoxChild
			this.togglebuttonOk = new global::Gtk.ToggleButton();
			this.togglebuttonOk.CanFocus = true;
			this.togglebuttonOk.Name = "togglebuttonOk";
			// Container child togglebuttonOk.Gtk.Container+ContainerChild
			this.togglebuttonOkHbox = new global::Gtk.HBox();
			this.togglebuttonOkHbox.Name = "togglebuttonOkHbox";
			this.togglebuttonOkHbox.Spacing = 2;
			// Container child togglebuttonOkHbox.Gtk.Box+BoxChild
			this.togglebuttonOkIcon = new global::MonoDevelop.Components.ImageView();
			this.togglebuttonOkIcon.Name = "togglebuttonOkIcon";
			this.togglebuttonOkIcon.IconId = "md-done";
			this.togglebuttonOkIcon.IconSize = ((global::Gtk.IconSize)(1));
			this.togglebuttonOkHbox.Add(this.togglebuttonOkIcon);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.togglebuttonOkHbox[this.togglebuttonOkIcon]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child togglebuttonOkHbox.Gtk.Box+BoxChild
			this.togglebuttonOkLabel = new global::Gtk.Label();
			this.togglebuttonOkLabel.Name = "togglebuttonOkLabel";
			this.togglebuttonOkLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Valid");
			this.togglebuttonOkLabel.UseUnderline = true;
			this.togglebuttonOkHbox.Add(this.togglebuttonOkLabel);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.togglebuttonOkHbox[this.togglebuttonOkLabel]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.togglebuttonOk.Add(this.togglebuttonOkHbox);
			this.hbox2.Add(this.togglebuttonOk);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.togglebuttonOk]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.togglebuttonMissing = new global::Gtk.ToggleButton();
			this.togglebuttonMissing.CanFocus = true;
			this.togglebuttonMissing.Name = "togglebuttonMissing";
			// Container child togglebuttonMissing.Gtk.Container+ContainerChild
			this.togglebuttonMissingHbox = new global::Gtk.HBox();
			this.togglebuttonMissingHbox.Name = "togglebuttonMissingHbox";
			this.togglebuttonMissingHbox.Spacing = 2;
			// Container child togglebuttonMissingHbox.Gtk.Box+BoxChild
			this.togglebuttonMissingIcon = new global::MonoDevelop.Components.ImageView();
			this.togglebuttonMissingIcon.Name = "togglebuttonMissingIcon";
			this.togglebuttonMissingIcon.IconId = "md-warning";
			this.togglebuttonMissingIcon.IconSize = ((global::Gtk.IconSize)(1));
			this.togglebuttonMissingHbox.Add(this.togglebuttonMissingIcon);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.togglebuttonMissingHbox[this.togglebuttonMissingIcon]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child togglebuttonMissingHbox.Gtk.Box+BoxChild
			this.togglebuttonMissingLabel = new global::Gtk.Label();
			this.togglebuttonMissingLabel.Name = "togglebuttonMissingLabel";
			this.togglebuttonMissingLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Missing");
			this.togglebuttonMissingLabel.UseUnderline = true;
			this.togglebuttonMissingHbox.Add(this.togglebuttonMissingLabel);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.togglebuttonMissingHbox[this.togglebuttonMissingLabel]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.togglebuttonMissing.Add(this.togglebuttonMissingHbox);
			this.hbox2.Add(this.togglebuttonMissing);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.togglebuttonMissing]));
			w12.Position = 3;
			w12.Expand = false;
			w12.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.togglebuttonFuzzy = new global::Gtk.ToggleButton();
			this.togglebuttonFuzzy.CanFocus = true;
			this.togglebuttonFuzzy.Name = "togglebuttonFuzzy";
			// Container child togglebuttonFuzzy.Gtk.Container+ContainerChild
			this.togglebuttonFuzzyHbox = new global::Gtk.HBox();
			this.togglebuttonFuzzyHbox.Name = "togglebuttonFuzzyHbox";
			this.togglebuttonFuzzyHbox.Spacing = 2;
			// Container child togglebuttonFuzzyHbox.Gtk.Box+BoxChild
			this.togglebuttonFuzzyIcon = new global::MonoDevelop.Components.ImageView();
			this.togglebuttonFuzzyIcon.Name = "togglebuttonFuzzyIcon";
			this.togglebuttonFuzzyIcon.IconId = "md-error";
			this.togglebuttonFuzzyIcon.IconSize = ((global::Gtk.IconSize)(1));
			this.togglebuttonFuzzyHbox.Add(this.togglebuttonFuzzyIcon);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.togglebuttonFuzzyHbox[this.togglebuttonFuzzyIcon]));
			w13.Position = 0;
			w13.Expand = false;
			w13.Fill = false;
			// Container child togglebuttonFuzzyHbox.Gtk.Box+BoxChild
			this.togglebuttonFuzzyLabel = new global::Gtk.Label();
			this.togglebuttonFuzzyLabel.Name = "togglebuttonFuzzyLabel";
			this.togglebuttonFuzzyLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Fuzzy");
			this.togglebuttonFuzzyLabel.UseUnderline = true;
			this.togglebuttonFuzzyHbox.Add(this.togglebuttonFuzzyLabel);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.togglebuttonFuzzyHbox[this.togglebuttonFuzzyLabel]));
			w14.Position = 1;
			w14.Expand = false;
			w14.Fill = false;
			this.togglebuttonFuzzy.Add(this.togglebuttonFuzzyHbox);
			this.hbox2.Add(this.togglebuttonFuzzy);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.togglebuttonFuzzy]));
			w16.Position = 4;
			w16.Expand = false;
			w16.Fill = false;
			this.vbox7.Add(this.hbox2);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.hbox2]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			// Container child vbox7.Gtk.Box+BoxChild
			this.vpaned2 = new global::Gtk.VPaned();
			this.vpaned2.CanFocus = true;
			this.vpaned2.Name = "vpaned2";
			this.vpaned2.Position = 186;
			// Container child vpaned2.Gtk.Paned+PanedChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.treeviewEntries = new global::Gtk.TreeView();
			this.treeviewEntries.CanFocus = true;
			this.treeviewEntries.Name = "treeviewEntries";
			this.scrolledwindow1.Add(this.treeviewEntries);
			this.vpaned2.Add(this.scrolledwindow1);
			global::Gtk.Paned.PanedChild w19 = ((global::Gtk.Paned.PanedChild)(this.vpaned2[this.scrolledwindow1]));
			w19.Resize = false;
			// Container child vpaned2.Gtk.Paned+PanedChild
			this.table1 = new global::Gtk.Table(((uint)(2)), ((uint)(2)), true);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("_Comments:");
			this.label6.UseUnderline = true;
			this.vbox3.Add(this.label6);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.label6]));
			w20.Position = 0;
			w20.Expand = false;
			w20.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.scrolledwindow3 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow3.CanFocus = true;
			this.scrolledwindow3.Name = "scrolledwindow3";
			this.scrolledwindow3.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow3.Gtk.Container+ContainerChild
			this.textviewComments = new global::Gtk.TextView();
			this.textviewComments.CanFocus = true;
			this.textviewComments.Name = "textviewComments";
			this.textviewComments.AcceptsTab = false;
			this.scrolledwindow3.Add(this.textviewComments);
			this.vbox3.Add(this.scrolledwindow3);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.scrolledwindow3]));
			w22.Position = 1;
			this.table1.Add(this.vbox3);
			global::Gtk.Table.TableChild w23 = ((global::Gtk.Table.TableChild)(this.table1[this.vbox3]));
			w23.TopAttach = ((uint)(1));
			w23.BottomAttach = ((uint)(2));
			w23.LeftAttach = ((uint)(1));
			w23.RightAttach = ((uint)(2));
			w23.XOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label7 = new global::Gtk.Label();
			this.label7.Name = "label7";
			this.label7.Xalign = 0F;
			this.label7.LabelProp = global::Mono.Unix.Catalog.GetString("_Translated (msgstr):");
			this.label7.UseUnderline = true;
			this.vbox4.Add(this.label7);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.label7]));
			w24.Position = 0;
			w24.Expand = false;
			w24.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.notebookTranslated = new global::Gtk.Notebook();
			this.notebookTranslated.CanFocus = true;
			this.notebookTranslated.Name = "notebookTranslated";
			this.notebookTranslated.CurrentPage = 0;
			// Notebook tab
			global::Gtk.Label w25 = new global::Gtk.Label();
			w25.Visible = true;
			this.notebookTranslated.Add(w25);
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("page1");
			this.notebookTranslated.SetTabLabel(w25, this.label1);
			this.label1.ShowAll();
			this.vbox4.Add(this.notebookTranslated);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.notebookTranslated]));
			w26.Position = 1;
			this.table1.Add(this.vbox4);
			global::Gtk.Table.TableChild w27 = ((global::Gtk.Table.TableChild)(this.table1[this.vbox4]));
			w27.TopAttach = ((uint)(1));
			w27.BottomAttach = ((uint)(2));
			w27.XOptions = ((global::Gtk.AttachOptions)(4));
			w27.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("Original (msgid):");
			this.hbox3.Add(this.label8);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.label8]));
			w28.Position = 0;
			// Container child hbox3.Gtk.Box+BoxChild
			this.checkbuttonWhiteSpaces = new global::Gtk.CheckButton();
			this.checkbuttonWhiteSpaces.CanFocus = true;
			this.checkbuttonWhiteSpaces.Name = "checkbuttonWhiteSpaces";
			this.checkbuttonWhiteSpaces.Label = global::Mono.Unix.Catalog.GetString("S_how whitespaces");
			this.checkbuttonWhiteSpaces.DrawIndicator = true;
			this.checkbuttonWhiteSpaces.UseUnderline = true;
			this.hbox3.Add(this.checkbuttonWhiteSpaces);
			global::Gtk.Box.BoxChild w29 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.checkbuttonWhiteSpaces]));
			w29.Position = 1;
			w29.Expand = false;
			this.vbox5.Add(this.hbox3);
			global::Gtk.Box.BoxChild w30 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox3]));
			w30.Position = 0;
			w30.Expand = false;
			w30.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.scrolledwindowOriginal = new global::Gtk.ScrolledWindow();
			this.scrolledwindowOriginal.CanFocus = true;
			this.scrolledwindowOriginal.Name = "scrolledwindowOriginal";
			this.scrolledwindowOriginal.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox5.Add(this.scrolledwindowOriginal);
			global::Gtk.Box.BoxChild w31 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.scrolledwindowOriginal]));
			w31.Position = 1;
			// Container child vbox5.Gtk.Box+BoxChild
			this.vbox8 = new global::Gtk.VBox();
			this.vbox8.Name = "vbox8";
			this.vbox8.Spacing = 6;
			// Container child vbox8.Gtk.Box+BoxChild
			this.label9 = new global::Gtk.Label();
			this.label9.Name = "label9";
			this.label9.Xalign = 0F;
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("Original plural (msgid_plural):");
			this.vbox8.Add(this.label9);
			global::Gtk.Box.BoxChild w32 = ((global::Gtk.Box.BoxChild)(this.vbox8[this.label9]));
			w32.Position = 0;
			w32.Expand = false;
			w32.Fill = false;
			// Container child vbox8.Gtk.Box+BoxChild
			this.scrolledwindowPlural = new global::Gtk.ScrolledWindow();
			this.scrolledwindowPlural.CanFocus = true;
			this.scrolledwindowPlural.Name = "scrolledwindowPlural";
			this.scrolledwindowPlural.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox8.Add(this.scrolledwindowPlural);
			global::Gtk.Box.BoxChild w33 = ((global::Gtk.Box.BoxChild)(this.vbox8[this.scrolledwindowPlural]));
			w33.Position = 1;
			this.vbox5.Add(this.vbox8);
			global::Gtk.Box.BoxChild w34 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.vbox8]));
			w34.Position = 2;
			this.table1.Add(this.vbox5);
			// Container child table1.Gtk.Table+TableChild
			this.vbox6 = new global::Gtk.VBox();
			this.vbox6.Name = "vbox6";
			this.vbox6.Spacing = 6;
			// Container child vbox6.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("F_ound in:");
			this.label4.UseUnderline = true;
			this.vbox6.Add(this.label4);
			global::Gtk.Box.BoxChild w36 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.label4]));
			w36.Position = 0;
			w36.Expand = false;
			w36.Fill = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.treeviewFoundIn = new global::Gtk.TreeView();
			this.treeviewFoundIn.CanFocus = true;
			this.treeviewFoundIn.Name = "treeviewFoundIn";
			this.scrolledwindow2.Add(this.treeviewFoundIn);
			this.vbox6.Add(this.scrolledwindow2);
			global::Gtk.Box.BoxChild w38 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.scrolledwindow2]));
			w38.Position = 1;
			this.table1.Add(this.vbox6);
			global::Gtk.Table.TableChild w39 = ((global::Gtk.Table.TableChild)(this.table1[this.vbox6]));
			w39.LeftAttach = ((uint)(1));
			w39.RightAttach = ((uint)(2));
			w39.XOptions = ((global::Gtk.AttachOptions)(4));
			w39.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vpaned2.Add(this.table1);
			global::Gtk.Paned.PanedChild w40 = ((global::Gtk.Paned.PanedChild)(this.vpaned2[this.table1]));
			w40.Resize = false;
			this.vbox7.Add(this.vpaned2);
			global::Gtk.Box.BoxChild w41 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.vpaned2]));
			w41.Position = 1;
			this.notebookPages.Add(this.vbox7);
			// Notebook tab
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("page1");
			this.notebookPages.SetTabLabel(this.vbox7, this.label5);
			this.label5.ShowAll();
			this.vbox2.Add(this.notebookPages);
			global::Gtk.Box.BoxChild w43 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.notebookPages]));
			w43.Position = 0;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.UIManager.AddUiFromString("<ui><toolbar name=\'toolbarPages\'/></ui>");
			this.toolbarPages = ((global::Gtk.Toolbar)(this.UIManager.GetWidget("/toolbarPages")));
			this.toolbarPages.Name = "toolbarPages";
			this.toolbarPages.ShowArrow = false;
			this.toolbarPages.ToolbarStyle = ((global::Gtk.ToolbarStyle)(0));
			this.toolbarPages.IconSize = ((global::Gtk.IconSize)(3));
			this.hbox1.Add(this.toolbarPages);
			global::Gtk.Box.BoxChild w44 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.toolbarPages]));
			w44.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.progressbar1 = new global::Gtk.ProgressBar();
			this.progressbar1.Name = "progressbar1";
			this.hbox1.Add(this.progressbar1);
			global::Gtk.Box.BoxChild w45 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.progressbar1]));
			w45.Position = 1;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w46 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w46.Position = 1;
			w46.Expand = false;
			w46.Fill = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			w1.SetUiManager(UIManager);
			this.label6.MnemonicWidget = this.textviewComments;
			this.label7.MnemonicWidget = this.notebookTranslated;
			this.label4.MnemonicWidget = this.treeviewFoundIn;
			this.Show();
		}
	}
}
#pragma warning restore 436
