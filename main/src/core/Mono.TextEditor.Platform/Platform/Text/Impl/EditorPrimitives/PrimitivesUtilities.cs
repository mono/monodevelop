//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using System;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// A set of utilities to help with various calculations
    /// in the editor primitives.
    /// </summary>
    internal static class PrimitivesUtilities
    {
        public static int GetColumnOfPoint(ITextSnapshot textSnapshot, SnapshotPoint pointPosition, SnapshotPoint startPosition, int tabSize, Func<SnapshotPoint, SnapshotPoint> getNextPosition)
        {
            int columnCount = 0;
            SnapshotPoint position = startPosition;
            while (position < pointPosition)
            {
                if (textSnapshot[position] == '\t')
                {
                    // If there is a tab in the text, then the column automatically jumps
                    // to the next tab stop.
                    columnCount = ((columnCount / tabSize) + 1) * tabSize;
                }
                else
                {
                    columnCount++;
                }
                position = getNextPosition(position);
            }
            return columnCount;
        }

        private static bool Edit(ITextBuffer buffer, Func<ITextEdit, bool> editAction)
        {
            using (ITextEdit edit = buffer.CreateEdit())
            {
                if (!editAction(edit))
                    return false;

                edit.Apply();

                if (edit.Canceled)
                    return false;
            }

            return true;
        }

        public static bool Delete(ITextBuffer buffer, int start, int length)
        {
            return Edit(buffer, edit => edit.Delete(start, length));
        }

        internal static bool Insert(ITextBuffer buffer, int position, string text)
        {
            return Edit(buffer, edit => edit.Insert(position, text));
        }

        internal static bool Delete(ITextBuffer buffer, Span span)
        {
            return Edit(buffer, edit => edit.Delete(span));
        }

        internal static bool Delete(SnapshotSpan span)
        {
            return Edit(span.Snapshot.TextBuffer, edit => edit.Delete(span));
        }

        internal static bool Replace(ITextBuffer buffer, Span span, string replacement)
        {
            return Edit(buffer, edit => edit.Replace(span, replacement));
        }
    }
}
