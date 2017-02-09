// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows;
using System;

    /// <summary>
    /// Provides information when an <see cref="ISpaceReservationAgent"/> is changed in an <see cref="ISpaceReservationManager"/>.
    /// </summary>
    
    public class SpaceReservationAgentChangedEventArgs : EventArgs
    {
        private readonly ISpaceReservationAgent _newAgent;
        private readonly ISpaceReservationAgent _oldAgent;

        /// <summary>
        /// Initializes a new instance of <see cref="SpaceReservationAgentChangedEventArgs"/>.
        /// </summary>
        /// <param name="oldAgent">The <see cref="ISpaceReservationAgent "/> associated with the previous value.</param>
        /// <param name="newAgent">The <see cref="ISpaceReservationAgent "/> associated with the new value.</param>
        public SpaceReservationAgentChangedEventArgs(ISpaceReservationAgent oldAgent, ISpaceReservationAgent newAgent)
        {
            _oldAgent = oldAgent;
            _newAgent = newAgent;
        }

        /// <summary>
        /// Gets the old agent.
        /// </summary>
        public ISpaceReservationAgent OldAgent { get { return _oldAgent; } }

        /// <summary>
        /// Gets the new agent.
        /// </summary>
        public ISpaceReservationAgent NewAgent { get { return _newAgent; } }
    }
}
