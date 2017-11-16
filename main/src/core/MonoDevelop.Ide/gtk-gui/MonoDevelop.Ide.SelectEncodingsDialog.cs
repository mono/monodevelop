#pragma warning disable 436

namespace MonoDevelop.Ide
{
	internal partial class SelectEncodingsDialog
	{
		private global::Gtk.Table table5;

		private global::Gtk.Label label106;

		private global::Gtk.Label label107;

		private global::Gtk.ScrolledWindow scrolledwindow10;

		private global::Gtk.TreeView listAvail;

		private global::Gtk.ScrolledWindow scrolledwindow11;

		private global::Gtk.TreeView listSelected;

		private global::Gtk.VBox vbox74;

		private global::Gtk.Label label108;

		private global::Gtk.Button btnAdd;

		private global::MonoDevelop.Components.ImageView imageAdd;

		private global::Gtk.Button btnRemove;

		private global::MonoDevelop.Components.ImageView imageRemove;

		private global::Gtk.Label label109;

		private global::Gtk.VBox vbox75;

		private global::Gtk.Button btnUp;

		private global::MonoDevelop.Components.ImageView imageUp;

		private global::Gtk.Button btnDown;

		private global::MonoDevelop.Components.ImageView imageDown;

		private global::Gtk.Button cancelbutton1;

