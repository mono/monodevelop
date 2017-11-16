#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class OutputOptionsPanelWidget
	{
		private global::Gtk.VBox vbox66;

		private global::Gtk.VBox vbox67;

		private global::Gtk.Label label93;

		private global::Gtk.HBox hbox57;

		private global::Gtk.Label label91;

		private global::Gtk.VBox vbox69;

		private global::Gtk.Table table10;

		private global::Gtk.Entry assemblyNameEntry;

		private global::Gtk.Label label98;

		private global::Gtk.Label label99;

		private global::MonoDevelop.Components.FolderEntry outputPathEntry;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.OutputOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.OutputOptionsPanelWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.OutputOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox66 = new global::Gtk.VBox();
			this.vbox66.Name = "vbox66";
			this.vbox66.Spacing = 12;
			// Container child vbox66.Gtk.Box+BoxChild
			this.vbox67 = new global::Gtk.VBox();
			this.vbox67.Name = "vbox67";
			this.vbox67.Spacing = 6;
			// Container child vbox67.Gtk.Box+BoxChild
			this.label93 = new global::Gtk.Label();
			this.label93.Name = "label93";
			this.label93.Xalign = 0F;
			this.label93.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Output</b>");
			this.label93.UseMarkup = true;
			this.vbox67.Add(this.label93);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox67[this.label93]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox67.Gtk.Box+BoxChild
			this.hbox57 = new global::Gtk.HBox();
			this.hbox57.Name = "hbox57";
			// Container child hbox57.Gtk.Box+BoxChild
			this.label91 = new global::Gtk.Label();
			this.label91.WidthRequest = 18;
			this.label91.Name = "label91";
			this.hbox57.Add(this.label91);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox57[this.label91]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox57.Gtk.Box+BoxChild
			this.vbox69 = new global::Gtk.VBox();
			this.vbox69.Name = "vbox69";
			this.vbox69.Spacing = 6;
			// Container child vbox69.Gtk.Box+BoxChild
			this.table10 = new global::Gtk.Table(((uint)(2)), ((uint)(2)), false);
			this.table10.Name = "table10";
			this.table10.RowSpacing = ((uint)(6));
			this.table10.ColumnSpacing = ((uint)(6));
			// Container child table10.Gtk.Table+TableChild
			this.assemblyNameEntry = new global::Gtk.Entry();
			this.assemblyNameEntry.Name = "assemblyNameEntry";
			this.assemblyNameEntry.IsEditable = true;
			this.assemblyNameEntry.InvisibleChar = '●';
			this.table10.Add(this.assemblyNameEntry);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table10[this.assemblyNameEntry]));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table10.Gtk.Table+TableChild
			this.label98 = new global::Gtk.Label();
			this.label98.Name = "label98";
			this.label98.Xalign = 0F;
			this.label98.LabelProp = global::Mono.Unix.Catalog.GetString("Assembly _name:");
			this.label98.UseUnderline = true;
			this.table10.Add(this.label98);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table10[this.label98]));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table10.Gtk.Table+TableChild
			this.label99 = new global::Gtk.Label();
			this.label99.Name = "label99";
			this.label99.Xalign = 0F;
			this.label99.LabelProp = global::Mono.Unix.Catalog.GetString("Output _path:");
			this.label99.UseUnderline = true;
			this.table10.Add(this.label99);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table10[this.label99]));
			w5.TopAttach = ((uint)(1));
			w5.BottomAttach = ((uint)(2));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table10.Gtk.Table+TableChild
			this.outputPathEntry = new global::MonoDevelop.Components.FolderEntry();
			this.outputPathEntry.Name = "outputPathEntry";
			this.table10.Add(this.outputPathEntry);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table10[this.outputPathEntry]));
			w6.TopAttach = ((uint)(1));
			w6.BottomAttach = ((uint)(2));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox69.Add(this.table10);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox69[this.table10]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			this.hbox57.Add(this.vbox69);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox57[this.vbox69]));
			w8.Position = 1;
			this.vbox67.Add(this.hbox57);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox67[this.hbox57]));
			w9.Position = 1;
			this.vbox66.Add(this.vbox67);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox66[this.vbox67]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			this.Add(this.vbox66);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label98.MnemonicWidget = this.assemblyNameEntry;
			this.Show();
		}
	}
}
#pragma warning restore 436
