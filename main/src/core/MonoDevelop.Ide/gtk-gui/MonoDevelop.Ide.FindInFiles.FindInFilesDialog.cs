#pragma warning disable 436

namespace MonoDevelop.Ide.FindInFiles
{
	public partial class FindInFilesDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox3;

		private global::Gtk.RadioButton toggleFindInFiles;

		private global::Gtk.VSeparator vseparator1;

		private global::Gtk.RadioButton toggleReplaceInFiles;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Table tableFindAndReplace;

		private global::Gtk.ComboBoxEntry comboboxentryFind;

		private global::Gtk.HBox hbox2;

		private global::Gtk.ComboBox comboboxScope;

		private global::Gtk.Label labelFind;

		private global::Gtk.Label labelScope;

		private global::Gtk.Table table1;

		private global::Gtk.CheckButton checkbuttonCaseSensitive;

		private global::Gtk.CheckButton checkbuttonRegexSearch;

		private global::Gtk.CheckButton checkbuttonWholeWordsOnly;

		private global::Gtk.Button buttonStop;

		private global::Gtk.Button buttonClose;

		private global::Gtk.Button buttonSearch;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.FindInFiles.FindInFilesDialog
			this.Name = "MonoDevelop.Ide.FindInFiles.FindInFilesDialog";
			this.TypeHint = ((global::Gdk.WindowTypeHint)(1));
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			this.DestroyWithParent = true;
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			// Internal child MonoDevelop.Ide.FindInFiles.FindInFilesDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.toggleFindInFiles = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Find in Files"));
			this.toggleFindInFiles.TooltipMarkup = "Switch to Find in Files";
			this.toggleFindInFiles.CanFocus = true;
			this.toggleFindInFiles.Name = "toggleFindInFiles";
			this.toggleFindInFiles.Active = true;
			this.toggleFindInFiles.DrawIndicator = false;
			this.toggleFindInFiles.UseUnderline = true;
			this.toggleFindInFiles.Relief = ((global::Gtk.ReliefStyle)(2));
			this.toggleFindInFiles.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.hbox3.Add(this.toggleFindInFiles);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.toggleFindInFiles]));
			w2.Position = 0;
			w2.Expand = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.vseparator1 = new global::Gtk.VSeparator();
			this.vseparator1.Name = "vseparator1";
			this.hbox3.Add(this.vseparator1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.vseparator1]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.toggleReplaceInFiles = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Replace in Files"));
			this.toggleReplaceInFiles.TooltipMarkup = "Switch to Replace in Files";
			this.toggleReplaceInFiles.CanFocus = true;
			this.toggleReplaceInFiles.Name = "toggleReplaceInFiles";
			this.toggleReplaceInFiles.DrawIndicator = false;
			this.toggleReplaceInFiles.UseUnderline = true;
			this.toggleReplaceInFiles.Relief = ((global::Gtk.ReliefStyle)(2));
			this.toggleReplaceInFiles.Group = this.toggleFindInFiles.Group;
			this.hbox3.Add(this.toggleReplaceInFiles);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.toggleReplaceInFiles]));
			w4.Position = 2;
			w4.Expand = false;
			this.vbox2.Add(this.hbox3);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox3]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.tableFindAndReplace = new global::Gtk.Table(((uint)(2)), ((uint)(2)), false);
			this.tableFindAndReplace.Name = "tableFindAndReplace";
			this.tableFindAndReplace.RowSpacing = ((uint)(6));
			this.tableFindAndReplace.ColumnSpacing = ((uint)(6));
			// Container child tableFindAndReplace.Gtk.Table+TableChild
			this.comboboxentryFind = global::Gtk.ComboBoxEntry.NewText();
			this.comboboxentryFind.Name = "comboboxentryFind";
			this.tableFindAndReplace.Add(this.comboboxentryFind);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.tableFindAndReplace[this.comboboxentryFind]));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFindAndReplace.Gtk.Table+TableChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.comboboxScope = global::Gtk.ComboBox.NewText();
			this.comboboxScope.Name = "comboboxScope";
			this.hbox2.Add(this.comboboxScope);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.comboboxScope]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			this.tableFindAndReplace.Add(this.hbox2);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.tableFindAndReplace[this.hbox2]));
			w8.TopAttach = ((uint)(1));
			w8.BottomAttach = ((uint)(2));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFindAndReplace.Gtk.Table+TableChild
			this.labelFind = new global::Gtk.Label();
			this.labelFind.Name = "labelFind";
			this.labelFind.Xalign = 0F;
			this.labelFind.LabelProp = global::Mono.Unix.Catalog.GetString("_Find:");
			this.labelFind.UseUnderline = true;
			this.tableFindAndReplace.Add(this.labelFind);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.tableFindAndReplace[this.labelFind]));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableFindAndReplace.Gtk.Table+TableChild
			this.labelScope = new global::Gtk.Label();
			this.labelScope.Name = "labelScope";
			this.labelScope.Xalign = 0F;
			this.labelScope.LabelProp = global::Mono.Unix.Catalog.GetString("_Look in:");
			this.labelScope.UseUnderline = true;
			this.tableFindAndReplace.Add(this.labelScope);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.tableFindAndReplace[this.labelScope]));
			w10.TopAttach = ((uint)(1));
			w10.BottomAttach = ((uint)(2));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			this.hbox1.Add(this.tableFindAndReplace);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.tableFindAndReplace]));
			w11.Position = 0;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.checkbuttonCaseSensitive = new global::Gtk.CheckButton();
			this.checkbuttonCaseSensitive.CanFocus = true;
			this.checkbuttonCaseSensitive.Name = "checkbuttonCaseSensitive";
			this.checkbuttonCaseSensitive.Label = global::Mono.Unix.Catalog.GetString("C_ase sensitive");
			this.checkbuttonCaseSensitive.DrawIndicator = true;
			this.checkbuttonCaseSensitive.UseUnderline = true;
			this.table1.Add(this.checkbuttonCaseSensitive);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table1[this.checkbuttonCaseSensitive]));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.checkbuttonRegexSearch = new global::Gtk.CheckButton();
			this.checkbuttonRegexSearch.CanFocus = true;
			this.checkbuttonRegexSearch.Name = "checkbuttonRegexSearch";
			this.checkbuttonRegexSearch.Label = global::Mono.Unix.Catalog.GetString("Rege_x search");
			this.checkbuttonRegexSearch.DrawIndicator = true;
			this.checkbuttonRegexSearch.UseUnderline = true;
			this.table1.Add(this.checkbuttonRegexSearch);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.table1[this.checkbuttonRegexSearch]));
			w14.TopAttach = ((uint)(2));
			w14.BottomAttach = ((uint)(3));
			w14.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.checkbuttonWholeWordsOnly = new global::Gtk.CheckButton();
			this.checkbuttonWholeWordsOnly.CanFocus = true;
			this.checkbuttonWholeWordsOnly.Name = "checkbuttonWholeWordsOnly";
			this.checkbuttonWholeWordsOnly.Label = global::Mono.Unix.Catalog.GetString("_Whole words only");
			this.checkbuttonWholeWordsOnly.DrawIndicator = true;
			this.checkbuttonWholeWordsOnly.UseUnderline = true;
			this.table1.Add(this.checkbuttonWholeWordsOnly);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.table1[this.checkbuttonWholeWordsOnly]));
			w15.TopAttach = ((uint)(1));
			w15.BottomAttach = ((uint)(2));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox2.Add(this.table1);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.table1]));
			w16.Position = 2;
			w16.Expand = false;
			w16.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			// Internal child MonoDevelop.Ide.FindInFiles.FindInFilesDialog.ActionArea
			global::Gtk.HButtonBox w18 = this.ActionArea;
			w18.Name = "dialog1_ActionArea";
			w18.Spacing = 6;
			w18.BorderWidth = ((uint)(5));
			w18.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonStop = new global::Gtk.Button();
			this.buttonStop.CanFocus = true;
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.UseStock = true;
			this.buttonStop.UseUnderline = true;
			this.buttonStop.Label = "gtk-stop";
			this.AddActionWidget(this.buttonStop, 0);
			global::Gtk.ButtonBox.ButtonBoxChild w19 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w18[this.buttonStop]));
			w19.Expand = false;
			w19.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonClose = new global::Gtk.Button();
			this.buttonClose.CanDefault = true;
			this.buttonClose.CanFocus = true;
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.UseStock = true;
			this.buttonClose.UseUnderline = true;
			this.buttonClose.Label = "gtk-close";
			this.AddActionWidget(this.buttonClose, -7);
			global::Gtk.ButtonBox.ButtonBoxChild w20 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w18[this.buttonClose]));
			w20.Position = 1;
			w20.Expand = false;
			w20.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonSearch = new global::Gtk.Button();
			this.buttonSearch.CanDefault = true;
			this.buttonSearch.CanFocus = true;
			this.buttonSearch.Name = "buttonSearch";
			this.buttonSearch.UseStock = true;
			this.buttonSearch.UseUnderline = true;
			this.buttonSearch.Label = "gtk-find";
			this.AddActionWidget(this.buttonSearch, 0);
			global::Gtk.ButtonBox.ButtonBoxChild w21 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w18[this.buttonSearch]));
			w21.Position = 2;
			w21.Expand = false;
			w21.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 456;
			this.DefaultHeight = 348;
			this.labelFind.MnemonicWidget = this.comboboxentryFind;
			this.labelScope.MnemonicWidget = this.comboboxScope;
			this.Hide();
		}
	}
}
#pragma warning restore 436
