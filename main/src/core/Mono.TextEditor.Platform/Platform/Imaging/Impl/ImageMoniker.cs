//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
#if !TARGET_VS
using System;

namespace Microsoft.VisualStudio.Imaging.Interop
{
    public struct ImageMoniker
    {
        public Guid Guid;
        public int Id;

        public ImageMoniker(Guid guid, int id)
        {
            Guid = guid;
            Id   = id;
        }
    }
}
#endif
