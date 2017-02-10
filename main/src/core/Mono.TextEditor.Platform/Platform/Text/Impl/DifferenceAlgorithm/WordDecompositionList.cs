using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    // TODO: Replace the logic in this class with the upcoming Line/Word
    // split utility. 
    
    /// <summary>
    /// This is a word decomposition of the given string.
    /// </summary>
    internal sealed class WordDecompositionList : TokenizedStringList
    {
        public WordDecompositionList(string original, WordSplitBehavior splitBehavior) 
            : base(original)
        {
            if (original.Length > 0)
            {
                int lastCharType = GetCharType(original[0], splitBehavior);

                for (int i = 1; i < original.Length; i++)
                {
                    int newCharType = GetCharType(original[i], splitBehavior);

                    if (newCharType != lastCharType)
                    {
                        Boundaries.Add(i);
                        lastCharType = newCharType;
                    }
                }
            }
        }

        public WordDecompositionList(SnapshotSpan original, WordSplitBehavior splitBehavior)
            : base(original)
        {
            if (original.Length > 0)
            {
                int start = original.Start;

                int lastCharType = GetCharType(original.Snapshot[start], splitBehavior);

                for (int i = 1; i < original.Length; i++)
                {
                    int newCharType = GetCharType(original.Snapshot[i + start], splitBehavior);

                    if (newCharType != lastCharType)
                    {
                        Boundaries.Add(i);
                        lastCharType = newCharType;
                    }
                }
            }
        }

        private static int GetCharType(char c, WordSplitBehavior splitBehavior)
        {
            if (char.IsWhiteSpace(c))
                return 1;

            if (splitBehavior == WordSplitBehavior.WhiteSpace)
                return 0;

            if (char.IsPunctuation(c) || char.IsSymbol(c))
                return 3;

            if (splitBehavior == WordSplitBehavior.WhiteSpaceAndPunctuation)
                return 0;

            if (char.IsDigit(c) || char.IsNumber(c))
                return 2;

            if (char.IsLetter(c))
                return 4;

            return 0;
        }
    }
}
