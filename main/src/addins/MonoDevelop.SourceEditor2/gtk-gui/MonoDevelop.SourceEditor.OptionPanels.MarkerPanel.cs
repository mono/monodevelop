#pragma warning disable 436

namespace MonoDevelop.SourceEditor.OptionPanels
{
	internal partial class MarkerPanel
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Label GtkLabel9;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.CheckButton showLineNumbersCheckbutton;

		private global::Gtk.CheckButton highlightMatchingBracketCheckbutton;

		private global::Gtk.CheckButton highlightCurrentLineCheckbutton;

		private global::Gtk.CheckButton showRulerCheckbutton;

		private global::Gtk.CheckButton enableAnimationCheckbutton1;

		private global::Gtk.CheckButton enableHighlightUsagesCheckbutton;

		private global::Gtk.CheckButton drawIndentMarkersCheckbutton;

		private global::Gtk.CheckButton enableQuickDiffCheckbutton;

		private global::Gtk.Table table1;

		private global::Gtk.CheckButton checkbuttonLineEndings;

		private global::Gtk.CheckButton checkbuttonSpaces;

		private global::Gtk.CheckButton checkbuttonTabs;

		private global::Gtk.Label label1;

		private global::Gtk.ComboBox showWhitespacesCombobox;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.SourceEditor.OptionPanels.MarkerPanel
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.SourceEditor.OptionPanels.MarkerPanel";
			// Container child MonoDevelop.SourceEditor.OptionPanels.MarkerPanel.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.GtkLabel9 = new global::Gtk.Label();
			this.GtkLabel9.Name = "GtkLabel9";
			this.GtkLabel9.Xalign = 0F;
			this.GtkLabel9.LabelProp = global::Mono.Unix.Catalog.GetString("<b>General</b>");
			this.GtkLabel9.UseMarkup = true;
			this.vbox1.Add(this.GtkLabel9);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.GtkLabel9]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.LeftPadding = ((uint)(12));
			// Container child alignment1.Gtk.Container+ContainerChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.showLineNumbersCheckbutton = new global::Gtk.CheckButton();
			this.showLineNumbersCheckbutton.CanFocus = true;
			this.showLineNumbersCheckbutton.Name = "showLineNumbersCheckbutton";
			this.showLineNumbersCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Show line numbers");
			this.showLineNumbersCheckbutton.DrawIndicator = true;
			this.showLineNumbersCheckbutton.UseUnderline = true;
			this.vbox3.Add(this.showLineNumbersCheckbutton);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.showLineNumbersCheckbutton]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.highlightMatchingBracketCheckbutton = new global::Gtk.CheckButton();
			this.highlightMatchingBracketCheckbutton.CanFocus = true;
			this.highlightMatchingBracketCheckbutton.Name = "highlightMatchingBracketCheckbutton";
			this.highlightMatchingBracketCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Highlight matching braces");
			this.highlightMatchingBracketCheckbutton.DrawIndicator = true;
			this.highlightMatchingBracketCheckbutton.UseUnderline = true;
			this.vbox3.Add(this.highlightMatchingBracketCheckbutton);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.highlightMatchingBracketCheckbutton]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.highlightCurrentLineCheckbutton = new global::Gtk.CheckButton();
			this.highlightCurrentLineCheckbutton.CanFocus = true;
			this.highlightCurrentLineCheckbutton.Name = "highlightCurrentLineCheckbutton";
			this.highlightCurrentLineCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Highlight _current line");
			this.highlightCurrentLineCheckbutton.DrawIndicator = true;
			this.highlightCurrentLineCheckbutton.UseUnderline = true;
			this.vbox3.Add(this.highlightCurrentLineCheckbutton);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.highlightCurrentLineCheckbutton]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.showRulerCheckbutton = new global::Gtk.CheckButton();
			this.showRulerCheckbutton.CanFocus = true;
			this.showRulerCheckbutton.Name = "showRulerCheckbutton";
			this.showRulerCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Show _column ruler");
			this.showRulerCheckbutton.DrawIndicator = true;
			this.showRulerCheckbutton.UseUnderline = true;
			this.vbox3.Add(this.showRulerCheckbutton);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.showRulerCheckbutton]));
			w5.Position = 3;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.enableAnimationCheckbutton1 = new global::Gtk.CheckButton();
			this.enableAnimationCheckbutton1.CanFocus = true;
			this.enableAnimationCheckbutton1.Name = "enableAnimationCheckbutton1";
			this.enableAnimationCheckbutton1.Label = global::Mono.Unix.Catalog.GetString("_Enable animations");
			this.enableAnimationCheckbutton1.DrawIndicator = true;
			this.enableAnimationCheckbutton1.UseUnderline = true;
			this.vbox3.Add(this.enableAnimationCheckbutton1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.enableAnimationCheckbutton1]));
			w6.Position = 4;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.enableHighlightUsagesCheckbutton = new global::Gtk.CheckButton();
			this.enableHighlightUsagesCheckbutton.CanFocus = true;
			this.enableHighlightUsagesCheckbutton.Name = "enableHighlightUsagesCheckbutton";
			this.enableHighlightUsagesCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Highlight _identifier references");
			this.enableHighlightUsagesCheckbutton.DrawIndicator = true;
			this.enableHighlightUsagesCheckbutton.UseUnderline = true;
			this.vbox3.Add(this.enableHighlightUsagesCheckbutton);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.enableHighlightUsagesCheckbutton]));
			w7.Position = 5;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.drawIndentMarkersCheckbutton = new global::Gtk.CheckButton();
			this.drawIndentMarkersCheckbutton.CanFocus = true;
			this.drawIndentMarkersCheckbutton.Name = "drawIndentMarkersCheckbutton";
			this.drawIndentMarkersCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Show indentation guides");
			this.drawIndentMarkersCheckbutton.DrawIndicator = true;
			this.drawIndentMarkersCheckbutton.UseUnderline = true;
			this.vbox3.Add(this.drawIndentMarkersCheckbutton);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.drawIndentMarkersCheckbutton]));
			w8.Position = 6;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.enableQuickDiffCheckbutton = new global::Gtk.CheckButton();
			this.enableQuickDiffCheckbutton.CanFocus = true;
			this.enableQuickDiffCheckbutton.Name = "enableQuickDiffCheckbutton";
			this.enableQuickDiffCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Visualize changed lines");
			this.enableQuickDiffCheckbutton.DrawIndicator = true;
			this.enableQuickDiffCheckbutton.UseUnderline = true;
			this.vbox3.Add(this.enableQuickDiffCheckbutton);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.enableQuickDiffCheckbutton]));
			w9.Position = 7;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(4)), ((uint)(4)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.checkbuttonLineEndings = new global::Gtk.CheckButton();
			this.checkbuttonLineEndings.CanFocus = true;
			this.checkbuttonLineEndings.Name = "checkbuttonLineEndings";
			this.checkbuttonLineEndings.Label = global::Mono.Unix.Catalog.GetString("Include Line Endings");
			this.checkbuttonLineEndings.DrawIndicator = true;
			this.checkbuttonLineEndings.UseUnderline = true;
			this.table1.Add(this.checkbuttonLineEndings);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table1[this.checkbuttonLineEndings]));
			w10.TopAttach = ((uint)(3));
			w10.BottomAttach = ((uint)(4));
			w10.LeftAttach = ((uint)(1));
			w10.RightAttach = ((uint)(4));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.checkbuttonSpaces = new global::Gtk.CheckButton();
			this.checkbuttonSpaces.CanFocus = true;
			this.checkbuttonSpaces.Name = "checkbuttonSpaces";
			this.checkbuttonSpaces.Label = global::Mono.Unix.Catalog.GetString("Include _Spaces");
			this.checkbuttonSpaces.DrawIndicator = true;
			this.checkbuttonSpaces.UseUnderline = true;
			this.table1.Add(this.checkbuttonSpaces);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table1[this.checkbuttonSpaces]));
			w11.TopAttach = ((uint)(1));
			w11.BottomAttach = ((uint)(2));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(4));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.checkbuttonTabs = new global::Gtk.CheckButton();
			this.checkbuttonTabs.CanFocus = true;
			this.checkbuttonTabs.Name = "checkbuttonTabs";
			this.checkbuttonTabs.Label = global::Mono.Unix.Catalog.GetString("Include Tabs");
			this.checkbuttonTabs.DrawIndicator = true;
			this.checkbuttonTabs.UseUnderline = true;
			this.table1.Add(this.checkbuttonTabs);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.table1[this.checkbuttonTabs]));
			w12.TopAttach = ((uint)(2));
			w12.BottomAttach = ((uint)(3));
			w12.LeftAttach = ((uint)(1));
			w12.RightAttach = ((uint)(4));
			w12.XOptions = ((global::Gtk.AttachOptions)(4));
			w12.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("_Show invisible characters:");
			this.label1.UseUnderline = true;
			this.table1.Add(this.label1);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table1[this.label1]));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.showWhitespacesCombobox = global::Gtk.ComboBox.NewText();
			this.showWhitespacesCombobox.Name = "showWhitespacesCombobox";
			this.table1.Add(this.showWhitespacesCombobox);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.table1[this.showWhitespacesCombobox]));
			w14.LeftAttach = ((uint)(1));
			w14.RightAttach = ((uint)(4));
			w14.XOptions = ((global::Gtk.AttachOptions)(4));
			w14.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox3.Add(this.table1);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.table1]));
			w15.Position = 8;
			w15.Fill = false;
			this.alignment1.Add(this.vbox3);
			this.vbox1.Add(this.alignment1);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.alignment1]));
			w17.Position = 1;
			w17.Expand = false;
			w17.Fill = false;
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
