using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;

namespace Microsoft.VisualStudio.Text.Utilities
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TelemetryLogger
    {
        // This import may fail if we are running outside of VS, in scenarios such as CodeFlow. That is ok
        // and this logging code should gracefully no-op in that case.
        [Import(AllowDefault = true)]
        private ILoggingServiceInternal LoggingService { get; set; }

        public const string VSEditorKey = "VS/Editor";

        DispatcherTimer _touchZoomTimer = null;
        DispatcherTimer _touchScrollTimer = null;
        DispatcherTimer _zoomTimer = null;
        DispatcherTimer _scrollTimer = null;

        uint _lastZoomLevel = 0;
        readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(1000.0);

        public void LogTouchZoom()
        {
            if (LoggingService != null)
            {
                if (_touchZoomTimer == null)
                {
                    _touchZoomTimer = new DispatcherTimer();
                    _touchZoomTimer.Interval = _timeout;
                    _touchZoomTimer.Tick += (s, e) =>
                    {
                        _touchZoomTimer.Stop();
                        LoggingService.AdjustCounter(TelemetryLogger.VSEditorKey, "TouchZoom", delta: 1);
                    };
                }

                // Restart timer
                _touchZoomTimer.Stop();
                _touchZoomTimer.Start();
            }
        }

        public void LogZoom(uint finalZoomLevel)
        {
            if (LoggingService != null)
            {
                if (_zoomTimer == null)
                {
                    _zoomTimer = new DispatcherTimer();
                    _zoomTimer.Interval = _timeout;
                    _zoomTimer.Tick += (s, e) =>
                    {
                        _zoomTimer.Stop();
                        LoggingService.PostEvent("VS/Editor/Zoom", "VS.Editor.Zoom.LastZoomLevel", _lastZoomLevel);
                    };
                }

                // Restart timer
                _zoomTimer.Stop();

                // Set _lastZoomLevel between stop and start out of paranoia regarding race conditions that shouldn't
                // actually occur while using DispatcherTimer, since it runs all on the same thread. However, if the
                // underlying timer get's changed, and this set were above the stop, there's a chance that we could
                // occasionally log incorrect data if the set happened, and then tick occurred before the stop.
                _lastZoomLevel = finalZoomLevel;

                _zoomTimer.Start();
            }
        }

        public void LogTouchScroll()
        {
            if (LoggingService != null)
            {
                if (_touchScrollTimer == null)
                {
                    _touchScrollTimer = new DispatcherTimer();
                    _touchScrollTimer.Interval = _timeout;
                    _touchScrollTimer.Tick += (s, e) =>
                    {
                        _touchScrollTimer.Stop();
                        LoggingService.AdjustCounter(TelemetryLogger.VSEditorKey, "TouchScroll", delta: 1);
                    };
                }

                // Restart timer
                _touchScrollTimer.Stop();
                _touchScrollTimer.Start();
            }
        }

        public void LogScroll()
        {
            if (LoggingService != null)
            {
                if (_scrollTimer == null)
                {
                    _scrollTimer = new DispatcherTimer();
                    _scrollTimer.Interval = _timeout;
                    _scrollTimer.Tick += (s, e) =>
                    {
                        _scrollTimer.Stop();
                        LoggingService.AdjustCounter(TelemetryLogger.VSEditorKey, "Scroll", delta: 1);
                    };
                }

                // Restart timer
                _scrollTimer.Stop();
                _scrollTimer.Start();
            }
        }

        public void PostCounters()
        {
            LoggingService.PostCounters();
        }
    }
}