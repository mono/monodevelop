////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    [Export(typeof(ISignatureHelpBroker))]
    internal class SignatureHelpBroker : ISignatureHelpBroker
    {
        [ImportMany]
        internal List<Lazy<ISignatureHelpSourceProvider, IOrderableContentTypeMetadata>> UnOrderedSignatureHelpSourceProviderExports
        { get; set; }

        [ImportMany]
        private List<Lazy<IIntellisensePresenterProvider, IOrderableContentTypeMetadata>> UnOrderedIntellisensePresenterProviderExports
        { get; set; }

        private IList<Lazy<IIntellisensePresenterProvider, IOrderableContentTypeMetadata>> _orderedIntellisensePresenterProviderExports;
        internal IList<Lazy<IIntellisensePresenterProvider, IOrderableContentTypeMetadata>> OrderedIntellisensePresenterProviderExports
        {
            get
            {
                if (_orderedIntellisensePresenterProviderExports == null)
                {
                    _orderedIntellisensePresenterProviderExports = Orderer.Order(this.UnOrderedIntellisensePresenterProviderExports);
                }

                return _orderedIntellisensePresenterProviderExports;
            }
        }

        [Import]
        internal IIntellisenseSessionStackMapService IntellisenseSessionStackMap { get; set; }

        [Import]
        internal GuardedOperations GuardedOperations { get; set; }

#if DEBUG
        [ImportMany]
        internal List<Lazy<IObjectTracker>> ObjectTrackers { get; set; }
#endif

        public void DismissAllSessions(ITextView textView)
        {
            foreach (var session in this.GetSessions(textView))
            {
                session.Dismiss();
            }
        }

        public bool IsSignatureHelpActive(ITextView textView)
        {
            return this.GetSessions(textView).Count > 0;
        }

        public ReadOnlyCollection<ISignatureHelpSession> GetSessions(ITextView textView)
        {
            FrugalList<ISignatureHelpSession> tempSessionList = new FrugalList<ISignatureHelpSession>();

            IIntellisenseSessionStack sessionStack = this.IntellisenseSessionStackMap.GetStackForTextView(textView);
            if (sessionStack == null)
            {
                return tempSessionList.AsReadOnly();
            }

            foreach (var session in sessionStack.Sessions)
            {
                ISignatureHelpSession sigHelpSession = session as ISignatureHelpSession;
                if (sigHelpSession != null)
                {
                    tempSessionList.Add(sigHelpSession);
                }
            }

            return tempSessionList.AsReadOnly();
        }

        public ISignatureHelpSession TriggerSignatureHelp(ITextView textView)
        {
            var caretPosition = textView.Caret.Position.BufferPosition;
            return this.TriggerSignatureHelp
                (textView,
                 caretPosition.Snapshot.CreateTrackingPoint
                    (caretPosition.Position, PointTrackingMode.Negative),
                 true);
        }

        public ISignatureHelpSession TriggerSignatureHelp(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
        {
            ISignatureHelpSession session = this.CreateSignatureHelpSession(textView, triggerPoint, trackCaret);
            session.Start();

            if (session.IsDismissed)
            {
                return (null);
            }

            return session;
        }

        public ISignatureHelpSession CreateSignatureHelpSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
        {
            if (triggerPoint.TextBuffer != textView.TextBuffer)
            {
                // Map the trigger point up to the surface buffer.  That's where the session will live.
                SnapshotPoint? surfaceTriggerSnapPoint = textView.BufferGraph.MapUpToBuffer
                    (triggerPoint.GetPoint(triggerPoint.TextBuffer.CurrentSnapshot),
                     PointTrackingMode.Negative,
                     PositionAffinity.Successor,
                     textView.TextBuffer);
                if (!surfaceTriggerSnapPoint.HasValue)
                {
                    // We can't trigger a session when the trigger point doesn't map to the surface buffer.
                    return null;
                }
                triggerPoint = surfaceTriggerSnapPoint.Value.Snapshot.CreateTrackingPoint
                    (surfaceTriggerSnapPoint.Value.Position,
                     PointTrackingMode.Negative);
            }

            // Make sure we can get ahold of a session stack.
            IIntellisenseSessionStack sessionStack = this.IntellisenseSessionStackMap.GetStackForTextView(textView);
            if (sessionStack == null)
            {
                throw new InvalidOperationException("Couldn't retrieve Intellisense Session Stack for the specified ITextView.");
            }

            SignatureHelpSession session = new SignatureHelpSession(this, textView, triggerPoint, trackCaret);

#if DEBUG
            Helpers.TrackObject(this.ObjectTrackers, "Signature Help Sessions", session);
#endif

            sessionStack.PushSession(session);
            return session;
        }

        internal IEnumerable<ISignatureHelpSource> GetSignatureHelpSources(IIntellisenseSession session)
        {
            return Helpers.GetSources(session, this.CreateSourcesForBuffer);
        }

        private IList<Lazy<ISignatureHelpSourceProvider, IOrderableContentTypeMetadata>> _orderedSignatureHelpSourceProviderExports;
        private IList<Lazy<ISignatureHelpSourceProvider, IOrderableContentTypeMetadata>> OrderedSignatureHelpSourceProviderExports
        {
            get
            {
                if (_orderedSignatureHelpSourceProviderExports == null)
                {
                    _orderedSignatureHelpSourceProviderExports = Orderer.Order(this.UnOrderedSignatureHelpSourceProviderExports);
                }

                return _orderedSignatureHelpSourceProviderExports;
            }
        }

        private IEnumerable<ISignatureHelpSource> CreateSourcesForBuffer(ITextBuffer buffer)
        {
            FrugalList<ISignatureHelpSource> sources = new FrugalList<ISignatureHelpSource>();
            foreach(var source in this.GuardedOperations.InvokeMatchingFactories
                                                            (this.OrderedSignatureHelpSourceProviderExports,
                                                             provider => provider.TryCreateSignatureHelpSource(buffer),
                                                             buffer.ContentType,
                                                             this))
            {
#if DEBUG
                Helpers.TrackObject(this.ObjectTrackers, "SignatureHelp Sources", source);
#endif
                sources.Add(source);
            }

            return sources;
        }
    }
}
