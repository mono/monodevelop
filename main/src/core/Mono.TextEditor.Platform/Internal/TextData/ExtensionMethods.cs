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

            // start with a small chunk size since few lines will start with enormous amounts of white space.
            int chunkSize = 32;
            int next = startIndex;
            int lineLength = line.Extent.Length;
            while (next < lineLength)
            {
                int length = Math.Min(chunkSize, line.Extent.End - (line.Extent.Start + next));
                string text = line.Snapshot.GetText(line.Extent.Start + next, length);
                for (int i = 0; i < length; ++i)
                {
                    if (!char.IsWhiteSpace(text[i]))
                    {
                        return next + i;
                    }
                }
                next += length;

                // don't let the chunks get too big; the whole point of this
                // method is to avoid giant string allocations. Use 2**14 as the threshold.
                if (chunkSize < 16384)
                {
                    chunkSize *= 2;
                }
            }

            return -1;
        }
    }
}