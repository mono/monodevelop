#pragma warning disable 436

namespace MonoDevelop.RegexToolkit
{
	public partial class RegexToolkitWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.VPaned vpaned1;

		private global::Gtk.VBox vbox1;

		private global::Gtk.HBox hbox1;

		private global::Gtk.VBox vbox6;

		private global::Gtk.Label label8;

		private global::Gtk.Entry entryRegEx;

		private global::Gtk.VBox vbox3;

		private global::Gtk.CheckButton checkbuttonReplace;

		private global::Gtk.Entry entryReplace;

		private global::Gtk.HBox hbox7;

		private global::Gtk.CheckButton expandMatches;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Label label10;

		private global::Gtk.ScrolledWindow scrolledwindow5;

		private global::Gtk.TreeView optionsTreeview;

		private global::Gtk.VBox HelpWidget;

		private global::Gtk.Label label9;

		private global::Gtk.HBox hbox5;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TextView inputTextview;

		private global::Gtk.HBox hbox4;

		private global::Gtk.Notebook notebook2;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.TreeView resultsTreeview;

		private global::Gtk.Label label3;

		private global::Gtk.ScrolledWindow scrolledwindow4;

		private global::Gtk.TextView replaceResultTextview;

		private global::Gtk.Label label4;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.RegexToolkit.RegexToolkitWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.RegexToolkit.RegexToolkitWidget";
			// Container child MonoDevelop.RegexToolkit.RegexToolkitWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.vpaned1 = new global::Gtk.VPaned();
			this.vpaned1.CanFocus = true;
			this.vpaned1.Name = "vpaned1";
			this.vpaned1.Position = 359;
			this.vpaned1.BorderWidth = ((uint)(6));
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox6 = new global::Gtk.VBox();
			this.vbox6.Name = "vbox6";
			this.vbox6.Spacing = 6;
			this.vbox6.BorderWidth = ((uint)(6));
			// Container child vbox6.Gtk.Box+BoxChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("Regular Expression");
			this.label8.UseUnderline = true;
			this.vbox6.Add(this.label8);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.label8]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.entryRegEx = new global::Gtk.Entry();
			this.entryRegEx.CanFocus = true;
			this.entryRegEx.Name = "entryRegEx";
			this.entryRegEx.IsEditable = true;
			this.entryRegEx.InvisibleChar = '●';
			this.vbox6.Add(this.entryRegEx);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.entryRegEx]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkbuttonReplace = new global::Gtk.CheckButton();
			this.checkbuttonReplace.CanFocus = true;
			this.checkbuttonReplace.Name = "checkbuttonReplace";
			this.checkbuttonReplace.Label = global::Mono.Unix.Catalog.GetString("Replace");
			this.checkbuttonReplace.Active = true;
			this.checkbuttonReplace.DrawIndicator = true;
			this.checkbuttonReplace.UseUnderline = true;
			this.vbox3.Add(this.checkbuttonReplace);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.checkbuttonReplace]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.entryReplace = new global::Gtk.Entry();
			this.entryReplace.CanFocus = true;
			this.entryReplace.Name = "entryReplace";
			this.entryReplace.IsEditable = true;
			this.entryReplace.InvisibleChar = '●';
			this.vbox3.Add(this.entryReplace);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.entryReplace]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox7 = new global::Gtk.HBox();
			this.hbox7.Name = "hbox7";
			this.hbox7.Spacing = 6;
			// Container child hbox7.Gtk.Box+BoxChild
			this.expandMatches = new global::Gtk.CheckButton();
			this.expandMatches.CanFocus = true;
			this.expandMatches.Name = "expandMatches";
			this.expandMatches.Label = global::Mono.Unix.Catalog.GetString("Expand matches");
			this.expandMatches.Active = true;
			this.expandMatches.DrawIndicator = true;
			this.expandMatches.UseUnderline = true;
			this.expandMatches.BorderWidth = ((uint)(3));
			this.hbox7.Add(this.expandMatches);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox7[this.expandMatches]));
			w5.Position = 0;
			w5.Expand = false;
			this.vbox3.Add(this.hbox7);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox7]));
			w6.PackType = ((global::Gtk.PackType)(1));
			w6.Position = 2;
			w6.Expand = false;
			w6.Fill = false;
			this.vbox6.Add(this.vbox3);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.vbox3]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			this.hbox1.Add(this.vbox6);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox6]));
			w8.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label10 = new global::Gtk.Label();
			this.label10.Name = "label10";
			this.label10.Xalign = 0F;
			this.label10.LabelProp = global::Mono.Unix.Catalog.GetString("Options:");
			this.label10.UseMarkup = true;
			this.label10.UseUnderline = true;
			this.vbox4.Add(this.label10);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.label10]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.scrolledwindow5 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow5.CanFocus = true;
			this.scrolledwindow5.Name = "scrolledwindow5";
			this.scrolledwindow5.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow5.Gtk.Container+ContainerChild
			this.optionsTreeview = new global::Gtk.TreeView();
			this.optionsTreeview.CanFocus = true;
			this.optionsTreeview.Name = "optionsTreeview";
			this.scrolledwindow5.Add(this.optionsTreeview);
			this.vbox4.Add(this.scrolledwindow5);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.scrolledwindow5]));
			w11.Position = 1;
			this.hbox1.Add(this.vbox4);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox4]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			this.vbox1.Add(this.hbox1);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
			w13.Position = 0;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.HelpWidget = new global::Gtk.VBox();
			this.HelpWidget.Name = "HelpWidget";
			this.HelpWidget.Spacing = 6;
			this.HelpWidget.BorderWidth = ((uint)(6));
			// Container child HelpWidget.Gtk.Box+BoxChild
			this.label9 = new global::Gtk.Label();
			this.label9.Name = "label9";
			this.label9.Xalign = 0F;
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("Input:");
			this.label9.UseMarkup = true;
			this.label9.UseUnderline = true;
			this.HelpWidget.Add(this.label9);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.HelpWidget[this.label9]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			// Container child HelpWidget.Gtk.Box+BoxChild
			this.hbox5 = new global::Gtk.HBox();
			this.hbox5.Name = "hbox5";
			this.hbox5.Spacing = 6;
			// Container child hbox5.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.inputTextview = new global::Gtk.TextView();
			this.inputTextview.CanFocus = true;
			this.inputTextview.Name = "inputTextview";
			this.scrolledwindow1.Add(this.inputTextview);
			this.hbox5.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.scrolledwindow1]));
			w16.Position = 0;
			this.HelpWidget.Add(this.hbox5);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.HelpWidget[this.hbox5]));
			w17.Position = 1;
			this.vbox1.Add(this.HelpWidget);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.HelpWidget]));
			w18.Position = 1;
			this.vpaned1.Add(this.vbox1);
			global::Gtk.Paned.PanedChild w19 = ((global::Gtk.Paned.PanedChild)(this.vpaned1[this.vbox1]));
			w19.Resize = false;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			this.hbox4.BorderWidth = ((uint)(6));
			// Container child hbox4.Gtk.Box+BoxChild
			this.notebook2 = new global::Gtk.Notebook();
			this.notebook2.CanFocus = true;
			this.notebook2.Name = "notebook2";
			this.notebook2.CurrentPage = 0;
			// Container child notebook2.Gtk.Notebook+NotebookChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.resultsTreeview = new global::Gtk.TreeView();
			this.resultsTreeview.CanFocus = true;
			this.resultsTreeview.Name = "resultsTreeview";
			this.scrolledwindow2.Add(this.resultsTreeview);
			this.notebook2.Add(this.scrolledwindow2);
			// Notebook tab
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Matches");
			this.notebook2.SetTabLabel(this.scrolledwindow2, this.label3);
			this.label3.ShowAll();
			// Container child notebook2.Gtk.Notebook+NotebookChild
			this.scrolledwindow4 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow4.CanFocus = true;
			this.scrolledwindow4.Name = "scrolledwindow4";
			this.scrolledwindow4.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow4.Gtk.Container+ContainerChild
			this.replaceResultTextview = new global::Gtk.TextView();
			this.replaceResultTextview.CanFocus = true;
			this.replaceResultTextview.Name = "replaceResultTextview";
			this.replaceResultTextview.Editable = false;
			this.replaceResultTextview.CursorVisible = false;
			this.scrolledwindow4.Add(this.replaceResultTextview);
			this.notebook2.Add(this.scrolledwindow4);
			global::Gtk.Notebook.NotebookChild w23 = ((global::Gtk.Notebook.NotebookChild)(this.notebook2[this.scrolledwindow4]));
			w23.Position = 1;
			// Notebook tab
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Replace");
			this.notebook2.SetTabLabel(this.scrolledwindow4, this.label4);
			this.label4.ShowAll();
			this.hbox4.Add(this.notebook2);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.notebook2]));
			w24.Position = 0;
			this.vpaned1.Add(this.hbox4);
			this.vbox2.Add(this.vpaned1);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.vpaned1]));
			w26.Position = 0;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label8.MnemonicWidget = this.inputTextview;
			this.label10.MnemonicWidget = this.optionsTreeview;
			this.label9.MnemonicWidget = this.inputTextview;
			this.Hide();
		}
	}
}
#pragma warning restore 436
