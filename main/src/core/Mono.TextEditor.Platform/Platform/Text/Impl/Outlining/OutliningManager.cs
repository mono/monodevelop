//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Tagging;
    using System.Linq;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.VisualStudio.Text.Utilities;

    internal sealed class OutliningManager : IAccurateOutliningManager
    {
        private ITextBuffer editBuffer;
        private IAccurateTagAggregator<IOutliningRegionTag> tagAggregator;
        private bool isEnabled = true;
        internal bool isDisposed;

        // We store only the collapsed regions and generate the expanded regions on demand.
        TrackingSpanTree<Collapsed> collapsedRegionTree;

        internal OutliningManager(ITextBuffer editBuffer, ITagAggregator<IOutliningRegionTag> tagAggregator, IEditorOptions options)
        {
            this.editBuffer = editBuffer;
            this.tagAggregator = tagAggregator as IAccurateTagAggregator<IOutliningRegionTag>;

            bool keepTrackingCurrent = false;
            if (options != null && options.IsOptionDefined("Stress Test Mode", false))
            {
                keepTrackingCurrent = options.GetOptionValue<bool>("Stress Test Mode");
            }
            collapsedRegionTree = new TrackingSpanTree<Collapsed>(editBuffer, keepTrackingCurrent);

            tagAggregator.BatchedTagsChanged += OutliningRegionTagsChanged;
            this.editBuffer.Changed += SourceTextChanged;
        }

        #region Events and event listeners

        public event EventHandler<RegionsChangedEventArgs> RegionsChanged;
        public event EventHandler<RegionsExpandedEventArgs> RegionsExpanded;
        public event EventHandler<RegionsCollapsedEventArgs> RegionsCollapsed;
        public event EventHandler<OutliningEnabledEventArgs> OutliningEnabledChanged;

        void OutliningRegionTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            if (!isEnabled)
            {
                return;
            }

            // Collect the spans from the various change events
            UpdateAfterChange(new NormalizedSnapshotSpanCollection(e.Spans.SelectMany(s => s.GetSpans(editBuffer))));
        }

        void SourceTextChanged(object sender, TextContentChangedEventArgs e)
        {
            if (!isEnabled)
            {
                return;
            }

            if (e.Changes.Count > 0)
            {
                UpdateAfterChange(new NormalizedSnapshotSpanCollection(e.After, e.Changes.Select(c => c.NewSpan)));

                AvoidPartialLinebreaks(e);
            }
        }

        private void AvoidPartialLinebreaks(TextContentChangedEventArgs args)
        {
            // The elision buffer and the view don't handle collapsed region
            // boundaries that fall within a two-character \r\n linebreak.
            // Currently the most common cause of such situations is regex
            // find replace operations and this tactical fix expands affected
            // collapsed regions in response to problematic replace operations
            // and any other such edits.

            var oldSnapshot = args.Before;

            bool expandAll = false;

            foreach (var change in args.Changes)
            {
                if (change.OldLength == 0)
                    continue;

                if (change.OldPosition > 0
                    && oldSnapshot[change.OldPosition] == '\n'
                    && oldSnapshot[change.OldPosition - 1] == '\r')
                {
                    expandAll = true;
                    break;
                }

                if (change.OldEnd > 0
                    && change.OldEnd < oldSnapshot.Length
                    && oldSnapshot[change.OldEnd] == '\n'
                    && oldSnapshot[change.OldEnd - 1] == '\r')
                {
                    expandAll = true;
                    break;
                }
            }

            if (expandAll)
            {
                this.ExpandAll(
                    new SnapshotSpan(
                        args.After,
                        Span.FromBounds(
                            args.Changes[0].NewPosition,
                            args.Changes[args.Changes.Count - 1].NewEnd)),
                    collapsed => true);
            }
        }

        void UpdateAfterChange(NormalizedSnapshotSpanCollection changedSpans)
        {
            // It's possible that we've been informed of an update (via BatchedTagsChanged or otherwise) that no longer maps to the
            // edit buffer.  As a result of this, there aren't any changed spans for us to consider, so we can return immediately.
            if (changedSpans.Count == 0)
                return;

            var currentCollapsed = GetCollapsedRegionsInternal(changedSpans, exposedRegionsOnly: false);

            if (currentCollapsed.Count > 0)
            {
                // When getting tags, we'll try to be as minimal as possible, since
                // this edit could be large and/or multi-part.  We'll only examine
                // the intersection of the given changed spans and the collapsed
                // regions.
                // NOTE: We could try to be even smarter and only use the child-most regions
                // that we've collected, but it's a bit hard to determine which ones they
                // are at this point (they are the nodes that have 0 children that intersect
                // the changed spans, not just nodes that have 0 children).
                
                var snapshot = changedSpans[0].Snapshot;
                var spansToCheck = NormalizedSnapshotSpanCollection.Intersection(
                        changedSpans,
                        new NormalizedSnapshotSpanCollection(
                            currentCollapsed.Select(c => c.Extent.GetSpan(snapshot))));

                var newCollapsibles = CollapsiblesFromTags(tagAggregator.GetTags(spansToCheck)).Keys;

                IEnumerable<ICollapsed> removed;

                MergeRegions(currentCollapsed, newCollapsibles, out removed);

                List<ICollapsible> expandedRegions = new List<ICollapsible>();

                foreach (var removedRegion in removed)
                {
                    var expandedRegion = this.ExpandInternal(removedRegion);

                    expandedRegions.Add(expandedRegion);
                }

                if (expandedRegions.Count > 0)
                {
                    // Send out the regions expanded event with the flag informing
                    // listeners that these regions are being removed.
                    var expandedEvent = RegionsExpanded;
                    if (expandedEvent != null)
                    {
                        expandedEvent(this, new RegionsExpandedEventArgs(expandedRegions, removalPending: true));
                    }
                }
            }

            // Send out the general "outlining has changed" event
            var handler = RegionsChanged;
            if (handler != null)
            {
                handler(this, new RegionsChangedEventArgs(new SnapshotSpan(changedSpans[0].Start, changedSpans[changedSpans.Count - 1].End)));
            }
        }

        #endregion

        public ICollapsed TryCollapse(ICollapsible collapsible)
        {
            ICollapsed newCollapsed = CollapseInternal(collapsible);

            if (newCollapsed == null)
                return newCollapsed;

            // Raise event.
            var handler = RegionsCollapsed;
            if (handler != null)
            {
                handler(this, new RegionsCollapsedEventArgs(Enumerable.Repeat(newCollapsed, 1)));
            }

            return newCollapsed;
        }

        private ICollapsed CollapseInternal(ICollapsible collapsible)
        {
            EnsureValid();

            if (collapsible.IsCollapsed)
                return null;

            Collapsed newCollapsed = new Collapsed(collapsible.Extent, collapsible.Tag);

            newCollapsed.Node = collapsedRegionTree.TryAddItem(newCollapsed, newCollapsed.Extent);

            if (newCollapsed.Node == null)
                return null;

            return newCollapsed;
        }

        public ICollapsible Expand(ICollapsed collapsed)
        {
            ICollapsible newCollapsible = ExpandInternal(collapsed);

            // Send out change event
            var handler = RegionsExpanded;
            if (handler != null)
            {
                handler(this, new RegionsExpandedEventArgs(Enumerable.Repeat(newCollapsible, 1)));
            }

            return newCollapsible;
        }

        private ICollapsible ExpandInternal(ICollapsed collapsed)
        {
            EnsureValid();

            Collapsed internalCollapsed = collapsed as Collapsed;
            if (internalCollapsed == null)
            {
                throw new ArgumentException("The given collapsed region was not created by this outlining manager.",
                                            "collapsed");
            }

            if (!internalCollapsed.IsValid)
            {
                throw new InvalidOperationException("The collapsed region is invalid, meaning it has already been expanded.");
            }

            if (!collapsedRegionTree.RemoveItem(internalCollapsed, internalCollapsed.Extent))
            {
                throw new ApplicationException("Unable to remove the collapsed region from outlining manager, which means there is an internal " +
                                                    "consistency issue.");
            }

            // Now that we've expanded the region, invalidate the ICollapsed so it can no longer be used.
            internalCollapsed.Invalidate();

            return new Collapsible(collapsed.Extent, collapsed.Tag);
        }

        public IEnumerable<ICollapsed> CollapseAll(SnapshotSpan span, Predicate<ICollapsible> match)
        {
            return this.InternalCollapseAll(span, match, cancel: null);
        }

        internal IEnumerable<ICollapsed> InternalCollapseAll(SnapshotSpan span, Predicate<ICollapsible> match, CancellationToken? cancel)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            EnsureValid(span);

            List<ICollapsed> allCollapsed = new List<ICollapsed>();

            foreach (var collapsible in this.InternalGetAllRegions(new NormalizedSnapshotSpanCollection(span), exposedRegionsOnly: false, cancel: cancel))
            {
                if (!collapsible.IsCollapsed && collapsible.IsCollapsible && match(collapsible))
                {
                    var collapsed = this.CollapseInternal(collapsible);

                    if (collapsed != null)
                    {
                        allCollapsed.Add(collapsed);
                    }
                }
            }

            if (allCollapsed.Count > 0)
            {
                // Send out change event
                var handler = RegionsCollapsed;
                if (handler != null)
                {
                    handler(this, new RegionsCollapsedEventArgs(allCollapsed));
                }
            }

            return allCollapsed;
        }

        public IEnumerable<ICollapsible> ExpandAll(SnapshotSpan span, Predicate<ICollapsed> match)
        {
            return ExpandAllInternal(/*removalPending = */ false, span, match);
        }

        public IEnumerable<ICollapsible> ExpandAllInternal(bool removalPending, SnapshotSpan span, Predicate<ICollapsed> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            EnsureValid(span);

            List<ICollapsible> allExpanded = new List<ICollapsible>();

            foreach (var collapsed in this.GetCollapsedRegions(span))
            {
                if (match(collapsed))
                {
                    var expanded = this.ExpandInternal(collapsed);

                    allExpanded.Add(expanded);
                }
            }

            if (allExpanded.Count > 0)
            {
                // Send out change event
                var handler = RegionsExpanded;
                if (handler != null)
                {
                    handler(this, new RegionsExpandedEventArgs(allExpanded, removalPending));
                }
            }

            return allExpanded;
        }

        public bool Enabled
        {
            get
            {
                return this.isEnabled;
            }
            set
            {
                if (this.isEnabled != value)
                {
                    // Expand all (if disabled)
                    ITextSnapshot snapshot = this.editBuffer.CurrentSnapshot;
                    SnapshotSpan snapshotSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
                    if (!value)
                    {
                        // Expand all regions, since we are going to remove them all
                        this.ExpandAllInternal(/*removalPending =*/ true, snapshotSpan, ((collapsed) => true));
                    }

                    // Update internal isEnabled flag after expanding all but before raising RegionsChanged event
                    this.isEnabled = value;

                    // Raise RegionsChanged event for whole buffer (Before disable event)
                    EventHandler<RegionsChangedEventArgs> regionsChanged = RegionsChanged;
                    if (regionsChanged != null && !value)
                    {
                        regionsChanged(this, new RegionsChangedEventArgs(snapshotSpan));
                    }

                    // Raise OutliningEnabledChanged event
                    EventHandler<OutliningEnabledEventArgs> outliningEnabledChanged = OutliningEnabledChanged;
                    if (outliningEnabledChanged != null)
                    {
                        outliningEnabledChanged(this, new OutliningEnabledEventArgs(this.isEnabled));
                    }

                    // Raise RegionsChanged event for whole buffer (After enable event)
                    if (regionsChanged != null && value)
                    {
                        regionsChanged(this, new RegionsChangedEventArgs(snapshotSpan));
                    }
                }
            }
        }


        #region Private helpers

        private SortedList<Collapsible, object> CollapsiblesFromTags(IEnumerable<IMappingTagSpan<IOutliningRegionTag>> tagSpans)
        {
            ITextSnapshot current = this.editBuffer.CurrentSnapshot;

            SortedList<Collapsible, object> collapsibles
                = new SortedList<Collapsible, object>(new CollapsibleSorter(editBuffer));

            foreach (var tagSpan in tagSpans)
            {
                var spans = tagSpan.Span.GetSpans(current);

                // We only accept this tag if it hasn't been split into multiple spans and if
                // it hasn't had pieces cut out of it from projection.  Also, refuse 0-length
                // tags, as they wouldn't be hiding anything.
                if (spans.Count == 1 &&
                    spans[0].Length > 0 &&
                    spans[0].Length == tagSpan.Span.GetSpans(tagSpan.Span.AnchorBuffer)[0].Length)
                {
                    ITrackingSpan trackingSpan = current.CreateTrackingSpan(spans[0], SpanTrackingMode.EdgeExclusive);
                    var collapsible = new Collapsible(trackingSpan, tagSpan.Tag);
                    if (collapsibles.ContainsKey(collapsible))
                    {
                        // TODO: Notify providers somehow.
                        //       Or rewrite so that such things are legal.
                        Debug.WriteLine("IGNORING TAG " + spans[0] + " due to span conflict");
                    }
                    else
                    {
                        collapsibles.Add(collapsible, null);
                    }
                }
                else
                {
                    Debug.WriteLine("IGNORING TAG " + tagSpan.Span.GetSpans(editBuffer) + " because it was split or shortened by projection");
                }
            }

            return collapsibles;
        }

        private IEnumerable<ICollapsible> MergeRegions(IEnumerable<ICollapsed> currentCollapsed, IEnumerable<ICollapsible> newCollapsibles,
                                                      out IEnumerable<ICollapsed> removedRegions)
        {
            List<ICollapsed> toRemove = new List<ICollapsed>();

            List<ICollapsed> oldRegions = new List<ICollapsed>(currentCollapsed);
            List<ICollapsible> newRegions = new List<ICollapsible>(newCollapsibles);

            List<ICollapsible> merged = new List<ICollapsible>(oldRegions.Count + newRegions.Count);

            int oldIndex = 0;
            int newIndex = 0;

            CollapsibleSorter sorter = new CollapsibleSorter(this.editBuffer);

            while (oldIndex < oldRegions.Count || newIndex < newRegions.Count)
            {
                if (oldIndex < oldRegions.Count && newIndex < newRegions.Count)
                {
                    Collapsed oldRegion = oldRegions[oldIndex] as Collapsed;
                    ICollapsible newRegion = newRegions[newIndex];

                    int compareVal = sorter.Compare(oldRegion, newRegion);

                    // Same region
                    if (compareVal == 0)
                    {
                        // might be the same region, but content could be new
                        oldRegion.Tag = newRegion.Tag;
                        merged.Add(oldRegion);

                        oldIndex++;
                        newIndex++;
                    }
                    // old region comes first
                    else if (compareVal < 0)
                    {
                        toRemove.Add(oldRegion);
                        oldIndex++;
                    }
                    // new region comes first
                    else if (compareVal > 0)
                    {
                        merged.Add(newRegion);
                        newIndex++;
                    }
                }
                else if (oldIndex < oldRegions.Count)
                {
                    toRemove.AddRange(oldRegions.GetRange(oldIndex, oldRegions.Count - oldIndex));
                    break;
                }
                else if (newIndex < newRegions.Count)
                {
                    merged.AddRange(newRegions.GetRange(newIndex, newRegions.Count - newIndex));
                    break;
                }
            }

            removedRegions = toRemove;

            return merged;
        }

        #endregion

        #region Getting collapsibles

        public IEnumerable<ICollapsed> GetCollapsedRegions(SnapshotSpan span)
        {
            return GetCollapsedRegionsInternal(new NormalizedSnapshotSpanCollection(span), exposedRegionsOnly: false);
        }

        public IEnumerable<ICollapsed> GetCollapsedRegions(SnapshotSpan span, bool exposedRegionsOnly)
        {
            EnsureValid(span);

            return GetCollapsedRegionsInternal(new NormalizedSnapshotSpanCollection(span), exposedRegionsOnly); 
        }

        public IEnumerable<ICollapsed> GetCollapsedRegions(NormalizedSnapshotSpanCollection spans)
        {
            return GetCollapsedRegionsInternal(spans, exposedRegionsOnly: false);
        }

        public IEnumerable<ICollapsed> GetCollapsedRegions(NormalizedSnapshotSpanCollection spans, bool exposedRegionsOnly)
        {
            return GetCollapsedRegionsInternal(spans, exposedRegionsOnly);
        }

        internal IList<Collapsed> GetCollapsedRegionsInternal(NormalizedSnapshotSpanCollection spans, bool exposedRegionsOnly)
        {
            EnsureValid(spans);

            // No collapsed if disabled
            if (!isEnabled)
            {
                return new List<Collapsed>();
            }

            if (exposedRegionsOnly)
                return collapsedRegionTree.FindTopLevelNodesIntersecting(spans).Select(node => node.Item).ToList();
            else
                return collapsedRegionTree.FindNodesIntersecting(spans).Select(node => node.Item).ToList();
        }

        public IEnumerable<ICollapsible> GetAllRegions(SnapshotSpan span)
        {
            return GetAllRegions(span, exposedRegionsOnly: false);
        }

        public IEnumerable<ICollapsible> GetAllRegions(SnapshotSpan span, bool exposedRegionsOnly)
        {
            EnsureValid(span);

            return GetAllRegions(new NormalizedSnapshotSpanCollection(span), exposedRegionsOnly);
        }

        public IEnumerable<ICollapsible> GetAllRegions(NormalizedSnapshotSpanCollection spans)
        {
            return GetAllRegions(spans, exposedRegionsOnly: false);
        }

        public IEnumerable<ICollapsible> GetAllRegions(NormalizedSnapshotSpanCollection spans, bool exposedRegionsOnly)
        {
            return InternalGetAllRegions(spans, exposedRegionsOnly);
        }

        internal IEnumerable<ICollapsible> InternalGetAllRegions(NormalizedSnapshotSpanCollection spans, bool exposedRegionsOnly, CancellationToken? cancel = null)
        {
            EnsureValid(spans);

            // No collapsibles if disabled
            if (!isEnabled || spans.Count == 0)
            {
                return new List<Collapsible>();
            }

            ITextSnapshot snapshot = spans[0].Snapshot;

            IList<Collapsed> currentCollapsed = GetCollapsedRegionsInternal(spans, exposedRegionsOnly);

            IEnumerable<ICollapsible> newCollapsibles;
            if (!exposedRegionsOnly || currentCollapsed.Count == 0)
            {
                newCollapsibles = CollapsiblesFromTags(this.InternalGetTags(spans, cancel)).Keys;
            }
            else
            {
                NormalizedSnapshotSpanCollection collapsedRegions = new NormalizedSnapshotSpanCollection(currentCollapsed.Select(c => c.Extent.GetSpan(snapshot)));
                NormalizedSnapshotSpanCollection exposed = NormalizedSnapshotSpanCollection.Difference(spans, collapsedRegions);

                // Ensure there is an empty region on each end
                SnapshotSpan first = spans[0];
                SnapshotSpan last = spans[spans.Count - 1];
                NormalizedSnapshotSpanCollection ends = new NormalizedSnapshotSpanCollection(new SnapshotSpan[] { new SnapshotSpan(first.Start, 0), new SnapshotSpan(last.End, 0) });
                exposed = NormalizedSnapshotSpanCollection.Union(exposed, ends);

                newCollapsibles = CollapsiblesFromTags(this.InternalGetTags(exposed, cancel)).Keys.Where(c => IsRegionExposed(c, snapshot));
            }

            IEnumerable<ICollapsed> removed;

            var merged = MergeRegions(currentCollapsed, newCollapsibles, out removed);

            // NOTE: IF we have misbehaved taggers, it is possible that we'll see invalid
            // changes here in removed regions that are currently collapsed.  We can deal 
            // with this by expanding as needed, but it will cause an event to be sent out, which will
            // likely be unexpected and cause bugs in our clients.

            // There are a few ways we can deal with this:

            // #1: Expand/collapse regions and event
            foreach (var removedRegion in removed)
            {
                Debug.Fail("Removing a region here means a tagger has misbehaved.");
                if (removedRegion.IsCollapsed)
                    Expand(removedRegion);
            }

            // Other options:
            // #2: Return the new regions without doing anything special
            // #3: Return the current collapsed + uncollapsed added regions

            return merged;
        }

        private IEnumerable<IMappingTagSpan<IOutliningRegionTag>> InternalGetTags(NormalizedSnapshotSpanCollection spans, CancellationToken? cancel)
        {
            if (cancel.HasValue)
            {
                return this.tagAggregator.GetAllTags(spans, cancel.Value);
            }

            return this.tagAggregator.GetTags(spans);
        }

        bool IsRegionExposed(ICollapsible region, ITextSnapshot current)
        {
            var regionSpan = region.Extent.GetSpan(current);

            // Filter out regions that don't have both end points exposed.
            return !collapsedRegionTree.IsPointContainedInANode(regionSpan.Start) &&
                   !collapsedRegionTree.IsPointContainedInANode(regionSpan.End);
        }

        #endregion

        #region IAccurateOutliningManager methods
        public IEnumerable<ICollapsed> CollapseAll(SnapshotSpan span, Predicate<ICollapsible> match, CancellationToken cancel)
        {
            return this.InternalCollapseAll(span, match, cancel: cancel);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                this.editBuffer.Changed -= this.SourceTextChanged;
                this.tagAggregator.BatchedTagsChanged -= this.OutliningRegionTagsChanged;
                this.tagAggregator.Dispose();
            }
        }

        private void EnsureValid()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("OutliningManager");
            }
        }

        private void EnsureValid(NormalizedSnapshotSpanCollection spans)
        {
            EnsureValid();

            if (spans == null)
            {
                throw new ArgumentNullException("spans");
            }

            if (spans.Count == 0)
            {
                throw new ArgumentException("The given span collection is empty.", "spans");
            }

            if (spans[0].Snapshot.TextBuffer != this.editBuffer)
            {
                throw new ArgumentException("The given span collection is on an invalid buffer." +
                                            "Spans must be generated against the view model's edit buffer",
                                            "spans");
            }
        }

        private void EnsureValid(SnapshotSpan span)
        {
            EnsureValid();

            if (span.Snapshot == null)
            {
                throw new ArgumentException("The given span is uninitialized.");
            }

            if (span.Snapshot.TextBuffer != this.editBuffer)
            {
                throw new ArgumentException("The given span is on an invalid buffer." +
                                            "Spans must be generated against the view model's edit buffer",
                                            "span");
            }
        }

        #endregion
    }

    #region Sorter for sorted lists of collapsibles
    class CollapsibleSorter : IComparer<ICollapsible>
    {
        private ITextBuffer SourceBuffer { get; set; }

        internal CollapsibleSorter(ITextBuffer sourceBuffer)
        {
            SourceBuffer = sourceBuffer;
        }

        public int Compare(ICollapsible x, ICollapsible y)
        {
            if (x == null)
                throw new ArgumentNullException("x");
            if (y == null)
                throw new ArgumentNullException("y");

            ITextSnapshot current = SourceBuffer.CurrentSnapshot;
            SnapshotSpan left = x.Extent.GetSpan(current);
            SnapshotSpan right = y.Extent.GetSpan(current);

            // The "first" collapsible should come first
            if (left.Start != right.Start)
                return left.Start.CompareTo(right.Start);
            // The largest collapsible should come first
            else
                return -left.Length.CompareTo(right.Length);
        }
    }
    #endregion
}
