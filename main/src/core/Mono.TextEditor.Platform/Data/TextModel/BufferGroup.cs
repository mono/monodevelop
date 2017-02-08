namespace Microsoft.VisualStudio.Text.Implementation
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Projection.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using System;

    internal class BufferGroup
    {
        private static bool tracing = false;
        private static bool detailedTracing = false;
        private static Exception LastMasterEditException = null;
        private static string LastMasterEditExceptionStackTrace = null;

        /// <summary>
        /// The text buffers that are members of this group. All buffers related by projection must be in the 
        /// same group.
        /// </summary>
        private readonly HashSet<BufferWeakReference> members = new HashSet<BufferWeakReference>();

        /// <summary>
        /// This queue is used to sequence all events raised due to snapshot changes. It ensures that
        /// events are always delivered in temporal order (i.e., no client is notified of snapshot N+1 before
        /// every client is notified of snapshot N) and also to ensure that events related to a projection edit
        /// are delivered 'bottom up'
        /// </summary>
        internal Queue<Tuple<BaseBuffer.ITextEventRaiser, BaseBuffer>> eventQueue = new Queue<Tuple<BaseBuffer.ITextEventRaiser, BaseBuffer>>();
        internal int depth = 0;
        internal bool eventingInProgress = false;

        private class GraphEntry
        {
            public HashSet<IProjectionBufferBase> Targets { get; private set; }
            public bool EditComplete { get; set; }
            public bool Dependent { get; set; }
            public GraphEntry(HashSet<IProjectionBufferBase> targets, bool editComplete, bool dependent)
            {
                Targets = targets;
                EditComplete = editComplete;
                Dependent = dependent;
            }
        }

        /// <summary>
        /// Transient fields that are valid during a 'master edit transaction'
        /// </summary>
        private BaseBuffer masterBuffer;
        private Dictionary<BaseBuffer, GraphEntry> graph;   // map from nodes to edges
        private Dictionary<ITextBuffer, ISubordinateTextEdit> buffer2EditMap;
        private HashSet<BaseProjectionBuffer> pendingIndependentBuffers;
        private EditOptions masterOptions;
        private object masterEditTag;

        public BufferGroup(ITextBuffer member)
        {
            this.members.Add(new BufferWeakReference(member));
        }

        #region Membership
        internal HashSet<BufferWeakReference> Members
        {
            get { return this.members; }
        }

        public bool MembersContains(ITextBuffer buffer)
        {
            return this.members.Contains(new BufferWeakReference(buffer));
        }

        public void AddMember(ITextBuffer member)
        {
            // Police this.members by removing the expired ones
            this.members.RemoveWhere((m) => m.Buffer == null);

            this.members.Add(new BufferWeakReference(member));
        }

        public void RemoveMember(ITextBuffer member)
        {
            // this is a very limited workaround to solve a Dev10 leak. New design for Dev11...
            Debug.Assert(this.masterBuffer == null);
            this.members.Remove(new BufferWeakReference(member));
        }

        public void Swallow(BufferGroup victim)
        {
            if (victim != this)
            {
                // todo: suitable checks to be sure it's OK to do this now
                foreach (var memberWeakReference in victim.Members)
                {
                    ITextBuffer member = memberWeakReference.Buffer;
                    if ((member != null) && this.members.Add(memberWeakReference))
                    {
                        BaseBuffer baseMember = (BaseBuffer)member;
                        baseMember.group = this;
                    }
                }

                if (victim.eventQueue.Count > 0)
                {
                    // maybe reconsider using Queue type
                    var newEventQueue = new Queue<Tuple<BaseBuffer.ITextEventRaiser, BaseBuffer>>(victim.eventQueue);
                    while (this.eventQueue.Count > 0)
                    {
                        newEventQueue.Enqueue(this.eventQueue.Dequeue());
                    }
                    this.eventQueue = newEventQueue;
                    victim.eventQueue.Clear();
                }
            }
        }
        #endregion

        public ITextBuffer MasterBuffer
        {
            get { return this.masterBuffer; }
        }

        public bool MasterEditInProgress
        {
            get { return this.masterBuffer != null; }
        }

        public static bool Tracing
        {
            get { return BufferGroup.tracing; }
            set { BufferGroup.tracing = value; }
        }

        public static bool DetailedTracing
        {
            get { return BufferGroup.detailedTracing; }
            set { BufferGroup.detailedTracing = value; }
        }

        public Dictionary<ITextBuffer, ISubordinateTextEdit> BufferToEditMap
        {
            get
            {
                if (this.buffer2EditMap == null)
                {
                    throw new InvalidOperationException();
                }
                return this.buffer2EditMap;
            }
        }

        public ITextEdit GetEdit(BaseBuffer buffer)
        {
            return GetEdit(buffer, this.masterOptions);
        }

        public ITextEdit GetEdit(BaseBuffer buffer, EditOptions options)
        {
            ISubordinateTextEdit subedit;
            if (!this.buffer2EditMap.TryGetValue(buffer, out subedit))
            {
                Debug.Assert(!(buffer is IProjectionBufferBase));
                subedit = buffer.CreateSubordinateEdit(options, null, this.masterEditTag);
                this.buffer2EditMap.Add(buffer, subedit);
            }
            return (ITextEdit)subedit;
        }

        [Conditional("DEBUG")]
        private static void Trace(ITextBuffer buffer, string operation)
        {
            if (BufferGroup.tracing)
            {
                string tag = TextUtilities.GetTag(buffer);
                if (string.IsNullOrEmpty(tag))
                {
                    tag = buffer.ToString();
                }

                Debug.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, "====> {0,12}: {1}", operation, tag));
            }
        }

        [Conditional("DEBUG")]
        private static void Trace(ISubordinateTextEdit subedit, string operation)
        {
            if (BufferGroup.tracing)
            {
                Trace(subedit.TextBuffer, operation);
                Debug.WriteLine(subedit.ToString());
            }
        }

        public void PerformMasterEdit(ITextBuffer buffer, ISubordinateTextEdit xedit, EditOptions options, object editTag)
        {
            Debug.Assert(this.MembersContains(buffer));
            if (this.masterBuffer != null)
            {
                // internal error
                throw new InvalidOperationException("Master edit already in progress");
            }

            try
            {
                this.masterBuffer = (BaseBuffer)buffer;
                this.masterOptions = options;
                this.masterEditTag = editTag;

                this.buffer2EditMap = new Dictionary<ITextBuffer, ISubordinateTextEdit>();
                this.buffer2EditMap.Add(buffer, xedit);

                this.pendingIndependentBuffers = new HashSet<BaseProjectionBuffer>();

                BuildGraph();

                Trace(buffer, "Master");

                Stack<ISubordinateTextEdit> appliedSubordinateEdits = new Stack<ISubordinateTextEdit>();
                while (this.buffer2EditMap.Count > 0)
                {
                    // Pick an edit that has no possibility of further impact from target projection buffers.
                    ISubordinateTextEdit subedit = PickEdit();
                    Trace(subedit, "Pre Apply");
                    PopulateSourceEdits(subedit.TextBuffer);
                    subedit.PreApply();                           // this may add more edits to buffer2EditMap
                    appliedSubordinateEdits.Push(subedit);
                }

                Action cancelAction = () =>
                    {
                        foreach (var edit in appliedSubordinateEdits)
                        {
                            edit.CancelApplication();
                        }

                        Debug.Assert(this.pendingIndependentBuffers.Count == 0);
                        Debug.Assert(this.depth == 0);

                        this.graph = null;
                        this.buffer2EditMap = null;
                        this.masterBuffer = null;
                        this.pendingIndependentBuffers = null;
                    };

                foreach (var edit in appliedSubordinateEdits)
                {
                    if (!edit.CheckForCancellation(cancelAction))
                    {
                        Debug.Assert(this.graph == null);
                        Debug.Assert(this.buffer2EditMap == null);
                        Debug.Assert(this.masterBuffer == null);
                        Debug.Assert(this.pendingIndependentBuffers == null);
                        Debug.Assert(this.eventQueue.Count == 0);
                        Debug.Assert(this.depth == 0);
                        Debug.Assert(!this.eventingInProgress);
                        return;
                    }
                }

                // pendingIndependentBuffers currently do not get a voice in cancelation.

                // now interpret events in the reverse order
                while (appliedSubordinateEdits.Count > 0)
                {
                    ISubordinateTextEdit subedit = appliedSubordinateEdits.Pop();
                    Trace(subedit.TextBuffer, "Final Apply");
                    subedit.FinalApply(); // this creates the snapshot and queues/raises events...TODO: make it return raisers
                }

                // move on to independent edits
                while (this.pendingIndependentBuffers.Count > 0)
                {
                    BaseProjectionBuffer projectionBuffer = PickIndependentBuffer();
                    Trace(projectionBuffer, "Propagate");
                    BaseBuffer.ITextEventRaiser raiser = projectionBuffer.PropagateSourceChanges(options, editTag);
                    this.eventQueue.Enqueue(new Tuple<BaseBuffer.ITextEventRaiser, BaseBuffer>(raiser, projectionBuffer));
                }
                
                this.graph = null;
                this.buffer2EditMap = null;
                this.masterBuffer = null;
                this.pendingIndependentBuffers = null;
            }
            catch (Exception e)
            {
                BufferGroup.LastMasterEditException = e;
                BufferGroup.LastMasterEditExceptionStackTrace = e.StackTrace;
                throw;
            }
        }

        public void ScheduleIndependentEdit(BaseProjectionBuffer projectionBuffer)
        {
            // A projection buffer has received a content change event from one of its source buffers, but 
            // it has no edit in progress. This means it is an independent buffer (with respect to the master edit)
            // and needs to have its event propagation scheduled.
            this.pendingIndependentBuffers.Add(projectionBuffer);
        }

        public void CancelIndependentEdit(BaseProjectionBuffer projectionBuffer)
        {
            // A buffer that thought it was independent turned out not to be so. May be called by buffers
            // that are not in the pendingIndependentBuffers set.
            this.pendingIndependentBuffers.Remove(projectionBuffer);
        }

        private void PopulateSourceEdits(ITextBuffer buffer)
        {
            IProjectionBufferBase projectionBuffer = buffer as IProjectionBufferBase;
            if (projectionBuffer != null)
            {
                // Add ITextEdit objects to the edit map for all source buffers of the current projection buffer that
                // are themselves projection buffers. This is so that such buffers are not considered independent buffers
                // if it happens that the master edit does not touch them directly (even though not touched directly, they
                // may receive events from their source buffers that were touched directly, and so FinalApply() will need
                // to be invoked on them in order to construct a snapshot that matches the last snapshots of their source buffers).
                foreach (ITextBuffer sourceBuffer in projectionBuffer.SourceBuffers)
                {
                    IProjectionBufferBase sourceProjectionBuffer = sourceBuffer as IProjectionBufferBase;
                    if (sourceProjectionBuffer != null)
                    {
                        BaseBuffer baseSourceBuffer = (BaseBuffer)sourceBuffer;
                        if (!this.buffer2EditMap.ContainsKey(baseSourceBuffer))
                        {
                            this.buffer2EditMap.Add(sourceBuffer, (baseSourceBuffer.CreateSubordinateEdit(this.masterOptions, null, this.masterEditTag)));
                        }
                    }
                }
            }
        }

        private ISubordinateTextEdit PickEdit()
        {
            // find a buffer for which we are sure that all potential subordinate edits have been generated.

            // this is an unsophisticated initial implementation. We are saved by the
            // fact that real graphs are small (e.g. the typical venus graph has four buffers,
            // five if outlining is involved)

            foreach (var pair in this.buffer2EditMap)
            {
                BaseBuffer buffer = (BaseBuffer)pair.Key;
                GraphEntry g;
                if (!this.graph.TryGetValue(buffer, out g))
                {
                    // if the buffer isn't in the graph, it is because it is being added to the graph
                    // as part of the current transaction (it must be the literal buffer of a projection buffer).
                    // We are cool to pick this buffer, which cannot possibly be affected by other edits.
                    this.buffer2EditMap.Remove(buffer);
                    return pair.Value;
                }
                else
                {
                    if (InvulnerableToFutureEdits(g))
                    {
                        g.EditComplete = true;
                        this.buffer2EditMap.Remove(buffer);     // can do better with indexed iteration
                        return pair.Value;
                    }
                }
            }

            throw new InvalidOperationException("Internal error in BufferGroup.PickEdit");
        }

        private bool InvulnerableToFutureEdits(GraphEntry graphEntry)
        {
            foreach (IProjectionBufferBase target in graphEntry.Targets)
            {
                BaseBuffer baseTarget = (BaseBuffer)target;
                GraphEntry targetEntry = this.graph[baseTarget];
                if (targetEntry.EditComplete)
                {
                    continue;
                }
                if (InvulnerableToFutureEdits(targetEntry) && !this.buffer2EditMap.ContainsKey(baseTarget))
                {
                    targetEntry.EditComplete = true;
                    continue;
                }
                return false;
            }
            return true;
        }

        private BaseProjectionBuffer PickIndependentBuffer()
        {
            // pick a buffer for which all source buffers have finished participating in the master edit.
            // source buffer could be stable because
            //  1. it is in the source closure of the master buffer -- all those edits are done by now
            //  2. it isn't in the target closure of the master buffer
            //  3. it has already been picked by this method

            // for the moment, we are ignoring #2, and such buffers won't get picked (resulting in an exception).

            foreach (BaseProjectionBuffer projectionBuffer in this.pendingIndependentBuffers)
            {
                bool ok = true;
                foreach (ITextBuffer sourceBuffer in projectionBuffer.SourceBuffers)
                {
                    if (!IsStableDuringIndependentPhase((BaseBuffer)sourceBuffer))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    GraphEntry gg = this.graph[(BaseBuffer)projectionBuffer];
                    gg.Dependent = true;    // indicate has been chosen
                    this.pendingIndependentBuffers.Remove(projectionBuffer);
                    return projectionBuffer;
                }
            }
            throw new InvalidOperationException("Couldn't pick an independent buffer");
        }

        private bool IsStableDuringIndependentPhase(BaseBuffer sourceBuffer)
        {
            BaseProjectionBuffer baseProjSourceBuffer = sourceBuffer as BaseProjectionBuffer;
            if (baseProjSourceBuffer != null && this.pendingIndependentBuffers.Contains(baseProjSourceBuffer))
            {
                return false;
            }
            else
            {
                GraphEntry g;
                if (this.graph.TryGetValue(sourceBuffer, out g))
                {
                    return g.Dependent || !InTargetClosureOfBuffer(sourceBuffer, this.masterBuffer);
                }
                else
                {
                    // must be a projection literal buffer
                    return true;
                }
            }
        }

        private bool InTargetClosureOfBuffer(BaseBuffer candidateBuffer, BaseBuffer governingBuffer)
        {
            GraphEntry g = this.graph[governingBuffer];
            foreach (var target in g.Targets)
            {
                if (target == candidateBuffer)
                {
                    return true;
                }
                if (InTargetClosureOfBuffer(candidateBuffer, (BaseBuffer)target))
                {
                    return true;
                }
            }
            return false;
        }

        #region Graph Construction
        private void BuildGraph()
        {
            // Police this.members by removing the expired ones
            this.members.RemoveWhere((member) => member.Buffer == null);

            this.graph = new Dictionary<BaseBuffer, GraphEntry>(this.members.Count);
            foreach (BufferWeakReference bufferWeakReference in this.members)
            {
                var buffer = bufferWeakReference.Buffer;
                if (buffer != null)     //There could have been a GC between the police action above and here.
                {
                    graph.Add(buffer, new GraphEntry(new HashSet<IProjectionBufferBase>(), false, false));
                }
            }
            foreach (BufferWeakReference bufferWeakReference in this.members)
            {
                IProjectionBufferBase p = bufferWeakReference.Buffer as IProjectionBufferBase;
                if (p != null)
                {
                    foreach (BaseBuffer source in p.SourceBuffers)
                    {
                        graph[source].Targets.Add(p);
                    }
                }
            }
            MarkMasterClosure(this.masterBuffer);
        }

        private void MarkMasterClosure(BaseBuffer buffer)
        {
            GraphEntry g = this.graph[buffer];
            if (!g.Dependent)
            {
                g.Dependent = true;
                IProjectionBufferBase projectionBuffer = buffer as IProjectionBufferBase;
                if (projectionBuffer != null)
                {
                    foreach (BaseBuffer source in projectionBuffer.SourceBuffers)
                    {
                        MarkMasterClosure(source);
                    }
                }
            }
        }

        public string DumpGraph()
        {
            StringBuilder sb = new StringBuilder("BufferGroup Graph");
            if (this.graph == null)
            {
                sb.AppendLine(" <null>");
            }
            else
            {
                sb.AppendLine("");
                foreach (var p in this.graph)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,8} {1}: ", TextUtilities.GetTag(p.Key), p.Value.EditComplete ? "T" : "F"));
                    foreach (var target in p.Value.Targets)
                    {
                        sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,8},", TextUtilities.GetTag(target)));
                    }
                    sb.Append("\r\n");
                }
            }
            return sb.ToString();
        }
        #endregion

        #region Edit and Event Management
        public void BeginEdit()
        {
            if (depth < 0)
            {
                throw new System.InvalidOperationException();
            }
            depth++;
        }

        public void FinishEdit()
        {
            if (depth <= 0)
            {
                throw new System.InvalidOperationException();
            }
            if (--depth == 0)
            {
                RaiseEvents();
            }
        }

        public void CancelEdit()
        {
            // if we were being transactional (that is, if we had multi-buffer transactions),
            // we would cancel events here. But we aren't.
            FinishEdit();
        }

        public void EnqueueEvents(BaseBuffer.ITextEventRaiser raiser, BaseBuffer baseBuffer)
        {
            if (depth <= 0)
            {
                throw new System.InvalidOperationException();
            }
            this.eventQueue.Enqueue(new Tuple<BaseBuffer.ITextEventRaiser, BaseBuffer>(raiser, baseBuffer));
        }

        public void EnqueueEvents(IEnumerable<BaseBuffer.ITextEventRaiser> raisers, BaseBuffer baseBuffer)
        {
            if (depth <= 0)
            {
                throw new System.InvalidOperationException();
            }
            foreach (var raiser in raisers)
            {
                this.eventQueue.Enqueue(new Tuple<BaseBuffer.ITextEventRaiser, BaseBuffer>(raiser, baseBuffer));
            }
        }

        private void RaiseEvents()
        {
            if (!this.eventingInProgress)
            {
                List<BaseBuffer> buffersToRaisePostChangedEvent = new List<BaseBuffer>();
                try
                {
                    this.eventingInProgress = true;
                    while (this.eventQueue.Count > 0)
                    {
                        var pair = this.eventQueue.Dequeue();
                        pair.Item1.RaiseEvent(pair.Item2, false);

                        if (pair.Item1.HasPostEvent)
                        {
                            buffersToRaisePostChangedEvent.Add(pair.Item2);
                        }
                    }
                }
                finally
                {
                    this.eventingInProgress = false;
                }

                foreach (var buffer in buffersToRaisePostChangedEvent)
                {
                    // Raise the post changed event in the same order the standard events
                    // were raised
                    buffer.RaisePostChangedEvent();
                }
            }
        }
        #endregion

        /// <summary>
        /// Helper class that lets a WeakReference play in a HashSet in much the same way that a conventional pointer would.
        /// The goal here is to let the BufferGroup keep a HashSet of its member buffers without preventing the members from
        /// being GC'd if everyone else has forgotten about them.
        /// </summary>
        internal class BufferWeakReference
        {
            private readonly WeakReference<BaseBuffer> _buffer;
            private readonly int _hashCode;

            public BufferWeakReference(ITextBuffer buffer)
            {
                _buffer = new WeakReference<BaseBuffer>((BaseBuffer)buffer);
                _hashCode = buffer.GetHashCode();
            }

            public BaseBuffer Buffer
            {
                get
                {
                    BaseBuffer buffer;
                    if (_buffer.TryGetTarget(out buffer))
                    {
                        return buffer;
                    }

                    return null;
                }
            }

            // Override the normal equality semantics so that two instances of a BufferWeakReference for the same buffer will be "equal"
            public override bool Equals(object obj)
            {
                if (object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                BufferWeakReference other = obj as BufferWeakReference;
                if (other != null)
                {
                    var myBuffer = this.Buffer;

                    // There are two interesting scenarios where we should return true:
                    //   We have two different BufferWeakReferences that refer to the same buffer.
                    //   We have a dead BufferWeakReference that we are removing from the list. In this case
                    //   we'll return true on the object.ReferenceEquals above.
                    //
                    // Given how we are using the BufferWeakReference, we will never have the situation where
                    // there were two distinct instances of a BufferWeakReference for the same buffer, the buffer
                    // died, and an equality test was done on the two dead references.
                    //
                    // As it stands, either the source buffer is alive (because we're doing a
                    //    this.members.Remove(new BufferWeakReference(member));
                    // (in which case the buffer in the WeakReference we are comparing it with is alive) or we're doing a
                    //    this.members.RemoveWhere(b => ...)
                    // (in which case the ReferenceEquals test will work).
                    return (myBuffer == other.Buffer) && (myBuffer != null);
                 }

                return false;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }
    }
}
