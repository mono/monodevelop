//*****************************************************************************
// LcsDiff.cs
// 
// An implementation of the difference algorithm described in
// "An O(ND) Difference Algorithm and its Variations" by Eugene W. Myers
//
// Copyright (C) 2008 Microsoft Corporation
//*****************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

//*************************************************************************
// The code from this point on is a soure-port of the TFS diff algorithm, to be available
// in cases where we don't have access to the TFS diff assembly (i.e. outside of Visual Studio).
//*************************************************************************

namespace Microsoft.TeamFoundation.Diff.Copy
{
    //*************************************************************************
    /// <summary>
    /// An implementation of the difference algorithm described in
    /// "An O(ND) Difference Algorithm and its Variations" by Eugene W. Myers
    /// </summary>
    //*************************************************************************
    internal class LcsDiff<T> : DiffFinder<T>
    {
        //*************************************************************************
        /// <summary>
        /// Constructs the DiffFinder
        /// </summary>
        //*************************************************************************
        public LcsDiff()
            : base()
        {
            m_forwardHistory = new List<int[]>(MaxDifferencesHistory);
            m_reverseHistory = new List<int[]>(MaxDifferencesHistory);
        }

        //*************************************************************************
        /// <summary>
        /// Disposes resources uses by this DiffFinder
        /// </summary>
        //*************************************************************************
        public override void Dispose()
        {
            base.Dispose();
            if (m_forwardHistory != null)
            {
                m_forwardHistory = null;
            }
            if (m_reverseHistory != null)
            {
                m_reverseHistory = null;
            }
            GC.SuppressFinalize(this);
        }

        //*************************************************************************
        /// <summary>
        /// Computes the differences between the original and modified input 
        /// sequences on the bounded range.
        /// </summary>
        /// <returns>An array of the differences between the two input 
        /// sequences.</returns>
        //*************************************************************************
        protected override IDiffChange[] ComputeDiff(int originalStart, int originalEnd,
                                                     int modifiedStart, int modifiedEnd)
        {
            bool quitEarly;
            IDiffChange[] changes = ComputeDiffRecursive(originalStart, originalEnd, modifiedStart, modifiedEnd, out quitEarly);

            // We have to clean up the computed diff to be more intuitive
            // but it turns out this cannot be done correctly until the entire set
            // of diffs have been computed
            return ShiftChanges(changes);
        }

