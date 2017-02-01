// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Tagging
{
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// The metadata interface for exporters and importers of metadata on <see cref="IViewTaggerProvider"/> factories.
    /// </summary>
    public interface IViewTaggerMetadata : ITaggerMetadata
    {
        /// <summary>
        /// Text view roles to which the tagger provider applies. Default value of null is provided for backward
        /// compatibility.
        /// </summary>
        [DefaultValue(null)]
        IEnumerable<string> TextViewRoles { get; }
    }
}
