// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Handles sending out pre/post mouse events and drag/drop events.
    /// </summary>
    internal class WpfMouseProcessor : IDisposable
    {
        #region Protected Members
        protected bool _isDisposed = false;
        protected IList<IMouseProcessor> _mouseProcessors;
        protected UIElement _element;
        protected GuardedOperations _guardedOperations;
        protected FrameworkElement _manipulationElement;
        #endregion // Private Members

        #region Public Surface
        /// <summary>
        /// Creates a new Mouse binding to relay WPF mouse events from a UIElement 
        /// to a collection of IMouseProcessors.
        /// </summary>
        /// <param name="element">
        /// UIElement on which to listen for mouse events.
        /// </param>
        /// <param name="mouseProcessors">
        /// The mouse processors to relay events to.
        /// </param>
        public WpfMouseProcessor(UIElement element, IList<IMouseProcessor> mouseProcessors, GuardedOperations guardedOperations, FrameworkElement manipulationElement = null)
            : this(element, guardedOperations, manipulationElement)
        {
            _mouseProcessors = mouseProcessors;
        }

        protected WpfMouseProcessor(UIElement element, GuardedOperations guardedOperations, FrameworkElement manipulationElement)
        {
            _element = element;
            _guardedOperations = guardedOperations;

            if (manipulationElement != null)
            {
                _manipulationElement = manipulationElement;
                // Enable touch manipulation on the ManipulationLayer Canvas and hook up touch event handlers
                _manipulationElement.IsManipulationEnabled = true;
                _manipulationElement.AddHandler(FrameworkElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(UIElement_ManipulationInertiaStarting), handledEventsToo: true);
                _manipulationElement.AddHandler(FrameworkElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(UIElement_ManipulationStarting), handledEventsToo: true);
                _manipulationElement.AddHandler(FrameworkElement.ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(UIElement_ManipulationCompleted), handledEventsToo: true);
                _manipulationElement.AddHandler(FrameworkElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(UIElement_ManipulationDelta), handledEventsToo: true);
                _manipulationElement.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(UIElement_TouchDown), handledEventsToo: true);
                _manipulationElement.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(UIElement_TouchUp), handledEventsToo: true);
                _manipulationElement.AddHandler(FrameworkElement.StylusSystemGestureEvent, new StylusSystemGestureEventHandler(UIElement_StylusSystemGesture), handledEventsToo: true);
            }

            // mouse button related events
            _element.AddHandler(FrameworkElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(UIElement_MouseLeftButtonDown), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(UIElement_MouseLeftButtonUp), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(UIElement_MouseRightButtonDown), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseRightButtonUpEvent, new MouseButtonEventHandler(UIElement_MouseRightButtonUp), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseDownEvent, new MouseButtonEventHandler(UIElement_MouseDown), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseUpEvent, new MouseButtonEventHandler(UIElement_MouseUp), handledEventsToo: true);
            
            _element.AddHandler(FrameworkElement.MouseMoveEvent, new MouseEventHandler(UIElement_MouseMove), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseWheelEvent, new MouseWheelEventHandler(UIElement_MouseWheel), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseEnterEvent, new MouseEventHandler(UIElement_MouseEnter), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.MouseLeaveEvent, new MouseEventHandler(UIElement_MouseLeave), handledEventsToo: true);

            // drag and drop related events
            _element.AddHandler(FrameworkElement.DragEnterEvent, new DragEventHandler(UIElement_DragEnter), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.DragLeaveEvent, new DragEventHandler(UIElement_DragLeave), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.DragOverEvent, new DragEventHandler(UIElement_DragOver), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.DropEvent, new DragEventHandler(UIElement_Drop), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.GiveFeedbackEvent, new GiveFeedbackEventHandler(UIElement_GiveFeedback), handledEventsToo: true);
            _element.AddHandler(FrameworkElement.QueryContinueDragEvent, new QueryContinueDragEventHandler(UIElement_QueryContinueDrag), handledEventsToo: true);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                GC.SuppressFinalize(this);

                if (_manipulationElement != null)
                {
                    _manipulationElement.ManipulationInertiaStarting -= UIElement_ManipulationInertiaStarting;
                    _manipulationElement.ManipulationStarting -= UIElement_ManipulationStarting;
                    _manipulationElement.ManipulationCompleted -= UIElement_ManipulationCompleted;
                    _manipulationElement.ManipulationDelta -= UIElement_ManipulationDelta;
                    _manipulationElement.TouchDown -= UIElement_TouchDown;
                    _manipulationElement.TouchUp -= UIElement_TouchUp;
                    _manipulationElement.StylusSystemGesture -= UIElement_StylusSystemGesture;
                }

                _element.MouseLeftButtonDown -= UIElement_MouseLeftButtonDown;
                _element.MouseLeftButtonUp -= UIElement_MouseLeftButtonUp;
                _element.MouseRightButtonDown -= UIElement_MouseRightButtonDown;
                _element.MouseRightButtonUp -= UIElement_MouseRightButtonUp;
                _element.MouseDown -= UIElement_MouseDown;
                _element.MouseUp -= UIElement_MouseUp;

                _element.MouseMove -= UIElement_MouseMove;
                _element.MouseWheel -= UIElement_MouseWheel;
                _element.MouseEnter -= UIElement_MouseEnter;
                _element.MouseLeave -= UIElement_MouseLeave;

                // drag and drop related events
                _element.DragEnter -= UIElement_DragEnter;
                _element.DragLeave -= UIElement_DragLeave;
                _element.DragOver -= UIElement_DragOver;
                _element.Drop -= UIElement_Drop;
                _element.GiveFeedback -= UIElement_GiveFeedback;
                _element.QueryContinueDrag -= UIElement_QueryContinueDrag;
            }
        }

        #endregion

        /// <summary>
        /// Take focus for our UIElement, depending on the state of focus currently
        /// and the state of the mouse event.
        /// </summary>
        /// <returns><c>true</c> if focus was taken.</returns>
        private bool TakeFocusFromMouseEvent(MouseEventArgs e)
        {
            // There are two cases where we take focus:
            // 1) If the event is unhandled and our UIElement doesn't have focus
            // 2) If the event is *handled* and the focus isn't somewhere within our UIElement
            //
            // Notably, we *don't* take focus if the event is handled and focus is already
            // within the UIElement.  That likely means that something else within the UIElement is
            // handling events, and we don't want to steal focus away from it.

            if (_element.Focusable &&
                ((!e.Handled && !_element.IsKeyboardFocused) ||
                 ( e.Handled && !_element.IsKeyboardFocusWithin)))
            {
                Keyboard.Focus(_element);
                return true;
            }

            return false;
        }

        #region Default Handlers
        // Default handlers (called only if none of the mouse processors handle the preprocess event).
        protected virtual void DefaultMouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e) { }
        protected virtual void DefaultMouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e) { }
        protected virtual void DefaultMouseLeftButtonUpPostprocessor(MouseButtonEventArgs e) { }
        protected virtual void DefaultMouseRightButtonDownHandler(object sender, MouseButtonEventArgs e) { }
        protected virtual void DefaultMouseRightButtonUpHandler(object sender, MouseButtonEventArgs e) { }
        protected virtual void DefaultMouseDownHandler(object sender, MouseButtonEventArgs e) { }
        protected virtual void DefaultMouseUpHandler(object sender, MouseButtonEventArgs e) { }
        protected virtual void DefaultMouseMoveHandler(object sender, MouseEventArgs e) { }
        protected virtual void DefaultMouseWheelHandler(object sender, MouseWheelEventArgs e) { }
        protected virtual void DefaultMouseEnterHandler(object sender, MouseEventArgs e) { }
        protected virtual void DefaultMouseLeaveHandler(object sender, MouseEventArgs e) { }
        protected virtual void DefaultDragEnterHandler(object sender, DragEventArgs e) { }
        protected virtual void DefaultDragLeaveHandler(object sender, DragEventArgs e) { }
        protected virtual void DefaultDragOverHandler(object sender, DragEventArgs e) { }
        protected virtual void DefaultDropHandler(object sender, DragEventArgs e) { }
        protected virtual void DefaultGiveFeedbackHandler(object sender, GiveFeedbackEventArgs e) { }
        protected virtual void DefaultQueryContinueDragHandler(object sender, QueryContinueDragEventArgs e) { }
        protected virtual void DefaultManipulationInertiaStartingHandler(object sender, ManipulationInertiaStartingEventArgs e) { }
        protected virtual void DefaultManipulationStartingHandler(object sender, ManipulationStartingEventArgs e) { }
        protected virtual void DefaultManipulationCompletedHandler(object sender, ManipulationCompletedEventArgs e) { }
        protected virtual void DefaultManipulationDeltaHandler(object sender, ManipulationDeltaEventArgs e) { }
        
        protected virtual void DefaultTouchDownHandler(object sender, TouchEventArgs e) { }
        protected virtual void DefaultTouchUpHandler(object sender, TouchEventArgs e) { }
        protected virtual void DefaultStylusSystemGestureHandler(object sender, StylusSystemGestureEventArgs e) { }
        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles events coming off of the Mouse Wheel
        /// </summary>
        public void UIElement_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseWheel(e), () => DefaultMouseWheelHandler(sender, e), (p) => p.PostprocessMouseWheel(e));
        }

        /// <summary>
        /// Handles the Mouse Left Button Down Event
        /// 
        /// Initializes a caret repositioning or selection start.
        /// </summary>
        public void UIElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            bool tookFocus = TakeFocusFromMouseEvent(e);

            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseLeftButtonDown(e), () => DefaultMouseLeftButtonDownHandler(sender, e), (p) => p.PostprocessMouseLeftButtonDown(e));

            e.Handled |= tookFocus;
        }

        /// <summary>
        /// Handles the Mouse Down Event
        /// </summary>
        public void UIElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bool tookFocus = false;
            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
            {
                tookFocus = TakeFocusFromMouseEvent(e);
            }

            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseDown(e), () => DefaultMouseDownHandler(sender, e), (p) => p.PostprocessMouseDown(e));

            e.Handled |= tookFocus;
        }

        /// <summary>
        /// Handles the Mouse Up Event
        /// </summary>
        public void UIElement_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseUp(e), () => DefaultMouseUpHandler(sender, e), (p) => p.PostprocessMouseUp(e));
        }

        /// <summary>
        /// Handles the Mouse Move Event
        /// 
        /// Tracks selection if the mouse Left button is held.
        /// </summary>
        public void UIElement_MouseMove(object sender, MouseEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseMove(e), () => DefaultMouseMoveHandler(sender, e), (p) => p.PostprocessMouseMove(e));
        }

        /// <summary>
        /// Handles the Mouse Left Button Up Event
        /// 
        /// Finishes a selection drag or sets the caret
        /// </summary>
        public void UIElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Note: This mouse handler was left inline rather than refactored to use MouseProcessorHandler 
            // because of the DefaultMouseLeftButtonUpPostprocessor call before the IMouseProcessor handlers.

            // Preprocess mouse left button up event
            foreach (IMouseProcessor mouseProcessor in _mouseProcessors)
            {
                if (e.Handled)
                    break;
                _guardedOperations.CallExtensionPoint(() => mouseProcessor.PreprocessMouseLeftButtonUp(e));
            }

            // Default handler
            if (!e.Handled)
            {
                DefaultMouseLeftButtonUpHandler(sender, e);
            }

            // Let any derived class handle this first
            DefaultMouseLeftButtonUpPostprocessor(e);

            // Post process event
            foreach (IMouseProcessor mouseProcessor in _mouseProcessors)
            {
                _guardedOperations.CallExtensionPoint(() => mouseProcessor.PostprocessMouseLeftButtonUp(e));
            }
        }

        /// <summary>
        /// Handles the Mouse Leave event
        /// </summary>
        public void UIElement_MouseLeave(object sender, MouseEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseLeave(e), () => DefaultMouseLeaveHandler(sender, e), (p) => p.PostprocessMouseLeave(e));
        }

        /// <summary>
        /// Handles the Mouse Enter event
        /// </summary>
        public void UIElement_MouseEnter(object sender, MouseEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseEnter(e), () => DefaultMouseEnterHandler(sender, e), (p) => p.PostprocessMouseEnter(e));
        }

        /// <summary>
        /// Handles the Mouse Right button up event
        /// </summary>
        public void UIElement_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseRightButtonUp(e), () => DefaultMouseRightButtonUpHandler(sender, e), (p) => p.PostprocessMouseRightButtonUp(e));
        }

        /// <summary>
        /// Handles the Mouse Right button down event
        /// </summary>
        public void UIElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            bool tookFocus = TakeFocusFromMouseEvent(e);

            this.MouseProcessorHandler(e, (p) => p.PreprocessMouseRightButtonDown(e), () => DefaultMouseRightButtonDownHandler(sender, e), (p) => p.PostprocessMouseRightButtonDown(e));

            e.Handled |= tookFocus;
        }

        /// <summary>
        /// Handles drag and drop's drop event
        /// </summary>
        public void UIElement_Drop(object sender, DragEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessDrop(e), () => DefaultDropHandler(sender, e), (p) => p.PostprocessDrop(e));
        }

        /// <summary>
        /// Handles a drap and drop DragOver event, this is a bubbling event
        /// </summary>
        public void UIElement_DragOver(object sender, DragEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessDragOver(e), () => DefaultDragOverHandler(sender, e), (p) => p.PostprocessDragOver(e));
        }

        /// <summary>
        /// Handles a drag leave event; this is fired when the mouse leaves the text view
        /// while a drag and drop operation is taking place
        /// </summary>
        public void UIElement_DragLeave(object sender, DragEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessDragLeave(e), () => DefaultDragLeaveHandler(sender, e), (p) => p.PostprocessDragLeave(e));
        }

        /// <summary>
        /// Handles a drag and drop drag enter event. This is fired when a drag and drop operation
        /// is taking place and the mouse enters the text view.
        /// </summary>
        public void UIElement_DragEnter(object sender, DragEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessDragEnter(e), () => DefaultDragEnterHandler(sender, e), (p) => p.PostprocessDragEnter(e));
        }

        /// <summary>
        /// This event is raised when a change in the keyboard state of a drag operation is occured. It's raised to query the drop target
        /// for what it wants to do with the operation based on the new change in the keyboard settings.
        /// </summary>
        public void UIElement_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            this.MouseProcessorHandler(e, (p) => p.PreprocessQueryContinueDrag(e), () => DefaultQueryContinueDragHandler(sender, e), (p) => p.PostprocessQueryContinueDrag(e));
        }

        /// <summary>
        /// When a new drag and drop operation is initiated, this event is fired on the the drop target to see whether it supports the 
        /// drop or if it wishes to decline it.
        /// </summary>
        public void UIElement_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessGiveFeedback(e), () => DefaultGiveFeedbackHandler(sender, e), (p) => p.PostprocessGiveFeedback(e));
        }

        public void UIElement_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessManipulationInertiaStarting(e), () => DefaultManipulationInertiaStartingHandler(sender, e), (p) => p.PostprocessManipulationInertiaStarting(e));
        }

        public void UIElement_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessManipulationStarting(e), () => DefaultManipulationStartingHandler(sender, e), (p) => p.PostprocessManipulationStarting(e));
        }

        public void UIElement_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessManipulationCompleted(e), () => DefaultManipulationCompletedHandler(sender, e), (p) => p.PostprocessManipulationCompleted(e));
        }

        public void UIElement_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessManipulationDelta(e), () => DefaultManipulationDeltaHandler(sender, e), (p) => p.PostprocessManipulationDelta(e));
        }

        public void UIElement_TouchDown(object sender, TouchEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessTouchDown(e), () => DefaultTouchDownHandler(sender, e), (p) => p.PostprocessTouchDown(e));
        }

        public void UIElement_TouchUp(object sender, TouchEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessTouchUp(e), () => DefaultTouchDownHandler(sender, e), (p) => p.PostprocessTouchUp(e));
        }

        public void UIElement_StylusSystemGesture(object sender, StylusSystemGestureEventArgs e)
        {
            this.MouseProcessor2Handler(e, (p) => p.PreprocessStylusSystemGesture(e), () => DefaultStylusSystemGestureHandler(sender, e), (p) => p.PostprocessStylusSystemGesture(e));
        }

        #endregion // Event Handlers

        private void MouseProcessorHandler(RoutedEventArgs e, Action<IMouseProcessor> preprocess, Action defaultAction, Action<IMouseProcessor> postprocess)
        {
            foreach (IMouseProcessor mouseProcessor in _mouseProcessors)
            {
                if (e.Handled)
                    break;

                _guardedOperations.CallExtensionPoint(() => preprocess(mouseProcessor));
            }

            // Default handler
            if (!e.Handled)
            {
                defaultAction();
            }

            // Post process Event
            foreach (IMouseProcessor mouseProcessor in _mouseProcessors)
            {
                _guardedOperations.CallExtensionPoint(() => postprocess(mouseProcessor));

            }
        }

        private void MouseProcessor2Handler(RoutedEventArgs e, Action<IMouseProcessor2> preprocess, Action defaultAction, Action<IMouseProcessor2> postprocess)
        {
            foreach (IMouseProcessor mouseProcessor in _mouseProcessors)
            {
                if (e.Handled)
                    break;

                IMouseProcessor2 mouseProcessor2 = mouseProcessor as IMouseProcessor2;
                if (mouseProcessor2 != null)
                {
                    _guardedOperations.CallExtensionPoint(() => preprocess(mouseProcessor2));
                }
            }

            // Default handler
            if (!e.Handled)
            {
                defaultAction();
            }

            // Post process Event
            foreach (IMouseProcessor mouseProcessor in _mouseProcessors)
            {
                IMouseProcessor2 mouseProcessor2 = mouseProcessor as IMouseProcessor2;
                if (mouseProcessor2 != null)
                {
                    _guardedOperations.CallExtensionPoint(() => postprocess(mouseProcessor2));
                }
            }
        }
    }
}