        //*************************************************************************
        /// <summary>
        /// Private helper method which computes the differences on the bounded range
        /// recursively.
        /// </summary>
        /// <returns>An array of the differences between the two input 
        /// sequences.</returns>
        //*************************************************************************
        private IDiffChange[] ComputeDiffRecursive(int originalStart, int originalEnd,
                                                   int modifiedStart, int modifiedEnd,
                                                   out bool quitEarly)
        {
            quitEarly = false;

            // Find the start of the differences
            while (originalStart <= originalEnd && modifiedStart <= modifiedEnd &&
                ElementsAreEqual(originalStart, modifiedStart))
            {
                originalStart++;
                modifiedStart++;
            }

            // Find the end of the differences
            while (originalEnd >= originalStart && modifiedEnd >= modifiedStart &&
                ElementsAreEqual(originalEnd, modifiedEnd))
            {
                originalEnd--;
                modifiedEnd--;
            }

            // In the special case where we either have all insertions or all deletions or the sequences are identical
            if (originalStart > originalEnd || modifiedStart > modifiedEnd)
            {
                IDiffChange[] changes;

                if (modifiedStart <= modifiedEnd)
                {
                    Debug.Assert(originalStart == originalEnd + 1, "originalStart should only be one more than originalEnd");

                    // All insertions
                    changes = new IDiffChange[1];
                    changes[0] = new DiffChange(originalStart, 0,
                                                modifiedStart,
                                                modifiedEnd - modifiedStart + 1);
                }
                else if (originalStart <= originalEnd)
                {
                    Debug.Assert(modifiedStart == modifiedEnd + 1, "modifiedStart should only be one more than modifiedEnd");

                    // All deletions
                    changes = new IDiffChange[1];
                    changes[0] = new DiffChange(originalStart,
                                                originalEnd - originalStart + 1,
                                                modifiedStart, 0);
                }
                else
                {
                    Debug.Assert(originalStart == originalEnd + 1, "originalStart should only be one more than originalEnd");
                    Debug.Assert(modifiedStart == modifiedEnd + 1, "modifiedStart should only be one more than modifiedEnd");

                    // Identical sequences - No differences
                    changes = new IDiffChange[0];
                }

                return changes;
            }

            // This problem can be solved using the Divide-And-Conquer technique.
            int midOriginal, midModified;
            IDiffChange[] result = ComputeRecursionPoint(originalStart, originalEnd, modifiedStart, modifiedEnd,
                                                         out midOriginal, out midModified, out quitEarly);

            if (result != null)
            {
                // Result is not-null when there was enough memory to compute the changes while
                // searching for the recursion point
                return result;
            }
            else if (!quitEarly)
            {
                // We can break the problem down recursively by finding the changes in the
                // First Half:   (originalStart, modifiedStart) to (midOriginal, midModified)
                // Second Half:  (midOriginal + 1, minModified + 1) to (originalEnd, modifiedEnd)
                // NOTE: ComputeDiff() is inclusive, therefore the second range starts on the next point
                IDiffChange[] leftChanges  = ComputeDiffRecursive(originalStart, midOriginal, modifiedStart, midModified, out quitEarly);
                IDiffChange[] rightChanges = new IDiffChange[0];

                if (!quitEarly)
                {
                    rightChanges = ComputeDiffRecursive(midOriginal + 1, originalEnd, midModified + 1, modifiedEnd, out quitEarly);
                }
                else
                {
                    // We did't have time to finish the first half, so we don't have time to compute this half.
                    // Consider the entire rest of the sequence different.
                    rightChanges = new DiffChange[]
                    {
                        new DiffChange(midOriginal + 1, originalEnd - (midOriginal + 1) + 1,
                                       midModified + 1, modifiedEnd - (midModified + 1) + 1)
                    };
                }

                return ConcatenateChanges(leftChanges, rightChanges);
            }

            // If we hit here, we quit early, and so can't return anything meaningful
            return new DiffChange[]
            {
                new DiffChange(originalStart, originalEnd -originalStart + 1,
                               modifiedStart, modifiedEnd - modifiedStart + 1)
            };
        }

