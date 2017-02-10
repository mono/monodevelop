using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// The set of smart tag session states.
    /// </summary>
    [Obsolete(SmartTag.DeprecationMessage)]
    public enum SmartTagState
    {
        /// <summary>
        /// The session is rendered in collapsed mode, which in the default presenter is indicated by a small colored rectangle
        /// </summary>
        Collapsed,

        /// <summary>
        /// The session is neither collapsed nor expanded. In the default presenter, this is indicated by a button
        /// but no action menu.
        /// </summary>
        Intermediate,

        /// <summary>
        /// The session is rendered in expanded mode, which in the default presenter is indicated by a menu from which the user
        /// can select actions.
        /// </summary>
        Expanded
    }
}
