#pragma warning disable 436

namespace MonoDevelop.Refactoring.SignatureChange
{
	public partial class SignatureChangeDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label1;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeviewParameterList;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Button buttonUp;

		private global::Gtk.Button buttonDown;

		private global::Gtk.Button buttonRemove;

		private global::Gtk.Button buttonRefresh;

		private global::Gtk.Frame framePreview;

		private global::Gtk.Alignment GtkAlignment6;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Label GtkLabel6;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Refactoring.SignatureChange.SignatureChangeDialog
			this.Name = "MonoDevelop.Refactoring.SignatureChange.SignatureChangeDialog";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.Refactoring.SignatureChange.SignatureChangeDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(2));
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Parameter:");
			this.vbox2.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeviewParameterList = new global::Gtk.TreeView();
			this.treeviewParameterList.CanFocus = true;
			this.treeviewParameterList.Name = "treeviewParameterList";
			this.GtkScrolledWindow.Add(this.treeviewParameterList);
			this.hbox1.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.GtkScrolledWindow]));
			w4.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonUp = new global::Gtk.Button();
			this.buttonUp.CanFocus = true;
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.UseStock = true;
			this.buttonUp.UseUnderline = true;
			this.buttonUp.Label = "gtk-go-up";
			this.vbox3.Add(this.buttonUp);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonUp]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonDown = new global::Gtk.Button();
			this.buttonDown.CanFocus = true;
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.UseStock = true;
			this.buttonDown.UseUnderline = true;
			this.buttonDown.Label = "gtk-go-down";
			this.vbox3.Add(this.buttonDown);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonDown]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonRemove = new global::Gtk.Button();
			this.buttonRemove.CanFocus = true;
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseStock = true;
			this.buttonRemove.UseUnderline = true;
			this.buttonRemove.Label = "gtk-remove";
			this.vbox3.Add(this.buttonRemove);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonRemove]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonRefresh = new global::Gtk.Button();
			this.buttonRefresh.CanFocus = true;
			this.buttonRefresh.Name = "buttonRefresh";
			this.buttonRefresh.UseStock = true;
			this.buttonRefresh.UseUnderline = true;
			this.buttonRefresh.Label = "gtk-refresh";
			this.vbox3.Add(this.buttonRefresh);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonRefresh]));
			w8.Position = 3;
			w8.Expand = false;
			w8.Fill = false;
			this.hbox1.Add(this.vbox3);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox3]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.framePreview = new global::Gtk.Frame();
			this.framePreview.Name = "framePreview";
			this.framePreview.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child framePreview.Gtk.Container+ContainerChild
			this.GtkAlignment6 = new global::Gtk.Alignment(0F, 0F, 1F, 1F);
			this.GtkAlignment6.Name = "GtkAlignment6";
			this.GtkAlignment6.LeftPadding = ((uint)(12));
			// Container child GtkAlignment6.Gtk.Container+ContainerChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			this.GtkAlignment6.Add(this.vbox4);
			this.framePreview.Add(this.GtkAlignment6);
			this.GtkLabel6 = new global::Gtk.Label();
			this.GtkLabel6.Name = "GtkLabel6";
			this.GtkLabel6.LabelProp = global::Mono.Unix.Catalog.GetString("Preview:");
			this.GtkLabel6.UseMarkup = true;
			this.framePreview.LabelWidget = this.GtkLabel6;
			this.vbox2.Add(this.framePreview);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.framePreview]));
			w13.Position = 2;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w14.Position = 0;
			// Internal child MonoDevelop.Refactoring.SignatureChange.SignatureChangeDialog.ActionArea
			global::Gtk.HButtonBox w15 = this.ActionArea;
			w15.Name = "dialog1_ActionArea";
			w15.Spacing = 10;
			w15.BorderWidth = ((uint)(5));
			w15.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w16 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w15[this.buttonCancel]));
			w16.Expand = false;
			w16.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w17 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w15[this.buttonOk]));
			w17.Position = 1;
			w17.Expand = false;
			w17.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 771;
			this.DefaultHeight = 643;
			this.Show();
		}
	}
}
#pragma warning restore 436
