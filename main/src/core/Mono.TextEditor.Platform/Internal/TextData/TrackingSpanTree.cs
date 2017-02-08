// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using Microsoft.VisualStudio.Text;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    
    /// <summary>
    /// Holds a well-formed tree of tracking spans and correlated items, for tracking the position and movement
    /// of spans over time.  Allows for efficient addition, removal, and searching over the tree, with methods for
    /// searching for intersection and containment over both <see cref="SnapshotSpan"/> and <see cref="NormalizedSnapshotSpanCollection"/>
    /// arguments.  The search results are returned pre-order (so parents before children, children closest to the start of the buffer first).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Well-formed means the relationship between all spans is either non-overlapping (siblings) or
    /// containment (parent-child), so there can be no partially overlapping spans.
    /// </para>
    /// <para>
    /// The only tracking mode that can be safely used is <see cref="SpanTrackingMode.EdgeExclusive"/>, as the other tracking modes
    /// can result in overlapping spans as the buffer changes.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of object that each span correlates to.</typeparam>
    /// <comment>This is used by the outlining manager to store collapsed regions and the outlining shims to store all hidden region adapters.</comment>
    public sealed class TrackingSpanTree<T>
    {
        public TrackingSpanNode<T> Root { get; private set; }
        public ITextBuffer Buffer { get; private set; }

        public int Count { get; private set; }

        private int advanceVersion = 0;

        /// <summary>
        /// Create a tracking span tree for the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer that all the spans in this tree are in.</param>
        /// <param name="keepTrackingCurrent">The tree should not allow tracking spans to point
        /// to old versions, at the expense of walking the tree on every text change.</param>
        public TrackingSpanTree(ITextBuffer buffer, bool keepTrackingCurrent)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            Buffer = buffer;
            Count = 0;

            // Leave the tracking span parameter as null, since it is
            // never used (assumed to always be the entire buffer)
            Root = new TrackingSpanNode<T>(default(T), null);

            if (keepTrackingCurrent)
            {
                buffer.Changed += OnBufferChanged;
            }
        }

        /// <summary>
        /// Try to add an item to the tree with the given tracking span.
        /// </summary>
        /// <param name="item">The item to add to the tree.</param>
        /// <param name="trackingSpan">The tracking span it is associated with.</param>
        /// <returns>The newly added node, if the item was successfully added; <c>null</c> if adding the item to the tree would
        /// violate the well-formedness of the tree.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="trackingSpan"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If the tracking mode of <paramref name="trackingSpan"/> is not <see cref="SpanTrackingMode.EdgeExclusive"/>.</exception>
        public TrackingSpanNode<T> TryAddItem(T item, ITrackingSpan trackingSpan)
        {
            if (trackingSpan == null)
                throw new ArgumentNullException("trackingSpan");

            if (trackingSpan.TrackingMode != SpanTrackingMode.EdgeExclusive)
                throw new ArgumentException("The tracking mode of the given tracking span must be SpanTrackingMode.EdgeExclusive", "trackingSpan");

            SnapshotSpan spanToAdd = trackingSpan.GetSpan(Buffer.CurrentSnapshot);
            TrackingSpanNode<T> node = new TrackingSpanNode<T>(item, trackingSpan);

            var newNode = TryAddNodeToRoot(node, spanToAdd, Root);
            if (newNode != null)
                Count++;

            return newNode;
        }
        
        /// <summary>
        /// Remove an item from the tree.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="trackingSpan">The span the item was located at.</param>
        /// <returns><c>true</c> if the item was removed, <c>false</c> if it wasn't found.</returns>
        public bool RemoveItem(T item, ITrackingSpan trackingSpan)
        {
            if (trackingSpan == null)
                throw new ArgumentNullException("trackingSpan");

            SnapshotSpan spanToRemove = trackingSpan.GetSpan(Buffer.CurrentSnapshot);

            if (RemoveItemFromRoot(item, spanToRemove, Root))
            {
                Count--;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove all nodes from the tree.
        /// </summary>
        public void Clear()
        {
            Root.Children.Clear();
            Count = 0;
        }

        /// <summary>
        /// Find nodes that intersect the given span.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>Nodes that intersect the given span.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindNodesIntersecting(SnapshotSpan span)
        {
            return FindNodesIntersecting(new NormalizedSnapshotSpanCollection(span));
        }

        /// <summary>
        /// Find nodes that intersect the given collection of spans (nodes that intersect more than one of the
        /// spans are only returned once).
        /// </summary>
        /// <param name="spans">The collection of spans to search.</param>
        /// <returns>Nodes that intersect the given collection of spans.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindNodesIntersecting(NormalizedSnapshotSpanCollection spans)
        {
            return FindNodes(spans, Root, recurse: true, contained: false);
        }

        /// <summary>
        /// Find nodes that intersect the given span that are at the top level of the tree (have no parent nodes).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>Nodes that are toplevel and intersect the given span.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindTopLevelNodesIntersecting(SnapshotSpan span)
        {
            return FindTopLevelNodesIntersecting(new NormalizedSnapshotSpanCollection(span));
        }

        /// <summary>
        /// Find nodes that intersect the given collection of spans that are at the top level of the tree (have no parent nodes).
        /// </summary>
        /// <param name="spans">The collection of spans to search.</param>
        /// <returns>Nodes that are toplevel and intersect the given collection of spans.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindTopLevelNodesIntersecting(NormalizedSnapshotSpanCollection spans)
        {
            return FindNodes(spans, Root, recurse: false, contained: false);
        }

        /// <summary>
        /// Find nodes that are contained completely by the given span.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>Nodes that are contained completely by the given span.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindNodesContainedBy(SnapshotSpan span)
        {
            return FindNodesContainedBy(new NormalizedSnapshotSpanCollection(span));
        }

        /// <summary>
        /// Find nodes that are contained completely by the given collection of spans.
        /// </summary>
        /// <param name="spans">The collection of spans to search.</param>
        /// <returns>Nodes that are contained completely by the given collection of spans.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindNodesContainedBy(NormalizedSnapshotSpanCollection spans)
        {
            return FindNodes(spans, Root, recurse: true, contained: true);
        }

        /// <summary>
        /// Find nodes that are contained completely by the given span that are at the top level of the tree (have no parent nodes).
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>Nodes that are toplevel and are contained completely by the given span.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindTopLevelNodesContainedBy(SnapshotSpan span)
        {
            return FindTopLevelNodesContainedBy(new NormalizedSnapshotSpanCollection(span));
        }

        /// <summary>
        /// Find nodes that are contained completely by the given collection of spans that are at the top level of the tree (have no parent nodes).
        /// </summary>
        /// <param name="spans">The collection of spans to search.</param>
        /// <returns>Nodes that are toplevel and are contained completely by the given collection of spans.</returns>
        public IEnumerable<TrackingSpanNode<T>> FindTopLevelNodesContainedBy(NormalizedSnapshotSpanCollection spans)
        {
            return FindNodes(spans, Root, recurse: false, contained: true);
        }

        /// <summary>
        /// Check if a given point is contained inside of a node (so the point is between the start and end points of a node).
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns><c>true</c> if the poin is inside a node.</returns>
        public bool IsPointContainedInANode(SnapshotPoint point)
        {
            return FindChild(point, Root.Children, left: true).Type == FindResultType.Inner;
        }

        /// <summary>
        /// Check if a given node is a toplevel node (has no parents).
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns><c>true</c> if the node has no parent node.</returns>
        public bool IsNodeTopLevel(TrackingSpanNode<T> node)
        {
            return Root.Children.Contains(node);
        }

        public void Advance(ITextVersion toVersion)
        {
            if (toVersion == null)
            {
                throw new ArgumentNullException("toVersion");
            }

            if (toVersion.VersionNumber > this.advanceVersion)
            {
                this.advanceVersion = toVersion.VersionNumber;
                Root.Advance(toVersion);
            }
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs args)
        {
            // this only runs in stress mode. It introduces synchronous processing on every buffer change
            // to advance all the tracking spans in the tree. This prevents these spans from pinning old
            // versions and obscuring leaks.
            this.Advance(args.After.Version);
        }

        #region Static helpers

        static IEnumerable<TrackingSpanNode<T>> FindNodes(NormalizedSnapshotSpanCollection spans, TrackingSpanNode<T> root, bool recurse = true, bool contained = false)
        {
            if (spans == null || spans.Count == 0 || root.Children.Count == 0)
                yield break;

            int requestIndex = 0;
            SnapshotSpan currentRequest = spans[requestIndex];

            // Find the first child
            FindResult findResult = FindChild(currentRequest.Start, root.Children, left: true);
            int childIndex = findResult.Index;

            if (childIndex >= root.Children.Count)
                yield break;

            ITextSnapshot snapshot = currentRequest.Snapshot;
            SnapshotSpan currentChild = root.Children[childIndex].TrackingSpan.GetSpan(snapshot);

            while (requestIndex < spans.Count && childIndex < root.Children.Count)
            {
                if (currentRequest.Start > currentChild.End)
                {
                    // Find the next child
                    childIndex = FindNextChild(root, currentRequest.Start, childIndex);

                    if (childIndex < root.Children.Count)
                        currentChild = root.Children[childIndex].TrackingSpan.GetSpan(snapshot);
                }
                else if (currentChild.Start > currentRequest.End)
                {
                    // Skip to the next request
                    if (++requestIndex < spans.Count)
                        currentRequest = spans[requestIndex];
                }
                else
                {
                    // Yield the region then move to the next
                    if (!contained || currentRequest.Contains(currentChild))
                        yield return root.Children[childIndex];

                    if (recurse)
                    {
                        foreach (var result in FindNodes(spans, root.Children[childIndex], recurse, contained))
                            yield return result;
                    }

                    // Find the next child
                    childIndex = FindNextChild(root, currentRequest.Start, childIndex);

                    if (childIndex < root.Children.Count)
                        currentChild = root.Children[childIndex].TrackingSpan.GetSpan(snapshot);
                }
            }
        }

        static int FindNextChild(TrackingSpanNode<T> root, SnapshotPoint point, int currentChildIndex)
        {
            // If we're already at the end, there's no need to continue searching
            if (currentChildIndex == root.Children.Count - 1)
                return currentChildIndex + 1;

            return FindChild(point, root.Children, left: true, lo: currentChildIndex + 1).Index;
        }

        static bool RemoveItemFromRoot(T item, SnapshotSpan span, TrackingSpanNode<T> root)
        {
            if (root.Children.Count == 0)
                return false;

            var result = FindChild(span.Start, root.Children, left: true);

            if (result.Index < 0 || result.Index >= root.Children.Count)
                return false;

            // Search from this index onward (there may be empty regions in the way)
            for (int i = result.Index; i < root.Children.Count; i++)
            {
                var child = root.Children[i];
                SnapshotSpan childSpan = child.TrackingSpan.GetSpan(span.Snapshot);

                // Check to see if we've walked past it
                if (childSpan.Start > span.End)
                {
                    return false;
                }
                else if (childSpan == span && object.Equals(child.Item, item))
                {
                    root.Children.RemoveAt(i);
                    root.Children.InsertRange(i, child.Children);
                    return true;
                }
                else if (childSpan.Contains(span))
                {
                    if (RemoveItemFromRoot(item, span, child))
                        return true;
                }
            }

            return false;
        }

        static TrackingSpanNode<T> TryAddNodeToRoot(TrackingSpanNode<T> newNode, SnapshotSpan span, TrackingSpanNode<T> root)
        {
            var children = root.Children;

            if (children.Count == 0)
            {
                children.Add(newNode);
                return newNode;
            }

            FindResult leftResult = FindIndexForAdd(span.Start, children, left: true);
            FindResult rightResult = FindIndexForAdd(span.End, children, left: false);

            // See if we can add the node anywhere

            // The indices cross if the searches fail, and the node is inside a gap.
            if (leftResult.Index > rightResult.Index)
            {
                // Case #1: If the new node should go in a gap between two nodes, just insert it in the correct location
                Debug.Assert(leftResult.Type == FindResultType.Outer || rightResult.Type == FindResultType.Outer);

                children.Insert(leftResult.Index, newNode);
                return newNode;
            }
            else
            {
                if (leftResult.Type == FindResultType.Inner || rightResult.Type == FindResultType.Inner)
                {
                    // Case #2: The new node is contained entirely in a single child node, so add it to that child
                    //  Check the nodes at either end of the resulting indices (they may not be the same index due to
                    //  0-length nodes that abut the correct child).
                    if (children[leftResult.Index].TrackingSpan.GetSpan(span.Snapshot).Contains(span))
                        return TryAddNodeToRoot(newNode, span, children[leftResult.Index]);
                    if (leftResult.Index != rightResult.Index && children[rightResult.Index].TrackingSpan.GetSpan(span.Snapshot).Contains(span))
                        return TryAddNodeToRoot(newNode, span, children[rightResult.Index]);

                    // This fails if the node isn't fully contained in a single child
                }
                else
                {
                    // Case #3: The new node contains any number of children, so we:
                    int start = leftResult.Index;
                    int count = rightResult.Index - leftResult.Index + 1;

                    // a) Add all the children this should contain to the new node,
                    newNode.Children.AddRange(children.Skip(start).Take(count));
                    // b) Remove them from the existing root node, and
                    children.RemoveRange(start, count);
                    // c) Add the new node in their place
                    children.Insert(start, newNode);

                    return newNode;
                }
            }

            // We couldn't find a place to add this node, so return failure
            return null;
        }


        // Find the child that intersects the given point (or child gap, if no nodes intersect).
        // When left is true, finds the left-most child intersecting the point.  Otherwise, finds the right-most child intersecting the point.
        static FindResult FindChild(SnapshotPoint point, List<TrackingSpanNode<T>> nodes, bool left, int lo = -1, int hi = -1)
        {
            if (nodes.Count == 0)
                return new FindResult() { Index = 0, Intersects = false, Type = FindResultType.Outer };

            ITextSnapshot snapshot = point.Snapshot;
            int position = point.Position;

            FindResultType type = FindResultType.Outer;

            bool intersects = false;

            // Binary search for the node containing the position
            if (lo == -1)
                lo = 0;
            if (hi == -1)
                hi = nodes.Count - 1;

            int mid = lo;

            SnapshotSpan midSpan = new SnapshotSpan();
            while (lo <= hi)
            {
                mid = (lo + hi) / 2;

                midSpan = nodes[mid].TrackingSpan.GetSpan(snapshot);

                if (position < midSpan.Start)
                {
                    hi = mid - 1;
                }
                else if (position > midSpan.End)
                {
                    lo = mid + 1;
                }
                else
                {
                    // midSpan contains or abuts the position
                    if (position > midSpan.Start && position < midSpan.End)
                        type = FindResultType.Inner;

                    intersects = true;

                    break;
                }
            }

            int index = mid;
            midSpan = nodes[index].TrackingSpan.GetSpan(snapshot); 

            // If this is an intersection, make sure we walk to the left or right as requested.
            if (intersects)
            {
                if (left)
                {
                    while (index >= lo)
                    {
                        midSpan = nodes[index].TrackingSpan.GetSpan(snapshot);
                        if (position > midSpan.End)
                        {
                            index++;
                            break;
                        }

                        index--;
                    }

                    // If we fell off the end, just return lo
                    if (index < lo)
                        index = lo;
                }
                else
                {
                    while (index <= hi)
                    {
                        midSpan = nodes[index].TrackingSpan.GetSpan(snapshot);
                        if (position < midSpan.Start)
                        {
                            index--;
                            break;
                        }

                        index++;
                    }

                    // If we fell off the end, just return hi
                    if (index > hi)
                        index = hi;
                }
            }

            return new FindResult() { Type = type, Index = index, Intersects = intersects};
        }

        // Find the left/right indices for adding a new node in the scope of the given list of nodes.
        // When left is true (and the point is not inside a node), finds the leftmost node with a start and end point >= the position.
        // When left is false (and the point is not inside a node), finds the rightmost node with a start and end point <= the position.
        static FindResult FindIndexForAdd(SnapshotPoint point, List<TrackingSpanNode<T>> nodes, bool left, int lo = -1, int hi = -1)
        {
            ITextSnapshot snapshot = point.Snapshot;
            int position = point.Position;

            if (lo == -1)
                lo = 0;
            if (hi == -1)
                hi = nodes.Count - 1;

            // Use the general FindIndex to start
            FindResult result = FindChild(point, nodes, left, lo, hi);

            int index = result.Index;
            SnapshotSpan midSpan = nodes[index].TrackingSpan.GetSpan(snapshot); 

            // If we hit a gap, figure out the correct index to use
            if (!result.Intersects)
            {
                // midSpan is the last span we found
                if (position < midSpan.Start && !left)
                    index--;
                else if (position > midSpan.End && left)
                    index++;
            }
            else if (result.Type == FindResultType.Outer)
            {
                // For an outer hit, we need to walk left or right to make sure we're containing all empty regions
                if (left)
                {
                    while (index <= hi)
                    {
                        midSpan = nodes[index].TrackingSpan.GetSpan(snapshot);
                        if (position <= midSpan.Start)
                        {
                            break;
                        }

                        index++;
                    }
                }
                else
                {
                    while (index >= lo)
                    {
                        midSpan = nodes[index].TrackingSpan.GetSpan(snapshot);
                        if (position >= midSpan.End)
                        {
                            break;
                        }

                        index--;
                    }
                }
            }

            return new FindResult() { Type = result.Type, Index = index, Intersects = result.Intersects };
        }

        enum FindResultType
        {
            Inner,      // The result found is inside a node
            Outer       // The result found is outside of nodes
        }

        struct FindResult
        {
            // Inner or outer, depending on where the result is
            public FindResultType Type;
            // The child index of the result
            public int Index;
            // true when the result intersects the given index
            public bool Intersects;
        }
        #endregion
    }

    /// <summary>
    /// A node in the tracking span tree.  A node contains a data item, an associated tracking span,
    /// and a (possibly empty) list of children, which can be modified as items are inserted and removed into the tree.
    /// </summary>
    public sealed class TrackingSpanNode<T>
    {
        public TrackingSpanNode(T item, ITrackingSpan trackingSpan) : this(item, trackingSpan, new List<TrackingSpanNode<T>>()) { }

        public TrackingSpanNode(T item, ITrackingSpan trackingSpan, List<TrackingSpanNode<T>> children)
        {
            Item = item;
            TrackingSpan = trackingSpan;
            Children = children;
        }

        public T Item { get; private set; }
        public ITrackingSpan TrackingSpan { get; private set; }
        public List<TrackingSpanNode<T>> Children { get; private set; }

        internal void Advance(ITextVersion toVersion)
        {
            if (TrackingSpan != null)
                TrackingSpan.GetSpan(toVersion);

            foreach (var child in Children)
            {
                child.Advance(toVersion);
            }
        }
    }
}
