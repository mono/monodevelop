namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Utilities;

    [Export]
    internal sealed class ToolTipStyleFactory2
    {
        private ToolTipPresenterStyle2 style;

        [ImportMany]
#pragma warning disable 649 // unassigned
        private List<Lazy<ToolTipPresenterStyle2, IOrderable>> unorderedPresenterStyles;
#pragma warning restore 649

        public ToolTipPresenterStyle2 Style
        {
            get
            {
                if (this.style == null)
                {
                    this.style = Orderer.Order(this.unorderedPresenterStyles).FirstOrDefault()?.Value;

                    if (this.style == null)
                    {
                        throw new ArgumentNullException($"No exports of type {nameof(ToolTipPresenterStyle2)}");
                    }
                }

                return this.style;
            }
        }
    }
}
