// Copyright (C) Microsoft Corporation.  All Rights Reserved.

namespace Microsoft.VisualStudio.Utilities.Implementation
{
    using System;
    using System.Collections.Generic;

    public interface IFileExtensionToContentTypeMetadata
    {
        string FileExtension { get; }
        IEnumerable<string> ContentTypes { get; }
    }
}

