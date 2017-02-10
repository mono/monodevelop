using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines the ways to display a point or range.
    /// </summary>
    public enum HowToShow
    {
        /// <summary>
        /// Show the point or start of the range as it is on screen, or scroll the
        /// view the minimal amount in order to bring the point or range into view.
        /// </summary>
        AsIs,

        /// <summary>
        /// Show the point or start of the range centered on the screen.
        /// </summary>
        Centered,

        /// <summary>
        /// Show the point or start of the range on the first line of the view.
        /// </summary>
        OnFirstLineOfView,
    }
}
