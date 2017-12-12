namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Language.Utilities;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;

    internal sealed class SquiggleQuickInfoSource : IAsyncQuickInfoSource
    {
        private bool disposed = false;
        private SquiggleQuickInfoSourceProvider componentContext;
        private ITextBuffer textBuffer;
        private ITextView tagAggregatorTextView;
        private ITagAggregator<IErrorTag> tagAggregator;

        public SquiggleQuickInfoSource(
            SquiggleQuickInfoSourceProvider componentContext,
            ITextBuffer textBuffer)
        {
            this.componentContext = componentContext
                ?? throw new ArgumentNullException(nameof(componentContext));
            this.textBuffer = textBuffer
                ?? throw new ArgumentNullException(nameof(textBuffer));
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session,
            CancellationToken cancellationToken)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("SquiggleQuickInfoSource");
            }

            if (session.TextView.TextBuffer != this.textBuffer)
            {
                return null;
            }

            // TagAggregators must be used exclusively on the UI thread.
            await this.componentContext.JoinableTaskContext.Factory.SwitchToMainThreadAsync();

            ITrackingSpan applicableToSpan = null;
            object quickInfoContent = null;
            ITagAggregator<IErrorTag> tagAggregator = GetTagAggregator(session.TextView);

            Debug.Assert(tagAggregator != null, "Couldn't create a tag aggregator for error tags");
            if (tagAggregator != null)
            {
                // Put together the span over which tags are to be discovered.  This will be the zero-length span at the trigger
                // point of the session.
                SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot);
                if (!subjectTriggerPoint.HasValue)
                {
                    Debug.Fail("The squiggle QuickInfo source is being called when it shouldn't be.");
                    return null;
                }

                ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
                var querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);

                // Ask for all of the error tags that intersect our query span.  We'll get back a list of mapping tag spans.
                // The first of these is what we'll use for our quick info.
                IEnumerable<IMappingTagSpan<IErrorTag>> tags = tagAggregator.GetTags(querySpan);
                ITrackingSpan appToSpan = null;
                foreach (MappingTagSpan<IErrorTag> tag in tags)
                {
                    NormalizedSnapshotSpanCollection applicableToSpans = tag.Span.GetSpans(currentSnapshot);
                    if (applicableToSpans.Count > 0)
                    {
                        // We've found a error tag at the right location with a tag span that maps to our subject buffer.
                        // Return the applicability span as well as the tooltip content.
                        appToSpan = IntellisenseUtilities.GetEncapsulatingSpan(
                            session.TextView,
                            appToSpan,
                            currentSnapshot.CreateTrackingSpan(
                                applicableToSpans[0].Span,
                                SpanTrackingMode.EdgeInclusive));

                        quickInfoContent = tag.Tag.ToolTipContent;
                    }
                }

                if (quickInfoContent != null)
                {
                    applicableToSpan = appToSpan;
                    return new QuickInfoItem(
                        applicableToSpan,
                        quickInfoContent);
                }
            }

            return null;
        }

        private ITagAggregator<IErrorTag> GetTagAggregator(ITextView textView)
        {
            if (this.tagAggregator == null)
            {
                this.tagAggregatorTextView = textView;
                this.tagAggregator = this.componentContext.TagAggregatorFactoryService.CreateTagAggregator<IErrorTag>(textView);
            }
            else if (this.tagAggregatorTextView != textView)
            {
                throw new ArgumentException ("The SquiggleQuickInfoSource cannot be shared between TextViews.");
            }

            return this.tagAggregator;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            Debug.Assert(this.componentContext.JoinableTaskContext.IsOnMainThread);

            // Get rid of the tag aggregator, if we created it.
            if (this.tagAggregator != null)
            {
                this.tagAggregator.Dispose();
                this.tagAggregator = null;
                this.tagAggregatorTextView = null;
            }

            this.componentContext = null;
            this.textBuffer = null;
        }
    }
}
