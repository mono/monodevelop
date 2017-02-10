namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Implementation of ITrackingPoint for the UndoRedo and Backward TrackingFidelityMode.
    /// Records noninvertible transition and consults these records as appropriate.
    /// </summary>
    internal class HighFidelityTrackingPoint : TrackingPoint
    {
        #region State and Construction
        private class VersionPositionHistory
        {
            private readonly ITextVersion version;
            private readonly int position;
            private readonly List<VersionNumberPosition> noninvertibleHistory;

            public VersionPositionHistory(ITextVersion version, int position, List<VersionNumberPosition> noninvertibleHistory)
            {
                this.version = version;
                this.position = position;
                this.noninvertibleHistory = noninvertibleHistory;
            }

            public ITextVersion Version { get { return this.version; } }
            public int Position { get { return this.position; } }
            public List<VersionNumberPosition> NoninvertibleHistory { get { return this.noninvertibleHistory; } }
        }

        private VersionPositionHistory cachedPosition;
        private TrackingFidelityMode fidelity;

        internal HighFidelityTrackingPoint(ITextVersion version, int position, PointTrackingMode trackingMode, TrackingFidelityMode fidelity)
            : base(version, position, trackingMode)
        {
            if (fidelity != TrackingFidelityMode.UndoRedo && fidelity != TrackingFidelityMode.Backward)
            {
                throw new ArgumentOutOfRangeException("fidelity");
            }
            List<VersionNumberPosition> initialHistory = null;
            if (fidelity == TrackingFidelityMode.UndoRedo && version.VersionNumber > 0)
            {
                // The system may perform undo operations that reach prior to the initial version; if any of
                // those transitions are noninvertible, then redoing back to the initial version will give the
                // wrong answer. Thus we save the state of the point for the initial version, unless the
                // initial version happens to be version zero (in which case we could not undo past it).

                initialHistory = new List<VersionNumberPosition>();

                if (version.VersionNumber != version.ReiteratedVersionNumber)
                {
                    Debug.Assert(version.ReiteratedVersionNumber < version.VersionNumber);
                    // If the current version and reiterated version differ, also remember the position
                    // using the reiterated version number, since when undoing back to this point it
                    // will be the key that is used.
                    initialHistory.Add(new VersionNumberPosition(version.ReiteratedVersionNumber, position));
                }

                initialHistory.Add(new VersionNumberPosition(version.VersionNumber, position));
            }
            this.cachedPosition = new VersionPositionHistory(version, position, initialHistory);
            this.fidelity = fidelity;
        }
        #endregion

        #region Overrides
        public override ITextBuffer TextBuffer
        {
            get { return this.cachedPosition.Version.TextBuffer; }
        }

        public override TrackingFidelityMode TrackingFidelity
        {
            get { return this.fidelity; }
        }

        protected override int TrackPosition(ITextVersion targetVersion)
        {
            // Compute the new position on the requested snapshot.
            // This object caches the most recently requested version and the position in that version.
            //
            // We are relying on the atomicity of pointer copies (this.cachedPosition might change after we've
            // fetched it below but we will always get a self-consistent VersionPosition). This ensures we
            // have proper behavior when called from multiple threads (multiple threads may all track and update the
            // cached value if called at inconvenient times, but they will return consistent results).
            //
            // In most cases, one can track backwards from the cached version to a previously computed
            // version and get the same result, but this is not always the case: in particular, when the
            // position lies in a deleted region, simulating reinsertion of that region will not cause
            // the previous value of the position to be recovered. Such transitions are called noninvertible.
            // This class explicitly tracks the positions of the point for versions for which the subsequent
            // transition is noninvertible; this allows the value to be computed properly when tracking backwards
            // or in undo/redo situations.

            VersionPositionHistory cached = this.cachedPosition;    // must fetch just once
            if (targetVersion == cached.Version)
            {
                // easy!
                return cached.Position;
            }

            List<VersionNumberPosition> noninvertibleHistory = cached.NoninvertibleHistory;
            int targetPosition;
            if (targetVersion.VersionNumber > cached.Version.VersionNumber)
            {
                // Roll the cached version forward to the requested version.
                targetPosition = TrackPositionForwardInTime
                                        (this.trackingMode,
                                         this.fidelity,
                                         ref noninvertibleHistory,
                                         cached.Position,
                                         cached.Version,
                                         targetVersion);

                // Cache new position
                this.cachedPosition = new VersionPositionHistory(targetVersion, targetPosition, noninvertibleHistory);
            }
            else
            {
                // roll backwards from the cached version
                targetPosition = TrackPositionBackwardInTime
                                        (this.trackingMode,
                                         this.fidelity == TrackingFidelityMode.Backward ? noninvertibleHistory : null,
                                         cached.Position,
                                         cached.Version,
                                         targetVersion);
            }
            return targetPosition;
        }
        #endregion

        internal static int TrackPositionForwardInTime(PointTrackingMode trackingMode,
                                                       TrackingFidelityMode fidelity,
                                                       ref List<VersionNumberPosition> noninvertibleHistory,
                                                       int currentPosition,
                                                       ITextVersion currentVersion,
                                                       ITextVersion targetVersion)
        {
            System.Diagnostics.Debug.Assert(targetVersion.VersionNumber > currentVersion.VersionNumber);

            // track forward in time
            ITextVersion roverVersion = currentVersion;
            while (roverVersion != targetVersion)
            {
                currentVersion = roverVersion;
                roverVersion = currentVersion.Next;

                if (fidelity == TrackingFidelityMode.UndoRedo &&
                    roverVersion.ReiteratedVersionNumber != roverVersion.VersionNumber &&
                    noninvertibleHistory != null)
                {
                    int p = noninvertibleHistory.BinarySearch
                                (new VersionNumberPosition(roverVersion.ReiteratedVersionNumber, 0),
                                 VersionNumberPositionComparer.Instance);
                    if (p >= 0)
                    {
                        currentPosition = noninvertibleHistory[p].Position;
                        continue;
                    }
                }

                int changeCount = currentVersion.Changes.Count;
                if (changeCount == 0)
                {
                    // A version which has no text changes preserves the reiterated version number of the previous version,
                    // so its version number and reiterated version number will be different. We need to record a possibly 
                    // noninvertible change with the reiterated version number as well as with the version number. 
                    Debug.Assert(roverVersion.VersionNumber != roverVersion.ReiteratedVersionNumber);
                    if (roverVersion.VersionNumber != roverVersion.ReiteratedVersionNumber)
                    {
                        RecordNoninvertibleTransition(ref noninvertibleHistory, currentPosition, roverVersion.ReiteratedVersionNumber);
                    }
                    continue;
                }

                int currentVersionStartingPosition = currentPosition;
                for (int c = 0; c < changeCount; ++c)
                {
                    ITextChange textChange = currentVersion.Changes[c];
                    if (IsOpaque(textChange))
                    {
                        if (textChange.NewPosition + textChange.OldLength <= currentPosition)
                        {
                            // point is to the right of the opaque change. shift it.
                            currentPosition += textChange.Delta;
                            continue;
                        }
                        else if (textChange.NewPosition <= currentPosition)
                        {
                            // point is within the opaque change. stay put (but within the change)
                            if (textChange.NewEnd <= currentPosition)
                            {
                                // we shift to the left because the new text is shorter than what
                                // it replaced. this is a noninvertible transition, so remember it.
                                RecordNoninvertibleTransition(ref noninvertibleHistory, currentVersionStartingPosition, currentVersion.VersionNumber);
                                currentPosition = textChange.NewEnd;
                            }
                            // No further change in this version can affect us.
                            break;
                        }
                        else
                        {
                            // point is to the left of the opaque change. no effect, and we are done.
                            break;
                        }
                    }

                    if (trackingMode == PointTrackingMode.Positive)
                    {
                        // Positive tracking mode: point moves with insertions at its location.
                        if (textChange.NewPosition <= currentPosition)
                        {
                            if (textChange.NewPosition + textChange.OldLength <= currentPosition)
                            {
                                // easy: the change is entirely to the left of our position.
                                // if NewPosition + OldLength == currentPosition, it means that the character
                                // immediately before our position was deleted (or replaced). When going back
                                // in time we will see this as an insertion, and since our tracking mode is positive,
                                // we will put ourselves after it.
                                currentPosition += textChange.Delta;
                            }
                            else
                            {
                                // textChange deleted text at the position of the tracked point; now the point is
                                // pushed to the end of the textChange insertion.

                                // This is a noninvertible transition. Remember it.
                                RecordNoninvertibleTransition(ref noninvertibleHistory, currentVersionStartingPosition, currentVersion.VersionNumber);
                                currentPosition = textChange.NewEnd;
                                break;
                            }
                        }
                        else
                        {
                            // the change is entirely to the right of our position; since changes are
                            // sorted, we are done.
                            break;
                        }
                    }
                    else
                    {
                        // Negative tracking mode: point doesn't move with respect to insertions at its location.
                        if (textChange.NewPosition < currentPosition)
                        {
                            if (textChange.NewPosition + textChange.OldLength < currentPosition)
                            {
                                // easy: the change is entirely to the left of our position. Since we
                                // are paying attention to the tracking mode when tracking back (to handle the
                                // before-the-origin-version case), we need to consider NewPosition + OldLength ==
                                // currentPosition case as noninvertible.
                                currentPosition += textChange.Delta;
                            }
                            else
                            {
                                // textChange deleted text at the position of the tracked point; now the point is
                                // at the position of the change, prior to any insertion.

                                // This is a nonivertible transition.
                                RecordNoninvertibleTransition(ref noninvertibleHistory, currentVersionStartingPosition, currentVersion.VersionNumber);
                                currentPosition = textChange.NewPosition;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return currentPosition;
        }

        private static void RecordNoninvertibleTransition(ref List<VersionNumberPosition> noninvertibleHistory, int currentPosition, int versionNumber)
        {
            if (noninvertibleHistory == null)
            {
                noninvertibleHistory = new List<VersionNumberPosition>();
            }
            if (noninvertibleHistory.Count > 0 && noninvertibleHistory[noninvertibleHistory.Count - 1].VersionNumber == versionNumber)
            {
                // We already recorded the version/position, either because it's the first version
                // or because of a reiterated version anomaly due to a version with no text changes
                Debug.Assert(currentPosition == noninvertibleHistory[noninvertibleHistory.Count - 1].Position);
                return;
            }
            noninvertibleHistory.Add(new VersionNumberPosition(versionNumber, currentPosition));

            // this list had better be sorted!
            Debug.Assert((noninvertibleHistory.Count == 1) ||
                         (noninvertibleHistory[noninvertibleHistory.Count - 1].VersionNumber >
                          noninvertibleHistory[noninvertibleHistory.Count - 2].VersionNumber));
        }

        internal static int TrackPositionBackwardInTime(PointTrackingMode trackingMode,
                                                        List<VersionNumberPosition> noninvertibleHistory,
                                                        int currentPosition,
                                                        ITextVersion currentVersion,
                                                        ITextVersion targetVersion)
        {
            System.Diagnostics.Debug.Assert(targetVersion.VersionNumber < currentVersion.VersionNumber);

            ITextVersion[] textVersionsStack = new ITextVersion[currentVersion.VersionNumber - targetVersion.VersionNumber];
            int top = 0;
            {
                ITextVersion roverVersion = targetVersion;
                while (roverVersion != currentVersion)
                {
                    textVersionsStack[top++] = roverVersion;
                    roverVersion = roverVersion.Next;
                }
            }

            while (top > 0)
            {
                ITextVersion textVersion = textVersionsStack[--top];

                if (noninvertibleHistory != null)
                {
                    int p = noninvertibleHistory.BinarySearch
                                (new VersionNumberPosition(textVersion.VersionNumber, 0), VersionNumberPositionComparer.Instance);
                    if (p >= 0)
                    {
                        // the undo or redo change was noninvertible, so we've recorded the state before the change
                        currentPosition = noninvertibleHistory[p].Position;
                        continue;
                    }
                }

                IList<ITextChange> textChanges = textVersion.Changes;
                for (int tc = textChanges.Count - 1; tc >= 0; --tc)
                {
                    ITextChange textChange = textChanges[tc];
                    if (IsOpaque(textChange))
                    {
                        if (textChange.NewEnd <= currentPosition)
                        {
                            // point is to the right of the opaque change. shift it.
                            currentPosition -= textChange.Delta;
                        }
                        else if (textChange.NewPosition <= currentPosition)
                        {
                            // point is within the opaque change. stay put (but within the change)
                            currentPosition = Math.Min(currentPosition, textChange.NewPosition + textChange.OldLength);
                        }
                        continue;
                    }

                    // tracking modes don't matter when going back over ground we've previously traversed,
                    // because we are really reversing a deletion whose behavior had nothing to do with the
                    // tracking mode, and the correct answer is captured in the noninvertible history. However,
                    // when going back in time before the tracking point was created, it makes sense to honor
                    // the tracking mode in the absence of any other information.
                    if (trackingMode == PointTrackingMode.Positive)
                    {
                        // Positive tracking mode: point moves with insertions at its location.
                        if (textChange.NewPosition <= currentPosition)
                        {
                            if (textChange.NewEnd <= currentPosition)
                            {
                                currentPosition -= textChange.Delta;
                            }
                            else
                            {
                                currentPosition = textChange.NewPosition + textChange.OldLength;
                            }
                        }
                    }
                    else
                    {
                        if (textChange.NewPosition < currentPosition)
                        {
                            if (textChange.NewEnd < currentPosition)
                            {
                                currentPosition -= textChange.Delta;
                            }
                            else
                            {
                                // this is why we run the changes in reverse...
                                currentPosition = textChange.NewPosition;
                            }
                        }
                    }
                }
            }

            return currentPosition;
        }

        private static bool IsOpaque(ITextChange textChange)
        {
            ITextChange2 tc2 = textChange as ITextChange2;
            return tc2 != null && tc2.IsOpaque;
        }

        #region Diagnostic Support
        public override string ToString()
        {
            VersionPositionHistory c = this.cachedPosition;
            System.Text.StringBuilder sb = new System.Text.StringBuilder("*");
            sb.Append(ToString(c.Version, c.Position, this.trackingMode));
            if (c.NoninvertibleHistory != null)
            {
                sb.Append("[");
                foreach (VersionNumberPosition vp in c.NoninvertibleHistory)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.CurrentCulture, "V{0}@{1}", vp.VersionNumber, vp.Position));
                }
                sb.Append("]");
            }
            return sb.ToString();
        }
        #endregion
    }
}
