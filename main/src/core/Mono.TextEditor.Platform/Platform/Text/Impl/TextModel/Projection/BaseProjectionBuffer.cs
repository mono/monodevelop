namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Utilities;
    using System.Collections.ObjectModel;

    internal abstract class BaseProjectionBuffer : BaseBuffer, IProjectionBufferBase
    {
        #region State and construction
        protected internal readonly IProjectionEditResolver resolver;
        protected bool editApplicationInProgress;
        protected List<TextContentChangedEventArgs> pendingContentChangedEventArgs = new List<TextContentChangedEventArgs>();

        protected BaseProjectionBuffer(IProjectionEditResolver resolver, IContentType contentType, ITextDifferencingService textDifferencingService, GuardedOperations guardedOperations)
            : base(contentType, 0, textDifferencingService, guardedOperations)
        {
            this.resolver = resolver;   // null is OK
        }
        #endregion

        #region Source buffer event handling
        internal abstract void OnSourceTextChanged(object sender, TextContentChangedEventArgs e);

        internal virtual void OnSourceBufferReadOnlyRegionsChanged(object sender, SnapshotSpanEventArgs e)
        {
            NormalizedSpanCollection mappedAffectedSpans = new NormalizedSpanCollection(this.CurrentBaseSnapshot.MapFromSourceSnapshot(e.Span));

            if (mappedAffectedSpans.Count > 0)
            {
                ITextEventRaiser raiser = 
                    new ReadOnlyRegionsChangedEventRaiser(new SnapshotSpan(this.currentSnapshot, 
                                                                           Span.FromBounds(mappedAffectedSpans[0].Start, 
                                                                                           mappedAffectedSpans[mappedAffectedSpans.Count - 1].End)));

                this.group.BeginEdit();
                this.group.EnqueueEvents(raiser, this);
                this.group.FinishEdit();
            }
        }

        internal void OnSourceBufferContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            TextContentChangedEventArgs args = new TextContentChangedEventArgs(e.Before, e.After, EditOptions.None, e.EditTag);
            this.pendingContentChangedEventArgs.Add(args);
            this.group.ScheduleIndependentEdit(this);
        }
        #endregion

        #region Editing Shortcuts
        public new IProjectionSnapshot Insert(int position, string text)
        {
            return (IProjectionSnapshot)base.Insert(position, text);
        }

        public new IProjectionSnapshot Delete(Span deleteSpan)
        {
            return (IProjectionSnapshot)base.Delete(deleteSpan);
        }

        public new IProjectionSnapshot Replace(Span replaceSpan, string replaceWith)
        {
            return (IProjectionSnapshot)base.Replace(replaceSpan, replaceWith);
        }
        #endregion

        #region Read Only Region support
        protected internal override bool IsReadOnlyImplementation(int position, bool isEdit)
        {
            if (this.CurrentBaseSnapshot.SpanCount == 0)
            {
                throw new InvalidOperationException();
            }
            if (base.IsReadOnlyImplementation(position, isEdit))
            {
                return true;
            }
            return AreBaseBuffersReadOnly(position, isEdit);
        }

        private bool AreBaseBuffersReadOnly(int position, bool isEdit)
        {
            // a position on a seam between two or more source spans is readonly only
            // if that position is readonly in all of the source buffers.
            ReadOnlyCollection<SnapshotPoint> snapPoints = this.CurrentBaseSnapshot.MapInsertionPointToSourceSnapshots(position, null);
            foreach (SnapshotPoint snapPoint in snapPoints)
            {
                BaseBuffer baseBuffer = (BaseBuffer)snapPoint.Snapshot.TextBuffer;
                if (!baseBuffer.IsReadOnlyImplementation(snapPoint.Position, isEdit))
                {
                    return false;
                }
            }
            return true;
        }

        protected internal override bool IsReadOnlyImplementation(Span span, bool isEdit)
        {
            if (this.CurrentSnapshot.SpanCount == 0)
            {
                throw new InvalidOperationException();
            }
            if (base.IsReadOnlyImplementation(span, isEdit))
            {
                return true;
            }
            return AreBaseBuffersReadOnly(span, isEdit);
        }

        public bool AreBaseBuffersReadOnly(Span span, bool isEdit)
        {
            if (span.Length == 0)
            {
                // treat like an insertion!
                return AreBaseBuffersReadOnly(span.Start, isEdit);
            }
            else
            {
                IList<SnapshotSpan> snapSpans = this.CurrentBaseSnapshot.MapToSourceSnapshots(span);
                foreach (SnapshotSpan snapSpan in snapSpans)
                {
                    BaseBuffer baseBuffer = (BaseBuffer)snapSpan.Snapshot.TextBuffer;
                    if (baseBuffer.IsReadOnlyImplementation(snapSpan, isEdit))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        protected internal override NormalizedSpanCollection GetReadOnlyExtentsImplementation(Span span)
        {
            // TODO: make something other than dead slow

            FrugalList<Span> result = new FrugalList<Span>(base.GetReadOnlyExtentsImplementation(span));

            IList<SnapshotSpan> restrictionSpans = this.CurrentBaseSnapshot.MapToSourceSnapshotsForRead(span);
            foreach (SnapshotSpan restrictionSpan in restrictionSpans)
            {
                SnapshotSpan? overlapSpan = (restrictionSpan.Span == span) ? restrictionSpan : restrictionSpan.Overlap(span);
                if (overlapSpan.HasValue)
                {
                    BaseBuffer baseBuffer = (BaseBuffer)restrictionSpan.Snapshot.TextBuffer;
                    NormalizedSpanCollection sourceExtents = baseBuffer.GetReadOnlyExtents(overlapSpan.Value);
                    foreach (Span sourceExtent in sourceExtents)
                    {
                        result.AddRange(this.CurrentBaseSnapshot.MapFromSourceSnapshot(new SnapshotSpan(restrictionSpan.Snapshot, sourceExtent)));
                    }
                }
            }

            return new NormalizedSpanCollection(result);
        }
        #endregion

        #region Abstract members
        protected abstract BaseProjectionSnapshot CurrentBaseSnapshot { get; }

        public override abstract ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag);

        protected internal override abstract ISubordinateTextEdit CreateSubordinateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag);

        protected override abstract BaseSnapshot TakeSnapshot();

        public new abstract IProjectionSnapshot CurrentSnapshot { get; }

        public abstract IList<ITextBuffer> SourceBuffers { get; }

        public abstract BaseBuffer.ITextEventRaiser PropagateSourceChanges(EditOptions options, object editTag);
        #endregion

        #region Debug support
        [Conditional("_DEBUG")]
        protected void DumpPendingChanges(List<Tuple<ITextBuffer, List<TextChange>>> pendingSourceChanges)
        {
            if (BufferGroup.Tracing)
            {
                StringBuilder sb = new StringBuilder(string.Format(System.Globalization.CultureInfo.CurrentCulture, "Pending Changes in {0}\r\n", TextUtilities.GetTag(this)));
                foreach (var p in pendingSourceChanges)
                {
                    sb.AppendLine(TextUtilities.GetTag(p.Item1));
                    for (int c = 0; c < p.Item2.Count - 1; ++c)    // don't display sentinel
                    {
                        sb.AppendLine(p.Item2[c].ToString());
                    }
                }
                sb.AppendLine("");
                Debug.Write(sb.ToString());
            }
        }

        [Conditional("_DEBUG")]
        protected void DumpPendingContentChangedEventArgs()
        {
            if (BufferGroup.Tracing)
            {
                StringBuilder sb = new StringBuilder(string.Format(System.Globalization.CultureInfo.CurrentCulture, "Pending Change Events in {0}\r\n", TextUtilities.GetTag(this)));
                foreach (var args in this.pendingContentChangedEventArgs)
                {
                    sb.Append(TextUtilities.GetTag(args.Before.TextBuffer));
                    sb.Append(" V");
                    sb.AppendLine(args.After.Version.VersionNumber.ToString());
                    foreach (var change in args.Changes)
                    {
                        sb.AppendLine(change.ToString());
                    }
                }
                sb.AppendLine("");
                Debug.Write(sb.ToString());
            }
        }
        #endregion
    }
}
