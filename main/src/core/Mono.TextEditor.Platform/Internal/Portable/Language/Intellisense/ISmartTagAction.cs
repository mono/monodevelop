////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using System.Collections.ObjectModel;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a smart tag action.  
    /// </summary>
    /// <remarks>
    /// Smart tag sessions contain zero or more actions, which are provided by smart tag sources.
    /// </remarks>
    [Obsolete(SmartTag.DeprecationMessage)]
    public interface ISmartTagAction
    {
        /// <summary>
        /// Gets the list of smart tag action sets contained inside this smart tag action.
        /// </summary>
        ReadOnlyCollection<SmartTagActionSet> ActionSets { get; }

        /// <summary>
        /// Gets image information that is displayed as an icon alongside the display text in the default smart tag.
        /// presenter.
        /// </summary>
        ImageSource Icon { get; }

        /// <summary>
        /// Gets the text that is displayed in the default smart tag presenter.
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// Determines whether the smart tag action is enabled. By default, disabled smart tags are
        /// rendered but not invokable.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// A callback used to invoke the smart tag action.  
        /// </summary>
        /// <remarks>
        /// You should implement this method to perform the action
        /// when this method is called.
        /// </remarks>
        void Invoke();
    }
}
