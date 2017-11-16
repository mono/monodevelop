#pragma warning disable 436

namespace MonoDevelop.VersionControl.Views
{
	public partial class LogWidget
	{
		private global::Gtk.UIManager UIManager;

		private global::Gtk.VBox vbox1;

		private global::Gtk.VPaned vpaned1;

		private global::Gtk.HPaned hpaned1;

		private global::Gtk.VBox vbox4;

		private global::Gtk.ScrolledWindow scrolledLoading;

		private global::Gtk.Label label3;

		private global::Gtk.ScrolledWindow scrolledLog;

		private global::Gtk.TreeView treeviewLog;

		private global::Gtk.VBox vbox2;

		private global::Gtk.EventBox commitBox;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Image imageUser;

		private global::Gtk.VBox vbox5;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label labelAuthor;

		private global::Gtk.Label labelRevision;

		private global::Gtk.Label labelDate;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TextView textviewDetails;

		private global::Gtk.ScrolledWindow scrolledwindowFiles;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Views.LogWidget
			Stetic.BinContainer w1 = global::Stetic.BinContainer.Attach(this);
			this.UIManager = new global::Gtk.UIManager();
			global::Gtk.ActionGroup w2 = new global::Gtk.ActionGroup("Default");
			this.UIManager.InsertActionGroup(w2, 0);
			this.Name = "MonoDevelop.VersionControl.Views.LogWidget";
			// Container child MonoDevelop.VersionControl.Views.LogWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.vpaned1 = new global::Gtk.VPaned();
			this.vpaned1.CanFocus = true;
			this.vpaned1.Name = "vpaned1";
			this.vpaned1.Position = 204;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.hpaned1 = new global::Gtk.HPaned();
			this.hpaned1.CanFocus = true;
			this.hpaned1.Name = "hpaned1";
			this.hpaned1.Position = 236;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.scrolledLoading = new global::Gtk.ScrolledWindow();
			this.scrolledLoading.CanFocus = true;
			this.scrolledLoading.Name = "scrolledLoading";
			// Container child scrolledLoading.Gtk.Container+ContainerChild
			global::Gtk.Viewport w3 = new global::Gtk.Viewport();
			w3.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child GtkViewport1.Gtk.Container+ContainerChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Loading...");
			w3.Add(this.label3);
			this.scrolledLoading.Add(w3);
			this.vbox4.Add(this.scrolledLoading);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.scrolledLoading]));
			w6.Position = 0;
			// Container child vbox4.Gtk.Box+BoxChild
			this.scrolledLog = new global::Gtk.ScrolledWindow();
			this.scrolledLog.Name = "scrolledLog";
			// Container child scrolledLog.Gtk.Container+ContainerChild
			this.treeviewLog = new global::Gtk.TreeView();
			this.treeviewLog.CanFocus = true;
			this.treeviewLog.Name = "treeviewLog";
			this.scrolledLog.Add(this.treeviewLog);
			this.vbox4.Add(this.scrolledLog);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.scrolledLog]));
			w8.Position = 1;
			this.hpaned1.Add(this.vbox4);
			global::Gtk.Paned.PanedChild w9 = ((global::Gtk.Paned.PanedChild)(this.hpaned1[this.vbox4]));
			w9.Resize = false;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			// Container child vbox2.Gtk.Box+BoxChild
			this.commitBox = new global::Gtk.EventBox();
			this.commitBox.Name = "commitBox";
			// Container child commitBox.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			this.hbox1.BorderWidth = ((uint)(8));
			// Container child hbox1.Gtk.Box+BoxChild
			this.imageUser = new global::Gtk.Image();
			this.imageUser.WidthRequest = 32;
			this.imageUser.HeightRequest = 32;
			this.imageUser.Name = "imageUser";
			this.hbox1.Add(this.imageUser);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.imageUser]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.labelAuthor = new global::Gtk.Label();
			this.labelAuthor.Name = "labelAuthor";
			this.labelAuthor.Xalign = 0F;
			this.labelAuthor.LabelProp = global::Mono.Unix.Catalog.GetString("Author");
			this.labelAuthor.Selectable = true;
			this.hbox2.Add(this.labelAuthor);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.labelAuthor]));
			w11.Position = 0;
			w11.Expand = false;
			w11.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.labelRevision = new global::Gtk.Label();
			this.labelRevision.Name = "labelRevision";
			this.labelRevision.Xalign = 1F;
			this.labelRevision.LabelProp = global::Mono.Unix.Catalog.GetString("Revision");
			this.labelRevision.Selectable = true;
			this.hbox2.Add(this.labelRevision);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.labelRevision]));
			w12.Position = 1;
			this.vbox5.Add(this.hbox2);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox2]));
			w13.Position = 0;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.labelDate = new global::Gtk.Label();
			this.labelDate.Name = "labelDate";
			this.labelDate.Xalign = 0F;
			this.labelDate.LabelProp = global::Mono.Unix.Catalog.GetString("Date");
			this.labelDate.Selectable = true;
			this.vbox5.Add(this.labelDate);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.labelDate]));
			w14.Position = 1;
			w14.Expand = false;
			w14.Fill = false;
			this.hbox1.Add(this.vbox5);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox5]));
			w15.Position = 1;
			this.commitBox.Add(this.hbox1);
			this.vbox2.Add(this.commitBox);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.commitBox]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.textviewDetails = new global::Gtk.TextView();
			this.textviewDetails.CanFocus = true;
			this.textviewDetails.Name = "textviewDetails";
			this.textviewDetails.Editable = false;
			this.scrolledwindow1.Add(this.textviewDetails);
			this.vbox2.Add(this.scrolledwindow1);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.scrolledwindow1]));
			w19.Position = 1;
			this.hpaned1.Add(this.vbox2);
			this.vpaned1.Add(this.hpaned1);
			global::Gtk.Paned.PanedChild w21 = ((global::Gtk.Paned.PanedChild)(this.vpaned1[this.hpaned1]));
			w21.Resize = false;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.scrolledwindowFiles = new global::Gtk.ScrolledWindow();
			this.scrolledwindowFiles.CanFocus = true;
			this.scrolledwindowFiles.Name = "scrolledwindowFiles";
			this.vpaned1.Add(this.scrolledwindowFiles);
			this.vbox1.Add(this.vpaned1);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.vpaned1]));
			w23.Position = 0;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			w1.SetUiManager(UIManager);
			this.scrolledLoading.Hide();
			this.Show();
			this.labelRevision.ButtonPressEvent += new global::Gtk.ButtonPressEventHandler(this.OnLabelRevisionButtonPressEvent);
		}
	}
}
#pragma warning restore 436
