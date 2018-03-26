////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal class SignatureHelpSession : IntellisenseSession, ISignatureHelpSession
    {
        private SignatureHelpBroker _componentContext;
        private bool _trackCaret;

        private BulkObservableCollection<ISignature> _activeSignatures = new BulkObservableCollection<ISignature>();
        private ReadOnlyObservableCollection<ISignature> _readOnlySignatures;
        private bool _isDismissed = false;
        private ISignature _selectedSignature;


        public SignatureHelpSession(SignatureHelpBroker componentContext,
                                    ITextView textView,
                                    ITrackingPoint triggerPoint,
                                    bool trackCaret)
            : base(textView, triggerPoint)
        {
            _componentContext = componentContext;

            // If we were asked to track the caret, go ahead and subscribe to the event that tracks its movement.

            _trackCaret = trackCaret;
            if (_trackCaret)
            {
                base.textView.Caret.PositionChanged += new EventHandler<CaretPositionChangedEventArgs>(OnCaretPositionChanged);
            }
        }

        public ReadOnlyObservableCollection<ISignature> Signatures
        {
            get
            {
                if (_readOnlySignatures == null)
                {
                    _readOnlySignatures = new ReadOnlyObservableCollection<ISignature>(_activeSignatures);
                }

                return _readOnlySignatures;
            }
        }

        public ISignature SelectedSignature
        {
            get
            {
                return (_selectedSignature);
            }
            set
            {
                if (value != _selectedSignature)
                {
                    if (_activeSignatures.Contains(value))
                    {
                        ISignature prevSelectedSignature = _selectedSignature;
                        _selectedSignature = value;
                        this.RaiseSelectedSignatureChanged(prevSelectedSignature, _selectedSignature);
                        return;
                    }

                    throw (new ArgumentException("Invalid signature.  Cannot set selection to signature that doesn't exist in the set of signatures for this session."));
                }
            }
        }

        public event EventHandler<SelectedSignatureChangedEventArgs> SelectedSignatureChanged;

        public override bool Match()
        {
            foreach (ISignatureHelpSource provider in _componentContext.GetSignatureHelpSources(this))
            {
                ISignature bestMatch = provider.GetBestMatch(this);
                if (bestMatch != null)
                {
                    this.SelectedSignature = bestMatch;
                    return (true);
                }
            }

            return (false);
        }

        public override void Start()
        {
            this.Recalculate();
        }

        public override void Recalculate()
        {
            if (_isDismissed)
            {
                throw (new InvalidOperationException("Cannot recalculate.  The session is dismissed."));
            }

            // Clear out the active set of signatures.
            _activeSignatures.Clear();

            // In order to find the signatures to be displayed in this session, we'll need to go through each of the signature help
            // providers and ask them for signatures at the trigger point.

            List<ISignature> signatures = new List<ISignature>();
            foreach (ISignatureHelpSource provider in _componentContext.GetSignatureHelpSources(this))
            {
                provider.AugmentSignatureHelpSession(this, signatures);

                // The source could have done anything to the session.  Let's verify that we're still alive and well.
                if (this.IsDismissed)
                {
                    return;
                }
            }

            if (signatures != null)
            {
                _activeSignatures.AddRange(signatures);
            }

            // If we ended-up with zero signatures, that's it.  We need to dismiss and get out of here.
            if (_activeSignatures.Count == 0)
            {
                this.Dismiss();
                return;
            }

            // Let's determine which signature should be "selected" before we find a presenter.
            if (!_activeSignatures.Contains(_selectedSignature))
            {
                // The old selection is no longer valid (or we never had one).  Let's select the first signature.
                this.SelectedSignature = _activeSignatures[0];
            }

            // If we don't already have a presenter for this session, find one.
            if (base.presenter == null)
            {
                base.presenter = FindPresenter(this, _componentContext.OrderedIntellisensePresenterProviderExports, _componentContext.GuardedOperations);
                if (base.presenter != null)
                {
                    this.RaisePresenterChanged();
                }
                else
                {
                    this.Dismiss();
                    return;
                }
            }

            base.RaiseRecalculated();
        }

        public override void Dismiss()
        {
            if (!_isDismissed)
            {
                // Stop listening to the caret movement event

                if (_trackCaret)
                {
                    base.textView.Caret.PositionChanged -= new EventHandler<CaretPositionChangedEventArgs>(OnCaretPositionChanged);
                }

                _isDismissed = true;

                // Fire the "Dismissed" event.  This will signal consumers (namely, the session stack) to release pointers to this
                // session and let us die a peaceful death of garbage collection

                base.RaiseDismissed();

                // Now that we're dismissed, there's no reason to hang-on to the rest of this stuff.
                _componentContext = null;
                _activeSignatures = null;
                _readOnlySignatures = null;
                _selectedSignature = null;

                base.Dismiss();
            }
        }

        public override bool IsDismissed
        {
            get { return (_isDismissed); }
        }

        public override void Collapse()
        {
            this.Dismiss();
        }

        void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // It is possible that one of the previous Caret.PositionChanged event handlers dismissed
            // the session and removed this handler (and null'd all data) from the event. In that case
            // we should simply ignore this call.
            if (_isDismissed)
            {
                return;
            }

            // Let's make sure that the caret is still inside the applicable-to for each of the signatures.  We'll want to remove
            // any completions that aren't applicable.

            ITextSnapshot snapshot = this.TextView.TextBuffer.CurrentSnapshot;
            SnapshotSpan caretSurfaceSpan = new SnapshotSpan(snapshot, this.TextView.Caret.Position.BufferPosition.Position, 0);

            List<ISignature> signaturesToRemove = new List<ISignature>();
            if (null != this.Signatures)
            {
                foreach (ISignature signature in this.Signatures)
                {
                    var signatureSurfaceSpans = this.TextView.BufferGraph.MapUpToBuffer
                        (signature.ApplicableToSpan.GetSpan(signature.ApplicableToSpan.TextBuffer.CurrentSnapshot),
                         signature.ApplicableToSpan.TrackingMode,
                         this.TextView.TextBuffer);
                    foreach (var signatureSurfaceSpan in signatureSurfaceSpans)
                    {
                        if (!signatureSurfaceSpan.IntersectsWith(caretSurfaceSpan))
                        {
                            signaturesToRemove.Add(signature);
                        }
                    }
                }
            }

            foreach (ISignature sigToRemove in signaturesToRemove)
            {
                _activeSignatures.Remove(sigToRemove);
            }

            // If we rejected all of the signatures, we'll want to make sure to dismiss the session
            if (_activeSignatures.Count == 0)
            {
                this.Dismiss();
            }
        }

        private void RaiseSelectedSignatureChanged(ISignature oldSelectedSignature, ISignature newSelectedSignature)
        {
            EventHandler<SelectedSignatureChangedEventArgs> tempHandler = this.SelectedSignatureChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new SelectedSignatureChangedEventArgs(oldSelectedSignature, newSelectedSignature));
            }
        }
    }
}
