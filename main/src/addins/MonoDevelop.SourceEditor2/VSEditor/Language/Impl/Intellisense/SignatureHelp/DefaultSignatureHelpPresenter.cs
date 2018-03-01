////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Specialized;
using System.Windows;
using Microsoft.VisualStudio.Language.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Components;
using Xwt;
using Xwt.Backends;
using Rect = Xwt.Rectangle;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal sealed class DefaultSignatureHelpPresenter : IPopupIntellisensePresenter, IObscuringTip, IIntellisenseCommandTarget, IMultiSessionIntellisensePresenter<ISignatureHelpSession>
    {
        private ISignatureHelpSession _session;
        private ITrackingSpan _presentationSpan = null;
        private DefaultSignatureHelpPresenterSurfaceElement _surfaceElement;
        private Widget wrappedXwtWidget;
        private EventHandler _surfaceElementChangedEvent;
        private bool _isDisposed = false;
        private SignatureHelpSessionView _sessionView;
        private PopupStyles _popupStyles = PopupStyles.None;

        public DefaultSignatureHelpPresenter(DefaultSignatureHelpPresenterProvider componentContext)
        {
            _surfaceElement = new DefaultSignatureHelpPresenterSurfaceElement();
            wrappedXwtWidget = _surfaceElement.Content;
            //TODO: _surfaceElement.MouseLeftButtonDown += this.OnSigHelpLayoutGrid_MouseLeftButtonDown;
            _surfaceElement.UpButtonClick += this.OnSurfaceElementUpButtonClick;
            _surfaceElement.DownButtonClick += this.OnSurfaceElementDownButtonClick;
            _sessionView = new SignatureHelpSessionView(componentContext);
        }

        public IIntellisenseSession Session
        {
            get { return (_session); }
        }

        public bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command)
        {
            // Certain keys are important to us.  We'll want to trap things like up/down/escape and make them mean something
            // special to us.

            switch (command)
            {
                case IntellisenseKeyboardCommand.Up:
                    return this.PreviousSignature();
                case IntellisenseKeyboardCommand.Down:
                    return this.NextSignature();
                default:
                    break;
            }

            return false;
        }

        public Xwt.Widget SurfaceElement
        {
            get
            {
                return wrappedXwtWidget;
            }
        }

        public event EventHandler SurfaceElementChanged
        {
            add { _surfaceElementChangedEvent += value; }
            remove { _surfaceElementChangedEvent -= value; }
        }

        public ITrackingSpan PresentationSpan
        {
            get
            {
                return _presentationSpan;
            }
        }

        public PopupStyles PopupStyles
        {
            get
            {
                return _popupStyles;
            }
            private set
            {
                if (value == _popupStyles)
                {
                    return;
                }

                PopupStyles oldPopupStyles = _popupStyles;
                _popupStyles = value;

                // Raise the 'PopupStylesChanged' event.
                EventHandler<ValueChangedEventArgs<PopupStyles>> tempHandler = this.PopupStylesChanged;
                if (tempHandler != null)
                {
                    tempHandler(this, new ValueChangedEventArgs<PopupStyles>(oldPopupStyles, _popupStyles));
                }
            }
        }

        public event EventHandler<ValueChangedEventArgs<PopupStyles>> PopupStylesChanged;

        public string SpaceReservationManagerName
        {
            get { return IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName; }
        }

        public event EventHandler PresentationSpanChanged;

        public double Opacity {
            get; set;
            //get { return (_surfaceElement.Opacity); }
            //set { _surfaceElement.Opacity = value; }
            }

        public void Dispose()
        {
            // Make sure we don't double-dispose.
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            _sessionView.Dispose();
        }

        public bool IsAttachedToSession
        {
            get { return _session != null; }
        }

        public void AttachToSession(ISignatureHelpSession session)
        {
            // If we're being attached to a session for the first time, set the allowable screen size for this presenter.  This
            // presenter instance should only be used for sessions on screens with this resolution.
            if (this.AllowableScreenSize == null)
            {
                this.AllowableScreenSize = Helpers.GetScreenRect(session);
            }

            // If we're re-attaching, make sure to unsubscribe from events on the old session.
            if (this.IsAttachedToSession)
            {
                this.DetachFromSession();
            }

            _session = session;
            _session.Dismissed += this.OnSessionDismissed;
            ((INotifyCollectionChanged)_session.Signatures).CollectionChanged += this.OnSessionSignaturesChanged;
            _session.TextView.TextBuffer.Changed += this.OnSessionViewBuffer_Changed;

            this.ComputePresentationSpan();

            // Bind the view model to the session;
            _sessionView.Session = session;

            // Use the view model as the data context for our presenter surface element.
            _surfaceElement.DataContext = _sessionView;

            // Make sure we start off fully-opaque
            this.Opacity = 1.0;
        }

        public void DetachFromSession()
        {
            // Don't use the view model as the data context anymore.
            _surfaceElement.DataContext = null;

            // Unbind the view model.
            _sessionView.Session = null;

            // Stop listening to all of the session/surface element events.
            _session.Dismissed -= this.OnSessionDismissed;
            ((INotifyCollectionChanged)_session.Signatures).CollectionChanged -= this.OnSessionSignaturesChanged;
            _session.TextView.TextBuffer.Changed -= this.OnSessionViewBuffer_Changed;

            _session = null;

            // Reset out popup styles for the next session to which we get attached.
            _popupStyles = PopupStyles.None;

            _surfaceElement.Hide();
            _presentationSpan = null;
        }

        public Rect? AllowableScreenSize { get; private set; }

        bool IObscuringTip.Dismiss()
        {
            if ((_session != null) && !(_session.IsDismissed))
            {
                _session.Dismiss();
                return true;
            }

            return false;
        }

        void IObscuringTip.SetOpacity(double opacity)
        {
            this.Opacity = opacity;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            this.DetachFromSession();
        }

        private void OnSurfaceElementDownButtonClick(object sender, EventArgs e)
        {
            this.NextSignature();

            // We don't want the focus to remain with this control.  We want to give it back ASAP.
            this.FocusTheSessionView();
        }

        private void OnSurfaceElementUpButtonClick(object sender, EventArgs e)
        {
            this.PreviousSignature();

            // We don't want the focus to remain with this control.  We want to give it back ASAP.
            this.FocusTheSessionView();
        }

        private void OnSigHelpLayoutGrid_MouseLeftButtonDown()
        {
            this.NextSignature();

            // We don't want the focus to remain with this control.  We want to give it back ASAP.
            this.FocusTheSessionView();
        }

        private void OnSessionSignaturesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.ComputePresentationSpan();
        }

        private void OnSessionViewBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            // When the view buffer changes, it's possible that we could want to display the presenter on-top of the signature
            // instead-of below it.
            if ((_session != null) && (!_session.IsDismissed))
            {
                foreach (var signature in _session.Signatures)
                {
                    SnapshotSpan applicabilitySpan = 
                        signature.ApplicableToSpan.GetSpan
                            (signature.ApplicableToSpan.TextBuffer.CurrentSnapshot);
                    if (applicabilitySpan.Start.GetContainingLine().LineNumber !=
                        applicabilitySpan.End.GetContainingLine().LineNumber)
                    {
                        this.PopupStyles |= PopupStyles.PreferLeftOrTopPosition;
                    }
                }
            }
        }

        private void FocusTheSessionView()
        {
            if (_session == null)
            {
                return;
            }

            var textView = _session.TextView as IMdTextView;
            if (textView != null)
            {
                textView.VisualElement.GrabFocus ();
            }
        }

        private void FirePresentationSpanChanged()
        {
            EventHandler tempHandler = this.PresentationSpanChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new EventArgs());
            }
        }

        private bool NextSignature()
        {
            // If there are fewer than two signatures, it doesn't make any sense to go to the next one.  We shouldn't be handling
            // keyboard events in this case.
            if ((_session == null) || (_session.Signatures.Count <= 1))
            {
                return false;
            }

            if (_session.SelectedSignature == null)
            {
                if (_session.Signatures.Count > 0)
                {
                    _session.SelectedSignature = _session.Signatures[0];
                }
            }
            else
            {
                int currentIndex = _session.Signatures.IndexOf(_session.SelectedSignature);
                if (currentIndex < (_session.Signatures.Count - 1))
                {
                    _session.SelectedSignature = _session.Signatures[++currentIndex];
                }
                else if (currentIndex == (_session.Signatures.Count - 1))
                {
                    _session.SelectedSignature = _session.Signatures[0];
                }
            }

            return true;
        }

        private bool PreviousSignature()
        {
            // If there are fewer than two signatures, it doesn't make any sense to go to the next one.  We shouldn't be handling
            // keyboard events in this case.
            if ((_session == null) || (_session.Signatures.Count <= 1))
            {
                return false;
            }

            if (_session.SelectedSignature == null)
            {
                if (_session.Signatures.Count > 0)
                {
                    _session.SelectedSignature = _session.Signatures[0];
                }
            }
            else
            {
                int currentIndex = _session.Signatures.IndexOf(_session.SelectedSignature);
                if (currentIndex > 0)
                {
                    _session.SelectedSignature = _session.Signatures[--currentIndex];
                }
                else if (currentIndex == 0)
                {
                    _session.SelectedSignature = _session.Signatures[_session.Signatures.Count - 1];
                }
            }

            return true;
        }

        private void ComputePresentationSpan()
        {
            if ((_session == null) || (_session.IsDismissed))
            { return; }

            // Save off the old presentation span so we can tell if it changes.
            ITrackingSpan oldPresentationSpan = _presentationSpan;

            if (_session.Signatures.Count > 0)
            {
                _presentationSpan = null;
                foreach (ISignature signature in _session.Signatures)
                {
                    _presentationSpan = IntellisenseUtilities.GetEncapsulatingSpan(_session.TextView, _presentationSpan, signature.ApplicableToSpan);
                }
            }

            // For now, we're only interested in letting consumers know about presentation span changes if the START of our
            // presentation span changed.  That's where our display position gets calculated.
            // TODO : Develop a better story around presentation span changes in popup presenters.  If the presentation location
            //        (coordinates) doesn't really change, we shouldn't have a flicker...
            ITextSnapshot viewSnapshot = _session.TextView.TextSnapshot;
            SnapshotSpan newSpan = _presentationSpan.GetSpan(viewSnapshot);
            SnapshotSpan? oldSpan = null;
            if (oldPresentationSpan != null)
            {
                oldSpan = oldPresentationSpan.GetSpan(viewSnapshot);
            }
            if (!oldSpan.HasValue || (newSpan.Start != oldSpan.Value.Start))
            {
                this.FirePresentationSpanChanged();
            }
        }
    }
}
