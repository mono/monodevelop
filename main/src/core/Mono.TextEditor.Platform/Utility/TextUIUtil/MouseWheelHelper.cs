// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.VisualStudio.Text.Editor;

    //Manages the state for handling mouse wheel events.
    internal class MouseWheelHelper
    {
        WeakReference _lastSender = new WeakReference(null);
        int _accumulatedMouseDelta = 0;
        bool _lastScrollByPages;

        public void HandleMouseWheelEvent(IWpfTextView view, object sender, MouseWheelEventArgs e)
        {
            if (_lastSender.Target != sender)
            {
                _lastSender = new WeakReference(sender);
                _accumulatedMouseDelta = 0;
            }

            if ((_accumulatedMouseDelta > 0) != (e.Delta > 0))
            {
                //If the scrolling direction changed, remove the accumulated Delta
                _accumulatedMouseDelta = 0;
            }

            bool scrollByPages = (SystemParameters.WheelScrollLines == -1);

            if (_lastScrollByPages != scrollByPages)
            {
                _lastScrollByPages = scrollByPages;
                _accumulatedMouseDelta = 0;
            }

            _accumulatedMouseDelta += e.Delta;
            int units = _accumulatedMouseDelta / Mouse.MouseWheelDeltaForOneLine;  //This truncates

            if (units != 0)
            {
                _accumulatedMouseDelta -= units * Mouse.MouseWheelDeltaForOneLine;

                if (scrollByPages)
                {
                    MouseWheelHelper.ScrollByPages(view, units);
                }
                else
                {
                    MouseWheelHelper.ScrollByLines(view, units * SystemParameters.WheelScrollLines);
                }
            }

            e.Handled = true;
        }

        public static void ScrollByPages(IWpfTextView view, int pages)
        {
            //Scroll by pages.
            IViewScroller scroller = view.ViewScroller;
            for (int i = Math.Abs(pages); (i > 0); --i)
            {
                scroller.ScrollViewportVerticallyByPage((pages > 0) ? ScrollDirection.Up : ScrollDirection.Down);
            }
        }

        public static void ScrollByLines(IWpfTextView view, int lines)
        {
            view.ViewScroller.ScrollViewportVerticallyByPixels(((double)lines) * view.LineHeight);
        }
    }
}
