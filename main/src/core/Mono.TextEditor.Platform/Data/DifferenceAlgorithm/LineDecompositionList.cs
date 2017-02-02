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
