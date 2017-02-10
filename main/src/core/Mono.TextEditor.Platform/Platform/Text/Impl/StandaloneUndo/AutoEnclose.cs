// ****************************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ****************************************************************************

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