        //*************************************************************************
        /// <summary>
        /// Given the range to compute the diff on, this method finds the point:
        /// (midOriginal, midModified)
        /// that exists in the middle of the LCS of the two sequences and 
        /// is the point at which the LCS problem may be broken down recursively.
        /// This method will try to keep the LCS trace in memory. If the LCS recursion
        /// point is calculated and the full trace is available in memory, then this method
        /// will return the change list.
        /// </summary>
        /// <param name="originalStart">The start bound of the original sequence range</param>
        /// <param name="originalEnd">The end bound of the original sequence range</param>
        /// <param name="modifiedStart">The start bound of the modified sequence range</param>
        /// <param name="modifiedEnd">The end bound of the modified sequence range</param>
        /// <param name="midOriginal">The middle point of the original sequence range</param>
        /// <param name="midModified">The middle point of the modified sequence range</param>
        /// <returns>The diff changes, if available, otherwise null</returns>
        //*************************************************************************
        private IDiffChange[] ComputeRecursionPoint(int originalStart, int originalEnd,
                                                    int modifiedStart, int modifiedEnd,
                                                    out int midOriginal, out int midModified,
                                                    out bool quitEarly)
        {
            int originalIndex, modifiedIndex;
            int diagonalForwardStart = 0, diagonalForwardEnd = 0;
            int diagonalReverseStart = 0, diagonalReverseEnd = 0;
            int numDifferences;

            // To traverse the edit graph and produce the proper LCS, our actual
            // start position is just outside the given boundary
            originalStart--;
            modifiedStart--;

            // We set these up to make the compiler happy, but they will
            // be replaced before we return with the actual recursion point
            midOriginal = 0;
            midModified = 0;

            // Clear out the history
            m_forwardHistory.Clear();
            m_reverseHistory.Clear();

            // Each cell in the two arrays corresponds to a diagonal in the edit graph.
            // The integer value in the cell represents the originalIndex of the furthest
            // reaching point found so far that ends in that diagonal.
            // The modifiedIndex can be computed mathematically from the originalIndex and the diagonal number.
            int maxDifferences = (originalEnd - originalStart) + (modifiedEnd - modifiedStart);
            int numDiagonals = maxDifferences + 1;
            int[] forwardPoints = new int[numDiagonals];
            int[] reversePoints = new int[numDiagonals];
            // diagonalForwardBase: Index into forwardPoints of the diagonal which passes through (originalStart, modifiedStart)
            // diagonalReverseBase: Index into reversePoints of the diagonal which passes through (originalEnd, modifiedEnd)
            int diagonalForwardBase = (modifiedEnd - modifiedStart);
            int diagonalReverseBase = (originalEnd - originalStart);
            // diagonalForwardOffset: Geometric offset which allows modifiedIndex to be computed from originalIndex and the
            //    diagonal number (relative to diagonalForwardBase)
            // diagonalReverseOffset: Geometric offset which allows modifiedIndex to be computed from originalIndex and the
            //    diagonal number (relative to diagonalReverseBase)
            int diagonalForwardOffset = (originalStart - modifiedStart);
            int diagonalReverseOffset = (originalEnd - modifiedEnd);

            // delta: The difference between the end diagonal and the start diagonal. This is used to relate diagonal numbers
            //   relative to the start diagonal with diagonal numbers relative to the end diagonal.
            // The Even/Oddn-ness of this delta is important for determining when we should check for overlap
            int delta = diagonalReverseBase - diagonalForwardBase;
            bool deltaIsEven = (delta % 2 == 0);

            // Here we set up the start and end points as the furthest points found so far
            // in both the forward and reverse directions, respectively
            forwardPoints[diagonalForwardBase] = originalStart;
            reversePoints[diagonalReverseBase] = originalEnd;

            // Remember if we quit early, and thus need to do a best-effort result instead of a real result.
            quitEarly = false;

            // A couple of points:
            // --With this method, we iterate on the number of differences between the two sequences.
            //   The more differences there actually are, the longer this will take.
            // --Also, as the number of differences increases, we have to search on diagonals further
            //   away from the reference diagonal (which is diagonalForwardBase for forward, diagonalReverseBase for reverse).
            // --We extend on even diagonals (relative to the reference diagonal) only when numDifferences
            //   is even and odd diagonals only when numDifferences is odd.
            for (numDifferences = 1; numDifferences <= (maxDifferences / 2) + 1; numDifferences++)
            {
                int furthestOriginalIndex = 0;
                int furthestModifiedIndex = 0;

                // Run the algorithm in the forward direction
                diagonalForwardStart = ClipDiagonalBound(diagonalForwardBase - numDifferences,
                                                         numDifferences,
                                                         diagonalForwardBase,
                                                         numDiagonals);
                diagonalForwardEnd = ClipDiagonalBound(diagonalForwardBase + numDifferences,
                                                         numDifferences,
                                                         diagonalForwardBase,
                                                         numDiagonals);
                for (int diagonal = diagonalForwardStart; diagonal <= diagonalForwardEnd; diagonal += 2)
                {
                    // STEP 1: We extend the furthest reaching point in the present diagonal
                    // by looking at the diagonals above and below and picking the one whose point
                    // is further away from the start point (originalStart, modifiedStart)
                    if (diagonal == diagonalForwardStart || (diagonal < diagonalForwardEnd &&
                        forwardPoints[diagonal - 1] < forwardPoints[diagonal + 1]))
                    {
                        originalIndex = forwardPoints[diagonal + 1];
                    }
                    else
                    {
                        originalIndex = forwardPoints[diagonal - 1] + 1;
                    }
                    modifiedIndex = originalIndex - (diagonal - diagonalForwardBase) - diagonalForwardOffset;

                    // Save the current originalIndex so we can test for false overlap in step 3
                    int tempOriginalIndex = originalIndex;

                    // STEP 2: We can continue to extend the furthest reaching point in the present diagonal
                    // so long as the elements are equal.
                    while (originalIndex < originalEnd && modifiedIndex < modifiedEnd
                        && ElementsAreEqual(originalIndex + 1, modifiedIndex + 1))
                    {
                        originalIndex++;
                        modifiedIndex++;
                    }
                    forwardPoints[diagonal] = originalIndex;

                    if (originalIndex + modifiedIndex > furthestOriginalIndex + furthestModifiedIndex)
                    {
                        furthestOriginalIndex = originalIndex;
                        furthestModifiedIndex = modifiedIndex;
                    }

                    // STEP 3: If delta is odd (overlap first happens on forward when delta is odd)
                    // and diagonal is in the range of reverse diagonals computed for numDifferences-1
                    // (the previous iteration; we havent computed reverse diagonals for numDifferences yet)
                    // then check for overlap.
                    if (!deltaIsEven && Math.Abs(diagonal - diagonalReverseBase) <= (numDifferences - 1))
                    {
                        if (originalIndex >= reversePoints[diagonal])
                        {
                            midOriginal = originalIndex;
                            midModified = modifiedIndex;

                            if (tempOriginalIndex <= reversePoints[diagonal]
                                && MaxDifferencesHistory > 0 && numDifferences <= (MaxDifferencesHistory + 1))
                            {
                                // BINGO! We overlapped, and we have the full trace in memory!
                                goto WALKTRACE;
                            }
                            else
                            {
                                // Either false overlap, or we didn't have enough memory for the full trace
                                // Just return the recursion point
                                return null;
                            }
                        }
                    }
                }

                // Check to see if we should be quitting early, before moving on to the next iteration.
                int matchLengthOfLongest =
                    ((furthestOriginalIndex - originalStart) + (furthestModifiedIndex - modifiedStart) - numDifferences) / 2;

                if (ContinueDifferencePredicate != null &&
                    !ContinueDifferencePredicate(furthestOriginalIndex, OriginalSequence, matchLengthOfLongest))
                {
                    // We can't finish, so skip ahead to generating a result from what we have.
                    quitEarly = true;

                    // Use the furthest distance we got in the forward direction.
                    midOriginal = furthestOriginalIndex;
                    midModified = furthestModifiedIndex;

                    if (matchLengthOfLongest > 0 && MaxDifferencesHistory > 0 && numDifferences <= (MaxDifferencesHistory + 1))
                    {
                        // Enough of the history is in memory to walk it backwards
                        goto WALKTRACE;
                    }
                    else
                    {
                        // We didn't actually remember enough of the history.

                        //Since we are quiting the diff early, we need to shift back the originalStart and modified start
                        //back into the boundary limits since we decremented their value above beyond the boundary limit.
                        originalStart++;
                        modifiedStart++;

                        return new DiffChange[]
                        {
                            new DiffChange(originalStart, originalEnd - originalStart + 1,
                                           modifiedStart, modifiedEnd - modifiedStart + 1)
                        };
                    }
                }

                // Run the algorithm in the reverse direction
                diagonalReverseStart = ClipDiagonalBound(diagonalReverseBase - numDifferences,
                                                         numDifferences,
                                                         diagonalReverseBase,
                                                         numDiagonals);
                diagonalReverseEnd = ClipDiagonalBound(diagonalReverseBase + numDifferences,
                                                         numDifferences,
                                                         diagonalReverseBase,
                                                         numDiagonals);
                for (int diagonal = diagonalReverseStart; diagonal <= diagonalReverseEnd; diagonal += 2)
                {
                    // STEP 1: We extend the furthest reaching point in the present diagonal
                    // by looking at the diagonals above and below and picking the one whose point
                    // is further away from the start point (originalEnd, modifiedEnd)
                    if (diagonal == diagonalReverseStart || (diagonal < diagonalReverseEnd &&
                        reversePoints[diagonal - 1] >= reversePoints[diagonal + 1]))
                    {
                        originalIndex = reversePoints[diagonal + 1] - 1;
                    }
                    else
                    {
                        originalIndex = reversePoints[diagonal - 1];
                    }
                    modifiedIndex = originalIndex - (diagonal - diagonalReverseBase) - diagonalReverseOffset;

                    // Save the current originalIndex so we can test for false overlap
                    int tempOriginalIndex = originalIndex;

                    // STEP 2: We can continue to extend the furthest reaching point in the present diagonal
                    // as long as the elements are equal.
                    while (originalIndex > originalStart && modifiedIndex > modifiedStart
                        && ElementsAreEqual(originalIndex, modifiedIndex))
                    {
                        originalIndex--;
                        modifiedIndex--;
                    }
                    reversePoints[diagonal] = originalIndex;

                    // STEP 4: If delta is even (overlap first happens on reverse when delta is even)
                    // and diagonal is in the range of forward diagonals computed for numDifferences
                    // then check for overlap.
                    if (deltaIsEven && Math.Abs(diagonal - diagonalForwardBase) <= numDifferences)
                    {
                        if (originalIndex <= forwardPoints[diagonal])
                        {
                            midOriginal = originalIndex;
                            midModified = modifiedIndex;

                            if (tempOriginalIndex >= forwardPoints[diagonal]
                                && MaxDifferencesHistory > 0 && numDifferences <= (MaxDifferencesHistory + 1))
                            {
                                // BINGO! We overlapped, and we have the full trace in memory!
                                goto WALKTRACE;
                            }
                            else
                            {
                                // Either false overlap, or we didn't have enough memory for the full trace
                                // Just return the recursion point
                                return null;
                            }
                        }
                    }
                }

                // Save current vectors to history before the next iteration
                if (numDifferences <= MaxDifferencesHistory)
                {
                    // We are allocating space for one extra int, which we fill with
                    // the index of the diagonal base index
                    int[] temp = new int[diagonalForwardEnd - diagonalForwardStart + 2];
                    temp[0] = diagonalForwardBase - diagonalForwardStart + 1;
                    Array.Copy(forwardPoints, diagonalForwardStart, temp, 1, diagonalForwardEnd - diagonalForwardStart + 1);
                    m_forwardHistory.Add(temp);

                    temp = new int[diagonalReverseEnd - diagonalReverseStart + 2];
                    temp[0] = diagonalReverseBase - diagonalReverseStart + 1;
                    Array.Copy(reversePoints, diagonalReverseStart, temp, 1, diagonalReverseEnd - diagonalReverseStart + 1);
                    m_reverseHistory.Add(temp);
                }
            }

            // If we got here, then we have the full trace in history. We just have to convert it to a change list
        // NOTE: This part is a bit messy
        WALKTRACE: IDiffChange[] forwardChanges, reverseChanges;

            // First, walk backward through the forward diagonals history
            using (DiffChangeHelper changeHelper = new DiffChangeHelper())
            {
                int diagonalMin = diagonalForwardStart;
                int diagonalMax = diagonalForwardEnd;
                int diagonalRelative = (midOriginal - midModified) - diagonalForwardOffset;
                int lastOriginalIndex = Int32.MinValue;
                int historyIndex = m_forwardHistory.Count - 1;

                do
                {
                    // Get the diagonal index from the relative diagonal number
                    int diagonal = diagonalRelative + diagonalForwardBase;

                    // Figure out where we came from
                    if (diagonal == diagonalMin || (diagonal < diagonalMax &&
                        forwardPoints[diagonal - 1] < forwardPoints[diagonal + 1]))
                    {
                        // Vertical line (the element is an insert)
                        originalIndex = forwardPoints[diagonal + 1];
                        modifiedIndex = originalIndex - diagonalRelative - diagonalForwardOffset;
                        if (originalIndex < lastOriginalIndex)
                        {
                            changeHelper.MarkNextChange();
                        }
                        lastOriginalIndex = originalIndex;
                        changeHelper.AddModifiedElement(originalIndex + 1, modifiedIndex);
                        diagonalRelative = (diagonal + 1) - diagonalForwardBase; //Setup for the next iteration
                    }
                    else
                    {
                        // Horizontal line (the element is a deletion)
                        originalIndex = forwardPoints[diagonal - 1] + 1;
                        modifiedIndex = originalIndex - diagonalRelative - diagonalForwardOffset;
                        if (originalIndex < lastOriginalIndex)
                        {
                            changeHelper.MarkNextChange();
                        }
                        lastOriginalIndex = originalIndex - 1;
                        changeHelper.AddOriginalElement(originalIndex, modifiedIndex + 1);
                        diagonalRelative = (diagonal - 1) - diagonalForwardBase; //Setup for the next iteration
                    }

                    if (historyIndex >= 0)
                    {
                        forwardPoints = m_forwardHistory[historyIndex];
                        diagonalForwardBase = forwardPoints[0]; //We stored this in the first spot
                        diagonalMin = 1;
                        diagonalMax = forwardPoints.Length - 1;
                    }
                } while (--historyIndex >= -1);

                // Ironically, we get the forward changes as the reverse of the
                // order we added them since we technically added them backwards
                forwardChanges = changeHelper.ReverseChanges;
            }

            if (quitEarly)
            {
                // Since we did quit early, assume everything after the midOriginal/midModified point is a diff

                int originalStartPoint = midOriginal + 1;
                int modifiedStartPoint = midModified + 1;

                if (forwardChanges != null && forwardChanges.Length > 0)
                {
                    IDiffChange lastForwardChange = forwardChanges[forwardChanges.Length - 1];
                    originalStartPoint = Math.Max(originalStartPoint, lastForwardChange.OriginalEnd);
                    modifiedStartPoint = Math.Max(modifiedStartPoint, lastForwardChange.ModifiedEnd);
                }

                reverseChanges = new DiffChange[]
                {
                    new DiffChange(originalStartPoint, originalEnd - originalStartPoint + 1,
                                   modifiedStartPoint, modifiedEnd - modifiedStartPoint + 1)
                };
            }
            else
            {
                // Now walk backward through the reverse diagonals history
                using (DiffChangeHelper changeHelper = new DiffChangeHelper())
                {
                    int diagonalMin = diagonalReverseStart;
                    int diagonalMax = diagonalReverseEnd;
                    int diagonalRelative = (midOriginal - midModified) - diagonalReverseOffset;
                    int lastOriginalIndex = Int32.MaxValue;
                    int historyIndex = (deltaIsEven) ? m_reverseHistory.Count - 1
                                                     : m_reverseHistory.Count - 2;

                    do
                    {
                        // Get the diagonal index from the relative diagonal number
                        int diagonal = diagonalRelative + diagonalReverseBase;

                        // Figure out where we came from
                        if (diagonal == diagonalMin || (diagonal < diagonalMax &&
                            reversePoints[diagonal - 1] >= reversePoints[diagonal + 1]))
                        {
                            // Horizontal line (the element is a deletion))
                            originalIndex = reversePoints[diagonal + 1] - 1;
                            modifiedIndex = originalIndex - diagonalRelative - diagonalReverseOffset;
                            if (originalIndex > lastOriginalIndex)
                            {
                                changeHelper.MarkNextChange();
                            }
                            lastOriginalIndex = originalIndex + 1;
                            changeHelper.AddOriginalElement(originalIndex + 1, modifiedIndex + 1);
                            diagonalRelative = (diagonal + 1) - diagonalReverseBase; //Setup for the next iteration
                        }
                        else
                        {
                            // Vertical line (the element is an insertion)
                            originalIndex = reversePoints[diagonal - 1];
                            modifiedIndex = originalIndex - diagonalRelative - diagonalReverseOffset;
                            if (originalIndex > lastOriginalIndex)
                            {
                                changeHelper.MarkNextChange();
                            }
                            lastOriginalIndex = originalIndex;
                            changeHelper.AddModifiedElement(originalIndex + 1, modifiedIndex + 1);
                            diagonalRelative = (diagonal - 1) - diagonalReverseBase; //Setup for the next iteration
                        }

                        if (historyIndex >= 0)
                        {
                            reversePoints = m_reverseHistory[historyIndex];
                            diagonalReverseBase = reversePoints[0]; //We stored this in the first spot
                            diagonalMin = 1;
                            diagonalMax = reversePoints.Length - 1;
                        }
                    } while (--historyIndex >= -1);

                    // There are cases where the reverse history will find diffs that
                    // are correct, but not intuitive, so we need shift them.
                    reverseChanges = changeHelper.Changes;
                }
            }

            return ConcatenateChanges(forwardChanges, reverseChanges);
        }

