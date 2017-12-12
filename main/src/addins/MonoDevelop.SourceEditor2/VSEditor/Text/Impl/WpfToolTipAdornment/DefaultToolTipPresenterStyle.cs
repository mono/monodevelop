namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System.ComponentModel.Composition;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ToolTipPresenterStyle))]
    [Name("default")]
    internal sealed class DefaultToolTipPresenterStyle : ToolTipPresenterStyle
    {
        public DefaultToolTipPresenterStyle()
        {
            this.BorderBrush = Brushes.Black;
            this.BackgroundBrush = Brushes.LightGray;
        }
    }
}
