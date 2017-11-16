#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class GtkProjectFolderPreviewWidget
	{
		private global::Gtk.VBox mainVBox;

		private global::Gtk.HBox previewLabelHBox;

		private global::Gtk.Label previewLabel;

		private global::Gtk.Label previewPaddingLabel;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView folderTreeView;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.GtkProjectFolderPreviewWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.GtkProjectFolderPreviewWidget";
			// Container child MonoDevelop.Ide.Projects.GtkProjectFolderPreviewWidget.Gtk.Container+ContainerChild
			this.mainVBox = new global::Gtk.VBox();
			this.mainVBox.Name = "mainVBox";
			this.mainVBox.Spacing = 6;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.previewLabelHBox = new global::Gtk.HBox();
			this.previewLabelHBox.Name = "previewLabelHBox";
			this.previewLabelHBox.Spacing = 6;
			// Container child previewLabelHBox.Gtk.Box+BoxChild
			this.previewLabel = new global::Gtk.Label();
			this.previewLabel.Name = "previewLabel";
			this.previewLabel.LabelProp = "<span weight=\'bold\' foreground=\'#555555\'>PREVIEW</span>";
			this.previewLabel.UseMarkup = true;
			this.previewLabelHBox.Add(this.previewLabel);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.previewLabelHBox[this.previewLabel]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child previewLabelHBox.Gtk.Box+BoxChild
			this.previewPaddingLabel = new global::Gtk.Label();
			this.previewPaddingLabel.Name = "previewPaddingLabel";
			this.previewLabelHBox.Add(this.previewPaddingLabel);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.previewLabelHBox[this.previewPaddingLabel]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			this.mainVBox.Add(this.previewLabelHBox);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.previewLabelHBox]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.folderTreeView = new global::Gtk.TreeView();
			this.folderTreeView.Name = "folderTreeView";
			this.folderTreeView.EnableSearch = false;
			this.folderTreeView.HeadersVisible = false;
			this.GtkScrolledWindow.Add(this.folderTreeView);
			this.mainVBox.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.GtkScrolledWindow]));
			w5.Position = 1;
			this.Add(this.mainVBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
