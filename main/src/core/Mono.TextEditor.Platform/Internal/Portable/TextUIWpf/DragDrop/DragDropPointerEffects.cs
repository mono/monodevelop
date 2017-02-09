// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.DragDrop
{
    
    /// <summary>
    /// Specifies the effects of a drag/drop operation.
    /// </summary>
    /// <remarks>
    /// This enumeration has the <see cref="System.FlagsAttribute"/> hence allowing bitwise combination of its member variables.
    /// </remarks>
    [System.Flags]
    public enum DragDropPointerEffects
    {
        /// <summary>
        /// None signals that the drag/drop operation is not allowed. The mouse icon will be changed to the "not allowed" icon and no tracker will be shown.
        /// </summary>
        None = 0,
        /// <summary>
        /// Copy signals that the drag/drop operation will result in data copy. The mouse icon will be changed to the copy icon.
        /// </summary>
        Copy = 1,
        /// <summary>
        /// Link signals that a shortcut/link will be created as the result of the drag/drop operation. The mouse icon will be changed to the shortcut creation icon.
        /// </summary>
        Link = 2,
        /// <summary>
        /// Move signals that the data will be moved from the drag source to the drop target. The mouse icon will be changed to the move icon.
        /// </summary>
        Move = 4,
        /// <summary>
        /// Scroll indicates that the drop operation is causing scrolling in the drop target.
        /// </summary>
        Scroll = 8,
        /// <summary>
        /// Track indicates that a tracker hinting the drop location on the editor will be shown to the user.
        /// </summary>
        Track = 16,
        /// <summary>
        /// All specifies all possible effects together.
        /// </summary>
        All = 31
    }
}