        //*********************************************************************
        /// <summary>
        /// Shifts the given changes to provide a more intuitive diff.
        /// While the first element in a diff matches the first element after the diff,
        /// we shift the diff down.
        /// </summary>
        /// <param name="changes">The list of changes to shift</param>
        /// <returns>The shifted changes</returns>
        //*********************************************************************
        private IDiffChange[] ShiftChanges(IDiffChange[] changes)
        {
            // Shift all the changes first
            for (int i = 0; i < changes.Length; i++)
            {
                Debug.Assert(changes[i] is DiffChange, "change is not a DiffChange");

                DiffChange change = changes[i] as DiffChange;
                int originalStop = (i < changes.Length - 1) ? changes[i + 1].OriginalStart : OriginalSequence.Count;
                int modifiedStop = (i < changes.Length - 1) ? changes[i + 1].ModifiedStart : ModifiedSequence.Count;
                bool checkOriginal = change.OriginalLength > 0;
                bool checkModified = change.ModifiedLength > 0;

                while (change.OriginalStart + change.OriginalLength < originalStop &&
                       change.ModifiedStart + change.ModifiedLength < modifiedStop &&
                       (!checkOriginal || OriginalElementsAreEqual(change.OriginalStart, change.OriginalStart + change.OriginalLength)) &&
                       (!checkModified || ModifiedElementsAreEqual(change.ModifiedStart, change.ModifiedStart + change.ModifiedLength)))
                {
                    change.OriginalStart++;
                    change.ModifiedStart++;
                }
            }

            // Build up the new list (we have to build a new list because we
            // might have changes we can merge together now)
            List<IDiffChange> result = new List<IDiffChange>(changes.Length);
            IDiffChange mergedChange;
            for (int i = 0; i < changes.Length; i++)
            {
                if (i < changes.Length - 1 && ChangesOverlap(changes[i], changes[i + 1], out mergedChange))
                {
                    result.Add(mergedChange);
                    i++;
                }
                else
                {
                    result.Add(changes[i]);
                }
            }

            return result.ToArray();
        }

