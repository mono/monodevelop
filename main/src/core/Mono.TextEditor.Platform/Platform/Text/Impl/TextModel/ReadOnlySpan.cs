// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;

    /// <summary>
    /// A span which tracks whether or not its edges can be inserted at.
    /// </summary>
    internal class ReadOnlySpan : ForwardFidelityTrackingSpan
    {
        #region Private members
        private readonly EdgeInsertionMode _startEdgeInsertionMode;
        private readonly EdgeInsertionMode _endEdgeInsertionMode;
        #endregion

        #region Constructors
        internal ReadOnlySpan(ITextVersion version, Span span, SpanTrackingMode trackingMode, EdgeInsertionMode startEdgeInsertionMode, EdgeInsertionMode endEdgeInsertionMode) 
          : base(version, span, trackingMode)
        {
            _startEdgeInsertionMode = startEdgeInsertionMode;
            _endEdgeInsertionMode = endEdgeInsertionMode;
        }

        internal ReadOnlySpan(ITextVersion version, IReadOnlyRegion readOnlyRegion)
          : base(version, readOnlyRegion.Span.GetSpan(version), readOnlyRegion.Span.TrackingMode)
        {
            _startEdgeInsertionMode = readOnlyRegion.EdgeInsertionMode;
            _endEdgeInsertionMode = readOnlyRegion.EdgeInsertionMode;
        }
        #endregion

        #region Public properties

        /// <summary>
        /// Whether this span allows insertions on the start edge
        /// </summary>
        public EdgeInsertionMode StartEdgeInsertionMode
        {
            get { return _startEdgeInsertionMode; }
        }

        /// <summary>
        /// Whether this span allows insertions on the end edge
        /// </summary>
        public EdgeInsertionMode EndEdgeInsertionMode
        {
            get { return _endEdgeInsertionMode; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determine if a replace of a particular span will be allowed by this read only region.
        /// 
        /// Validate parameters before calling this method.
        /// 
        /// Zero length read only regions only disallow inserts.
        /// </summary>
        /// <param name="span">The span to check to see if a change would be allowed.</param>
        /// <param name="textSnapshot">The snapshot to check to see if replace is allowed.</param>
        /// <returns>Whether or not the change is allowed.</returns>
        public bool IsReplaceAllowed(Span span, ITextSnapshot textSnapshot)
        {
            // Check to see if insert is allowed since we are doing a replace of a zero length span.
            if (span.Length == 0)
            {
                return IsInsertAllowed(span.Start, textSnapshot);
            }

            Span currentSpan = this.GetSpan(textSnapshot);

            // Zero length read only regions only
            // disallow inserts.
            if (currentSpan.Length == 0)
            {
                return true;
            }

            // If the span overlaps this read only
            // region, then the change will not be allowed.
            if ((currentSpan == span) || currentSpan.OverlapsWith(span))
            {
                return false;
            }

            // Nothing disallows this change
            return true;
        }

        /// <summary>
        /// Determine if an insert is allowed by this read only region.
        /// 
        /// Validate parameters before calling this method.
        /// </summary>
        /// <param name="position">The position to check if the insert is allowed.</param>
        /// <param name="textSnapshot">The text snapshot to check the position relative to.</param>
        /// <returns></returns>
        public bool IsInsertAllowed(int position, ITextSnapshot textSnapshot)
        {
            Span currentSpan = this.GetSpan(textSnapshot);

            // Does this position fall in the middle of this span?
            if ((currentSpan.Start < position) && (currentSpan.End > position))
            {
                return false;
            }

            // If edge insertions are prohibited on the start edge
            // and an insert is occurring on the start edge, the insert
            // is not allowed.
            if (this.StartEdgeInsertionMode == EdgeInsertionMode.Deny)
            {
                if (position == currentSpan.Start)
                {
                    return false;
                }
            }

            // If edge insertions are prohibited on the end edge
            // and an insert is occurring on the end edge, the change
            // is not allowed.
            if (EndEdgeInsertionMode == EdgeInsertionMode.Deny)
            {
                if (position == currentSpan.End)
                {
                    return false;
                }
            }

            // Nothing disallows this change
            return true;
        }

        #endregion
    }
}

