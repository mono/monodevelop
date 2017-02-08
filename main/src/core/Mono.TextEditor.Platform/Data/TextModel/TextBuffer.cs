namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Utilities;

    internal sealed partial class TextBuffer : BaseBuffer
    {
        #region BasicEdit class
        private class BasicEdit : Edit, ISubordinateTextEdit
        {
            private TextBuffer textBuffer;
            private bool subordinate;

            public BasicEdit(TextBuffer textBuffer, ITextSnapshot originSnapshot, EditOptions options, int? reiteratedVersionNumber, object editTag)
                : base(textBuffer, originSnapshot, options, reiteratedVersionNumber, editTag)
            {
                this.textBuffer = textBuffer;
                this.subordinate = true;
            }

            public ITextBuffer TextBuffer
            {
                get { return this.textBuffer; }
            }

            // this is the master edit path -- initiated from outside
            protected override ITextSnapshot PerformApply()
            {
                CheckActive();
                this.applied = true;
                this.subordinate = false;

                ITextSnapshot result = this.textBuffer.CurrentSnapshot;

                if (this.changes.Count > 0)
                {
                    if (this.textBuffer.group.Members.Count == 1 || this.textBuffer.spurnGroup)
                    {
                        // Take a simpler faster path if there are no other buffers in our group
                        if (this.CheckForCancellation(() => { }))
                        {
                            FinalApply();
                            result = this.textBuffer.CurrentSnapshot;
                        }
                        else
                        {
                            Debug.Assert(this.canceled);
                            Debug.Assert(!this.baseBuffer.editInProgress);
                        }
                    }
                    else
                    {
                        this.textBuffer.group.PerformMasterEdit(this.textBuffer, this, this.options, this.editTag);

                        if (!this.Canceled)
                        {
                            result = this.textBuffer.CurrentSnapshot;
                        }
                    }
                }
                else
                {
                    // vacuous edit
                    this.baseBuffer.editInProgress = false;
                }

                Debug.Assert(!this.baseBuffer.editInProgress);

                return result;
            }

            public void PreApply()
            {
                // called for all non-vacuous edits
                // everything happens in FinalApply()
            }

            public void FinalApply()
            {
                Debug.Assert(!this.canceled);
                if (this.changes.Count > 0)
                {
                    ITextEventRaiser eventRaiser = this.textBuffer.ApplyChangesAndSetSnapshot(this.changes, this.options, this.reiteratedVersionNumber, this.editTag);
                    this.baseBuffer.group.EnqueueEvents(eventRaiser, this.baseBuffer);

                    // raise immediate events
                    eventRaiser.RaiseEvent(this.baseBuffer, true);
                }

                this.baseBuffer.editInProgress = false;
                if (this.subordinate)
                {
                    this.baseBuffer.group.FinishEdit();
                }
            }
        }
        #endregion

        #region ReloadEdit class
        private class ReloadEdit : TextBufferBaseEdit, ISubordinateTextEdit
        {
            private IStringRebuilder newContent;
            private TextBuffer textBuffer;
            private ITextSnapshot originSnapshot;
            private object editTag;
            private EditOptions editOptions;
            private TextContentChangingEventArgs raisedChangingEventArgs;
            private Action cancelAction;

            public ReloadEdit(TextBuffer textBuffer, ITextSnapshot originSnapshot, EditOptions editOptions, object editTag) : base(textBuffer)
            {
                this.textBuffer = textBuffer;
                this.originSnapshot = originSnapshot;
                this.editOptions = editOptions;
                this.editTag = editTag;
            }

            public ITextSnapshot ReloadContent(IStringRebuilder newContent)
            {
                if (this.baseBuffer.IsReadOnlyImplementation(new Span(0, this.originSnapshot.Length), isEdit: true))
                {
                    this.applied = true;
                    this.baseBuffer.editInProgress = false;
                    this.baseBuffer.group.FinishEdit();
                    return this.originSnapshot;
                }
                else
                {
                    this.newContent = newContent;
                    this.baseBuffer.group.PerformMasterEdit(this.textBuffer, this, this.editOptions, this.editTag);
                    this.baseBuffer.group.FinishEdit();
                    return this.textBuffer.CurrentSnapshot;
                }
            }

            public void PreApply()
            {
            }

            // copied from BaseBuffer.Edit. Could arrange to inherit.
            public bool CheckForCancellation(Action cancelationResponse)
            {
                Debug.Assert(this.raisedChangingEventArgs == null, "just checking");
                if (this.raisedChangingEventArgs == null)
                {
                    this.cancelAction = cancelationResponse;
                    this.raisedChangingEventArgs = new TextContentChangingEventArgs(this.originSnapshot, this.editTag, (args) =>
                    {
                        this.Cancel();
                    });
                    this.baseBuffer.RaiseChangingEvent(this.raisedChangingEventArgs);
                }
                this.canceled = this.raisedChangingEventArgs.Canceled;
                return !this.raisedChangingEventArgs.Canceled;
            }

            public void FinalApply()
            {
                TextContentChangedEventArgs args = this.textBuffer.ApplyReload(this.newContent, this.editOptions, this.editTag);
                TextContentChangedEventRaiser raiser = new TextContentChangedEventRaiser(this.originSnapshot, this.baseBuffer.currentSnapshot, this.editOptions, this.editTag);
                this.applied = true;
                this.baseBuffer.group.EnqueueEvents(raiser, this.baseBuffer);
                raiser.RaiseEvent(this.baseBuffer, true);
                this.baseBuffer.editInProgress = false;
            }

            public ITextBuffer TextBuffer
            {
                get { return this.baseBuffer; }
            }

            public void RecordMasterChangeOffset(int masterChangeOffset)
            {
                throw new InvalidOperationException("Reloads should not be getting offsets from any other change.");
            }
        }
        #endregion

        #region State and Construction
        IStringRebuilder builder;
        bool spurnGroup;

        public TextBuffer(IContentType contentType, IStringRebuilder content, ITextDifferencingService textDifferencingService, GuardedOperations guardedOperations)
            : this(contentType, content, textDifferencingService, guardedOperations, false)
        {
        }

        public TextBuffer(IContentType contentType, IStringRebuilder content, ITextDifferencingService textDifferencingService, GuardedOperations guardedOperations, bool spurnGroup)
            : base(contentType, content.Length, textDifferencingService, guardedOperations)
        {
            // Parameters are validated outside
            this.group = new BufferGroup(this);
            this.builder = content;
            this.spurnGroup = spurnGroup;
            this.currentSnapshot = this.TakeSnapshot();
        }
        #endregion

        #region Reload
        /// <summary>
        /// Replace the contents of the buffer with the contents of a different string rebuilder.
        /// </summary>
        /// <param name="newContent">The new contents of the buffer (presumably read from a file).</param>
        /// <param name="editOptions">Options to apply to the edit. Differencing is highly likely to be selected.</param>
        /// <param name="editTag">Arbitrary tag associated with the reload that will appear in event arguments.</param>
        /// <returns></returns>
        public ITextSnapshot ReloadContent(IStringRebuilder newContent, EditOptions editOptions, object editTag)
        {
            using (ReloadEdit edit = new ReloadEdit(this, this.currentSnapshot, editOptions, editTag))
            {
                return edit.ReloadContent(newContent);
            }
        }

        internal TextContentChangedEventArgs ApplyReload(IStringRebuilder newContent, EditOptions editOptions, object editTag)
        {
            // we construct a normalized change list where the inserted text is a reference string that
            // points "forward" to the next snapshot and whose deleted text is a reference string that points
            // "backward" to the prior snapshot. This pins both snapshots in memory but that's better than materializing
            // giant strings, and when (?) we have paging text storage, memory requirements will be minimal.
            TextVersion newVersion = new TextVersion(this, this.currentVersion.VersionNumber + 1, this.currentVersion.VersionNumber + 1, newContent.Length);
            ITextSnapshot oldSnapshot = this.currentSnapshot;
            TextSnapshot newSnapshot = new TextSnapshot(this, newVersion, newContent);
            ReferenceChangeString oldText = new ReferenceChangeString(new SnapshotSpan(oldSnapshot, 0, oldSnapshot.Length));
            ReferenceChangeString newText = new ReferenceChangeString(new SnapshotSpan(newSnapshot, 0, newSnapshot.Length));
            TextChange change = new TextChange(oldPosition: 0,
                                               oldText: oldText,
                                               newText: newText,
                                               currentSnapshot: oldSnapshot);
            this.currentVersion.AddNextVersion(NormalizedTextChangeCollection.Create(new FrugalList<TextChange>() { change }, 
                                                                                     editOptions.ComputeMinimalChange 
                                                                                         ? (StringDifferenceOptions?)editOptions.DifferenceOptions 
                                                                                         : null, 
                                                                                     this.textDifferencingService,
                                                                                     oldSnapshot, newSnapshot), 
                                               newVersion);
            this.builder = newContent;
            this.currentVersion = newVersion;
            this.currentSnapshot = newSnapshot;
            return new TextContentChangedEventArgs(oldSnapshot, newSnapshot, editOptions, editTag);
        }
        #endregion

        #region Overridden methods
        public override ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            return new BasicEdit(this, this.currentSnapshot, options, reiteratedVersionNumber, editTag);
        }

        protected internal override ISubordinateTextEdit CreateSubordinateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            return new BasicEdit(this, this.currentSnapshot, options, reiteratedVersionNumber, editTag);
        }

        private ITextEventRaiser ApplyChangesAndSetSnapshot(FrugalList<TextChange> changes, EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            INormalizedTextChangeCollection normalizedChanges = NormalizedTextChangeCollection.Create(changes,
                                                                                                      options.ComputeMinimalChange ? (StringDifferenceOptions?)options.DifferenceOptions : null,
                                                                                                      this.textDifferencingService);
            int changeCount = normalizedChanges.Count;
            for (int c = 0; c < changeCount; ++c)
            {
                ITextChange change = normalizedChanges[c];
                this.builder = this.builder.Replace(new Span(change.NewPosition, change.OldLength), change.NewText);
            }
            ITextSnapshot originSnapshot = base.CurrentSnapshot;
            if (reiteratedVersionNumber.HasValue)
            {
                base.SetCurrentVersionAndSnapshot(normalizedChanges, reiteratedVersionNumber.Value);
            }
            else
            {
                base.SetCurrentVersionAndSnapshot(normalizedChanges);
            }
            return new TextContentChangedEventRaiser(originSnapshot, this.CurrentSnapshot, options, editTag);
        }

        protected override BaseSnapshot TakeSnapshot()
        {
            return new TextSnapshot(this, this.currentVersion, this.builder);
        }
        #endregion
    }
}
