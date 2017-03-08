//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// The metadata interface for exporters and importers of metadata on <see cref="IViewTaggerProvider"/> factories.
    /// </summary>
    public interface IViewTaggerMetadata : INamedTaggerMetadata
    {
        /// <summary>
        /// Text view roles to which the tagger provider applies. Default value of null is provided for backward
        /// compatibility.
        /// </summary>
        [DefaultValue(null)]
        IEnumerable<string> TextViewRoles { get; }
    }
}
