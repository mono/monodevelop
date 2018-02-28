////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Components;
using Rect = Xwt.Rectangle;
using Point = Xwt.Point;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal class CurrentLineSpaceReservationAgent : ISpaceReservationAgent
    {
        internal const string CurrentLineSRManagerName = "currentline";

        private ITextView _textView;
        private IIntellisenseSessionStack _sessionStack;
        private bool _isAttached = false;

        [Export(typeof(ITextViewCreationListener))]
        [ContentType("Text")]
        [TextViewRole(PredefinedTextViewRoles.Editable)]
        internal class CurrentLineSpaceReservationAgent_ViewCreationListener : ITextViewCreationListener
        {
            [Import]
            internal IIntellisenseSessionStackMapService IntellisenseSessionStackMapService { get; set; }

            public void TextViewCreated(ITextView textView)
            {
                var sessionStack = this.IntellisenseSessionStackMapService.GetStackForTextView(textView);
                if (sessionStack != null)
                {
                    var currentLineReservationAgent = new CurrentLineSpaceReservationAgent(textView, sessionStack);
                    textView.Properties.AddProperty(typeof(CurrentLineSpaceReservationAgent), currentLineReservationAgent);
                }
            }
        }

        internal CurrentLineSpaceReservationAgent(ITextView textView, IIntellisenseSessionStack sessionStack)
        {
            _textView = textView;
            _sessionStack = sessionStack;

            // We'll need to know when the collection of sessions changes.
            ((INotifyCollectionChanged)_sessionStack.Sessions).CollectionChanged += this.OnIntellisenseSessions_CollectionChanged;

            // Make sure to unsubscribe when the view closes.
            EventHandler closedHandler = null;
            closedHandler = delegate
            {
                this.Detach();

                ((INotifyCollectionChanged)_sessionStack.Sessions).CollectionChanged -= this.OnIntellisenseSessions_CollectionChanged;
                _textView.Closed -= closedHandler;
            };
            _textView.Closed += closedHandler;
        }

        public Geometry PositionAndDisplay(Geometry reservedSpace)
        {
            return this.CurrentLineGeometry;
        }

        public void Hide()
        { 
        }

        public bool IsMouseOver
        {
            get { return false; }
        }

        public bool HasFocus
        {
            get { return false; }
        }

#pragma warning disable 67
        public event EventHandler LostFocus;
        public event EventHandler GotFocus;
#pragma warning restore 67

        private void OnIntellisenseSessions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IEnumerable<IIntellisenseSession> sessions = (IEnumerable<IIntellisenseSession>)sender;

            bool shouldBeAttached = false;
            foreach (var session in sessions)
            {
                if ((session is ICompletionSession) || (session is ISignatureHelpSession))
                {
                    shouldBeAttached = true;
                    break;
                }
            }

            if (_isAttached && !shouldBeAttached)
            {
                this.Detach();
            }
            else if (!_isAttached && shouldBeAttached)
            {
                this.Attach();
            }
        }

        private void OnCaret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (_isAttached)
            {
                _textView.QueueSpaceReservationStackRefresh();
            }
        }

        private void OnSRManager_AgentChanged(object sender, SpaceReservationAgentChangedEventArgs e)
        {
            if (_isAttached && (e.OldAgent == this))
            {
                // When our space reservation agent is removed from the view, wait for the next layout before re-adding the agent.
                EventHandler<TextViewLayoutChangedEventArgs> layoutChangedHandler = null;
                layoutChangedHandler = delegate(object layoutSender, TextViewLayoutChangedEventArgs layoutArgs)
                {
                    // Make sure to always unsubscribe from the event.  Do this first so we're sure not to leak this ref.
                    ITextView textView = (ITextView)layoutSender;
                    textView.LayoutChanged -= layoutChangedHandler;

                    if ((_textView != null) && (this.CurrentLineSRManager != null))
                    {
                        this.CurrentLineSRManager.AddAgent(this);
                    }
                };
                _textView.LayoutChanged += layoutChangedHandler;
            }
        }

        private void Attach()
        {
            _isAttached = true;

            this.CurrentLineSRManager.AgentChanged += this.OnSRManager_AgentChanged;
            _textView.Caret.PositionChanged += this.OnCaret_PositionChanged;

            if (this.CurrentLineSRManager != null)
            {
                this.CurrentLineSRManager.AddAgent(this);
            }
        }

        private void Detach()
        {
            _isAttached = false;

            this.CurrentLineSRManager.AgentChanged -= this.OnSRManager_AgentChanged;
            _textView.Caret.PositionChanged -= this.OnCaret_PositionChanged;

            if (this.CurrentLineSRManager != null)
            {
                this.CurrentLineSRManager.RemoveAgent(this);
            }
        }

        private Geometry CurrentLineGeometry
        {
            get
            {
                Debug.Assert(_textView != null);

                if (!_isAttached || (_textView == null))
                {
                    return Geometry.Empty;
                }

                var caretLine = _textView.Caret.ContainingTextViewLine;

                if ((caretLine != null) &&
                    (caretLine.VisibilityState != VisibilityState.Unattached) &&
                    (caretLine.VisibilityState != VisibilityState.Hidden))
                {
                    var topLeft = ((IMdTextView)_textView).VisualElement.GetScreenCoordinates
                        (new Gdk.Point((int)_textView.ViewportLeft, (int)(caretLine.TextTop - _textView.ViewportTop)));
                    Rect screenRect = new Rect
                        (topLeft.X,
                         topLeft.Y,
                         _textView.ViewportWidth,
                         (caretLine.TextHeight) + 3); // Add some buffer to allow for the possibility of a smart
                                                      // tag tickler below the line.
                    return new RectangleGeometry(screenRect);
                }
                else
                {
                    return Geometry.Empty;
                }
            }
        }

        private ISpaceReservationManager _currentLineSRManager;
        private ISpaceReservationManager CurrentLineSRManager
        {
            get
            {
                if (_currentLineSRManager == null)
                {
                    _currentLineSRManager = ((IMdTextView)_textView).GetSpaceReservationManager(CurrentLineSRManagerName);
                }

                return _currentLineSRManager;
            }
        }
    }
}
