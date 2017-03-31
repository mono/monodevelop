//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    internal static class EncodedLineBreaks
    {
        public static uint EncodePosition(int position, bool isSingleCharLineBreak)
        {
            return isSingleCharLineBreak ? (uint)position | 0x80000000
                                         : (uint)position;
        }

        public static int DecodePosition(uint encodedPosition)
        {
            return (int)(encodedPosition & 0x7FFFFFFF);
        }

        public static bool IsSingleCharLineBreak(uint encodedPosition)
        {
            return (encodedPosition & 0x80000000) != 0;
        }
    }
}