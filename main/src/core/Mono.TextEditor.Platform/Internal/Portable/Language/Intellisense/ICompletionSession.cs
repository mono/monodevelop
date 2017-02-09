////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a statement completion session, which is a type of IntelliSense session.
    /// </summary>
    public interface ICompletionSession : IIntellisenseSession
    {
        /// <summary>
        /// Gets the collection of <see cref="CompletionSet"/> objects.
        /// </summary>
        ReadOnlyObservableCollection<CompletionSet> CompletionSets { get; }

        /// <summary>
        /// Gets or sets the selected <see cref="CompletionSet"/>.
        /// </summary>
        CompletionSet SelectedCompletionSet { get; set; }

        /// <summary>
        /// Occurs when the SelectedCompletionSet property changes.
        /// </summary>
        event EventHandler<ValueChangedEventArgs<CompletionSet>> SelectedCompletionSetChanged;

        /// <summary>
        /// Filters the session's completion items, based on the current state of the text buffer.  
        /// </summary>
        /// <remarks>
        /// If a completion's display text
        /// or insertion text contains the text in its applicability span, it remains part of the CompletionSets
        /// collection, otherwise it will be removed. The underlying providers will not be asked for additional completion
        /// information because of this call.
        /// </remarks>
        void Filter();

        /// <summary>
        /// Commits a completion session. The selected completion's insertion text is inserted into the buffer in place of
        /// its applicability span.
        /// </summary>
        void Commit();

        /// <summary>
        /// Occurs after a completion session is committed.
        /// </summary>
        event EventHandler Committed;

        /// <summary>
        /// Determines whether the completion session has been started.
        /// </summary>
        bool IsStarted { get; }
    }
}
