////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a set of smart tag actions.
    /// </summary>
    [Obsolete(SmartTag.DeprecationMessage)]
    public sealed class SmartTagActionSet
    {
        ReadOnlyCollection<ISmartTagAction> _actions;

        /// <summary>
        /// Constructions a <see cref="SmartTagActionSet"/>.
        /// </summary>
        /// <param name="actions"></param>
        public SmartTagActionSet(ReadOnlyCollection<ISmartTagAction> actions)
        {
            _actions = actions;
        }

        /// <summary>
        /// The collection of smart tag actions.
        /// </summary>
        public ReadOnlyCollection<ISmartTagAction> Actions
        {
            get { return _actions; }
        }
    }
}
