// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Defines the aggregator for all the classifiers - and is available to the external world via a VisualStudio
    /// Service
    /// </summary>
    internal sealed class ClassifierAggregator : IAccurateClassifier, IDisposable
    {
        #region Private Members

        private ITextBuffer _textBuffer;
        private IClassificationTypeRegistryService _classificationTypeRegistry;
        private IAccurateTagAggregator<IClassificationTag> _tagAggregator;
        #endregion // Private Members

        #region Constructors

        internal ClassifierAggregator(ITextBuffer textBuffer,
                                      IBufferTagAggregatorFactoryService bufferTagAggregatorFactory,
                                      IClassificationTypeRegistryService classificationTypeRegistry)
        {
            // Validate.
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            if (bufferTagAggregatorFactory == null)
            {
                throw new ArgumentNullException("bufferTagAggregatorFactory");
            }
            if (classificationTypeRegistry == null)
            {
                throw new ArgumentNullException("classificationTypeRegistry");
            }

            _textBuffer = textBuffer;
            _classificationTypeRegistry = classificationTypeRegistry;

            // Create a tag aggregator that maps by content type, so we don't map through projection buffers that aren't "projection" content type.
            _tagAggregator = bufferTagAggregatorFactory.CreateTagAggregator<IClassificationTag>(textBuffer, TagAggregatorOptions.MapByContentType | (TagAggregatorOptions)TagAggregatorOptions2.DeferTaggerCreation) as IAccurateTagAggregator<IClassificationTag>;
            _tagAggregator.TagsChanged += OnTagsChanged;
        }

		//HACK internal ClassifierAggregator(ITextView textView,
		//HACK                               IViewTagAggregatorFactoryService viewTagAggregatorFactory,
		//HACK                               IClassificationTypeRegistryService classificationTypeRegistry)
		//HACK {
		//HACK     // Validate.
		//HACK     if (textView == null)
		//HACK     {
		//HACK         throw new ArgumentNullException("textView");
		//HACK     }
		//HACK     if (viewTagAggregatorFactory == null)
		//HACK     {
		//HACK         throw new ArgumentNullException("viewTagAggregatorFactory");
		//HACK     }
		//HACK     if (classificationTypeRegistry == null)
		//HACK     {
		//HACK         throw new ArgumentNullException("classificationTypeRegistry");
		//HACK     }
		//HACK 
		//HACK     _textBuffer = textView.TextBuffer;
		//HACK     _classificationTypeRegistry = classificationTypeRegistry;
		//HACK 
		//HACK     // Create a tag aggregator that maps by content type, so we don't map through projection buffers that aren't "projection" content type.
		//HACK     _tagAggregator = viewTagAggregatorFactory.CreateTagAggregator<IClassificationTag>(textView, TagAggregatorOptions.MapByContentType) as IAccurateTagAggregator<IClassificationTag>;
		//HACK     _tagAggregator.TagsChanged += OnTagsChanged;
		//HACK }

		#endregion // Constructors

		#region Exposed Methods

		/// <summary>
		/// Gets all classification spans that overlap the given range of text
		/// </summary>
		/// <param name="span">
		/// The span of text of interest
		/// </param>
		/// <returns>
		/// A list of lists of ClassificationSpans that intersect with the given range
		/// </returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return this.InternalGetClassificationSpans(span, _tagAggregator.GetTags(span));
        }

        public IList<ClassificationSpan> GetAllClassificationSpans(SnapshotSpan span, CancellationToken cancel)
        {
            return this.InternalGetClassificationSpans(span, _tagAggregator.GetAllTags(span, cancel));
        }
        #endregion // Exposed Methods

        #region IDisposable
        public void Dispose()
        {
            if (_tagAggregator != null)
            {
                _tagAggregator.TagsChanged -= OnTagsChanged;
                _tagAggregator.Dispose();

                _tagAggregator = null;
            }
        }
        #endregion

        #region Public Events

        /// <summary>
        /// Notifies a classification change.  The aggregator also bubbles ClassificationChanged
        /// events up from aggregated classifiers
        /// </summary>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        #endregion // Public Events

        #region Event Handlers

        private void OnTagsChanged(object sender, TagsChangedEventArgs e)
        {
            foreach(var span in e.Span.GetSpans(_textBuffer))
            {
                var tempEvent = ClassificationChanged;
                if (tempEvent != null)
                    tempEvent(this, new ClassificationChangedEventArgs(span));
            }
        }

        #endregion // Event Handlers

        #region Private Helpers

        private IList<ClassificationSpan> InternalGetClassificationSpans(SnapshotSpan span, IEnumerable<IMappingTagSpan<IClassificationTag>> tags)
        {
            List<ClassificationSpan> allSpans = new List<ClassificationSpan>();
            foreach (var tagSpan in tags)
            {
                NormalizedSnapshotSpanCollection tagSpans = tagSpan.Span.GetSpans(span.Snapshot.TextBuffer);
                for (int s = 0; s < tagSpans.Count; ++s)
                {
                    allSpans.Add(new ClassificationSpan(
                                    tagSpans[s].TranslateTo(span.Snapshot, SpanTrackingMode.EdgeExclusive),
                                    tagSpan.Tag.ClassificationType));
                }
            }

            return this.NormalizeClassificationSpans(span, allSpans);
        }

        #region Private classes
        struct PointData
        {
            public readonly bool IsStart;
            public readonly int Position;
            public readonly IClassificationType ClassificationType;

            public PointData(bool isStart, int position, IClassificationType classificationType)
            {
                this.IsStart = isStart;
                this.Position = position;
                this.ClassificationType = classificationType;
            }
        }

        class OpenSpanData
        {
            public readonly IClassificationType ClassificationType;

            public int Count = 1;           // How many open classification spans with this classification type are there?

            public OpenSpanData(IClassificationType classificationType)
            {
                this.ClassificationType = classificationType;
            }
        };
        #endregion

        /// <summary>
        /// Normalizes a list of classifications.  Given an arbitrary set of ClassificationSpan lists, creates
        /// a new list with each ClassificationSpan, sorted by position and with the appropriate transient
        /// classification types for spans that had multiple classifications.
        /// </summary>
        /// <param name="spans">A list of lists that contain ClassificationSpans.</param>
        /// <param name="requestedRange">
        /// The requested range of this call so results can be trimmed if there are any
        /// classifiers that returned spans past the requested range they can be trimmed.
        /// </param>
        /// <returns>A sorted list of normalized spans.</returns>
        /// <remarks>
        /// Basic algorithm:
        ///   Create a list of the starting and ending points for each classification (clipped against requestedRange).
        ///   Sort the points list by position. If the positions are the same, starting points go before ending points (this
        ///   prevents a seam between two adjoining classifications of the same type).
        /// 
        ///   Make a list of the currently open spans.
        ///   Walk the points in order.
        ///     When encountering a starting point:
        ///       If there is already an open span of starting point's type, increment the count associated with the open span.
        ///       Otherwise, add a new classification span based on the currently open spans and then add an open span (based on the startpoint) to the list of open spans.
        /// 
        ///     When encountering an ending point:
        ///       Check the count for the open span of the ending point's type (there must be one):
        ///         If it is one, add a new classiciation based on the open spans and remove that open span from the list of open spans.
        ///         Otherwise, decrement the count associated with the open span.
        /// </remarks>
        private List<ClassificationSpan> NormalizeClassificationSpans(SnapshotSpan requestedRange, IList<ClassificationSpan> spans)
        {
            // Add the start and end points of each classification to one sorted list
            List<PointData> points = new List<PointData>(spans.Count * 2);
            int lastEndPoint = -1;
            IClassificationType lastClassificationType = null;
            bool requiresNormalization = false;
            foreach (ClassificationSpan classification in spans)
            {
                // Only consider classifications that overlap the requested span
                Span? span = requestedRange.Overlap(classification.Span.TranslateTo(requestedRange.Snapshot, SpanTrackingMode.EdgeExclusive));

                if (span.HasValue)
                {
                    // Use a <= test so that adjoining classifications will go through the normalizing process
                    // (so that adjoining classifications of the same type will be merged into a single
                    // classification span).
                    Span overlap = span.Value;

                    if ((overlap.Start < lastEndPoint) ||
                        ((overlap.Start == lastEndPoint) && (classification.ClassificationType == lastClassificationType)))
                    {
                        requiresNormalization = true;
                    }

                    lastEndPoint = overlap.End;
                    lastClassificationType = classification.ClassificationType;

                    points.Add(new PointData(true, overlap.Start, lastClassificationType));
                    points.Add(new PointData(false, overlap.End, lastClassificationType));
                }
            }

            List<ClassificationSpan> results = new List<ClassificationSpan>();
            if (!requiresNormalization)
            {
                // The generated points list is already sorted, so simple generate a list of classifications from it without normalizing
                // (we can't use spans since its classifications have not been clipped to requestedRange).
                for (int i = 1; (i < points.Count); i += 2)
                {
                    PointData startPoint = points[i - 1];
                    PointData endPoint = points[i];

                    results.Add(new ClassificationSpan(new SnapshotSpan(requestedRange.Snapshot, startPoint.Position, endPoint.Position - startPoint.Position),
                                                       startPoint.ClassificationType));
                }
            }
            else
            {
                points.Sort(Compare);

                int nextSpanStart = 0;

                List<OpenSpanData> openSpans = new List<OpenSpanData>();
                for (int p = 0; p < points.Count; ++p)
                {
                    PointData point = points[p];
                    OpenSpanData openSpanData = null;
                    // write this little search explicitly instead of using the Find method, because allocating
                    // a delegate here in a high-frequency loop allocates noticeable amounts of memory
                    for (int os = 0; os < openSpans.Count; ++os)
                    {
                        if (openSpans[os].ClassificationType == point.ClassificationType)
                        {
                            openSpanData = openSpans[os];
                            break;
                        }
                    }
                    if (point.IsStart)
                    {
                        if (openSpanData != null)
                        {
                            ++(openSpanData.Count);
                        }
                        else
                        {
                            if ((openSpans.Count > 0) && (point.Position > nextSpanStart))
                            {
                                this.AddClassificationSpan(openSpans, requestedRange.Snapshot, nextSpanStart, point.Position, results);
                            }
                            nextSpanStart = point.Position;
                            openSpans.Add(new OpenSpanData(point.ClassificationType));
                        }
                    }
                    else
                    {
                        if (openSpanData.Count > 1)
                        {
                            --(openSpanData.Count);
                        }
                        else
                        {
                            if (point.Position > nextSpanStart)
                            {
                                this.AddClassificationSpan(openSpans, requestedRange.Snapshot, nextSpanStart, point.Position, results);
                            }
                            nextSpanStart = point.Position;
                            openSpans.Remove(openSpanData);
                        }
                    }
                }
            }

            // Return our list of aggregated spans.
            return results;
        }

        private int Compare(PointData a, PointData b)
        {
            if (a.Position == b.Position)
                return (b.IsStart.CompareTo(a.IsStart)); // startpoints go before end points when positions are tied
            else
                return (a.Position - b.Position);
        }

        /// <summary>
        /// Add a classification span to <paramref name="results"/> that corresponds to the open classification spans in <paramref name="openSpans"/>
        /// between <paramref name="start"/> and <paramref name="end"/>.
        /// </summary>
        private void AddClassificationSpan(List<OpenSpanData> openSpans, ITextSnapshot snapshot, int start, int end, IList<ClassificationSpan> results)
        {
            IClassificationType classificationType;

            if (openSpans.Count == 1)
            {
                // If there is only one classification type, then don't create a transient type.
                classificationType = openSpans[0].ClassificationType;
            }
            else
            {
                // There is more than one classification type, create a transient type that corresponds
                // to all the open classificaiton types.
                List<IClassificationType> classifications = new List<IClassificationType>(openSpans.Count);
                foreach (OpenSpanData osd in openSpans)
                    classifications.Add(osd.ClassificationType);

                classificationType = _classificationTypeRegistry.CreateTransientClassificationType(classifications);
            }

            results.Add(new ClassificationSpan(new SnapshotSpan(snapshot, start, end - start),
                                               classificationType));
        }
        #endregion
    }
}
