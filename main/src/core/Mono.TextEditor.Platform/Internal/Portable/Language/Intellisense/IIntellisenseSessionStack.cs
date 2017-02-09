////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a stack of IntelliSense sessions that manages session activation and currency.  
    /// </summary>
    /// <remarks>
    /// Each IntelliSense session is
    /// part of the stack, whether or not it appears in a TextView popup. The topmost session on the stack has
    /// special privileges, such as being able to capture the keyboard.
    /// </remarks>
    public interface IIntellisenseSessionStack
    {
        /// <summary>
        /// Adds a session to the top of the stack.
        /// </summary>
        /// <param name="session">An <see cref="IIntellisenseSession"/> to add to the top of the stack.</param>
        void PushSession(IIntellisenseSession session);

        /// <summary>
        /// Removes the topmost session from the stack and returns it.
        /// </summary>
        /// <returns>The session that was removed.</returns>
        IIntellisenseSession PopSession();

        /// <summary>
        /// Moves a session already in the session stack to the top of the stack.  The keyboard session will be re-evaluated.
        /// </summary>
        void MoveSessionToTop (IIntellisenseSession session);

        /// <summary>
        /// Gets the list of sessions in the stack, ordered from bottom to top.
        /// </summary>
        ReadOnlyObservableCollection<IIntellisenseSession> Sessions { get; }

        /// <summary>
        /// Gets the topmost session in the stack.
        /// </summary>
        IIntellisenseSession TopSession { get; }

        /// <summary>
        /// Reduces all sessions in the session stack to their collapsed state, or dismisses them if they have no such state.
        /// </summary>
        void CollapseAllSessions();
    }
}
