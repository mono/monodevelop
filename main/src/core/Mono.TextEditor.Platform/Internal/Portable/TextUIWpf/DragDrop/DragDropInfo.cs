// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.DragDrop
{
    using System.Windows;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Provides information about an ongoing drag and drop operation. 
    /// It is passed to <see cref="IDropHandler"/> objects when the state
    /// of the drag and drop operation changes. 
    /// </summary>
    public class DragDropInfo
    {
        /// <summary>
        /// Gets the location of the cursor relative to the left top corner of the text view.
        /// </summary>
        public Point Location { get; private set; }

        /// <summary>
        /// Gets the state of the keyboard during the operation. This field can be queried to check
        /// whether certain keys have been pressed.
        /// </summary>
        public DragDropKeyStates KeyStates { get; private set; }

        /// <summary>
        /// Represents the <see cref="IDataObject"/> of the drag and drop operation.
        /// </summary>
        public IDataObject Data { get; private set; }

        /// <summary>
        /// Determines whether the drag and drop operation has been initiated from within the editor.
        /// </summary>
        public bool IsInternal { get; private set; }

        /// <summary>
        /// Gets the object that initiated the drag and drop operation.
        /// </summary>
        public object Source { get; private set; }

        /// <summary>
        /// Gets the buffer position of the cursor during the drag and drop operation.
        /// </summary>
        public VirtualSnapshotPoint VirtualBufferPosition { get; private set; }

        /// <summary>
        /// Gets the drag and drop effects allowed by the source. 
        /// </summary>
        /// <remarks>As part of the contract between the source and the target, 
        /// the target must honor the effects allowed by the source. For example,
        /// if the source does not permit a DragDropEffects.Move, then the target should not execute a move.</remarks>
        public DragDropEffects AllowedEffects { get; private set; }

        #region Construction

        /// <summary>
        /// Initializes a new instance of <see cref="DragDropInfo"/> with the specified settings.
        /// </summary>
        /// <param name="location">The location of the cursor relative to the left top corner of the text view.</param>
        /// <param name="keyStates">The state of the keyboard during the operation.</param>
        /// <param name="data">The <see cref="IDataObject"/> of the drag and drop operation.</param>
        /// <param name="isInternal"><c>true</c> if the drag has been initiated from within the editor, otherwise <c>false</c>.</param>
        /// <param name="source">The object that initiated the drag and drop operation.</param>
        /// <param name="allowedEffects">The drag and drop effects allowed by the source.</param>
        /// <param name="bufferPosition">The buffer position of the cursor during the drag and drop operation.</param>
        public DragDropInfo(Point location, DragDropKeyStates keyStates, IDataObject data, bool isInternal, object source, DragDropEffects allowedEffects, VirtualSnapshotPoint bufferPosition)
        {
            Location = location;
            KeyStates = keyStates;
            Data = data;
            IsInternal = isInternal;
            Source = source;
            AllowedEffects = allowedEffects;
            VirtualBufferPosition = bufferPosition;
        }

        #endregion //Construction

        #region Object Overrides

        /// <summary>
        /// Determines whether two <see cref="DragDropInfo"/> objects have the same settings.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if the two objects have the same settings, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            DragDropInfo other = obj as DragDropInfo;
            if (obj != null)
            {
                return Location == other.Location && KeyStates == other.KeyStates && Data == other.Data &&
                    AllowedEffects == other.AllowedEffects && IsInternal == other.IsInternal && Source == other.Source &&
                    VirtualBufferPosition == other.VirtualBufferPosition;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the hash code for this <see cref="DragDropInfo"/> object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Location.GetHashCode() ^ KeyStates.GetHashCode() ^ Data.GetHashCode() ^
                IsInternal.GetHashCode() ^ Source.GetHashCode() ^
                AllowedEffects.GetHashCode() ^ VirtualBufferPosition.GetHashCode();
        }

        /// <summary>
        /// Determines whether two <see cref="DragDropInfo"/> objects have the same settings.
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <returns><c>true</c> if the two objects have the same settings, otherwise <c>false</c>.</returns>
        public static bool operator ==(DragDropInfo first, DragDropInfo second)
        {
            if (object.ReferenceEquals(first, null))
                return object.ReferenceEquals(second, null);
            else
                return first.Equals(second);
        }

        /// <summary>
        /// Determines whether two <see cref="DragDropInfo"/> objects have different settings.
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <returns><c>true</c> if the two objects have different settings, otherwise <c>false</c>.</returns>
        public static bool operator !=(DragDropInfo first, DragDropInfo second)
        {
            return !(first == second);
        }

        #endregion //Object Overrides
    }
}
