//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using System.Collections.Generic;

    public interface ITextViewRoleMetadata
    {
        IEnumerable<string> TextViewRoles { get; }
    }
}