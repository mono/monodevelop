#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class AddinLoadErrorDialog
	{
		private global::Gtk.HBox hbox1;

		private global::MonoDevelop.Components.ImageView iconError;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Label label4;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView errorTree;

		private global::Gtk.Label messageLabel;

		private global::Gtk.Button noButton;

		private global::Gtk.Button yesButton;

		private global::Gtk.Button closeButton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Dialogs.AddinLoadErrorDialog
			this.Name = "MonoDevelop.Ide.Gui.Dialogs.AddinLoadErrorDialog";
			this.Title = "";
			this.TypeHint = ((global::Gdk.WindowTypeHint)(1));
			this.BorderWidth = ((uint)(6));
			this.DefaultHeight = 350;
			// Internal child MonoDevelop.Ide.Gui.Dialogs.AddinLoadErrorDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog-vbox1";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog-vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 12;
			this.hbox1.BorderWidth = ((uint)(6));
			// Container child hbox1.Gtk.Box+BoxChild
			this.iconError = new global::MonoDevelop.Components.ImageView();
			this.iconError.Name = "iconError";
			this.iconError.Xalign = 0F;
			this.iconError.Yalign = 0F;
			this.iconError.IconId = "gtk-dialog-error";
			this.iconError.IconSize = ((global::Gtk.IconSize)(6));
			this.hbox1.Add(this.iconError);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.iconError]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.Yalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("The following extensions could not be started:");
			this.vbox4.Add(this.label4);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.label4]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.errorTree = new global::Gtk.TreeView();
			this.errorTree.Name = "errorTree";
			this.errorTree.HeadersVisible = false;
			this.scrolledwindow1.Add(this.errorTree);
			this.vbox4.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.scrolledwindow1]));
			w5.Position = 1;
			// Container child vbox4.Gtk.Box+BoxChild
			this.messageLabel = new global::Gtk.Label();
			this.messageLabel.WidthRequest = 479;
			this.messageLabel.Name = "messageLabel";
			this.messageLabel.Xalign = 0F;
			this.messageLabel.Yalign = 0F;
			this.messageLabel.Wrap = true;
			this.vbox4.Add(this.messageLabel);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.messageLabel]));
			w6.Position = 2;
			w6.Expand = false;
			w6.Fill = false;
			this.hbox1.Add(this.vbox4);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox4]));
			w7.Position = 1;
			w1.Add(this.hbox1);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(w1[this.hbox1]));
			w8.Position = 0;
			// Internal child MonoDevelop.Ide.Gui.Dialogs.AddinLoadErrorDialog.ActionArea
			global::Gtk.HButtonBox w9 = this.ActionArea;
			w9.Name = "GtkDialog_ActionArea";
			w9.Spacing = 6;
			w9.BorderWidth = ((uint)(5));
			w9.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child GtkDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.noButton = new global::Gtk.Button();
			this.noButton.CanFocus = true;
			this.noButton.Name = "noButton";
			this.noButton.UseStock = true;
			this.noButton.UseUnderline = true;
			this.noButton.Label = "gtk-no";
			this.AddActionWidget(this.noButton, -9);
			global::Gtk.ButtonBox.ButtonBoxChild w10 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w9[this.noButton]));
			w10.Expand = false;
			w10.Fill = false;
			// Container child GtkDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.yesButton = new global::Gtk.Button();
			this.yesButton.CanFocus = true;
			this.yesButton.Name = "yesButton";
			this.yesButton.UseStock = true;
			this.yesButton.UseUnderline = true;
			this.yesButton.Label = "gtk-yes";
			this.AddActionWidget(this.yesButton, -8);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w9[this.yesButton]));
			w11.Position = 1;
			w11.Expand = false;
			w11.Fill = false;
			// Container child GtkDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.closeButton = new global::Gtk.Button();
			this.closeButton.CanFocus = true;
			this.closeButton.Name = "closeButton";
			this.closeButton.UseStock = true;
			this.closeButton.UseUnderline = true;
			this.closeButton.Label = "gtk-close";
			this.AddActionWidget(this.closeButton, -7);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w9[this.closeButton]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 575;
			this.closeButton.Hide();
			this.Hide();
		}
	}
}
#pragma warning restore 436
