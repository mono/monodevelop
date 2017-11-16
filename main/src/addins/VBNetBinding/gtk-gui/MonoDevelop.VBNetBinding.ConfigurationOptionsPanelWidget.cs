#pragma warning disable 436

namespace MonoDevelop.VBNetBinding
{
	public partial class ConfigurationOptionsPanelWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Label label82;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label76;

		private global::Gtk.Table table1;

		private global::Gtk.ComboBox cmbDebugType;

		private global::Gtk.ComboBox cmbDefineDEBUG;

		private global::Gtk.ComboBox cmbDefineTRACE;

		private global::Gtk.ComboBox cmbOptimize;

		private global::Gtk.Label label86;

		private global::Gtk.Label label87;

		private global::Gtk.Label label88;

		private global::Gtk.Label label94;

		private global::Gtk.Label label83;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label label77;

		private global::Gtk.Table table3;

		private global::Gtk.ComboBox cmbEnableWarnings;

		private global::Gtk.Label label91;

		private global::Gtk.Label label92;

		private global::Gtk.Label label93;

		private global::Gtk.Entry txtDontWarnAbout;

		private global::Gtk.Entry txtTreatAsError;

		private global::Gtk.Label label85;

		private global::Gtk.HBox hbox4;

		private global::Gtk.Label label79;

		private global::Gtk.Table table2;

		private global::Gtk.ComboBox cmbGenerateXmlDocumentation;

		private global::Gtk.ComboBox cmbRemoveIntegerChecks;

		private global::Gtk.Label label89;

		private global::Gtk.Label label90;

		private global::Gtk.Label label95;

		private global::Gtk.Label label96;

		private global::Gtk.Entry txtAdditionalArguments;

