
// This file has been generated by the GUI designer. Do not modify.
namespace MonoDevelop.FSharp.Gui
{
	public partial class FSharpSettingsWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label label4;

		private global::Gtk.CheckButton advanceToNextLineCheckbox;

		private global::Gtk.HSeparator hseparator4;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label2;

		private global::Gtk.CheckButton checkCompilerUseDefault;

		private global::Gtk.Frame frame1;

		private global::Gtk.Table table2;

		private global::Gtk.Button buttonCompilerBrowse;

		private global::Gtk.Entry entryCompilerPath;

		private global::Gtk.Label label3;

		private global::Gtk.HSeparator hseparator1;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Label label8;

		private global::Gtk.CheckButton checkHighlightMutables;

		private global::Gtk.CheckButton checkTypeSignatures;

		private global::Gtk.CheckButton checkStatusBarTooltips;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.FSharp.Gui.FSharpSettingsWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.FSharp.Gui.FSharpSettingsWidget";
			// Container child MonoDevelop.FSharp.Gui.FSharpSettingsWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			this.vbox1.BorderWidth = ((uint)(6));
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("<b>F# Interactive</b>");
			this.label4.UseMarkup = true;
			this.hbox2.Add(this.label4);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label4]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			this.vbox1.Add(this.hbox2);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox2]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.advanceToNextLineCheckbox = new global::Gtk.CheckButton();
			this.advanceToNextLineCheckbox.TooltipMarkup = "When sending a line or an empty selection to F# interactive this property automat" +
				"ically advances to the next line.";
			this.advanceToNextLineCheckbox.CanFocus = true;
			this.advanceToNextLineCheckbox.Name = "advanceToNextLineCheckbox";
			this.advanceToNextLineCheckbox.Label = global::Mono.Unix.Catalog.GetString("Advance to next line");
			this.advanceToNextLineCheckbox.Active = true;
			this.advanceToNextLineCheckbox.DrawIndicator = true;
			this.advanceToNextLineCheckbox.UseUnderline = true;
			this.vbox1.Add(this.advanceToNextLineCheckbox);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.advanceToNextLineCheckbox]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hseparator4 = new global::Gtk.HSeparator();
			this.hseparator4.Name = "hseparator4";
			this.vbox1.Add(this.hseparator4);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hseparator4]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.TooltipMarkup = "This is only used when xbuild is not being used.";
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("<b>F# Default Compiler</b>");
			this.label2.UseMarkup = true;
			this.hbox1.Add(this.label2);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label2]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.checkCompilerUseDefault = new global::Gtk.CheckButton();
			this.checkCompilerUseDefault.CanFocus = true;
			this.checkCompilerUseDefault.Name = "checkCompilerUseDefault";
			this.checkCompilerUseDefault.Label = global::Mono.Unix.Catalog.GetString("Use Default");
			this.checkCompilerUseDefault.Active = true;
			this.checkCompilerUseDefault.DrawIndicator = true;
			this.checkCompilerUseDefault.UseUnderline = true;
			this.hbox1.Add(this.checkCompilerUseDefault);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.checkCompilerUseDefault]));
			w6.Position = 1;
			this.vbox1.Add(this.hbox1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
			w7.Position = 3;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.frame1 = new global::Gtk.Frame();
			this.frame1.Name = "frame1";
			this.frame1.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child frame1.Gtk.Container+ContainerChild
			this.table2 = new global::Gtk.Table(((uint)(1)), ((uint)(3)), false);
			this.table2.Name = "table2";
			this.table2.RowSpacing = ((uint)(6));
			this.table2.ColumnSpacing = ((uint)(6));
			// Container child table2.Gtk.Table+TableChild
			this.buttonCompilerBrowse = new global::Gtk.Button();
			this.buttonCompilerBrowse.CanFocus = true;
			this.buttonCompilerBrowse.Name = "buttonCompilerBrowse";
			this.buttonCompilerBrowse.UseUnderline = true;
			this.buttonCompilerBrowse.Label = global::Mono.Unix.Catalog.GetString("_Browse...");
			this.table2.Add(this.buttonCompilerBrowse);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table2[this.buttonCompilerBrowse]));
			w8.LeftAttach = ((uint)(2));
			w8.RightAttach = ((uint)(3));
			w8.XPadding = ((uint)(8));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.entryCompilerPath = new global::Gtk.Entry();
			this.entryCompilerPath.CanFocus = true;
			this.entryCompilerPath.Name = "entryCompilerPath";
			this.entryCompilerPath.IsEditable = true;
			this.entryCompilerPath.InvisibleChar = '●';
			this.table2.Add(this.entryCompilerPath);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table2[this.entryCompilerPath]));
			w9.LeftAttach = ((uint)(1));
			w9.RightAttach = ((uint)(2));
			w9.XPadding = ((uint)(8));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Path");
			this.table2.Add(this.label3);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table2[this.label3]));
			w10.XPadding = ((uint)(8));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			this.frame1.Add(this.table2);
			this.vbox1.Add(this.frame1);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.frame1]));
			w12.Position = 4;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hseparator1 = new global::Gtk.HSeparator();
			this.hseparator1.Name = "hseparator1";
			this.vbox1.Add(this.hseparator1);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hseparator1]));
			w13.Position = 5;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("<b>F# Editor</b>");
			this.label8.UseMarkup = true;
			this.hbox3.Add(this.label8);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.label8]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			this.vbox1.Add(this.hbox3);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox3]));
			w15.Position = 6;
			w15.Expand = false;
			w15.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkHighlightMutables = new global::Gtk.CheckButton();
			this.checkHighlightMutables.CanFocus = true;
			this.checkHighlightMutables.Name = "checkHighlightMutables";
			this.checkHighlightMutables.Label = global::Mono.Unix.Catalog.GetString("Highlight mutable variables");
			this.checkHighlightMutables.Active = true;
			this.checkHighlightMutables.DrawIndicator = true;
			this.checkHighlightMutables.UseUnderline = true;
			this.vbox1.Add(this.checkHighlightMutables);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkHighlightMutables]));
			w16.Position = 7;
			w16.Expand = false;
			w16.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkTypeSignatures = new global::Gtk.CheckButton();
			this.checkTypeSignatures.CanFocus = true;
			this.checkTypeSignatures.Name = "checkTypeSignatures";
			this.checkTypeSignatures.Label = global::Mono.Unix.Catalog.GetString("Show function type signatures");
			this.checkTypeSignatures.DrawIndicator = true;
			this.checkTypeSignatures.UseUnderline = true;
			this.vbox1.Add(this.checkTypeSignatures);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkTypeSignatures]));
			w17.Position = 8;
			w17.Expand = false;
			w17.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.checkStatusBarTooltips = new global::Gtk.CheckButton();
			this.checkStatusBarTooltips.CanFocus = true;
			this.checkStatusBarTooltips.Name = "checkStatusBarTooltips";
			this.checkStatusBarTooltips.Label = global::Mono.Unix.Catalog.GetString("Show status bar tooltips");
			this.checkStatusBarTooltips.Active = true;
			this.checkStatusBarTooltips.DrawIndicator = true;
			this.checkStatusBarTooltips.UseUnderline = true;
			this.vbox1.Add(this.checkStatusBarTooltips);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.checkStatusBarTooltips]));
			w18.Position = 9;
			w18.Expand = false;
			w18.Fill = false;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
