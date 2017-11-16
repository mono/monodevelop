#pragma warning disable 436

namespace MonoDevelop.Deployment.Gui
{
	internal partial class EntrySelectionTree
	{
		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.TreeView tree;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Deployment.Gui.EntrySelectionTree
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Deployment.Gui.EntrySelectionTree";
			// Container child MonoDevelop.Deployment.Gui.EntrySelectionTree.Gtk.Container+ContainerChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.tree = new global::Gtk.TreeView();
			this.tree.CanFocus = true;
			this.tree.Name = "tree";
			this.scrolledwindow1.Add(this.tree);
			this.Add(this.scrolledwindow1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
