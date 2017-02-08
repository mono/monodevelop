namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ITaggerProvider))]
    [ContentType("projection")]
    [TagType(typeof(ClassificationTag))]
    internal class ProjectionWorkaroundProvider : ITaggerProvider
    {
        [Import]
        internal IDifferenceService diffService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            IProjectionBuffer projectionBuffer = buffer as IProjectionBuffer;
            if (projectionBuffer == null)
                return null;

            return new ProjectionWorkaroundTagger(projectionBuffer, diffService) as ITagger<T>;
        }
    }

    /// <summary>
    /// This is a workaround for projection buffers.  Currently, the way our projection buffer
    /// implementation does "minimal" updating is by using *lexical* differencing.  The
    /// problem with this approach is that if you replace a span in a projection buffer
    /// with a lexically equivalent span from a *different* buffer, the projection buffer
    /// will event that no changes have been made.
    /// </summary>
    internal class ProjectionWorkaroundTagger : ITagger<ClassificationTag>
    {
        IProjectionBuffer ProjectionBuffer { get; set; }
        IDifferenceService diffService;

        internal ProjectionWorkaroundTagger(IProjectionBuffer projectionBuffer, IDifferenceService diffService)
        {
            this.ProjectionBuffer = projectionBuffer;
            this.diffService = diffService;
            this.ProjectionBuffer.SourceBuffersChanged += SourceSpansChanged;
        }

        #region ITagger<ClassificationTag> members
        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            yield break;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        #region Source span differencing + change event
        private void SourceSpansChanged(object sender, ProjectionSourceSpansChangedEventArgs e)
        {
            if (e.Changes.Count == 0)
            {
                // If there weren't text changes, but there were span changes, then
                // send out a classification changed event over the spans that changed.
                ProjectionSpanDifference difference = ProjectionSpanDiffer.DiffSourceSpans(this.diffService, e.Before, e.After);
                int pos = 0;
                int start = int.MaxValue;
                int end = int.MinValue;
                foreach (var diff in difference.DifferenceCollection)
                {
                    pos += GetMatchSize(difference.DeletedSpans, diff.Before);
                    start = Math.Min(start, pos);

                    // Now, for every span added in the new snapshot that replaced
                    // the deleted spans, add it to our span to raise changed events 
                    // over.
                    for (int i = diff.Right.Start; i < diff.Right.End; i++)
                    {
                        pos += difference.InsertedSpans[i].Length;
                    }

                    end = Math.Max(end, pos);
                }

                if (start != int.MaxValue && end != int.MinValue)
                {
                    RaiseTagsChangedEvent(new SnapshotSpan(e.After, Span.FromBounds(start, end)));
                }
            }
        }

        private static int GetMatchSize(ReadOnlyCollection<SnapshotSpan> spans, Match match)
        {
            int size = 0;
            if (match != null)
            {
                Span extent = match.Left;
                for (int s = extent.Start; s < extent.End; ++s)
                {
                    size += spans[s].Length;
                }
            }
            return size;
        }

        private void RaiseTagsChangedEvent(SnapshotSpan span)
        {
            var handler = TagsChanged;
            if (handler != null)
            {
                handler(this, new SnapshotSpanEventArgs(span));
            }
        }

        #endregion
    }
}
