////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal class StackList<T> : ObservableCollection<T>
    {
        internal void Push(T item)
        {
            this.Insert(0, item);
        }

        internal T Pop()
        {
            if (this.Count > 0)
            {
                T item = this[0];
                this.RemoveAt(0);
                return (item);
            }
            else
            {
                return (default(T));
            }
        }

        internal T Peek()
        {
            if (this.Count > 0)
            {
                return (this[0]);
            }
            else
            {
                return (default(T));
            }
        }
    }

    internal sealed class IntellisenseSessionStack : IIntellisenseSessionStack, IIntellisenseCommandTarget
    {
        private readonly IObscuringTipManager _tipManager;
        private IMdTextView _textView;
        private StackList<IIntellisenseSession> _sessions = new StackList<IIntellisenseSession>();
        private ReadOnlyObservableCollection<IIntellisenseSession> _readOnlySessions;
        private Dictionary<IIntellisenseSession, ISpaceReservationAgent> _reservationAgentIndex =
            new Dictionary<IIntellisenseSession, ISpaceReservationAgent>();
        private Dictionary<ISpaceReservationAgent, ISpaceReservationManager> _reservationManagerIndex =
            new Dictionary<ISpaceReservationAgent, ISpaceReservationManager>();
        private IIntellisenseSession _keyboardSession;
        private IIntellisenseSession _sessionBeingRehosted;

        public IntellisenseSessionStack(IMdTextView textView, IObscuringTipManager tipManager)
        {
            _textView = textView;
            _tipManager = tipManager;       // This can be null
        }

        public IIntellisenseSession PopSession()
        {
            // First, remove the session at the top of the stack.

            IIntellisenseSession session = _sessions.Pop();
            if (session != null)
            {
                this.ReleaseKeyboard();
                this.RemoveSession(session);
            }

            // Now, figure out once again who deserves to own the keyboard.

            this.DoleOutKeyboard();

            return (session);
        }

        public void PushSession(IIntellisenseSession session)
        {
            // make sure the session is not already dismissed
            if (session.IsDismissed)
            {
                throw new ArgumentException("We cannot push a dismissed session on to the stack.");
            }

            // Tell whatever session has the keyboard to release it.  After we push, we'll figure out who deserves it.

            this.ReleaseKeyboard();

            // Add the session to our list.

            _sessions.Push(session);

            // Subscribe to events on this new session

            session.Dismissed += this.OnSessionDismissed;
            session.PresenterChanged += this.OnSessionPresenterChanged;
            IPopupIntellisensePresenter popupPresenter = session.Presenter as IPopupIntellisensePresenter;
            if (popupPresenter != null)
            {
                popupPresenter.SurfaceElementChanged += this.OnPresenterSurfaceElementChanged;
                popupPresenter.PresentationSpanChanged += this.OnPresenterPresentationSpanChanged;
                popupPresenter.PopupStylesChanged += this.OnPresenterPopupStylesChanged;

                // Since this is a popup presenter, we're responsible for drawing it.  Therefore, here we'll create a popup agent
                // that will take care of rendering this session's presenter.

                this.HostSession(session, popupPresenter);
            }
            else
            {
                ICustomIntellisensePresenter customPresenter = session.Presenter as ICustomIntellisensePresenter;
                if (customPresenter != null)
                {
                    customPresenter.Render();
                }
            }

            // Now, figure out once again who deserves to own the keyboard.

            this.DoleOutKeyboard();
        }

        public void MoveSessionToTop(IIntellisenseSession session)
        {
            // Make sure this session is actually in the stack.
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }
            int sessionIndex = _sessions.IndexOf(session);
            if (sessionIndex == -1)
            {
                throw new ArgumentException
                    ("IIntellisenseSessionStack.MoveSessionToTop() must be called with a session already in the stack.",
                     "session");
            }

            // Release the keyboard.  We'll give it back in a minute.
            this.ReleaseKeyboard();

            // Remove the session from the stack at its old position and re-add it at the top.
            _sessions.RemoveAt(sessionIndex);
            _sessions.Push(session);

            // Dole out the keyboard once again.
            this.DoleOutKeyboard();
        }

        public ReadOnlyObservableCollection<IIntellisenseSession> Sessions
        {
            get
            {
                if (_readOnlySessions == null)
                {
                    _readOnlySessions = new ReadOnlyObservableCollection<IIntellisenseSession>(_sessions);
                }

                return _readOnlySessions;
            }
        }

        public IIntellisenseSession TopSession
        {
            get { return (_sessions.Peek()); }
        }

        public void CollapseAllSessions()
        {
            List<IIntellisenseSession> sessions = new List<IIntellisenseSession>(_sessions);
            foreach (var session in sessions)
            {
                session.Collapse();
            }
        }

        /// <summary>
        /// Calls each of the stack's session presenters, in order, to see if they want to handle the keyboard command
        /// </summary>
        public bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command)
        {
            // We don't care if there's a keyboard session here or not.  If someone has captured the keyboard, this should only get
            // called if the capturer has decided not to handle the command.

            // Run through the sessions from the topmost to the bottom-most.
            foreach (IIntellisenseSession session in _sessions)
            {
                IIntellisenseCommandTarget commandTarget = session.Presenter as IIntellisenseCommandTarget;
                if (commandTarget != null)
                {
                    if (commandTarget.ExecuteKeyboardCommand(command))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnSpaceReservationManager_AgentChanged(object sender, SpaceReservationAgentChangedEventArgs e)
        {
            if (e.NewAgent == null)
            {
                List<IIntellisenseSession> sessionsToDismiss = new List<IIntellisenseSession>();

                foreach (IIntellisenseSession session in _reservationAgentIndex.Keys)
                {
                    // Skip sessions that are being re-hosted.

                    if (session == _sessionBeingRehosted)
                    { continue; }

                    if (_reservationAgentIndex[session] == e.OldAgent)
                    {
                        // We've just noticed that one of the agents we've put up on-screen has gone away.  We need to dismiss
                        // the session being displayed with this agent.

                        if (!session.IsDismissed)
                        {
                            sessionsToDismiss.Add(session);
                        }
                    }
                }

                // We need to dismiss these outside of the main loop, as dismissing will change the _reservationAgentIndex, which
                // will fail the enumeration above.

                foreach (IIntellisenseSession session in sessionsToDismiss)
                {
                    session.Dismiss();
                }
            }
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            // Whenever a session is dismissed, we'll want to remove it from the stack, as it's no longer a "valid" session.

            IIntellisenseSession session = sender as IIntellisenseSession;
            if (session == null)
            {
                throw new ArgumentException("Expected 'sender' to be of type IIntellisenseSession", "sender");
            }

            if (!_sessions.Contains(session))
            {
                throw new ArgumentException("Expected session that is already on the stack", "sender");
            }

            // If it's the top session that was dismissed, our job is easy, we just have to pop() it off.

            if ((session.TextView != null) && (_tipManager != null))
            {
                var tip = session.Presenter as IObscuringTip;
                if (tip != null)
                {
                    _tipManager.RemoveTip(session.TextView, tip);
                }
            }

            if (session == this.TopSession)
            {
                this.PopSession();
                return;
            }

            // Must not have been the top session.  We'll have to remove it from the stack manually, then remove all of our
            // event handlers, etc.

            _sessions.Remove(session);
            this.RemoveSession(session);

            // If the session being removed is the one that's holding-on to the keyboard, we need to figure out who owns the
            // keyboard once again.

            if (session == _keyboardSession)
            {
                this.DoleOutKeyboard();
            }
        }

        private void OnPresenterPresentationSpanChanged(object sender, EventArgs e)
        {
            IPopupIntellisensePresenter presenter = sender as IPopupIntellisensePresenter;
            if (presenter == null)
            {
                throw new ArgumentException("Expected 'sender' to be of type IPopupIntellisensePresenter", "sender");
            }

            this.RehostPresenter(presenter);
        }

        private void OnPresenterSurfaceElementChanged(object sender, EventArgs e)
        {
            IPopupIntellisensePresenter presenter = sender as IPopupIntellisensePresenter;
            if (presenter == null)
            {
                throw new ArgumentException("Expected 'sender' to be of type IPopupIntellisensePresenter", "sender");
            }

            this.RehostPresenter(presenter);
        }

        private void OnPresenterPopupStylesChanged(object sender, ValueChangedEventArgs<Text.Adornments.PopupStyles> e)
        {
            IPopupIntellisensePresenter presenter = sender as IPopupIntellisensePresenter;
            if (presenter == null)
            {
                throw new ArgumentException("Expected 'sender' to be of type IPopupIntellisensePresenter", "sender");
            }

            this.RehostPresenter(presenter);
        }

        private void OnSessionPresenterChanged(object sender, EventArgs e)
        {
            // It could have been any of our sessions that fired this event.  That means we've first got to determine which one it
            // was.

            IIntellisenseSession session = sender as IIntellisenseSession;
            if (session == null)
            {
                throw new ArgumentException("Expected 'sender' to be of type IIntellisenseSession", "sender");
            }

            // Since the presenter changed, we could have a new owner for the keyboard.  Figure it out.

            this.ReleaseKeyboard();

            // We should make sure to re-subscribe to events on this new presenter.

            IPopupIntellisensePresenter popupPresenter = session.Presenter as IPopupIntellisensePresenter;
            if (popupPresenter != null)
            {
                popupPresenter.PresentationSpanChanged += this.OnPresenterPresentationSpanChanged;
                popupPresenter.SurfaceElementChanged += this.OnPresenterSurfaceElementChanged;
                popupPresenter.PopupStylesChanged += this.OnPresenterPopupStylesChanged;

                this.RehostSession(session);
            }
            else
            {
                ICustomIntellisensePresenter customPresenter = session.Presenter as ICustomIntellisensePresenter;
                if (customPresenter != null)
                {
                    customPresenter.Render();
                }
            }

            if ((session.TextView != null) && (_tipManager != null))
            {
                var tip = session.Presenter as IObscuringTip;
                if (tip != null)
                {
                    _tipManager.PushTip(session.TextView, tip);
                }
            }

            this.DoleOutKeyboard();
        }

        private void ReleaseKeyboard()
        {
            if (_keyboardSession != null)
            {
                ICustomKeyboardHandler keyboardHandler = _keyboardSession.Presenter as ICustomKeyboardHandler;
                if (keyboardHandler != null)
                {
                    keyboardHandler.ReleaseKeyboard();
                }
            }

            _keyboardSession = null;
        }

        private void DoleOutKeyboard()
        {
            if (_keyboardSession != null)
            {
                this.ReleaseKeyboard();
            }

            // The idea is to walk down the stack from top to bottom, looking for the first session that has a presenter.  When we
            // find it, give it the keyboard, unless it already has it.

            foreach (IIntellisenseSession session in _sessions)
            {
                ICustomKeyboardHandler keyboardHandler = session.Presenter as ICustomKeyboardHandler;
                if (keyboardHandler != null)
                {
                    if (keyboardHandler.CaptureKeyboard())
                    {
                        _keyboardSession = session;
                        break;
                    }
                }
            }
        }

        private void HostSession(IIntellisenseSession session, IPopupIntellisensePresenter popupPresenter)
        {
            // If the Popup presenter doesn't have anything to draw, don't even bother.

            if (popupPresenter.SurfaceElement == null)
            { return; }

            ISpaceReservationManager manager = _textView.GetSpaceReservationManager(popupPresenter.SpaceReservationManagerName);
            if (manager != null)
            {
                // If this is the first time we've seen this manager, subscribe to its AgentChanged event.

                if (!_reservationManagerIndex.ContainsValue(manager))
                {
                    manager.AgentChanged += this.OnSpaceReservationManager_AgentChanged;
                }

                ISpaceReservationAgent agent = manager.CreatePopupAgent(popupPresenter.PresentationSpan,
                    popupPresenter.PopupStyles,
                    popupPresenter.SurfaceElement);

                // We'll need to hold-on to the manager and agent so that later, when we want to hide this popup, we can clear
                // the agent.

                _reservationManagerIndex[agent] = manager;
                _reservationAgentIndex[session] = agent;

                // When we add this agent to the manager's collection, the popup will become visible.

                manager.AddAgent(agent);
            }
        }

        private void RehostSession(IIntellisenseSession session)
        {
            IPopupIntellisensePresenter popupPresenter = session.Presenter as IPopupIntellisensePresenter;
            if (popupPresenter == null)
            {
                throw new ArgumentException("Expected to rehost a session with presenter of type IPopupIntellisensePresenter",
                    "session");
            }

            // If the Popup presenter doesn't have anything to draw, don't even bother.

            if (popupPresenter.PresentationSpan == null || popupPresenter.SurfaceElement == null)
            {
                return;
            }

            // We need to re-draw this popup.  This involves tearing down the old agent and re-creating it.  First, we've
            // got to find it.  We'll also want to save-off a reference to it so that we know we're re-hosting this session.  The
            // process of tearing-down and re-adding a space reservation agent is loud and messy.  We don't want to accidentally
            // dismiss this session, thinking that it went out-of-focus.

            _sessionBeingRehosted = session;

            try
            {
                ISpaceReservationAgent oldAgent = null;
                if ((_reservationAgentIndex.TryGetValue(session, out oldAgent)) && (oldAgent != null))
                {
                    ISpaceReservationManager manager = null;
                    if ((_reservationManagerIndex.TryGetValue(oldAgent, out manager)) && (manager != null))
                    {
                        manager.UpdatePopupAgent(oldAgent, popupPresenter.PresentationSpan, popupPresenter.PopupStyles);
                    }
                }
                else
                {
                    // This presenter hasn't yet been hosted in a popup.  Host it for the first time.

                    this.HostSession(session, popupPresenter);
                }
            }
            finally
            {
                // Clear out our state.  We're no longer re-hosting this session.

                _sessionBeingRehosted = null;
            }
        }

        private void RemoveSession(IIntellisenseSession session)
        {
            // A session can be added to the stack in only one way.  It must be pushed onto the top of the stack.  A session can
            // come off the stack in many ways, however.  It can be popped, or the session can be dismissed, etc.  We'll assume that
            // the session passed-in has already been removed from the stack.  We're just interested in removing it from our indices
            // and getting ourselves unhooked from its delegates.

            // First, unsubscribe from events to which we previously subscribed

            session.Dismissed -= this.OnSessionDismissed;
            session.PresenterChanged -= this.OnSessionPresenterChanged;

            IPopupIntellisensePresenter popupPresenter = session.Presenter as IPopupIntellisensePresenter;
            if (popupPresenter != null)
            {
                popupPresenter.SurfaceElementChanged -= this.OnPresenterSurfaceElementChanged;
                popupPresenter.PresentationSpanChanged -= this.OnPresenterPresentationSpanChanged;
                popupPresenter.PopupStylesChanged -= this.OnPresenterPopupStylesChanged;

                // Now, see if we have a popup agent for this presenter.  If so, let's get rid of it.  This is what will "hide" the
                // popup.

                ISpaceReservationAgent agent;
                if ((_reservationAgentIndex.TryGetValue(session, out agent)) && (agent != null))
                {
                    ISpaceReservationManager manager;
                    if ((_reservationManagerIndex.TryGetValue(agent, out manager)) && (manager != null))
                    {
                        manager.RemoveAgent(agent);
                        _reservationManagerIndex.Remove(agent);

                        // If this was the last reference to this particular manager, stop listening to its AgentChanged event.

                        if (!_reservationManagerIndex.ContainsValue(manager))
                        {
                            manager.AgentChanged -= this.OnSpaceReservationManager_AgentChanged;
                        }
                    }
                    _reservationAgentIndex.Remove(session);
                }
            }
        }

        private void RehostPresenter(IPopupIntellisensePresenter presenter)
        {
            IIntellisenseSession session = presenter.Session;
            if (session != null)
            {
                // Technically, there's probably no reason to release the keyboard here, but maybe there's a presenter that
                // conditionally holds-on to the keyboard depending on its presentation.  In that case, we'll give them another
                // chance to own the keyboard here.

                this.ReleaseKeyboard();

                RehostSession(session);

                this.DoleOutKeyboard();
            }
        }
    }
}
