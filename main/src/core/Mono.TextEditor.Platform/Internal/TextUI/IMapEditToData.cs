// Copyright (c) Microsoft Corporation
// All rights reserved

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
