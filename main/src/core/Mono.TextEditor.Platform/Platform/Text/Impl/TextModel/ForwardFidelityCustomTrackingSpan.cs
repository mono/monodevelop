namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    internal sealed class ForwardFidelityCustomTrackingSpan : ForwardFidelityTrackingSpan 
    {
        object customState;
        CustomTrackToVersion behavior;

        public ForwardFidelityCustomTrackingSpan(TextVersion version, Span span, object customState, CustomTrackToVersion behavior)
            : base(version, span, SpanTrackingMode.Custom)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException("behavior");
            }
            this.behavior = behavior;
            this.customState = customState;
        }

        protected override Span TrackSpanForwardInTime(Span span, ITextVersion currentVersion, ITextVersion targetVersion)
        {
            return behavior(this, currentVersion, targetVersion, span, this.customState);
        }

        protected override Span TrackSpanBackwardInTime(Span span, ITextVersion currentVersion, ITextVersion targetVersion)
        {
            return behavior(this, currentVersion, targetVersion, span, this.customState);
        }
    }
}