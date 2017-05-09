//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;

namespace Microsoft.VisualStudio.Text
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Find the index of the next non-whitespace character in a line.
        /// </summary>
        /// <param name="line">The line to search.</param>
        /// <param name="startIndex">The index at which to begin the search, relative to the start of the line.</param>
        /// <returns>The index, relative to the start of the line, of the first non-whitespace character whose index 
        /// is <paramref name="startIndex"/> or greater, or -1 if there are not any non-whitespace characters at that index or greater.</returns>
        /// <exception cref="ArgumentOutOfRangeException"> if <paramref name="startIndex"/> is negative.</exception>
        public static int IndexOfNextNonWhiteSpaceCharacter(this ITextSnapshotLine line, int startIndex)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("start");
            }

            // Take advantage of performant [] operators on the ITextSnapshot
            ITextSnapshot snapshot = line.Snapshot;
            int snapshotPos = line.Start.Position;
            for (int i = startIndex; i < line.Length; i++)
            {
                if (!char.IsWhiteSpace(snapshot[snapshotPos + i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}