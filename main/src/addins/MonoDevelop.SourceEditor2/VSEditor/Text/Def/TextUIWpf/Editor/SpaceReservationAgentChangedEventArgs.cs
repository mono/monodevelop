//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace MonoDevelop.SourceEditor
{
    using System;

    /// <summary>
    /// Provides information when an <see cref="IMDSpaceReservationAgent"/> is changed in an <see cref="IMDSpaceReservationManager"/>.
    /// </summary>

    public class MDSpaceReservationAgentChangedEventArgs : EventArgs
    {
        private readonly IMDSpaceReservationAgent _newAgent;
        private readonly IMDSpaceReservationAgent _oldAgent;

        /// <summary>
        /// Initializes a new instance of <see cref="MDSpaceReservationAgentChangedEventArgs"/>.
        /// </summary>
        /// <param name="oldAgent">The <see cref="IMDSpaceReservationAgent "/> associated with the previous value.</param>
        /// <param name="newAgent">The <see cref="IMDSpaceReservationAgent "/> associated with the new value.</param>
        public MDSpaceReservationAgentChangedEventArgs(IMDSpaceReservationAgent oldAgent, IMDSpaceReservationAgent newAgent)
        {
            _oldAgent = oldAgent;
            _newAgent = newAgent;
        }

        /// <summary>
        /// Gets the old agent.
        /// </summary>
        public IMDSpaceReservationAgent OldAgent { get { return _oldAgent; } }

        /// <summary>
        /// Gets the new agent.
        /// </summary>
        public IMDSpaceReservationAgent NewAgent { get { return _newAgent; } }
    }
}
