////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides a way to intercede in the command-handling chain to handle keyboard commands.
    /// </summary>
    public interface IIntellisenseCommandTarget
    {
        /// <summary>
        /// Executes a user-initiated keyboard command.  
        /// </summary>
        /// <param name="command">The keyboard command to execute.</param>
        /// <returns><c>true</c> if the command was handled, <c>false</c> otherwise.</returns>
        /// <remarks>
        /// Keyboard commands are normally handled by the underlying view, but
        /// IntelliSense presenters may intercede in the command-handling chain in order to handle certain keyboard commands.
        /// </remarks>
        bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command);
    }
}
