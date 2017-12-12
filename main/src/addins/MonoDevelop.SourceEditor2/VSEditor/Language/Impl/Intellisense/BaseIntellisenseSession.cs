using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal abstract class BaseIntellisenseSession : IIntellisenseSession
    {
        private PropertyCollection properties = new PropertyCollection();
        protected readonly ITextView textView;
        protected IIntellisensePresenter presenter;

        protected BaseIntellisenseSession(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }

            this.textView = textView;
        }

        public event EventHandler PresenterChanged;
        public event EventHandler Recalculated;
        public event EventHandler Dismissed;

        public ITextView TextView
        {
            get { return this.textView; }
        }

        public PropertyCollection Properties
        {
            get { return this.properties; }
        }

        public IIntellisensePresenter Presenter
        {
            get { return this.presenter; }
        }

        public virtual void Dismiss()
        {
            this.properties = null;
            this.presenter = null;
        }

        public abstract void Start();
        public abstract void Recalculate();
        public abstract bool Match();
        public abstract void Collapse();
        public abstract bool IsDismissed { get; }
        public abstract ITrackingPoint TriggerPoint { get; }

        public virtual ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer)
        {
            var mappedTriggerPoint = GetTriggerPoint(textBuffer.CurrentSnapshot);

            if (!mappedTriggerPoint.HasValue)
            {
                return null;
            }

            return mappedTriggerPoint.Value.Snapshot.CreateTrackingPoint(mappedTriggerPoint.Value, PointTrackingMode.Negative);
        }

        public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot)
        {
            var triggerSnapshotPoint = this.TriggerPoint.GetPoint(this.textView.TextSnapshot);
            var triggerSpan = new SnapshotSpan(triggerSnapshotPoint, 0);

            var mappedSpans = new FrugalList<SnapshotSpan>();
            MappingHelper.MapDownToBufferNoTrack(triggerSpan, textSnapshot.TextBuffer, mappedSpans);

            if (mappedSpans.Count == 0)
            {
                return null;
            }
            else
            {
                return mappedSpans[0].Start;
            }
        }

        protected IIntellisensePresenter FindPresenter
                                        (IIntellisenseSession session,
                                         IList<Lazy<IIntellisensePresenterProvider, IOrderableContentTypeMetadata>> orderedPresenterProviders,
                                         GuardedOperations guardedOperations)
        {
            var buffers = Helpers.GetBuffersForTriggerPoint(session);

            foreach (var presenterProviderExport in orderedPresenterProviders)
            {
                foreach (var buffer in buffers)
                {
                    foreach (var contentType in presenterProviderExport.Metadata.ContentTypes)
                    {
                        if (buffer.ContentType.IsOfType(contentType))
                        {
                            IIntellisensePresenter presenter = guardedOperations.InstantiateExtension(
                                session, presenterProviderExport,
                                factory => factory.TryCreateIntellisensePresenter(session));
                            if (presenter != null)
                            {
                                return presenter;
                            }
                        }
                    }
                }
            }

            return null;
        }



        protected void RaisePresenterChanged()
        {
            EventHandler tempHandler = this.PresenterChanged;
            if (tempHandler != null)
            {
                tempHandler(this, EventArgs.Empty);
            }
        }

        protected void RaiseRecalculated()
        {
            EventHandler tempHandler = this.Recalculated;
            if (tempHandler != null)
            {
                tempHandler(this, EventArgs.Empty);
            }
        }

        protected void RaiseDismissed()
        {
            EventHandler tempHandler = this.Dismissed;
            if (tempHandler != null)
            {
                tempHandler(this, EventArgs.Empty);
            }
        }
    }
}
