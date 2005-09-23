// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Gui.Search
{
	/// <summary>
	/// Implements the Knuth, Morris, Pratt searching algorithm.
	/// </summary>
	internal class KMPSearchStrategy : ISearchStrategy
	{
		string searchPattern;
		int[]  overlap;
		
		public void CompilePattern(SearchOptions options)
		{
			if (searchPattern != (options.IgnoreCase ? options.SearchPattern.ToUpper() : options.SearchPattern)) {
				searchPattern = options.IgnoreCase ? options.SearchPattern.ToUpper() : options.SearchPattern;
				overlap = new int[searchPattern.Length + 1];
				Preprocessing();
			}
		}
		
		void Preprocessing()
		{
			overlap[0] = -1;
			for (int i = 0, j = -1; i < searchPattern.Length;) {
				while (j >= 0 && searchPattern[i] != searchPattern[j]) {
					j = overlap[j];
				}
				++i;
				++j;
				overlap[i] = j;
			}
		}
		
		int InternalFindNext(ITextIterator textIterator, SearchOptions options)
		{
			int j = 0;
			if (!textIterator.MoveAhead(1)) {
				return -1;
			}
			while (true) { // until pattern found or Iterator finished
				while (j >= 0 && searchPattern[j] != (options.IgnoreCase ? Char.ToUpper(textIterator.GetCharRelative(j)) : textIterator.GetCharRelative(j))) {
					if (!textIterator.MoveAhead(j - overlap[j])) {
						return -1;
					}
					j = overlap[j];
				}
				if (++j >= searchPattern.Length) {
					if ((!options.SearchWholeWordOnly || SearchReplaceUtilities.IsWholeWordAt(textIterator, searchPattern.Length))) {
						return textIterator.Position;
					}
					if (!textIterator.MoveAhead(j - overlap[j])) {
						return -1;
					}
					j = overlap[j];
				}
			}			
		}
		
		public ISearchResult FindNext(ITextIterator textIterator, SearchOptions options, bool reverseSearch)
		{
			if (reverseSearch)
				throw new NotSupportedException ();
				
			int pos = textIterator.Position;
			
			int offset = InternalFindNext(textIterator, options);
			if (offset == -1) return null;
			
			if (textIterator.GetCharRelative (searchPattern.Length) == char.MinValue) {
				if (pos != offset)
					return FindNext(textIterator, options, false);
				else
					return null;
			}
			
			return new DefaultSearchResult (textIterator, searchPattern.Length);
		}
		
		public bool SupportsReverseSearch (ITextIterator textIterator, SearchOptions options)
		{
			return false;
		}
	}
}
