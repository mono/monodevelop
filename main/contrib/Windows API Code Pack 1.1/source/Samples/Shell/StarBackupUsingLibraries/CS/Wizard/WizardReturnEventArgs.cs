//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    public class WizardReturnEventArgs
    {
        WizardResult result;
        object data;

        public WizardReturnEventArgs(WizardResult result, object data)
        {
            this.result = result;
            this.data = data;
        }

        public WizardResult Result
        {
            get { return this.result; }
        }

        public object Data
        {
            get { return this.data; }
        }
    }
}
