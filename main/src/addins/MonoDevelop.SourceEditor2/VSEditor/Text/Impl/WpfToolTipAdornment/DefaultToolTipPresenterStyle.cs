namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System.ComponentModel.Composition;
    using Xwt.Drawing;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ToolTipPresenterStyle))]
    [Name("default")]
    internal sealed class DefaultToolTipPresenterStyle : ToolTipPresenterStyle
    {
        public DefaultToolTipPresenterStyle()
        {
            this.BorderBrush = Colors.Black;
            this.BackgroundBrush = Colors.LightGray;
        }
    }
}
