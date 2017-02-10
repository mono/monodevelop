using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    abstract class DecompositionListMaker
    {
        protected DecompositionListMaker(StringDifferenceOptions options)
        {
            // Make a copy, so we don't accidentally modify the one we're given
            options = new StringDifferenceOptions(options);
            if (options.WordSplitBehavior == WordSplitBehavior.LanguageAppropriate)
                options.WordSplitBehavior = WordSplitBehavior.WhiteSpaceAndPunctuation;

            Options = options;
        }

        protected StringDifferenceOptions Options { get; private set; }

        public abstract IList<string> FilteredLineList { get; }
        public abstract ITokenizedStringListInternal UnfilteredLineList { get; }
        public abstract ITokenizedStringListInternal WordList { get; }
        public abstract ITokenizedStringListInternal CharacterList { get; }
    }

    class SnapshotSpanDecompositionListMaker : DecompositionListMaker
    {
        SnapshotSpan _snapshotSpan;
        Func<ITextSnapshotLine, string> _getLineTextCallback;

        public SnapshotSpanDecompositionListMaker(SnapshotSpan snapshotSpan, StringDifferenceOptions options, Func<ITextSnapshotLine, string> getLineTextCallback)
            : base(options)
        {
            _snapshotSpan = snapshotSpan;
            _getLineTextCallback = getLineTextCallback;
        }

        public override IList<string> FilteredLineList
        {
            get { return new SnapshotLineList(_snapshotSpan, _getLineTextCallback, Options); }
        }

        public override ITokenizedStringListInternal UnfilteredLineList
        {
            get 
            {
                // Ensure the options don't have ignore trim whitespace set
                StringDifferenceOptions options = new StringDifferenceOptions(Options);
                options.IgnoreTrimWhiteSpace = false;

                return new SnapshotLineList(_snapshotSpan, DefaultTextDifferencingService.DefaultGetLineTextCallback, options); 
            }
        }

        public override ITokenizedStringListInternal WordList
        {
            get { return new WordDecompositionList(_snapshotSpan, Options.WordSplitBehavior); }
        }

        public override ITokenizedStringListInternal CharacterList
        {
            get { return new CharacterDecompositionList(_snapshotSpan); }
        }
    }

    class StringDecompositionListMaker : DecompositionListMaker
    {
        string _string;

        public StringDecompositionListMaker(string s, StringDifferenceOptions options)
            : base(options)
        {
            _string = s;
        }

        public override IList<string> FilteredLineList
        {
            get
            {
                var unfilteredList = UnfilteredLineList;

                if (Options.IgnoreTrimWhiteSpace)
                {
                    List<string> filteredLines = new List<string>(unfilteredList.Count);
                    for (int i = 0; i < unfilteredList.Count; i++)
                    {
                        filteredLines.Add(unfilteredList[i].Trim());
                    }

                    return filteredLines;
                }
                else
                {
                    return unfilteredList;
                }
            }
        }

        public override ITokenizedStringListInternal UnfilteredLineList
        {
            get { return new LineDecompositionList(_string); }
        }

        public override ITokenizedStringListInternal WordList
        {
            get { return new WordDecompositionList(_string, Options.WordSplitBehavior); }
        }

        public override ITokenizedStringListInternal CharacterList
        {
            get { return new CharacterDecompositionList(_string); }
        }
    }
}
