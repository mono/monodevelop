using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal abstract class IntellisenseSession : BaseIntellisenseSession
    {
        protected readonly ITrackingPoint triggerPoint;

        protected IntellisenseSession(ITextView textView, ITrackingPoint triggerPoint)
            : base(textView)
        {
            if (triggerPoint == null)
            {
                throw new ArgumentNullException("triggerPoint");
            }

            this.triggerPoint = triggerPoint;
        }

        public override ITrackingPoint TriggerPoint
        {
            get
            {
                return this.triggerPoint;
            }
        }
    }
}
