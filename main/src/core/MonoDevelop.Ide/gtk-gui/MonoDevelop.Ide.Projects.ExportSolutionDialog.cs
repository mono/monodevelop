#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class ExportSolutionDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Table table;

		private global::Gtk.ComboBox comboFormat;

		private global::MonoDevelop.Components.FolderEntry folderEntry;

		private global::Gtk.Label label2;

		private global::Gtk.Label label4;

		private global::Gtk.Label labelNewFormat;

		private global::Gtk.Label newFormatLabel;

		private global::Gtk.Button button51;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.ExportSolutionDialog
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.Ide.Projects.ExportSolutionDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Export Solution");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			this.Resizable = false;
			// Internal child MonoDevelop.Ide.Projects.ExportSolutionDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Events = ((global::Gdk.EventMask)(256));
			w1.Name = "dialog_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 12;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.table = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table.Name = "table";
			this.table.RowSpacing = ((uint)(6));
			this.table.ColumnSpacing = ((uint)(6));
			// Container child table.Gtk.Table+TableChild
			this.comboFormat = global::Gtk.ComboBox.NewText();
			this.comboFormat.Name = "comboFormat";
			this.table.Add(this.comboFormat);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table[this.comboFormat]));
			w2.TopAttach = ((uint)(1));
			w2.BottomAttach = ((uint)(2));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table.Gtk.Table+TableChild
			this.folderEntry = new global::MonoDevelop.Components.FolderEntry();
			this.folderEntry.Name = "folderEntry";
			this.table.Add(this.folderEntry);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table[this.folderEntry]));
			w3.TopAttach = ((uint)(2));
			w3.BottomAttach = ((uint)(3));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Target folder:");
			this.table.Add(this.label2);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table[this.label2]));
			w4.TopAttach = ((uint)(2));
			w4.BottomAttach = ((uint)(3));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table.Gtk.Table+TableChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Current format:");
			this.table.Add(this.label4);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table[this.label4]));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table.Gtk.Table+TableChild
			this.labelNewFormat = new global::Gtk.Label();
			this.labelNewFormat.Name = "labelNewFormat";
			this.labelNewFormat.Xalign = 0F;
			this.table.Add(this.labelNewFormat);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table[this.labelNewFormat]));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table.Gtk.Table+TableChild
			this.newFormatLabel = new global::Gtk.Label();
			this.newFormatLabel.Name = "newFormatLabel";
			this.newFormatLabel.Xalign = 0F;
			this.newFormatLabel.LabelProp = global::Mono.Unix.Catalog.GetString("New format:");
			this.table.Add(this.newFormatLabel);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table[this.newFormatLabel]));
			w7.TopAttach = ((uint)(1));
			w7.BottomAttach = ((uint)(2));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox2.Add(this.table);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.table]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Internal child MonoDevelop.Ide.Projects.ExportSolutionDialog.ActionArea
			global::Gtk.HButtonBox w10 = this.ActionArea;
			w10.Name = "MonoDevelop.Ide.ExportProjectDialog_ActionArea";
			w10.Spacing = 6;
			w10.BorderWidth = ((uint)(5));
			w10.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child MonoDevelop.Ide.ExportProjectDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button51 = new global::Gtk.Button();
			this.button51.CanDefault = true;
			this.button51.CanFocus = true;
			this.button51.Name = "button51";
			this.button51.UseStock = true;
			this.button51.UseUnderline = true;
			this.button51.Label = "gtk-cancel";
			this.AddActionWidget(this.button51, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.button51]));
			w11.Expand = false;
			w11.Fill = false;
			// Container child MonoDevelop.Ide.ExportProjectDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.buttonOk]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 496;
			this.DefaultHeight = 154;
			this.Hide();
		}
	}
}
#pragma warning restore 436
