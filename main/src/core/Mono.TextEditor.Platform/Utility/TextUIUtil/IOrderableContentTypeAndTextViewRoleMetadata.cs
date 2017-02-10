// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Metadata which includes Ordering, Content Types and Text View Roles
    /// </summary>
    public interface IOrderableContentTypeAndTextViewRoleMetadata : IContentTypeAndTextViewRoleMetadata, IOrderable
    {
    }
}
