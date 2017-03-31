//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.ComponentModel;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Metadata which includes Content Types and Text View Roles
    /// </summary>
    public interface IDeferrableContentTypeAndTextViewRoleMetadata : IContentTypeAndTextViewRoleMetadata
    {
        /// <summary>
        /// Optional OptionId that controls creation of the extension.
        /// </summary>
        [DefaultValue(null)]
        string OptionName { get; }
    }
}
