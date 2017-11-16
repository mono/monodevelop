#pragma warning disable 436

namespace MonoDevelop.SourceEditor.OptionPanels
{
	internal partial class BehaviorPanel
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Label GtkLabel5;

		private global::Gtk.Alignment alignment3;

		private global::Gtk.VBox vbox4;

		private global::Gtk.CheckButton autoInsertBraceCheckbutton;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label fixed1;

		private global::Gtk.CheckButton smartSemicolonPlaceCheckbutton;

		private global::Gtk.CheckButton checkbuttonOnTheFlyFormatting;

		private global::Gtk.CheckButton checkbuttonFormatOnSave;

		private global::Gtk.CheckButton checkbuttonAutoSetSearchPatternCasing;

		private global::Gtk.CheckButton checkbuttonGenerateFormattingUndoStep;

		private global::Gtk.CheckButton checkbuttonEnableSelectionSurrounding;

		private global::Gtk.Label GtkLabel8;

		private global::Gtk.Alignment GtkAlignment;

		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label1;

		private global::Gtk.ComboBox indentationCombobox;

		private global::Gtk.CheckButton tabAsReindentCheckbutton;

		private global::Gtk.CheckButton smartBackspaceCheckbutton;

		private global::Gtk.Label GtkLabel10;

		private global::Gtk.Alignment alignment4;

		private global::Gtk.VBox vbox5;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Label label2;

