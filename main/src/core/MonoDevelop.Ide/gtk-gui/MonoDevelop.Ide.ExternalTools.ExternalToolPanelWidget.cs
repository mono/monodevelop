#pragma warning disable 436

namespace MonoDevelop.Ide.ExternalTools
{
	public partial class ExternalToolPanelWidget
	{
		private global::Gtk.VBox vbox32;

		private global::Gtk.HBox hbox21;

		private global::Gtk.ScrolledWindow scrolledwindow4;

		private global::Gtk.TreeView toolListBox;

		private global::Gtk.VBox buttons;

		private global::Gtk.Button addButton;

		private global::Gtk.Button removeButton;

		private global::Gtk.Label label34;

		private global::Gtk.Button moveUpButton;

		private global::Gtk.Button moveDownButton;

		private global::Gtk.Table table2;

		private global::Gtk.Label argumentLabel;

		private global::MonoDevelop.Components.FileEntry browseButton;

		private global::Gtk.Label commandLabel;

		private global::Gtk.Label defaultKeyLabel;

		private global::Gtk.Table table3;

		private global::Gtk.Entry argumentTextBox;

		private global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton tagSelectorArgs;

		private global::Gtk.Table table4;

		private global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton tagSelectorPath;

		private global::Gtk.Entry workingDirTextBox;

		private global::Gtk.Table table5;

		private global::Gtk.Entry defaultKeyTextBox;

		private global::Gtk.EventBox keyBindingInfoEventBox;

		private global::MonoDevelop.Components.ImageView defaultKeyInfoIcon;

		private global::Gtk.Label label2;

		private global::Gtk.Label titleLabel;

		private global::Gtk.Entry titleTextBox;

		private global::Gtk.Label workingDirLabel;

		private global::Gtk.Table table1;

		private global::Gtk.CheckButton promptArgsCheckBox;

		private global::Gtk.CheckButton saveCurrentFileCheckBox;

