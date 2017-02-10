// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    /// <summary>
    /// An internal implementation of ITextVersion
    /// </summary>
    internal partial class TextVersion : ITextVersion
    {
        private ITextBuffer textBuffer;
        private int versionNumber;
        private int reiteratedVersionNumber;

        private TextVersion next;
        private INormalizedTextChangeCollection normalizedChanges;
        private int versionLength;

        /// <summary>
        /// Initializes a new instance of a <see cref="TextVersion"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> to which the version belongs.</param>
        /// <param name="versionNumber">The version number, which should be one greater than the preceding version.</param>
        /// <param name="reiteratedVersionNumber">The reiterated version number, which must be less than or equal to the versionNumber.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="versionNumber"/> is negative, or 
        /// <paramref name="reiteratedVersionNumber"/> is either negative or greater than <paramref name="versionNumber"/>.</exception>
        public TextVersion(ITextBuffer textBuffer, int versionNumber, int reiteratedVersionNumber, int length)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            if (versionNumber < 0)
            {
                throw new ArgumentOutOfRangeException("versionNumber");
            }
            if (reiteratedVersionNumber < 0 || reiteratedVersionNumber > versionNumber)
            {
                throw new ArgumentOutOfRangeException("reiteratedVersionNumber");
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            this.textBuffer = textBuffer;
            this.versionNumber = versionNumber;
            this.reiteratedVersionNumber = reiteratedVersionNumber;
            this.versionLength = length;
        }


        /// <summary>
        /// Attaches the change to the current node and creates the next version node
        /// </summary>
        public TextVersion CreateNext(INormalizedTextChangeCollection changes, int reiteratedVersionNumber)
        {
            this.normalizedChanges = changes;
            int delta = 0;
            int changeCount = changes.Count;
            for (int c = 0; c < changeCount; ++c)
            {
                delta += changes[c].Delta;
            }
            this.next = new TextVersion(this.TextBuffer, this.VersionNumber + 1, reiteratedVersionNumber, this.versionLength + delta);
            return this.next;
        }

        public TextVersion CreateNext(INormalizedTextChangeCollection changes)
        {
            if (changes.Count == 0)
            {
                // If there are no changes (e.g. readonly region edit or content type change), then
                // we consider this a reiteration of the current version.
                return CreateNext(changes, this.ReiteratedVersionNumber);
            }
            else
            {
                return CreateNext(changes, this.VersionNumber + 1);
            }
        }

        /// <summary>
        /// Used when the normalized change list of a version needs to refer ahead to the following snapshot
        /// </summary>
        public void AddNextVersion(INormalizedTextChangeCollection changes, TextVersion nextVersion)
        {
            this.normalizedChanges = changes;
            this.next = nextVersion;
        }

        public ITextBuffer TextBuffer
        {
            get { return this.textBuffer; }
        }

        public int VersionNumber
        {
            get { return this.versionNumber; }
        }

        public int ReiteratedVersionNumber
        {
            get { return this.reiteratedVersionNumber; }
        }

        /// <summary>
        /// Gets the next version node
        /// </summary>
        public ITextVersion Next
        {
            get { return this.next; }
        }

        /// <summary>
        /// Gets the current change information
        /// </summary>
        public INormalizedTextChangeCollection Changes
        {
            get { return this.normalizedChanges; }
        }

        public int Length
        {
            get { return this.versionLength; }
        }

        internal int InternalLength
        {
            set { this.versionLength = value; } // hacky
        }

        #region Point and Span Factories
        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            // Forward fidelity is implicit
            return new ForwardFidelityTrackingPoint(this, position, trackingMode);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            if (trackingFidelity == TrackingFidelityMode.Forward)
            {
                return new ForwardFidelityTrackingPoint(this, position, trackingMode);
            }
            else
            {
                return new HighFidelityTrackingPoint(this, position, trackingMode, trackingFidelity);
            }
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            // Forward fidelity is implicit
            if (trackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            return new ForwardFidelityTrackingSpan(this, new Span(start, length), trackingMode);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return CreateTrackingSpan(new Span(start, length), trackingMode, trackingFidelity);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            // Forward fidelity is implicit
            if (trackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            return new ForwardFidelityTrackingSpan(this, span, trackingMode);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            if (trackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }
            if (trackingFidelity == TrackingFidelityMode.Forward)
            {
                return new ForwardFidelityTrackingSpan(this, span, trackingMode);
            }
            else 
            {
                return new HighFidelityTrackingSpan(this, span, trackingMode, trackingFidelity);
            }
        }

        public ITrackingSpan CreateCustomTrackingSpan(Span span, TrackingFidelityMode trackingFidelity, object customState, CustomTrackToVersion behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException("behavior");
            }
            if (trackingFidelity != TrackingFidelityMode.Forward)
            {
                throw new NotImplementedException();
            }
            return new ForwardFidelityCustomTrackingSpan(this, span, customState, behavior);
        }
        #endregion

        public override string ToString()
        {
            return String.Format("V{0} (r{1})", VersionNumber, ReiteratedVersionNumber);
        }
    }
}
