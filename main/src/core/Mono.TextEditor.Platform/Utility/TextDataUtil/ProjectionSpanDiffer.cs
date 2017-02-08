namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Projection;

    internal class ProjectionSpanDiffer
    {
        private IDifferenceService diffService;

        private ReadOnlyCollection<SnapshotSpan> inputDeletedSnapSpans;
        private ReadOnlyCollection<SnapshotSpan> inputInsertedSnapSpans;
        private bool computed;

        // exposed to unit tests
        internal List<SnapshotSpan>[] deletedSurrogates;
        internal List<SnapshotSpan>[] insertedSurrogates;

        public static ProjectionSpanDifference DiffSourceSpans(IDifferenceService diffService, 
                                                               IProjectionSnapshot left, 
                                                               IProjectionSnapshot right)
        {
            if (left == null)
            {
                throw new ArgumentNullException("left");
            }
            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            if (!object.ReferenceEquals(left.TextBuffer, right.TextBuffer))
            {
                throw new ArgumentException("left does not belong to the same text buffer as right");
            }

            ProjectionSpanDiffer differ = new ProjectionSpanDiffer
                                                (diffService,
                                                 left.GetSourceSpans(),
                                                 right.GetSourceSpans());

            return new ProjectionSpanDifference(differ.GetDifferences(), differ.InsertedSpans, differ.DeletedSpans);
        }

        public ProjectionSpanDiffer(IDifferenceService diffService,
                                    ReadOnlyCollection<SnapshotSpan> deletedSnapSpans,
                                    ReadOnlyCollection<SnapshotSpan> insertedSnapSpans)
        {
            this.diffService = diffService;
            this.inputDeletedSnapSpans = deletedSnapSpans;
            this.inputInsertedSnapSpans = insertedSnapSpans;
        }

        public ReadOnlyCollection<SnapshotSpan> DeletedSpans { get; private set; }
        public ReadOnlyCollection<SnapshotSpan> InsertedSpans { get; private set; }

        public IDifferenceCollection<SnapshotSpan> GetDifferences()
        {
            if (!this.computed)
            {
                DecomposeSpans();
                this.computed = true;
            }

            var deletedSpans = new List<SnapshotSpan>();
            var insertedSpans = new List<SnapshotSpan>();

            DeletedSpans = deletedSpans.AsReadOnly();
            InsertedSpans = insertedSpans.AsReadOnly();

            for (int s = 0; s < deletedSurrogates.Length; ++s)
            {
                deletedSpans.AddRange(deletedSurrogates[s]);
            }

            for (int s = 0; s < insertedSurrogates.Length; ++s)
            {
                insertedSpans.AddRange(insertedSurrogates[s]);
            }

            return this.diffService.DifferenceSequences(deletedSpans, insertedSpans);
        }

        #region Internal (for testing) helpers
        internal void DecomposeSpans()
        {
            // Prepare spans for diffing. The basic idea is this: suppose we have input spans from some source snapshot as follows:
            //
            // deleted:  0..10
            // inserted: 0..3 7..10
            //
            // We would like to raise a text change event that indicates that the text from 3..7 was deleted, rather than
            // an event indicating that all the text from 0..10 was deleted and replaced. We could simply compute a string
            // difference of the before & after text, but there might be a lot of text (so that would be expensive), and we
            // also don't want to suppress eventing when identical text comes from different source buffers (which might have
            // different content types). So, this routine converts the input spans into a form suitable for diffing:
            //
            // deleted: 0..3 3..7 7..10
            // inserted 0..3 7..10
            //
            // then we compute the differences of the spans qua spans, which in this case will indicate that the span named "3..7"
            // was deleted, and that's what we use to generate text change events.

            // what to substitute for input spans during diffing
            this.deletedSurrogates = new List<SnapshotSpan>[this.inputDeletedSnapSpans.Count];
            this.insertedSurrogates = new List<SnapshotSpan>[this.inputInsertedSnapSpans.Count];

            // collect spans by text buffer

            Dictionary<ITextSnapshot, List<Thing>> buffer2DeletedThings = new Dictionary<ITextSnapshot, List<Thing>>();
            for (int ds = 0; ds < this.inputDeletedSnapSpans.Count; ++ds)
            {
                SnapshotSpan ss = this.inputDeletedSnapSpans[ds];
                List<Thing> things;
                if (!buffer2DeletedThings.TryGetValue(ss.Snapshot, out things))
                {
                    things = new List<Thing>();
                    buffer2DeletedThings.Add(ss.Snapshot, things);
                }
                things.Add(new Thing(ss.Span, ds));

                // unrelated
                deletedSurrogates[ds] = new List<SnapshotSpan>();
            }

            Dictionary<ITextSnapshot, List<Thing>> buffer2InsertedThings = new Dictionary<ITextSnapshot, List<Thing>>();
            for (int ns = 0; ns < this.inputInsertedSnapSpans.Count; ++ns)
            {
                SnapshotSpan ss = this.inputInsertedSnapSpans[ns];
                List<Thing> things;
                if (!buffer2InsertedThings.TryGetValue(ss.Snapshot, out things))
                {
                    things = new List<Thing>();
                    buffer2InsertedThings.Add(ss.Snapshot, things);
                }
                things.Add(new Thing(ss.Span, ns));

                // unrelated
                insertedSurrogates[ns] = new List<SnapshotSpan>();
            }

            foreach (KeyValuePair<ITextSnapshot, List<Thing>> pair in buffer2DeletedThings)
            {
                List<Thing> insertedThings;
                ITextSnapshot snapshot = pair.Key;
                if (buffer2InsertedThings.TryGetValue(snapshot, out insertedThings))
                {
                    List<Thing> deletedThings = pair.Value;
                    insertedThings.Sort(Comparison);
                    deletedThings.Sort(Comparison);

                    int i = 0;
                    int d = 0;
                    do
                    {
                        Span inserted = insertedThings[i].span;
                        Span deleted = deletedThings[d].span;
                        Span? overlap = inserted.Overlap(deleted);

                        if (overlap == null)
                        {
                            if (inserted.Start < deleted.Start)
                            {
                                i++;
                            }
                            else
                            {
                                d++;
                            }
                        }
                        else
                        {
                            NormalizedSpanCollection insertedResidue = NormalizedSpanCollection.Difference(new NormalizedSpanCollection(inserted),
                                                                                                           new NormalizedSpanCollection(overlap.Value));  // todo add overload to normalizedspancollection
                            if (insertedResidue.Count > 0)
                            {
                                int pos = insertedThings[i].position;
                                insertedThings.RemoveAt(i);
                                bool didOverlap = false;
                                int ir = 0;
                                while (ir < insertedResidue.Count)
                                {
                                    Span r = insertedResidue[ir];
                                    if (didOverlap || r.Start < overlap.Value.Start)
                                    {
                                        insertedThings.Insert(i++, new Thing(r, pos));
                                        ir++;
                                    }
                                    else
                                    {
                                        insertedThings.Insert(i++, new Thing(overlap.Value, pos));
                                        didOverlap = true;
                                    }
                                }
                                if (!didOverlap)
                                {
                                    insertedThings.Insert(i++, new Thing(overlap.Value, pos));
                                }
                                i--;
                            }

                            NormalizedSpanCollection deletedResidue = NormalizedSpanCollection.Difference(new NormalizedSpanCollection(deleted),
                                                                                                          new NormalizedSpanCollection(overlap.Value));
                            if (deletedResidue.Count > 0)
                            {
                                int pos = deletedThings[d].position;
                                deletedThings.RemoveAt(d);
                                bool didOverlap = false;
                                int dr = 0;
                                while (dr < deletedResidue.Count)
                                {
                                    Span r = deletedResidue[dr];
                                    if (didOverlap || r.Start < overlap.Value.Start)
                                    {
                                        deletedThings.Insert(d++, new Thing(r, pos));
                                        dr++;
                                    }
                                    else
                                    {
                                        deletedThings.Insert(d++, new Thing(overlap.Value, pos));
                                        didOverlap = true;
                                    }
                                }
                                if (!didOverlap)
                                {
                                    deletedThings.Insert(d++, new Thing(overlap.Value, pos));
                                }
                                d--;
                            }
                        }
                        if (inserted.End <= deleted.End)
                        {
                            i++;
                        }
                        if (deleted.End <= inserted.End)
                        {
                            d++;
                        }
                    } while (i < insertedThings.Count && d < deletedThings.Count);
                }
            }

            foreach (KeyValuePair<ITextSnapshot, List<Thing>> pair in buffer2DeletedThings)
            {
                foreach (Thing t in pair.Value)
                {
                    deletedSurrogates[t.position].Add(new SnapshotSpan(pair.Key, t.span));
                }
            }

            foreach (KeyValuePair<ITextSnapshot, List<Thing>> pair in buffer2InsertedThings)
            {
                foreach (Thing t in pair.Value)
                {
                    insertedSurrogates[t.position].Add(new SnapshotSpan(pair.Key, t.span));
                }
            }
        }

        #endregion

        #region Private helpers
        private class Thing
        {
            public Span span;
            public int position;
            public Thing(Span span, int position)
            {
                this.span = span;
                this.position = position;
            }
        }

        private int Comparison(Thing left, Thing right)
        {
            return left.span.Start - right.span.Start;
        }
        #endregion

    }
}