		private global::Gtk.ComboBox controlLeftRightCombobox;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.SourceEditor.OptionPanels.BehaviorPanel
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.SourceEditor.OptionPanels.BehaviorPanel";
			// Container child MonoDevelop.SourceEditor.OptionPanels.BehaviorPanel.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkLabel5 = new global::Gtk.Label();
			this.GtkLabel5.Name = "GtkLabel5";
			this.GtkLabel5.Xalign = 0F;
			this.GtkLabel5.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Automatic behaviors</b>");
			this.GtkLabel5.UseMarkup = true;
			this.vbox1.Add(this.GtkLabel5);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkLabel5]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment3 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment3.Name = "alignment3";
			this.alignment3.LeftPadding = ((uint)(12));
			// Container child alignment3.Gtk.Container+ContainerChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.autoInsertBraceCheckbutton = new global::Gtk.CheckButton();
			this.autoInsertBraceCheckbutton.CanFocus = true;
			this.autoInsertBraceCheckbutton.Name = "autoInsertBraceCheckbutton";
			this.autoInsertBraceCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Insert matching brace");
			this.autoInsertBraceCheckbutton.DrawIndicator = true;
			this.autoInsertBraceCheckbutton.UseUnderline = true;
			this.vbox4.Add(this.autoInsertBraceCheckbutton);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.autoInsertBraceCheckbutton]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.fixed1 = new global::Gtk.Label();
			this.fixed1.Name = "fixed1";
			this.hbox2.Add(this.fixed1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.fixed1]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			w3.Padding = ((uint)(6));
			// Container child hbox2.Gtk.Box+BoxChild
			this.smartSemicolonPlaceCheckbutton = new global::Gtk.CheckButton();
			this.smartSemicolonPlaceCheckbutton.CanFocus = true;
			this.smartSemicolonPlaceCheckbutton.Name = "smartSemicolonPlaceCheckbutton";
			this.smartSemicolonPlaceCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Smart semicolon placement");
			this.smartSemicolonPlaceCheckbutton.Active = true;
			this.smartSemicolonPlaceCheckbutton.DrawIndicator = true;
			this.smartSemicolonPlaceCheckbutton.UseUnderline = true;
			this.hbox2.Add(this.smartSemicolonPlaceCheckbutton);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.smartSemicolonPlaceCheckbutton]));
			w4.Position = 1;
			this.vbox4.Add(this.hbox2);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox2]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.checkbuttonOnTheFlyFormatting = new global::Gtk.CheckButton();
			this.checkbuttonOnTheFlyFormatting.CanFocus = true;
			this.checkbuttonOnTheFlyFormatting.Name = "checkbuttonOnTheFlyFormatting";
			this.checkbuttonOnTheFlyFormatting.Label = global::Mono.Unix.Catalog.GetString("_Enable on the fly code formatting");
			this.checkbuttonOnTheFlyFormatting.DrawIndicator = true;
			this.checkbuttonOnTheFlyFormatting.UseUnderline = true;
			this.vbox4.Add(this.checkbuttonOnTheFlyFormatting);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.checkbuttonOnTheFlyFormatting]));
			w6.Position = 2;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.checkbuttonFormatOnSave = new global::Gtk.CheckButton();
			this.checkbuttonFormatOnSave.CanFocus = true;
			this.checkbuttonFormatOnSave.Name = "checkbuttonFormatOnSave";
			this.checkbuttonFormatOnSave.Label = global::Mono.Unix.Catalog.GetString("_Format document on save");
			this.checkbuttonFormatOnSave.DrawIndicator = true;
			this.checkbuttonFormatOnSave.UseUnderline = true;
			this.vbox4.Add(this.checkbuttonFormatOnSave);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.checkbuttonFormatOnSave]));
			w7.Position = 3;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.checkbuttonAutoSetSearchPatternCasing = new global::Gtk.CheckButton();
			this.checkbuttonAutoSetSearchPatternCasing.CanFocus = true;
			this.checkbuttonAutoSetSearchPatternCasing.Name = "checkbuttonAutoSetSearchPatternCasing";
			this.checkbuttonAutoSetSearchPatternCasing.Label = global::Mono.Unix.Catalog.GetString("_Automatically set search pattern case sensitivity");
			this.checkbuttonAutoSetSearchPatternCasing.DrawIndicator = true;
			this.checkbuttonAutoSetSearchPatternCasing.UseUnderline = true;
			this.vbox4.Add(this.checkbuttonAutoSetSearchPatternCasing);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.checkbuttonAutoSetSearchPatternCasing]));
			w8.Position = 4;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.checkbuttonGenerateFormattingUndoStep = new global::Gtk.CheckButton();
			this.checkbuttonGenerateFormattingUndoStep.CanFocus = true;
			this.checkbuttonGenerateFormattingUndoStep.Name = "checkbuttonGenerateFormattingUndoStep";
			this.checkbuttonGenerateFormattingUndoStep.Label = global::Mono.Unix.Catalog.GetString("_Generate additional undo steps for formatting");
			this.checkbuttonGenerateFormattingUndoStep.DrawIndicator = true;
			this.checkbuttonGenerateFormattingUndoStep.UseUnderline = true;
			this.vbox4.Add(this.checkbuttonGenerateFormattingUndoStep);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.checkbuttonGenerateFormattingUndoStep]));
			w9.Position = 5;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.checkbuttonEnableSelectionSurrounding = new global::Gtk.CheckButton();
			this.checkbuttonEnableSelectionSurrounding.CanFocus = true;
			this.checkbuttonEnableSelectionSurrounding.Name = "checkbuttonEnableSelectionSurrounding";
			this.checkbuttonEnableSelectionSurrounding.Label = global::Mono.Unix.Catalog.GetString("Enable _selection surrounding keys");
			this.checkbuttonEnableSelectionSurrounding.DrawIndicator = true;
			this.checkbuttonEnableSelectionSurrounding.UseUnderline = true;
			this.vbox4.Add(this.checkbuttonEnableSelectionSurrounding);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.checkbuttonEnableSelectionSurrounding]));
			w10.Position = 6;
			w10.Expand = false;
			w10.Fill = false;
			this.alignment3.Add(this.vbox4);
			this.vbox1.Add(this.alignment3);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.alignment3]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkLabel8 = new global::Gtk.Label();
			this.GtkLabel8.Name = "GtkLabel8";
			this.GtkLabel8.Xalign = 0F;
			this.GtkLabel8.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Indentation</b>");
			this.GtkLabel8.UseMarkup = true;
			this.vbox1.Add(this.GtkLabel8);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkLabel8]));
			w13.Position = 2;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkAlignment = new global::Gtk.Alignment(0F, 0F, 1F, 1F);
			this.GtkAlignment.Name = "GtkAlignment";
			this.GtkAlignment.LeftPadding = ((uint)(12));
			// Container child GtkAlignment.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("_Indentation mode:");
			this.label1.UseUnderline = true;
			this.hbox1.Add(this.label1);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label1]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.indentationCombobox = global::Gtk.ComboBox.NewText();
			this.indentationCombobox.Name = "indentationCombobox";
			this.hbox1.Add(this.indentationCombobox);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.indentationCombobox]));
			w15.Position = 1;
			w15.Expand = false;
			w15.Fill = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w16.Position = 0;
			w16.Expand = false;
			w16.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.tabAsReindentCheckbutton = new global::Gtk.CheckButton();
			this.tabAsReindentCheckbutton.CanFocus = true;
			this.tabAsReindentCheckbutton.Name = "tabAsReindentCheckbutton";
			this.tabAsReindentCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Interpret tab _keystroke as reindent command");
			this.tabAsReindentCheckbutton.DrawIndicator = true;
			this.tabAsReindentCheckbutton.UseUnderline = true;
			this.vbox2.Add(this.tabAsReindentCheckbutton);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.tabAsReindentCheckbutton]));
			w17.Position = 1;
			w17.Expand = false;
			w17.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.smartBackspaceCheckbutton = new global::Gtk.CheckButton();
			this.smartBackspaceCheckbutton.CanFocus = true;
			this.smartBackspaceCheckbutton.Name = "smartBackspaceCheckbutton";
			this.smartBackspaceCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Backspace removes indentation");
			this.smartBackspaceCheckbutton.DrawIndicator = true;
			this.smartBackspaceCheckbutton.UseUnderline = true;
			this.vbox2.Add(this.smartBackspaceCheckbutton);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.smartBackspaceCheckbutton]));
			w18.Position = 2;
			w18.Expand = false;
			w18.Fill = false;
			this.GtkAlignment.Add(this.vbox2);
			this.vbox1.Add(this.GtkAlignment);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkAlignment]));
			w20.Position = 3;
			w20.Expand = false;
			w20.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkLabel10 = new global::Gtk.Label();
			this.GtkLabel10.Name = "GtkLabel10";
			this.GtkLabel10.Xalign = 0F;
			this.GtkLabel10.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Navigation</b>");
			this.GtkLabel10.UseMarkup = true;
			this.vbox1.Add(this.GtkLabel10);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkLabel10]));
			w21.Position = 4;
			w21.Expand = false;
			w21.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment4 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment4.Name = "alignment4";
			this.alignment4.LeftPadding = ((uint)(12));
			// Container child alignment4.Gtk.Container+ContainerChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Word _break mode:");
			this.label2.UseUnderline = true;
			this.hbox3.Add(this.label2);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.label2]));
			w22.Position = 0;
			w22.Expand = false;
			w22.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.controlLeftRightCombobox = global::Gtk.ComboBox.NewText();
			this.controlLeftRightCombobox.Name = "controlLeftRightCombobox";
			this.hbox3.Add(this.controlLeftRightCombobox);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.controlLeftRightCombobox]));
			w23.Position = 1;
			w23.Expand = false;
			w23.Fill = false;
			this.vbox5.Add(this.hbox3);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox3]));
			w24.Position = 0;
			w24.Expand = false;
			w24.Fill = false;
			this.alignment4.Add(this.vbox5);
			this.vbox1.Add(this.alignment4);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.alignment4]));
			w26.Position = 5;
			w26.Expand = false;
			w26.Fill = false;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
