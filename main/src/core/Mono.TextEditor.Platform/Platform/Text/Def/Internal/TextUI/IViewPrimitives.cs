//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// Represents common view primitives and an extensible mechanism for replacing their values and adding new options.
    /// </summary>
    public interface IViewPrimitives : IBufferPrimitives
    {
        /// <summary>
        /// Gets the <see cref="View"/> primitive used for scrolling the editor window.
        /// </summary>
        TextView View { get; }

        /// <summary>
        /// Gets the <see cref="Selection"/> primitive used for selection manipulation.
        /// </summary>
        Selection Selection { get; }

        /// <summary>
        /// Gets the <see cref="Caret"/> primitive used for caret movement.
        /// </summary>
        Caret Caret { get; }
    }

    /// <summary>
    /// Represents the common editor primitives produced by this subsystem.
    /// </summary>
    public static class EditorPrimitiveIds
    {
        /// <summary>
        /// The ID for the view.
        /// </summary>
        public const string ViewPrimitiveId = "Editor.View";

        /// <summary>
        /// The ID for the selection.
        /// </summary>
        public const string SelectionPrimitiveId = "Editor.Selection";

        /// <summary>
        /// The ID for the caret.
        /// </summary>
        public const string CaretPrimitiveId = "Editor.Caret";

        /// <summary>
        /// The ID for the buffer.
        /// </summary>
        public const string BufferPrimitiveId = "Editor.TextBuffer";
    }
}
