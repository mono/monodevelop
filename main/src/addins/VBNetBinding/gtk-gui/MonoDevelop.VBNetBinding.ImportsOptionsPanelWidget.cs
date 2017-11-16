#pragma warning disable 436

namespace MonoDevelop.VBNetBinding
{
	public partial class ImportsOptionsPanelWidget
	{
		private global::Gtk.Table table3;

		private global::Gtk.Button cmdAdd;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeview1;

		private global::Gtk.Entry txtImport;

		private global::Gtk.VBox vbox1;

		private global::Gtk.Button cmdRemove;

		private global::Gtk.Label label10;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VBNetBinding.ImportsOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.VBNetBinding.ImportsOptionsPanelWidget";
			// Container child MonoDevelop.VBNetBinding.ImportsOptionsPanelWidget.Gtk.Container+ContainerChild
			this.table3 = new global::Gtk.Table(((uint)(2)), ((uint)(2)), false);
			this.table3.Name = "table3";
			this.table3.RowSpacing = ((uint)(6));
			this.table3.ColumnSpacing = ((uint)(6));
			// Container child table3.Gtk.Table+TableChild
			this.cmdAdd = new global::Gtk.Button();
			this.cmdAdd.CanFocus = true;
			this.cmdAdd.Name = "cmdAdd";
			this.cmdAdd.UseUnderline = true;
			this.cmdAdd.Label = global::Mono.Unix.Catalog.GetString("Add");
			this.table3.Add(this.cmdAdd);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.table3[this.cmdAdd]));
			w1.LeftAttach = ((uint)(1));
			w1.RightAttach = ((uint)(2));
			w1.XOptions = ((global::Gtk.AttachOptions)(4));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeview1 = new global::Gtk.TreeView();
			this.treeview1.CanFocus = true;
			this.treeview1.Name = "treeview1";
			this.GtkScrolledWindow.Add(this.treeview1);
			this.table3.Add(this.GtkScrolledWindow);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table3[this.GtkScrolledWindow]));
			w3.TopAttach = ((uint)(1));
			w3.BottomAttach = ((uint)(2));
			// Container child table3.Gtk.Table+TableChild
			this.txtImport = new global::Gtk.Entry();
			this.txtImport.CanFocus = true;
			this.txtImport.Name = "txtImport";
			this.txtImport.IsEditable = true;
			this.txtImport.InvisibleChar = '●';
			this.table3.Add(this.txtImport);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table3[this.txtImport]));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table3.Gtk.Table+TableChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.cmdRemove = new global::Gtk.Button();
			this.cmdRemove.CanFocus = true;
			this.cmdRemove.Name = "cmdRemove";
			this.cmdRemove.UseUnderline = true;
			this.cmdRemove.Label = global::Mono.Unix.Catalog.GetString("Remove");
			this.vbox1.Add(this.cmdRemove);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.cmdRemove]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label10 = new global::Gtk.Label();
			this.label10.Name = "label10";
			this.vbox1.Add(this.label10);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label10]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			this.table3.Add(this.vbox1);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table3[this.vbox1]));
			w7.TopAttach = ((uint)(1));
			w7.BottomAttach = ((uint)(2));
			w7.LeftAttach = ((uint)(1));
			w7.RightAttach = ((uint)(2));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			this.Add(this.table3);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.cmdRemove.Clicked += new global::System.EventHandler(this.OnCmdRemoveClicked);
			this.txtImport.Changed += new global::System.EventHandler(this.OnTxtImportChanged);
			this.cmdAdd.Clicked += new global::System.EventHandler(this.OnCmdAddClicked);
		}
	}
}
#pragma warning restore 436
