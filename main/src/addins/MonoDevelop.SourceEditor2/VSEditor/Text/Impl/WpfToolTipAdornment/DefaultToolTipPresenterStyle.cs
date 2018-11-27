namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System.ComponentModel.Composition;
    using Xwt.Drawing;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ToolTipPresenterStyle2))]
    [Name("default")]
    internal sealed class DefaultToolTipPresenterStyle : ToolTipPresenterStyle2
    {
        public DefaultToolTipPresenterStyle()
        {
            this.BorderBrush = Colors.Black;
            this.BackgroundBrush = Colors.LightGray;
        }
    }
}
