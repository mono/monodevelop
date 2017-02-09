// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Provides touch related extensions for mouse bindings.
    /// </summary>
    public interface IMouseProcessor2 : IMouseProcessor
    {
        /// <summary>
        /// Handles a touch down event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessTouchDown(TouchEventArgs e);

        /// <summary>
        /// Handles a touch down event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessTouchDown(TouchEventArgs e);

        /// <summary>
        /// Handles a touch up event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessTouchUp(TouchEventArgs e);

        /// <summary>
        /// Handles a touch up event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessTouchUp(TouchEventArgs e);

        /// <summary>
        /// Handles a Stylus SystemGesture event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessStylusSystemGesture(StylusSystemGestureEventArgs e);

        /// <summary>
        /// Handles a Stylus SystemGesture event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessStylusSystemGesture(StylusSystemGestureEventArgs e);

        /// <summary>
        /// Handles a touch manipulation inertia starting event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e);

        /// <summary>
        /// Handles a touch manipulation inertia starting event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e);

        /// <summary>
        /// Handles a touch manipulation starting event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessManipulationStarting(ManipulationStartingEventArgs e);

        /// <summary>
        /// Handles a touch manipulation starting event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessManipulationStarting(ManipulationStartingEventArgs e);


        /// <summary>
        /// Handles a touch manipulation delta event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessManipulationDelta(ManipulationDeltaEventArgs e);

        /// <summary>
        /// Handles a touch manipulation delta event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessManipulationDelta(ManipulationDeltaEventArgs e);
    
        /// <summary>
        /// Handles a touch manipulation completed event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessManipulationCompleted(ManipulationCompletedEventArgs e);

        /// <summary>
        /// Handles a touch manipulation completed event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessManipulationCompleted(ManipulationCompletedEventArgs e);
    }
}