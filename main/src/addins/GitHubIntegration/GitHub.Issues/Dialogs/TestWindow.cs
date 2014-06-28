using System;

namespace GitHub.Issues
{
	public partial class TestWindow : Gtk.Window
	{
		public TestWindow () :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();
		}
	}
}