        //*************************************************************************
        /// <summary>
        /// Concatentates the two input DiffChange lists and returns the resulting
        /// list.
        /// </summary>
        /// <param name="left">The left changes</param>
        /// <param name="right">The right changes</param>
        /// <returns>The concatenated list</returns>
        //*************************************************************************
        private IDiffChange[] ConcatenateChanges(IDiffChange[] left, IDiffChange[] right)
        {
            IDiffChange mergedChange;

            if (left.Length == 0 || right.Length == 0)
            {
                return (right.Length > 0) ? right : left;
            }
            else if (ChangesOverlap(left[left.Length - 1], right[0], out mergedChange))
            {
                // Since we break the problem down recursively, it is possible that we
                // might recurse in the middle of a change thereby splitting it into
                // two changes. Here in the combining stage, we detect and fuse those
                // changes back together
                IDiffChange[] result = new IDiffChange[left.Length + right.Length - 1];
                Array.Copy(left, 0, result, 0, left.Length - 1);
                result[left.Length - 1] = mergedChange;
                Array.Copy(right, 1, result, left.Length, right.Length - 1);

                return result;
            }
            else
            {
                IDiffChange[] result = new IDiffChange[left.Length + right.Length];
                Array.Copy(left, 0, result, 0, left.Length);
                Array.Copy(right, 0, result, left.Length, right.Length);

                return result;
            }
        }

