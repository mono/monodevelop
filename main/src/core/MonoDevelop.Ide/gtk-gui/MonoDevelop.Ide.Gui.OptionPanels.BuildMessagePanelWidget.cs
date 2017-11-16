#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal partial class BuildMessagePanelWidget
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Table table4;

		private global::Gtk.ComboBox comboboxErrorPadAfter;

		private global::Gtk.ComboBox comboboxJumpToFirst;

		private global::Gtk.ComboBox comboboxMessageBubbles;

		private global::Gtk.Label label3;

		private global::Gtk.Label label5;

		private global::Gtk.Label label6;

		private global::Gtk.HBox hbox4;

		private global::Gtk.Label label10;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.OptionPanels.BuildMessagePanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.OptionPanels.BuildMessagePanelWidget";
			// Container child MonoDevelop.Ide.Gui.OptionPanels.BuildMessagePanelWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.table4 = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table4.Name = "table4";
			this.table4.RowSpacing = ((uint)(6));
			this.table4.ColumnSpacing = ((uint)(6));
			// Container child table4.Gtk.Table+TableChild
			this.comboboxErrorPadAfter = global::Gtk.ComboBox.NewText();
			this.comboboxErrorPadAfter.AppendText(global::Mono.Unix.Catalog.GetString("Never"));
			this.comboboxErrorPadAfter.Name = "comboboxErrorPadAfter";
			this.comboboxErrorPadAfter.Active = 0;
			this.table4.Add(this.comboboxErrorPadAfter);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.table4[this.comboboxErrorPadAfter]));
			w1.TopAttach = ((uint)(1));
			w1.BottomAttach = ((uint)(2));
			w1.LeftAttach = ((uint)(1));
			w1.RightAttach = ((uint)(2));
			w1.XOptions = ((global::Gtk.AttachOptions)(4));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table4.Gtk.Table+TableChild
			this.comboboxJumpToFirst = global::Gtk.ComboBox.NewText();
			this.comboboxJumpToFirst.AppendText(global::Mono.Unix.Catalog.GetString("Never"));
			this.comboboxJumpToFirst.Name = "comboboxJumpToFirst";
			this.comboboxJumpToFirst.Active = 0;
			this.table4.Add(this.comboboxJumpToFirst);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table4[this.comboboxJumpToFirst]));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table4.Gtk.Table+TableChild
			this.comboboxMessageBubbles = global::Gtk.ComboBox.NewText();
			this.comboboxMessageBubbles.AppendText(global::Mono.Unix.Catalog.GetString("Never"));
			this.comboboxMessageBubbles.Name = "comboboxMessageBubbles";
			this.comboboxMessageBubbles.Active = 0;
			this.table4.Add(this.comboboxMessageBubbles);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table4[this.comboboxMessageBubbles]));
			w3.TopAttach = ((uint)(2));
			w3.BottomAttach = ((uint)(3));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table4.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Show error pad:");
			this.table4.Add(this.label3);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table4[this.label3]));
			w4.TopAttach = ((uint)(1));
			w4.BottomAttach = ((uint)(2));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table4.Gtk.Table+TableChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("Show message bubbles:");
			this.table4.Add(this.label5);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table4[this.label5]));
			w5.TopAttach = ((uint)(2));
			w5.BottomAttach = ((uint)(3));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table4.Gtk.Table+TableChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("Jump to first error or warning:");
			this.table4.Add(this.label6);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table4[this.label6]));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox1.Add(this.table4);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.table4]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			// Container child hbox4.Gtk.Box+BoxChild
			this.label10 = new global::Gtk.Label();
			this.label10.Name = "label10";
			this.label10.LabelProp = global::Mono.Unix.Catalog.GetString("    ");
			this.hbox4.Add(this.label10);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.label10]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			this.vbox1.Add(this.hbox4);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox4]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
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
