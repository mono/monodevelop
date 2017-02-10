using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// The set of smart tag session types.
    /// </summary>
    [Obsolete(SmartTag.DeprecationMessage)]
    public enum SmartTagType
    {
        /// <summary>
        /// A general tag that is valid for a long period of time.  
        /// </summary>
        /// <remarks>
        /// This type of tag indicates 
        /// that an action may be performed on a region of text, and is displayed independently of user action.
        /// </remarks>
        Factoid,

        /// <summary>
        /// A tag that is valid only for a specific period of time.  
        /// </summary>
        /// <remarks>
        /// This type of tag is displayed in response to a modification of the
        /// buffer that could trigger additional actions, such as refactorings.
        /// </remarks>
        Ephemeral
    }
}
