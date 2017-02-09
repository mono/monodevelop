////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a MEF service responsible for tracking the keyboard in hosts of the WPF editor.  Keyboard tracking is necessary as
    /// some hosts (such as VisualStudio) do their own keyboard handling, causing inconsistent behavior of WPF elements, even when
    /// they have keyboard focus.  By tracking the keyboard, all keyboard events will be routed to WPF first, giving focused WPF
    /// controls a shot at handling keyboard events.
    /// </summary>
    [CLSCompliant(false)]
    public interface IWpfKeyboardTrackingService
    {
        /// <summary>
        /// Starts tracking the keyboard.  Once called, all keyboard events will be routed to WPF first, before the host application
        /// sees them.
        /// </summary>
        /// <param name="handle">A valid Win32 window handle (HWND) to which messages should be redirected</param>
        /// <param name="messagesToCapture">A list of Win32 messages to redirect to the specified window handle</param>
        void BeginTrackingKeyboard(IntPtr handle, IList<uint> messagesToCapture);

        /// <summary>
        /// Stops tracking the keyboard.
        /// </summary>
        void EndTrackingKeyboard();
    }
}
