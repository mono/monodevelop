#pragma warning disable 436

namespace MonoDevelop.Deployment.Gui
{
	internal partial class SourcesZipEditorWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label4;

		private global::Gtk.Table table1;

		private global::Gtk.ComboBox comboFormat;

		private global::MonoDevelop.Components.FolderEntry folderEntry;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Entry entryZip;

		private global::Gtk.ComboBox comboZip;

		private global::Gtk.Label label1;

		private global::Gtk.Label label2;

		private global::Gtk.Label label3;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Deployment.Gui.SourcesZipEditorWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Deployment.Gui.SourcesZipEditorWidget";
			// Container child MonoDevelop.Deployment.Gui.SourcesZipEditorWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 12;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Select the archive file name and format:");
			this.vbox2.Add(this.label4);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label4]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(3)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.comboFormat = global::Gtk.ComboBox.NewText();
			this.comboFormat.Name = "comboFormat";
			this.table1.Add(this.comboFormat);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table1[this.comboFormat]));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.folderEntry = new global::MonoDevelop.Components.FolderEntry();
			this.folderEntry.Name = "folderEntry";
			this.table1.Add(this.folderEntry);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1[this.folderEntry]));
			w3.TopAttach = ((uint)(1));
			w3.BottomAttach = ((uint)(2));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.entryZip = new global::Gtk.Entry();
			this.entryZip.CanFocus = true;
			this.entryZip.Name = "entryZip";
			this.entryZip.IsEditable = true;
			this.entryZip.InvisibleChar = '●';
			this.hbox1.Add(this.entryZip);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.entryZip]));
			w4.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.comboZip = global::Gtk.ComboBox.NewText();
			this.comboZip.Name = "comboZip";
			this.hbox1.Add(this.comboZip);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.comboZip]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.table1.Add(this.hbox1);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.hbox1]));
			w6.TopAttach = ((uint)(2));
			w6.BottomAttach = ((uint)(3));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("File:");
			this.table1.Add(this.label1);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table1[this.label1]));
			w7.TopAttach = ((uint)(2));
			w7.BottomAttach = ((uint)(3));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Target folder:");
			this.table1.Add(this.label2);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table1[this.label2]));
			w8.TopAttach = ((uint)(1));
			w8.BottomAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("File format:");
			this.table1.Add(this.label3);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table1[this.label3]));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox2.Add(this.table1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.table1]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
			this.entryZip.Changed += new global::System.EventHandler(this.OnEntryZipChanged);
			this.comboZip.Changed += new global::System.EventHandler(this.OnComboZipChanged);
			this.comboFormat.Changed += new global::System.EventHandler(this.OnComboFormatChanged);
		}
	}
}
#pragma warning restore 436
