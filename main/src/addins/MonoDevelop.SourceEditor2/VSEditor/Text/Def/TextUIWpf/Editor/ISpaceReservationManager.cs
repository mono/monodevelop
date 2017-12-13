//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.Text.Adornments;
    using System;
    using MonoDevelop.Components;

    /// <summary>
    /// Manages space reservation adornments.
    /// </summary>
    public interface ISpaceReservationManager
    {
        /// <summary>
        /// Creates a default implementation of an <see cref="ISpaceReservationAgent"/> that displays <paramref name="content"/> in a popup window.
        /// </summary>
        /// <param name="visualSpan">The span of text associated with the tip.</param>
        /// <param name="style">The style options for displaying the tip.</param>
        /// <param name="content">The UI element to be displayed in the tip.</param>
        /// <returns>An <see cref="ISpaceReservationAgent"/> that will display the desired content in a popup window.</returns>
        ISpaceReservationAgent CreatePopupAgent(ITrackingSpan visualSpan, PopupStyles style, Xwt.Widget content);

        /// <summary>
        /// Updates <paramref name="agent"/> with the <paramref name="visualSpan"/>.
        /// This only works for PopupAgents and returns for other agents.
        /// </summary>
        /// <param name="agent">The agent to add.</param>
        /// <param name="visualSpan">The agent's new visual span.</param>
        void UpdatePopupAgent(ISpaceReservationAgent agent, ITrackingSpan visualSpan, PopupStyles styles);

        /// <summary>
        /// Adds <paramref name="agent"/> to the list of agents managed by this manager.
        /// </summary>
        /// <param name="agent">The agent to add.</param>
        void AddAgent(ISpaceReservationAgent agent);

        /// <summary>
        /// Removes <paramref name="agent"/> from the list of agents managed by this manager.
        /// </summary>
        /// <param name="agent">The agent to remove.</param>
        /// <returns><c>true</c> if the agent was in the list of agents to remove.</returns>
        bool RemoveAgent(ISpaceReservationAgent agent);

        /// <summary>
        /// Gets the list of agents managed by this manager.
        /// </summary>
        /// <remarks>Any implementation of aa <see cref="ISpaceReservationAgent"/> can be used for this method.</remarks>
        ReadOnlyCollection<ISpaceReservationAgent> Agents { get; }

        /// <summary>
        /// Occurs when the agent is changed.
        /// </summary>
        /// <remarks></remarks>
        event EventHandler<SpaceReservationAgentChangedEventArgs> AgentChanged;

        /// <summary>
        /// Determines whether the mouse is over an agent managed by this manager.
        /// </summary>
        bool IsMouseOver { get; }

        /// <summary>
        /// Determines whether the adornment created by the space reservation agent has keyboard focus.
        /// </summary>
        bool HasAggregateFocus { get; }

        /// <summary>
        /// Occurs when keyboard focus is lost by any of the managed adornments.
        /// </summary>
        event EventHandler LostAggregateFocus;

        /// <summary>
        /// Occurs when any of the managed adornments gets keyboard focus.
        /// </summary>
        event EventHandler GotAggregateFocus;
    }
}
