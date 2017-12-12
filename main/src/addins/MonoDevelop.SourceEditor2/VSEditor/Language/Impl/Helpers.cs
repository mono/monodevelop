////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using TextNativeMethods = Microsoft.VisualStudio.Text.Utilities.NativeMethods;
using Microsoft.VisualStudio.Language.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal delegate void MouseActionDelegate(IMouseProcessor processor);

    internal static class Helpers
    {
        private static readonly Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        internal static double GuessMaxWidth(IList<Completion> completions, TextRunProperties textProps)
        {
            if (completions.Count > 0)
            {
                // We measure both the first and the item with the most characters
                // We measure the first because we don't want to truncate any Meta-entry (like "<Create New Event>")
                string[] candidates = new string[2];

                Completion longestCompletion = completions[0];
                string displayText = GetAllTextDisplayedByCompletion(longestCompletion);
                candidates[0] = displayText;
                int length = String.IsNullOrEmpty(displayText) ? 0 : displayText.Length;

                // We'll look at up to 100 items in the list to determine the max width.

                int delta = 1;
                int completionsCount = completions.Count;
                if (completionsCount >= 100)
                {
                    delta = (int)(completionsCount / 100);
                }

                // we have the length of the first item, so start at the second item
                int currentCompletion = delta;
                while (currentCompletion < completionsCount)
                {
                    Completion completion = completions[currentCompletion];
                    displayText = GetAllTextDisplayedByCompletion(completion);
                    int newLength = String.IsNullOrEmpty(displayText) ? 0 : displayText.Length;
                    if (newLength > length)
                    {
                        candidates[1] = displayText;
                        length = newLength;
                    }

                    currentCompletion += delta;
                }

                // Try to figure out how big (horizontally) this completion would be on-screen.

                FormattableTextBlock tb = new FormattableTextBlock();
                tb.TextRunProperties = textProps;
                double first = 0, second = 0;
                if (!string.IsNullOrEmpty(candidates[0]))
                {
                    tb.Text = candidates[0];
                    tb.Measure(infiniteSize);
                    first = tb.DesiredSize.Width;
                }

                if (!string.IsNullOrEmpty(candidates[1]))
                {
                    tb.Text = candidates[1];
                    tb.Measure(infiniteSize);
                    second = tb.DesiredSize.Width;
                }
                // After some tweaking and experimentation, I've found 75 units (or 50 without a scroll bar) to be a good
                // offset here.  We need to guess at the size of the longest item, since classification/templating can change
                // the width of any of these items.

                double buffer = completions.Count <= DefaultCompletionSetPresenter.NumCompletionsPerPage ? 50 : 75;

                return Math.Max(first, second) + buffer;
            }

            return (0);
        }

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

        /// <summary>
        /// Creates an <see cref="IWpfTextView"/> that can be used in intellisense tooltips such as signature help and quick info.
        /// </summary>
        internal static IWpfTextView CreateTooltipTextView(ITextEditorFactoryService editorFactoryService, ITextBuffer buffer, IEditorOptions options)
        {
            // ensure that no word-wrap is on initially. We only want to enable word-wrap if a text view has captured all its available width
            // while still not able to fit its content
            options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.None);

            IWpfTextView view = editorFactoryService.CreateTextView(buffer, editorFactoryService.NoRoles, options);

            // Set common visual properties on the view
            view.VisualElement.Cursor = System.Windows.Input.Cursors.Arrow;
            view.VisualElement.Focusable = false;
            view.VisualElement.HorizontalAlignment = HorizontalAlignment.Left;
            view.Background = Brushes.Transparent; // such that the background of the hosting intellisense window shows through

            // Text views used in intellisense tooltips are never zoomed, so we can always use Display text formatting mode
            TextOptions.SetTextFormattingMode(view.VisualElement, TextFormattingMode.Display);

            return view;
        }

        internal static void AutoSizeTextView(IWpfTextView textView, double maxWidth, double maxHeight)
        {
            if (textView.IsClosed || textView.InLayout)
            {
                return;
            }

            // Fix the height
            if (textView.TextViewLines.FormattedSpan.Start.Position != 0 ||
                textView.TextViewLines.FormattedSpan.End.Position != textView.TextSnapshot.Length)
            {
                textView.DisplayTextLineContainingBufferPosition(new SnapshotPoint(textView.TextSnapshot, 0), 0, ViewRelativePosition.Top, maxWidth, maxHeight);
            }
            double desiredHeight = Math.Min(maxHeight, textView.TextViewLines[textView.TextViewLines.Count - 1].Bottom - textView.TextViewLines[0].Top);
            if (textView.VisualElement.Height != desiredHeight)
            {
                textView.VisualElement.Height = desiredHeight;
            }

            // Fix the width

            // It doesn't make sense to change the width of the view if word-wrap is on, unless it's bigger than
            // the maximum allowed
            if ((textView.Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) == WordWrapStyles.WordWrap)
            {
                if (textView.VisualElement.Width > maxWidth)
                {
                    textView.VisualElement.Width = maxWidth;
                }
            }
            else
            {
                // 1. Text view is bigger than the width of its formatted content. We need to shrink the width of the text view.
                // 2. Text view is smaller than the width of its formatted content
                //      2.a. If the content fits in the maximum available width, then resize the view
                //      2.b. If the content does not fit in the maximum available width, then turn word-wrap on
                if (maxWidth >= textView.MaxTextRightCoordinate)
                {
                    if (textView.VisualElement.Width != textView.MaxTextRightCoordinate)
                    {
                        textView.VisualElement.Width = textView.MaxTextRightCoordinate;
                    }
                }
                else
                {
                    textView.VisualElement.Width = maxWidth;

                    textView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.WordWrap);
                }
            }
        }

        internal static void ExecMouseAction(object mouseProcessor, MouseActionDelegate dlg)
        {
            IMouseProcessor subPresenterProcessor = mouseProcessor as IMouseProcessor;
            if (subPresenterProcessor != null)
            {
                dlg(subPresenterProcessor);
            }
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

        internal static UIElement FindUIElement<TData, TContext>
                                        (IIntellisenseSession session,
                                         TData itemToRender,
                                         TContext context,
                                         UIElementType elementType,
                                         IList<Lazy<IUIElementProvider<TData, TContext>, IOrderableContentTypeMetadata>> orderedUIElementProviders,
                                         GuardedOperations guardedOperations)
        {
            var buffers = Helpers.GetBuffersForTriggerPoint(session).ToList();

            foreach (var presenterProviderExport in orderedUIElementProviders)
            {
                foreach (var buffer in buffers)
                {
                    foreach (var contentType in presenterProviderExport.Metadata.ContentTypes)
                    {
                        if (buffer.ContentType.IsOfType(contentType))
                        {
                            UIElement element = guardedOperations.InstantiateExtension(
                                presenterProviderExport, presenterProviderExport,
                                provider => provider.GetUIElement(
                                    itemToRender, context, elementType));
                            if (element != null)
                            {
                                return element;
                            }
                        }
                    }
                }
            }

            return null;
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

        internal static Color BrushToColor(Brush brush)
        {
            SolidColorBrush solidColorBrush = brush as SolidColorBrush;
            if (solidColorBrush != null)
            {
                return solidColorBrush.Color;
            }

            GradientBrush gradientBrush = brush as GradientBrush;
            if (gradientBrush != null && gradientBrush.GradientStops.Count > 0)
            {
                return gradientBrush.GradientStops[0].Color;
            }

            return Colors.Transparent;
        }

        internal static bool HighContrast
        {
            get
            {
                // If WPF tells us that we're in high-contrast, Go ahead and believe it.
                if (SystemParameters.HighContrast)
                {
                    return true;
                }

                // If WPF doesn't tell us that we're in high-contrast, we still could be.  We'll have to check some colors for
                // ourselves.
                Color controlColor = SystemColors.ControlColor;
                Color controlTextColor = SystemColors.ControlTextColor;

                Color white = Color.FromRgb(0xFF, 0xFF, 0xFF);
                Color black = Color.FromRgb(0x00, 0x00, 0x00);
                Color green = Color.FromRgb(0x00, 0xFF, 0x00);

                // Are we running under High Contrast #1 or High Contrast Black?
                if ((controlColor == black) && (controlTextColor == white))
                {
                    return true;
                }

                // Are we running under High Contrast White?
                if ((controlColor == white) && (controlTextColor == black))
                {
                    return true;
                }

                // Are we running under High Contrast #2?
                if ((controlColor == black) && (controlTextColor == green))
                {
                    return true;
                }

                return false;
            }
        }

        internal static Rect GetScreenRect(IIntellisenseSession session)
        {
            if ((session != null) && (session.TextView != null))
            {
                Visual sessionViewVisual = ((IWpfTextView)session.TextView).VisualElement;
                if ((sessionViewVisual != null) && PresentationSource.FromVisual(sessionViewVisual) != null)
                {
                    Rect nativeScreenRect = WpfHelper.GetScreenRect(sessionViewVisual.PointToScreen(new Point(0, 0)));
                    return new Rect
                       (0,
                        0,
                        nativeScreenRect.Width * WpfHelper.DeviceScaleX,
                        nativeScreenRect.Height * WpfHelper.DeviceScaleY);
                }
            }

            return new Rect
                (0,
                 0,
                 SystemParameters.PrimaryScreenWidth * WpfHelper.DeviceScaleX,
                 SystemParameters.PrimaryScreenHeight * WpfHelper.DeviceScaleY);
        }

        internal static Collection<ITextBuffer> GetBuffersForTriggerPoint(IIntellisenseSession session)
        {
            return session.TextView.BufferGraph.GetTextBuffers(
                buffer => session.GetTriggerPoint(buffer) != null);
        }

        internal static bool TryGetMonitorRect(Point targetPoint, out TextNativeMethods.RECT screenRect)
        {
            var ptStruct = new TextNativeMethods.POINT();
            ptStruct.x = (int)targetPoint.X;
            ptStruct.y = (int)targetPoint.Y;

            IntPtr monitor = TextNativeMethods.MonitorFromPoint(ptStruct, TextNativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new TextNativeMethods.MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(TextNativeMethods.MONITORINFO));
                if (TextNativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                {
                    screenRect = monitorInfo.rcMonitor;
                    return true;
                }
            }

            screenRect = default(TextNativeMethods.RECT);
            return false;
        }

        internal static Matrix GetTransformFromDevice(Visual targetVisual)
        {
            HwndSource hwndSource = null;
            if (targetVisual != null)
            {
                hwndSource = PresentationSource.FromVisual(targetVisual) as HwndSource;
            }
            if (hwndSource != null)
            {
                CompositionTarget ct = hwndSource.CompositionTarget;
                if (ct != null)
                {
                    return ct.TransformFromDevice;
                }
            }

            return Matrix.Identity;
        }

        public static bool CanMapDownToBuffer(ITextView textView, ITextBuffer textBuffer, ITrackingPoint triggerPoint)
        {
            SnapshotPoint triggerSnapshotPoint = triggerPoint.GetPoint(textView.TextSnapshot);
            var triggerSpan = new SnapshotSpan(triggerSnapshotPoint, 0);

            var mappedSpans = new FrugalList<SnapshotSpan>();
            MappingHelper.MapDownToBufferNoTrack(triggerSpan, textBuffer, mappedSpans);
            return mappedSpans.Count > 0;
        }

        public static DependencyObject GetParent(this DependencyObject element)
        {
            DependencyObject parent = VisualTreeExtensions.GetVisualOrLogicalParent(element);

            return parent;
        }

        public static T FindAncestor<T>(this DependencyObject element)
            where T : class
        {
            while (element != null)
            {
                element = GetParent(element);
                if (element != null && element is T)
                {
                    return element as T;
                }
            }
            return null;
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
