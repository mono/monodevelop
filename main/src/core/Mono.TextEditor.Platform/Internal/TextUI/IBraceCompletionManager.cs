// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.BraceCompletion
{
    using System;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A per text view manager for brace completion.
    /// </summary>
    public interface IBraceCompletionManager
    {
        /// <summary>
        /// Returns true if brace completion is enabled.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Returns true if there are currently active sessions.
        /// </summary>
        bool HasActiveSessions { get; }

        /// <summary>
        /// Opening brace characters the brace completion manager is currently registered to handle.
        /// </summary>
        string OpeningBraces { get; }

        /// <summary>
        /// Closing brace characters the brace completion manager is currently registered to handle.
        /// </summary>
        string ClosingBraces { get; }

        /// <summary>
        /// Called by the editor when a character has been typed and before it is 
        /// inserted into the buffer.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the closing brace character from being 
        /// inserted into the buffer.</param>
        void PreTypeChar(char character, out bool handledCommand);

        /// <summary>
        /// Called by the editor after a character has been typed.
        /// </summary>
        void PostTypeChar(char character);

        /// <summary>
        /// Called by the editor when tab has been pressed and before it is inserted into the buffer.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the tab from being inserted into the buffer.</param>
        void PreTab(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the tab has been inserted.
        /// </summary>
        void PostTab();

        /// <summary>
        /// Called by the editor before the character has been removed.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the backspace action from completing.</param>
        void PreBackspace(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the character has been removed.
        /// </summary>
        void PostBackspace();

        /// <summary>
        /// Called by the editor when delete is pressed within the session.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the deletion.</param>
        void PreDelete(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the delete action.
        /// </summary>
        void PostDelete();

        /// <summary>
        /// Called by the editor when return is pressed within the session.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the new line insertion.</param>
        void PreReturn(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the new line has been inserted.
        /// </summary>
        void PostReturn();

    }
}
