//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;
    
    public interface IMapEditToData
    {
        int MapEditToData(int editPoint);
        int MapDataToEdit(int dataPoint);
    }
}
