//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
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

