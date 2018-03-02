////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal static class Helpers
    {
        private static string GetAllTextDisplayedByCompletion(Completion c)
        {
            string displayText = c.DisplayText;
            string suffixText = null;

            var c4 = c as Completion4;
            if (c4 != null)
            {
                suffixText = c4.Suffix;
            }

            return string.IsNullOrEmpty(suffixText) ? displayText : (displayText + suffixText);
        }

#if DEBUG
        internal static void TrackObject(List<Lazy<IObjectTracker>> objectTrackers, string bucketName, object value)
        {
            foreach (Lazy<IObjectTracker> trackerExport in objectTrackers)
            {
                IObjectTracker objectTracker = trackerExport.Value;
                if (objectTracker != null)
                {
                    objectTracker.TrackObject(value, bucketName);
                }
            }
        }
#endif

        internal static ITrackingSpan GetEncapsulatingSpan(ITextView textView, ITrackingSpan span1, ITrackingSpan span2)
        {
            // If either of the spans is null, return the other one.  If they're both null, we'll end up returning null
            // (as it should be).
            if (span1 == null)
            {
                var spans = textView.BufferGraph.MapUpToBuffer
                    (
                    span2.GetSpan(span2.TextBuffer.CurrentSnapshot),
                    span2.TrackingMode,
                    textView.TextBuffer
                    );
                Debug.Assert(spans.Count == 1);
                return spans[0].Snapshot.CreateTrackingSpan(spans[0], span2.TrackingMode);
            }
            if (span2 == null)
            {
                var spans = textView.BufferGraph.MapUpToBuffer
                    (
                    span1.GetSpan(span2.TextBuffer.CurrentSnapshot),
                    span1.TrackingMode,
                    textView.TextBuffer
                    );
                Debug.Assert(spans.Count == 1);
                return spans[0].Snapshot.CreateTrackingSpan(spans[0], span1.TrackingMode);
            }

            List<SnapshotSpan> surfaceSpans = new List<SnapshotSpan>();
            surfaceSpans.AddRange
                (
                textView.BufferGraph.MapUpToBuffer
                    (
                    span1.GetSpan(span1.TextBuffer.CurrentSnapshot),
                    span1.TrackingMode,
                    textView.TextBuffer
                    )
                );
            surfaceSpans.AddRange
                (
                textView.BufferGraph.MapUpToBuffer
                    (
                    span2.GetSpan(span2.TextBuffer.CurrentSnapshot),
                    span2.TrackingMode,
                    textView.TextBuffer
                    )
                );

            ITrackingSpan encapsulatingSpan = null;
            foreach (var span in surfaceSpans)
            {
                encapsulatingSpan = Helpers.GetEncapsulatingSpan
                    (
                    encapsulatingSpan,
                    span.Snapshot.CreateTrackingSpan(span, span1.TrackingMode)
                    );
            }

            return encapsulatingSpan;
        }

        internal static ITrackingSpan GetEncapsulatingSpan(ITrackingSpan span1, ITrackingSpan span2)
        {
            // If either of the spans is null, return the other one.  If they're both null, we'll end up returning null
            // (as it should be).
            if (span1 == null)
            {
                return span2;
            }
            if (span2 == null)
            {
                return span1;
            }

            // In the case where the two spans exist over different buffers, just use the first span.
            if (span1.TextBuffer != span2.TextBuffer)
            {
                return span1;
            }

            ITextSnapshot snapshot = span1.TextBuffer.CurrentSnapshot;
            SnapshotSpan snapSpan1 = span1.GetSpan(snapshot);
            SnapshotSpan snapSpan2 = span2.GetSpan(snapshot);

            return snapshot.CreateTrackingSpan
                (Span.FromBounds
                    (Math.Min(snapSpan1.Start.Position, snapSpan2.Start.Position),
                     Math.Max(snapSpan1.End.Position, snapSpan2.End.Position)),
                 span1.TrackingMode);
        }

        internal static IEnumerable<TStyle> GetMatchingPresenterStyles<TSession, TStyle>
                                                (TSession session,
                                                 IList<Lazy<TStyle, IOrderableContentTypeMetadata>> orderedPresenterStyles,
                                                 GuardedOperations guardedOperations)
            where TSession : IIntellisenseSession
        {
            List<TStyle> styles = new List<TStyle>();

            ITextView textView = session.TextView;
            SnapshotPoint? surfaceBufferPoint = session.GetTriggerPoint(textView.TextSnapshot);
            if (surfaceBufferPoint == null)
            {
                return styles;
            }

            var buffers = Helpers.GetBuffersForTriggerPoint(session).ToList();

            foreach (var styleExport in orderedPresenterStyles)
            {
                bool usedThisProviderAlready = false;
                foreach (var buffer in buffers)
                {
                    foreach (string contentType in styleExport.Metadata.ContentTypes)
                    {
                        if (buffer.ContentType.IsOfType(contentType))
                        {
                            var style = guardedOperations.InstantiateExtension(styleExport, styleExport);
                            if (!Object.Equals(style, default(TStyle)))
                            {
                                styles.Add(style);
                            }
                            usedThisProviderAlready = true;
                            break;
                        }
                    }
                    if (usedThisProviderAlready)
                    {
                        break;
                    }
                }
            }

            return styles;
        }

        internal static Xwt.Rectangle GetScreenRect(IIntellisenseSession session)
        {
            return Xwt.MessageDialog.RootWindow.Screen.VisibleBounds;
            //TODO
            //if ((session != null) && (session.TextView != null)) {
            //  Visual sessionViewVisual = ((IWpfTextView)session.TextView).VisualElement;
            //  if ((sessionViewVisual != null) && PresentationSource.FromVisual (sessionViewVisual) != null) {
            //      Rect nativeScreenRect = WpfHelper.GetScreenRect (sessionViewVisual.PointToScreen (new Point (0, 0)));
            //      return new Rect
            //         (0,
            //          0,
            //          nativeScreenRect.Width * WpfHelper.DeviceScaleX,
            //          nativeScreenRect.Height * WpfHelper.DeviceScaleY);
            //  }
            //}

            //return new Rect
            //  (0,
            //   0,
            //   SystemParameters.PrimaryScreenWidth * WpfHelper.DeviceScaleX,
            //   SystemParameters.PrimaryScreenHeight * WpfHelper.DeviceScaleY);
        }

        internal static Collection<ITextBuffer> GetBuffersForTriggerPoint(IIntellisenseSession session)
        {
            return session.TextView.BufferGraph.GetTextBuffers(
                buffer => session.GetTriggerPoint(buffer) != null);
        }

        public static bool CanMapDownToBuffer(ITextView textView, ITextBuffer textBuffer, ITrackingPoint triggerPoint)
        {
            SnapshotPoint triggerSnapshotPoint = triggerPoint.GetPoint(textView.TextSnapshot);
            var triggerSpan = new SnapshotSpan(triggerSnapshotPoint, 0);

            var mappedSpans = new FrugalList<SnapshotSpan>();
            MappingHelper.MapDownToBufferNoTrack(triggerSpan, textBuffer, mappedSpans);
            return mappedSpans.Count > 0;
        }

        public static IEnumerable<TSource> GetSources<TSource>(
            IIntellisenseSession session,
            Func<ITextBuffer, IEnumerable<TSource>> sourceCreator) where TSource : IDisposable
        {
            return IntellisenseSourceCache.GetSources(
                session.TextView,
                GetBuffersForTriggerPoint(session),
                sourceCreator);
        }
    }
}
