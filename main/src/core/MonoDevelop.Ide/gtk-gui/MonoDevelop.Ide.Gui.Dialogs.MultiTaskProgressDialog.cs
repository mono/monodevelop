#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class MultiTaskProgressDialog
	{
		private global::Gtk.Label title;

		private global::Gtk.ScrolledWindow progressScroll;

		private global::Gtk.TreeView progressTreeView;

		private global::Gtk.Label label1;

		private global::Gtk.ScrolledWindow detailsScroll;

		private global::Gtk.TextView detailsTextView;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonClose;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Dialogs.MultiTaskProgressDialog
			this.Name = "MonoDevelop.Ide.Gui.Dialogs.MultiTaskProgressDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Progress");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.Ide.Gui.Dialogs.MultiTaskProgressDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.title = new global::Gtk.Label();
			this.title.Name = "title";
			this.title.Xalign = 0F;
			this.title.UseMarkup = true;
			w1.Add(this.title);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(w1[this.title]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.progressScroll = new global::Gtk.ScrolledWindow();
			this.progressScroll.WidthRequest = 400;
			this.progressScroll.HeightRequest = 150;
			this.progressScroll.CanFocus = true;
			this.progressScroll.Name = "progressScroll";
			this.progressScroll.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.progressScroll.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child progressScroll.Gtk.Container+ContainerChild
			this.progressTreeView = new global::Gtk.TreeView();
			this.progressTreeView.CanFocus = true;
			this.progressTreeView.Name = "progressTreeView";
			this.progressScroll.Add(this.progressTreeView);
			w1.Add(this.progressScroll);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(w1[this.progressScroll]));
			w4.Position = 1;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Details:");
			w1.Add(this.label1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(w1[this.label1]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			w5.Padding = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.detailsScroll = new global::Gtk.ScrolledWindow();
			this.detailsScroll.HeightRequest = 120;
			this.detailsScroll.CanFocus = true;
			this.detailsScroll.Name = "detailsScroll";
			this.detailsScroll.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.detailsScroll.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child detailsScroll.Gtk.Container+ContainerChild
			this.detailsTextView = new global::Gtk.TextView();
			this.detailsTextView.CanFocus = true;
			this.detailsTextView.Name = "detailsTextView";
			this.detailsTextView.Editable = false;
			this.detailsTextView.CursorVisible = false;
			this.detailsTextView.WrapMode = ((global::Gtk.WrapMode)(3));
			this.detailsScroll.Add(this.detailsTextView);
			w1.Add(this.detailsScroll);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(w1[this.detailsScroll]));
			w7.Position = 3;
			// Internal child MonoDevelop.Ide.Gui.Dialogs.MultiTaskProgressDialog.ActionArea
			global::Gtk.HButtonBox w8 = this.ActionArea;
			w8.Name = "dialog1_ActionArea";
			w8.Spacing = 6;
			w8.BorderWidth = ((uint)(5));
			w8.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w9 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w8[this.buttonCancel]));
			w9.Expand = false;
			w9.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonClose = new global::Gtk.Button();
			this.buttonClose.CanDefault = true;
			this.buttonClose.CanFocus = true;
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.UseStock = true;
			this.buttonClose.UseUnderline = true;
			this.buttonClose.Label = "gtk-close";
			this.AddActionWidget(this.buttonClose, -7);
			global::Gtk.ButtonBox.ButtonBoxChild w10 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w8[this.buttonClose]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 491;
			this.DefaultHeight = 418;
			this.title.Hide();
			this.Hide();
			this.DeleteEvent += new global::Gtk.DeleteEventHandler(this.DeleteActivated);
			this.buttonCancel.Clicked += new global::System.EventHandler(this.OnCancel);
			this.buttonClose.Clicked += new global::System.EventHandler(this.OnClose);
		}
	}
}
#pragma warning restore 436
