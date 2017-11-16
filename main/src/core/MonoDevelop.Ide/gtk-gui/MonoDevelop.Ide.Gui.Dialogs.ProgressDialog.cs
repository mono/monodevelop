#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class ProgressDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ProgressBar progressBar;

		private global::Gtk.Button btnCancel;

		private global::Gtk.Button btnClose;

		private global::Gtk.Expander expander;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TextView detailsTextView;

		private global::Gtk.Label expanderLabel;

		private global::Gtk.Button button103;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Dialogs.ProgressDialog
			this.Name = "MonoDevelop.Ide.Gui.Dialogs.ProgressDialog";
			this.Title = "";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			// Internal child MonoDevelop.Ide.Gui.Dialogs.ProgressDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(12));
			// Container child vbox2.Gtk.Box+BoxChild
			this.label = new global::Gtk.Label();
			this.label.Name = "label";
			this.label.Xalign = 0F;
			this.label.LabelProp = "label";
			this.vbox2.Add(this.label);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.progressBar = new global::Gtk.ProgressBar();
			this.progressBar.Name = "progressBar";
			this.hbox1.Add(this.progressBar);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.progressBar]));
			w3.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnCancel = new global::Gtk.Button();
			this.btnCancel.CanDefault = true;
			this.btnCancel.CanFocus = true;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseStock = true;
			this.btnCancel.UseUnderline = true;
			this.btnCancel.Label = "gtk-cancel";
			this.hbox1.Add(this.btnCancel);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnCancel]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnClose = new global::Gtk.Button();
			this.btnClose.CanDefault = true;
			this.btnClose.CanFocus = true;
			this.btnClose.Name = "btnClose";
			this.btnClose.UseStock = true;
			this.btnClose.UseUnderline = true;
			this.btnClose.Label = "gtk-close";
			this.hbox1.Add(this.btnClose);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnClose]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.expander = new global::Gtk.Expander(null);
			this.expander.CanFocus = true;
			this.expander.Name = "expander";
			// Container child expander.Gtk.Container+ContainerChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.HeightRequest = 250;
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.detailsTextView = new global::Gtk.TextView();
			this.detailsTextView.CanFocus = true;
			this.detailsTextView.Name = "detailsTextView";
			this.GtkScrolledWindow.Add(this.detailsTextView);
			this.expander.Add(this.GtkScrolledWindow);
			this.expanderLabel = new global::Gtk.Label();
			this.expanderLabel.Name = "expanderLabel";
			this.expanderLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Details");
			this.expanderLabel.UseUnderline = true;
			this.expander.LabelWidget = this.expanderLabel;
			this.vbox2.Add(this.expander);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.expander]));
			w9.Position = 2;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w10.Position = 0;
			// Internal child MonoDevelop.Ide.Gui.Dialogs.ProgressDialog.ActionArea
			global::Gtk.HButtonBox w11 = this.ActionArea;
			w11.Name = "dialog1_ActionArea";
			w11.Spacing = 10;
			w11.BorderWidth = ((uint)(5));
			w11.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button103 = new global::Gtk.Button();
			this.button103.CanFocus = true;
			this.button103.Name = "button103";
			this.button103.UseUnderline = true;
			this.button103.Label = global::Mono.Unix.Catalog.GetString("GtkButton");
			this.AddActionWidget(this.button103, 0);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w11[this.button103]));
			w12.Expand = false;
			w12.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 544;
			this.DefaultHeight = 170;
			this.btnClose.Hide();
			w11.Hide();
			this.Hide();
			this.btnCancel.Clicked += new global::System.EventHandler(this.OnBtnCancelClicked);
			this.btnClose.Clicked += new global::System.EventHandler(this.OnBtnCloseClicked);
			this.expander.Activated += new global::System.EventHandler(this.OnExpander1Activated);
		}
	}
}
#pragma warning restore 436
