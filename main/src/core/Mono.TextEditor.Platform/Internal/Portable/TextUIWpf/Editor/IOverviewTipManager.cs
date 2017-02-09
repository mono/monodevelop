// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.OverviewMargin
{
    using Microsoft.VisualStudio.Text.Editor;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Provides information about what <see cref="ToolTip"/> should be shown when the mouse moves over the overview margin.
    /// </summary>
    public interface IOverviewTipManager
    {
        /// <summary>
        /// Update the ToolTip to reflect an appropriate tip for the specified mouse position.
        /// </summary>
        /// <param name="margin">The <see cref="IVerticalScrollBar"/> for which the tip should be displayed.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> that cause the tip to be displayed.</param>
        /// <param name="tip">The ToolTip to update (manager must set the content, size, etc.).</param>
        /// <returns>True if this manager has modified the tip.</returns>
        bool UpdateTip(IVerticalScrollBar margin, MouseEventArgs e, ToolTip tip);
    }
}
