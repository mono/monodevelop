// This file has been generated by the GUI designer. Do not modify.
namespace MonoDevelop.Ide.Fonts
{
	public partial class FontChooserPanelWidget
	{
		private global::Gtk.VBox mainBox;

		protected virtual void Build ()
		{
			MonoDevelop.Components.Gui.Initialize (this);
			// Widget MonoDevelop.Ide.Fonts.FontChooserPanelWidget
			MonoDevelop.Components.BinContainer.Attach (this);
			this.Name = "MonoDevelop.Ide.Fonts.FontChooserPanelWidget";
			// Container child MonoDevelop.Ide.Fonts.FontChooserPanelWidget.Gtk.Container+ContainerChild
			this.mainBox = new global::Gtk.VBox ();
			this.mainBox.Name = "mainBox";
			this.mainBox.Spacing = 6;
			this.Add (this.mainBox);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
		}
	}
}
