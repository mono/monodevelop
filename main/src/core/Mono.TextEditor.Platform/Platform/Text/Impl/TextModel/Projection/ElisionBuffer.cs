namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Utilities;

    internal sealed class ElisionBuffer : BaseProjectionBuffer, IElisionBuffer
    {
        #region ElisionEdit class
        private class ElisionEdit : Edit, ISubordinateTextEdit
        {
            private ElisionBuffer elisionBuffer;
            private bool subordinate;

            public ElisionEdit(ElisionBuffer elisionBuffer, ITextSnapshot originSnapshot, EditOptions options, int? reiteratedVersionNumber, object editTag)
                : base(elisionBuffer, originSnapshot, options, reiteratedVersionNumber, editTag)
            {
                this.elisionBuffer = elisionBuffer;
                this.subordinate = true;
            }

            public ITextBuffer TextBuffer
            {
                get { return this.elisionBuffer; }
            }

            // this is the master edit path -- initiated from outside
            protected override ITextSnapshot PerformApply()
            {
                CheckActive();
                this.applied = true;
                this.subordinate = false;

                ITextSnapshot result = this.baseBuffer.currentSnapshot;

                if (this.changes.Count > 0)
                {
                    this.elisionBuffer.group.PerformMasterEdit(this.elisionBuffer, this, this.options, this.editTag);

                    if (!this.Canceled)
                    {
                        result = this.baseBuffer.currentSnapshot;
                    }
                }
                else
                {
                    // vacuous edit
                    this.baseBuffer.editInProgress = false;
                }

                return result;
            }

            public void PreApply()
            {
                if (this.changes.Count > 0)
                {
                    this.elisionBuffer.ComputeSourceEdits(this.changes);
                }
            }

            public void FinalApply()
            {
                if (this.changes.Count > 0 || this.elisionBuffer.pendingContentChangedEventArgs.Count > 0)
                {
                    this.elisionBuffer.group.CancelIndependentEdit(this.elisionBuffer);   // just in case
                    TextContentChangedEventRaiser raiser = this.elisionBuffer.IncorporateChanges();
                    this.baseBuffer.group.EnqueueEvents(raiser, this.baseBuffer);
                    raiser.RaiseEvent(this.baseBuffer, true);
                }

                this.elisionBuffer.editInProgress = false;
                this.elisionBuffer.editApplicationInProgress = false;
                if (this.subordinate)
                {
                    this.elisionBuffer.group.FinishEdit();
                }
            }

            public override void CancelApplication()
            {
                if (!this.Canceled)
                {
                    base.CancelApplication();
                    this.elisionBuffer.editApplicationInProgress = false;
                    this.elisionBuffer.pendingContentChangedEventArgs.Clear();
                }
            }
        }
        #endregion

        #region Private members, Construction, and Disposal
        private ElisionBufferOptions elisionOptions;
        private ElisionMap content;
        private ElisionSnapshot currentElisionSnapshot;
        private readonly ITextBuffer sourceBuffer;
        private ITextSnapshot sourceSnapshot;
        private WeakEventHook eventHook;

        public ElisionBuffer(IProjectionEditResolver resolver, 
                             IContentType contentType, 
                             ITextBuffer sourceBuffer,
                             NormalizedSpanCollection exposedSpans,
                             ElisionBufferOptions options,
                             ITextDifferencingService textDifferencingService,
                             GuardedOperations guardedOperations)
            : base(resolver, contentType, textDifferencingService, guardedOperations)
        {
            Debug.Assert(sourceBuffer != null);
            this.sourceBuffer = sourceBuffer;
            this.sourceSnapshot = sourceBuffer.CurrentSnapshot;

            BaseBuffer baseSourceBuffer = (BaseBuffer)sourceBuffer;

            this.eventHook = new WeakEventHook(this, baseSourceBuffer);

            this.group = baseSourceBuffer.group;
            this.group.AddMember(this);

            this.content = new ElisionMap(this.sourceSnapshot, exposedSpans);
            this.elisionOptions = options;
            this.currentVersion.InternalLength = content.Length;
            this.currentElisionSnapshot = new ElisionSnapshot(this, this.sourceSnapshot, base.currentVersion, this.content, (options & ElisionBufferOptions.FillInMappingMode) != 0);
            this.currentSnapshot = this.currentElisionSnapshot;
        }
        #endregion

        #region Source Buffer
        public override IList<ITextBuffer> SourceBuffers 
        {
            get { return new FrugalList<ITextBuffer>() { this.sourceBuffer }; }
        }

        public ITextBuffer SourceBuffer 
        {
            get { return this.sourceBuffer; }
        }

        public ElisionBufferOptions Options
        {
            get { return this.elisionOptions; }
        }

        #endregion

        #region ElisionSourceSpansChangedEventRaiser Class
        private class ElisionSourceSpansChangedEventRaiser : ITextEventRaiser
        {
            private readonly ElisionSourceSpansChangedEventArgs args;

            public ElisionSourceSpansChangedEventRaiser(ElisionSourceSpansChangedEventArgs args)
            {
                this.args = args;
            }

            public void RaiseEvent(BaseBuffer baseBuffer, bool immediate)
            {
                ElisionBuffer elBuffer = (ElisionBuffer)baseBuffer;
                EventHandler<ElisionSourceSpansChangedEventArgs> spanHandlers = elBuffer.SourceSpansChanged;
                if (spanHandlers != null)
                {
                    spanHandlers(this, args);
                }

                // now raise the text content changed event
                baseBuffer.RawRaiseEvent(args, immediate);
            }

            public bool HasPostEvent
            {
                get { return false; }
            }
        }
        #endregion

        #region Span Editing
        private class SpanEdit : TextBufferBaseEdit
        {
            private readonly ElisionBuffer elBuffer;

            public SpanEdit(ElisionBuffer elBuffer): base(elBuffer)
            {
                this.elBuffer = elBuffer;
            }

            public IProjectionSnapshot Apply(NormalizedSpanCollection spansToElide, NormalizedSpanCollection spansToExpand)
            {
                this.applied = true;
                try
                {
                    if (spansToElide == null)
                    {
                        spansToElide = NormalizedSpanCollection.Empty;
                    }
                    if (spansToExpand == null)
                    {
                        spansToExpand = NormalizedSpanCollection.Empty;
                    }
                    if (spansToElide.Count > 0 || spansToExpand.Count > 0)
                    {
                        if ((spansToElide.Count > 0) && (spansToElide[spansToElide.Count - 1].End > this.elBuffer.sourceSnapshot.Length))
                        {
                            throw new ArgumentOutOfRangeException("spansToElide");
                        }
                        if ((spansToExpand.Count > 0) && (spansToExpand[spansToExpand.Count - 1].End > this.elBuffer.sourceSnapshot.Length))
                        {
                            throw new ArgumentOutOfRangeException("spansToExpand");
                        }
                        ElisionSourceSpansChangedEventArgs args = this.elBuffer.ApplySpanChanges(spansToElide, spansToExpand);
                        if (args != null)
                        {
                            ElisionSourceSpansChangedEventRaiser raiser = new ElisionSourceSpansChangedEventRaiser(args);
                            this.baseBuffer.group.EnqueueEvents(raiser, this.baseBuffer);
                            raiser.RaiseEvent(this.baseBuffer, true);
                        }
                        this.baseBuffer.editInProgress = false;
                    }
                    else
                    {
                        this.baseBuffer.editInProgress = false;
                    }
                }
                finally
                {
                    this.baseBuffer.group.FinishEdit();
                }
                return this.elBuffer.currentElisionSnapshot;
            }
        }

        public IProjectionSnapshot ElideSpans(NormalizedSpanCollection spansToElide)
        {
            if (spansToElide == null)
            {
                throw new ArgumentNullException("spansToElide");
            }
            return ModifySpans(spansToElide, null);
        }

        public IProjectionSnapshot ExpandSpans(NormalizedSpanCollection spansToExpand)
        {
            if (spansToExpand == null)
            {
                throw new ArgumentNullException("spansToExpand");
            }
            return ModifySpans(null, spansToExpand);
        }

        public IProjectionSnapshot ModifySpans(NormalizedSpanCollection spansToElide, NormalizedSpanCollection spansToExpand)
        {
            using (SpanEdit spedit = new SpanEdit(this))
            {
                return spedit.Apply(spansToElide, spansToExpand);
            }
        }
        #endregion

        public override ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            return new ElisionEdit(this, this.currentElisionSnapshot, options, reiteratedVersionNumber, editTag);
        }

        protected internal override ISubordinateTextEdit CreateSubordinateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            return new ElisionEdit(this, this.currentElisionSnapshot, options, reiteratedVersionNumber, editTag);
        }

        internal void ComputeSourceEdits(FrugalList<TextChange> changes)
        {
            ITextEdit xedit = this.group.GetEdit((BaseBuffer)this.sourceBuffer);
            foreach (TextChange change in changes)
            {
                if (change.OldLength > 0)
                {
                    IList<SnapshotSpan> sourceDeletionSpans = this.currentElisionSnapshot.MapToSourceSnapshots(new Span(change.OldPosition, change.OldLength));
                    foreach (SnapshotSpan sourceDeletionSpan in sourceDeletionSpans)
                    {
                        xedit.Delete(sourceDeletionSpan);
                    }
                }
                if (change.NewLength > 0)
                {
                    // change includes an insertion
                    ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints = this.currentElisionSnapshot.MapInsertionPointToSourceSnapshots(change.OldPosition, null);

                    if (sourceInsertionPoints.Count == 1)
                    {
                        // the insertion point is unambiguous
                        xedit.Insert(sourceInsertionPoints[0].Position, change.NewText);
                    }
                    else
                    {
                        // the insertion is at the boundary of source spans
                        int[] insertionSizes = new int[sourceInsertionPoints.Count];

                        if (this.resolver != null)
                        {
                            this.resolver.FillInInsertionSizes(new SnapshotPoint(this.currentElisionSnapshot, change.OldPosition),
                                                               sourceInsertionPoints, change.NewText, insertionSizes);
                        }

                        // if resolver was not provided, we just use zeros for the insertion sizes, which will push the entire insertion 
                        // into the last slot.

                        int pos = 0;
                        for (int i = 0; i < insertionSizes.Length; ++i)
                        {
                            // contend with any old garbage that the client passed back.
                            int size = (i == insertionSizes.Length - 1)
                                            ? change.NewLength - pos
                                            : Math.Min(insertionSizes[i], change.NewLength - pos);
                            if (size > 0)
                            {
                                xedit.Insert(sourceInsertionPoints[i].Position, change.NewText.Substring(pos, size));
                                pos += size;
                                if (pos == change.NewLength)
                                {
                                    break;  // inserted text is used up, whether we've visited all of the insertionSizes or not
                                }
                            }
                        }
                    }
                }
            }
            this.editApplicationInProgress = true;
        }

        public override BaseBuffer.ITextEventRaiser PropagateSourceChanges(EditOptions options, object editTag)
        {
            TextContentChangedEventRaiser raiser = IncorporateChanges();
            raiser.RaiseEvent(this, true);
            return raiser;
        }

        #region ChangeApplication

        private ElisionSourceSpansChangedEventArgs ApplySpanChanges(NormalizedSpanCollection spansToElide, NormalizedSpanCollection spansToExpand)
        {
            ElisionSnapshot beforeSnapshot = this.currentElisionSnapshot;
            FrugalList<TextChange> textChanges;
            ElisionMap newContent = this.content.EditSpans(this.sourceSnapshot, spansToElide, spansToExpand, out textChanges);
            if (newContent != this.content)
            {
                this.content = newContent;
                INormalizedTextChangeCollection normalizedChanges = NormalizedTextChangeCollection.Create(textChanges);
                SetCurrentVersionAndSnapshot(normalizedChanges);
                return new ElisionSourceSpansChangedEventArgs(beforeSnapshot, this.currentElisionSnapshot,
                                                              spansToElide, spansToExpand, null);
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Snapshots

        public override IProjectionSnapshot CurrentSnapshot
        {
            get { return this.currentElisionSnapshot; }
        }

        protected override BaseProjectionSnapshot CurrentBaseSnapshot
        {
            get { return this.currentElisionSnapshot; }
        }

        IElisionSnapshot IElisionBuffer.CurrentSnapshot
        {
            get { return this.currentElisionSnapshot; }
        }

        protected override BaseSnapshot TakeSnapshot()
        {
            this.currentElisionSnapshot = 
                new ElisionSnapshot(this, this.sourceSnapshot, this.currentVersion, this.content, 
                                    (this.elisionOptions & ElisionBufferOptions.FillInMappingMode) != 0);
            return this.currentElisionSnapshot;
        }
        #endregion

        private TextContentChangedEventRaiser IncorporateChanges()
        {
            Debug.Assert(this.sourceSnapshot == this.pendingContentChangedEventArgs[0].Before);
            FrugalList<TextChange> projectedChanges = new FrugalList<TextChange>();

            var args0 = this.pendingContentChangedEventArgs[0];
            INormalizedTextChangeCollection sourceChanges;

            // Separate the easy and common case:
            if (this.pendingContentChangedEventArgs.Count == 1)
            {
                sourceChanges = args0.Changes;
                this.sourceSnapshot = args0.After;
            }
            else
            {
                // there is more than one snapshot of the source buffer to deal with. Since the changes may be 
                // interleaved by position, we need to get a normalized list in sequence. First we denormalize the
                // changes so they are all relative to the same single starting snapshot, then we normalize them again into
                // a single list.

                // This relies crucially on the fact that we know something about the multiple snapshots: they were
                // induced by projection span adjustments, and the changes across them are independent. That is to say,
                // it is not the case that text inserted in one snapshot is deleted in a later snapshot in the series.

                DumpPendingContentChangedEventArgs();
                List<TextChange> denormalizedChanges = new List<TextChange>() { new TextChange(int.MaxValue, "", "", LineBreakBoundaryConditions.None) };
                for (int a = 0; a < this.pendingContentChangedEventArgs.Count; ++a)
                {
                    NormalizedTextChangeCollection.Denormalize(this.pendingContentChangedEventArgs[a].Changes, denormalizedChanges);
                }
                DumpPendingChanges(new List<Tuple<ITextBuffer, List<TextChange>>>() { new Tuple<ITextBuffer, List<TextChange>>(this.sourceBuffer, denormalizedChanges) } );
                FrugalList<TextChange> slicedChanges = new FrugalList<TextChange>();

                // remove the sentinel
                for (int d = 0; d < denormalizedChanges.Count - 1; ++d)
                {
                    slicedChanges.Add(denormalizedChanges[d]);
                }
                sourceChanges = NormalizedTextChangeCollection.Create(slicedChanges);
                this.sourceSnapshot = this.pendingContentChangedEventArgs[this.pendingContentChangedEventArgs.Count - 1].After;
            }

            if (sourceChanges.Count > 0)
            {
                this.content = this.content.IncorporateChanges(sourceChanges, projectedChanges, args0.Before, this.sourceSnapshot, this.currentElisionSnapshot);
            }

            this.pendingContentChangedEventArgs.Clear();
            ElisionSnapshot beforeSnapshot = this.currentElisionSnapshot;
            SetCurrentVersionAndSnapshot(NormalizedTextChangeCollection.Create(projectedChanges));
            this.editApplicationInProgress = false;
            return new TextContentChangedEventRaiser(beforeSnapshot, this.currentElisionSnapshot, args0.Options, args0.EditTag);
        }

        #region Event Handling
        internal override void OnSourceTextChanged(object sender, TextContentChangedEventArgs e)
        {
            this.pendingContentChangedEventArgs.Add(e);

            if (!this.editApplicationInProgress)
            {
                // We had better be a member of the same group as the buffer that we just heard from
                Debug.Assert(this.group.MembersContains(e.After.TextBuffer));
                // Let the buffer group decide when to issue events (this allows us to coalesce multiple snapshots
                // from the source buffer (this can happen if the source buffer is a projection buffer) into a single snapshot here.
                this.group.ScheduleIndependentEdit(this);
            }
        }
        #endregion

        #region Public Events
        public event EventHandler<ElisionSourceSpansChangedEventArgs> SourceSpansChanged;
        #endregion
    }
}
