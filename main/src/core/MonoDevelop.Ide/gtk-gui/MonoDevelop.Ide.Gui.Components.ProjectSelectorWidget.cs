#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class ProjectSelectorWidget
	{
		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView tree;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Components.ProjectSelectorWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.Components.ProjectSelectorWidget";
			// Container child MonoDevelop.Ide.Gui.Components.ProjectSelectorWidget.Gtk.Container+ContainerChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.tree = new global::Gtk.TreeView();
			this.tree.CanFocus = true;
			this.tree.Name = "tree";
			this.GtkScrolledWindow.Add(this.tree);
			this.Add(this.GtkScrolledWindow);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
