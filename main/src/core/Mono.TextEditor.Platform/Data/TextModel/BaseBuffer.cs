namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Utilities;

    internal abstract partial class BaseBuffer : ITextBuffer
    {
        #region ITextEventRaiser Interface
        /// <summary>
        /// Implemented internally to support a heterogeneous event queue.
        /// </summary>
        internal interface ITextEventRaiser
        {
            void RaiseEvent(BaseBuffer baseBuffer, bool immediate);
            bool HasPostEvent { get; }
        }
        #endregion

        #region TextContentChangedEventRaiser Class
        /// <summary>
        /// The agent that knows how to raise ordinary TextContent changed events.
        /// </summary>
        internal class TextContentChangedEventRaiser : ITextEventRaiser
        {
            private TextContentChangedEventArgs args;

            public TextContentChangedEventRaiser(ITextSnapshot beforeSnapshot,
                                                 ITextSnapshot afterSnapshot,
                                                 EditOptions options,
                                                 Object editTag)
            {
                args = new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, options, editTag);
            }

            public void RaiseEvent(BaseBuffer baseBuffer, bool immediate)
            {
                baseBuffer.RawRaiseEvent(args, immediate);
            }

            public bool HasPostEvent
            {
                get { return true; }
            }
        }
        #endregion

        #region TextBufferBaseEdit Class Definition
        /// <summary>
        /// Checking for edits already in progress and modifications on the proper thread.
        /// </summary>
        protected abstract class TextBufferBaseEdit : IDisposable
        {
            protected BaseBuffer baseBuffer;
            protected bool applied;
            protected bool canceled;

            public TextBufferBaseEdit(BaseBuffer baseBuffer)
            {
                this.baseBuffer = baseBuffer;
                if (!baseBuffer.CheckEditAccess())
                {
                    throw new InvalidOperationException(Strings.InvalidTextBufferEditThread);
                }
                if (baseBuffer.editInProgress)
                {
                    throw new InvalidOperationException(Strings.SimultaneousEdit);
                }
                baseBuffer.editInProgress = true;
                baseBuffer.group.BeginEdit();
            }

            public virtual void Cancel()
            {
                this.CancelApplication();
            }

            public virtual void CancelApplication()
            {
                if (!this.canceled)
                {
                    this.canceled = true;
                    this.baseBuffer.editInProgress = false;
                    this.baseBuffer.group.CancelEdit();
                }
            }

            public bool Canceled
            {
                get
                {
                    return this.canceled;
                }
            }

            public void Dispose()
            {
                if (!this.applied && !this.canceled)
                {
                    this.CancelApplication();
                }
                GC.SuppressFinalize(this);
            }
        }
        #endregion

        #region TextBufferEdit Class Definition
        /// <summary>
        /// Edit protocol checking.
        /// </summary>
        protected abstract partial class TextBufferEdit : TextBufferBaseEdit
        {
            protected ITextSnapshot originSnapshot;
            protected object editTag;

            public TextBufferEdit(BaseBuffer baseBuffer, ITextSnapshot snapshot, object editTag)
                : base(baseBuffer)
            {
                this.baseBuffer = baseBuffer;
                this.originSnapshot = snapshot;
                this.editTag = editTag;
            }

            public ITextSnapshot Snapshot
            {
                get { return this.originSnapshot; }
            }

            public ITextSnapshot Apply()
            {
                ITextSnapshot snapshot;
                try
                {
                    snapshot = PerformApply();
                }
                finally
                {
                    // TextBufferBaseEdit.Cancel may have been called via
                    // the cancellable Changing event. In that case, group.CancelEdit will
                    // have been called and canceled will be true.
                    if (!this.canceled)
                    {
                        this.baseBuffer.group.FinishEdit();
                    }
                }

                return snapshot;
            }

            protected abstract ITextSnapshot PerformApply();

            protected void CheckActive()
            {
                if (this.canceled)
                {
                    throw new InvalidOperationException(Strings.ContinueCanceledEdit);
                }
                if (this.applied)
                {
                    throw new InvalidOperationException(Strings.ReuseAppliedEdit);
                }
            }
        }
        #endregion

        #region Edit Class Definition
        /// <summary>
        /// Fundamental editing operations.
        /// </summary>
        protected abstract partial class Edit : TextBufferEdit, ITextEdit
        {
            private readonly int bufferLength;
            protected FrugalList<TextChange> changes;
            protected readonly EditOptions options;
            protected readonly int? reiteratedVersionNumber;
            private TextContentChangingEventArgs raisedChangingEventArgs;
            private Action cancelAction;
            private bool hasFailedChanges;

            protected Edit(BaseBuffer baseBuffer, ITextSnapshot originSnapshot, EditOptions options, int? reiteratedVersionNumber, Object editTag)
                : base(baseBuffer, originSnapshot, editTag)
            {
                this.bufferLength = originSnapshot.Length;
                this.changes = new FrugalList<TextChange>();
                this.options = options;
                this.reiteratedVersionNumber = reiteratedVersionNumber;
                this.raisedChangingEventArgs = null;
                this.cancelAction = null;
                this.hasFailedChanges = false;
            }

            public bool Insert(int position, string text)
            {
                CheckActive();
                if (position < 0 || position > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("position");
                }
                if (text == null)
                {
                    throw new ArgumentNullException("text");
                }

                // Check for ReadOnly
                if (this.baseBuffer.IsReadOnlyImplementation(position, isEdit: true))
                {
                    this.hasFailedChanges = true;
                    return false;
                }

                if (text.Length != 0)
                {
                    this.changes.Add(new TextChange(position, "", text, this.originSnapshot));
                }
                return true;
            }

            public bool Insert(int position, char[] characterBuffer, int startIndex, int length)
            {
                CheckActive();
                if (position < 0 || position > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("position");
                }
                if (characterBuffer == null)
                {
                    throw new ArgumentNullException("characterBuffer");
                }
                if (startIndex < 0 || startIndex > characterBuffer.Length)
                {
                    throw new ArgumentOutOfRangeException("startIndex");
                }
                if (length < 0 || startIndex + length > characterBuffer.Length)
                {
                    throw new ArgumentOutOfRangeException("length");
                }

                // Check for ReadOnly
                if (this.baseBuffer.IsReadOnlyImplementation(position, isEdit: true))
                {
                    this.hasFailedChanges = true;
                    return false;
                }

                if (length != 0)
                {
                    this.changes.Add(new TextChange(position, "", new string(characterBuffer, startIndex, length), this.originSnapshot));
                }
                return true;
            }

            public bool Replace(int startPosition, int charsToReplace, string replaceWith)
            {
                CheckActive();
                if (startPosition < 0 || startPosition > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("startPosition");
                }
                if (charsToReplace < 0 || startPosition + charsToReplace > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("charsToReplace");
                }
                if (replaceWith == null)
                {
                    throw new ArgumentNullException("replaceWith");
                }

                // Check for ReadOnly
                if (this.baseBuffer.IsReadOnlyImplementation(new Span(startPosition, charsToReplace), isEdit: true))
                {
                    this.hasFailedChanges = true;
                    return false;
                }

                if (charsToReplace != 0 || replaceWith.Length != 0)
                {
                    this.changes.Add(new TextChange(startPosition, DeletionChangeString(new Span(startPosition, charsToReplace)), new LiteralChangeString(replaceWith), this.originSnapshot));
                }
                return true;
            }

            public bool Replace(Span replaceSpan, string replaceWith)
            {
                CheckActive();
                if (replaceSpan.End > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("replaceSpan");
                }
                if (replaceWith == null)
                {
                    throw new ArgumentNullException("replaceWith");
                }

                // Check for ReadOnly
                if (this.baseBuffer.IsReadOnlyImplementation(replaceSpan, isEdit: true))
                {
                    this.hasFailedChanges = true;
                    return false;
                }

                if (replaceSpan.Length != 0 || replaceWith.Length != 0)
                {
                    this.changes.Add(new TextChange(replaceSpan.Start, DeletionChangeString(replaceSpan), new LiteralChangeString(replaceWith), this.originSnapshot));
                }
                return true;
            }

            public bool Delete(int startPosition, int charsToDelete)
            {
                CheckActive();
                if (startPosition < 0 || startPosition > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("startPosition");
                }
                if (charsToDelete < 0 || startPosition + charsToDelete > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("charsToDelete");
                }

                // Check for ReadOnly
                if (this.baseBuffer.IsReadOnlyImplementation(new Span(startPosition, charsToDelete), isEdit: true))
                {
                    this.hasFailedChanges = true;
                    return false;
                }

                if (charsToDelete != 0)
                {
                    this.changes.Add(new TextChange(startPosition, DeletionChangeString(new Span(startPosition, charsToDelete)), ChangeString.EmptyChangeString, this.originSnapshot));
                }
                return true;
            }

            public bool Delete(Span deleteSpan)
            {
                CheckActive();
                if (deleteSpan.End > this.bufferLength)
                {
                    throw new ArgumentOutOfRangeException("deleteSpan");
                }

                // Check for ReadOnly
                if (this.baseBuffer.IsReadOnlyImplementation(deleteSpan, isEdit: true))
                {
                    this.hasFailedChanges = true;
                    return false;
                }

                if (deleteSpan.Length != 0)
                {
                    this.changes.Add(new TextChange(deleteSpan.Start, DeletionChangeString(deleteSpan), ChangeString.EmptyChangeString, this.originSnapshot));
                }
                return true;
            }

            private ChangeString DeletionChangeString(Span deleteSpan)
            {
                return ReferenceChangeString.CreateChangeString(this.originSnapshot, deleteSpan);
            }

            /// <summary>
            /// Checks whether the edit on the buffer is allowed to continue.
            /// </summary>
            /// <param name="cancelationResponse">Additional action to perform if the edit itself is canceled.</param>
            public bool CheckForCancellation(Action cancelationResponse)
            {
                Debug.Assert(this.raisedChangingEventArgs == null, "just checking");

                // If no changes are being applied to this edit's buffer then there will be no new snapshot produced and
                // the Changed event won't be raised and so the cancelable Changing event should not be raised either.
                if (this.changes.Count == 0)
                {
                    return true;
                }

                if (this.raisedChangingEventArgs == null)
                {
                    this.cancelAction = cancelationResponse;
                    this.raisedChangingEventArgs = new TextContentChangingEventArgs(this.Snapshot, this.editTag, (args) =>
                    {
                        this.Cancel();
                    });
                    this.baseBuffer.RaiseChangingEvent(this.raisedChangingEventArgs);
                }
                this.canceled = this.raisedChangingEventArgs.Canceled;
                //Debug.Assert(!this.canceled || !this.applied, "an edit shouldn't be both canceled and applied");
                return !this.raisedChangingEventArgs.Canceled;
            }

            public override void Cancel()
            {
                base.Cancel();

                if (this.cancelAction != null)
                {
                    this.cancelAction();
                }
            }

            public bool HasEffectiveChanges
            {
                get
                {
                    return this.changes.Count > 0;
                }
            }

            public bool HasFailedChanges
            {
                get
                {
                    return this.hasFailedChanges;
                }
            }

            public override string ToString()
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                for (int c = 0; c < this.changes.Count; ++c)
                {
                    TextChange change = this.changes[c];
                    builder.Append(change.ToString(brief: true));
                    if (c < this.changes.Count - 1)
                    {
                        builder.Append("\r\n");
                    }
                }
                return builder.ToString();
            }

            public void RecordMasterChangeOffset(int masterChangeOffset)
            {
                if (this.changes.Count == 0)
                {
                    throw new InvalidOperationException("Can't record a change offset without a change.");
                }

                this.changes[this.changes.Count - 1].RecordMasterChangeOffset(masterChangeOffset);
            }
        }
        #endregion // Edit Class Definition

        #region Read Only Region Edit Class Definition
        private sealed partial class ReadOnlyRegionEdit : TextBufferEdit, IReadOnlyRegionEdit
        {
            private List<IReadOnlyRegion> readOnlyRegionsToAdd = new List<IReadOnlyRegion>();
            private List<IReadOnlyRegion> readOnlyRegionsToRemove = new List<IReadOnlyRegion>();

            private int aggregateEnd = int.MinValue;
            private int aggregateStart = int.MaxValue;

            public ReadOnlyRegionEdit(BaseBuffer baseBuffer, ITextSnapshot originSnapshot, Object editTag)
                : base(baseBuffer, originSnapshot, editTag)
            {
            }

            protected override ITextSnapshot PerformApply()
            {
                CheckActive();

                this.applied = true;

                if ((this.readOnlyRegionsToAdd.Count > 0) || (this.readOnlyRegionsToRemove.Count > 0))
                {
                    if (this.readOnlyRegionsToAdd.Count > 0)
                    {
                        // We leave the read only regions collection on the buffer null
                        // since most buffers will never have a read only region. Since
                        // regions are being added, create it now.
                        if (this.baseBuffer.readOnlyRegions == null)
                        {
                            this.baseBuffer.readOnlyRegions = new FrugalList<IReadOnlyRegion>();
                        }

                        this.baseBuffer.readOnlyRegions.AddRange(this.readOnlyRegionsToAdd);
                    }

                    if (this.readOnlyRegionsToRemove.Count > 0)
                    {
                        // We've already verified that it makes sense to remove these read only
                        // regions, so just proceed without further checks.
                        foreach (IReadOnlyRegion readOnlyRegion in this.readOnlyRegionsToRemove)
                        {
                            this.baseBuffer.readOnlyRegions.Remove(readOnlyRegion);
                        }
                    }

                    // Save off the current state of the read only spans
                    this.baseBuffer.readOnlyRegionSpanCollection = new ReadOnlySpanCollection(this.baseBuffer.CurrentVersion, this.baseBuffer.readOnlyRegions);

                    ReadOnlyRegionsChangedEventRaiser raiser = 
                        new ReadOnlyRegionsChangedEventRaiser(new SnapshotSpan(this.baseBuffer.CurrentSnapshot, this.aggregateStart, this.aggregateEnd - this.aggregateStart));
                    this.baseBuffer.group.EnqueueEvents(raiser, this.baseBuffer);
                    // no immediate event for read only regions
                    this.baseBuffer.editInProgress = false;
                }
                else
                {
                    this.baseBuffer.editInProgress = false;
                }

                // no new snapshot
                return this.originSnapshot;
            }

            #region IReadOnlyRegionEdit Members

            public IReadOnlyRegion CreateReadOnlyRegion(Span span, SpanTrackingMode trackingMode, EdgeInsertionMode edgeInsertionMode)
            {
                return CreateDynamicReadOnlyRegion(span, trackingMode, edgeInsertionMode, callback: null);
            }

            public IReadOnlyRegion CreateDynamicReadOnlyRegion(Span span, SpanTrackingMode trackingMode, EdgeInsertionMode edgeInsertionMode, DynamicReadOnlyRegionQuery callback)
            {
                ReadOnlyRegion readOnlyRegion = new ReadOnlyRegion(this.baseBuffer.CurrentVersion, span, trackingMode, edgeInsertionMode, callback);

                readOnlyRegionsToAdd.Add(readOnlyRegion);

                this.aggregateStart = Math.Min(this.aggregateStart, span.Start);
                this.aggregateEnd = Math.Max(this.aggregateEnd, span.End);

                return readOnlyRegion;
            }

            public IReadOnlyRegion CreateReadOnlyRegion(Span span)
            {
                return CreateReadOnlyRegion(span, SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Allow);
            }

            public void RemoveReadOnlyRegion(IReadOnlyRegion readOnlyRegion)
            {
                // Throw if trying to remove a region if there aren't that many regions created.
                if (this.baseBuffer.readOnlyRegions == null)
                {
                    throw new InvalidOperationException(Strings.RemoveNoReadOnlyRegion);
                }

                // Throw if trying to remove a region from the wrong buffer
                if (this.readOnlyRegionsToRemove.Exists(delegate(IReadOnlyRegion match) { return !object.ReferenceEquals(match.Span.TextBuffer, this.baseBuffer); }))
                {
                    throw new InvalidOperationException(Strings.InvalidReadOnlyRegion);
                }

                this.readOnlyRegionsToRemove.Add(readOnlyRegion);

                Span regionSpan = readOnlyRegion.Span.GetSpan(this.baseBuffer.CurrentSnapshot);
                this.aggregateStart = Math.Min(this.aggregateStart, regionSpan.Start);
                this.aggregateEnd = Math.Max(this.aggregateEnd, regionSpan.End);
            }

            #endregion
        }
        #endregion // Read Only Region Edit Class Definition

        #region ContentType Edit Class Definition
        private sealed class ContentTypeEdit : TextBufferEdit, ISubordinateTextEdit
        {
            private IContentType _newContentType;

            public ContentTypeEdit(BaseBuffer baseBuffer, ITextSnapshot originSnapshot, Object editTag, IContentType newContentType)
                : base(baseBuffer, originSnapshot, editTag)
            {
                _newContentType = newContentType;
            }

            public ITextBuffer TextBuffer
            {
                get { return this.baseBuffer; }
            }

            protected override ITextSnapshot PerformApply()
            {
                CheckActive();

                this.applied = true;

                if (_newContentType != null)
                {
                    // we need to perform a group edit because any projection buffers that use this buffer will
                    // generate new snapshots as independent edits.
                    this.baseBuffer.group.PerformMasterEdit(this.baseBuffer, this, EditOptions.None, this.editTag);
                }
                else
                {
                    this.baseBuffer.editInProgress = false;
                }

                return this.baseBuffer.currentSnapshot;
            }

            public void PreApply()
            {
                // all the action is in FinalApply()
            }

            public void FinalApply()
            {
                IContentType beforeContentType = baseBuffer.contentType;
                this.baseBuffer.contentType = _newContentType;
                this.baseBuffer.SetCurrentVersionAndSnapshot(NormalizedTextChangeCollection.Create(new FrugalList<TextChange>()));
                ITextEventRaiser raiser = new ContentTypeChangedEventRaiser(this.originSnapshot, baseBuffer.currentSnapshot, beforeContentType, baseBuffer.contentType, editTag);
                this.baseBuffer.group.EnqueueEvents(raiser, this.baseBuffer);
                raiser.RaiseEvent(this.baseBuffer, true);
                this.baseBuffer.editInProgress = false;
            }

            public bool CheckForCancellation(Action cancelAction)
            {
                // Not cancelable.
                return true;
            }

            public void RecordMasterChangeOffset(int masterChangeOffset)
            {
                throw new InvalidOperationException("Content type edits shouldn't have change offsets.");
            }
        }
        #endregion // IContentType Edit Class Definition

        #region Private members and construction
        private IContentType contentType;
        private PropertyCollection properties;
        private readonly Object syncLock = new Object();
        private Thread editThread;
        protected internal BufferGroup group;

        protected internal BaseSnapshot currentSnapshot;
        protected TextVersion currentVersion;
        private FrugalList<IReadOnlyRegion> readOnlyRegions;
        protected ReadOnlySpanCollection readOnlyRegionSpanCollection;
        protected internal bool editInProgress;
        protected internal ITextDifferencingService textDifferencingService;
        protected readonly GuardedOperations guardedOperations;

        private static bool eventTracing = false;
        private static int eventDepth = 0;

        protected BaseBuffer(IContentType contentType, int initialLength, ITextDifferencingService textDifferencingService, GuardedOperations guardedOperations)
        {
            // parameters are validated outside
            Debug.Assert(contentType != null);

            this.contentType = contentType;
            this.currentVersion = new TextVersion(this, 0, 0, initialLength);
            this.textDifferencingService = textDifferencingService;
            this.guardedOperations = guardedOperations;
        }
        #endregion

        #region ITextBuffer members
        public IContentType ContentType
        {
            get { return this.contentType; }
        }

        public PropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    lock (this.syncLock)
                    {
                        if (this.properties == null)
                        {
                            this.properties = new PropertyCollection();
                        }
                    }
                }
                return this.properties;
            }
        }

        public ITextSnapshot CurrentSnapshot
        {
            get { return this.currentSnapshot; }
        }

        protected BaseSnapshot ProtectedCurrentSnapshot
        {
            set { this.currentSnapshot = value; }
        }

        protected TextVersion CurrentVersion
        {
            get { return this.currentVersion; }
        }

        public abstract ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag);

        public ITextEdit CreateEdit()
        {
            return CreateEdit(EditOptions.None, null, null);
        }

        public IReadOnlyRegionEdit CreateReadOnlyRegionEdit()
        {
            return CreateReadOnlyRegionEdit(null);
        }

        public IReadOnlyRegionEdit CreateReadOnlyRegionEdit(object editTag)
        {
            return new ReadOnlyRegionEdit(this, this.CurrentSnapshot, editTag);
        }

        public void ChangeContentType(IContentType newContentType, object editTag)
        {
            if (newContentType == null)
            {
                throw new ArgumentNullException("newContentType");
            }

            if (newContentType != this.contentType)
            {
                using (ContentTypeEdit edit = new ContentTypeEdit(this, this.currentSnapshot, editTag, newContentType))
                {
                    edit.Apply();
                }
            }
        }

        public bool EditInProgress
        {
            get { return this.editInProgress; }
        }

        public void TakeThreadOwnership()
        {
            lock (this.syncLock)
            {
                if (this.editThread != null && this.editThread != Thread.CurrentThread)
                {
                    throw new InvalidOperationException(Strings.InvalidBufferThreadOwnershipChange);
                }
                this.editThread = Thread.CurrentThread;
            }
        }

        public bool CheckEditAccess()
        {
            return this.editThread == null || this.editThread == Thread.CurrentThread;
        }

        protected abstract BaseSnapshot TakeSnapshot();
        #endregion

        #region ReadOnlyRegion support
        public bool IsReadOnly(int position)
        {
            return IsReadOnly(position, isEdit: false);
        }

        public bool IsReadOnly(int position, bool isEdit)
        {
            ReadOnlyQueryThreadCheck();
            if ((position < 0) || (position > this.currentSnapshot.Length))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            return IsReadOnlyImplementation(position, isEdit);
        }

        public bool IsReadOnly(Span span)
        {
            return IsReadOnly(span, isEdit: false);
        }

        public bool IsReadOnly(Span span, bool isEdit)
        {
            ReadOnlyQueryThreadCheck();
            if (span.End > this.currentSnapshot.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }

            return IsReadOnlyImplementation(span, isEdit);
        }

        protected internal virtual bool IsReadOnlyImplementation(int position, bool isEdit)
        {
            if (this.readOnlyRegionSpanCollection == null)
            {
                return false;
            }
            return this.readOnlyRegionSpanCollection.IsReadOnly(position, this.currentSnapshot, isEdit);
        }

        protected internal virtual bool IsReadOnlyImplementation(Span span, bool isEdit)
        {
            if (this.readOnlyRegionSpanCollection == null)
            {
                return false;
            }
            return this.readOnlyRegionSpanCollection.IsReadOnly(span, this.currentSnapshot, isEdit);
        }

        public NormalizedSpanCollection GetReadOnlyExtents(Span span)
        {
            ReadOnlyQueryThreadCheck();
            if (span.End > this.CurrentSnapshot.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }
            return GetReadOnlyExtentsImplementation(span);
        }

        protected internal virtual NormalizedSpanCollection GetReadOnlyExtentsImplementation(Span span)
        {
            FrugalList<Span> spans = new FrugalList<Span>();

            if (this.readOnlyRegionSpanCollection != null)
            {
                foreach (ReadOnlySpan readOnlySpan in this.readOnlyRegionSpanCollection.QueryAllEffectiveReadOnlySpans(this.currentVersion))
                {
                    Span readOnlySpanSpan = readOnlySpan.GetSpan(this.currentSnapshot);
                    Span? overlapSpan = (readOnlySpanSpan == span) ? readOnlySpanSpan : readOnlySpanSpan.Overlap(span);
                    if (overlapSpan.HasValue)
                    {
                        spans.Add(overlapSpan.Value);
                    }
                }
            }

            return new NormalizedSpanCollection(spans);
        }

        private void ReadOnlyQueryThreadCheck()
        {
            if (!CheckEditAccess())
            {
                throw new InvalidOperationException(Strings.InvalidTextBufferEditThread);
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged;

        protected class ReadOnlyRegionsChangedEventRaiser : ITextEventRaiser
        {
            private SnapshotSpan affectedSpan;

            public ReadOnlyRegionsChangedEventRaiser(SnapshotSpan affectedSpan)
            {
                this.affectedSpan = affectedSpan;
            }

            public void RaiseEvent(BaseBuffer baseBuffer, bool immediate)
            {
                // there is no immediate form of this event since it does not create a snapshot
                Debug.Assert(!immediate);
                EventHandler<SnapshotSpanEventArgs> handler = baseBuffer.ReadOnlyRegionsChanged;
                if (handler != null)
                {
                    var args = new SnapshotSpanEventArgs(affectedSpan);
                    baseBuffer.guardedOperations.RaiseEvent(baseBuffer, handler, args);
                }
            }

            public bool HasPostEvent
            {
                get { return false; }
            }
        }
        #endregion

        #region IContentType change support
        public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

        protected class ContentTypeChangedEventRaiser : ITextEventRaiser
        {
            #region Private Members
            ITextSnapshot beforeSnapshot;
            ITextSnapshot afterSnapshot;
            object editTag;
            IContentType beforeContentType;
            IContentType afterContentType;
            #endregion

            public ContentTypeChangedEventRaiser(ITextSnapshot beforeSnapshot, ITextSnapshot afterSnapshot, IContentType beforeContentType, IContentType afterContentType, object editTag)
            {
                this.beforeSnapshot = beforeSnapshot;
                this.afterSnapshot = afterSnapshot;
                this.editTag = editTag;
                this.beforeContentType = beforeContentType;
                this.afterContentType = afterContentType;
            }

            public void RaiseEvent(BaseBuffer baseBuffer, bool immediate)
            {
                EventHandler<ContentTypeChangedEventArgs> handler = immediate ? baseBuffer.ContentTypeChangedImmediate : baseBuffer.ContentTypeChanged;
                if (handler != null)
                {
                    var eventArgs = new ContentTypeChangedEventArgs(this.beforeSnapshot, this.afterSnapshot, this.beforeContentType, this.afterContentType, this.editTag);
                    baseBuffer.guardedOperations.RaiseEvent(baseBuffer, handler, eventArgs);
                }
            }

            public bool HasPostEvent
            {
                get { return true; }
            }
        }
        #endregion

        #region Editing Shortcuts
        public ITextSnapshot Insert(int position, string text)
        {
            using (ITextEdit textEdit = CreateEdit())
            {
                textEdit.Insert(position, text);
                return textEdit.Apply();
            }
        }

        public ITextSnapshot Delete(Span deleteSpan)
        {
            using (ITextEdit textEdit = CreateEdit())
            {
                textEdit.Delete(deleteSpan);
                return textEdit.Apply();
            }
        }

        public ITextSnapshot Replace(Span replaceSpan, string replaceWith)
        {
            using (ITextEdit textEdit = CreateEdit())
            {
                textEdit.Replace(replaceSpan, replaceWith);
                return textEdit.Apply();
            }
        }
        #endregion

        #region Change Application and Eventing
        protected void SetCurrentVersionAndSnapshot(INormalizedTextChangeCollection normalizedChanges, int reiteratedVersionNumber)
        {
            this.currentVersion = this.currentVersion.CreateNext(normalizedChanges, reiteratedVersionNumber);
            UpdateSnapshot();
        }

        protected void SetCurrentVersionAndSnapshot(INormalizedTextChangeCollection normalizedChanges)
        {
            this.currentVersion = this.currentVersion.CreateNext(normalizedChanges);
            UpdateSnapshot();
        }

        protected void UpdateSnapshot()
        {
            this.currentSnapshot = TakeSnapshot();
        }

        protected internal abstract ISubordinateTextEdit CreateSubordinateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag);


        public event EventHandler<TextContentChangingEventArgs> Changing;

        public event EventHandler<TextContentChangedEventArgs> ChangedHighPriority;
        public event EventHandler<TextContentChangedEventArgs> Changed;
        public event EventHandler<TextContentChangedEventArgs> ChangedLowPriority;

        public event EventHandler PostChanged;

        internal event EventHandler<TextContentChangedEventArgs> ChangedImmediate;
        internal event EventHandler<ContentTypeChangedEventArgs> ContentTypeChangedImmediate;

        internal void RawRaiseEvent(TextContentChangedEventArgs args, bool immediate)
        {
            if (immediate)
            {
                EventHandler<TextContentChangedEventArgs> immediateHandler = ChangedImmediate;
                if (immediateHandler != null)
                {
                    if (BaseBuffer.eventTracing)
                    {
                        Debug.WriteLine("<<< Imm  events from " + ToString());
                    }
                    immediateHandler(this, args);
                }
                return;
            }

            EventHandler<TextContentChangedEventArgs> highHandler = ChangedHighPriority;
            EventHandler<TextContentChangedEventArgs> medHandler = Changed;
            EventHandler<TextContentChangedEventArgs> lowHandler = ChangedLowPriority;

            BaseBuffer.eventDepth++;
            string indent = BaseBuffer.eventTracing ? new String(' ', 3 * (BaseBuffer.eventDepth - 1)) : null;
            if (highHandler != null)
            {
                if (BaseBuffer.eventTracing)
                {
                    Debug.WriteLine(">>> " + indent + "High events from " + ToString());
                }
                this.guardedOperations.RaiseEvent(this, highHandler, args);
            }
            if (medHandler != null)
            {
                if (BaseBuffer.eventTracing)
                {
                    Debug.WriteLine(">>> " + indent + "Med  events from " + ToString());
                }
                this.guardedOperations.RaiseEvent(this, medHandler, args);
            }
            if (lowHandler != null)
            {
                if (BaseBuffer.eventTracing)
                {
                    Debug.WriteLine(">>> " + indent + "Low  events from " + ToString());
                }
                this.guardedOperations.RaiseEvent(this, lowHandler, args);
            }
            BaseBuffer.eventDepth--;
        }

        internal void RaisePostChangedEvent()
        {
            this.guardedOperations.RaiseEvent(this, PostChanged);
        }

        internal void RaiseChangingEvent(TextContentChangingEventArgs args)
        {
            var changing = this.Changing;

            if (changing != null)
            {
                foreach (Delegate handlerDelegate in changing.GetInvocationList())
                {
                    var handler = (EventHandler<TextContentChangingEventArgs>)handlerDelegate;
                    try
                    {
                        handler(this, args);
                    }
                    catch (Exception e)
                    {
                        this.guardedOperations.HandleException(handler, e);
                    }

                    if (args.Canceled)
                        return;
                }
            }
        }
        #endregion

        #region Diagnostic Support

        public override string ToString()
        {
            string suffix = TextUtilities.GetTag(this);
            if (string.IsNullOrEmpty(suffix))
            {
                ITextSnapshot snap = this.currentSnapshot;
                if (snap != null)
                {
                    suffix = "\"" + snap.GetText(0, Math.Min(16, snap.Length)) + "\"";
                }
            }
            return this.ContentType.TypeName + ":" + suffix;
        }

#if _DEBUG
        private string DebugOnly_AllText
        {
            get
            {
                return this.currentSnapshot.DebugOnly_AllText;
            }
        }
#endif

        #endregion
    }
}
