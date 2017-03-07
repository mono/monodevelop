//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Text.Utilities
{
    public interface IContentTypeMetadata
    {
        IEnumerable<string> ContentTypes { get; }
    }
}
