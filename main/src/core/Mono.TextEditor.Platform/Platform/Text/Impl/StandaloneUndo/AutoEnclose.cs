//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;

namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    internal delegate void AutoEncloseDelegate();

    internal class AutoEnclose : IDisposable 
    {
        private AutoEncloseDelegate end;

        public AutoEnclose(AutoEncloseDelegate end)
        {
            this.end = end;
        }

        public void Dispose()
        {
            if (end != null) end();
            GC.SuppressFinalize(this);
        }
    }
}
