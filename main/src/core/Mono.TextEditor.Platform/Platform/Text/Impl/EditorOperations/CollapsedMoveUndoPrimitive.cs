//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Outlining;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

    /// <summary>
    /// BeforeCollapsedMoveUndoPrimitive is added to the Undo stack before a collapsed region is moved.
    /// Undo operations will cause this primitive to collapse the given regions, returning them
    /// to their original pre-move state. Redo is ignored here and handled in the AfterCollapsedMoveUndoPrimitive.
    /// </summary>
    internal class BeforeCollapsedMoveUndoPrimitive : CollapsedMoveUndoPrimitive
    {
        public BeforeCollapsedMoveUndoPrimitive(IOutliningManager outliningManager, ITextView textView, IEnumerable<Tuple<Span, IOutliningRegionTag>> collapsedSpans)
            : base(outliningManager, textView, collapsedSpans)
        {

        }

        /// <summary>
        /// Redo the action
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be undone.</exception>
        public override void Do()
        {
            if (!CanRedo)
            {
                throw new InvalidOperationException(Strings.CannotRedo);
            }

            // Redo is handled by AfterCollapsedMoveUndoPrimitive and there is nothing for the before to Do here.
            _canUndo = true;
        }

        /// <summary>
        /// Undo the action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be undone.</exception>
        public override void Undo()
        {
            // Validate that we can undo this change
            if (!CanUndo)
            {
                throw new InvalidOperationException(Strings.CannotUndo);
            }

            CollapseRegions();

            _canUndo = false;
        }
    }


    /// <summary>
    /// AfterCollapsedMoveUndoPrimitive is added to the Undo stack after a collapsed region has been moved.
    /// When a Redo occurs this primitive will collapse the regions to return them to their correct state.
    /// Undo operations are ignored here as they are handled in the BeforeCollapsedMoveUndoPrimitive.
    /// </summary>
    internal class AfterCollapsedMoveUndoPrimitive : CollapsedMoveUndoPrimitive
    {
        public AfterCollapsedMoveUndoPrimitive(IOutliningManager outliningManager, ITextView textView, IEnumerable<Tuple<Span, IOutliningRegionTag>> collapsedSpans)
            : base(outliningManager, textView, collapsedSpans)
        {

        }

        /// <summary>
        /// Redo the action
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be undone.</exception>
        public override void Do()
        {
            if (!CanRedo)
            {
                throw new InvalidOperationException(Strings.CannotRedo);
            }

            CollapseRegions();

            _canUndo = true;
        }

        /// <summary>
        /// Undo the action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be undone.</exception>
        public override void Undo()
        {
            // Validate that we can undo this change
            if (!CanUndo)
            {
                throw new InvalidOperationException(Strings.CannotUndo);
            }

            // Undo is handled by the BeforeCollapsedMoveUndoPrimitive and so there is nothing to do here.
            _canUndo = false;
        }
    }

    /// <summary>
    /// CollapsedMoveUndoPrimitive handles re-tagging of regions and collapsing them during moves.
    /// OutliningUndoManager does not handle collapsing moved regions due to the way it listens to collapse/expands
    /// events and then records an Expand undo operation for the collapse. For Move Line operations we need to always collapse. 
    /// Re-tagging of the outlined regions is needed since the tagging it not present on the newly inserted text
    /// in the middle of the undo/redo operation, and without it the region cannot be collapsed.
    /// </summary>
    internal abstract class CollapsedMoveUndoPrimitive : TextUndoPrimitive
    {
        readonly IOutliningManager _outliningManager;
        readonly ITextView _textView;
        readonly IEnumerable<Tuple<Span, IOutliningRegionTag>> _collapsedSpans;
        protected bool _canUndo;

        public CollapsedMoveUndoPrimitive(IOutliningManager outliningManager, ITextView textView, IEnumerable<Tuple<Span, IOutliningRegionTag>> collaspedSpans)
        {
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }

            if (outliningManager == null)
            {
                throw new ArgumentNullException("outliningManager");
            }

            if (collaspedSpans == null)
            {
                throw new ArgumentNullException("collaspedSpans");
            }

            _outliningManager = outliningManager;
            _textView = textView;
            _collapsedSpans = collaspedSpans;

            _canUndo = true;
        }

        // Re-collapse the spans
        public void CollapseRegions()
        {
            if (_outliningManager == null || _textView == null || _collapsedSpans == null)
                return;

            ITextSnapshot snapshot = _textView.TextBuffer.CurrentSnapshot;

            // Get a span that includes all collapsed regions
            int min = Int32.MinValue;
            int max = Int32.MinValue;

            foreach (Tuple<Span, IOutliningRegionTag> span in _collapsedSpans)
            {
                if (min == Int32.MinValue || span.Item1.Start < min)
                {
                    min = span.Item1.Start;
                }

                if (max == Int32.MinValue || span.Item1.End > max)
                {
                    max = span.Item1.End;
                }
            }

            // avoid running if there were no spans
            if (min == Int32.MinValue)
            {
                Debug.Fail("No spans");
                return;
            }

            // span containing all collapsed regions
            SnapshotSpan entireSpan = new SnapshotSpan(snapshot, min, max - min);

            // regions have not yet been tagged by the language service during the undo/redo and 
            // so we need to tag them again in order to do the collapse
            SimpleTagger<IOutliningRegionTag> simpleTagger =
                        _textView.TextBuffer.Properties.GetOrCreateSingletonProperty<SimpleTagger<IOutliningRegionTag>>(() => new SimpleTagger<IOutliningRegionTag>(_textView.TextBuffer));

            Debug.Assert(!simpleTagger.GetTaggedSpans(entireSpan).GetEnumerator().MoveNext(),
                "The code is not expecting the regions to be tagged already. Verify that redundant tagging is not occurring.");

            List<Span> toCollapse = new List<Span>();

            // tag the regions and add them to the list to be bulk collapsed
            foreach (Tuple<Span, IOutliningRegionTag> span in _collapsedSpans)
            {
                ITrackingSpan tspan = snapshot.CreateTrackingSpan(span.Item1, SpanTrackingMode.EdgeExclusive);
                simpleTagger.CreateTagSpan(tspan, span.Item2);

                toCollapse.Add(span.Item1);
            }

            // Disable the OutliningUndoManager to avoid it adding our collapse to the undo stack as an expand
            bool disableOutliningUndo = _textView.Options.IsOutliningUndoEnabled();

            try
            {
                if (disableOutliningUndo)
                {
                    _textView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, false);
                }

                // Do the collapse
                _outliningManager.CollapseAll(entireSpan, colSpan => (!colSpan.IsCollapsed && toCollapse.Contains(colSpan.Extent.GetSpan(snapshot))));
            }
            finally
            {
                if (disableOutliningUndo)
                {
                    _textView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, true);
                }
            }
        }

        public override bool CanUndo
        {
            get { return _canUndo; }
        }

        public override bool CanRedo
        {
            get { return !_canUndo; }
        }
    }
}