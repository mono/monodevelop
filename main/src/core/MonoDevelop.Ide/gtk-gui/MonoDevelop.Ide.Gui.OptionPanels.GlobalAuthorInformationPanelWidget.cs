#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal partial class GlobalAuthorInformationPanelWidget
	{
		private global::Gtk.Table table1;

		private global::Gtk.Entry companyEntry;

		private global::Gtk.Entry copyrightEntry;

		private global::Gtk.Entry emailEntry;

		private global::Gtk.Label label2;

		private global::Gtk.Label label3;

		private global::Gtk.Label label4;

		private global::Gtk.Label label5;

		private global::Gtk.Label label6;

		private global::Gtk.Entry nameEntry;

		private global::Gtk.Entry trademarkEntry;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.OptionPanels.GlobalAuthorInformationPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.OptionPanels.GlobalAuthorInformationPanelWidget";
			// Container child MonoDevelop.Ide.Gui.OptionPanels.GlobalAuthorInformationPanelWidget.Gtk.Container+ContainerChild
			this.table1 = new global::Gtk.Table(((uint)(5)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.companyEntry = new global::Gtk.Entry();
			this.companyEntry.CanFocus = true;
			this.companyEntry.Name = "companyEntry";
			this.companyEntry.IsEditable = true;
			this.companyEntry.InvisibleChar = '●';
			this.table1.Add(this.companyEntry);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.table1[this.companyEntry]));
			w1.TopAttach = ((uint)(3));
			w1.BottomAttach = ((uint)(4));
			w1.LeftAttach = ((uint)(1));
			w1.RightAttach = ((uint)(2));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.copyrightEntry = new global::Gtk.Entry();
			this.copyrightEntry.CanFocus = true;
			this.copyrightEntry.Name = "copyrightEntry";
			this.copyrightEntry.IsEditable = true;
			this.copyrightEntry.InvisibleChar = '●';
			this.table1.Add(this.copyrightEntry);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table1[this.copyrightEntry]));
			w2.TopAttach = ((uint)(2));
			w2.BottomAttach = ((uint)(3));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.emailEntry = new global::Gtk.Entry();
			this.emailEntry.CanFocus = true;
			this.emailEntry.Name = "emailEntry";
			this.emailEntry.IsEditable = true;
			this.emailEntry.InvisibleChar = '●';
			this.table1.Add(this.emailEntry);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1[this.emailEntry]));
			w3.TopAttach = ((uint)(1));
			w3.BottomAttach = ((uint)(2));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("_Name:");
			this.label2.UseUnderline = true;
			this.table1.Add(this.label2);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.label2]));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("_Copyright:");
			this.label3.UseUnderline = true;
			this.table1.Add(this.label3);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.label3]));
			w5.TopAttach = ((uint)(2));
			w5.BottomAttach = ((uint)(3));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("_Email:");
			this.label4.UseUnderline = true;
			this.table1.Add(this.label4);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.label4]));
			w6.TopAttach = ((uint)(1));
			w6.BottomAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("C_ompany:");
			this.label5.UseUnderline = true;
			this.table1.Add(this.label5);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table1[this.label5]));
			w7.TopAttach = ((uint)(3));
			w7.BottomAttach = ((uint)(4));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("_Trademark:");
			this.label6.UseUnderline = true;
			this.table1.Add(this.label6);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table1[this.label6]));
			w8.TopAttach = ((uint)(4));
			w8.BottomAttach = ((uint)(5));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.nameEntry = new global::Gtk.Entry();
			this.nameEntry.CanFocus = true;
			this.nameEntry.Name = "nameEntry";
			this.nameEntry.IsEditable = true;
			this.nameEntry.InvisibleChar = '●';
			this.table1.Add(this.nameEntry);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table1[this.nameEntry]));
			w9.LeftAttach = ((uint)(1));
			w9.RightAttach = ((uint)(2));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.trademarkEntry = new global::Gtk.Entry();
			this.trademarkEntry.CanFocus = true;
			this.trademarkEntry.Name = "trademarkEntry";
			this.trademarkEntry.IsEditable = true;
			this.trademarkEntry.InvisibleChar = '●';
			this.table1.Add(this.trademarkEntry);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table1[this.trademarkEntry]));
			w10.TopAttach = ((uint)(4));
			w10.BottomAttach = ((uint)(5));
			w10.LeftAttach = ((uint)(1));
			w10.RightAttach = ((uint)(2));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			this.Add(this.table1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label2.MnemonicWidget = this.nameEntry;
			this.Hide();
		}
	}
}
#pragma warning restore 436
