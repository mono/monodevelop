// Copyright (C) Microsoft Corporation.  All Rights Reserved.

namespace Microsoft.VisualStudio.Utilities.Implementation
{
    using System;
    using System.Collections.Generic;

    public interface IFileNameToContentTypeMetadata
    {
        string FileName { get; }
        IEnumerable<string> ContentTypes { get; }
    }
}

