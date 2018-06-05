namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Utilities;

    [Export]
    internal sealed class ToolTipStyleFactory
    {
        private ToolTipPresenterStyle style;

        [ImportMany]
        #pragma warning disable 649 // unassigned
        private List<Lazy<ToolTipPresenterStyle, IOrderable>> unorderedPresenterStyles;
        #pragma warning restore 649

        public ToolTipPresenterStyle Style
        {
            get
            {
                if (this.style == null)
                {
                    this.style = Orderer.Order(this.unorderedPresenterStyles).FirstOrDefault()?.Value;

                    if (this.style == null)
                    {
                        throw new ArgumentNullException($"No exports of type {nameof(ToolTipPresenterStyle)}");
                    }
                }

                return this.style;
            }
        }
    }
}