		private global::Gtk.Button okbutton1;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.SelectEncodingsDialog
			this.Name = "MonoDevelop.Ide.SelectEncodingsDialog";
			this.Title = "Select Text Encodings";
			this.TypeHint = ((global::Gdk.WindowTypeHint)(1));
			this.Modal = true;
			this.BorderWidth = ((uint)(6));
			this.DefaultWidth = 700;
			this.DefaultHeight = 450;
			// Internal child MonoDevelop.Ide.SelectEncodingsDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog-vbox5";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog-vbox5.Gtk.Box+BoxChild
			this.table5 = new global::Gtk.Table(((uint)(2)), ((uint)(4)), false);
			this.table5.Name = "table5";
			this.table5.RowSpacing = ((uint)(6));
			this.table5.ColumnSpacing = ((uint)(12));
			this.table5.BorderWidth = ((uint)(6));
			// Container child table5.Gtk.Table+TableChild
			this.label106 = new global::Gtk.Label();
			this.label106.Name = "label106";
			this.label106.Xalign = 0F;
			this.label106.Yalign = 0F;
			this.label106.LabelProp = global::Mono.Unix.Catalog.GetString("Available encodings:");
			this.label106.WidthChars = 20;
			this.table5.Add(this.label106);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table5[this.label106]));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table5.Gtk.Table+TableChild
			this.label107 = new global::Gtk.Label();
			this.label107.Name = "label107";
			this.label107.Xalign = 0F;
			this.label107.Yalign = 0F;
			this.label107.LabelProp = global::Mono.Unix.Catalog.GetString("Encodings shown in menu:");
			this.label107.WidthChars = 20;
			this.table5.Add(this.label107);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table5[this.label107]));
			w3.LeftAttach = ((uint)(2));
			w3.RightAttach = ((uint)(3));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table5.Gtk.Table+TableChild
			this.scrolledwindow10 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow10.Name = "scrolledwindow10";
			this.scrolledwindow10.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow10.Gtk.Container+ContainerChild
			this.listAvail = new global::Gtk.TreeView();
			this.listAvail.Name = "listAvail";
			this.scrolledwindow10.Add(this.listAvail);
			this.table5.Add(this.scrolledwindow10);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table5[this.scrolledwindow10]));
			w5.TopAttach = ((uint)(1));
			w5.BottomAttach = ((uint)(2));
			// Container child table5.Gtk.Table+TableChild
			this.scrolledwindow11 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow11.Name = "scrolledwindow11";
			this.scrolledwindow11.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow11.Gtk.Container+ContainerChild
			this.listSelected = new global::Gtk.TreeView();
			this.listSelected.Name = "listSelected";
			this.scrolledwindow11.Add(this.listSelected);
			this.table5.Add(this.scrolledwindow11);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table5[this.scrolledwindow11]));
			w7.TopAttach = ((uint)(1));
			w7.BottomAttach = ((uint)(2));
			w7.LeftAttach = ((uint)(2));
			w7.RightAttach = ((uint)(3));
			// Container child table5.Gtk.Table+TableChild
			this.vbox74 = new global::Gtk.VBox();
			this.vbox74.Name = "vbox74";
			this.vbox74.Spacing = 6;
			// Container child vbox74.Gtk.Box+BoxChild
			this.label108 = new global::Gtk.Label();
			this.label108.Name = "label108";
			this.label108.Xalign = 0F;
			this.label108.Yalign = 0F;
			this.vbox74.Add(this.label108);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox74[this.label108]));
			w8.Position = 0;
			// Container child vbox74.Gtk.Box+BoxChild
			this.btnAdd = new global::Gtk.Button();
			this.btnAdd.Name = "btnAdd";
			// Container child btnAdd.Gtk.Container+ContainerChild
			this.imageAdd = new global::MonoDevelop.Components.ImageView();
			this.imageAdd.Name = "imageAdd";
			this.imageAdd.IconId = "gtk-add";
			this.imageAdd.IconSize = ((global::Gtk.IconSize)(4));
			this.btnAdd.Add(this.imageAdd);
			this.vbox74.Add(this.btnAdd);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox74[this.btnAdd]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox74.Gtk.Box+BoxChild
			this.btnRemove = new global::Gtk.Button();
			this.btnRemove.Name = "btnRemove";
			// Container child btnRemove.Gtk.Container+ContainerChild
			this.imageRemove = new global::MonoDevelop.Components.ImageView();
			this.imageRemove.Name = "imageRemove";
			this.imageRemove.IconId = "gtk-remove";
			this.imageRemove.IconSize = ((global::Gtk.IconSize)(4));
			this.btnRemove.Add(this.imageRemove);
			this.vbox74.Add(this.btnRemove);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox74[this.btnRemove]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox74.Gtk.Box+BoxChild
			this.label109 = new global::Gtk.Label();
			this.label109.Name = "label109";
			this.label109.Xalign = 0F;
			this.label109.Yalign = 0F;
			this.vbox74.Add(this.label109);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox74[this.label109]));
			w13.Position = 3;
			this.table5.Add(this.vbox74);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.table5[this.vbox74]));
			w14.TopAttach = ((uint)(1));
			w14.BottomAttach = ((uint)(2));
			w14.LeftAttach = ((uint)(1));
			w14.RightAttach = ((uint)(2));
			w14.XOptions = ((global::Gtk.AttachOptions)(0));
			w14.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table5.Gtk.Table+TableChild
			this.vbox75 = new global::Gtk.VBox();
			this.vbox75.Name = "vbox75";
			this.vbox75.Spacing = 6;
			// Container child vbox75.Gtk.Box+BoxChild
			this.btnUp = new global::Gtk.Button();
			this.btnUp.Name = "btnUp";
			// Container child btnUp.Gtk.Container+ContainerChild
			this.imageUp = new global::MonoDevelop.Components.ImageView();
			this.imageUp.Name = "imageUp";
			this.imageUp.IconId = "gtk-go-up";
			this.imageUp.IconSize = ((global::Gtk.IconSize)(4));
			this.btnUp.Add(this.imageUp);
			this.vbox75.Add(this.btnUp);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox75[this.btnUp]));
			w16.Position = 0;
			w16.Expand = false;
			w16.Fill = false;
			// Container child vbox75.Gtk.Box+BoxChild
			this.btnDown = new global::Gtk.Button();
			this.btnDown.Name = "btnDown";
			// Container child btnDown.Gtk.Container+ContainerChild
			this.imageDown = new global::MonoDevelop.Components.ImageView();
			this.imageDown.Name = "imageDown";
			this.imageDown.IconId = "gtk-go-down";
			this.imageDown.IconSize = ((global::Gtk.IconSize)(4));
			this.btnDown.Add(this.imageDown);
			this.vbox75.Add(this.btnDown);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.vbox75[this.btnDown]));
			w18.Position = 1;
			w18.Expand = false;
			w18.Fill = false;
			this.table5.Add(this.vbox75);
			global::Gtk.Table.TableChild w19 = ((global::Gtk.Table.TableChild)(this.table5[this.vbox75]));
			w19.TopAttach = ((uint)(1));
			w19.BottomAttach = ((uint)(2));
			w19.LeftAttach = ((uint)(3));
			w19.RightAttach = ((uint)(4));
			w19.XOptions = ((global::Gtk.AttachOptions)(0));
			w19.YOptions = ((global::Gtk.AttachOptions)(0));
			w1.Add(this.table5);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(w1[this.table5]));
			w20.Position = 0;
			// Internal child MonoDevelop.Ide.SelectEncodingsDialog.ActionArea
			global::Gtk.HButtonBox w21 = this.ActionArea;
			w21.Name = "dialog-action_area5";
			w21.Spacing = 6;
			w21.BorderWidth = ((uint)(5));
			w21.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog-action_area5.Gtk.ButtonBox+ButtonBoxChild
			this.cancelbutton1 = new global::Gtk.Button();
			this.cancelbutton1.Name = "cancelbutton1";
			this.cancelbutton1.UseStock = true;
			this.cancelbutton1.UseUnderline = true;
			this.cancelbutton1.Label = "gtk-cancel";
			this.AddActionWidget(this.cancelbutton1, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w22 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w21[this.cancelbutton1]));
			w22.Expand = false;
			w22.Fill = false;
			// Container child dialog-action_area5.Gtk.ButtonBox+ButtonBoxChild
			this.okbutton1 = new global::Gtk.Button();
			this.okbutton1.Name = "okbutton1";
			this.okbutton1.UseStock = true;
			this.okbutton1.UseUnderline = true;
			this.okbutton1.Label = "gtk-ok";
			this.AddActionWidget(this.okbutton1, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w23 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w21[this.okbutton1]));
			w23.Position = 1;
			w23.Expand = false;
			w23.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.Response += new global::Gtk.ResponseHandler(this.OnRespond);
			this.btnUp.Clicked += new global::System.EventHandler(this.OnUpClicked);
			this.btnDown.Clicked += new global::System.EventHandler(this.OnDownClicked);
			this.btnAdd.Clicked += new global::System.EventHandler(this.OnAddClicked);
			this.btnRemove.Clicked += new global::System.EventHandler(this.OnRemoveClicked);
		}
	}
}
#pragma warning restore 436
