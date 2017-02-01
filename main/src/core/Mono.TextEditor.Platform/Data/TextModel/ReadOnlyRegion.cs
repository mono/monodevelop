// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    /// <summary>
    /// A possibly empty read only region of text.
    /// </summary>
    internal partial class ReadOnlyRegion : IReadOnlyRegion
    {
        #region Private Members

        private readonly ITrackingSpan _trackingSpan;
        private readonly EdgeInsertionMode _edgeInsertionMode;

        #endregion // Private Members

        #region Constructors

        /// <summary>
        /// Creates a ReadOnlyRegionHandle for the given buffer and span.
        /// </summary>
        /// <param name="version">
        /// The <see cref="TextVersion"/> with which this read only region is associated.
        /// </param>
        /// <param name="span">
        /// The span of interest.
        /// </param>
        /// <param name="trackingMode">
        /// Specifies the tracking behavior of the read only region.
        /// </param>
        /// <param name="edgeInsertionMode">
        /// Specifies if insertions should be allowed at the edges 
        /// </param>
        /// <remarks>
        /// Don't call this constructor with invalid parameters.  It doesn't verify all of them.
        /// </remarks>
        internal ReadOnlyRegion(TextVersion version, Span span, SpanTrackingMode trackingMode, EdgeInsertionMode edgeInsertionMode, DynamicReadOnlyRegionQuery callback)
        {
            _edgeInsertionMode = edgeInsertionMode;
            // TODO: change to simple forward tracking text span
            _trackingSpan = new ForwardFidelityTrackingSpan(version, span, trackingMode);
            QueryCallback = callback;
        }

        #endregion

        public DynamicReadOnlyRegionQuery QueryCallback { get; private set; }

        #region Public properties

        /// <summary>
        /// Span of text marked read only by this region.
        /// </summary>
        public ITrackingSpan Span
        {
            get { return _trackingSpan; }
        }

        /// <summary>
        /// The edge insertion behavior of this read only region.
        /// </summary>
        public EdgeInsertionMode EdgeInsertionMode
        {
            get { return _edgeInsertionMode; }
        }

        #endregion

        #region Overrides
        /// <summary>
        /// String representation of the ReadOnlyRegion.
        /// </summary>
        public override string ToString()
        {
            Span currentTrackingSpan = _trackingSpan.GetSpan(_trackingSpan.TextBuffer.CurrentSnapshot);

            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "RO: {2}{0}..{1}{3}", currentTrackingSpan.Start, currentTrackingSpan.End, _edgeInsertionMode == EdgeInsertionMode.Deny ? "[" : "(",
                _edgeInsertionMode == EdgeInsertionMode.Deny ? "]" : ")");
        }
        #endregion
    }
}

