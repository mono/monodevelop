// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    // todo: denormalizations that are performed may possibly be removed by exploiting OldPosition
    // information in TextChange. Investigate this.

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Text.Differencing;

    using Strings = Microsoft.VisualStudio.Text.Implementation.Strings;

    internal sealed partial class ProjectionBuffer : BaseProjectionBuffer, IProjectionBuffer
    {
        #region ProjectionEdit Class
        private class ProjectionEdit : Edit, ISubordinateTextEdit
        {
            private ProjectionBuffer projectionBuffer;
            private bool subordinate;

            public ProjectionEdit(ProjectionBuffer projectionBuffer, ITextSnapshot originSnapshot, EditOptions options, int? reiteratedVersionNumber, object editTag)
                : base(projectionBuffer, originSnapshot, options, reiteratedVersionNumber, editTag)
            {
                this.projectionBuffer = projectionBuffer;
                this.subordinate = true;
            }

            public ITextBuffer TextBuffer
            {
                get { return this.projectionBuffer; }
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
                    this.projectionBuffer.group.PerformMasterEdit(this.projectionBuffer, this, this.options, this.editTag);

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
                // called for all non-vacuous edits
                if (this.changes.Count > 0)
                {
                    this.projectionBuffer.ComputeSourceEdits(this.changes);
                }
            }

            public void FinalApply()    // TODO: make FinalApply return event raisers, eliminate FinishEdit()
            {
                // called for all non-vacuous edits
                if (this.changes.Count > 0 || this.projectionBuffer.pendingContentChangedEventArgs.Count > 0)
                {
                    this.projectionBuffer.group.CancelIndependentEdit(this.projectionBuffer);   // just in case
                    IList<ITextEventRaiser> eventRaisers = this.projectionBuffer.InterpretSourceChanges(this.options, /*this.reiteratedVersionNumber,*/ this.editTag);
                    this.projectionBuffer.group.EnqueueEvents(eventRaisers, this.baseBuffer);

                    // raise immediate events
                    foreach (var raiser in eventRaisers)
                    {
                        raiser.RaiseEvent(this.baseBuffer, true);
                    }
                }

                this.projectionBuffer.editInProgress = false;
                if (this.subordinate)
                {
                    this.baseBuffer.group.FinishEdit();
                }
            }

            public override void CancelApplication()
            {
                if (!this.canceled)
                {
                    base.CancelApplication();
                    this.projectionBuffer.editApplicationInProgress = false;
                    this.projectionBuffer.pendingContentChangedEventArgs.Clear();
                }
            }
        }
        #endregion

        #region SourceBufferSet Class
        /// <summary>
        /// Tracks the Source TextBuffers of this ProjectionBuffer. There is exactly one SourceBufferSet
        /// per ProjectionBufferImpl.
        /// </summary>
        private class SourceBufferSet
        {
            private class BufferTracker
            {
                public ITextBuffer _buffer;
                public int _spanCount;
                public BufferTracker(ITextBuffer buffer)
                {
                    _buffer = buffer;
                    _spanCount = 1;
                }
            }

            // presumption: the number of source buffers is small
            private bool _inTransaction;
            private List<BufferTracker> _sourceBufferTrackers = new List<BufferTracker>();
            private FrugalList<ITextBuffer> _addedBuffers;
            private FrugalList<ITextBuffer> _removedBuffers;

            private int Find(ITextBuffer buffer)
            {
                for (int i = 0; i < _sourceBufferTrackers.Count; ++i)
                {
                    if (buffer == _sourceBufferTrackers[i]._buffer)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public List<ITextBuffer> SourceBuffers
            {
                get
                {
                    List<ITextBuffer> sourceBuffers = new List<ITextBuffer>();
                    foreach (BufferTracker bt in _sourceBufferTrackers)
                    {
                        if (!sourceBuffers.Contains(bt._buffer))
                        {
                            sourceBuffers.Add(bt._buffer);
                        }
                    }
                    return sourceBuffers;
                }
            }

            public void StartTransaction()
            {
                Debug.Assert(!_inTransaction);
                _addedBuffers = new FrugalList<ITextBuffer>();
                _removedBuffers = new FrugalList<ITextBuffer>();
                _inTransaction = true;
            }

            public void FinishTransaction(out FrugalList<ITextBuffer> addedBuffers, out FrugalList<ITextBuffer> removedBuffers)
            {
                Debug.Assert(_inTransaction);

                // If a buffer was removed and then added, eliminate it from both lists.
                // Since these lists should be extremely short, nothing fancy here.
                FrugalList<ITextBuffer> comingAndGoingBuffers = new FrugalList<ITextBuffer>();
                foreach (ITextBuffer buffer in _addedBuffers)
                {
                    if (_removedBuffers.Contains(buffer))
                    {
                        comingAndGoingBuffers.Add(buffer);
                    }
                }
                foreach (ITextBuffer buffer in comingAndGoingBuffers)
                {
                    _addedBuffers.Remove(buffer);
                    _removedBuffers.Remove(buffer);
                }

                addedBuffers = _addedBuffers;
                removedBuffers = _removedBuffers;
                _addedBuffers = null;
                _removedBuffers = null;
                _inTransaction = false;
            }

            public void AddSpan(ITrackingSpan span)
            {
                Debug.Assert(_inTransaction);
                int i = Find(span.TextBuffer);
                if (i < 0)
                {
                    _sourceBufferTrackers.Add(new BufferTracker(span.TextBuffer));
                    _addedBuffers.Add(span.TextBuffer);
                }
                else
                {
                    _sourceBufferTrackers[i]._spanCount++;
                }
            }

            public void RemoveSpan(ITrackingSpan span)
            {
                Debug.Assert(_inTransaction);
                int i = Find(span.TextBuffer);
                Debug.Assert(i >= 0);
                if (--_sourceBufferTrackers[i]._spanCount == 0)
                {
                    _sourceBufferTrackers.RemoveAt(i);
                    _removedBuffers.Add(span.TextBuffer);
                }
            }
        }

        public override IList<ITextBuffer> SourceBuffers
        {
            // this is problematic, but we need it until the buffer graph implementation catches up to the new world
            get
            {
                return this.sourceBufferSet.SourceBuffers;
            }
        }
        #endregion

        #region Private State
        private ProjectionBufferOptions bufferOptions;
        private IDifferenceService differenceService;
        private List<ITrackingSpan> sourceSpans = new List<ITrackingSpan>();
        private SourceBufferSet sourceBufferSet = new SourceBufferSet();
        private ProjectionSnapshot currentProjectionSnapshot;

        private IInternalTextBufferFactory textBufferFactory;
        internal ITextBuffer literalBuffer;
        private IReadOnlyRegion literalBufferRor;

        private List<WeakEventHook> eventHooks = new List<WeakEventHook>();
        #endregion

        #region Construction
        public ProjectionBuffer(IInternalTextBufferFactory textBufferFactory,
                                IProjectionEditResolver resolver, 
                                IContentType contentType, 
                                IList<object> initialSourceSpans, 
                                IDifferenceService differenceService,
                                ITextDifferencingService textDifferencingService,
                                ProjectionBufferOptions options,
                                GuardedOperations guardedOperations)
            : base(resolver, contentType, textDifferencingService, guardedOperations)
        {
            // Parameters are validated outside
            Debug.Assert(initialSourceSpans != null);
            this.textBufferFactory = textBufferFactory;
            this.differenceService = differenceService;
            this.bufferOptions = options;

            SpanManager spanManager = new SpanManager(this, 0, 0, initialSourceSpans, false, false);
            spanManager.PerformChecks();
            spanManager.ProcessLiteralSpans();

            int spanPos = 0;
            this.sourceBufferSet.StartTransaction();
            int initialLength = 0;
            List<SnapshotSpan> snapshotSpans = new List<SnapshotSpan>();
            foreach (ITrackingSpan initialTrackingSpan in spanManager.SpansToInsert)
            {
                AddSpan(spanPos++, initialTrackingSpan);
                SnapshotSpan snapSpan = new SnapshotSpan(initialTrackingSpan.TextBuffer.CurrentSnapshot,
                                                         initialTrackingSpan.GetSpan(initialTrackingSpan.TextBuffer.CurrentSnapshot));
                initialLength += snapSpan.Length;
                snapshotSpans.Add(snapSpan);
            }
            this.currentVersion.InternalLength = initialLength; // this is a bit hacky

            FrugalList<ITextBuffer> addedBuffers;
            FrugalList<ITextBuffer> removedBuffers;
            this.sourceBufferSet.FinishTransaction(out addedBuffers, out removedBuffers);

            // listen to changes to source buffers
            bool firstAddedBuffer = true;
            BufferGroup chosenGroup = null;
            foreach (ITextBuffer addedBuffer in addedBuffers)
            {
                BaseBuffer baseAddedBuffer = (BaseBuffer)addedBuffer;
                if (firstAddedBuffer)
                {
                    firstAddedBuffer = false;
                    chosenGroup = baseAddedBuffer.group;
                    chosenGroup.AddMember(this);
                }
                else
                {
                    chosenGroup.Swallow(baseAddedBuffer.group);
                }
                if ((baseAddedBuffer != this.literalBuffer) || ((this.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) != 0))
                {
                    this.eventHooks.Add(new WeakEventHook(this, baseAddedBuffer));
                }
            }
            this.group = chosenGroup ?? new BufferGroup(this);

            this.currentProjectionSnapshot = new ProjectionSnapshot(this, base.currentVersion, snapshotSpans);
            this.ProtectedCurrentSnapshot = this.currentProjectionSnapshot;
        }
        #endregion

        #region Span Editing and Management
        private class SourceSpansChangedEventRaiser : ITextEventRaiser
        {
            ProjectionSourceSpansChangedEventArgs args;

            public SourceSpansChangedEventRaiser(ProjectionSourceSpansChangedEventArgs args)
            {
                this.args = args;
            }

            public void RaiseEvent(BaseBuffer baseBuffer, bool immediate)
            {
                ProjectionBuffer projBuffer = (ProjectionBuffer)baseBuffer;
                ProjectionSourceBuffersChangedEventArgs bufferArgs = args as ProjectionSourceBuffersChangedEventArgs;
                if (bufferArgs != null)
                {
                    EventHandler<ProjectionSourceBuffersChangedEventArgs> bufferHandlers = 
                        immediate ? projBuffer.SourceBuffersChangedImmediate : projBuffer.SourceBuffersChanged;
                    if (bufferHandlers != null)
                    {
                        bufferHandlers(baseBuffer, bufferArgs);
                    }
                }

                EventHandler<ProjectionSourceSpansChangedEventArgs> spanHandlers =
                    immediate ? projBuffer.SourceSpansChangedImmediate : projBuffer.SourceSpansChanged;
                if (spanHandlers != null)
                {
                    spanHandlers(baseBuffer, args);
                }

                // now raise the text content changed event
                baseBuffer.RawRaiseEvent(args, immediate);
            }

            public bool HasPostEvent
            {
                get { return true; }
            }
        }

        /// <summary>
        /// Perform validity checking and normalization of source spans that are to be inserted. Checks
        /// for spans that are null, of the wrong type (neither string nor ITrackingSpan), would induce
        /// projection buffer cycles, have the wrong tracking mode, or induce projection overlaps.
        /// Also converts string literals to ITrackingSpans over the literal source buffer.
        /// </summary>
        private class SpanManager
        {
            public int Position { get; private set; }
            public int SpansToDelete { get; private set; }
            public List<object> RawSpansToInsert { get; private set; }

            // The set of text buffers that have been previously visited in a cyclic dependency check
            private Dictionary<ITextBuffer, bool> visitedBufferSet = new Dictionary<ITextBuffer, bool>();
            private ProjectionBuffer projBuffer;
            private List<ITrackingSpan> spansToInsert;
            private LiteralBufferHelper lit;
            private bool checkForCycles;

            public SpanManager(ProjectionBuffer projBuffer, int position, int spansToDelete, IList<object> rawSpansToInsert, bool checkForCycles, bool groupEdit)
            {
                this.projBuffer = projBuffer;
                this.checkForCycles = checkForCycles;
                this.lit = new LiteralBufferHelper(projBuffer, (this.projBuffer.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) == 0, groupEdit);

                this.Position = position;
                this.SpansToDelete = spansToDelete;
                this.RawSpansToInsert = new List<object>(rawSpansToInsert);
            }

            public List<ITrackingSpan> SpansToInsert
            {
                get
                {
                    if (this.spansToInsert == null)
                    {
                        this.lit.FinishEdit();
                        this.spansToInsert = new List<ITrackingSpan>();
                        foreach (object rawSpan in this.RawSpansToInsert)
                        {
                            ITrackingSpan trackingSpan = rawSpan as ITrackingSpan;
                            if (trackingSpan == null)
                            {
                                trackingSpan = this.projBuffer.literalBuffer.CurrentSnapshot.CreateTrackingSpan((Span)rawSpan, SpanTrackingMode.EdgeExclusive, TrackingFidelityMode.Forward);
                            }
                            this.spansToInsert.Add(trackingSpan);
                        }
                    }
                    return this.spansToInsert;
                }
            }

            private void CheckForSourceBufferCycle(ITextBuffer buffer)
            {
                // TODO shouldn't we load up the visitedbufferset with existing source buffers first? at least for perf.
                if (!this.visitedBufferSet.ContainsKey(buffer))
                {
                    if (buffer == this.projBuffer)
                    {
                        throw new ArgumentException(Strings.SourceBufferCycle);
                    }
                    this.visitedBufferSet.Add(buffer, true);
                    IProjectionBuffer p = buffer as ProjectionBuffer;
                    if (p != null)
                    {
                        foreach (ITextBuffer sourceBuffer in p.SourceBuffers)
                        {
                            CheckForSourceBufferCycle(sourceBuffer);
                        }
                    }
                }
            }

            private static void CheckTrackingMode(ITrackingSpan spanToInsert)
            {
                if (spanToInsert.TrackingMode != SpanTrackingMode.EdgeExclusive && spanToInsert.TrackingMode != SpanTrackingMode.Custom)
                {
                    ITextSnapshot snap = spanToInsert.TextBuffer.CurrentSnapshot;
                    Span span = spanToInsert.GetSpan(snap);
                    if (spanToInsert.TrackingMode == SpanTrackingMode.EdgeInclusive)
                    {
                        if (span.Start > 0 || span.End < snap.Length)
                        {
                            throw new ArgumentException(Strings.InvalidEdgeInclusiveSourceSpan);
                        }
                    }
                    else if (spanToInsert.TrackingMode == SpanTrackingMode.EdgePositive)
                    {
                        if (span.End < snap.Length)
                        {
                            throw new ArgumentException(Strings.InvalidEdgePositiveSourceSpan);
                        }
                    }
                    else if (span.Start > 0)
                    {
                        throw new ArgumentException(Strings.InvalidEdgeNegativeSourceSpan);
                    }
                }
            }

            private IEnumerable<ITrackingSpan> GetProposedSpans()
            {
                for (int s = 0; s < this.Position; ++s)
                {
                    yield return projBuffer.sourceSpans[s];
                }
                for (int s = this.Position + this.SpansToDelete; s < projBuffer.sourceSpans.Count; ++s)
                {
                    yield return projBuffer.sourceSpans[s];
                }
                for (int s = 0; s < this.RawSpansToInsert.Count; ++s)
                {
                    ITrackingSpan ts = this.RawSpansToInsert[s] as ITrackingSpan;
                    if (ts != null)
                    {
                        yield return ts;
                    }
                }
            }

            /// <summary>
            /// Build a list of ultimate sources for the given span, looking through all projections
            /// </summary>
            /// <param name="span"></param>
            /// <returns></returns>
            private IList<SnapshotSpan> BaseSourceSpans(SnapshotSpan span)
            {
                List<SnapshotSpan> result = new List<SnapshotSpan>();
                if (span.Snapshot.TextBuffer is IProjectionBuffer)
                {
                    foreach (SnapshotSpan s in ((IProjectionSnapshot)span.Snapshot).MapToSourceSnapshots(span))
                    {
                        result.AddRange(BaseSourceSpans(s));
                    }
                }
                else
                {
                    result.Add(span);
                }
                return result;
            }

            private void CheckOverlap()
            {
                Dictionary<ITextBuffer, List<Span>> bufferToSpans = new Dictionary<ITextBuffer, List<Span>>();
                foreach (ITrackingSpan proposedSpan in GetProposedSpans())
                {
                    // Look through all projections
                    IList<SnapshotSpan> baseSpans = BaseSourceSpans(proposedSpan.GetSpan(proposedSpan.TextBuffer.CurrentSnapshot));

                    // Group by source buffer
                    foreach (SnapshotSpan baseSpan in baseSpans)
                    {
                        List<Span> spans;
                        ITextBuffer buffer = baseSpan.Snapshot.TextBuffer;
                        if (!bufferToSpans.TryGetValue(buffer, out spans))
                        {
                            spans = new List<Span>();
                            bufferToSpans.Add(buffer, spans);
                        }
                        spans.Add(baseSpan);
                    }
                }

                foreach (KeyValuePair<ITextBuffer, List<Span>> bufferSpans in bufferToSpans)
                {
                    if (bufferSpans.Value.Count > 1)
                    {
                        // sort and check for overlap.
                        // sort by start position, except if two spans start at the same position sort by
                        // end position so that a null span comes first
                        bufferSpans.Value.Sort(delegate(Span x, Span y) { return x.Start == y.Start ? x.End - y.End : x.Start - y.Start; });
                        for (int s = 1; s < bufferSpans.Value.Count; ++s)
                        {
                            if (bufferSpans.Value[s].Start < bufferSpans.Value[s - 1].End)
                            {
                                throw new ArgumentException(Strings.OverlappingSourceSpans);
                            }
                        }
                    }
                }
            }

            public void PerformChecks()
            {
                foreach (object spanToInsert in this.RawSpansToInsert)
                {
                    if (spanToInsert == null)
                    {
                        throw new ArgumentNullException("spansToInsert");
                    }
                    ITrackingSpan trackingSpanToInsert = spanToInsert as ITrackingSpan;
                    if (trackingSpanToInsert != null)
                    {
                        if (checkForCycles)
                        {
                            try
                            {
                                CheckForSourceBufferCycle(trackingSpanToInsert.TextBuffer);
                            }
                            catch (ArgumentException)
                            {
                                throw new ArgumentException(Strings.SourceBufferCycle);
                            }
                        }
                        if ((this.projBuffer.bufferOptions & ProjectionBufferOptions.PermissiveEdgeInclusiveSourceSpans) == 0)
                        {
                            CheckTrackingMode(trackingSpanToInsert);
                        }
                    }
                    else
                    {
                        if (!(spanToInsert is string))
                        {
                            throw new ArgumentException(Strings.NeitherSpanNorString);
                        }
                    }
                }
                CheckOverlap();
            }

            public void ProcessLiteralSpans()
            {
                // must do deletions first!
                for (int d = this.Position; d < this.Position + this.SpansToDelete; ++d)
                {
                    ITrackingSpan sourceSpan = this.projBuffer.sourceSpans[d];
                    if (sourceSpan.TextBuffer == this.projBuffer.literalBuffer)
                    {
                        this.lit.Delete(sourceSpan);
                    }
                }

                for (int r = 0; r < this.RawSpansToInsert.Count; ++r)
                {
                    object rawSpan = this.RawSpansToInsert[r];
                    string literalSpan = rawSpan as string;
                    if (literalSpan != null)
                    {
                        // change the string into a Span indicating what the bounds of
                        // the span will be when it is ready
                        this.RawSpansToInsert[r] = this.lit.Append(literalSpan);
                    }
                }
            }
        }

        private class LiteralBufferHelper
        {
            private ProjectionBuffer projBuffer;
            bool performedEdit;
            bool readOnly;
            bool groupEdit;
            int totalInsertions;
            int totalDeletions;
            int insertionPoint;

            public LiteralBufferHelper(ProjectionBuffer projBuffer, bool readOnly, bool groupEdit)
            {
                this.projBuffer = projBuffer;
                this.readOnly = readOnly;
                this.groupEdit = groupEdit;
                if (this.projBuffer.literalBuffer != null)
                {
                    this.insertionPoint = this.projBuffer.literalBuffer.CurrentSnapshot.Length;
                }
            }

            private void PrepareEdit()
            {
                if (this.projBuffer.literalBuffer == null)
                {
                    this.projBuffer.literalBuffer = 
                        projBuffer.textBufferFactory.CreateTextBuffer("", projBuffer.textBufferFactory.InertContentType, readOnly);
                    this.insertionPoint = 0;
                }
                else if (this.projBuffer.literalBufferRor != null)
                {
                    using (IReadOnlyRegionEdit rorEdit = this.projBuffer.literalBuffer.CreateReadOnlyRegionEdit())
                    {
                        rorEdit.RemoveReadOnlyRegion(this.projBuffer.literalBufferRor);
                        rorEdit.Apply();
                    }
                    this.projBuffer.literalBufferRor = null;
                }
                this.performedEdit = true;
            }

            public Span Append(string text)
            {
                PrepareEdit();
                Span result;
                if (this.groupEdit)
                {
                    ITextEdit edit = this.projBuffer.group.GetEdit((BaseBuffer)this.projBuffer.literalBuffer, EditOptions.None);
                    edit.Insert(this.insertionPoint, text);
                    result = new Span(this.insertionPoint + this.totalInsertions - this.totalDeletions, text.Length);
                    this.totalInsertions += text.Length;
                }
                else
                {
                    ITextSnapshot literalSnap = projBuffer.literalBuffer.CurrentSnapshot;
                    int length = literalSnap.Length;
                    literalSnap = projBuffer.literalBuffer.Insert(length, text);
                    result = new Span(length, text.Length);
                }
                return result;
            }

            public void Delete(ITrackingSpan trackingSpan)
            {
                PrepareEdit();
                Span span = trackingSpan.GetSpan(projBuffer.literalBuffer.CurrentSnapshot);
                if (this.groupEdit)
                {
                    ITextEdit edit = this.projBuffer.group.GetEdit((BaseBuffer)this.projBuffer.literalBuffer, EditOptions.None);
                    edit.Delete(span);
                    totalDeletions += span.Length;
                }
                else
                {
                    projBuffer.literalBuffer.Delete(span);
                }
            }

            public void FinishEdit()
            {
                if (this.performedEdit && this.readOnly)
                {
                    if (this.projBuffer.literalBuffer != null)
                    {
                        Debug.Assert(this.projBuffer.literalBufferRor == null);
                        using (IReadOnlyRegionEdit rorEdit = this.projBuffer.literalBuffer.CreateReadOnlyRegionEdit())
                        {
                            this.projBuffer.literalBufferRor =
                                rorEdit.CreateReadOnlyRegion(new Span(0, rorEdit.Snapshot.Length), SpanTrackingMode.EdgeInclusive, EdgeInsertionMode.Deny);
                            rorEdit.Apply();
                        }
                    }
                }
            }
        }

        private class SpanEdit : TextBufferBaseEdit, ISubordinateTextEdit
        {
            private ProjectionBuffer projBuffer;
            private EditOptions editOptions = EditOptions.None;
            private object tag = null;
            private SpanManager spanManager;

            public SpanEdit(ProjectionBuffer projBuffer) : base(projBuffer)
            {
                this.projBuffer = projBuffer;
            }

            public ITextBuffer TextBuffer
            {
                get { return this.projBuffer; }
            }

            public IProjectionSnapshot ReplaceSpans(int position, int spansToReplace, IList<object> spansToInsert, EditOptions options, object editTag)
            {
                if (position < 0 || position > this.projBuffer.sourceSpans.Count)
                {
                    throw new ArgumentOutOfRangeException("position");
                }
                if (spansToReplace < 0 || position + spansToReplace > this.projBuffer.sourceSpans.Count)
                {
                    throw new ArgumentOutOfRangeException("spansToReplace");
                }
                if (spansToInsert == null)
                {
                    throw new ArgumentNullException("spansToInsert");
                }

                this.spanManager = new SpanManager(this.projBuffer, position, spansToReplace, spansToInsert, true, (this.projBuffer.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) != 0);
                this.editOptions = options;
                this.tag = editTag;

                this.spanManager.PerformChecks();

                // we are committed!

                this.applied = true;
                if (this.spanManager.SpansToDelete > 0 || this.spanManager.RawSpansToInsert.Count > 0)
                {
                    // non-vacuous
                    this.projBuffer.group.PerformMasterEdit(this.projBuffer, this, this.editOptions, this.tag);
                }
                this.baseBuffer.group.FinishEdit();
                this.baseBuffer.editInProgress = false;
                return this.projBuffer.currentProjectionSnapshot;
            }

            public void PreApply()
            {
                this.projBuffer.editApplicationInProgress = true;
                this.spanManager.ProcessLiteralSpans();
            }

            public void FinalApply()
            {
                ProjectionSourceSpansChangedEventArgs args = this.projBuffer.ApplySpanChanges(this.spanManager.Position, this.spanManager.SpansToDelete, this.spanManager.SpansToInsert, this.editOptions, this.tag);
                SourceSpansChangedEventRaiser raiser = new SourceSpansChangedEventRaiser(args);
                this.baseBuffer.group.EnqueueEvents(raiser, this.baseBuffer);
                raiser.RaiseEvent(this.baseBuffer, true);
                this.baseBuffer.editInProgress = false;
                this.projBuffer.editApplicationInProgress = false;

                if ((this.projBuffer.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) != 0)
                {
                    // the only pending changes should be changes to our literal buffer
                    Debug.Assert(this.projBuffer.pendingContentChangedEventArgs.Count <= 1);
                    if (this.projBuffer.pendingContentChangedEventArgs.Count == 1)
                    {
                        Debug.Assert(this.projBuffer.pendingContentChangedEventArgs[0].Before.TextBuffer == this.projBuffer.literalBuffer);
                    }
                    // forget about changes to our literal buffer; we've already incorporated them
                    this.projBuffer.pendingContentChangedEventArgs.Clear();
                }
                else
                {
                    Debug.Assert(this.projBuffer.pendingContentChangedEventArgs.Count == 0);
                }
            }

            public bool CheckForCancellation(Action cancelAction)
            {
                return true;
            }

            public override string ToString()
            {
                StringBuilder insertions = new StringBuilder();
                for (int t = 0; t < this.spanManager.RawSpansToInsert.Count; ++t)
                {
                    ITrackingSpan insertion = this.spanManager.RawSpansToInsert[t] as ITrackingSpan;
                    if (insertion != null)
                    {
                        insertions.Append(insertion.ToString());
                    }
                    else
                    {
                        insertions.Append("(Literal)");
                    }
                    if (t < this.spanManager.RawSpansToInsert.Count - 1)
                    {
                        insertions.Append(",");
                    }
                }
                return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                     "pos: {0}, delete: {1}, insert: {2}",
                                     this.spanManager.Position, this.spanManager.SpansToDelete, insertions.ToString());
            }

            public void RecordMasterChangeOffset(int masterChangeOffset)
            {
                throw new InvalidOperationException("Projection span edits shouldn't have change offsets.");
            }
        }

        private ProjectionSourceSpansChangedEventArgs ApplySpanChanges(int position, int spansToDelete, IList<ITrackingSpan> spansToInsert, EditOptions options, object editTag)
        {
            ProjectionSnapshot beforeSnapshot = this.currentProjectionSnapshot;

            List<ITrackingSpan> deletedSpans = new List<ITrackingSpan>();

            List<SnapshotSpan> insertedSnapSpans = new List<SnapshotSpan>();
            List<SnapshotSpan> deletedSnapSpans = new List<SnapshotSpan>();

            this.sourceBufferSet.StartTransaction();

            for (int i = position + spansToDelete - 1; i >= position; --i)
            {
                ITrackingSpan removedSpan = RemoveSpan(i);
                deletedSpans.Insert(0, removedSpan);    // preserve order!
                deletedSnapSpans.Insert(0, this.currentProjectionSnapshot.GetSourceSpan(i));
            }

            int insertPosition = position;
            foreach (ITrackingSpan span in spansToInsert)
            {
                AddSpan(insertPosition++, span);
                insertedSnapSpans.Add(span.GetSpan(span.TextBuffer.CurrentSnapshot));
            }

            FrugalList<ITextBuffer> addedBuffers;
            FrugalList<ITextBuffer> removedBuffers;
            this.sourceBufferSet.FinishTransaction(out addedBuffers, out removedBuffers);

            // todo make this transactional in case it fails here, or else check thread affinity earlier
            // todo combine with later loop (can it move up here?)
            foreach (ITextBuffer addedBuffer in addedBuffers)
            {
                BaseBuffer baseAddedBuffer = (BaseBuffer)addedBuffer;
                this.group.Swallow(baseAddedBuffer.group);
            }

            int textPosition = 0;
            for (int i = 0; i < position; ++i)
            {
                textPosition += this.currentProjectionSnapshot.GetSourceSpan(i).Length;
            }

            INormalizedTextChangeCollection normalizedChanges;
            if (options.ComputeMinimalChange)
            {
                normalizedChanges = ComputeTextChangesByStringDiffing(options.DifferenceOptions, textPosition, deletedSnapSpans, insertedSnapSpans);
            }
            else
            {
                normalizedChanges = ComputeTextChangesBySpanDiffing(textPosition, deletedSnapSpans, insertedSnapSpans);
            }
            SetCurrentVersionAndSnapshot(normalizedChanges);

            ProjectionSourceSpansChangedEventArgs args = null;
            if (addedBuffers.Count > 0 || removedBuffers.Count > 0)
            {
                // Adjust buffer change listening
                foreach (ITextBuffer addedBuffer in addedBuffers)
                {
                    if (addedBuffer != this.literalBuffer || (this.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) != 0)
                    {
                        BaseBuffer baseAddedBuffer = (BaseBuffer)addedBuffer;
                        this.eventHooks.Add(new WeakEventHook(this, baseAddedBuffer));
                    }
                }
                foreach (ITextBuffer removedBuffer in removedBuffers)
                {
                    if (removedBuffer != this.literalBuffer || (this.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) != 0)
                    {
                        BaseBuffer baseRemovedBuffer = (BaseBuffer)removedBuffer;

                        for (int i = 0; (i < this.eventHooks.Count); ++i)
                        {
                            var hook = this.eventHooks[i];
                            if (hook.SourceBuffer == baseRemovedBuffer)
                            {
                                hook.UnsubscribeFromSourceBuffer();
                                this.eventHooks.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                args = new ProjectionSourceBuffersChangedEventArgs
                        (beforeSnapshot, this.currentProjectionSnapshot,
                         spansToInsert, deletedSpans, position, addedBuffers, removedBuffers, options, editTag);
            }
            else
            {
                args = new ProjectionSourceSpansChangedEventArgs
                        (beforeSnapshot, this.currentProjectionSnapshot,
                         spansToInsert, deletedSpans, position, options, editTag);
            }
            return args;
        }

        private INormalizedTextChangeCollection ComputeTextChangesByStringDiffing(StringDifferenceOptions differenceOptions, int textPosition, List<SnapshotSpan> deletedSnapSpans, List<SnapshotSpan> insertedSnapSpans)
        {
            StringBuilder oldText = new StringBuilder();
            foreach (SnapshotSpan deletedSnapSpan in deletedSnapSpans)
            {
                oldText.Append(deletedSnapSpan.GetText());
            }

            StringBuilder newText = new StringBuilder();
            foreach (SnapshotSpan insertedSnapSpan in insertedSnapSpans)
            {
                newText.Append(insertedSnapSpan.GetText());
            }

            List<TextChange> changes = new List<TextChange>();
            if (oldText.Length > 0 || newText.Length > 0)
            {
                changes.Add(new TextChange(textPosition, oldText.ToString(), newText.ToString(), this.currentProjectionSnapshot));
            }

            return NormalizedTextChangeCollection.Create(changes, differenceOptions, this.textDifferencingService);
        }

        private INormalizedTextChangeCollection ComputeTextChangesBySpanDiffing(int textPosition, List<SnapshotSpan> deletedSnapSpans, List<SnapshotSpan> insertedSnapSpans)
        {
            ProjectionSpanDiffer differ = new ProjectionSpanDiffer(this.differenceService, deletedSnapSpans.AsReadOnly(), insertedSnapSpans.AsReadOnly());
            return new ProjectionSpanToNormalizedChangeConverter(differ, textPosition, this.currentProjectionSnapshot).NormalizedChanges;
        }

        private void AddSpan(int position, ITrackingSpan sourceSpan)
        {
            this.sourceSpans.Insert(position, sourceSpan);
            this.sourceBufferSet.AddSpan(sourceSpan);
        }

        private ITrackingSpan RemoveSpan(int position)
        {
            ITrackingSpan result = this.sourceSpans[position];
            this.sourceSpans.RemoveAt(position);
            this.sourceBufferSet.RemoveSpan(result);
            return result;
        }

        public IProjectionSnapshot InsertSpan(int position, ITrackingSpan spanToInsert)
        {
            return ReplaceSpans(position, 0, new List<object>(1) { spanToInsert }, EditOptions.None, null);
        }

        public IProjectionSnapshot InsertSpan(int position, string literalSpanToInsert)
        {
            return ReplaceSpans(position, 0, new List<object>(1) { literalSpanToInsert }, EditOptions.None, null);
        }

        public IProjectionSnapshot InsertSpans(int position, IList<object> spansToInsert)
        {
            return ReplaceSpans(position, 0, spansToInsert, EditOptions.None, null);
        }

        public IProjectionSnapshot DeleteSpans(int position, int spansToDelete)
        {
            return ReplaceSpans(position, spansToDelete, new List<object>(0), EditOptions.None, null);
        }

        public IProjectionSnapshot ReplaceSpans(int position, int spansToReplace, IList<object> spansToInsert, EditOptions options, object editTag)
        {
            using (SpanEdit spedit = new SpanEdit(this))
            {
                return spedit.ReplaceSpans(position, spansToReplace, spansToInsert, options, editTag);
            }
        }
        #endregion

        #region Edit Creation
        public override ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            return new ProjectionEdit(this, this.currentProjectionSnapshot, options, reiteratedVersionNumber, editTag);
        }

        protected internal override ISubordinateTextEdit CreateSubordinateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            return new ProjectionEdit(this, this.currentProjectionSnapshot, options, reiteratedVersionNumber, editTag);
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Respond to a content change in a source buffer by mapping the change events to the coordinate system of
        /// this projection buffer.
        /// </summary>
        internal override void OnSourceTextChanged(object sender, TextContentChangedEventArgs e)
        {
            this.pendingContentChangedEventArgs.Add(e);

            if (!this.editApplicationInProgress)
            {
                // We had better be a member of the same group as the buffer that we just heard from
                Debug.Assert(this.group.MembersContains(e.After.TextBuffer));
                // Let the buffer group decide when to issue events; this allows us to collect changes from multiple
                // source buffers into a single snapshot, and (more important) to avoid inconsistencies
                this.group.ScheduleIndependentEdit(this);
            }
        }
        #endregion

        #region Source Change Interpretation

        public override BaseBuffer.ITextEventRaiser PropagateSourceChanges(EditOptions options, object editTag)
        {
            IList<ITextEventRaiser> eventRaisers = InterpretSourceChanges(options, /*this.reiteratedVersionNumber,*/ editTag);
            Debug.Assert(eventRaisers.Count == 1);
            eventRaisers[0].RaiseEvent(this, true);
            return eventRaisers[0];
        }

        /// <summary>
        /// TextChanges that are to be accomplished by a change to source spans (because of EdgeExclusive anomalies).
        /// </summary>
        private class SpanAdjustment
        {
            public TextChange LeadingChange;    // Text change induced by span growth/shrinkage on the leading edge
            public TextChange TrailingChange;   // Text change induced by span growth/shrinkage on the trailing edge
        }

        private IList<ITextEventRaiser> InterpretSourceChanges(EditOptions options, object editTag)
        {
            // now that all source edits have been applied, we can interpret the events they raised
            List<ITextEventRaiser> eventRaisers = new List<ITextEventRaiser>();

            SortedDictionary<int, SpanAdjustment> spanPreAdjustments = new SortedDictionary<int, SpanAdjustment>();
            SortedDictionary<int, SpanAdjustment> spanPostAdjustments = new SortedDictionary<int, SpanAdjustment>();

            INormalizedTextChangeCollection normalizedChanges = ComputeProjectedChanges(spanPreAdjustments, spanPostAdjustments);

            // Shrink source spans for each span pre-adjustment. Each one generates a new projection version/snapshot.
            if (BufferGroup.Tracing)
            {
                Debug.WriteLine("Span Preadjustments");
            }
            foreach (KeyValuePair<int, SpanAdjustment> positionAndAdjustment in spanPreAdjustments)
            {
                eventRaisers.Add(PerformSpanAdjustments(positionAndAdjustment.Key, positionAndAdjustment.Value, true, editTag));
            }

            if (BufferGroup.Tracing)
            {
                Debug.WriteLine("Main Snapshot");
            }
            //if (normalizedChanges.Count > 0)    // TODO: optimize this snapshot away if Count == 0
            {
                ITextSnapshot originSnapshot = this.currentProjectionSnapshot;
                base.SetCurrentVersionAndSnapshot(normalizedChanges);
                eventRaisers.Add(new TextContentChangedEventRaiser(originSnapshot, this.currentProjectionSnapshot, options, editTag));
            }

            // Generate additional transaction(s) that alter EdgeExclusive source spans as needed
            if (BufferGroup.Tracing)
            {
                Debug.WriteLine("Span Postadjustments");
            }
            foreach (KeyValuePair<int, SpanAdjustment> positionAndAdjustment in spanPostAdjustments)
            {
                eventRaisers.Add(PerformSpanAdjustments(positionAndAdjustment.Key, positionAndAdjustment.Value, false, editTag));
            }

            this.editApplicationInProgress = false;
            return eventRaisers;
        }

        private SourceSpansChangedEventRaiser PerformSpanAdjustments(int spanPosition, SpanAdjustment adjustment, bool beforeBaseSnapshot, object editTag)
        {
            IProjectionSnapshot originSnapshot = this.currentProjectionSnapshot;
            List<SnapshotSpan> newSourceSpans = new List<SnapshotSpan>(originSnapshot.GetSourceSpans());
            this.sourceBufferSet.StartTransaction();

            ITrackingSpan deletedSpan = RemoveSpan(spanPosition);
            SnapshotSpan sourceOriginSpan = originSnapshot.GetSourceSpans(spanPosition, 1)[0];
            int start = sourceOriginSpan.Start;
            int end = sourceOriginSpan.End;

            List<TextChange> changes = new List<TextChange>();
            if (adjustment.LeadingChange != null)
            {
                changes.Add(adjustment.LeadingChange);
                if (beforeBaseSnapshot)
                {
                    start += adjustment.LeadingChange.OldLength;
                }
                else
                {
                    start -= adjustment.LeadingChange.NewLength;
                }
            }
            if (adjustment.TrailingChange != null)
            {
                changes.Add(adjustment.TrailingChange);
                if (beforeBaseSnapshot)
                {
                    end -= adjustment.TrailingChange.OldLength;
                }
                else
                {
                    end += adjustment.TrailingChange.NewLength;
                }
            }
            ITrackingSpan insertedSpan = sourceOriginSpan.Snapshot.CreateTrackingSpan(Span.FromBounds(start, end), deletedSpan.TrackingMode);
            AddSpan(spanPosition, insertedSpan);
            newSourceSpans[spanPosition] = new SnapshotSpan(sourceOriginSpan.Snapshot, Span.FromBounds(start, end));

            FrugalList<ITextBuffer> addedBuffers;
            FrugalList<ITextBuffer> removedBuffers;
            this.sourceBufferSet.FinishTransaction(out addedBuffers, out removedBuffers);

            INormalizedTextChangeCollection normalizedChanges = NormalizedTextChangeCollection.Create(changes);
            this.currentVersion = this.currentVersion.CreateNext(normalizedChanges);
            if (beforeBaseSnapshot)
            {
                this.currentProjectionSnapshot = TakeStaticSnapshot(newSourceSpans);
            }
            else
            {
                this.ProtectedCurrentSnapshot = TakeSnapshot();
            }

            ProjectionSourceSpansChangedEventArgs args =
                new ProjectionSourceSpansChangedEventArgs(originSnapshot, this.currentProjectionSnapshot,
                                                          new ITrackingSpan[] { insertedSpan }, new ITrackingSpan[] { deletedSpan },
                                                          spanPosition, EditOptions.None, editTag);
            return new SourceSpansChangedEventRaiser(args);
        }

        private void DeleteFromSource(SnapshotSpan deletionSpan)
        {
            ITextBuffer sourceBuffer = deletionSpan.Snapshot.TextBuffer;
            ITextEdit xedit = this.group.GetEdit((BaseBuffer)sourceBuffer);
            xedit.Delete(deletionSpan);
        }

        private void ReplaceInSource(SnapshotSpan deletionSpan, string insertionText, int masterChangeOffset)
        {
            ITextBuffer sourceBuffer = deletionSpan.Snapshot.TextBuffer;
            ITextEdit xedit = this.group.GetEdit((BaseBuffer)sourceBuffer);
            xedit.Replace(deletionSpan, insertionText);

            ((ISubordinateTextEdit)xedit).RecordMasterChangeOffset(masterChangeOffset);
                // This above cast is safe because the buffer group stores edits of type ISubordinateTextEdit
                // and casts them to ITextEdit before returning them.
        }

        private void InsertInSource(SnapshotPoint sourceInsertionPoint, string text, int masterChangeOffset)
        {
            ITextBuffer sourceBuffer = sourceInsertionPoint.Snapshot.TextBuffer;
            ITextEdit xedit = this.group.GetEdit((BaseBuffer)sourceBuffer);
            xedit.Insert(sourceInsertionPoint.Position, text);

            ((ISubordinateTextEdit)xedit).RecordMasterChangeOffset(masterChangeOffset);
            // This above cast is safe because the buffer group stores edits of type ISubordinateTextEdit
            // and casts them to ITextEdit before returning them.
        }

        /// <summary>
        /// Generate the set of normalized text changes to the projection buffer that are induced by the current set of
        /// pending source changes.
        /// </summary>
        /// <param name="spanPreAdjustments">Adjustments to be manifested as shrinkage of EdgeExclusive source spans before adopting new source snapshot(s).</param>
        /// <param name="spanPostAdjustments">Adjustments to be manifested as growth of EdgeExclusive source spans adopting new source snapshot(s).</param>
        /// <returns></returns>
        private INormalizedTextChangeCollection ComputeProjectedChanges(SortedDictionary<int, SpanAdjustment> spanPreAdjustments,
                                                                        SortedDictionary<int, SpanAdjustment> spanPostAdjustments)
        {
            DumpPendingContentChangedEventArgs();
            List<Tuple<ITextBuffer, List<TextChange>>> pendingSourceChanges = PreparePendingChanges();
            List<TextChange> projectedChanges = new List<TextChange>();

            DumpPendingChanges(pendingSourceChanges);
            
            // these are the points in ordinary text buffers at which we have performed span adjustments
            // to account for edge-exclusive anomalies. We track them so that we never repeat the adjustment
            // for the same change.
            HashSet<SnapshotPoint> urPoints = new HashSet<SnapshotPoint>();

            // Process the pending changes in the reverse of the order in which we received them. This means
            // that the 'nearest' buffers are processed first.
            for (int p = pendingSourceChanges.Count - 1; p >= 0; --p)
            {
                var pair = pendingSourceChanges[p];
                List<TextChange> sourceChanges = pair.Item2;
                int accumulatedDelta = 0;
                for (int sc = 0; sc < sourceChanges.Count - 1; ++sc)    // skip the sentinel
                {
                    InterpretSourceBufferChange(pair.Item1, sourceChanges[sc], projectedChanges, urPoints, spanPreAdjustments, spanPostAdjustments, accumulatedDelta);
                    accumulatedDelta += sourceChanges[sc].Delta;
                }
            }

            if (this.editApplicationInProgress && (spanPreAdjustments.Count > 0 || spanPostAdjustments.Count > 0))
            {
                // We may be generating several distinct versions, so we have to do some position normalization.
                // This only occurs in edits originating on this buffer, not independent changes to source buffers.
                CorrectSpanAdjustments(projectedChanges, spanPreAdjustments, spanPostAdjustments);
            }

            return NormalizedTextChangeCollection.Create(projectedChanges);
        }

        private List<Tuple<ITextBuffer, List<TextChange>>> PreparePendingChanges()
        {
            // Changes to source buffers are interpreted against the state of the projection buffer
            // at the beginning of the transaction. However, events raised by source buffers have
            // been normalized so that coordinates of later versions are expressed in terms of the
            // immediately preceding version, not the version at the beginning of the transaction.
            // Therefore we denormalize the changes before considering them here. This has effect only
            // when more than one new version has been created for a particular source buffer.

            List<Tuple<ITextBuffer, List<TextChange>>> pendingSourceChanges = new List<Tuple<ITextBuffer, List<TextChange>>>();
            foreach (TextContentChangedEventArgs args in this.pendingContentChangedEventArgs)
            {
                ITextBuffer sourceBuffer = args.Before.TextBuffer;

                // Get the list of pending changes associated with the source buffer. In the case of an 
                // independent change to the source buffer, this list is always empty. For dependent changes,
                // the list can be nonempty if there are multiple routes to the source buffer.
                // In the usual case there will be only one change event for a source buffer so this
                // list will not be found.
                List<TextChange> bufferChanges;
                var pair = pendingSourceChanges.Find((p) => (p.Item1 == sourceBuffer));
                if (pair == null)
                {
                    bufferChanges = new List<TextChange>(1) { new TextChange(int.MaxValue, "", "", LineBreakBoundaryConditions.None) };
                    pendingSourceChanges.Add(new Tuple<ITextBuffer, List<TextChange>>(sourceBuffer, bufferChanges));
                }
                else
                {
                    bufferChanges = pair.Item2;
                }

                NormalizedTextChangeCollection.Denormalize(args.Changes, bufferChanges);
            }
            this.pendingContentChangedEventArgs.Clear();
            return pendingSourceChanges;
        }

        private static FrugalList<Tuple<TextChange, int>> Load(SortedDictionary<int, SpanAdjustment> adjustments)
        {
            FrugalList<Tuple<TextChange, int>> result = new FrugalList<Tuple<TextChange, int>>();

            foreach (SpanAdjustment adjustment in adjustments.Values)
            {
                int fudge = 0;
                if (adjustment.LeadingChange != null)
                {
                    result.Add(new Tuple<TextChange, int>(adjustment.LeadingChange, 0));
                    fudge = adjustment.LeadingChange.Delta;
                }
                if (adjustment.TrailingChange != null)
                {
                    result.Add(new Tuple<TextChange, int>(adjustment.TrailingChange, fudge));
                }
            }
            return result;
        }

        private static void CorrectSpanAdjustments(List<TextChange> ordinaryChanges,
                                                   SortedDictionary<int, SpanAdjustment> spanPreAdjustments,
                                                   SortedDictionary<int, SpanAdjustment> spanPostAdjustments)
        {
            TextChange[] sortedOrdinary = TextUtilities.StableSort(ordinaryChanges, (left, right) => (left.OldPosition - right.OldPosition));

            FrugalList<TextChange> ordinary = new FrugalList<TextChange>(sortedOrdinary);
            FrugalList<Tuple<TextChange, int>> preAdjustments = Load(spanPreAdjustments);
            FrugalList<Tuple<TextChange, int>> postAdjustments = Load(spanPostAdjustments);

            int count = ordinary.Count + preAdjustments.Count + postAdjustments.Count;

            TextChange sentinel = new TextChange(int.MaxValue, "", "", LineBreakBoundaryConditions.None);
            ordinary.Add(sentinel);
            preAdjustments.Add(new Tuple<TextChange, int>(sentinel,0));
            postAdjustments.Add(new Tuple<TextChange, int>(sentinel, 0));

            int ordDelta = 0;
            int preDelta = 0;
            int postDelta = 0;

            int ordIndex = 0;
            int preIndex = 0;
            int postIndex = 0;

            int ordPos = ordinary[0].OldPosition;
            int prePos = preAdjustments[0].Item1.OldPosition;
            int postPos = postAdjustments[0].Item1.OldPosition;

            int c = 0;
            while (c < count)
            {
                if (ordPos < prePos && ordPos < postPos)
                {
                    // ord is minimum
                    ordinary[ordIndex].OldPosition += preDelta;
                    ordinary[ordIndex].NewPosition = ordinary[ordIndex].OldPosition;
                    ordDelta += ordinary[ordIndex].Delta;
                    ordPos = ordinary[++ordIndex].OldPosition;
                    c++;
                }
                else if (prePos < ordPos && prePos < postPos)
                {
                    // pre is minimum
                    preAdjustments[preIndex].Item1.OldPosition += preDelta - preAdjustments[preIndex].Item2;
                    preAdjustments[preIndex].Item1.NewPosition = preAdjustments[preIndex].Item1.OldPosition;
                    preDelta += preAdjustments[preIndex].Item1.Delta;
                    prePos = preAdjustments[++preIndex].Item1.OldPosition;
                    c++;
                }
                else if (postPos < prePos && postPos < ordPos)
                {
                    // post is minimum
                    postAdjustments[postIndex].Item1.OldPosition += preDelta + ordDelta + postDelta - postAdjustments[postIndex].Item2;
                    postAdjustments[postIndex].Item1.NewPosition = postAdjustments[postIndex].Item1.OldPosition;
                    postDelta += postAdjustments[postIndex].Item1.Delta;
                    postPos = postAdjustments[++postIndex].Item1.OldPosition;
                    c++;
                }
                else if (prePos == ordPos && ordPos == postPos)
                {
                    // all three are equal
                    // apply pre and ord deltas; these don't affect each other

                    preAdjustments[preIndex].Item1.OldPosition += preDelta - preAdjustments[preIndex].Item2;
                    preAdjustments[preIndex].Item1.NewPosition = preAdjustments[preIndex].Item1.OldPosition;

                    ordinary[ordIndex].OldPosition += preDelta;
                    ordinary[ordIndex].NewPosition = ordinary[ordIndex].OldPosition;

                    postAdjustments[postIndex].Item1.OldPosition += preDelta + ordDelta + postDelta - postAdjustments[postIndex].Item2;
                    postAdjustments[postIndex].Item1.NewPosition = postAdjustments[postIndex].Item1.OldPosition;

                    preDelta += preAdjustments[preIndex].Item1.Delta;
                    prePos = preAdjustments[++preIndex].Item1.OldPosition;

                    ordDelta += ordinary[ordIndex].Delta;
                    ordPos = ordinary[++ordIndex].OldPosition;

                    postDelta += postAdjustments[postIndex].Item1.Delta;
                    postPos = postAdjustments[++postIndex].Item1.OldPosition;

                    c += 3;
                }
                else if (ordPos == prePos)
                {
                    preAdjustments[preIndex].Item1.OldPosition += preDelta - preAdjustments[preIndex].Item2;
                    preAdjustments[preIndex].Item1.NewPosition = preAdjustments[preIndex].Item1.OldPosition;

                    ordinary[ordIndex].OldPosition += preDelta;
                    ordinary[ordIndex].NewPosition = ordinary[ordIndex].OldPosition;

                    preDelta += preAdjustments[preIndex].Item1.Delta;
                    prePos = preAdjustments[++preIndex].Item1.OldPosition;

                    ordDelta += ordinary[ordIndex].Delta;
                    ordPos = ordinary[++ordIndex].OldPosition;

                    c += 2;
                }
                else if (prePos == postPos)
                {
                    preAdjustments[preIndex].Item1.OldPosition += preDelta - preAdjustments[preIndex].Item2;
                    preAdjustments[preIndex].Item1.NewPosition = preAdjustments[preIndex].Item1.OldPosition;

                    postAdjustments[postIndex].Item1.OldPosition += preDelta + ordDelta + postDelta - postAdjustments[postIndex].Item2;
                    postAdjustments[postIndex].Item1.NewPosition = postAdjustments[postIndex].Item1.OldPosition;

                    preDelta += preAdjustments[preIndex].Item1.Delta;
                    prePos = preAdjustments[++preIndex].Item1.OldPosition;

                    postDelta += postAdjustments[postIndex].Item1.Delta;
                    postPos = postAdjustments[++postIndex].Item1.OldPosition;

                    c += 2;
                }
                else
                {
                    Debug.Assert(ordPos == postPos);

                    ordinary[ordIndex].OldPosition += preDelta;
                    ordinary[ordIndex].NewPosition = ordinary[ordIndex].OldPosition;

                    postAdjustments[postIndex].Item1.OldPosition += preDelta + ordDelta + postDelta - postAdjustments[postIndex].Item2;
                    postAdjustments[postIndex].Item1.NewPosition = postAdjustments[postIndex].Item1.OldPosition;

                    ordDelta += ordinary[ordIndex].Delta;
                    ordPos = ordinary[++ordIndex].OldPosition;

                    postDelta += postAdjustments[postIndex].Item1.Delta;
                    postPos = postAdjustments[++postIndex].Item1.OldPosition;

                    c += 2;
                }
            }
            Debug.Assert(ordIndex == ordinary.Count - 1);
            Debug.Assert(preIndex == preAdjustments.Count - 1);
            Debug.Assert(postIndex == postAdjustments.Count - 1);
        }

        /// <summary>
        /// Figure out how a source buffer change affects the projection buffer.
        /// </summary>
        /// <param name="changedBuffer">The source buffer.</param>
        /// <param name="change">The TextChange received from the source buffer.</param>
        /// <param name="projectedChanges">List of changes to projection buffer to be augmented if <paramref name="change"/>
        /// applies to the source buffer and is not otherwised covered by a span adjustment.</param>
        /// <param name="urPoints"></param>
        /// <param name="spanPreAdjustments">Adjustments to be manifested as shrinkage of EdgeExclusive source spans before adopting new source snapshot(s).</param>
        /// <param name="spanPostAdjustments">Adjustments to be manifested as growth of EdgeExclusive source spans adopting new source snapshot(s).</param>
        private void InterpretSourceBufferChange(ITextBuffer changedBuffer,
                                                 ITextChange change,
                                                 List<TextChange> projectedChanges,
                                                 HashSet<SnapshotPoint> urPoints,
                                                 SortedDictionary<int, SpanAdjustment> spanPreAdjustments,
                                                 SortedDictionary<int, SpanAdjustment> spanPostAdjustments, 
                                                 int accumulatedDelta)
        {
            ProjectionSnapshot priorSnapshot = this.currentProjectionSnapshot;
            int sourceChangePosition = change.NewPosition;
            Span deletionSpan = new Span(sourceChangePosition, change.OldLength);
            int insertionCount = change.NewLength;
            int cumulativeLength = 0;

            int spanPosition = 0;

            ITextSnapshot afterSourceSnapshot = changedBuffer.CurrentSnapshot;   
            // todo: consider whether need to use a more precise snapshot. I don't think so, but give it more thought.
            // this is used only for mapping to urPoints.

            // This algorithm does a linear search of source spans in forward order.

            foreach (ITrackingSpan sourceSpan in this.sourceSpans)
            {
                SnapshotSpan priorRawSpan = priorSnapshot.GetSourceSpan(spanPosition);
                // Note: if we switch back to not generating a new snapshot of the projection buffer on every source
                // buffer change, then here we have to be careful to map the priorRawSpan to the current snapshot of the source buffer,
                // since it might be coming from an old snapshot (see e.g. Edit00 unit test)

                if (sourceSpan.TextBuffer == changedBuffer)
                {
                    SpanTrackingMode mode = sourceSpan.TrackingMode;
                    // is there an easy way to handle custom spans here?
                    Span? deletedHere = deletionSpan.Overlap(priorRawSpan);
                    // n.b.: Null span does not overlap with anything
                    if (deletedHere.HasValue && deletedHere.Value.Length > 0)
                    {
                        // part or all of the source span was deleted by the change

                        // compute the position at which the change takes place in the projection buffer 
                        // with respect to its current snapshot
                        int projectedPosition = cumulativeLength + deletedHere.Value.Start - priorRawSpan.Start;

                        Debug.Assert(projectedPosition >= 0 && projectedPosition <= priorSnapshot.Length);

                        string deletedText = change.OldText.Substring(Math.Max(priorRawSpan.Start.Position - deletionSpan.Start, 0), deletedHere.Value.Length);
                        string insertedText = string.Empty;  

                        SnapshotSpan adjustedPriorRawSpan = new SnapshotSpan(priorRawSpan.Snapshot, priorRawSpan.Start, priorRawSpan.Length - deletedText.Length);

                        if (sourceSpan.TrackingMode != SpanTrackingMode.EdgeInclusive && sourceSpan.TrackingMode != SpanTrackingMode.Custom && this.editInProgress)
                        {
                            // the tricky cases. If the deletion touches the edge of the source span, we first explicitly
                            // shrink the span to effect the deletion. If the change is later undone, the source span
                            // will be grown explicitly to encompass the restored text (otherwise we would lose it since
                            // the source span is EdgeExclusive and won't grow on its own).
                            if ((sourceSpan.TrackingMode != SpanTrackingMode.EdgeNegative) && (deletedHere.Value.Start == priorRawSpan.Start))
                            {
                                // A prefix of the source span (or the whole thing) is to be shrunk to effect the deletion.
                                SpanAdjustment adjust = GetAdjustment(spanPreAdjustments, spanPosition);
                                // Create the text change that will be induced by the span adjustment
                                Debug.Assert(adjust.LeadingChange == null); // there can only be one leading change for a particular span
                                adjust.LeadingChange = new TextChange(projectedPosition, deletedText, "", this.currentProjectionSnapshot);
                                Debug.Assert(adjust.LeadingChange.OldEnd <= priorSnapshot.Length);
                                deletedText = string.Empty;
                            }
                            else if ((sourceSpan.TrackingMode != SpanTrackingMode.EdgePositive) && (deletedHere.Value.End == priorRawSpan.End))
                            {
                                // A suffix of the source span is to be shrunk to effect the deletion.
                                SpanAdjustment adjust = GetAdjustment(spanPreAdjustments, spanPosition);
                                // Create the text change that will be induced by the span adjustment
                                Debug.Assert(adjust.TrailingChange == null);
                                adjust.TrailingChange = new TextChange(projectedPosition, deletedText, "", this.currentProjectionSnapshot);
                                Debug.Assert(adjust.TrailingChange.OldEnd <= priorSnapshot.Length);
                                deletedText = string.Empty;
                            }
                        }

                        if (change.NewLength > 0)                             // change includes an insertion
                        {
                            insertedText = InsertionLiesInSpan(afterSourceSnapshot, projectedPosition, spanPosition, adjustedPriorRawSpan, deletionSpan, 
                                                               sourceChangePosition, mode, change, urPoints, spanPostAdjustments, accumulatedDelta);
                            if (insertedText.Length > 0)
                            {
                                // replacement string is inserted here. There can be more than one insertion
                                // per change if custom tracking spans are involved.
                                insertionCount = change.NewLength - insertedText.Length;
                            }
                        }

                        if (deletedText.Length > 0 || insertedText.Length > 0)
                        {
                            TextChange interpretedChange = new TextChange(projectedPosition, deletedText, insertedText, this.currentProjectionSnapshot);
                            Debug.Assert(interpretedChange.OldEnd <= priorSnapshot.Length);
                            projectedChanges.Add(interpretedChange);
                        }
                    }
                    else if (insertionCount > 0)
                    {
                        int projectedPosition = cumulativeLength + Math.Max(sourceChangePosition - priorRawSpan.Start, 0);
                        // if the insertion is part of a replacement and the source span in question is edge inclusive, a sourceChangePosition to the
                        // left of the current source span may actually end up being interesting, in which case it would be at the beginning of the span.
                        // If those conditions don't obtain, InsertionLiesInSpan will return false and nobody will be the wiser.
                        int hack = spanPostAdjustments == null ? 0 : spanPostAdjustments.Count;
                        string insertedText = InsertionLiesInSpan(afterSourceSnapshot, projectedPosition, spanPosition, priorRawSpan, deletionSpan,
                                                                  sourceChangePosition, mode, change, urPoints, spanPostAdjustments, accumulatedDelta);
                        if (insertedText.Length > 0)
                        {
                            // a pure insertion into the source span

                            TextChange interpretedChange = new TextChange(projectedPosition, "", insertedText, this.currentProjectionSnapshot);
                            projectedChanges.Add(interpretedChange);
                        }
                        if (spanPostAdjustments != null && spanPostAdjustments.Count != hack)   // ur points should have eliminated the need for the hack
                        {
                            insertionCount = 0;
                        }
                    }
                }
                cumulativeLength += priorRawSpan.Length;
                spanPosition++;
            }
        }

        private string InsertionLiesInSpan(ITextSnapshot afterSourceSnapshot,
                                           int projectedPosition,
                                           int spanPosition, 
                                           SnapshotSpan rawSpan, 
                                           Span deletionSpan,
                                           int sourcePosition, 
                                           SpanTrackingMode mode, 
                                           ITextChange incomingChange, 
                                           HashSet<SnapshotPoint> urPoints,
                                           SortedDictionary<int, SpanAdjustment> spanAdjustments,
                                           int accumulatedDelta)
        {
            int renormalizedSourcePosition = sourcePosition + accumulatedDelta;
            if (mode == SpanTrackingMode.Custom)
            {
                return InsertionLiesInCustomSpan(afterSourceSnapshot, spanPosition, incomingChange, urPoints, accumulatedDelta);
            }

            bool contains = rawSpan.Contains(sourcePosition);
            if (mode == SpanTrackingMode.EdgeInclusive)
            {
                if ((this.bufferOptions & ProjectionBufferOptions.PermissiveEdgeInclusiveSourceSpans) != 0)
                {
                    return InsertionLiesInPermissiveInclusiveSpan
                                (afterSourceSnapshot, rawSpan, deletionSpan, sourcePosition, renormalizedSourcePosition, incomingChange, urPoints);
                }
                else
                {
                    return contains || sourcePosition == rawSpan.End ? incomingChange.NewText : string.Empty;
                }
            }
            else
            {
                if (!this.editInProgress)
                {
                    // Edit originated in the source buffer; we don't do any implicit growing
                    // of spans
                    bool included;
                    if (mode == SpanTrackingMode.EdgeNegative)
                    {
                        included = contains;
                    }
                    else if (mode == SpanTrackingMode.EdgePositive)
                    {
                        included = (sourcePosition != rawSpan.Start) && (contains || sourcePosition == rawSpan.End);
                    }
                    else
                    {
                        included = contains && sourcePosition != rawSpan.Start;
                    }
                    return included ? incomingChange.NewText : string.Empty;
                }
                else
                {
                    if (sourcePosition == rawSpan.Start && (mode != SpanTrackingMode.EdgeNegative))
                    {
                        SnapshotPoint? urPoint = MappingHelper.MapDownToFirstMatchNoTrack(new SnapshotPoint(afterSourceSnapshot, renormalizedSourcePosition),
                                                                                          (buffer) => (buffer is TextBuffer), 
                                                                                          PositionAffinity.Successor);
                        Debug.Assert(urPoint.HasValue);
                        if (urPoints.Add(urPoint.Value))
                        {
                            if (BufferGroup.Tracing)
                            {
                                Debug.WriteLine("UR-Point [exclusive:start]" + urPoint.Value.ToString());
                            }
                            // Insertion at exclusive left edge of source span: we need to grow the source span on the left
                            SpanAdjustment adjust = GetAdjustment(spanAdjustments, spanPosition);
                            // Create the text change that will be induced by the span adjustment
                            Debug.Assert(adjust.LeadingChange == null);
                            adjust.LeadingChange = new TextChange(projectedPosition, "", incomingChange.NewText, this.currentProjectionSnapshot);
                        }
                        return string.Empty;   // this insertion either already happened or happens on a subsequent transaction, not this one
                    }
                    else if (sourcePosition == rawSpan.End && (mode != SpanTrackingMode.EdgePositive))
                    {
                        SnapshotPoint? urPoint = MappingHelper.MapDownToFirstMatchNoTrack(new SnapshotPoint(afterSourceSnapshot, renormalizedSourcePosition),
                                                                                        (buffer) => (buffer is TextBuffer), 
                                                                                        PositionAffinity.Predecessor);
                        Debug.Assert(urPoint.HasValue);
                        if (urPoints.Add(urPoint.Value))
                        {
                            if (BufferGroup.Tracing)
                            {
                                Debug.WriteLine("UR-Point [exclusive:end]" + urPoint.Value.ToString());
                            }
                            // Insertion at exclusive right edge of source span: we need to grow the source span on the right
                            SpanAdjustment adjust = GetAdjustment(spanAdjustments, spanPosition);
                            // Create the text change that will be induced by the span adjustment
                            Debug.Assert(adjust.TrailingChange == null);
                            adjust.TrailingChange = new TextChange(projectedPosition, "", incomingChange.NewText, this.currentProjectionSnapshot);
                        }
                        return string.Empty;   // this insertion either already happened or happens on a subsequent transaction, not this one
                    }
                    return (contains || (mode == SpanTrackingMode.EdgePositive && sourcePosition == rawSpan.End)) ? incomingChange.NewText : string.Empty;
                }
            }
        }

        private string InsertionLiesInCustomSpan(ITextSnapshot afterSourceSnapshot, 
                                                 int spanPosition, 
                                                 ITextChange incomingChange,
                                                 HashSet<SnapshotPoint> urPoints, 
                                                 int accumulatedDelta)
        {
            // just evaluate the new span and see if it overlaps the insertion.
            ITrackingSpan sourceTrackingSpan = this.sourceSpans[spanPosition];
            SnapshotSpan afterSpan = sourceTrackingSpan.GetSpan(afterSourceSnapshot);

            Span newSpan = new Span(incomingChange.NewPosition + accumulatedDelta, incomingChange.NewLength);
            Span? over = newSpan.Overlap(afterSpan);
            return over.HasValue ? afterSourceSnapshot.GetText(over.Value) : string.Empty;

            
            //if (futureSpan.Contains(renormalizedSourcePosition))
            //{
            //    if (BufferGroup.Tracing)
            //    {
            //        Debug.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture,
            //                                      "Custom renormPosition {0} priorSpan [{1}..{2})", renormalizedSourcePosition, priorSpan.Start.Position + accumulatedDelta, priorSpan.End.Position + accumulatedDelta));
            //    }
            //    if (renormalizedSourcePosition == (priorSpan.Start.Position + accumulatedDelta) || renormalizedSourcePosition == (priorSpan.End.Position + accumulatedDelta))
            //    {
            //        SnapshotPoint? urPoint = MappingHelper.MapDownToFirstMatchNoTrack(new SnapshotPoint(afterSourceSnapshot, renormalizedSourcePosition),
            //                                                                          (buffer) => (buffer is TextBuffer),
            //                                                                          renormalizedSourcePosition == priorSpan.Start.Position + accumulatedDelta
            //                                                                          ? PositionAffinity.Successor
            //                                                                          : PositionAffinity.Predecessor);
            //        Debug.Assert(urPoint.HasValue);
            //        bool added = urPoints.Add(urPoint.Value);
            //        Debug.Assert(added);    // if this is false we are sorta hosed - we already handled this point
            //        if (BufferGroup.Tracing)
            //        {
            //            Debug.WriteLine("UR-Point [custom]" + urPoint.Value.ToString());
            //        }
            //    }
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
        }

        private static string InsertionLiesInPermissiveInclusiveSpan(ITextSnapshot afterSourceSnapshot,
                                                                     SnapshotSpan rawSpan,
                                                                     Span deletionSpan,
                                                                     int sourcePosition,
                                                                     int renormalizedSourcePosition,
                                                                     ITextChange incomingChange,
                                                                     HashSet<SnapshotPoint> urPoints)
        {
            bool leadingInclusiveGrowth = sourcePosition < rawSpan.Start && deletionSpan.End >= rawSpan.Start;
            if (sourcePosition == rawSpan.Start || leadingInclusiveGrowth)
            {
                SnapshotPoint? urPoint = MappingHelper.MapDownToFirstMatchNoTrack(new SnapshotPoint(afterSourceSnapshot, renormalizedSourcePosition),
                                                                                  (buffer) => (buffer is TextBuffer),
                                                                                  PositionAffinity.Successor);
                Debug.Assert(urPoint.HasValue);
                bool added = urPoints.Add(urPoint.Value);
                Debug.Assert(added);    // if this is false we are sorta hosed - we already handled this point
                if (BufferGroup.Tracing)
                {
                    Debug.WriteLine("UR-Point [inclusive:start]" + urPoint.Value.ToString());
                }
                return incomingChange.NewText;
            }
            else if (sourcePosition == rawSpan.End)
            {
                SnapshotPoint? urPoint = MappingHelper.MapDownToFirstMatchNoTrack(new SnapshotPoint(afterSourceSnapshot, renormalizedSourcePosition),
                                                                                  (buffer) => (buffer is TextBuffer),
                                                                                  PositionAffinity.Predecessor);
                Debug.Assert(urPoint.HasValue);
                bool added = urPoints.Add(urPoint.Value);
                Debug.Assert(added);    // if this is false we are sorta hosed - we already handled this point
                if (BufferGroup.Tracing)
                {
                    Debug.WriteLine("UR-Point [inclusive:end]" + urPoint.Value.ToString());
                }
                return incomingChange.NewText;
            }
            else
            {
                return rawSpan.Contains(sourcePosition) ? incomingChange.NewText : string.Empty;
            }
        }

        /// <summary>
        /// Fetch <see cref="SpanAdjustment"/> for this span if it already exists, or create a new one.
        /// </summary>
        private static SpanAdjustment GetAdjustment(SortedDictionary<int, SpanAdjustment> spanAdjustments, int spanPosition)
        {
            SpanAdjustment adjust;
            if (!spanAdjustments.TryGetValue(spanPosition, out adjust))
            {
                adjust = new SpanAdjustment();
                spanAdjustments.Add(spanPosition, adjust);
            }
            return adjust;
        }
        #endregion

        #region Change Application
        /// <summary>
        /// Given the set of changes to apply to this buffer, compute the set of changes to apply to its
        /// source buffers. These edit objects are managed by the buffer group, which will decide when to
        /// apply them.
        /// </summary>
        private void ComputeSourceEdits(FrugalList<TextChange> changes)
        {
            foreach (TextChange change in changes)
            {
                if (change.OldLength > 0 && change.NewLength == 0)
                {
                    // the change is a deletion
                    IList<SnapshotSpan> sourceDeletionSpans = this.currentProjectionSnapshot.MapToSourceSnapshots(new Span(change.NewPosition, change.OldLength));
                    foreach (SnapshotSpan sourceDeletionSpan in sourceDeletionSpans)
                    {
                        DeleteFromSource(sourceDeletionSpan);
                    }
                }

                else if (change.OldLength > 0 && change.NewLength > 0)
                {
                    // the change is a replacement
                    ReadOnlyCollection<SnapshotSpan> allSourceReplacementSpans =
                        this.currentProjectionSnapshot.MapReplacementSpanToSourceSnapshots
                            (new Span(change.OldPosition, change.OldLength), (this.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) == 0 ? this.literalBuffer : null);

                    //Filter out replacement spans that are read-only (since we couldn't edit them in any case).
                    FrugalList<SnapshotSpan> sourceReplacementSpans = new FrugalList<SnapshotSpan>();
                    foreach (var s in allSourceReplacementSpans)
                    {
                        if (!s.Snapshot.TextBuffer.IsReadOnly(s.Span, true))
                            sourceReplacementSpans.Add(s);
                    }

                    Debug.Assert(sourceReplacementSpans.Count > 0);  // if replacement is on read-only buffers, the read only check will have already caught it

                    if (sourceReplacementSpans.Count == 1)
                    {
                        ReplaceInSource(sourceReplacementSpans[0], change.NewText, 0 + change.MasterChangeOffset);
                    }
                    else
                    {
                        // the replacement hits the boundary of source spans
                        int[] insertionSizes = new int[sourceReplacementSpans.Count];

                        if (this.resolver != null)
                        {
                            SnapshotSpan projectionReplacementSpan = new SnapshotSpan(this.currentProjectionSnapshot, change.OldPosition, change.OldLength);
                            this.resolver.FillInReplacementSizes(projectionReplacementSpan, new ReadOnlyCollection<SnapshotSpan>(sourceReplacementSpans), change.NewText, insertionSizes);
                            if (BufferGroup.Tracing)
                            {
                                Debug.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                                              "## Seam Replacement @:{0}", projectionReplacementSpan));
                                for (int s = 0; s < sourceReplacementSpans.Count; ++s)
                                {
                                    Debug.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                                                  "##    {0,4}: {1}", insertionSizes[s], sourceReplacementSpans[s]));
                                }
                                Debug.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                                              "## Replacement Text:'{0}'", TextUtilities.Escape(change.NewText)));
                            }
                        }
                        insertionSizes[insertionSizes.Length - 1] = int.MaxValue;

                        int pos = 0;
                        for (int i = 0; i < insertionSizes.Length; ++i)
                        {
                            // contend with any old garbage that the client passed back.
                            int insertionSize = Math.Min(insertionSizes[i], change.NewLength - pos);
                            if (insertionSize > 0)
                            {
                                ReplaceInSource(sourceReplacementSpans[i], change.NewText.Substring(pos, insertionSize), pos + change.MasterChangeOffset);
                                pos += insertionSize;
                            }
                            else if (sourceReplacementSpans[i].Length > 0)
                            {
                                DeleteFromSource(sourceReplacementSpans[i]);
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert(change.OldLength == 0 && change.NewLength > 0);
                    // the change is an insertion
                    ReadOnlyCollection<SnapshotPoint> allSourceInsertionPoints =
                        this.currentProjectionSnapshot.MapInsertionPointToSourceSnapshots
                            (change.NewPosition, (this.bufferOptions & ProjectionBufferOptions.WritableLiteralSpans) == 0 ? this.literalBuffer : null);

                    Debug.Assert(allSourceInsertionPoints.Count > 0);  // if insertion point is between two literal spans, the read only check will have already caught it

                    //Filter out replacement spans that are read-only (since we couldn't edit them in any case).
                    FrugalList<SnapshotPoint> sourceInsertionPoints = new FrugalList<SnapshotPoint>();
                    foreach (var p in allSourceInsertionPoints)
                    {
                        if (!p.Snapshot.TextBuffer.IsReadOnly(p.Position, true))
                            sourceInsertionPoints.Add(p);
                    }

                    Debug.Assert(sourceInsertionPoints.Count > 0);  // if insertion point is between only read-only buffers, the read only check will have already caught it

                    if (sourceInsertionPoints.Count == 1)
                    {
                        // the insertion point is unambiguous
                        InsertInSource(sourceInsertionPoints[0], change.NewText, 0 + change.MasterChangeOffset);
                    }
                    else
                    {
                        // the insertion is at the boundary of source spans
                        int[] insertionSizes = new int[sourceInsertionPoints.Count];

                        if (this.resolver != null)
                        {
                            this.resolver.FillInInsertionSizes(new SnapshotPoint(this.currentProjectionSnapshot, change.NewPosition),
                                                               new ReadOnlyCollection<SnapshotPoint>(sourceInsertionPoints), change.NewText, insertionSizes);
                        }

                        // if resolver was not provided, we just use zeros for the insertion sizes, which will push the entire insertion 
                        // into the last slot.
                        insertionSizes[insertionSizes.Length - 1] = int.MaxValue;

                        int pos = 0;
                        for (int i = 0; i < insertionSizes.Length; ++i)
                        {
                            // contend with any old garbage that the client passed back.
                            int size = Math.Min(insertionSizes[i], change.NewLength - pos);
                            if (size > 0)
                            {
                                InsertInSource(sourceInsertionPoints[i], change.NewText.Substring(pos, size), pos + change.MasterChangeOffset);
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
            // defer interpretation of events that will be raised by source buffers as we make these edits
            this.editApplicationInProgress = true;
        }

        #endregion

        #region Snapshots
        protected override BaseSnapshot TakeSnapshot()
        {
            List<SnapshotSpan> newSourceSpans = new List<SnapshotSpan>(this.sourceSpans.Count);
            foreach (ITrackingSpan sourceSpan in this.sourceSpans)
            {
                // since we are on the main thread, we can safely just look at current snapshots
                newSourceSpans.Add(sourceSpan.GetSpan(sourceSpan.TextBuffer.CurrentSnapshot));
            }
            this.currentProjectionSnapshot = MakeSnapshot(newSourceSpans);
            return this.currentProjectionSnapshot;
        }

        private ProjectionSnapshot TakeStaticSnapshot(List<SnapshotSpan> newSourceSpans)
        {
            // this form of snapshot uses the same source snapshots as current snapshot rather than current snapshots
            return MakeSnapshot(newSourceSpans);
        }

        private ProjectionSnapshot MakeSnapshot(List<SnapshotSpan> newSourceSpans)
        {
            ITextBuffer doppelBottom;
            if (Properties.TryGetProperty<ITextBuffer>("IdentityMapping", out doppelBottom))
            {
                return new ProjectionSnapshotDoppelganger(this, this.currentVersion, newSourceSpans, doppelBottom.CurrentSnapshot);
            }
            else
            {
                return new ProjectionSnapshot(this, this.currentVersion, newSourceSpans);
            }
        }


        public override IProjectionSnapshot CurrentSnapshot
        {
            get { return this.currentProjectionSnapshot; }
        }

        protected override BaseProjectionSnapshot CurrentBaseSnapshot
        {
            get { return this.currentProjectionSnapshot; }
        }
        #endregion

        #region Events
        internal event EventHandler<ProjectionSourceBuffersChangedEventArgs> SourceBuffersChangedImmediate;
        internal event EventHandler<ProjectionSourceSpansChangedEventArgs> SourceSpansChangedImmediate;

        public event EventHandler<ProjectionSourceBuffersChangedEventArgs> SourceBuffersChanged;
        public event EventHandler<ProjectionSourceSpansChangedEventArgs> SourceSpansChanged;
        #endregion
    }
}
