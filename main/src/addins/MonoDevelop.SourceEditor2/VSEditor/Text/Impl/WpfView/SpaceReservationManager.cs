//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using MonoDevelop.Components;

    internal class SpaceReservationManager : ISpaceReservationManager
    {
        public readonly string Name;
        public readonly int Rank;
		private readonly Mono.TextEditor.MonoTextEditor _view;
        private bool _hasAggregateFocus;
        internal IList<ISpaceReservationAgent> _agents = new List<ISpaceReservationAgent>();

		public SpaceReservationManager(string name, int rank, Mono.TextEditor.MonoTextEditor view)
        {
            this.Name = name;
            this.Rank = rank;
            _view = view;
            _view.Closed += this.OnViewClosed;
        }

        #region ISpaceReservationManager Members
        public ISpaceReservationAgent CreatePopupAgent(ITrackingSpan visualSpan, PopupStyles styles, Xwt.Widget content)
        {
            return new PopupAgent(_view, this, visualSpan, styles, content);
        }

        public void UpdatePopupAgent(ISpaceReservationAgent agent, ITrackingSpan visualSpan, PopupStyles styles)
        {
            if (agent == null)
                throw new ArgumentNullException("agent");
            if (visualSpan == null)
                throw new ArgumentNullException("visualSpan");

            PopupAgent popupAgent = agent as PopupAgent;
            if (popupAgent == null)
                throw new ArgumentException("The agent is not a PopupAgent", "agent");

            popupAgent.SetVisualSpan(visualSpan);
            popupAgent._style = styles;
            this.CheckFocusChange();
            _view.QueueSpaceReservationStackRefresh();
        }

        public ReadOnlyCollection<ISpaceReservationAgent> Agents
        {
            get { return new ReadOnlyCollection<ISpaceReservationAgent>(_agents); }
        }

        public void AddAgent(ISpaceReservationAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException("agent");

            _agents.Add(agent);
            this.ChangeAgents(null, agent);
            this.CheckFocusChange();
            _view.QueueSpaceReservationStackRefresh();
        }

        public bool RemoveAgent(ISpaceReservationAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException("agent");

            if (_agents.Remove(agent))
            {
                this.ChangeAgents(agent, null);
                this.CheckFocusChange();
                _view.QueueSpaceReservationStackRefresh();

                return true;
            }

            return false;
        }

        public event EventHandler<SpaceReservationAgentChangedEventArgs> AgentChanged;

        public bool IsMouseOver
        {
            get
            {
                foreach (var agent in _agents)
                {
                    if (agent.IsMouseOver)
                        return true;
                }

                return false;
            }
        }

        public bool HasAggregateFocus
        {
            get
            {
                //We can't uses _hasAggregateFocus (the got focus event may not have reached the agent yet)
                foreach (var agent in _agents)
                {
                    if (agent.HasFocus)
                        return true;
                }

                return false;
            }
        }

        public event EventHandler LostAggregateFocus;
        public event EventHandler GotAggregateFocus;
        #endregion

        internal void ChangeAgents(ISpaceReservationAgent oldAgent, ISpaceReservationAgent newAgent)
        {
            if (oldAgent != null)
            {
                oldAgent.LostFocus -= OnAgentLostFocus;
                oldAgent.GotFocus -= OnAgentGotFocus;
                oldAgent.Hide();
            }

            EventHandler<SpaceReservationAgentChangedEventArgs> agentChanged = this.AgentChanged;
            if (agentChanged != null)
                agentChanged(this, new SpaceReservationAgentChangedEventArgs(oldAgent, newAgent));

            if (newAgent != null)
            {
                newAgent.LostFocus += OnAgentLostFocus;
                newAgent.GotFocus += OnAgentGotFocus;
            }

            _view.QueueSpaceReservationStackRefresh();
        }

        void OnAgentLostFocus(object sender, EventArgs e)
        {
            if (_hasAggregateFocus)
            {
                foreach (var agent in _agents)
                {
                    if (agent.HasFocus)
                        return;
                }

                _hasAggregateFocus = false;
                EventHandler lostAggregateFocus = this.LostAggregateFocus;
                if (lostAggregateFocus != null)
                    lostAggregateFocus(sender, e);
            }
        }

        void OnAgentGotFocus(object sender, EventArgs e)
        {
            if (!_hasAggregateFocus)
            {
                _hasAggregateFocus = true;
                EventHandler gotAggregateFocus = this.GotAggregateFocus;
                if (gotAggregateFocus != null)
                    gotAggregateFocus(sender, e);
            }
        }

        /// <summary>
        /// Handle the close event for TextView.  If the view is closed, then dismiss all agents.
        /// </summary>
        void OnViewClosed(object sender, EventArgs e)
        {
            List<ISpaceReservationAgent> agentsToRemove = new List<ISpaceReservationAgent>();
            agentsToRemove.AddRange (_agents);

            foreach (ISpaceReservationAgent agent in agentsToRemove)
            {
                this.RemoveAgent (agent);
            }

            _view.Closed -= this.OnViewClosed;
        }

        internal void PositionAndDisplay(GeometryGroup reservedGeometry)
        {
            _view.GuardedOperations.CallExtensionPoint(this,
               () =>
               {
                   if (_agents.Count != 0)
                   {
					   if (_view.Visible)
                       {
                           for (int i = _agents.Count - 1; (i >= 0); --i)
                           {
                               ISpaceReservationAgent agent = _agents[i];

                               Geometry requestedGeometry = agent.PositionAndDisplay(reservedGeometry);
                               if (requestedGeometry == null)
                               {
                                   _agents.RemoveAt(i);
                                   this.ChangeAgents(agent, null);
                               }
                               else if (!requestedGeometry.IsEmpty())
                                   reservedGeometry.Children.Add(requestedGeometry);
                           }
                       }
                       else
                       {
                           for (int i = _agents.Count - 1; (i >= 0); --i)
                           {
                               ISpaceReservationAgent agent = _agents[i];
                               _agents.RemoveAt(i);
                               this.ChangeAgents(agent, null);
                           }
                       }

                       this.CheckFocusChange();
                   }
               });
        }

        private void CheckFocusChange()
        {
            bool newFocus = this.HasAggregateFocus;
            if (_hasAggregateFocus != newFocus)
            {
                _hasAggregateFocus = newFocus;
                EventHandler focusChangeHandler = _hasAggregateFocus ? this.GotAggregateFocus : this.LostAggregateFocus;

                if (focusChangeHandler != null)
                    focusChangeHandler(this, new EventArgs());
            }
        }
    }
}