        //*************************************************************************
        /// <summary>
        /// Returns true if the two changes overlap and can be merged into a single
        /// change
        /// </summary>
        /// <param name="left">The left change</param>
        /// <param name="right">The right change</param>
        /// <param name="mergedChange">The merged change if the two overlap, 
        /// null otherwise</param>
        /// <returns>True if the two changes overlap</returns>
        //*************************************************************************
        private bool ChangesOverlap(IDiffChange left, IDiffChange right, out IDiffChange mergedChange)
        {
            Debug.Assert(left.OriginalStart <= right.OriginalStart, "Left change is not less than or equal to right change");
            Debug.Assert(left.ModifiedStart <= right.ModifiedStart, "Left change is not less than or equal to right change");

            if (left.OriginalStart + left.OriginalLength >= right.OriginalStart
             || left.ModifiedStart + left.ModifiedLength >= right.ModifiedStart)
            {
                int originalStart = left.OriginalStart;
                int originalLength = left.OriginalLength;
                int modifiedStart = left.ModifiedStart;
                int modifiedLength = left.ModifiedLength;

                if (left.OriginalStart + left.OriginalLength >= right.OriginalStart)
                {
                    originalLength = right.OriginalStart + right.OriginalLength - left.OriginalStart;
                }
                if (left.ModifiedStart + left.ModifiedLength >= right.ModifiedStart)
                {
                    modifiedLength = right.ModifiedStart + right.ModifiedLength - left.ModifiedStart;
                }

                mergedChange = new DiffChange(originalStart, originalLength, modifiedStart, modifiedLength);
                return true;
            }
            else
            {
                mergedChange = null;
                return false;
            }
        }

