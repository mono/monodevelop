#pragma warning disable 436

namespace MonoDevelop.Deployment.Gui
{
	internal partial class PackagingFeatureWidget
	{
		private global::Gtk.VBox box;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Deployment.Gui.PackagingFeatureWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Deployment.Gui.PackagingFeatureWidget";
			// Container child MonoDevelop.Deployment.Gui.PackagingFeatureWidget.Gtk.Container+ContainerChild
			this.box = new global::Gtk.VBox();
			this.box.Name = "box";
			this.box.Spacing = 6;
			this.Add(this.box);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
