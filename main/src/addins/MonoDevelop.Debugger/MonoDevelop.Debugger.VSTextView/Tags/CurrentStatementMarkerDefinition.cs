using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (EditorFormatDefinition))]
	[Name (CurrentStatementTag.TagId)]
	[UserVisible (true)]
	[Priority (1)]
	internal class CurrentStatementMarkerDefinition : MarkerFormatDefinition
	{
		public CurrentStatementMarkerDefinition ()
		{
			this.ZOrder = 3;
			this.Fill = new SolidColorBrush (Color.FromArgb (255, 255, 238, 98));
			this.Fill.Freeze ();
		}
	}
}
