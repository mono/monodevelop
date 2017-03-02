//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines the reasons for connecting or disconnecting a text buffer and a text view.
    /// </summary>
    public enum ConnectionReason
    {
        /// <summary>
        /// The <see cref="IWpfTextView"/> has been opened or closed.
        /// </summary>
        TextViewLifetime,

        /// <summary>
        /// The <see cref="Microsoft.VisualStudio.Utilities.IContentType"/> of the subject buffer has changed.
        /// </summary>
        ContentTypeChange,

        /// <summary>
        /// A buffer has been added to or removed from <see cref="Microsoft.VisualStudio.Text.Projection.IBufferGraph"/>.
        /// </summary>
        BufferGraphChange
    }
}
