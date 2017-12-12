//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows.Media;
    using System;

    /// <summary>
    /// Handles the display of space reservation adornments.
    /// </summary>
    public interface ISpaceReservationAgent
    {
        /// <summary>
        /// Positions and displays the contents of the the <see cref="ISpaceReservationAgent"/>.
        /// </summary>
        /// <param name="reservedSpace">Currently reserved space.</param>
        /// <returns>The space. If null is returned, the <see cref="ISpaceReservationManager"/> will remove the agent.</returns>
        /// <remarks>If an agent does not want to be removed, but also does not wish to request any additional space, it can return a non-null but empty Geometry.</remarks>
        Geometry PositionAndDisplay(Geometry reservedSpace);

        /// <summary>
        /// Called whenever the content of the space reservation agent should be hidden.
        /// </summary>
        /// <remarks>This method is called by the manager to hide the content of the space reservation agent.</remarks>
        void Hide();

        /// <summary>
        /// Determines whether the mouse is over this agent or anything it contains.
        /// </summary>
        bool IsMouseOver { get; }

        /// <summary>
        /// Determines whether the adornment created by the space reservation agent has keyboard focus.
        /// </summary>
        bool HasFocus { get; }

        /// <summary>
        /// Occurs when the adornment created by the ISpaceReservationAgent loses focus.
        /// </summary>
        event EventHandler LostFocus;

        /// <summary>
        /// Occurs when the adornment created by the ISpaceReservationAgent gets focus.
        /// </summary>
        event EventHandler GotFocus;
    }
}
