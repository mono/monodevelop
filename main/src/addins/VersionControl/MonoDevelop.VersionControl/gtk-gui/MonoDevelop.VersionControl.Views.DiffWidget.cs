#pragma warning disable 436

namespace MonoDevelop.VersionControl.Views
{
	internal partial class DiffWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Notebook notebook1;

		private global::Gtk.VBox vboxComparisonView;

		private global::Gtk.Label label1;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.Label label3;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Views.DiffWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.VersionControl.Views.DiffWidget";
			// Container child MonoDevelop.VersionControl.Views.DiffWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.notebook1 = new global::Gtk.Notebook();
			this.notebook1.CanFocus = true;
			this.notebook1.Name = "notebook1";
			this.notebook1.CurrentPage = 0;
			this.notebook1.ShowBorder = false;
			this.notebook1.ShowTabs = false;
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.vboxComparisonView = new global::Gtk.VBox();
			this.vboxComparisonView.Name = "vboxComparisonView";
			this.vboxComparisonView.Spacing = 6;
			this.notebook1.Add(this.vboxComparisonView);
			// Notebook tab
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("page2");
			this.notebook1.SetTabLabel(this.vboxComparisonView, this.label1);
			this.label1.ShowAll();
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			this.notebook1.Add(this.scrolledwindow1);
			global::Gtk.Notebook.NotebookChild w2 = ((global::Gtk.Notebook.NotebookChild)(this.notebook1[this.scrolledwindow1]));
			w2.Position = 1;
			// Notebook tab
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("page2");
			this.notebook1.SetTabLabel(this.scrolledwindow1, this.label3);
			this.label3.ShowAll();
			this.vbox2.Add(this.notebook1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.notebook1]));
			w3.Position = 0;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
