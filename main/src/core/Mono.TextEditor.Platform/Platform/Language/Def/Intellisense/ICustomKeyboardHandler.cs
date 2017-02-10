////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a custom handler of keyboard events
    /// </summary>
    public interface ICustomKeyboardHandler
    {
        /// <summary>
        /// Signals the handler that it's ok to begin capturing keyboard events.
        /// </summary>
        bool CaptureKeyboard();

        /// <summary>
        /// Signals the handler that it should cease capturing keyboard events.
        /// </summary>
        void ReleaseKeyboard();
    }
}
