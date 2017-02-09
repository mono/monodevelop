// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a set of parameters that control <see cref="IPeekSession" /> creation.
    /// </summary>
    public class PeekSessionCreationOptions
    {
        /// <summary>
        /// This constructor produces <see cref="PeekSessionCreationOptions" /> that will create a Peek session
        /// using the default sizing behavior.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger a Peek session.</param>
        /// <param name="triggerPoint">
        /// The point in the text buffer at which a Peek session is requested. If set to <value>null</value> the
        /// current carat position is assumed.
        /// </param>
        /// <param name="defaultHeight">The default height of the Peek control.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a Peek session.</param>
        /// <param name="allowUserResize">Specifies whether the Peek control resizers should be enabled.</param>
        /// <param name="resizeListener">A callback object that will be notified when the user resizes the Peek control.</param>
        /// <param name="shouldFocusOnLoad">Specifies whether the Peek control should get focus when it is first loaded.</param>
        public PeekSessionCreationOptions(
                   ITextView textView, 
                   string relationshipName, 
                   ITrackingPoint triggerPoint = null, 
                   double? defaultHeight = null,
                   bool allowUserResize = true,
                   IPeekResizeListener resizeListener = null,
                   bool shouldFocusOnLoad = true
                   )
        {
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }

            if (string.IsNullOrWhiteSpace(relationshipName))
            {
                throw new ArgumentNullException("relationshipName");
            }

            this.TextView = textView;

            if (triggerPoint == null)
            {
                SnapshotPoint caretPosition = textView.Caret.Position.BufferPosition;
                this.TriggerPoint = caretPosition.Snapshot.CreateTrackingPoint(caretPosition, PointTrackingMode.Negative);
            }
            else
            {
                this.TriggerPoint = triggerPoint;
            }

            this.RelationshipName = relationshipName;
            this.DefaultHeight = defaultHeight;
            this.AllowUserResize = allowUserResize;
            this.ResizeListener = resizeListener;
            this.ShouldFocusOnLoad = shouldFocusOnLoad;
        }

        /// <summary>
        /// The <see cref="ITextView"/> over which to create a Peek session.
        /// </summary>
        public ITextView TextView { get; private set; }

        /// <summary>
        /// The point in the text buffer at which a Peek session is requested.
        /// </summary>
        public ITrackingPoint TriggerPoint { get; private set; }

        /// <summary>
        /// The name of the requested relationship to be explored by a Peek session.
        /// </summary>
        public string RelationshipName { get; private set; }

        /// <summary>
        /// The default height, in pixels, for the Peek control. This height
        /// will be overridden by presentations which implement <see cref="IDesiredHeightProvider"/>. If this
        /// is <value>null</value> then the Peek control will attempt to guess an ideal height as a
        /// percentage of the view instead of an absolute pixel height.
        /// </summary>
        public double? DefaultHeight { get; private set; }

        /// <summary>
        /// Indicates whether the top and bottom resizers on the Peek control should be enabled.
        /// </summary>
        public bool AllowUserResize { get; private set; }

        /// <summary>
        /// If set, this contains an object that will be notified when the user resizes the Peek control.
        /// </summary>
        public IPeekResizeListener ResizeListener { get; private set; }

        /// <summary>
        /// Indicates whether or not the Peek control should get the user's focus when it is first loaded.
        /// </summary>
        public bool ShouldFocusOnLoad { get; private set; }
    }
}
