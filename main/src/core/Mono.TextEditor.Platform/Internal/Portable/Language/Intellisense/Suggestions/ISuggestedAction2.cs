// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Extends <see cref="ISuggestedAction"/> by providing support for <see cref="DisplayTextSuffix"/> property.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedAction2 : ISuggestedAction
    {
        /// <summary>
        /// Gets the localized text representing a suffix to be added to the <see cref="ISuggestedAction.DisplayText"/>.
        /// </summary>
        string DisplayTextSuffix { get; }
    }
}
