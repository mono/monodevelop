#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal partial class TasksPanelWidget
	{
		private global::Gtk.VBox vbox6;

		private global::Gtk.HBox hbox2;

		private global::Gtk.VBox vbox7;

		private global::Gtk.Label labelTokens;

		private global::Gtk.ScrolledWindow scrolledwindow3;

		private global::Gtk.TreeView tokensTreeView;

		private global::Gtk.VBox vbox14;

		private global::Gtk.VBox vboxPriority;

		private global::Gtk.Label label112;

		private global::Gtk.Entry entryToken;

		private global::Gtk.Label label113;

		private global::Gtk.HButtonBox hbuttonbox2;

		private global::Gtk.Button buttonChange;

		private global::Gtk.Button buttonRemove;

		private global::Gtk.Button buttonAdd;

		private global::Gtk.Label label;

		private global::Gtk.HSeparator hseparator2;

		private global::Gtk.Frame frame1;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.Table table6;

		private global::Gtk.ColorButton colorbuttonHighPrio;

		private global::Gtk.ColorButton colorbuttonLowPrio;

		private global::Gtk.ColorButton colorbuttonNormalPrio;

		private global::Gtk.Label label10;

		private global::Gtk.Label label11;

		private global::Gtk.Label label12;

		private global::Gtk.Label label9;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.OptionPanels.TasksPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.OptionPanels.TasksPanelWidget";
			// Container child MonoDevelop.Ide.Gui.OptionPanels.TasksPanelWidget.Gtk.Container+ContainerChild
			this.vbox6 = new global::Gtk.VBox();
			this.vbox6.Name = "vbox6";
			this.vbox6.Spacing = 12;
			// Container child vbox6.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 8;
			// Container child hbox2.Gtk.Box+BoxChild
			this.vbox7 = new global::Gtk.VBox();
			this.vbox7.Name = "vbox7";
			// Container child vbox7.Gtk.Box+BoxChild
			this.labelTokens = new global::Gtk.Label();
			this.labelTokens.Name = "labelTokens";
			this.labelTokens.Xalign = 0F;
			this.labelTokens.Yalign = 0F;
			this.labelTokens.LabelProp = global::Mono.Unix.Catalog.GetString("_Token List:");
			this.labelTokens.UseUnderline = true;
			this.vbox7.Add(this.labelTokens);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.labelTokens]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox7.Gtk.Box+BoxChild
			this.scrolledwindow3 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow3.WidthRequest = 200;
			this.scrolledwindow3.Name = "scrolledwindow3";
			this.scrolledwindow3.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow3.Gtk.Container+ContainerChild
			this.tokensTreeView = new global::Gtk.TreeView();
			this.tokensTreeView.Name = "tokensTreeView";
			this.tokensTreeView.HeadersVisible = false;
			this.scrolledwindow3.Add(this.tokensTreeView);
			this.vbox7.Add(this.scrolledwindow3);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.scrolledwindow3]));
			w3.Position = 1;
			this.hbox2.Add(this.vbox7);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.vbox7]));
			w4.Position = 0;
			// Container child hbox2.Gtk.Box+BoxChild
			this.vbox14 = new global::Gtk.VBox();
			this.vbox14.Name = "vbox14";
			this.vbox14.Spacing = 4;
			// Container child vbox14.Gtk.Box+BoxChild
			this.vboxPriority = new global::Gtk.VBox();
			this.vboxPriority.Name = "vboxPriority";
			this.vboxPriority.Spacing = 4;
			// Container child vboxPriority.Gtk.Box+BoxChild
			this.label112 = new global::Gtk.Label();
			this.label112.Name = "label112";
			this.label112.Xalign = 0F;
			this.label112.LabelProp = global::Mono.Unix.Catalog.GetString("_Name:");
			this.label112.UseUnderline = true;
			this.vboxPriority.Add(this.label112);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vboxPriority[this.label112]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vboxPriority.Gtk.Box+BoxChild
			this.entryToken = new global::Gtk.Entry();
			this.entryToken.Name = "entryToken";
			this.entryToken.IsEditable = true;
			this.entryToken.MaxLength = 50;
			this.entryToken.InvisibleChar = '●';
			this.vboxPriority.Add(this.entryToken);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vboxPriority[this.entryToken]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vboxPriority.Gtk.Box+BoxChild
			this.label113 = new global::Gtk.Label();
			this.label113.Name = "label113";
			this.label113.Xalign = 0F;
			this.label113.LabelProp = global::Mono.Unix.Catalog.GetString("Priority:");
			this.vboxPriority.Add(this.label113);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vboxPriority[this.label113]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			this.vbox14.Add(this.vboxPriority);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox14[this.vboxPriority]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox14.Gtk.Box+BoxChild
			this.hbuttonbox2 = new global::Gtk.HButtonBox();
			this.hbuttonbox2.Name = "hbuttonbox2";
			this.hbuttonbox2.Spacing = 6;
			this.hbuttonbox2.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child hbuttonbox2.Gtk.ButtonBox+ButtonBoxChild
			this.buttonChange = new global::Gtk.Button();
			this.buttonChange.Name = "buttonChange";
			this.buttonChange.UseStock = true;
			this.buttonChange.UseUnderline = true;
			this.buttonChange.Label = "gtk-edit";
			this.hbuttonbox2.Add(this.buttonChange);
			global::Gtk.ButtonBox.ButtonBoxChild w9 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.hbuttonbox2[this.buttonChange]));
			w9.Expand = false;
			w9.Fill = false;
			// Container child hbuttonbox2.Gtk.ButtonBox+ButtonBoxChild
			this.buttonRemove = new global::Gtk.Button();
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseStock = true;
			this.buttonRemove.UseUnderline = true;
			this.buttonRemove.Label = "gtk-remove";
			this.hbuttonbox2.Add(this.buttonRemove);
			global::Gtk.ButtonBox.ButtonBoxChild w10 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.hbuttonbox2[this.buttonRemove]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			// Container child hbuttonbox2.Gtk.ButtonBox+ButtonBoxChild
			this.buttonAdd = new global::Gtk.Button();
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.UseStock = true;
			this.buttonAdd.UseUnderline = true;
			this.buttonAdd.Label = "gtk-add";
			this.hbuttonbox2.Add(this.buttonAdd);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.hbuttonbox2[this.buttonAdd]));
			w11.Position = 2;
			w11.Expand = false;
			w11.Fill = false;
			this.vbox14.Add(this.hbuttonbox2);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox14[this.hbuttonbox2]));
			w12.Position = 1;
			w12.Expand = false;
			// Container child vbox14.Gtk.Box+BoxChild
			this.label = new global::Gtk.Label();
			this.label.Name = "label";
			this.label.Ypad = 12;
			this.label.Yalign = 0F;
			this.label.LabelProp = global::Mono.Unix.Catalog.GetString("<i><b>Note:</b> Only Letters, Digits and Underscore are allowed.</i>");
			this.label.UseMarkup = true;
			this.label.Wrap = true;
			this.label.Justify = ((global::Gtk.Justification)(2));
			this.vbox14.Add(this.label);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox14[this.label]));
			w13.Position = 2;
			w13.Expand = false;
			w13.Fill = false;
			this.hbox2.Add(this.vbox14);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.vbox14]));
			w14.Position = 1;
			this.vbox6.Add(this.hbox2);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.hbox2]));
			w15.Position = 0;
			// Container child vbox6.Gtk.Box+BoxChild
			this.hseparator2 = new global::Gtk.HSeparator();
			this.hseparator2.Name = "hseparator2";
			this.vbox6.Add(this.hseparator2);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.hseparator2]));
			w16.Position = 1;
			w16.Expand = false;
			// Container child vbox6.Gtk.Box+BoxChild
			this.frame1 = new global::Gtk.Frame();
			this.frame1.Name = "frame1";
			this.frame1.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child frame1.Gtk.Container+ContainerChild
			this.alignment1 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.LeftPadding = ((uint)(12));
			this.alignment1.TopPadding = ((uint)(4));
			// Container child alignment1.Gtk.Container+ContainerChild
			this.table6 = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table6.Name = "table6";
			this.table6.RowSpacing = ((uint)(4));
			this.table6.ColumnSpacing = ((uint)(6));
			// Container child table6.Gtk.Table+TableChild
			this.colorbuttonHighPrio = new global::Gtk.ColorButton();
			this.colorbuttonHighPrio.Name = "colorbuttonHighPrio";
			this.table6.Add(this.colorbuttonHighPrio);
			global::Gtk.Table.TableChild w17 = ((global::Gtk.Table.TableChild)(this.table6[this.colorbuttonHighPrio]));
			w17.LeftAttach = ((uint)(1));
			w17.RightAttach = ((uint)(2));
			w17.XOptions = ((global::Gtk.AttachOptions)(0));
			w17.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table6.Gtk.Table+TableChild
			this.colorbuttonLowPrio = new global::Gtk.ColorButton();
			this.colorbuttonLowPrio.Name = "colorbuttonLowPrio";
			this.table6.Add(this.colorbuttonLowPrio);
			global::Gtk.Table.TableChild w18 = ((global::Gtk.Table.TableChild)(this.table6[this.colorbuttonLowPrio]));
			w18.TopAttach = ((uint)(2));
			w18.BottomAttach = ((uint)(3));
			w18.LeftAttach = ((uint)(1));
			w18.RightAttach = ((uint)(2));
			w18.XOptions = ((global::Gtk.AttachOptions)(0));
			w18.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table6.Gtk.Table+TableChild
			this.colorbuttonNormalPrio = new global::Gtk.ColorButton();
			this.colorbuttonNormalPrio.Name = "colorbuttonNormalPrio";
			this.table6.Add(this.colorbuttonNormalPrio);
			global::Gtk.Table.TableChild w19 = ((global::Gtk.Table.TableChild)(this.table6[this.colorbuttonNormalPrio]));
			w19.TopAttach = ((uint)(1));
			w19.BottomAttach = ((uint)(2));
			w19.LeftAttach = ((uint)(1));
			w19.RightAttach = ((uint)(2));
			w19.XOptions = ((global::Gtk.AttachOptions)(0));
			w19.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table6.Gtk.Table+TableChild
			this.label10 = new global::Gtk.Label();
			this.label10.Name = "label10";
			this.label10.Xalign = 1F;
			this.label10.LabelProp = global::Mono.Unix.Catalog.GetString("High");
			this.table6.Add(this.label10);
			global::Gtk.Table.TableChild w20 = ((global::Gtk.Table.TableChild)(this.table6[this.label10]));
			w20.XOptions = ((global::Gtk.AttachOptions)(0));
			w20.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table6.Gtk.Table+TableChild
			this.label11 = new global::Gtk.Label();
			this.label11.Name = "label11";
			this.label11.Xalign = 1F;
			this.label11.LabelProp = global::Mono.Unix.Catalog.GetString("Normal");
			this.table6.Add(this.label11);
			global::Gtk.Table.TableChild w21 = ((global::Gtk.Table.TableChild)(this.table6[this.label11]));
			w21.TopAttach = ((uint)(1));
			w21.BottomAttach = ((uint)(2));
			w21.XOptions = ((global::Gtk.AttachOptions)(0));
			w21.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table6.Gtk.Table+TableChild
			this.label12 = new global::Gtk.Label();
			this.label12.Name = "label12";
			this.label12.Xalign = 1F;
			this.label12.LabelProp = global::Mono.Unix.Catalog.GetString("Low");
			this.label12.Justify = ((global::Gtk.Justification)(2));
			this.table6.Add(this.label12);
			global::Gtk.Table.TableChild w22 = ((global::Gtk.Table.TableChild)(this.table6[this.label12]));
			w22.TopAttach = ((uint)(2));
			w22.BottomAttach = ((uint)(3));
			w22.XOptions = ((global::Gtk.AttachOptions)(0));
			w22.YOptions = ((global::Gtk.AttachOptions)(0));
			this.alignment1.Add(this.table6);
			this.frame1.Add(this.alignment1);
			this.label9 = new global::Gtk.Label();
			this.label9.Name = "label9";
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Task Priorities Foreground Colors</b>");
			this.label9.UseMarkup = true;
			this.frame1.LabelWidget = this.label9;
			this.vbox6.Add(this.frame1);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.frame1]));
			w25.PackType = ((global::Gtk.PackType)(1));
			w25.Position = 2;
			w25.Expand = false;
			w25.Fill = false;
			this.Add(this.vbox6);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label112.MnemonicWidget = this.entryToken;
			this.Show();
		}
	}
}
#pragma warning restore 436
