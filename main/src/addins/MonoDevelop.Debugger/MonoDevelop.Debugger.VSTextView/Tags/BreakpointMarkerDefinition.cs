using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (EditorFormatDefinition))]
	[Name (BreakpointTag.TagId)]
	[UserVisible (true)]
	[Priority (1)] // necessary to override the standard one from TextMarkerProviderFactory.cs
	internal class BreakpointMarkerDefinition : MarkerFormatDefinition
	{
		public BreakpointMarkerDefinition ()
		{
			this.ZOrder = 2;
			this.Fill = new SolidColorBrush (Color.FromArgb (204, 150, 58, 70));
			this.Fill.Freeze ();
		}
	}
}
