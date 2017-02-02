// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Metadata which includes Ordering and Content Types
    /// </summary>
    public interface IOrderableContentTypeMetadata : IContentTypeMetadata, IOrderable
    {
    }
}
