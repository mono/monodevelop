using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (EditorFormatDefinition))]
	[Name (ReturnStatementTag.TagId)]
	[UserVisible (true)]
	[Priority (1)]
	internal class ReturnStatementMarkerDefinition : MarkerFormatDefinition
	{
		public ReturnStatementMarkerDefinition ()
		{
			this.ZOrder = 4;
			this.Fill = new SolidColorBrush (Color.FromArgb (255, 180, 228, 180));
			this.Fill.Freeze ();
		}
	}
}
