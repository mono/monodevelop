#pragma warning disable 436

namespace MonoDevelop.VBNetBinding
{
	public partial class ProjectOptionsPanelWidget
	{
		private global::Gtk.VBox vbox5;

		private global::Gtk.Table table1;

		private global::Gtk.ComboBoxEntry cmbCodePage;

		private global::Gtk.ComboBox cmbOptionCompare;

		private global::Gtk.ComboBox cmbOptionExplicit;

		private global::Gtk.ComboBox cmbOptionInfer;

		private global::Gtk.ComboBox cmbOptionStrict;

		private global::Gtk.ComboBox compileTargetCombo;

		private global::Gtk.ComboBoxEntry entryMainClass;

		private global::MonoDevelop.Components.FileEntry iconEntry;

		private global::Gtk.Label label1;

		private global::Gtk.Label label3;

		private global::Gtk.Label label4;

		private global::Gtk.Label label5;

		private global::Gtk.Label label6;

		private global::Gtk.Label label7;

		private global::Gtk.Label label8;

		private global::Gtk.Label label86;

		private global::Gtk.Label label88;

		private global::Gtk.ComboBoxEntry txtMyType;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VBNetBinding.ProjectOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.VBNetBinding.ProjectOptionsPanelWidget";
			// Container child MonoDevelop.VBNetBinding.ProjectOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(9)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.cmbCodePage = global::Gtk.ComboBoxEntry.NewText();
			this.cmbCodePage.Name = "cmbCodePage";
			this.table1.Add(this.cmbCodePage);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbCodePage]));
			w1.TopAttach = ((uint)(3));
			w1.BottomAttach = ((uint)(4));
			w1.LeftAttach = ((uint)(1));
			w1.RightAttach = ((uint)(2));
			w1.XOptions = ((global::Gtk.AttachOptions)(4));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cmbOptionCompare = new global::Gtk.ComboBox();
			this.cmbOptionCompare.Name = "cmbOptionCompare";
			this.table1.Add(this.cmbOptionCompare);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbOptionCompare]));
			w2.TopAttach = ((uint)(7));
			w2.BottomAttach = ((uint)(8));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cmbOptionExplicit = new global::Gtk.ComboBox();
			this.cmbOptionExplicit.Name = "cmbOptionExplicit";
			this.table1.Add(this.cmbOptionExplicit);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbOptionExplicit]));
			w3.TopAttach = ((uint)(5));
			w3.BottomAttach = ((uint)(6));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cmbOptionInfer = new global::Gtk.ComboBox();
			this.cmbOptionInfer.Name = "cmbOptionInfer";
			this.table1.Add(this.cmbOptionInfer);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbOptionInfer]));
			w4.TopAttach = ((uint)(8));
			w4.BottomAttach = ((uint)(9));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(2));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cmbOptionStrict = new global::Gtk.ComboBox();
			this.cmbOptionStrict.Name = "cmbOptionStrict";
			this.table1.Add(this.cmbOptionStrict);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.cmbOptionStrict]));
			w5.TopAttach = ((uint)(6));
			w5.BottomAttach = ((uint)(7));
			w5.LeftAttach = ((uint)(1));
			w5.RightAttach = ((uint)(2));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.compileTargetCombo = new global::Gtk.ComboBox();
			this.compileTargetCombo.Name = "compileTargetCombo";
			this.table1.Add(this.compileTargetCombo);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.compileTargetCombo]));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryMainClass = new global::Gtk.ComboBoxEntry();
			this.entryMainClass.Name = "entryMainClass";
			this.table1.Add(this.entryMainClass);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table1[this.entryMainClass]));
			w7.TopAttach = ((uint)(1));
			w7.BottomAttach = ((uint)(2));
			w7.LeftAttach = ((uint)(1));
			w7.RightAttach = ((uint)(2));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.iconEntry = new global::MonoDevelop.Components.FileEntry();
			this.iconEntry.Name = "iconEntry";
			this.table1.Add(this.iconEntry);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table1[this.iconEntry]));
			w8.TopAttach = ((uint)(2));
			w8.BottomAttach = ((uint)(3));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Option Infer:");
			this.table1.Add(this.label1);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table1[this.label1]));
			w9.TopAttach = ((uint)(8));
			w9.BottomAttach = ((uint)(9));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Win32 Icon:");
			this.table1.Add(this.label3);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table1[this.label3]));
			w10.TopAttach = ((uint)(2));
			w10.BottomAttach = ((uint)(3));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("My Type:");
			this.table1.Add(this.label4);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table1[this.label4]));
			w11.TopAttach = ((uint)(4));
			w11.BottomAttach = ((uint)(5));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("Option Explicit:");
			this.table1.Add(this.label5);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.table1[this.label5]));
			w12.TopAttach = ((uint)(5));
			w12.BottomAttach = ((uint)(6));
			w12.XOptions = ((global::Gtk.AttachOptions)(4));
			w12.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("Option Strict:");
			this.table1.Add(this.label6);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table1[this.label6]));
			w13.TopAttach = ((uint)(6));
			w13.BottomAttach = ((uint)(7));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label7 = new global::Gtk.Label();
			this.label7.Name = "label7";
			this.label7.Xalign = 0F;
			this.label7.LabelProp = global::Mono.Unix.Catalog.GetString("Option Compare:");
			this.table1.Add(this.label7);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.table1[this.label7]));
			w14.TopAttach = ((uint)(7));
			w14.BottomAttach = ((uint)(8));
			w14.XOptions = ((global::Gtk.AttachOptions)(4));
			w14.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("Compiler Code Page:");
			this.table1.Add(this.label8);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.table1[this.label8]));
			w15.TopAttach = ((uint)(3));
			w15.BottomAttach = ((uint)(4));
			w15.XOptions = ((global::Gtk.AttachOptions)(4));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label86 = new global::Gtk.Label();
			this.label86.Name = "label86";
			this.label86.Xalign = 0F;
			this.label86.LabelProp = global::Mono.Unix.Catalog.GetString("Compile _Target:");
			this.label86.UseUnderline = true;
			this.table1.Add(this.label86);
			global::Gtk.Table.TableChild w16 = ((global::Gtk.Table.TableChild)(this.table1[this.label86]));
			w16.XOptions = ((global::Gtk.AttachOptions)(4));
			w16.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label88 = new global::Gtk.Label();
			this.label88.Name = "label88";
			this.label88.Xalign = 0F;
			this.label88.LabelProp = global::Mono.Unix.Catalog.GetString("_Main Class:");
			this.label88.UseUnderline = true;
			this.table1.Add(this.label88);
			global::Gtk.Table.TableChild w17 = ((global::Gtk.Table.TableChild)(this.table1[this.label88]));
			w17.TopAttach = ((uint)(1));
			w17.BottomAttach = ((uint)(2));
			w17.XOptions = ((global::Gtk.AttachOptions)(4));
			w17.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.txtMyType = new global::Gtk.ComboBoxEntry();
			this.txtMyType.Name = "txtMyType";
			this.table1.Add(this.txtMyType);
			global::Gtk.Table.TableChild w18 = ((global::Gtk.Table.TableChild)(this.table1[this.txtMyType]));
			w18.TopAttach = ((uint)(4));
			w18.BottomAttach = ((uint)(5));
			w18.LeftAttach = ((uint)(1));
			w18.RightAttach = ((uint)(2));
			w18.XOptions = ((global::Gtk.AttachOptions)(4));
			w18.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox5.Add(this.table1);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.table1]));
			w19.Position = 0;
			w19.Expand = false;
			w19.Fill = false;
			this.Add(this.vbox5);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
