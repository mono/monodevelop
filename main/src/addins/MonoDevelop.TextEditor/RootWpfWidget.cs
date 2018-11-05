using MonoDevelop.Components.Windows;
using System.Windows.Controls;

namespace MonoDevelop.Ide.Text
{
	class RootWpfWidget : GtkWPFWidget
	{
		public RootWpfWidget (Control control) : base(control)
		{
		}

		protected override void RepositionWpfWindow ()
		{
			var scale = MonoDevelop.Components.GtkWorkarounds.GetScaleFactor (this);
			RepositionWpfWindow (scale, scale);
		}
	}
}