//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;

namespace Microsoft.VisualStudio.Text.Utilities
{
    internal static class ArgumentValidation
    {
        [ContractArgumentValidator]
        public static void NotNull(object variable, string variableName)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(variableName);
            }

            Contract.EndContractBlock();
        }
    }
}