		private global::Gtk.CheckButton useOutputPadCheckBox;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.ExternalTools.ExternalToolPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.ExternalTools.ExternalToolPanelWidget";
			// Container child MonoDevelop.Ide.ExternalTools.ExternalToolPanelWidget.Gtk.Container+ContainerChild
			this.vbox32 = new global::Gtk.VBox();
			this.vbox32.Name = "vbox32";
			this.vbox32.Spacing = 12;
			// Container child vbox32.Gtk.Box+BoxChild
			this.hbox21 = new global::Gtk.HBox();
			this.hbox21.Name = "hbox21";
			this.hbox21.Spacing = 6;
			// Container child hbox21.Gtk.Box+BoxChild
			this.scrolledwindow4 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow4.Name = "scrolledwindow4";
			this.scrolledwindow4.ShadowType = ((global::Gtk.ShadowType)(4));
			// Container child scrolledwindow4.Gtk.Container+ContainerChild
			this.toolListBox = new global::Gtk.TreeView();
			this.toolListBox.WidthRequest = 200;
			this.toolListBox.HeightRequest = 150;
			this.toolListBox.Name = "toolListBox";
			this.scrolledwindow4.Add(this.toolListBox);
			this.hbox21.Add(this.scrolledwindow4);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox21[this.scrolledwindow4]));
			w2.Position = 0;
			// Container child hbox21.Gtk.Box+BoxChild
			this.buttons = new global::Gtk.VBox();
			this.buttons.Name = "buttons";
			this.buttons.Spacing = 6;
			// Container child buttons.Gtk.Box+BoxChild
			this.addButton = new global::Gtk.Button();
			this.addButton.Name = "addButton";
			this.addButton.UseStock = true;
			this.addButton.UseUnderline = true;
			this.addButton.Label = "gtk-add";
			this.buttons.Add(this.addButton);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.buttons[this.addButton]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child buttons.Gtk.Box+BoxChild
			this.removeButton = new global::Gtk.Button();
			this.removeButton.Name = "removeButton";
			this.removeButton.UseStock = true;
			this.removeButton.UseUnderline = true;
			this.removeButton.Label = "gtk-remove";
			this.buttons.Add(this.removeButton);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.buttons[this.removeButton]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child buttons.Gtk.Box+BoxChild
			this.label34 = new global::Gtk.Label();
			this.label34.Name = "label34";
			this.label34.Xalign = 0F;
			this.label34.Yalign = 0F;
			this.label34.LabelProp = "    ";
			this.buttons.Add(this.label34);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.buttons[this.label34]));
			w5.Position = 2;
			// Container child buttons.Gtk.Box+BoxChild
			this.moveUpButton = new global::Gtk.Button();
			this.moveUpButton.Name = "moveUpButton";
			this.moveUpButton.UseStock = true;
			this.moveUpButton.UseUnderline = true;
			this.moveUpButton.Label = "gtk-go-up";
			this.buttons.Add(this.moveUpButton);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.buttons[this.moveUpButton]));
			w6.Position = 3;
			w6.Expand = false;
			w6.Fill = false;
			// Container child buttons.Gtk.Box+BoxChild
			this.moveDownButton = new global::Gtk.Button();
			this.moveDownButton.Name = "moveDownButton";
			this.moveDownButton.UseStock = true;
			this.moveDownButton.UseUnderline = true;
			this.moveDownButton.Label = "gtk-go-down";
			this.buttons.Add(this.moveDownButton);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.buttons[this.moveDownButton]));
			w7.Position = 4;
			w7.Expand = false;
			w7.Fill = false;
			this.hbox21.Add(this.buttons);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox21[this.buttons]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			this.vbox32.Add(this.hbox21);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox32[this.hbox21]));
			w9.Position = 0;
			// Container child vbox32.Gtk.Box+BoxChild
			this.table2 = new global::Gtk.Table(((uint)(5)), ((uint)(2)), false);
			this.table2.Name = "table2";
			this.table2.RowSpacing = ((uint)(6));
			this.table2.ColumnSpacing = ((uint)(6));
			// Container child table2.Gtk.Table+TableChild
			this.argumentLabel = new global::Gtk.Label();
			this.argumentLabel.Name = "argumentLabel";
			this.argumentLabel.Xalign = 0F;
			this.argumentLabel.Yalign = 0F;
			this.argumentLabel.LabelProp = global::Mono.Unix.Catalog.GetString("_Arguments:");
			this.argumentLabel.UseUnderline = true;
			this.table2.Add(this.argumentLabel);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table2[this.argumentLabel]));
			w10.TopAttach = ((uint)(2));
			w10.BottomAttach = ((uint)(3));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table2.Gtk.Table+TableChild
			this.browseButton = new global::MonoDevelop.Components.FileEntry();
			this.browseButton.Name = "browseButton";
			this.table2.Add(this.browseButton);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table2[this.browseButton]));
			w11.TopAttach = ((uint)(1));
			w11.BottomAttach = ((uint)(2));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(2));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.commandLabel = new global::Gtk.Label();
			this.commandLabel.Name = "commandLabel";
			this.commandLabel.Xalign = 0F;
			this.commandLabel.Yalign = 0F;
			this.commandLabel.LabelProp = global::Mono.Unix.Catalog.GetString("_Command:");
			this.commandLabel.UseUnderline = true;
			this.table2.Add(this.commandLabel);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.table2[this.commandLabel]));
			w12.TopAttach = ((uint)(1));
			w12.BottomAttach = ((uint)(2));
			w12.XOptions = ((global::Gtk.AttachOptions)(4));
			w12.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table2.Gtk.Table+TableChild
			this.defaultKeyLabel = new global::Gtk.Label();
			this.defaultKeyLabel.Name = "defaultKeyLabel";
			this.defaultKeyLabel.Xalign = 0F;
			this.defaultKeyLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Key Binding:");
			this.table2.Add(this.defaultKeyLabel);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table2[this.defaultKeyLabel]));
			w13.TopAttach = ((uint)(4));
			w13.BottomAttach = ((uint)(5));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.table3 = new global::Gtk.Table(((uint)(1)), ((uint)(2)), false);
			this.table3.Name = "table3";
			this.table3.RowSpacing = ((uint)(6));
			this.table3.ColumnSpacing = ((uint)(4));
			// Container child table3.Gtk.Table+TableChild
			this.argumentTextBox = new global::Gtk.Entry();
			this.argumentTextBox.Name = "argumentTextBox";
			this.argumentTextBox.IsEditable = true;
			this.argumentTextBox.InvisibleChar = '●';
			this.table3.Add(this.argumentTextBox);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.table3[this.argumentTextBox]));
			w14.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table3.Gtk.Table+TableChild
			this.tagSelectorArgs = new global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton();
			this.tagSelectorArgs.Events = ((global::Gdk.EventMask)(256));
			this.tagSelectorArgs.Name = "tagSelectorArgs";
			this.table3.Add(this.tagSelectorArgs);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.table3[this.tagSelectorArgs]));
			w15.LeftAttach = ((uint)(1));
			w15.RightAttach = ((uint)(2));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			this.table2.Add(this.table3);
			global::Gtk.Table.TableChild w16 = ((global::Gtk.Table.TableChild)(this.table2[this.table3]));
			w16.TopAttach = ((uint)(2));
			w16.BottomAttach = ((uint)(3));
			w16.LeftAttach = ((uint)(1));
			w16.RightAttach = ((uint)(2));
			w16.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table2.Gtk.Table+TableChild
			this.table4 = new global::Gtk.Table(((uint)(1)), ((uint)(2)), false);
			this.table4.Name = "table4";
			this.table4.RowSpacing = ((uint)(6));
			this.table4.ColumnSpacing = ((uint)(4));
			// Container child table4.Gtk.Table+TableChild
			this.tagSelectorPath = new global::MonoDevelop.Ide.Gui.Components.StringTagSelectorButton();
			this.tagSelectorPath.Events = ((global::Gdk.EventMask)(256));
			this.tagSelectorPath.Name = "tagSelectorPath";
			this.table4.Add(this.tagSelectorPath);
			global::Gtk.Table.TableChild w17 = ((global::Gtk.Table.TableChild)(this.table4[this.tagSelectorPath]));
			w17.LeftAttach = ((uint)(1));
			w17.RightAttach = ((uint)(2));
			w17.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table4.Gtk.Table+TableChild
			this.workingDirTextBox = new global::Gtk.Entry();
			this.workingDirTextBox.Name = "workingDirTextBox";
			this.workingDirTextBox.IsEditable = true;
			this.workingDirTextBox.InvisibleChar = '●';
			this.table4.Add(this.workingDirTextBox);
			global::Gtk.Table.TableChild w18 = ((global::Gtk.Table.TableChild)(this.table4[this.workingDirTextBox]));
			w18.YOptions = ((global::Gtk.AttachOptions)(0));
			this.table2.Add(this.table4);
			global::Gtk.Table.TableChild w19 = ((global::Gtk.Table.TableChild)(this.table2[this.table4]));
			w19.TopAttach = ((uint)(3));
			w19.BottomAttach = ((uint)(4));
			w19.LeftAttach = ((uint)(1));
			w19.RightAttach = ((uint)(2));
			w19.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table2.Gtk.Table+TableChild
			this.table5 = new global::Gtk.Table(((uint)(1)), ((uint)(3)), false);
			this.table5.Name = "table5";
			this.table5.RowSpacing = ((uint)(6));
			this.table5.ColumnSpacing = ((uint)(6));
			// Container child table5.Gtk.Table+TableChild
			this.defaultKeyTextBox = new global::Gtk.Entry();
			this.defaultKeyTextBox.CanFocus = true;
			this.defaultKeyTextBox.Name = "defaultKeyTextBox";
			this.defaultKeyTextBox.IsEditable = true;
			this.defaultKeyTextBox.InvisibleChar = '●';
			this.table5.Add(this.defaultKeyTextBox);
			global::Gtk.Table.TableChild w20 = ((global::Gtk.Table.TableChild)(this.table5[this.defaultKeyTextBox]));
			w20.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table5.Gtk.Table+TableChild
			this.keyBindingInfoEventBox = new global::Gtk.EventBox();
			this.keyBindingInfoEventBox.WidthRequest = 16;
			this.keyBindingInfoEventBox.HeightRequest = 16;
			this.keyBindingInfoEventBox.Name = "keyBindingInfoEventBox";
			this.keyBindingInfoEventBox.VisibleWindow = false;
			// Container child keyBindingInfoEventBox.Gtk.Container+ContainerChild
			this.defaultKeyInfoIcon = new global::MonoDevelop.Components.ImageView();
			this.defaultKeyInfoIcon.Name = "defaultKeyInfoIcon";
			this.defaultKeyInfoIcon.IconId = "md-warning";
			this.defaultKeyInfoIcon.IconSize = ((global::Gtk.IconSize)(1));
			this.keyBindingInfoEventBox.Add(this.defaultKeyInfoIcon);
			this.table5.Add(this.keyBindingInfoEventBox);
			global::Gtk.Table.TableChild w22 = ((global::Gtk.Table.TableChild)(this.table5[this.keyBindingInfoEventBox]));
			w22.LeftAttach = ((uint)(1));
			w22.RightAttach = ((uint)(2));
			w22.XOptions = ((global::Gtk.AttachOptions)(4));
			w22.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table5.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.table5.Add(this.label2);
			global::Gtk.Table.TableChild w23 = ((global::Gtk.Table.TableChild)(this.table5[this.label2]));
			w23.LeftAttach = ((uint)(2));
			w23.RightAttach = ((uint)(3));
			w23.YOptions = ((global::Gtk.AttachOptions)(4));
			this.table2.Add(this.table5);
			global::Gtk.Table.TableChild w24 = ((global::Gtk.Table.TableChild)(this.table2[this.table5]));
			w24.TopAttach = ((uint)(4));
			w24.BottomAttach = ((uint)(5));
			w24.LeftAttach = ((uint)(1));
			w24.RightAttach = ((uint)(2));
			w24.XOptions = ((global::Gtk.AttachOptions)(4));
			w24.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.titleLabel = new global::Gtk.Label();
			this.titleLabel.Name = "titleLabel";
			this.titleLabel.Xalign = 0F;
			this.titleLabel.Yalign = 0F;
			this.titleLabel.LabelProp = global::Mono.Unix.Catalog.GetString("_Title:");
			this.titleLabel.UseUnderline = true;
			this.table2.Add(this.titleLabel);
			global::Gtk.Table.TableChild w25 = ((global::Gtk.Table.TableChild)(this.table2[this.titleLabel]));
			w25.XOptions = ((global::Gtk.AttachOptions)(4));
			w25.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table2.Gtk.Table+TableChild
			this.titleTextBox = new global::Gtk.Entry();
			this.titleTextBox.Name = "titleTextBox";
			this.titleTextBox.IsEditable = true;
			this.titleTextBox.InvisibleChar = '●';
			this.table2.Add(this.titleTextBox);
			global::Gtk.Table.TableChild w26 = ((global::Gtk.Table.TableChild)(this.table2[this.titleTextBox]));
			w26.LeftAttach = ((uint)(1));
			w26.RightAttach = ((uint)(2));
			w26.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table2.Gtk.Table+TableChild
			this.workingDirLabel = new global::Gtk.Label();
			this.workingDirLabel.Name = "workingDirLabel";
			this.workingDirLabel.Xalign = 0F;
			this.workingDirLabel.Yalign = 0F;
			this.workingDirLabel.LabelProp = global::Mono.Unix.Catalog.GetString("_Working directory:");
			this.workingDirLabel.UseUnderline = true;
			this.table2.Add(this.workingDirLabel);
			global::Gtk.Table.TableChild w27 = ((global::Gtk.Table.TableChild)(this.table2[this.workingDirLabel]));
			w27.TopAttach = ((uint)(3));
			w27.BottomAttach = ((uint)(4));
			w27.XOptions = ((global::Gtk.AttachOptions)(4));
			w27.YOptions = ((global::Gtk.AttachOptions)(0));
			this.vbox32.Add(this.table2);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(this.vbox32[this.table2]));
			w28.Position = 1;
			w28.Expand = false;
			w28.Fill = false;
			// Container child vbox32.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(2)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.promptArgsCheckBox = new global::Gtk.CheckButton();
			this.promptArgsCheckBox.Name = "promptArgsCheckBox";
			this.promptArgsCheckBox.Label = global::Mono.Unix.Catalog.GetString("_Prompt for arguments");
			this.promptArgsCheckBox.DrawIndicator = true;
			this.promptArgsCheckBox.UseUnderline = true;
			this.table1.Add(this.promptArgsCheckBox);
			global::Gtk.Table.TableChild w29 = ((global::Gtk.Table.TableChild)(this.table1[this.promptArgsCheckBox]));
			w29.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.saveCurrentFileCheckBox = new global::Gtk.CheckButton();
			this.saveCurrentFileCheckBox.CanFocus = true;
			this.saveCurrentFileCheckBox.Name = "saveCurrentFileCheckBox";
			this.saveCurrentFileCheckBox.Label = global::Mono.Unix.Catalog.GetString("_Save current file");
			this.saveCurrentFileCheckBox.DrawIndicator = true;
			this.saveCurrentFileCheckBox.UseUnderline = true;
			this.table1.Add(this.saveCurrentFileCheckBox);
			global::Gtk.Table.TableChild w30 = ((global::Gtk.Table.TableChild)(this.table1[this.saveCurrentFileCheckBox]));
			w30.LeftAttach = ((uint)(1));
			w30.RightAttach = ((uint)(2));
			w30.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.useOutputPadCheckBox = new global::Gtk.CheckButton();
			this.useOutputPadCheckBox.Name = "useOutputPadCheckBox";
			this.useOutputPadCheckBox.Label = global::Mono.Unix.Catalog.GetString("Use _output window");
			this.useOutputPadCheckBox.DrawIndicator = true;
			this.useOutputPadCheckBox.UseUnderline = true;
			this.table1.Add(this.useOutputPadCheckBox);
			global::Gtk.Table.TableChild w31 = ((global::Gtk.Table.TableChild)(this.table1[this.useOutputPadCheckBox]));
			w31.TopAttach = ((uint)(1));
			w31.BottomAttach = ((uint)(2));
			w31.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox32.Add(this.table1);
			global::Gtk.Box.BoxChild w32 = ((global::Gtk.Box.BoxChild)(this.vbox32[this.table1]));
			w32.Position = 2;
			w32.Expand = false;
			w32.Fill = false;
			this.Add(this.vbox32);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.keyBindingInfoEventBox.Hide();
			this.titleLabel.MnemonicWidget = this.titleTextBox;
			this.Show();
		}
	}
}
#pragma warning restore 436
