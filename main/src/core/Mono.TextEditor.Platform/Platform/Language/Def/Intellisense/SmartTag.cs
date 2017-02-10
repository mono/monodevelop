////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// A tag used to contain actions that may be performed on a span of text. This tag is consumed by the Intellisense
    /// infrastructure and will spawn smart tag Intellisense sessions.
    /// </summary>
    [Obsolete(SmartTag.DeprecationMessage)]
    public class SmartTag : ITag
    {
        internal const string DeprecationMessage = "This API is deprecated in this version of the Visual Studio SDK, and will be retired in a future version. To find out more about the replacement API, Light Bulb, refer to http://go.microsoft.com/fwlink/?LinkId=394601.";

        /// <summary>
        /// Initializes a new instance of <see cref="SmartTag"/>.
        /// </summary>
        /// <param name="smartTagType">The type of smart tag session that should be created.</param>
        /// <param name="actionSets">The set of actions that should be a part of the smart tag session.</param>
        public SmartTag(SmartTagType smartTagType, ReadOnlyCollection<SmartTagActionSet> actionSets)
        {
            if (actionSets == null)
                throw new ArgumentNullException("actionSets");

            this.SmartTagType = smartTagType;
            this.ActionSets = actionSets;
        }

        /// <summary>
        /// The type of smart tag session that should be created.
        /// </summary>
        public SmartTagType SmartTagType { get; private set; }

        /// <summary>
        /// The set of actions that should be a part of the smart tag session.
        /// </summary>
        public ReadOnlyCollection<SmartTagActionSet> ActionSets { get; private set; }
    }
}