        //*************************************************************************
        /// <summary>
        /// Helper method used to clip a diagonal index to the range of valid
        /// diagonals. This also decides whether or not the diagonal index,
        /// if it exceeds the boundary, should be clipped to the boundary or clipped
        /// one inside the boundary depending on the Even/Odd status of the boundary
        /// and numDifferences.
        /// </summary>
        /// <param name="diagonal">The index of the diagonal to clip.</param>
        /// <param name="numDifferences">The current number of differences being
        /// iterated upon.</param>
        /// <param name="diagonalBaseIndex">The base reference diagonal.</param>
        /// <param name="numDiagonals">The total number of diagonals.</param>
        /// <returns>The clipped diagonal index.</returns>
        //*************************************************************************
        private int ClipDiagonalBound(int diagonal,
                                      int numDifferences,
                                      int diagonalBaseIndex,
                                      int numDiagonals)
        {
            if (diagonal >= 0 && diagonal < numDiagonals)
            {
                // Nothing to clip, its in range
                return diagonal;
            }

            // diagonalsBelow: The number of diagonals below the reference diagonal
            // diagonalsAbove: The number of diagonals above the reference diagonal
            int diagonalsBelow = diagonalBaseIndex;
            int diagonalsAbove = numDiagonals - diagonalBaseIndex - 1;
            bool diffEven = (numDifferences % 2 == 0);

            if (diagonal < 0)
            {
                bool lowerBoundEven = (diagonalsBelow % 2 == 0);
                return (diffEven == lowerBoundEven) ? 0 : 1;
            }
            else
            {
                bool upperBoundEven = (diagonalsAbove % 2 == 0);
                return (diffEven == upperBoundEven) ? numDiagonals - 1 : numDiagonals - 2;
            }
        }

        // Member variables
        private List<int[]> m_forwardHistory;
        private List<int[]> m_reverseHistory;

        // Our total memory usage for storing history is (worst-case):
        // 2 * [(MaxDifferencesHistory + 1) * (MaxDifferencesHistory + 1) - 1] * sizeof(int)
        // 2 * [1448*1448 - 1] * 4 = 16773624 = 16MB
        private const int MaxDifferencesHistory = 1447;
    }
}
