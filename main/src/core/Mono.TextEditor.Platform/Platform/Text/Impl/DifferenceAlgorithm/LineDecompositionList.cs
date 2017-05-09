//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    // TODO: Replace the logic in this class with the upcoming Line/Word
    // split utility. 
    
    /// <summary>
    /// This is a decomposition of the given string into lines.
    /// </summary>
    internal sealed class LineDecompositionList : TokenizedStringList
    {
        public LineDecompositionList(string original)
            : base(original)
        {
            for (int i = 0; i < original.Length; )      //No increment.
            {
                int breakLength = TextUtilities.LengthOfLineBreak(original, i, original.Length);
                if (breakLength > 0)
                {
                    i += breakLength;
                    base.Boundaries.Add(i);
                }
                else
                    ++i;
            }
        }
    }
}