		private global::Gtk.Entry txtDefineConstants;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VBNetBinding.ConfigurationOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.VBNetBinding.ConfigurationOptionsPanelWidget";
			// Container child MonoDevelop.VBNetBinding.ConfigurationOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label82 = new global::Gtk.Label();
			this.label82.Name = "label82";
			this.label82.Xalign = 0F;
			this.label82.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Optimization/Debug options</b>");
			this.label82.UseMarkup = true;
			this.vbox1.Add(this.label82);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label82]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label76 = new global::Gtk.Label();
			this.label76.WidthRequest = 18;
			this.label76.Name = "label76";
			this.hbox1.Add(this.label76);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label76]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(4)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.cmbDebugType = new global::Gtk.ComboBox();
			this.cmbDebugType.Name = "cmbDebugType";
			this.table1.Add(this.cmbDebugType);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbDebugType]));
			w3.TopAttach = ((uint)(3));
			w3.BottomAttach = ((uint)(4));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cmbDefineDEBUG = new global::Gtk.ComboBox();
			this.cmbDefineDEBUG.Name = "cmbDefineDEBUG";
			this.table1.Add(this.cmbDefineDEBUG);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbDefineDEBUG]));
			w4.TopAttach = ((uint)(1));
			w4.BottomAttach = ((uint)(2));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(2));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cmbDefineTRACE = new global::Gtk.ComboBox();
			this.cmbDefineTRACE.Name = "cmbDefineTRACE";
			this.table1.Add(this.cmbDefineTRACE);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbDefineTRACE]));
			w5.TopAttach = ((uint)(2));
			w5.BottomAttach = ((uint)(3));
			w5.LeftAttach = ((uint)(1));
			w5.RightAttach = ((uint)(2));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cmbOptimize = new global::Gtk.ComboBox();
			this.cmbOptimize.Name = "cmbOptimize";
			this.table1.Add(this.cmbOptimize);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbOptimize]));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label86 = new global::Gtk.Label();
			this.label86.Name = "label86";
			this.label86.Xalign = 0F;
			this.label86.LabelProp = global::Mono.Unix.Catalog.GetString("Debug Type:");
			this.label86.UseUnderline = true;
			this.table1.Add(this.label86);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table1[this.label86]));
			w7.TopAttach = ((uint)(3));
			w7.BottomAttach = ((uint)(4));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label87 = new global::Gtk.Label();
			this.label87.Name = "label87";
			this.label87.Xalign = 0F;
			this.label87.LabelProp = global::Mono.Unix.Catalog.GetString("Define DEBUG:");
			this.label87.UseUnderline = true;
			this.table1.Add(this.label87);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table1[this.label87]));
			w8.TopAttach = ((uint)(1));
			w8.BottomAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label88 = new global::Gtk.Label();
			this.label88.Name = "label88";
			this.label88.Xalign = 0F;
			this.label88.LabelProp = global::Mono.Unix.Catalog.GetString("Define TRACE:");
			this.label88.UseUnderline = true;
			this.table1.Add(this.label88);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table1[this.label88]));
			w9.TopAttach = ((uint)(2));
			w9.BottomAttach = ((uint)(3));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label94 = new global::Gtk.Label();
			this.label94.Name = "label94";
			this.label94.Xalign = 0F;
			this.label94.LabelProp = global::Mono.Unix.Catalog.GetString("Optimize:");
			this.label94.UseUnderline = true;
			this.table1.Add(this.label94);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table1[this.label94]));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			this.hbox1.Add(this.table1);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.table1]));
			w11.Position = 1;
			w11.Expand = false;
			w11.Fill = false;
			this.vbox1.Add(this.hbox1);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label83 = new global::Gtk.Label();
			this.label83.Name = "label83";
			this.label83.Xalign = 0F;
			this.label83.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Warnings</b>");
			this.label83.UseMarkup = true;
			this.vbox1.Add(this.label83);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label83]));
			w13.Position = 2;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label77 = new global::Gtk.Label();
			this.label77.WidthRequest = 18;
			this.label77.Name = "label77";
			this.hbox2.Add(this.label77);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label77]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.table3 = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table3.Name = "table3";
			this.table3.RowSpacing = ((uint)(6));
			this.table3.ColumnSpacing = ((uint)(6));
			// Container child table3.Gtk.Table+TableChild
			this.cmbEnableWarnings = new global::Gtk.ComboBox();
			this.cmbEnableWarnings.Name = "cmbEnableWarnings";
			this.table3.Add(this.cmbEnableWarnings);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.table3[this.cmbEnableWarnings]));
			w15.LeftAttach = ((uint)(1));
			w15.RightAttach = ((uint)(2));
			w15.XOptions = ((global::Gtk.AttachOptions)(4));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.label91 = new global::Gtk.Label();
			this.label91.Name = "label91";
			this.label91.Xalign = 0F;
			this.label91.LabelProp = global::Mono.Unix.Catalog.GetString("Treat as error:");
			this.label91.UseUnderline = true;
			this.table3.Add(this.label91);
			global::Gtk.Table.TableChild w16 = ((global::Gtk.Table.TableChild)(this.table3[this.label91]));
			w16.TopAttach = ((uint)(2));
			w16.BottomAttach = ((uint)(3));
			w16.XOptions = ((global::Gtk.AttachOptions)(4));
			w16.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.label92 = new global::Gtk.Label();
			this.label92.Name = "label92";
			this.label92.Xalign = 0F;
			this.label92.LabelProp = global::Mono.Unix.Catalog.GetString("Enable Warnings:");
			this.label92.UseUnderline = true;
			this.table3.Add(this.label92);
			global::Gtk.Table.TableChild w17 = ((global::Gtk.Table.TableChild)(this.table3[this.label92]));
			w17.XOptions = ((global::Gtk.AttachOptions)(4));
			w17.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.label93 = new global::Gtk.Label();
			this.label93.Name = "label93";
			this.label93.Xalign = 0F;
			this.label93.LabelProp = global::Mono.Unix.Catalog.GetString("Don\'t warn about:");
			this.label93.UseUnderline = true;
			this.table3.Add(this.label93);
			global::Gtk.Table.TableChild w18 = ((global::Gtk.Table.TableChild)(this.table3[this.label93]));
			w18.TopAttach = ((uint)(1));
			w18.BottomAttach = ((uint)(2));
			w18.XOptions = ((global::Gtk.AttachOptions)(4));
			w18.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.txtDontWarnAbout = new global::Gtk.Entry();
			this.txtDontWarnAbout.CanFocus = true;
			this.txtDontWarnAbout.Name = "txtDontWarnAbout";
			this.txtDontWarnAbout.IsEditable = true;
			this.txtDontWarnAbout.InvisibleChar = '●';
			this.table3.Add(this.txtDontWarnAbout);
			global::Gtk.Table.TableChild w19 = ((global::Gtk.Table.TableChild)(this.table3[this.txtDontWarnAbout]));
			w19.TopAttach = ((uint)(1));
			w19.BottomAttach = ((uint)(2));
			w19.LeftAttach = ((uint)(1));
			w19.RightAttach = ((uint)(2));
			w19.XOptions = ((global::Gtk.AttachOptions)(4));
			w19.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.txtTreatAsError = new global::Gtk.Entry();
			this.txtTreatAsError.CanFocus = true;
			this.txtTreatAsError.Name = "txtTreatAsError";
			this.txtTreatAsError.IsEditable = true;
			this.txtTreatAsError.InvisibleChar = '●';
			this.table3.Add(this.txtTreatAsError);
			global::Gtk.Table.TableChild w20 = ((global::Gtk.Table.TableChild)(this.table3[this.txtTreatAsError]));
			w20.TopAttach = ((uint)(2));
			w20.BottomAttach = ((uint)(3));
			w20.LeftAttach = ((uint)(1));
			w20.RightAttach = ((uint)(2));
			w20.XOptions = ((global::Gtk.AttachOptions)(4));
			w20.YOptions = ((global::Gtk.AttachOptions)(4));
			this.hbox2.Add(this.table3);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.table3]));
			w21.Position = 1;
			w21.Expand = false;
			w21.Fill = false;
			this.vbox1.Add(this.hbox2);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox2]));
			w22.Position = 3;
			w22.Expand = false;
			w22.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label85 = new global::Gtk.Label();
			this.label85.Name = "label85";
			this.label85.Xalign = 0F;
			this.label85.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Misc</b>");
			this.label85.UseMarkup = true;
			this.vbox1.Add(this.label85);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label85]));
			w23.Position = 4;
			w23.Expand = false;
			w23.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			// Container child hbox4.Gtk.Box+BoxChild
			this.label79 = new global::Gtk.Label();
			this.label79.WidthRequest = 18;
			this.label79.Name = "label79";
			this.hbox4.Add(this.label79);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.label79]));
			w24.Position = 0;
			w24.Expand = false;
			w24.Fill = false;
			// Container child hbox4.Gtk.Box+BoxChild
			this.table2 = new global::Gtk.Table(((uint)(4)), ((uint)(2)), false);
			this.table2.Name = "table2";
			this.table2.RowSpacing = ((uint)(6));
			this.table2.ColumnSpacing = ((uint)(6));
			// Container child table2.Gtk.Table+TableChild
			this.cmbGenerateXmlDocumentation = new global::Gtk.ComboBox();
			this.cmbGenerateXmlDocumentation.Name = "cmbGenerateXmlDocumentation";
			this.table2.Add(this.cmbGenerateXmlDocumentation);
			global::Gtk.Table.TableChild w25 = ((global::Gtk.Table.TableChild)(this.table2[this.cmbGenerateXmlDocumentation]));
			w25.LeftAttach = ((uint)(1));
			w25.RightAttach = ((uint)(2));
			w25.XOptions = ((global::Gtk.AttachOptions)(4));
			w25.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.cmbRemoveIntegerChecks = new global::Gtk.ComboBox();
			this.cmbRemoveIntegerChecks.Name = "cmbRemoveIntegerChecks";
			this.table2.Add(this.cmbRemoveIntegerChecks);
			global::Gtk.Table.TableChild w26 = ((global::Gtk.Table.TableChild)(this.table2[this.cmbRemoveIntegerChecks]));
			w26.TopAttach = ((uint)(1));
			w26.BottomAttach = ((uint)(2));
			w26.LeftAttach = ((uint)(1));
			w26.RightAttach = ((uint)(2));
			w26.XOptions = ((global::Gtk.AttachOptions)(4));
			w26.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.label89 = new global::Gtk.Label();
			this.label89.Name = "label89";
			this.label89.Xalign = 0F;
			this.label89.LabelProp = global::Mono.Unix.Catalog.GetString("Additional compiler arguments:");
			this.label89.UseUnderline = true;
			this.table2.Add(this.label89);
			global::Gtk.Table.TableChild w27 = ((global::Gtk.Table.TableChild)(this.table2[this.label89]));
			w27.TopAttach = ((uint)(3));
			w27.BottomAttach = ((uint)(4));
			w27.XOptions = ((global::Gtk.AttachOptions)(4));
			w27.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.label90 = new global::Gtk.Label();
			this.label90.Name = "label90";
			this.label90.Xalign = 0F;
			this.label90.LabelProp = global::Mono.Unix.Catalog.GetString("Generate XML documentation:");
			this.label90.UseUnderline = true;
			this.table2.Add(this.label90);
			global::Gtk.Table.TableChild w28 = ((global::Gtk.Table.TableChild)(this.table2[this.label90]));
			w28.XOptions = ((global::Gtk.AttachOptions)(4));
			w28.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.label95 = new global::Gtk.Label();
			this.label95.Name = "label95";
			this.label95.Xalign = 0F;
			this.label95.LabelProp = global::Mono.Unix.Catalog.GetString("Remove integer checks:");
			this.label95.UseUnderline = true;
			this.table2.Add(this.label95);
			global::Gtk.Table.TableChild w29 = ((global::Gtk.Table.TableChild)(this.table2[this.label95]));
			w29.TopAttach = ((uint)(1));
			w29.BottomAttach = ((uint)(2));
			w29.XOptions = ((global::Gtk.AttachOptions)(4));
			w29.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.label96 = new global::Gtk.Label();
			this.label96.Name = "label96";
			this.label96.Xalign = 0F;
			this.label96.LabelProp = global::Mono.Unix.Catalog.GetString("Define constants:");
			this.label96.UseUnderline = true;
			this.table2.Add(this.label96);
			global::Gtk.Table.TableChild w30 = ((global::Gtk.Table.TableChild)(this.table2[this.label96]));
			w30.TopAttach = ((uint)(2));
			w30.BottomAttach = ((uint)(3));
			w30.XOptions = ((global::Gtk.AttachOptions)(4));
			w30.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.txtAdditionalArguments = new global::Gtk.Entry();
			this.txtAdditionalArguments.CanFocus = true;
			this.txtAdditionalArguments.Name = "txtAdditionalArguments";
			this.txtAdditionalArguments.IsEditable = true;
			this.txtAdditionalArguments.InvisibleChar = '●';
			this.table2.Add(this.txtAdditionalArguments);
			global::Gtk.Table.TableChild w31 = ((global::Gtk.Table.TableChild)(this.table2[this.txtAdditionalArguments]));
			w31.TopAttach = ((uint)(3));
			w31.BottomAttach = ((uint)(4));
			w31.LeftAttach = ((uint)(1));
			w31.RightAttach = ((uint)(2));
			w31.XOptions = ((global::Gtk.AttachOptions)(4));
			w31.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.txtDefineConstants = new global::Gtk.Entry();
			this.txtDefineConstants.CanFocus = true;
			this.txtDefineConstants.Name = "txtDefineConstants";
			this.txtDefineConstants.IsEditable = true;
			this.txtDefineConstants.InvisibleChar = '●';
			this.table2.Add(this.txtDefineConstants);
			global::Gtk.Table.TableChild w32 = ((global::Gtk.Table.TableChild)(this.table2[this.txtDefineConstants]));
			w32.TopAttach = ((uint)(2));
			w32.BottomAttach = ((uint)(3));
			w32.LeftAttach = ((uint)(1));
			w32.RightAttach = ((uint)(2));
			w32.XOptions = ((global::Gtk.AttachOptions)(4));
			w32.YOptions = ((global::Gtk.AttachOptions)(4));
			this.hbox4.Add(this.table2);
			global::Gtk.Box.BoxChild w33 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.table2]));
			w33.Position = 1;
			w33.Expand = false;
			w33.Fill = false;
			this.vbox1.Add(this.hbox4);
			global::Gtk.Box.BoxChild w34 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox4]));
			w34.PackType = ((global::Gtk.PackType)(1));
			w34.Position = 5;
			w34.Expand = false;
			w34.Fill = false;
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
