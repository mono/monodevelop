// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Utilities;

    internal partial class NormalizedTextChangeCollection : ReadOnlyCollection<ITextChange>, INormalizedTextChangeCollection 
    {
        public static INormalizedTextChangeCollection Create(IList<TextChange> changes)
        {
            INormalizedTextChangeCollection result = GetTrivialCollection(changes);
            return result != null ? result : new NormalizedTextChangeCollection(changes);
        }

        public static INormalizedTextChangeCollection Create(IList<TextChange> changes, StringDifferenceOptions? differenceOptions, ITextDifferencingService textDifferencingService,
                                                             ITextSnapshot before = null, ITextSnapshot after = null)
        {
            INormalizedTextChangeCollection result = GetTrivialCollection(changes);
            return result != null ? result : new NormalizedTextChangeCollection(changes, differenceOptions, textDifferencingService, before, after);
        }

        private static TrivialNormalizedTextChangeCollection GetTrivialCollection(IList<TextChange> changes)
        {
            if (changes == null)
            {
                throw new ArgumentNullException("changes");
            }
            if (changes.Count == 1)
            {
                TextChange tc = changes[0];
                if (tc.OldLength + tc.NewLength == 1 && tc.LineBreakBoundaryConditions == LineBreakBoundaryConditions.None &&
                    tc.LineCountDelta == 0)
                {
                    bool isInsertion = tc.NewLength == 1;
                    char data = isInsertion ? tc.NewText[0] : tc.OldText[0];
                    return new TrivialNormalizedTextChangeCollection(data, isInsertion, tc.OldPosition);
                }
            }
            return null;
        }

        /// <summary>
        /// Construct a normalized version of the given TextChange collection,
        /// but don't compute minimal edits.
        /// </summary>
        /// <param name="changes">List of changes to normalize</param>
        private NormalizedTextChangeCollection(IList<TextChange> changes)
            : base(Normalize(changes, null, null, null, null))
        {
        }

        /// <summary>
        /// Construct a normalized version of the given TextChange collection.
        /// </summary>
        /// <param name="changes">List of changes to normalize</param>
        /// <param name="differenceOptions">The difference options to use for minimal differencing, if any.</param>
        /// <param name="textDifferencingService">The difference service to use, if differenceOptions were supplied.</param>
        /// <param name="before">Text snapshot before the change (can be null).</param>
        /// <param name="after">Text snapshot after the change (can be null).</param>
        private NormalizedTextChangeCollection(IList<TextChange> changes, StringDifferenceOptions? differenceOptions, ITextDifferencingService textDifferencingService,
                                               ITextSnapshot before, ITextSnapshot after)
            : base(Normalize(changes, differenceOptions, textDifferencingService, before, after))
        {
        }

        public bool IncludesLineChanges
        {
            get
            {
                foreach (ITextChange change in this)
                {
                    if (change.LineCountDelta != 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Normalize a sequence of changes that were all applied consecutively to the same version of a buffer. Positions of the
        /// normalized changes are adjusted to account for other changes that occur at lower indexes in the
        /// buffer, and changes are sorted and merged if possible.
        /// </summary>
        /// <param name="changes">The changes to normalize.</param>
        /// <param name="differenceOptions">The options to use for minimal differencing, if any.</param>
        /// <param name="before">Text snapshot before the change (can be null).</param>
        /// <param name="after">Text snapshot after the change (can be null).</param>
        /// <returns>A (possibly empty) list of changes, sorted by Position, with adjacent and overlapping changes combined
        /// where possible.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
        private static IList<ITextChange> Normalize(IList<TextChange> changes, StringDifferenceOptions? differenceOptions, ITextDifferencingService textDifferencingService,
                                                    ITextSnapshot before, ITextSnapshot after)
        {
            if (changes.Count == 1 && differenceOptions == null)
            {
                // By far the most common path
                // If we are computing minimal changes, we need to go through the
                // algorithm anyway, since this change may be split up into many
                // smaller changes
                FrugalList<ITextChange> singleResult = new FrugalList<ITextChange>();
                singleResult.Add(changes[0]);
                return singleResult;
            }
            else if (changes.Count == 0)
            {
                return new FrugalList<ITextChange>();
            }

            TextChange[] work = TextUtilities.StableSort(changes, TextChange.Compare);

            // work is now sorted by increasing Position

            int accumulatedDelta = 0;
            int a = 0;
            int b = 1;
            while (b < work.Length)
            {
                // examine a pair of changes and attempt to combine them
                TextChange aChange = work[a];
                TextChange bChange = work[b];
                int gap = bChange.OldPosition - aChange.OldEnd;

                if (gap > 0)
                {
                    // independent changes
                    aChange.NewPosition = aChange.OldPosition + accumulatedDelta;
                    accumulatedDelta += aChange.Delta;
                    a = b;
                    b = a + 1;
                }
                else
                {
                    // dependent changes. merge all adjacent dependent changes into a single change in one pass,
                    // to avoid expensive pairwise concatenations.
                    //
                    // Use StringRebuilders (which allow strings to be concatenated without creating copies of the strings) to assemble the
                    // pieces and then convert the rebuilders to a ReferenceChangeString (which wraps a StringRebuilder) at the end.
                    IStringRebuilder newRebuilder = aChange._newText.Content;
                    IStringRebuilder oldRebuilder = aChange._oldText.Content;

                    int aChangeIncrementalDeletions = 0;
                    do
                    {
                        newRebuilder = newRebuilder.Append(bChange._newText.Content);

                        if (gap == 0)
                        {
                            // abutting deletions
                            oldRebuilder = oldRebuilder.Append(bChange._oldText.Content);
                            aChangeIncrementalDeletions += bChange.OldLength;
                            aChange.LineBreakBoundaryConditions =
                                (aChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) |
                                (bChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.SucceedingNewline);
                        }
                        else
                        {
                            // overlapping deletions
                            if (aChange.OldEnd + aChangeIncrementalDeletions < bChange.OldEnd)
                            {
                                int overlap = aChange.OldEnd + aChangeIncrementalDeletions - bChange.OldPosition;
                                oldRebuilder = oldRebuilder.Append(bChange._oldText.Content.Substring(Span.FromBounds(overlap, bChange._oldText.Length)));
                                aChangeIncrementalDeletions += (bChange.OldLength - overlap);
                                aChange.LineBreakBoundaryConditions =
                                    (aChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) |
                                    (bChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.SucceedingNewline);
                            }
                            // else bChange deletion subsumed by aChange deletion
                        }

                        work[b] = null;
                        b++;
                        if (b == work.Length)
                        {
                            break;
                        }
                        bChange = work[b];
                        gap = bChange.OldPosition - aChange.OldEnd - aChangeIncrementalDeletions;
                    } while (gap <= 0);

                    work[a]._oldText = ReferenceChangeString.CreateChangeString(oldRebuilder);
                    work[a]._newText = ReferenceChangeString.CreateChangeString(newRebuilder);

                    if (b < work.Length)
                    {
                        aChange.NewPosition = aChange.OldPosition + accumulatedDelta;
                        accumulatedDelta += aChange.Delta;
                        a = b;
                        b = a + 1;
                    }
                }
            }
            // a points to the last surviving change
            work[a].NewPosition = work[a].OldPosition + accumulatedDelta;

            List<ITextChange> result = new List<ITextChange>();

            if (differenceOptions.HasValue)
            {
                if (textDifferencingService == null)
                {
                    throw new ArgumentNullException("stringDifferenceUtility");
                }
                foreach (TextChange change in work)
                {
                    if (change == null) continue;

                    // Make sure this is a replacement
                    if (change.OldLength == 0 || change.NewLength == 0)
                    {
                        result.Add(change);
                        continue;
                    }

                    if (change.OldLength >= TextModelOptions.DiffSizeThreshold || 
                        change.NewLength >= TextModelOptions.DiffSizeThreshold)
                    {
                        change.IsOpaque = true;
                        result.Add(change);
                        continue;
                        // too big to even attempt a diff. This is aimed at the reload-a-giant-file scenario
                        // where OOM during diff is a distinct possibility.
                    }

                    // Make sure to turn off IgnoreTrimWhiteSpace, since that doesn't make sense in
                    // the context of a minimal edit
                    StringDifferenceOptions options = new StringDifferenceOptions(differenceOptions.Value);
                    options.IgnoreTrimWhiteSpace = false;
                    IHierarchicalDifferenceCollection diffs;

                    if (before != null && after != null)
                    {
                        // Don't materialize the strings when we know the before and after snapshots. They might be really huge and cause OOM.
                        // We will take this path in the file reload case.
                        diffs = textDifferencingService.DiffSnapshotSpans(new SnapshotSpan(before, change.OldSpan),
                                                                          new SnapshotSpan(after, change.NewSpan), options);
                    }
                    else
                    {
                        // We need to evaluate the old and new text for the differencing service
                        string oldText = change.OldText;
                        string newText = change.NewText;

                        if (oldText == newText)
                        {
                            // This change simply evaporates. This case occurs frequently in Venus and it is much
                            // better to short circuit it here than to fire up the differencing engine.
                            continue;
                        }
                        diffs = textDifferencingService.DiffStrings(oldText, newText, options);
                    }
                    
                    // Keep track of deltas for the "new" position, for sanity check
                    int delta = 0;

                    // Add all the changes from the difference collection
                    result.AddRange(GetChangesFromDifferenceCollection(ref delta, change, change._oldText, change._newText, diffs));

                    // Sanity check
                    // If delta != 0, then we've constructed asymmetrical insertions and
                    // deletions, which should be impossible
                    Debug.Assert(delta == change.Delta, "Minimal edit delta should be equal to replaced text change's delta.");
                }
            }
            // If we aren't computing minimal changes, then copy over the non-null changes
            else
            {
                foreach (TextChange change in work)
                {
                    if (change != null)
                        result.Add(change);
                }
            }

            return result;
        }

        private static IList<TextChange> GetChangesFromDifferenceCollection(ref int delta,
                                                                            TextChange originalChange,
                                                                            ChangeString oldText,
                                                                            ChangeString newText,
                                                                            IHierarchicalDifferenceCollection diffCollection,
                                                                            int leftOffset = 0,
                                                                            int rightOffset = 0)
        {
            List<TextChange> changes = new List<TextChange>();

            for (int i = 0; i < diffCollection.Differences.Count; i++)
            {
                Difference currentDiff = diffCollection.Differences[i];

                Span leftDiffSpan = Translate(diffCollection.LeftDecomposition.GetSpanInOriginal(currentDiff.Left), leftOffset);
                Span rightDiffSpan = Translate(diffCollection.RightDecomposition.GetSpanInOriginal(currentDiff.Right), rightOffset);

                // TODO: Since this evaluates differences lazily, we should add something here to *not* compute the next
                // level of differences if we think it would be too expensive.
                IHierarchicalDifferenceCollection nextLevelDiffs = diffCollection.GetContainedDifferences(i);

                if (nextLevelDiffs != null)
                {
                    changes.AddRange(GetChangesFromDifferenceCollection(ref delta, originalChange, oldText, newText, nextLevelDiffs, leftDiffSpan.Start, rightDiffSpan.Start));
                }
                else
                {
                    TextChange minimalChange = new TextChange(originalChange.OldPosition + leftDiffSpan.Start,
                                                              oldText.Substring(leftDiffSpan.Start, leftDiffSpan.Length),
                                                              newText.Substring(rightDiffSpan.Start, rightDiffSpan.Length),
                                                              ComputeBoundaryConditions(originalChange, oldText, leftDiffSpan));
                    
                    minimalChange.NewPosition = originalChange.NewPosition + rightDiffSpan.Start;
                    if (minimalChange.OldLength > 0 && minimalChange.NewLength > 0)
                    {
                        minimalChange.IsOpaque = true;
                    }

                    delta += minimalChange.Delta;
                    changes.Add(minimalChange);
                }
            }

            return changes;
        }


        private static LineBreakBoundaryConditions ComputeBoundaryConditions(TextChange outerChange, ChangeString oldText, Span leftSpan)
        {
            LineBreakBoundaryConditions bc = LineBreakBoundaryConditions.None;
            if (leftSpan.Start == 0)
            {
                bc = (outerChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.PrecedingReturn);
            }
            else if (oldText[leftSpan.Start - 1] == '\r')
            {
                bc = LineBreakBoundaryConditions.PrecedingReturn;
            }
            if (leftSpan.End == oldText.Length)
            {
                bc |= (outerChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.SucceedingNewline);
            }
            else if (oldText[leftSpan.End] == '\n')
            {
                bc |= LineBreakBoundaryConditions.SucceedingNewline;
            }
            return bc;
        }

        private static Span Translate(Span span, int amount)
        {
            return new Span(span.Start + amount, span.Length);
        }

        private static bool PriorTo(ITextChange denormalizedChange, ITextChange normalizedChange, int accumulatedDelta, int accumulatedNormalizedDelta)
        {
            // notice that denormalizedChange.OldPosition == denormalizedChange.NewPosition
            if ((denormalizedChange.OldLength != 0) && (normalizedChange.OldLength != 0))
            {
                // both deletions
                return denormalizedChange.OldPosition <= normalizedChange.NewPosition - accumulatedDelta - accumulatedNormalizedDelta;
            }
            else
            {
                return denormalizedChange.OldPosition < normalizedChange.NewPosition - accumulatedDelta - accumulatedNormalizedDelta;
            }
        }

        /// <summary>
        /// Given a set of changes against a particular snapshot, merge in a list of normalized changes that occurred
        /// immediately after those changes (as part of the next snapshot) so that the merged changes refer to the 
        /// earlier snapshot.
        /// </summary>
        /// <param name="normalizedChanges">The list of changes to be merged.</param>
        /// <param name="denormChangesWithSentinel">The list of changes into which to merge.</param>
        public static void Denormalize(INormalizedTextChangeCollection normalizedChanges, List<TextChange> denormChangesWithSentinel)
        {
            // denormalizedChangesWithSentinel contains a list of changes that have been denormalized to the origin snapshot
            // (the New positions in those changes are the same as the Old positions), and also has a sentinel at the end
            // that has int.MaxValue for its position.
            // args.Changes contains a list of changes that are normalized with respect to the most recent snapshot, so we know
            // that they are independent and properly ordered -- thus we can perform a single merge pass against the
            // denormalized changes.

            int rover = 0;
            int accumulatedDelta = 0;
            int accumulatedNormalizedDelta = 0;
            List<ITextChange> normChanges = new List<ITextChange>(normalizedChanges);
            for (int n = 0; n < normChanges.Count; ++n)
            {
                ITextChange normChange = normChanges[n];

                // 1. skip past all denormalized changes that begin prior to the beginning of the current change.

                while (PriorTo(denormChangesWithSentinel[rover], normChange, accumulatedDelta, accumulatedNormalizedDelta))
                {
                    accumulatedDelta += denormChangesWithSentinel[rover++].Delta;
                }

                // 2. normChange will be inserted at [rover], but it may need to be split

                if ((normChange.OldEnd - accumulatedDelta) > denormChangesWithSentinel[rover].OldPosition)
                {
                    // split required. for example, text at 5..10 was deleted in snapshot 1, and then text at 0..10 was deleted
                    // in snapshot 2; the latter turns into two deletions in terms of snapshot 1: 0..5 and 10..15.
                    int deletionSuffix = (normChange.OldEnd - accumulatedDelta) - denormChangesWithSentinel[rover].OldPosition;
                    int deletionPrefix = normChange.OldLength - deletionSuffix;
                    int normDelta = normChange.NewPosition - normChange.OldPosition;
                    denormChangesWithSentinel.Insert
                        (rover, new TextChange(normChange.OldPosition - accumulatedDelta,
                                               normChange.OldText.Substring(0, deletionPrefix),
                                               normChange.NewText,
                                               LineBreakBoundaryConditions.None));
                    accumulatedNormalizedDelta += normDelta;

                    // the second part remains 'normalized' in case it needs to be split again
                    TextChange splitee = new TextChange(normChange.OldPosition + deletionPrefix,
                                                        normChange.OldText.Substring(deletionPrefix, deletionSuffix),
                                                        "",
                                                        LineBreakBoundaryConditions.None);
                    splitee.NewPosition += normDelta;
                    normChanges.Insert(n + 1, splitee);
                }
                else
                {
                    denormChangesWithSentinel.Insert
                        (rover, new TextChange(normChange.OldPosition - accumulatedDelta,
                                               normChange.OldText,
                                               normChange.NewText,
                                               LineBreakBoundaryConditions.None));
                    accumulatedNormalizedDelta += normChange.Delta;
                }

                rover++;
            }
        }
    }
}
